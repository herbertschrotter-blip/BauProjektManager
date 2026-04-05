using System.Windows;
using BauProjektManager.Domain.Interfaces;

namespace BauProjektManager.App;

public partial class DevToolsDialog : Window
{
    private readonly IDeveloperToolsService _devTools;

    public DevToolsDialog(IDeveloperToolsService devTools)
    {
        InitializeComponent();
        _devTools = devTools;
        TxtDbPath.Text = _devTools.DatabasePath;
        TxtLogPath.Text = _devTools.LogDirectory;
        TxtSettingsPath.Text = _devTools.SettingsPath;
        var sysInfo = _devTools.GetSystemInfo();

        // DPI via WPF — nur hier verfügbar
        try
        {
            var source = System.Windows.PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                var dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                sysInfo += $"DPI-Skalierung:    {dpiX / 96.0 * 100:F0}% ({dpiX:F0} dpi){Environment.NewLine}";
            }
        }
        catch { sysInfo += $"DPI-Skalierung:    (nicht ermittelbar){Environment.NewLine}"; }

        TxtSystemInfo.Text = sysInfo;
        LoadLog();
    }

    private void LoadLog()
    {
        TxtLogContent.Text = _devTools.ReadLogTail(200);
        LogScroller.ScrollToBottom();
    }

    private void OnResetDb(object sender, RoutedEventArgs e)
    {
        var db = _devTools.DatabasePath;
        var result = MessageBox.Show(
            $"Folgende Dateien werden gelöscht:\n\n" +
            $"  {db}\n" +
            $"  {db + "-wal"}\n" +
            $"  {db + "-shm"}\n\n" +
            "Die App wird danach neu gestartet.\n\n" +
            "Alle lokalen Daten gehen verloren!",
            "Lokale Datenbank löschen",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.OK) return;

        _devTools.RequestDatabaseReset(() => System.Windows.Application.Current.Shutdown());
    }

    private void OnFullReset(object sender, RoutedEventArgs e)
    {
        var db = _devTools.DatabasePath;
        var result = MessageBox.Show(
            $"Folgende Dateien werden gelöscht:\n\n" +
            $"  {db}\n" +
            $"  {db + "-wal"}\n" +
            $"  {db + "-shm"}\n" +
            $"  {_devTools.SettingsPath}\n\n" +
            "Die App startet danach neu — Ersteinrichtung wird angezeigt.\n\n" +
            "Alle lokalen Daten gehen verloren!",
            "Komplett-Reset",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.OK) return;
        _devTools.RequestFullReset(() => System.Windows.Application.Current.Shutdown());
    }

    private void OnOpenLogs(object sender, RoutedEventArgs e)
    {
        _devTools.OpenLogDirectory();
    }

    private void OnRefreshLog(object sender, RoutedEventArgs e)
    {
        LoadLog();
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
