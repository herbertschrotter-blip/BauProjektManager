using System.Collections.ObjectModel;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// ViewModel des In-App-Explorers (BPM-112.06a, ADR-061 P.6 Modell A):
/// liest den Projektbaum LIVE vom Dateisystem — die DB bleibt kuratierter
/// Index, kein Vollspiegel. Ordner laden ihre Kinder lazy beim Aufklappen.
/// Getrackt-/Drift-Kennzeichnung folgt mit 112.06b/c.
/// </summary>
public partial class ExplorerViewModel : ObservableObject
{
    // BPM-112.05 (ADR-060 Slice 5): FS-Ports statt direktem System.IO in der VM.
    private static readonly Infrastructure.Services.LocalFileSystem _fs = new();

    private readonly IFileLauncher? _launcher;
    private readonly PlanManagerDatabase? _db;
    private readonly ArchiveMoveService? _move;
    private readonly PlanReconcileService? _reconcile;
    private string _rootPath = "";
    private string _inboxFullPath = "";
    private string _driftSummary = "";

    // Getrackt-Lookup (112.06b): normalisierter relativer Pfad -> Archiv-Eintrag.
    // Die DB bleibt kuratierter Index — der Explorer fragt nur nach, spiegelt nicht.
    private readonly Dictionary<string, PlanArchiveEntry> _tracked = new(StringComparer.OrdinalIgnoreCase);

    // Drift-Lookup (112.06c): normalisierter relativer Pfad -> Reconcile-Befund.
    private readonly Dictionary<string, DriftEntry> _drift = new(StringComparer.OrdinalIgnoreCase);

    public ExplorerViewModel(IFileLauncher? launcher = null, PlanManagerDatabase? db = null)
    {
        _launcher = launcher;
        _db = db;
        _move = db is null ? null : new ArchiveMoveService(db);
        _reconcile = db is null ? null : new PlanReconcileService(db, _fs);
    }

    public ObservableCollection<ExplorerFolderNode> RootNodes { get; } = [];

    public ObservableCollection<ExplorerFileRow> Files { get; } = [];

    [ObservableProperty]
    private ExplorerFolderNode? _selectedFolder;

    [ObservableProperty]
    private ExplorerFileRow? _selectedFile;

    [ObservableProperty]
    private string _breadcrumb = "";

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private bool _hasRoot;

    /// <summary>Projektwurzel setzen und obersten Baum laden. Leerer Root = Empty-State.</summary>
    public void Initialize(string rootPath, string inboxRelativePath = "")
    {
        _rootPath = rootPath;
        _inboxFullPath = string.IsNullOrWhiteSpace(inboxRelativePath)
            ? ""
            : _fs.Combine(rootPath, inboxRelativePath.Replace('/', '\\'));
        HasRoot = !string.IsNullOrWhiteSpace(rootPath) && _fs.DirectoryExists(rootPath);
        RootNodes.Clear();
        Files.Clear();
        if (!HasRoot)
        {
            StatusText = "Projektpfad nicht gesetzt oder nicht erreichbar.";
            return;
        }

        BuildTrackedIndex();
        RunReconcile();
        foreach (var node in BuildChildNodes(rootPath))
            RootNodes.Add(node);
        StatusText = _driftSummary.Length > 0
            ? $"{RootNodes.Count} Ordner im Projektroot · ⚠ {_driftSummary}"
            : $"{RootNodes.Count} Ordner im Projektroot";
    }

    /// <summary>
    /// Startup-Reconcile (112.06c): Drift-Befunde der getrackten Teilmenge einsammeln.
    /// Läuft bei Initialize und jedem Refresh — nie automatisch reparierend.
    /// </summary>
    private void RunReconcile()
    {
        _drift.Clear();
        _driftSummary = "";
        if (_reconcile is null)
            return;
        var result = _reconcile.Reconcile(_rootPath);
        foreach (var entry in result.Drift)
            _drift[Normalize(entry.RelativePath)] = entry;

        var missing = result.Drift.Count(d => d.Kind is DriftKind.MissingOnDisk or DriftKind.RelinkCandidate);
        var changed = result.Drift.Count(d => d.Kind == DriftKind.ChangedOnDisk);
        List<string> parts = [];
        if (missing > 0)
            parts.Add($"{missing}× fehlt auf Disk");
        if (changed > 0)
            parts.Add($"{changed}× geändert");
        _driftSummary = string.Join(" · ", parts);
    }

    /// <summary>
    /// Getrackt-Index neu aufbauen: alle Dateien der current-Revisionen
    /// (plan_documents/plan_revisions) nach relativem Pfad aufloesbar machen.
    /// </summary>
    private void BuildTrackedIndex()
    {
        _tracked.Clear();
        if (_db is null)
            return;
        try
        {
            foreach (var entry in _db.GetArchiveEntries())
                foreach (var file in _db.GetFilesForRevision(entry.RevisionId))
                    _tracked[Normalize(file.RelativePath)] = entry;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Explorer: Getrackt-Index nicht ladbar");
        }
    }

