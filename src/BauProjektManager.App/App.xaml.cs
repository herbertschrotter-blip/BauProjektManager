using System.IO;
using System.Windows;
using BauProjektManager.Infrastructure.Dev;
using BauProjektManager.Infrastructure.Persistence;
using Serilog;

namespace BauProjektManager.App;

/// <summary>
/// Application startup with Serilog logging and first-run setup.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Prevent app from closing when setup dialog closes
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Log-Ordner: %LocalAppData%\BauProjektManager\Logs\
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

        // Check if setup is needed
        var settingsService = new AppSettingsService();
        Log.Debug("Service registered: {Service}", "AppSettingsService");

        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BauProjektManager", "settings.json");
        Log.Debug("Loading settings from {Path}", settingsPath);
        var settings = settingsService.Load();

        if (settings.IsFirstRun)
        {
            Log.Debug("First run detected — showing setup dialog");
            Log.Information("First run detected — showing setup dialog");
            var setupDialog = new SetupDialog(settingsService, settings);
            setupDialog.ShowDialog();

            if (!setupDialog.SetupCompleted)
            {
                Log.Information("Setup cancelled — shutting down");
                Shutdown();
                return;
            }

            settings = settingsService.Load(); // Reload after save
        }
        else
        {
            // Validate paths on every start
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

        // Validate shared config reachability
        if (!string.IsNullOrEmpty(settings.BasePath))
        {
            var sharedDir = AppSettingsService.GetSharedConfigDir(settings.BasePath);
            var sharedPath = Path.Combine(sharedDir, "shared-config.json");
            if (!File.Exists(sharedPath))
            {
                Log.Warning("Shared config not reachable at {Path}", sharedPath);
            }
            else
            {
                Log.Information("Shared config OK at {Path}", sharedPath);
            }
        }

        // Now show main window and switch shutdown mode
        var userContext = new Infrastructure.Services.LocalUserContext(settings);
        var db = new ProjectDatabase(new Infrastructure.Services.UlidIdGenerator(), userContext);
        Log.Debug("Service registered: {Service}", "ProjectDatabase");
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BauProjektManager", "Logs");

#if DEBUG
        var devTools = new DeveloperToolsService(db.GetDatabasePath(), logDir);
        Log.Debug("Service registered: {Service}", "DeveloperToolsService");
        Log.Debug("Creating MainWindow");
        var mainWindow = new MainWindow(devTools);
#else
        Log.Debug("Creating MainWindow");
        var mainWindow = new MainWindow(null);
#endif
        MainWindow = mainWindow;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("=== BauProjektManager beendet ===");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
