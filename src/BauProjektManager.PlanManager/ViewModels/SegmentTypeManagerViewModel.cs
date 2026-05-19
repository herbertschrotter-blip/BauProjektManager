using System.Collections.ObjectModel;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// ViewModel fuer den Segmenttyp-Manager-Dialog (BPM-108 Phase C Teil 3).
/// </summary>
/// <remarks>
/// Erlaubt das Editieren aller Segmenttypen (Name/Farbe/Gruppe/Aktiv-Status),
/// das Anlegen neuer Custom-Typen und das Soft-Loeschen von Custom-Typen.
/// Built-ins sind voll editierbar (Name/Farbe/Gruppe/Active) — Aenderungen setzen die
/// <c>user_modified_*</c>-Flags damit App-Updates die User-Aenderung nicht ueberschreiben.
/// <see cref="SegmentTypeDefinition.SemanticRole"/> und <c>token_key</c> sind read-only.
/// </remarks>
public partial class SegmentTypeManagerViewModel : ObservableObject
{
    private readonly ISegmentTypeRepository _repository;
    private readonly ISegmentTypeCatalog _catalog;
    private readonly IIdGenerator _idGenerator;

    public SegmentTypeManagerViewModel(
        ISegmentTypeRepository repository,
        ISegmentTypeCatalog catalog,
        IIdGenerator idGenerator)
    {
        _repository = repository;
        _catalog = catalog;
        _idGenerator = idGenerator;

        Refresh();
    }

    // === Listendaten ===

    /// <summary>Aktive Gruppen + ihre Typen (inkl. inaktive, exkl. geloeschte) in Sortierreihenfolge.</summary>
    [ObservableProperty]
    private ObservableCollection<GroupBucket> _groups = [];

    /// <summary>Verfuegbare Gruppen fuer das Gruppen-Dropdown im Edit-Panel.</summary>
    [ObservableProperty]
    private ObservableCollection<SegmentTypeGroupDefinition> _availableGroups = [];

    // === Auswahl + Drafts ===

    [ObservableProperty]
    private SegmentTypeDefinition? _selectedType;

    [ObservableProperty]
    private string _nameDraft = "";

    [ObservableProperty]
    private string _colorDraft = "#A87142";

    [ObservableProperty]
    private string _groupIdDraft = "";

    [ObservableProperty]
    private bool _isDirty;

    // === Read-only Anzeigen fuer Built-ins ===

    public bool IsSelectionBuiltin => SelectedType?.IsBuiltin ?? false;
    public bool IsSelectionCustom => SelectedType is not null && !SelectedType.IsBuiltin;
    public bool HasSelection => SelectedType is not null;

    public string SemanticRoleDisplay => SelectedType?.SemanticRole switch
    {
        SegmentSemanticRole.PlanNumber => "Plannummer",
        SegmentSemanticRole.PlanIndex => "Index",
        SegmentSemanticRole.ProjectNumber => "Projektnummer",
        SegmentSemanticRole.Date => "Datum",
        SegmentSemanticRole.Description => "Beschreibung",
        SegmentSemanticRole.Spatial => "Raeumlich",
        SegmentSemanticRole.Ignore => "Ignorieren",
        SegmentSemanticRole.None => "Ohne Sonderrolle",
        null => "—",
        _ => SelectedType.SemanticRole.ToString() ?? "—"
    };

    public string SemanticRoleInfo => SelectedType?.SemanticRole switch
    {
        SegmentSemanticRole.PlanNumber => "Genau ein Segment mit dieser Rolle ist pro Profil erforderlich.",
        SegmentSemanticRole.PlanIndex => "Erforderlich wenn IndexSource = Aus Dateiname gewaehlt ist.",
        SegmentSemanticRole.Spatial => "Wird automatisch Teil der Dokument-Identitaet, wenn dieser Segmenttyp einem Profilsegment zugewiesen ist.",
        SegmentSemanticRole.Date => "Variabel — wird im Wizard bei Recognition-Auswahl als Warnung markiert.",
        _ => ""
    };

    public string TokenKeyDisplay => SelectedType?.TokenKey ?? "";

    // === Palette ===

