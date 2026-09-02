using System.Collections.ObjectModel;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.PlanManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// Plandaten-Tab (BPM-126): tabellarische Sicht auf den kuratierten Planindex —
/// alle Eigenschaften je Plan mit Suche und Filtern. Read-only; Struktur-Änderungen
/// laufen weiter über Import-/Radial-/Archiv-Workflows. Bauteil-/Geschoss-Namen
/// kommen aus den bpm.db-Stammdaten (Cross-DB Soft Reference, ADR-058-Addendum).
/// </summary>
public partial class PlanDataViewModel : ObservableObject
{
    /// <summary>Filter-Eintrag „alle" (kein Filter aktiv).</summary>
    public const string AllFilter = "(alle)";

    private readonly PlanManagerDatabase _planDb;
    private readonly ProjectDatabase? _bpmDb;
    private readonly string _projectId;

    private List<PlanDataRow> _allRows = [];
    private readonly Dictionary<string, string> _partNames = new();
    private readonly Dictionary<string, string> _levelNames = new();

    public PlanDataViewModel(PlanManagerDatabase planDb, string projectId, ProjectDatabase? bpmDb = null)
    {
        _planDb = planDb;
        _projectId = projectId;
        _bpmDb = bpmDb;
        TypeFilters.Add(AllFilter);
        PartFilters.Add(AllFilter);
    }

    public ObservableCollection<PlanDataRowViewModel> Rows { get; } = [];

    public ObservableCollection<string> TypeFilters { get; } = [];

    public ObservableCollection<string> PartFilters { get; } = [];

    [ObservableProperty]
    private PlanDataRowViewModel? _selectedRow;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _selectedTypeFilter = AllFilter;

    [ObservableProperty]
    private string _selectedPartFilter = AllFilter;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private bool _hasRows;

    // ── Detail-Panel (BPM-126b) ─────────────────────────────────────

    /// <summary>Dateien der current-Revision mit Groesse/MD5.</summary>
    public ObservableCollection<PlanFileDisplay> DetailFiles { get; } = [];

    /// <summary>Revisions-Historie des gewaehlten Dokuments (neueste zuerst).</summary>
    public ObservableCollection<PlanRevisionDisplay> DetailRevisions { get; } = [];

    [ObservableProperty]
    private bool _hasSelection;

    partial void OnSelectedRowChanged(PlanDataRowViewModel? value) => LoadDetails(value);