    private static string Normalize(string relativePath) => relativePath.Replace('/', '\\');

    private static string GetDirectory(string relativePath)
    {
        var normalized = Normalize(relativePath);
        var idx = normalized.LastIndexOf('\\');
        return idx < 0 ? "" : normalized[..idx];
    }

    /// <summary>Kinder eines Knotens nachladen (lazy beim Aufklappen).</summary>
    public void LoadChildren(ExplorerFolderNode node)
    {
        if (node.ChildrenLoaded)
            return;
        node.ChildrenLoaded = true;
        node.Children.Clear();
        foreach (var child in BuildChildNodes(node.FullPath))
            node.Children.Add(child);
    }

    partial void OnSelectedFolderChanged(ExplorerFolderNode? value)
    {
        Files.Clear();
        SelectedFile = null;
        if (value is null)
        {
            Breadcrumb = "";
            return;
        }

        Breadcrumb = ToRelative(value.FullPath).Replace('\\', '/').Replace("/", " › ");
        try
        {
            foreach (var path in _fs.EnumerateFiles(value.FullPath)
                         .Where(p => !_fs.GetFileName(p).StartsWith('.'))
                         .OrderBy(p => _fs.GetFileName(p), StringComparer.OrdinalIgnoreCase))
            {
                var info = _fs.GetFileInfo(path);
                var rel = Normalize(ToRelative(path));
                var entry = _tracked.GetValueOrDefault(rel);
                var drift = _drift.GetValueOrDefault(rel);
                var (statusText, statusKind, tooltip) = drift?.Kind == DriftKind.ChangedOnDisk
                    ? ("Geändert auf Disk", "changed",
                       "Dateigröße weicht vom erfassten Stand ab — Inhalt wurde außerhalb der App verändert.")
                    : entry is null
                        ? ("", "", "")
                        : ($"Getrackt · {entry.PlanIndex ?? "Erstausg."}", "tracked", "");
                Files.Add(new ExplorerFileRow(
                    _fs.GetFileName(path), path, FormatSize(info.Length),
                    info.Exists ? info.LastWriteTimeUtc.ToLocalTime().ToString("dd.MM.yy") : "",
                    statusText, entry, statusKind, tooltip));
            }

            // Geisterzeilen (112.06c): getrackte Dateien dieses Ordners, die auf
            // Disk fehlen — Relink ist nur ein Hinweis, nie eine Aktion.
            var folderRel = Normalize(ToRelative(value.FullPath));
            foreach (var drift in _drift.Values.Where(d =>
                         d.Kind is DriftKind.MissingOnDisk or DriftKind.RelinkCandidate
                         && string.Equals(GetDirectory(d.RelativePath), folderRel,
                             StringComparison.OrdinalIgnoreCase)))
            {
                Files.Add(new ExplorerFileRow(
                    drift.FileName, _fs.Combine(_rootPath, drift.RelativePath), "—", "—",
                    drift.Kind == DriftKind.RelinkCandidate ? "Fehlt · Relink?" : "Fehlt auf Disk",
                    null, "missing",
                    drift.Kind == DriftKind.RelinkCandidate
                        ? $"Gleicher Inhalt gefunden unter: {drift.RelinkPath} — Relink erfolgt nicht automatisch (ADR-061)."
                        : $"Getrackter Plan {drift.PlanNumber} ({drift.PlanIndex ?? "Erstausg."}) liegt nicht mehr am erfassten Ort."));
            }

            var trackedCount = Files.Count(f => f.IsTracked);
            var driftCount = Files.Count(f => f.StatusKind is "missing" or "changed");
            StatusText = (trackedCount, driftCount) switch
            {
                (_, > 0) => $"{Files.Count} Datei(en) · {trackedCount} getrackt · ⚠ {driftCount} Drift-Hinweis(e)",
                ( > 0, _) => $"{Files.Count} Datei(en) · {trackedCount} getrackt",
                _ => $"{Files.Count} Datei(en)"
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Explorer: Ordner nicht lesbar");
            StatusText = "Ordner nicht lesbar.";
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        var selectedPath = SelectedFolder?.FullPath;
        Initialize(_rootPath);
        if (selectedPath is not null)
            SelectedFolder = RootNodes.FirstOrDefault(n =>
                string.Equals(n.FullPath, selectedPath, StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand]
    private void OpenFile(ExplorerFileRow? row)
    {
        row ??= SelectedFile;
        if (row is null || _launcher is null)
            return;
        if (!_launcher.OpenFile(row.FullPath))
            StatusText = "Datei konnte nicht geöffnet werden.";
    }

    [RelayCommand]
    private void RevealInExplorer()
    {
        if (_launcher is null)
            return;
        var ok = SelectedFile is not null
            ? _launcher.RevealInExplorer(SelectedFile.FullPath)
            : SelectedFolder is not null && _launcher.OpenFolder(SelectedFolder.FullPath);
        if (!ok)
            StatusText = "Windows-Explorer konnte nicht geöffnet werden.";
    }

    /// <summary>Pfad, den "Pfad kopieren" in die Zwischenablage legt (Datei vor Ordner).</summary>
    public string? PathForClipboard => SelectedFile?.FullPath ?? SelectedFolder?.FullPath;

    /// <summary>Absoluter Projektroot (fuer den Zielordner-Dialog des Hosts).</summary>
    public string ProjectRootPath => _rootPath;

    /// <summary>
    /// Getrackten Plan journalisiert verschieben (112.06b, ADR-061 P.6):
    /// laeuft ueber den ArchiveMoveService — alle Dateien der current-Revision
    /// ziehen gemeinsam um, Journal-Action VOR jedem Move, nicht undo-bar.
    /// </summary>
    public void MoveTracked(ExplorerFileRow row, string targetRelativeDirectory)
    {
        if (_move is null || row.Entry is null)
            return;
        var result = _move.MoveDocument(row.Entry, targetRelativeDirectory, _rootPath);
        StatusText = result.Success
            ? $"✓ {row.Entry.PlanNumber} verschoben nach {targetRelativeDirectory} ({result.MovedFiles} Datei(en))"
            : $"⚠ Verschieben fehlgeschlagen: {result.Error}";
        if (result.Success)
        {
            var folder = SelectedFolder;
            BuildTrackedIndex();
            RunReconcile();
            OnSelectedFolderChanged(folder);
        }
    }

    private IEnumerable<ExplorerFolderNode> BuildChildNodes(string parentPath)
    {
        List<ExplorerFolderNode> nodes = [];
        try
        {
            foreach (var dir in _fs.EnumerateDirectories(parentPath)
                         .Where(d => !_fs.GetFileName(d).StartsWith('.'))
                         .OrderBy(d => _fs.GetFileName(d), StringComparer.OrdinalIgnoreCase))
            {
                var node = new ExplorerFolderNode(_fs.GetFileName(dir), dir);
                if (HasVisibleSubdirectories(dir))
                    node.Children.Add(ExplorerFolderNode.Placeholder());
                if (_inboxFullPath.Length > 0
                    && string.Equals(dir, _inboxFullPath, StringComparison.OrdinalIgnoreCase))
                    node.BadgeText = $"({CountFiles(dir)})";
                nodes.Add(node);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Explorer: Unterordner nicht lesbar");
        }
        return nodes;
    }

    private int CountFiles(string path)
    {
        try
        {
            return _fs.EnumerateFiles(path).Count(f => !_fs.GetFileName(f).StartsWith('.'));
        }
        catch
        {
            return 0;
        }
    }

    private bool HasVisibleSubdirectories(string path)
    {
        try
        {
            return _fs.EnumerateDirectories(path).Any(d => !_fs.GetFileName(d).StartsWith('.'));
        }
        catch
        {
            return false;
        }
    }

    private string ToRelative(string fullPath)
        => fullPath.Length > _rootPath.Length && fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase)
            ? fullPath[_rootPath.Length..].TrimStart('\\', '/')
            : fullPath;

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024} KB",
        _ => $"{bytes / 1024.0 / 1024.0:0.#} MB"
    };
}

/// <summary>Ordner-Knoten des Explorer-Baums; Kinder werden lazy geladen.</summary>
public partial class ExplorerFolderNode : ObservableObject
{
    /// <summary>Marker-Kind, damit der Aufklapp-Pfeil vor dem Lazy-Load erscheint.</summary>
    public static ExplorerFolderNode Placeholder() => new("…", "") { IsPlaceholder = true };

    public ExplorerFolderNode(string name, string fullPath)
    {
        Name = name;
        FullPath = fullPath;
    }

    public string Name { get; }
    public string FullPath { get; }
    public bool IsPlaceholder { get; private init; }
    public bool ChildrenLoaded { get; set; }
    public ObservableCollection<ExplorerFolderNode> Children { get; } = [];

    /// <summary>Zusatz-Label hinter dem Namen (z. B. Dateizahl am Eingang), leer = kein Badge.</summary>
    [ObservableProperty]
    private string _badgeText = "";
}

/// <summary>
/// Zeile der Dateiliste (rechte Seite); Entry gesetzt = getrackter Plan (112.06b).
/// StatusKind steuert die Badge-Farbe: "tracked" (Info) / "changed" (Warnung) /
/// "missing" (Fehler, Geisterzeile ohne Disk-Datei) — 112.06c.
/// </summary>
public sealed record ExplorerFileRow(
    string Name, string FullPath, string SizeText, string ChangedText,
    string StatusText = "", PlanArchiveEntry? Entry = null,
    string StatusKind = "", string StatusTooltip = "")
{
    public bool IsTracked => Entry is not null;
}