    public IReadOnlyList<string> Palette { get; } =
    [
        "#0F6E56", "#993C1D", "#534AB7", "#185FA5",
        "#1F7280", "#555555", "#7A1F5C", "#A87142",
        "#3D7B47", "#8B6914", "#5C3D8E", "#2E7D8A"
    ];

    // === Auswahl-Handling ===

    public void SelectType(SegmentTypeDefinition? type)
    {
        SelectedType = type;
        if (type is null)
        {
            NameDraft = "";
            ColorDraft = "#A87142";
            GroupIdDraft = "";
        }
        else
        {
            NameDraft = type.Name;
            ColorDraft = type.Color;
            GroupIdDraft = type.GroupId;
        }
        IsDirty = false;
        NotifySelectionChanged();
    }

    partial void OnSelectedTypeChanged(SegmentTypeDefinition? value)
    {
        NotifySelectionChanged();
    }

    partial void OnNameDraftChanged(string value) => UpdateDirtyState();
    partial void OnColorDraftChanged(string value) => UpdateDirtyState();
    partial void OnGroupIdDraftChanged(string value) => UpdateDirtyState();

    private void UpdateDirtyState()
    {
        if (SelectedType is null) { IsDirty = false; return; }
        IsDirty = !string.Equals(NameDraft, SelectedType.Name)
            || !string.Equals(ColorDraft, SelectedType.Color)
            || !string.Equals(GroupIdDraft, SelectedType.GroupId);
    }

