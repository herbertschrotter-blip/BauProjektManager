using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;
using Serilog;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// In-App-Explorer (BPM-112.06a, ADR-061 P.6 Modell A): Live-Ansicht des
/// Projektordners — Baum + Dateiliste + Launcher-Aktionen. Code-behind macht
/// nur UI-Orchestrierung (Lazy-Expand, Selektion, Clipboard); Fachlogik im VM.
/// </summary>
public partial class ExplorerView : UserControl
{
    // BPM-112.05 (ADR-060 Slice 5): FS-Port fuer Zugriffe; der FileSystemWatcher
    // selbst bleibt System.IO (UI-Live-Refresh, kein Port-Aequivalent — bewusst,
    // gleiches Muster wie ProjectEditDialog).
    private static readonly Infrastructure.Services.LocalFileSystem _fs = new();

    /// <summary>Sammelfenster fuer FS-Events (Import erzeugt Dutzende, OneDrive rauscht).</summary>
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(750);

    private FileSystemWatcher? _fileWatcher;
    private FileSystemWatcher? _dirWatcher;
    private DispatcherTimer? _debounce;
    private bool _pendingTreeReload;

    public ExplorerView()
    {
        Resources.Add("BoolToVis", new BoolToVisConverter());
        Resources.Add("InverseBoolToVis", new InverseBoolToVisConverter());
        InitializeComponent();
    }

    public ExplorerViewModel? ViewModel => DataContext as ExplorerViewModel;

    /// <summary>"Im PlanManager anzeigen" — der Host wechselt zum ManuellSortieren-Tab (Archiv).</summary>
    public event EventHandler? PlanManagerRequested;

    /// <summary>Vom Host (ProjectDetailView) aufgerufen: VM aufbauen + Root laden.</summary>
    public void Initialize(
        string projectRootPath, IFileLauncher? fileLauncher,
        string inboxRelativePath = "", PlanManagerDatabase? planDb = null)
    {
        var vm = new ExplorerViewModel(fileLauncher, planDb);
        DataContext = vm;
        vm.Initialize(projectRootPath, inboxRelativePath);
        StartWatching(projectRootPath);
    }

    // ── Live-Aktualisierung (BPM-112.06d) ───────────────────────────

