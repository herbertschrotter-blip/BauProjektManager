using System.IO;
using System.Windows;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Dev;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
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
        Log.Information("=== BauProjektManager gestartet ===");
        var version = System.Reflection.Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString() ?? "unknown";
        Log.Information("Version: {Version}", version);
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
        sc.AddSingleton<IIdGenerator, UlidIdGenerator>();
        sc.AddSingleton<IUserContext>(sp => new LocalUserContext(sp.GetRequiredService<AppSettings>()));
        sc.AddSingleton<IDialogService, BpmDialogService>();
        sc.AddSingleton<ProjectDatabase>();

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
#if DEBUG
            var devTools = sp.GetService<IDeveloperToolsService>();
            return new MainWindow(db, idGen, dialog, devTools);
#else
            return new MainWindow(db, idGen, dialog);
#endif
        });

        Services = sc.BuildServiceProvider();
        Log.Information("DI Container aufgebaut — {Count} Services registriert", sc.Count);

        // --- MainWindow anzeigen ---
        var mainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        mainWindow.Show();
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
