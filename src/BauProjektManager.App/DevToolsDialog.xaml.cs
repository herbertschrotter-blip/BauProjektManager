using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;
using Serilog;

namespace BauProjektManager.App;

public partial class DevToolsDialog : Window
{
    private readonly IDeveloperToolsService _devTools;
    private readonly AppSettingsService? _settingsService;
    private string _selectedReset = "DbOnly";
    private bool _isInitializing = true;
    private string _lastLogContent = string.Empty;

    private static readonly Regex LogLinePattern = new(
        @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(?:\.\d+)?)\s+(\[\w+\])\s+(.*)$",
        RegexOptions.Compiled);

    private const string DeleteIcon = "🗑";
    private const long LargeFileWarningThresholdBytes = 2 * 1024 * 1024; // 2 MB

    private readonly Dictionary<string, string> _resetLabels = new()
    {
        { "DbOnly",       $"{DeleteIcon} Datenbank zurücksetzen und neu starten" },
        { "SettingsOnly", $"{DeleteIcon} Einstellungen zurücksetzen und neu starten" },
        { "FirstRun",     $"{DeleteIcon} Ersteinrichtung zurücksetzen und neu starten" },
        { "All",          $"{DeleteIcon} Alles zurücksetzen und neu starten" }
    };

    public DevToolsDialog(IDeveloperToolsService devTools, AppSettingsService? settingsService = null)
    {
        InitializeComponent();
        _devTools = devTools;
        _settingsService = settingsService;
        LoadSystemInfo();
        InitLogFilter();
        LoadLog();
        _isInitializing = false;
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
        var (mode, lineCount, selectedSession) = GetCurrentFilter();
        var content = ReadLogByMode(mode, lineCount, selectedSession, out var loadedLineCount);

        _lastLogContent = content;
        TxtLogContent.Inlines.Clear();
        foreach (var inline in BuildColoredLogInlines(content))
            TxtLogContent.Inlines.Add(inline);

        UpdateStatusHint(mode, lineCount, selectedSession, loadedLineCount);
        UpdateWarning(mode);
        LogScroller.ScrollToBottom();
    }

    /// <summary>
    /// Parst Log-Content zeilenweise und erzeugt farbige Inline-Runs.
    /// Format: "yyyy-MM-dd HH:mm:ss.fff [LVL] Message" + APP-START-Marker.
    /// Level-Mapping: VRB/DBG grau, INF blau-bold, WRN orange-bold, ERR/FTL rot-bold.
    /// </summary>
    private IEnumerable<Inline> BuildColoredLogInlines(string content)
    {
        if (string.IsNullOrEmpty(content))
            yield break;

        var brushSecondary = (Brush)FindResource("BpmTextSecondary");
        var brushPrimary = (Brush)FindResource("BpmTextPrimary");
        var brushAccent = (Brush)FindResource("BpmAccentPrimary");
        var brushInfo = (Brush)FindResource("BpmInfo");
        var brushWarning = (Brush)FindResource("BpmWarning");
        var brushError = (Brush)FindResource("BpmError");

        var lines = content.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');

            // App-Start-Marker: ganze Zeile blau bold
            if (line.Contains("═══ APP START"))
            {
                yield return new Run(line) { Foreground = brushAccent, FontWeight = FontWeights.Bold };
                yield return new LineBreak();
                continue;
            }

            // Standard Serilog-Format
            var match = LogLinePattern.Match(line);
            if (match.Success)
            {
                var levelTag = match.Groups[2].Value;
                var (levelBrush, levelBold) = levelTag switch
                {
                    "[VRB]" or "[Verbose]"     => (brushSecondary, FontWeights.Normal),
                    "[DBG]" or "[Debug]"       => (brushSecondary, FontWeights.Normal),
                    "[INF]" or "[Information]" => (brushInfo,      FontWeights.Bold),
                    "[WRN]" or "[Warning]"     => (brushWarning,   FontWeights.Bold),
                    "[ERR]" or "[Error]"       => (brushError,     FontWeights.Bold),
                    "[FTL]" or "[Fatal]"       => (brushError,     FontWeights.Bold),
                    _                          => (brushPrimary,   FontWeights.Normal),
                };

                yield return new Run(match.Groups[1].Value) { Foreground = brushSecondary };
                yield return new Run(" ") { Foreground = brushPrimary };
                yield return new Run(levelTag) { Foreground = levelBrush, FontWeight = levelBold };
                yield return new Run(" " + match.Groups[3].Value) { Foreground = brushPrimary };
                yield return new LineBreak();
                continue;
            }

            // Fallback: ganze Zeile als Standardtext
            yield return new Run(line) { Foreground = brushPrimary };
            yield return new LineBreak();
        }
    }

    private (string mode, int lineCount, int selectedSession) GetCurrentFilter()
    {
        if (_settingsService is null)
            return ("last200Lines", 200, 0);

        var device = _settingsService.LoadDevice();
        var filter = device.DevTools?.LogFilter ?? new LogFilterSettings();
        var mode = string.IsNullOrEmpty(filter.Mode) ? "last200Lines" : filter.Mode;
        var count = ClampLineCount(filter.CustomLineCount);
        return (mode, count, filter.SelectedSessionNumber);
    }

    private string ReadLogByMode(string mode, int customLineCount, int selectedSession, out int loadedLineCount)
    {
        string content = mode switch
        {
            "last200Lines"    => _devTools.ReadLogTail(200),
            "lastNLines"      => _devTools.ReadLogTail(customLineCount),
            "currentSession"  => _devTools.ReadCurrentSession(),
            "previousSession" => _devTools.ReadPreviousSession(),
            "entireFile"      => _devTools.ReadEntireLog(),
            "specificSession" => _devTools.ReadSessionByNumber(selectedSession),
            _                 => _devTools.ReadLogTail(200)
        };

        loadedLineCount = string.IsNullOrEmpty(content)
            ? 0
            : content.Count(c => c == '\n') + 1;
        return content;
    }

    private void InitLogFilter()
    {
        var (mode, lineCount, selectedSession) = GetCurrentFilter();

        // Sub-ComboBox CmbSession mit verfuegbaren Sessions befuellen (sortiert: neueste oben).
        CmbSession.Items.Clear();
        var sessions = _devTools.GetAvailableSessionNumbers(); // bereits absteigend sortiert
        var current = _devTools.GetCurrentSessionNumber();
        foreach (var n in sessions)
        {
            var label = n == current ? $"Session #{n} (aktuell)" : $"Session #{n}";
            CmbSession.Items.Add(new ComboBoxItem { Content = label, Tag = n });
        }

        // Haupt-Mode setzen
        ComboBoxItem? toSelect = null;
        foreach (var item in CmbLogFilter.Items)
        {
            if (item is ComboBoxItem cbi && cbi.Tag?.ToString() == mode)
            {
                toSelect = cbi;
                break;
            }
        }
        CmbLogFilter.SelectedItem = toSelect ?? CmbLogFilter.Items[0];

        // Sub-Session in CmbSession vorwaehlen
        if (mode == "specificSession" && selectedSession > 0)
        {
            foreach (ComboBoxItem item in CmbSession.Items)
            {
                if (item.Tag is int n && n == selectedSession)
                {
                    CmbSession.SelectedItem = item;
                    break;
                }
            }
        }
        if (CmbSession.SelectedItem is null && CmbSession.Items.Count > 0)
            CmbSession.SelectedIndex = 0; // Default: aktuelle Session

        // Custom-Line-Count-Eingabe
        TxtCustomLineCount.Text = lineCount.ToString();
        UpdateOptionalControlsVisibility(mode);
    }

    private void UpdateOptionalControlsVisibility(string mode)
    {
        bool showLineCount = mode == "lastNLines";
        TxtCustomLineCount.Visibility = showLineCount ? Visibility.Visible : Visibility.Collapsed;
        TxtLineRange.Visibility = showLineCount ? Visibility.Visible : Visibility.Collapsed;

        bool showSession = mode == "specificSession";
        CmbSession.Visibility = showSession ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateStatusHint(string mode, int lineCount, int selectedSession, int loadedLines)
    {
        var fileName = _devTools.GetCurrentLogFileName();
        var sizeBytes = _devTools.GetCurrentLogFileSize();
        var sizeText = FormatBytes(sizeBytes);

        var modeLabel = mode switch
        {
            "last200Lines"    => "letzte 200 Zeilen",
            "lastNLines"      => $"letzte {lineCount} Zeilen",
            "currentSession"  => "aktuelle Session",
            "previousSession" => "letzte Session",
            "entireFile"      => "komplettes Logfile",
            "specificSession" => $"Session #{selectedSession}",
            _                 => "letzte 200 Zeilen"
        };

        TxtLogStatus.Text = string.IsNullOrEmpty(fileName)
            ? $"{loadedLines} Zeilen ({modeLabel})"
            : $"{loadedLines} Zeilen geladen aus {fileName} ({sizeText}) - {modeLabel}";
    }

    private void UpdateWarning(string mode)
    {
        BorderLogWarning.Visibility = Visibility.Collapsed;

        if (mode == "entireFile")
        {
            var size = _devTools.GetCurrentLogFileSize();
            if (size > LargeFileWarningThresholdBytes)
            {
                TxtLogWarning.Text = $"⚠ Komplettes Logfile ist {FormatBytes(size)} groß — UI kann beim Wechsel kurz blockieren.";
                BorderLogWarning.Visibility = Visibility.Visible;
            }
        }
        else if (mode == "previousSession")
        {
            var content = TxtLogContent.Text;
            if (content.StartsWith("(Keine vorherige Session"))
            {
                TxtLogWarning.Text = "⚠ Keine vorherige Session gefunden — App wurde noch nicht zweimal gestartet, oder Logs sind älter als die letzten 2 Tage.";
                BorderLogWarning.Visibility = Visibility.Visible;
            }
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / 1024.0 / 1024.0:F1} MB";
    }

    private static int ClampLineCount(int value)
    {
        if (value < 10) return 10;
        if (value > 10000) return 10000;
        return value;
    }

    private static bool IsLineCountInRange(int value) => value >= 10 && value <= 10000;

    private void OnLogFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (CmbLogFilter.SelectedItem is not ComboBoxItem item) return;

        var mode = item.Tag?.ToString() ?? "last200Lines";
        UpdateOptionalControlsVisibility(mode);

        // Bei specificSession: Falls noch keine Session ausgewaehlt → erste (aktuelle) waehlen.
        int sessionNumber = 0;
        if (mode == "specificSession")
        {
            if (CmbSession.SelectedItem is null && CmbSession.Items.Count > 0)
                CmbSession.SelectedIndex = 0;
            if (CmbSession.SelectedItem is ComboBoxItem si && si.Tag is int n)
                sessionNumber = n;
        }

        SaveFilterSettings(mode, GetCurrentLineCountFromInput(), sessionNumber);
        LoadLog();
    }

    private void OnSessionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (CmbSession.SelectedItem is not ComboBoxItem item) return;
        if (item.Tag is not int sessionNumber) return;

        SaveFilterSettings("specificSession", GetCurrentLineCountFromInput(), sessionNumber);
        LoadLog();
    }

    private void OnCustomLineCountChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        ApplyCustomLineCount();
    }

    private void OnCustomLineCountKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !_isInitializing)
        {
            ApplyCustomLineCount();
        }
    }

    private void ApplyCustomLineCount()
    {
        var current = GetCurrentFilter();
        if (!int.TryParse(TxtCustomLineCount.Text, out var parsed))
        {
            BpmInfoDialog.ShowWarning("Bitte eine ganze Zahl eingeben.", "Ungueltige Eingabe");
            TxtCustomLineCount.Text = current.lineCount.ToString();
            return;
        }
        if (!IsLineCountInRange(parsed))
        {
            BpmInfoDialog.ShowWarning(
                $"Werte zwischen 10 und 10000 erlaubt. Eingegeben: {parsed}",
                "Ungueltiger Wert");
            TxtCustomLineCount.Text = current.lineCount.ToString();
            return;
        }
        SaveFilterSettings(GetCurrentMode(), parsed, current.selectedSession);
        LoadLog();
    }

    private int GetCurrentLineCountFromInput()
    {
        if (int.TryParse(TxtCustomLineCount.Text, out var parsed))
            return ClampLineCount(parsed);
        return 200;
    }

    private string GetCurrentMode()
    {
        if (CmbLogFilter.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            return tag;
        return "last200Lines";
    }

    private void SaveFilterSettings(string mode, int lineCount, int sessionNumber)
    {
        if (_settingsService is null) return;

        try
        {
            var device = _settingsService.LoadDevice();
            device.DevTools ??= new DevToolsSettings();
            device.DevTools.LogFilter ??= new LogFilterSettings();
            device.DevTools.LogFilter.Mode = mode;
            device.DevTools.LogFilter.CustomLineCount = lineCount;
            device.DevTools.LogFilter.SelectedSessionNumber = sessionNumber;
            _settingsService.SaveDevice(device);
        }
        catch (Exception ex)
        {
            Log.Warning("DevTools: LogFilter-Settings speichern fehlgeschlagen: {Error}", ex.Message);
        }
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
        Clipboard.SetText(_lastLogContent);
        BpmInfoDialog.ShowInfo("Log in Zwischenablage kopiert.", "Kopiert");
    }

    private void OnOpenLogs(object sender, RoutedEventArgs e) => _devTools.OpenLogDirectory();
    private void OnRefreshLog(object sender, RoutedEventArgs e) => LoadLog();
    private void OnClose(object sender, RoutedEventArgs e) => Close();
}