    /// <summary>
    /// Ueberwacht den Projektordner und frischt Liste/Badges/Drift automatisch auf.
    /// Zwei Watcher, damit Datei- und Struktur-Aenderungen unterscheidbar bleiben:
    /// Dateien -> Daten-Refresh (Auswahl bleibt), Ordner -> Baum-Neuaufbau.
    /// Alle Events laufen durch ein Debounce-Fenster, weil ein Import Dutzende
    /// Events erzeugt und der Reconcile bei fehlenden Dateien MD5 rechnet.
    /// </summary>
    private void StartWatching(string projectRootPath)
    {
        StopWatching();
        if (string.IsNullOrWhiteSpace(projectRootPath) || !_fs.DirectoryExists(projectRootPath))
            return;

        try
        {
            _debounce = new DispatcherTimer { Interval = DebounceInterval };
            _debounce.Tick += OnDebounceTick;

            _fileWatcher = new FileSystemWatcher(projectRootPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            _fileWatcher.Created += (_, _) => Bump(structureChanged: false);
            _fileWatcher.Deleted += (_, _) => Bump(structureChanged: false);
            _fileWatcher.Changed += (_, _) => Bump(structureChanged: false);
            _fileWatcher.Renamed += (_, _) => Bump(structureChanged: false);
            _fileWatcher.Error += OnWatcherError;

            _dirWatcher = new FileSystemWatcher(projectRootPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.DirectoryName,
                EnableRaisingEvents = true
            };
            _dirWatcher.Created += (_, _) => Bump(structureChanged: true);
            _dirWatcher.Deleted += (_, _) => Bump(structureChanged: true);
            _dirWatcher.Renamed += (_, _) => Bump(structureChanged: true);
            _dirWatcher.Error += OnWatcherError;
        }
        catch (Exception ex)
        {
            // Netzlaufwerke/Cloud koennen Watcher verweigern — dann bleibt der
            // Refresh-Button der Weg (kein Funktionsverlust).
            Log.Warning(ex, "Explorer: Live-Ueberwachung nicht moeglich");
            StopWatching();
        }
    }

    /// <summary>Vom Host beim Verlassen der Projektansicht aufgerufen.</summary>
    public void StopWatching()
    {
        if (_debounce is not null)
        {
            _debounce.Stop();
            _debounce.Tick -= OnDebounceTick;
            _debounce = null;
        }
        foreach (var watcher in new[] { _fileWatcher, _dirWatcher })
        {
            if (watcher is null)
                continue;
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _fileWatcher = null;
        _dirWatcher = null;
        _pendingTreeReload = false;
    }

    /// <summary>Event vom Worker-Thread: Debounce-Fenster auf dem UI-Thread neu starten.</summary>
    private void Bump(bool structureChanged)
        => Dispatcher.InvokeAsync(() =>
        {
            _pendingTreeReload |= structureChanged;
            _debounce?.Stop();
            _debounce?.Start();
        });

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        // Puffer-Ueberlauf: Einzelevents sind verloren -> sicherheitshalber alles neu.
        Log.Warning(e.GetException(), "Explorer: Watcher-Fehler — vollstaendige Aktualisierung");
        Bump(structureChanged: true);
    }

    private void OnDebounceTick(object? sender, EventArgs e)
    {
        _debounce?.Stop();
        if (ViewModel is not { } vm)
            return;
        if (_pendingTreeReload)
        {
            _pendingTreeReload = false;
            vm.ReloadTree();
        }
        else
        {
            vm.RefreshCommand.Execute(null);
        }
    }

    private void OnFolderExpanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem { DataContext: ExplorerFolderNode node }
            && !node.IsPlaceholder)
            ViewModel?.LoadChildren(node);
    }

    private void OnFolderSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (ViewModel is { } vm && e.NewValue is ExplorerFolderNode { IsPlaceholder: false } node)
            vm.SelectedFolder = node;
    }

    private void OnFileDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel is { SelectedFile: not null } vm)
            vm.OpenFileCommand.Execute(vm.SelectedFile);
    }

    // ── Kontextmenü (112.06b) ───────────────────────────────────────

    /// <summary>Rechtsklick selektiert die Zeile unter dem Cursor, bevor das Menü aufgeht.</summary>
    private void OnListContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source)
        {
            var element = source;
            while (element is not null and not DataGridRow)
                element = VisualTreeHelper.GetParent(element);
            if (element is DataGridRow { Item: ExplorerFileRow row })
                FileList.SelectedItem = row;
        }

        var tracked = ViewModel?.SelectedFile?.IsTracked == true;
        CtxShowInPlanManager.IsEnabled = tracked;
        CtxMove.IsEnabled = tracked && ViewModel?.SelectedFile?.Entry is not null;
    }

    private void OnCtxOpenClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { SelectedFile: not null } vm)
            vm.OpenFileCommand.Execute(vm.SelectedFile);
    }

    private void OnCtxRevealClick(object sender, RoutedEventArgs e)
        => ViewModel?.RevealInExplorerCommand.Execute(null);

    private void OnCtxShowInPlanManagerClick(object sender, RoutedEventArgs e)
        => PlanManagerRequested?.Invoke(this, EventArgs.Empty);

    private void OnCtxMoveClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { SelectedFile: { Entry: not null } row } vm)
            return;

        var dialog = new FolderPickerDialog(vm.ProjectRootPath,
            $"{row.Entry.PlanNumber} ({row.Entry.Title})")
        {
            Owner = Window.GetWindow(this)
        };
        if (dialog.ShowDialog() == true && dialog.SelectedRelativeDirectory.Length > 0)
            vm.MoveTracked(row, dialog.SelectedRelativeDirectory);
    }

    private void OnCopyPathClick(object sender, RoutedEventArgs e)
    {
        var path = ViewModel?.PathForClipboard;
        if (path is null)
            return;
        try
        {
            Clipboard.SetText(path);
            if (ViewModel is { } vm)
                vm.StatusText = "Pfad kopiert.";
        }
        catch (Exception ex)
        {
            // Zwischenablage kann durch andere Prozesse gesperrt sein (COM) —
            // kein Crash, nur Status.
            Log.Warning(ex, "Explorer: Zwischenablage nicht verfuegbar");
            if (ViewModel is { } vm)
                vm.StatusText = "Zwischenablage nicht verfügbar.";
        }
    }
}
