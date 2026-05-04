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
    private readonly IPersistenceRegistry? _persistenceRegistry;
    private readonly System.Collections.ObjectModel.ObservableCollection<InventoryItemViewModel> _inventoryItems = new();
    private string _selectedReset = "DbOnly";
    private bool _isInitializing = true;
    private string _lastLogContent = string.Empty;

    // BPM-103: LogFilter wird nur beim Window-Close persistiert (nicht bei jedem Combo-Wechsel).
    // Initial-Werte (beim Dialog-Open) und Current-Werte (laufende Auswahl) getrennt halten.
    private string _initialMode = "last200Lines";
    private int _initialLineCount = 200;
    private int _initialSessionNumber = 0;
    private string _currentMode = "last200Lines";
    private int _currentLineCount = 200;
    private int _currentSessionNumber = 0;

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
        { "All",          $"{DeleteIcon} Alles zurücksetzen und neu starten" },
        { "Logs",         $"{DeleteIcon} Logs löschen (kein Restart)" }
    };

    public DevToolsDialog(IDeveloperToolsService devTools, AppSettingsService? settingsService = null, IPersistenceRegistry? persistenceRegistry = null)
    {
        InitializeComponent();
        _devTools = devTools;
        _settingsService = settingsService;
        _persistenceRegistry = persistenceRegistry;
        LoadSystemInfo();
        InitLogFilter();
        LoadLog();
        LoadInventory();
        _isInitializing = false;
    }

    /// <summary>
    /// BPM-104.04: Laedt Persistenz-Inventar fuer Detail-Auswahl im Reset-Tab.
    /// Gruppiert per CollectionViewSource nach Type (Database/Config/Log/...).
    /// </summary>
    private void LoadInventory()
    {
        _inventoryItems.Clear();
        if (_persistenceRegistry is null)
        {
            TxtInventoryStatus.Text = "Persistenz-Registry nicht verfuegbar.";
            return;
        }

        // FS-Scan triggern (zusaetzlich zu in-memory Eintraegen)
        var basePath = _settingsService?.LoadDevice().BasePath;
        _persistenceRegistry.RescanFilesystem(basePath, Array.Empty<string>());

        var entries = _persistenceRegistry.GetAll();
        foreach (var entry in entries)
        {
            var (sizeText, modText) = ReadFileMeta(entry.AbsolutePath);
            var (scopeLabel, scopeColor) = MapScope(entry.Scope);
            _inventoryItems.Add(new InventoryItemViewModel
            {
                DisplayName = entry.DisplayName,
                AbsolutePath = entry.AbsolutePath,
                Type = MapTypeLabel(entry.Type),
                Scope = entry.Scope.ToString(),
                ScopeLabel = scopeLabel,
                ScopeColor = scopeColor,
                SizeText = sizeText,
                ModifiedText = modText,
                IsSelected = false
            });
        }

        // Gruppierung nach Type-Label
        var view = new System.Windows.Data.CollectionViewSource { Source = _inventoryItems };
        view.GroupDescriptions.Add(new System.Windows.Data.PropertyGroupDescription("Type"));
        LstInventory.ItemsSource = view.View;

        TxtInventoryStatus.Text = $"{entries.Count} Eintraege gefunden, gruppiert nach Typ. Group-Checkbox markiert/entmarkiert alle Items in der Gruppe.";
    }

    /// <summary>
    /// Mappt PersistenceType auf benutzerfreundlichen Gruppen-Header.
    /// </summary>
    private static string MapTypeLabel(BauProjektManager.Domain.Enums.PersistenceType type) => type switch
    {
        BauProjektManager.Domain.Enums.PersistenceType.Database    => "Datenbanken",
        BauProjektManager.Domain.Enums.PersistenceType.Config      => "Konfiguration",
        BauProjektManager.Domain.Enums.PersistenceType.Log         => "Logs",
        BauProjektManager.Domain.Enums.PersistenceType.ProjectData => "Projekt-Daten",
        BauProjektManager.Domain.Enums.PersistenceType.Cache       => "Cache",
        _                                                          => "Sonstige"
    };

    private static (string label, string color) MapScope(BauProjektManager.Domain.Enums.PersistenceScope scope) => scope switch
    {
        BauProjektManager.Domain.Enums.PersistenceScope.Local        => ("LOKAL",   "#37373D"),
        BauProjektManager.Domain.Enums.PersistenceScope.CloudShared  => ("CLOUD",   "#04395E"),
        BauProjektManager.Domain.Enums.PersistenceScope.ProjectLocal => ("PROJEKT", "#0F6E56"),
        _                                                             => ("?",       "#37373D")
    };

    private static (string size, string modified) ReadFileMeta(string path)
    {
        try
        {
            if (!System.IO.File.Exists(path)) return ("(fehlt)", "");
            var fi = new System.IO.FileInfo(path);
            string size = fi.Length switch
            {
                < 1024            => $"{fi.Length} B",
                < 1024 * 1024     => $"{fi.Length / 1024.0:F1} KB",
                _                 => $"{fi.Length / 1024.0 / 1024.0:F1} MB"
            };
            return (size, fi.LastWriteTime.ToString("dd.MM. HH:mm"));
        }
        catch { return ("?", ""); }
    }

    /// <summary>
    /// BPM-104.04: Group-Checkbox toggelt alle Items in der Gruppe an/aus.
    /// </summary>
    private void OnGroupCheckClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox cb) return;
        if (cb.Tag is not string groupName) return;

        var newState = cb.IsChecked == true;
        foreach (var item in _inventoryItems.Where(i => i.Type == groupName))
        {
            item.IsSelected = newState;
        }
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
        // BPM-103: nutzt _current* statt GetCurrentFilter() — laufende UI-Auswahl, nicht persistierter Stand
        var content = ReadLogByMode(_currentMode, _currentLineCount, _currentSessionNumber, out var loadedLineCount);

        _lastLogContent = content;
        TxtLogContent.Inlines.Clear();
        foreach (var inline in BuildColoredLogInlines(content))
            TxtLogContent.Inlines.Add(inline);

        UpdateStatusHint(_currentMode, _currentLineCount, _currentSessionNumber, loadedLineCount);
        UpdateWarning(_currentMode);
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

        // BPM-103: Initial- und Current-State festhalten fuer Diff-Check beim Close
        _initialMode = mode;
        _initialLineCount = lineCount;
        _initialSessionNumber = selectedSession;
        _currentMode = mode;
        _currentLineCount = lineCount;
        _currentSessionNumber = selectedSession;

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
        _currentMode = mode;

        // Bei specificSession: Falls noch keine Session ausgewaehlt → erste (aktuelle) waehlen.
        if (mode == "specificSession")
        {
            if (CmbSession.SelectedItem is null && CmbSession.Items.Count > 0)
                CmbSession.SelectedIndex = 0;
            if (CmbSession.SelectedItem is ComboBoxItem si && si.Tag is int n)
                _currentSessionNumber = n;
        }

        // BPM-103: kein SaveFilterSettings — wird beim Window-Close persistiert
        LoadLog();
    }

    private void OnSessionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (CmbSession.SelectedItem is not ComboBoxItem item) return;
        if (item.Tag is not int sessionNumber) return;

        _currentMode = "specificSession";
        _currentSessionNumber = sessionNumber;
        // BPM-103: kein SaveFilterSettings — wird beim Window-Close persistiert
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
        if (!int.TryParse(TxtCustomLineCount.Text, out var parsed))
        {
            BpmInfoDialog.ShowWarning("Bitte eine ganze Zahl eingeben.", "Ungueltige Eingabe");
            TxtCustomLineCount.Text = _currentLineCount.ToString();
            return;
        }
        if (!IsLineCountInRange(parsed))
        {
            BpmInfoDialog.ShowWarning(
                $"Werte zwischen 10 und 10000 erlaubt. Eingegeben: {parsed}",
                "Ungueltiger Wert");
            TxtCustomLineCount.Text = _currentLineCount.ToString();
            return;
        }
        _currentLineCount = parsed;
        _currentMode = GetCurrentMode();
        // BPM-103: kein SaveFilterSettings — wird beim Window-Close persistiert
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

    /// <summary>
    /// BPM-103: Persistiert LogFilter-Settings beim Window-Close — genau einmal,
    /// und nur wenn sich was vs. Initial-Wert geaendert hat.
    /// </summary>
    private void PersistFilterSettingsIfChanged()
    {
        if (_settingsService is null) return;
        if (_currentMode == _initialMode
            && _currentLineCount == _initialLineCount
            && _currentSessionNumber == _initialSessionNumber)
        {
            return; // Nichts geaendert
        }
        SaveFilterSettings(_currentMode, _currentLineCount, _currentSessionNumber);
        Log.Debug("DevTools: LogFilter persisted on close");
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        PersistFilterSettingsIfChanged();
    }

    /// <summary>
    /// BPM-104.04 Polish: Quick-Reset-Card-Klick markiert Items im Detail-Inventar
    /// (statt direkter Reset-Aktion). Loeschen erfolgt einheitlich ueber Bulk-Delete-Button.
    /// </summary>
    private void OnQuickSelect(object sender, MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.Border border) return;
        var tag = border.Tag?.ToString() ?? "DbOnly";

        // Erst alle deselect, dann je nach Tag selektieren
        foreach (var item in _inventoryItems) item.IsSelected = false;

        switch (tag)
        {
            case "DbOnly":
                foreach (var i in _inventoryItems.Where(x => x.Type == "Datenbanken")) i.IsSelected = true;
                break;
            case "SettingsOnly":
                foreach (var i in _inventoryItems.Where(x => x.Type == "Konfiguration"
                                                            && x.AbsolutePath.EndsWith("device-settings.json", StringComparison.OrdinalIgnoreCase)))
                    i.IsSelected = true;
                break;
            case "Logs":
                foreach (var i in _inventoryItems.Where(x => x.Type == "Logs")) i.IsSelected = true;
                break;
            case "All":
                foreach (var i in _inventoryItems) i.IsSelected = true;
                break;
            case "FirstRun":
                // Spezial: kein File-Loesch — markiert nichts, FirstRun-Reset ist Toggle (nicht Delete).
                MessageBox.Show(
                    "Ersteinrichtung-Reset setzt nur isFirstRun=true in device-settings.json.\n\nKein Datei-Löschen — wähle den Eintrag manuell aus oder nutze einen anderen Quick-Reset.",
                    "FirstRun ist ein Toggle",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
        }

        SelectReset(tag);
    }

    private void SelectReset(string tag)
    {
        _selectedReset = tag;

        var borders = new[] { BorderDbOnly, BorderSettingsOnly, BorderFirstRun, BorderAll, BorderLogs };
        var tags    = new[] { "DbOnly", "SettingsOnly", "FirstRun", "All", "Logs" };

        for (int i = 0; i < tags.Length; i++)
        {
            bool active = tags[i] == tag;
            borders[i].BorderBrush = active
                ? FindResource("BpmAccentPrimary") as System.Windows.Media.Brush
                : FindResource("BpmBorderDefault") as System.Windows.Media.Brush;
            borders[i].BorderThickness = active
                ? new Thickness(2)
                : new Thickness(1);
        }
    }

    // Hinweis (BPM-104.04 Polish, v0.28.22): OnReset wird nicht mehr aus dem UI aufgerufen
    // (BtnReset entfernt). Methode bleibt nur als Fallback fuer kuenftige Quick-Action-Buttons.
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
            "Logs" =>
                $"Alle BPM_*.log Files in:\n\n  {_devTools.LogDirectory}\n\nwerden gelöscht.\n\nKein Restart nötig.",
            _ => ""
        };

        string title = _selectedReset switch
        {
            "DbOnly"       => "Datenbank zurücksetzen",
            "SettingsOnly" => "Einstellungen zurücksetzen",
            "FirstRun"     => "Ersteinrichtung zurücksetzen",
            "All"          => "Komplett-Reset",
            "Logs"         => "Logs löschen",
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
            case "Logs":
                int count = _devTools.DeleteAllLogs();
                MessageBox.Show($"{count} Logfiles gelöscht.", "Logs gelöscht", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadInventory(); // Inventar refreshen
                break;
        }
    }

    /// <summary>
    /// BPM-104.04: Bulk-Delete der ausgewaehlten Inventar-Eintraege.
    /// Wenn bpm.db oder device-settings.json in Auswahl: nutzt RequestXxxReset (mit Restart),
    /// sonst direktes DeleteFiles ohne Restart.
    /// </summary>
    private void OnBulkDelete(object sender, RoutedEventArgs e)
    {
        var selected = _inventoryItems.Where(i => i.IsSelected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Keine Files ausgewählt.", "Bulk-Delete", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var paths = selected.Select(s => s.AbsolutePath).ToList();
        bool hasDb = paths.Any(p => p.EndsWith("bpm.db", StringComparison.OrdinalIgnoreCase));
        bool hasSettings = paths.Any(p => p.EndsWith("device-settings.json", StringComparison.OrdinalIgnoreCase));

        string warning = (hasDb || hasSettings)
            ? "\n\n⚠ Auswahl enthaelt aktive App-Dateien (DB / Settings) — App wird nach Loeschung neu gestartet."
            : "\n\nKein Restart noetig.";
        var msg = $"Folgende {selected.Count} Files werden gelöscht:\n\n" +
                  string.Join("\n", paths.Take(10)) +
                  (paths.Count > 10 ? $"\n... und {paths.Count - 10} weitere" : "") +
                  warning + "\n\nUnwiderruflich. Fortfahren?";

        var result = MessageBox.Show(msg, "Bulk-Delete bestätigen", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (result != MessageBoxResult.OK) return;

        Action shutdown = () => System.Windows.Application.Current.Shutdown();

        if (hasDb && hasSettings)
        {
            _devTools.RequestFullReset(shutdown);
            return;
        }
        if (hasDb)
        {
            _devTools.RequestDatabaseReset(shutdown);
            return;
        }
        if (hasSettings)
        {
            _devTools.RequestSettingsReset(shutdown);
            return;
        }

        // Sonst: direkter Delete ohne Restart (Logs, shared-config, etc.)
        var deleted = _devTools.DeleteFiles(paths);
        MessageBox.Show($"{deleted} von {paths.Count} Files gelöscht.", "Bulk-Delete fertig", MessageBoxButton.OK, MessageBoxImage.Information);

        foreach (var path in paths)
        {
            if (!System.IO.File.Exists(path))
                _persistenceRegistry?.Unregister(path);
        }
        LoadInventory();
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

    // BPM-104.03 — Pfad-Aktionen im System-Info-Tab.
    // Tag des Buttons = Pfad (kann File oder Verzeichnis sein).

    private void OnPathOpenFolder(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string path || string.IsNullOrEmpty(path)) return;
        var folder = System.IO.File.Exists(path)
            ? System.IO.Path.GetDirectoryName(path)
            : path;
        if (string.IsNullOrEmpty(folder) || !System.IO.Directory.Exists(folder)) return;
        try { System.Diagnostics.Process.Start("explorer.exe", folder); }
        catch (Exception ex) { Log.Warning("OpenFolder fehlgeschlagen: {Error}", ex.Message); }
    }

    private void OnPathOpenWithDefault(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string path || string.IsNullOrEmpty(path)) return;
        if (!System.IO.File.Exists(path)) return;
        try
        {
            // UseShellExecute=true triggert Windows-Dialog 'Oeffnen mit ...' falls keine App registriert.
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex) { Log.Warning("OpenWithDefault fehlgeschlagen: {Error}", ex.Message); }
    }

    private void OnPathRevealInExplorer(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string path || string.IsNullOrEmpty(path)) return;
        if (!System.IO.File.Exists(path) && !System.IO.Directory.Exists(path)) return;
        try { System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\""); }
        catch (Exception ex) { Log.Warning("RevealInExplorer fehlgeschlagen: {Error}", ex.Message); }
    }
}

/// <summary>
/// BPM-104.04: ViewModel fuer Detail-Auswahl im Reset-Tab Inventar-Liste.
/// Zeigt: Display-Name + Scope-Badge + Pfad + Groesse + Datum + Aktions-Buttons.
/// </summary>
public sealed class InventoryItemViewModel : System.ComponentModel.INotifyPropertyChanged
{
    private bool _isSelected;
    public string DisplayName { get; set; } = "";
    public string AbsolutePath { get; set; } = "";
    public string Type { get; set; } = "";
    public string Scope { get; set; } = "";
    public string ScopeLabel { get; set; } = "";
    public string ScopeColor { get; set; } = "#37373D";
    public string SizeText { get; set; } = "";
    public string ModifiedText { get; set; } = "";
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected))); }
    }
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}