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
        LoadLog();
    }

    private void LoadLog()
    {
        TxtLogContent.Text = _devTools.ReadLogTail(200);
        LogScroller.ScrollToBottom();
    }

    private void OnResetDb(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            $"Folgende Dateien werden gelöscht:\n\n" +
            $"  {_devTools.DatabasePath}\n" +
            $"  {_devTools.DatabasePath}-wal\n" +
            $"  {_devTools.DatabasePath}-shm\n\n" +
            "Die App wird danach neu gestartet.\n\n" +
            "Alle lokalen Daten gehen verloren!",
            "Lokale Datenbank löschen",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.OK) return;

        _devTools.RequestDatabaseResetAndRestart();
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