    private void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(IsSelectionBuiltin));
        OnPropertyChanged(nameof(IsSelectionCustom));
        OnPropertyChanged(nameof(SemanticRoleDisplay));
        OnPropertyChanged(nameof(SemanticRoleInfo));
        OnPropertyChanged(nameof(TokenKeyDisplay));
    }

    // === Save / Cancel Draft ===

    [RelayCommand]
    private void SaveDraft()
    {
        if (SelectedType is null) return;
        if (!IsDirty) return;

        var t = SelectedType;

        if (!string.Equals(NameDraft, t.Name))
        {
            t.Name = NameDraft.Trim();
            if (t.IsBuiltin) t.UserModifiedName = true;
        }
        if (!string.Equals(ColorDraft, t.Color))
        {
            t.Color = ColorDraft;
            if (t.IsBuiltin) t.UserModifiedColor = true;
        }
        if (!string.Equals(GroupIdDraft, t.GroupId))
        {
            t.GroupId = GroupIdDraft;
            if (t.IsBuiltin) t.UserModifiedGroup = true;
        }

        try
        {
            _repository.SaveType(t);
            _catalog.Invalidate();
            Log.Information("BPM-108: Segmenttyp aktualisiert: {Name} ({Id})", t.Name, t.Id);
            Refresh();
            IsDirty = false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BPM-108: Save Segmenttyp fehlgeschlagen ({Id})", t.Id);
        }
    }

    [RelayCommand]
    private void CancelDraft()
    {
        if (SelectedType is null) return;
        NameDraft = SelectedType.Name;
        ColorDraft = SelectedType.Color;
        GroupIdDraft = SelectedType.GroupId;
        IsDirty = false;
    }

    // === Toggle Active (Item) ===

    public void ToggleTypeActive(SegmentTypeDefinition type)
    {
        type.IsActive = !type.IsActive;
        if (type.IsBuiltin) type.UserModifiedActive = true;
        _repository.SaveType(type);
        _catalog.Invalidate();
        Log.Information("BPM-108: Segmenttyp {Op}: {Name} ({Id})",
            type.IsActive ? "aktiviert" : "deaktiviert", type.Name, type.Id);
        Refresh();
    }

    // === Toggle Active (Gruppe) ===

    public void ToggleGroupActive(SegmentTypeGroupDefinition group)
    {
        group.IsActive = !group.IsActive;
        if (group.IsBuiltin) group.UserModifiedActive = true;
        _repository.SaveGroup(group);
        _catalog.Invalidate();
        Log.Information("BPM-108: Gruppe {Op}: {Name} ({Id})",
            group.IsActive ? "aktiviert" : "deaktiviert", group.Name, group.Id);
        Refresh();
    }

    // === Soft-Delete (nur Custom) ===

    [RelayCommand]
    private void DeleteSelectedCustom()
    {
        if (SelectedType is null || SelectedType.IsBuiltin) return;

        var id = SelectedType.Id;
        try
        {
            _repository.SoftDeleteType(id);
            _catalog.Invalidate();
            Log.Information("BPM-108: Custom-Segmenttyp soft-geloescht: {Name} ({Id})",
                SelectedType.Name, id);
            SelectType(null);
            Refresh();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BPM-108: Soft-Delete fehlgeschlagen ({Id})", id);
        }
    }

    // === Neue Custom-Anlage ===

    [RelayCommand]
    private void CreateNewCustom()
    {
        var baseKey = "neuer_segmenttyp";
        var token = TokenKeyGenerator.EnsureUnique(baseKey, k => _repository.TokenKeyExists(k));
        var newType = new SegmentTypeDefinition
        {
            Id = _idGenerator.NewId(),
            Name = "Neuer Segmenttyp",
            Color = "#A87142",
            TokenKey = token,
            SemanticRole = null,
            GroupId = "grp_eigene",
            SortOrder = NextCustomSortOrder(),
            IsActive = true,
            IsBuiltin = false
        };

        try
        {
            _repository.SaveType(newType);
            _catalog.Invalidate();
            Log.Information("BPM-108: Neuer Custom-Segmenttyp angelegt: {Token} ({Id})",
                newType.TokenKey, newType.Id);
            Refresh();

            // Auto-Select fuer sofortiges Editieren
            var added = Groups
                .SelectMany(g => g.Items)
                .FirstOrDefault(t => t.Id == newType.Id);
            if (added is not null) SelectType(added);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BPM-108: CreateNewCustom fehlgeschlagen");
        }
    }

    private int NextCustomSortOrder()
    {
        var customs = _catalog.GetEffectiveActive()
            .Where(t => t.GroupId == "grp_eigene")
            .ToList();
        return customs.Count == 0 ? 10 : customs.Max(t => t.SortOrder) + 10;
    }

    // === Neue Custom-Gruppe ===

    [RelayCommand]
    private void CreateNewGroup()
    {
        var newGroup = new SegmentTypeGroupDefinition
        {
            Id = _idGenerator.NewId(),
            Name = "Neue Gruppe",
            SortOrder = NextCustomGroupSortOrder(),
            IsActive = true,
            IsBuiltin = false
        };

        try
        {
            _repository.SaveGroup(newGroup);
            _catalog.Invalidate();
            Log.Information("BPM-108: Neue Custom-Gruppe angelegt: {Id}", newGroup.Id);
            Refresh();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BPM-108: CreateNewGroup fehlgeschlagen");
        }
    }

    private int NextCustomGroupSortOrder()
    {
        // Custom-Gruppen unterhalb von grp_eigene (SortOrder 50 + n*10)
        var allGroups = _repository.LoadAllGroups();
        if (allGroups.Count == 0) return 50;
        return allGroups.Max(g => g.SortOrder) + 10;
    }

    // === Refresh / Rebuild ===

    private void Refresh()
    {
        var allGroups = _repository.LoadAllGroups()
            .OrderBy(g => g.SortOrder)
            .ToList();
        var allTypes = _repository.LoadAllTypes()
            .OrderBy(t => t.SortOrder)
            .ToList();

        var buckets = new ObservableCollection<GroupBucket>();
        foreach (var g in allGroups)
        {
            var items = new ObservableCollection<SegmentTypeDefinition>(
                allTypes.Where(t => t.GroupId == g.Id));
            buckets.Add(new GroupBucket(g, items));
        }
        Groups = buckets;
        AvailableGroups = new ObservableCollection<SegmentTypeGroupDefinition>(allGroups);
    }
}

/// <summary>Gruppe + ihre Typen fuer den Manager-Dialog. Bucket statt Tree-Node wegen WPF-DataTemplate-Bindings.</summary>
public class GroupBucket
{
    public SegmentTypeGroupDefinition Group { get; }
    public ObservableCollection<SegmentTypeDefinition> Items { get; }
    public string Header => $"{Group.Name} ({Items.Count})";

    public GroupBucket(SegmentTypeGroupDefinition group, ObservableCollection<SegmentTypeDefinition> items)
    {
        Group = group;
        Items = items;
    }
}
