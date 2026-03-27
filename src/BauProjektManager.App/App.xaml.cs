using System.IO;
using System.Windows;
using Serilog;

namespace BauProjektManager.App;

/// <summary>
/// Application startup with Serilog logging.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
        Log.Information("Version: {Version}", "0.5.0");
        Log.Information("OS: {OS}", Environment.OSVersion);
        Log.Information("Machine: {Machine}", Environment.MachineName);

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("=== BauProjektManager beendet ===");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
