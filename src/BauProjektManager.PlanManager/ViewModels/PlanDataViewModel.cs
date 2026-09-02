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
        ApplyFilter();
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
public sealed class PlanDataRowViewModel(PlanDataRow row, string partName, string levelName)
{
    public PlanDataRow Row { get; } = row;

    public string PlanNumber => Row.PlanNumber;
    public string PlanIndex => Row.PlanIndex ?? "—";
    public string Title => Row.Title;
    public string DocumentType => Row.DocumentType;
    public string BuildingPart { get; } = partName;
    public string BuildingLevel { get; } = levelName;
    public string ChangeNote => Row.ChangeNote.Length > 0 ? Row.ChangeNote : "Erstausgabe";

    /// <summary>Index-Datum lokal formatiert; leer wenn (noch) nicht gesetzt.</summary>
    public string ReleasedAt => DateTime.TryParse(Row.ReleasedAt, out var d)
        ? d.ToLocalTime().ToString("dd.MM.yyyy")
        : "—";

    /// <summary>Dateitypen der Revision, z. B. „PDF · DWG".</summary>
    public string FileTypes => Row.FileTypes is null or ""
        ? "—"
        : string.Join(" · ", Row.FileTypes.Split(',', StringSplitOptions.RemoveEmptyEntries));

    public string SegmentText => Row.SegmentCount == 0 ? "—" : $"{Row.SegmentCount} Segmente";
}