    private void LoadDetails(PlanDataRowViewModel? row)
    {
        DetailFiles.Clear();
        DetailRevisions.Clear();
        HasSelection = row is not null;
        if (row is null)
            return;
        try
        {
            foreach (var file in _planDb.GetFileDetailsForRevision(row.Row.RevisionId))
                DetailFiles.Add(new PlanFileDisplay(file));
            foreach (var rev in _planDb.GetRevisionsForDocument(row.Row.DocumentId)
                         .OrderByDescending(r => r.CurrentFrom))
                DetailRevisions.Add(new PlanRevisionDisplay(rev));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Plandaten: Detaildaten nicht ladbar");
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedTypeFilterChanged(string value) => ApplyFilter();

    partial void OnSelectedPartFilterChanged(string value) => ApplyFilter();

    /// <summary>Stammdaten + Plandaten laden (bei Tab-Öffnen und nach Import/Undo).</summary>
    [RelayCommand]
    public void Load()
    {
        LoadMasterDataNames();
        try
        {
            _allRows = _planDb.GetPlanDataRows();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Plandaten nicht ladbar");
            _allRows = [];
            StatusText = "Plandaten konnten nicht geladen werden.";
            return;
        }

        RebuildFilterLists();
        // Auswahl ueber die Dokument-Id halten — sonst klappt das Detail-Panel
        // nach jeder Aktion (Segment speichern, Import, Undo) zu.
        LastSelectedDocumentId = SelectedRow?.Row.DocumentId;
        ApplyFilter();
        RestoreSelection();
    }

    /// <summary>Dokument-Id der letzten Auswahl — Anker fuer RestoreSelection.</summary>
    public string? LastSelectedDocumentId { get; private set; }

    /// <summary>
    /// Auswahl nach einem Neuladen wiederherstellen. Das DataGrid setzt seine
    /// Selektion beim Leeren der Liste asynchron zurueck — der Host ruft das
    /// deshalb zusaetzlich verzoegert auf (sonst klappt das Detail-Panel zu).
    /// </summary>
    public void RestoreSelection()
    {
        if (LastSelectedDocumentId is null || SelectedRow is not null)
            return;
        var match = Rows.FirstOrDefault(r => r.Row.DocumentId == LastSelectedDocumentId);
        if (match is not null)
            SelectedRow = match;
    }

    /// <summary>Bauteil-/Geschoss-Namen aus der bpm.db für die Anzeige auflösen.</summary>
    private void LoadMasterDataNames()
    {
        _partNames.Clear();
        _levelNames.Clear();
        if (_bpmDb is null)
            return;
        try
        {
            foreach (var part in _bpmDb.GetBuildingParts(_projectId))
            {
                _partNames[part.Id] = part.ShortName;
                foreach (var level in part.Levels)
                    _levelNames[level.Id] = level.Name;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Plandaten: Stammdaten-Namen nicht ladbar");
        }
    }

    private void RebuildFilterLists()
    {
        var type = SelectedTypeFilter;
        var part = SelectedPartFilter;

        TypeFilters.Clear();
        TypeFilters.Add(AllFilter);
        foreach (var t in _allRows.Select(r => r.DocumentType).Distinct().OrderBy(t => t))
            TypeFilters.Add(t);

        PartFilters.Clear();
        PartFilters.Add(AllFilter);
        foreach (var p in _allRows
                     .Select(r => ResolvePart(r.BuildingPartId))
                     .Where(p => p.Length > 0).Distinct().OrderBy(p => p))
            PartFilters.Add(p);

        SelectedTypeFilter = TypeFilters.Contains(type) ? type : AllFilter;
        SelectedPartFilter = PartFilters.Contains(part) ? part : AllFilter;
    }

    private void ApplyFilter()
    {
        var search = SearchText.Trim();
        Rows.Clear();
        foreach (var row in _allRows)
        {
            var partName = ResolvePart(row.BuildingPartId);
            if (SelectedTypeFilter != AllFilter && row.DocumentType != SelectedTypeFilter)
                continue;
            if (SelectedPartFilter != AllFilter && partName != SelectedPartFilter)
                continue;
            if (search.Length > 0 && !MatchesSearch(row, search))
                continue;

            Rows.Add(new PlanDataRowViewModel(row, partName, ResolveLevel(row.BuildingLevelId)));
        }

        HasRows = Rows.Count > 0;
        var filtered = Rows.Count != _allRows.Count;
        StatusText = _allRows.Count == 0
            ? "Noch keine Pläne erfasst — Dokumente erscheinen hier nach dem Import."
            : filtered
                ? $"{Rows.Count} von {_allRows.Count} Dokument(en) · Filter aktiv"
                : $"{_allRows.Count} Dokument(e)";
    }

    private static bool MatchesSearch(PlanDataRow row, string search)
        => row.PlanNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
           || row.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
           || row.ChangeNote.Contains(search, StringComparison.OrdinalIgnoreCase)
           || row.DocumentType.Contains(search, StringComparison.OrdinalIgnoreCase);

    private string ResolvePart(string? id)
        => id is not null && _partNames.TryGetValue(id, out var name) ? name : "";

    private string ResolveLevel(string? id)
        => id is not null && _levelNames.TryGetValue(id, out var name) ? name : "";
}

/// <summary>Anzeige-Zeile der Plandaten-Tabelle (BPM-126).</summary>
public sealed partial class PlanDataRowViewModel : ObservableObject
{
    public PlanDataRowViewModel(PlanDataRow row, string partName, string levelName)
    {
        Row = row;
        BuildingPart = partName;
        BuildingLevel = levelName;
        _segmentCount = row.SegmentCount;
    }

    public PlanDataRow Row { get; }

    public string PlanNumber => Row.PlanNumber;
    public string PlanIndex => Row.PlanIndex ?? "—";
    public string Title => Row.Title;
    public string DocumentType => Row.DocumentType;
    public string BuildingPart { get; }
    public string BuildingLevel { get; }
    public string ChangeNote => Row.ChangeNote.Length > 0 ? Row.ChangeNote : "Erstausgabe";

    /// <summary>Index-Datum lokal formatiert; leer wenn (noch) nicht gesetzt.</summary>
    public string ReleasedAt => DateTime.TryParse(Row.ReleasedAt, out var d)
        ? d.ToLocalTime().ToString("dd.MM.yyyy")
        : "—";

    /// <summary>Dateitypen der Revision, z. B. „PDF · DWG".</summary>
    public string FileTypes => Row.FileTypes is null or ""
        ? "—"
        : string.Join(" · ", Row.FileTypes.Split(',', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Segment-Anzahl — nach einer Zuweisung im Editor wird NUR dieser Wert
    /// aktualisiert (kein Neuladen der Liste, sonst verliert das DataGrid
    /// seine Auswahl und das Detail-Panel klappt zu).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SegmentText))]
    private int _segmentCount;

    public string SegmentText => SegmentCount == 0 ? "—" : $"{SegmentCount} Segmente";
}

/// <summary>Anzeige-Wrapper einer Revision für die Historie im Detail-Panel (BPM-126b).</summary>
public sealed class PlanRevisionDisplay(PlanRevision revision)
{
    public PlanRevision Revision { get; } = revision;

    public string IndexText => Revision.PlanIndex ?? "—";

    /// <summary>Freigabedatum, sonst Hinzufügedatum (BPM-109.04b-Reihenfolge).</summary>
    public string DateText => DateTime.TryParse(Revision.ReleasedAt ?? Revision.ReceivedAt, out var d)
        ? d.ToLocalTime().ToString("dd.MM.yyyy")
        : "—";

    public string ChangeNote => Revision.ChangeNote.Length > 0 ? Revision.ChangeNote : "Erstausgabe";

    /// <summary>true = aktuelle Revision (in der Historie hervorgehoben).</summary>
    public bool IsCurrent => Revision.RevisionStatus == "current";
}

/// <summary>Anzeige-Wrapper einer Revisionsdatei im Detail-Panel (BPM-126b).</summary>
public sealed class PlanFileDisplay(PlanFileDetail detail)
{
    public PlanFileDetail Detail { get; } = detail;

    public string FileName => Detail.FileName;
    public string RelativePath => Detail.RelativePath;
    public bool IsPrimary => Detail.IsPrimary;

    public string SizeText => Detail.FileSize switch
    {
        < 1024 => $"{Detail.FileSize} B",
        < 1024 * 1024 => $"{Detail.FileSize / 1024} KB",
        _ => $"{Detail.FileSize / 1024.0 / 1024.0:0.#} MB"
    };

    /// <summary>Gekürzter Fingerprint für die Anzeige (voller Wert im Tooltip).</summary>
    public string Md5Short => Detail.Md5.Length >= 8 ? "md5 " + Detail.Md5[..8].ToLowerInvariant() + "…" : "";
}
