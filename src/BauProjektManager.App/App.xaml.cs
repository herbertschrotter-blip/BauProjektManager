using System.IO;
using System.Windows;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Dev;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BauProjektManager.App;

/// <summary>
/// Application startup with Serilog logging, DI container, and first-run setup.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Zentraler DI-Container — für alle Services und ViewModels.
    /// </summary>
    public static ServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // --- Serilog ---
        string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BauProjektManager", "Logs", "BPM_.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console()
            .CreateLogger();

        Log.Debug("Serilog configured — MinimumLevel: Verbose");

        // App-Start-Marker für DevTools-Session-Erkennung (BPM-101).
        // Format: "═══ APP START · v{version} · {timestamp} · PID {pid} · Session #{n} ═══"
        // Session-Nummer = max(Session #N in allen BPM_*.log) + 1.
        var version = System.Reflection.Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString() ?? "unknown";
        var pid = Environment.ProcessId;
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var sessionNumber = DetermineNextSessionNumber(Path.GetDirectoryName(logPath) ?? "");
        Log.Information("═══ APP START · v{Version} · {Timestamp} · PID {Pid} · Session #{Session} ═══",
            version, timestamp, pid, sessionNumber);

        Log.Information("OS: {OS}", Environment.OSVersion);
        Log.Information("Machine: {Machine}", Environment.MachineName);

        // --- Settings laden ---
        var settingsService = new AppSettingsService();
        var settings = settingsService.Load();

        // --- First-Run / Setup ---
        if (settings.IsFirstRun)
        {
            Log.Information("First run detected — showing setup dialog");
            var setupDialog = new SetupDialog(settingsService, settings);
            setupDialog.ShowDialog();

            if (!setupDialog.SetupCompleted)
            {
                Log.Information("Setup cancelled — shutting down");
                Shutdown();
                return;
            }

            settings = settingsService.Load();
        }
        else
        {
            var problems = AppSettingsService.ValidatePaths(settings);
            if (problems.Count > 0)
            {
                Log.Warning("Path problems detected: {Problems}", string.Join(", ", problems));
                var dialog = new BpmConfirmDialog();
                dialog.Title = "Pfad-Problem erkannt";
                dialog.TxtMessage.Text = $"{string.Join("\n", problems)}\n\nEinstellungen öffnen?";
                dialog.TxtIcon.Text = "⚠";
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    var setupDialog = new SetupDialog(settingsService, settings);
                    setupDialog.ShowDialog();

                    if (!setupDialog.SetupCompleted)
                    {
                        Log.Information("Setup cancelled — shutting down");
                        Shutdown();
                        return;
                    }
                }
            }
        }

        Log.Information("BasePath: {BasePath}", settings.BasePath);
        Log.Information("ArchivePath: {ArchivePath}", settings.ArchivePath);

        // Validate shared config
        if (!string.IsNullOrEmpty(settings.BasePath))
        {
            var sharedDir = AppSettingsService.GetSharedConfigDir(settings.BasePath);
            var sharedPath = Path.Combine(sharedDir, "shared-config.json");
            if (!File.Exists(sharedPath))
                Log.Warning("Shared config not reachable at {Path}", sharedPath);
            else
                Log.Information("Shared config OK at {Path}", sharedPath);
        }

        // --- DI Container aufbauen ---
        var sc = new ServiceCollection();

        // Singleton: einmalig erstellt, überall dieselbe Instanz
        sc.AddSingleton(settings);
        sc.AddSingleton(settingsService);
        sc.AddSingleton<DeviceSettings>(sp => sp.GetRequiredService<AppSettingsService>().LoadDevice());
        sc.AddSingleton<IIdGenerator, UlidIdGenerator>();
        sc.AddSingleton<IUserContext>(sp => new LocalUserContext(sp.GetRequiredService<AppSettings>()));
        sc.AddSingleton<IDeviceContext>(sp => new LocalDeviceContext(sp.GetRequiredService<DeviceSettings>()));
        sc.AddSingleton<IDialogService, BpmDialogService>();
        sc.AddSingleton<IPersistenceRegistry, PersistenceRegistry>();

        // BPM-112 (ADR-060): Dateisystem-Ports — ein Adapter, drei Interfaces,
        // dieselbe Instanz. Kein direktes System.IO mehr ausserhalb des Adapters.
        sc.AddSingleton<LocalFileSystem>();
        sc.AddSingleton<IFileSystemReader>(sp => sp.GetRequiredService<LocalFileSystem>());
        sc.AddSingleton<IFileSystemWriter>(sp => sp.GetRequiredService<LocalFileSystem>());
        sc.AddSingleton<IPathService>(sp => sp.GetRequiredService<LocalFileSystem>());

        // ADR-060 Punkt 3: Shell-Launcher (Datei/Ordner in Standard-App bzw. Explorer)
        sc.AddSingleton<IFileLauncher, LocalFileLauncher>();

        // ADR-062/063 (Addendum Teil 47): EINE Engine für Rendern + Text —
        // PDFium via Docnet.Core. Bild-Pixel und Zeichen-Boxen entstehen im
        // selben Engine-Durchlauf/Koordinatenraum (der "Acrobat-Weg").
        sc.AddSingleton<PdfiumPdfService>();
        sc.AddSingleton<IPdfRenderService>(sp => sp.GetRequiredService<PdfiumPdfService>());
        sc.AddSingleton<IPdfTextService>(sp => sp.GetRequiredService<PdfiumPdfService>());

        sc.AddSingleton<ProjectDatabase>();

        // BPM-108: Segmenttyp-Katalog (Phase A) — vor ProfileManager registrieren,
        // damit der Manager ihn fuer ProfileHealth-Berechnung bekommt.
        sc.AddSingleton<ISegmentTypeRepository, SegmentTypeRepository>();
        sc.AddSingleton<SegmentTypeSeedService>();
        sc.AddSingleton<ISegmentTypeCatalog, SegmentTypeCatalog>();

        sc.AddSingleton<IProfileManager>(sp => new ProfileManager(
            sp.GetRequiredService<IIdGenerator>(),
            sp.GetService<IPersistenceRegistry>(),
            sp.GetService<ISegmentTypeCatalog>()));

        // BPM-108 Phase B: DevTool-Befehl fuer Schema-v4-Reset
        sc.AddSingleton<IProfileArchiveService, ProfileArchiveService>();

        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BauProjektManager", "Logs");

