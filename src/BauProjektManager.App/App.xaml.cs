using System.IO;
using System.Windows;
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
            .MinimumLevel.Information()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console()
            .CreateLogger();

        Log.Information("=== BauProjektManager gestartet ===");
        Log.Information("Version: {Version}", "0.10.0");
        Log.Information("OS: {OS}", Environment.OSVersion);
        Log.Information("Machine: {Machine}", Environment.MachineName);

        // Check if setup is needed
        var settingsService = new AppSettingsService();
        var settings = settingsService.Load();

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

            settings = settingsService.Load(); // Reload after save
        }
        else
        {
            // Validate paths on every start
            var problems = AppSettingsService.ValidatePaths(settings);
            if (problems.Count > 0)
            {
                Log.Warning("Path problems detected: {Problems}", string.Join(", ", problems));
                var result = MessageBox.Show(
                    $"Pfad-Problem erkannt:\n\n{string.Join("\n", problems)}\n\nEinstellungen öffnen?",
                    "BauProjektManager",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
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

        // Now show main window and switch shutdown mode
        var mainWindow = new MainWindow();
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
