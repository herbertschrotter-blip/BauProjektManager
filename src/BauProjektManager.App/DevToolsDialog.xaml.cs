using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BauProjektManager.Domain.Interfaces;

namespace BauProjektManager.App;

public partial class DevToolsDialog : Window
{
    private readonly IDeveloperToolsService _devTools;
    private string _selectedReset = "DbOnly";

    private const string DeleteIcon = "🗑";

    private readonly Dictionary<string, string> _resetLabels = new()
    {
        { "DbOnly",       $"{DeleteIcon} Datenbank zurücksetzen und neu starten" },
        { "SettingsOnly", $"{DeleteIcon} Einstellungen zurücksetzen und neu starten" },
        { "FirstRun",     $"{DeleteIcon} Ersteinrichtung zurücksetzen und neu starten" },
        { "All",          $"{DeleteIcon} Alles zurücksetzen und neu starten" }
    };

    public DevToolsDialog(IDeveloperToolsService devTools)
    {
        InitializeComponent();
        _devTools = devTools;
        LoadSystemInfo();
        LoadLog();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // DPI is now part of GetDisplayInfo()

        // Display info is now loaded in LoadSystemInfo via GetDisplayInfo()
    }

    private void LoadSystemInfo()
    {
        var info = _devTools.GetSystemInfo();
        var lines = info.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(':', 2);
            if (parts.Length < 2) continue;
            var key = parts[0].Trim();
            var val = parts[1].Trim();

            switch (key)
            {
                case "App-Version":       TxtAppVersion.Text = val; break;
                case ".NET Runtime":      TxtRuntime.Text = val; break;
                case "Windows":           TxtWindows.Text = val; break;
                case "Rechner":           TxtMachine.Text = val; break;
                case "Benutzer":          TxtUser.Text = val; break;
                case "DB-Pfad":           TxtDbPath.Text = val; break;
                case "DB-Größe":          TxtDbSize.Text = val; break;
                case "Freier Speicher":   TxtFreeSpace.Text = val; break;
            }
        }

        // Display info (physical resolution + multi-monitor)
        var displayInfo = _devTools.GetDisplayInfo();
        var displayLines = displayInfo.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var monitorCount = "";
        var monitorDetails = new System.Text.StringBuilder();

        foreach (var dline in displayLines)
        {
            var dparts = dline.Split(':', 2);
            if (dparts.Length < 2) continue;
            var dkey = dparts[0].Trim();
            var dval = dparts[1].Trim();

            if (dkey == "Monitore")
                monitorCount = dval;
            else
                monitorDetails.AppendLine($"{dkey}: {dval}");
        }

        TxtResolution.Text = monitorCount + " Monitor(e)";
        TxtDpi.Text = monitorDetails.ToString().TrimEnd();

        TxtSettingsPath.Text = _devTools.SettingsPath;
        TxtLogPath.Text = _devTools.LogDirectory;
    }

    private void LoadLog()
    {
        TxtLogContent.Text = _devTools.ReadLogTail(200);
        LogScroller.ScrollToBottom();
    }

    private void OnSelectReset(object sender, MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.Border border) return;
        var tag = border.Tag?.ToString() ?? "DbOnly";
        SelectReset(tag);
    }

    private void SelectReset(string tag)
    {
        _selectedReset = tag;

        var borders = new[] { BorderDbOnly, BorderSettingsOnly, BorderFirstRun, BorderAll };
        var dots    = new[] { DotDbOnly, DotSettingsOnly, DotFirstRun, DotAll };
        var tags    = new[] { "DbOnly", "SettingsOnly", "FirstRun", "All" };

        for (int i = 0; i < tags.Length; i++)
        {
            bool active = tags[i] == tag;
            borders[i].BorderBrush = active
                ? FindResource("BpmAccentPrimary") as System.Windows.Media.Brush
                : FindResource("BpmBorderDefault") as System.Windows.Media.Brush;
            dots[i].Fill = active
                ? FindResource("BpmAccentPrimary") as System.Windows.Media.Brush
                : System.Windows.Media.Brushes.Transparent;
            dots[i].Stroke = active
                ? FindResource("BpmAccentPrimary") as System.Windows.Media.Brush
                : FindResource("BpmTextSecondary") as System.Windows.Media.Brush;
        }

        BtnReset.Content = _resetLabels[tag];
    }

    private void OnReset(object sender, RoutedEventArgs e)
    {
        var db = _devTools.DatabasePath;
        string message = _selectedReset switch
        {
            "DbOnly" =>
                $"Folgende Dateien werden gelöscht:\n\n  {db}\n  {db}-wal\n  {db}-shm\n\n" +
                "Die App wird danach neu gestartet.\n\nAlle Projektdaten gehen verloren!",
            "SettingsOnly" =>
                $"Folgende Datei wird gelöscht:\n\n  {_devTools.SettingsPath}\n\n" +
                "Die App wird danach neu gestartet.\n\nAlle Pfade und Einstellungen gehen verloren!",
            "FirstRun" =>
                "IsFirstRun wird auf true gesetzt.\n\n" +
                "Der Ersteinrichtungs-Dialog erscheint beim nächsten Start.\n" +
                "Daten und Pfade bleiben erhalten.",
            "All" =>
                $"Folgende Dateien werden gelöscht:\n\n  {db}\n  {db}-wal\n  {db}-shm\n  {_devTools.SettingsPath}\n\n" +
                "Die App startet danach neu — Ersteinrichtung wird angezeigt.\n\nAlle lokalen Daten gehen verloren!",
            _ => ""
        };

        string title = _selectedReset switch
        {
            "DbOnly"       => "Datenbank zurücksetzen",
            "SettingsOnly" => "Einstellungen zurücksetzen",
            "FirstRun"     => "Ersteinrichtung zurücksetzen",
            "All"          => "Komplett-Reset",
            _ => "Reset"
        };

        var result = MessageBox.Show(message, title, MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (result != MessageBoxResult.OK) return;

        Action shutdown = () => System.Windows.Application.Current.Shutdown();

        switch (_selectedReset)
        {
            case "DbOnly":       _devTools.RequestDatabaseReset(shutdown); break;
            case "SettingsOnly": _devTools.RequestSettingsReset(shutdown); break;
            case "FirstRun":     _devTools.RequestFirstRunReset(shutdown); break;
            case "All":          _devTools.RequestFullReset(shutdown); break;
        }
    }

    private void OnCopyBugReport(object sender, RoutedEventArgs e)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BauProjektManager Bug-Report");
        sb.AppendLine("============================");
        sb.AppendLine(_devTools.GetSystemInfo());
        sb.AppendLine(_devTools.GetDisplayInfo());
        sb.AppendLine($"Einstellungen:     {_devTools.SettingsPath}");
        sb.AppendLine($"Log-Verzeichnis:   {_devTools.LogDirectory}");
        sb.AppendLine();
        sb.AppendLine("--- LOG ---");
        sb.AppendLine(_devTools.ReadLogTail(200));
        Clipboard.SetText(sb.ToString());
        MessageBox.Show("Bug-Report in Zwischenablage kopiert.", "Kopiert", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnCopyLog(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(TxtLogContent.Text);
        MessageBox.Show("Log in Zwischenablage kopiert.", "Kopiert", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnOpenLogs(object sender, RoutedEventArgs e) => _devTools.OpenLogDirectory();
    private void OnRefreshLog(object sender, RoutedEventArgs e) => LoadLog();
    private void OnClose(object sender, RoutedEventArgs e) => Close();
}