#if DEBUG
        sc.AddSingleton<IDeveloperToolsService>(sp =>
            new DeveloperToolsService(
                sp.GetRequiredService<ProjectDatabase>().GetDatabasePath(),
                logDir));
#endif

        // MainWindow
        sc.AddSingleton(sp =>
        {
            var db = sp.GetRequiredService<ProjectDatabase>();
            var idGen = sp.GetRequiredService<IIdGenerator>();
            var dialog = sp.GetRequiredService<IDialogService>();
            var profileManager = sp.GetRequiredService<IProfileManager>();
            var settingsService = sp.GetRequiredService<AppSettingsService>();
            var catalog = sp.GetService<ISegmentTypeCatalog>();
            var repo = sp.GetService<ISegmentTypeRepository>();
            var archive = sp.GetService<IProfileArchiveService>();
            var pdfRender = sp.GetService<IPdfRenderService>();
            var pdfText = sp.GetService<IPdfTextService>();
            var fileLauncher = sp.GetService<IFileLauncher>();
#if DEBUG
            var devTools = sp.GetService<IDeveloperToolsService>();
            var registry = sp.GetService<IPersistenceRegistry>();
            return new MainWindow(db, idGen, dialog, profileManager, settingsService, devTools, registry, catalog, repo, archive,
                pdfRender, fileLauncher, pdfText);
#else
            return new MainWindow(db, idGen, dialog, profileManager, settingsService,
                persistenceRegistry: null, segmentTypeCatalog: catalog,
                segmentTypeRepository: repo, profileArchiveService: archive,
                pdfRenderService: pdfRender, fileLauncher: fileLauncher,
                pdfTextService: pdfText);
#endif
        });

        Services = sc.BuildServiceProvider();
        Log.Information("DI Container aufgebaut — {Count} Services registriert", sc.Count);

        // BPM-104.02: zentrale Persistenz-Registrierung nach DI-Build
        InitializePersistenceRegistry(settings, logDir);

        // BPM-108: Built-in Segmenttyp-Seed beim App-Start
        try
        {
            Services.GetRequiredService<SegmentTypeSeedService>().Seed();
        }
        catch (Exception ex)
        {
            Log.Warning("BPM-108: Segmenttyp-Seed fehlgeschlagen: {Error}", ex.Message);
        }

        // BPM-108 Phase C: WizardCatalogContext fuer XAML-Converter aktivieren
        WizardCatalogContext.Initialize(Services.GetService<ISegmentTypeCatalog>());

        // --- MainWindow anzeigen ---
        var mainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        mainWindow.Show();
    }

    /// <summary>
    /// BPM-104.02: Registriert beim Start die zentralen Persistenz-Pfade
    /// (device-settings, bpm.db, aktuelles Log-File, shared-config wenn BasePath gesetzt)
    /// und triggert einen FS-Scan ueber alle bekannten Patterns.
    /// </summary>
    private static void InitializePersistenceRegistry(BauProjektManager.Domain.Models.AppSettings settings, string logDir)
    {
        try
        {
            var registry = Services.GetRequiredService<IPersistenceRegistry>();
            var localAppData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BauProjektManager");

            // device-settings.json
            var deviceSettingsPath = Path.Combine(localAppData, "device-settings.json");
            if (File.Exists(deviceSettingsPath))
            {
                registry.Register(new BauProjektManager.Domain.Models.PersistenceEntry(
                    "device-settings.json", deviceSettingsPath,
                    BauProjektManager.Domain.Enums.PersistenceType.Config,
                    BauProjektManager.Domain.Enums.PersistenceScope.Local,
                    "Geraetespezifische Einstellungen (DeviceId, Pfade, DevTools)"));
            }

            // Aktuelles Log-File (Serilog rolling daily)
            var todayLog = Path.Combine(logDir, $"BPM_{DateTime.Now:yyyyMMdd}.log");
            if (File.Exists(todayLog))
            {
                registry.Register(new BauProjektManager.Domain.Models.PersistenceEntry(
                    Path.GetFileName(todayLog), todayLog,
                    BauProjektManager.Domain.Enums.PersistenceType.Log,
                    BauProjektManager.Domain.Enums.PersistenceScope.Local,
                    "Aktuelles Serilog-Logfile"));
            }

            // shared-config.json (CloudShared)
            if (!string.IsNullOrEmpty(settings.BasePath))
            {
                var sharedDir = AppSettingsService.GetSharedConfigDir(settings.BasePath);
                var sharedPath = Path.Combine(sharedDir, "shared-config.json");
                if (File.Exists(sharedPath))
                {
                    registry.Register(new BauProjektManager.Domain.Models.PersistenceEntry(
                        "shared-config.json", sharedPath,
                        BauProjektManager.Domain.Enums.PersistenceType.Config,
                        BauProjektManager.Domain.Enums.PersistenceScope.CloudShared,
                        "Geteilte Konfiguration (FolderTemplate, Listen, Rollen)"));
                }
            }

            // BPM-106: Projekt-Roots aus DB laden, damit RescanFilesystem
            // jedes <projectRoot>\.bpm\ scannt (manifest.json, project.json,
            // profiles/*.json sichtbar im DevTools-Reset-Tab).
            var projectRoots = Array.Empty<string>();
            try
            {
                var projectDb = Services.GetRequiredService<ProjectDatabase>();
                projectRoots = projectDb.LoadAllProjects()
                    .Where(p => !string.IsNullOrWhiteSpace(p.Paths.Root))
                    .Select(p => p.Paths.Root)
                    .ToArray();
            }
            catch (Exception ex)
            {
                Log.Warning("PersistenceRegistry: Projekt-Roots laden fehlgeschlagen: {Error}", ex.Message);
            }

            // FS-Scan: ergaenzt alle nicht-registrierten Files (Logs, .bpm/, etc.)
            registry.RescanFilesystem(settings.BasePath, projectRoots);

            Log.Debug("PersistenceRegistry initialisiert: {Count} Eintraege ({Roots} Projekt-Roots gescannt)",
                registry.GetAll().Count, projectRoots.Length);
        }
        catch (Exception ex)
        {
            Log.Warning("PersistenceRegistry-Initialisierung fehlgeschlagen: {Error}", ex.Message);
        }
    }

    /// <summary>
    /// Bestimmt die nächste Session-Nummer für den App-Start-Marker.
    /// Scannt alle BPM_*.log Files nach "Session #N" Mustern und liefert max+1.
    /// Wenn kein Marker gefunden: startet bei 1.
    /// </summary>
    private static int DetermineNextSessionNumber(string logDir)
    {
        try
        {
            if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
                return 1;

            var pattern = new System.Text.RegularExpressions.Regex(
                @"Session #(\d+)",
                System.Text.RegularExpressions.RegexOptions.Compiled);

            int maxSession = 0;
            foreach (var file in Directory.GetFiles(logDir, "BPM_*.log"))
            {
                try
                {
                    using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream);
                    string? line;
                    while ((line = reader.ReadLine()) is not null)
                    {
                        var match = pattern.Match(line);
                        if (match.Success && int.TryParse(match.Groups[1].Value, out var n) && n > maxSession)
                            maxSession = n;
                    }
                }
                catch { /* skip locked / unreadable files */ }
            }
            return maxSession + 1;
        }
        catch
        {
            return 1;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("=== BauProjektManager beendet ===");
        Log.CloseAndFlush();

        if (Services is IDisposable disposable)
            disposable.Dispose();

        base.OnExit(e);
    }
}
