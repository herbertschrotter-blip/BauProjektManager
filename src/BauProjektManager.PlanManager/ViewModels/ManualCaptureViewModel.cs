using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// Eine Eingangs-Datei in der ManuellSortieren-Tabelle (BPM-111.05 Slice 2b).
/// </summary>
public partial class CaptureRowViewModel : ObservableObject
{
    public CaptureRowViewModel(CaptureItem item)
    {
        Item = item;
    }

    public CaptureItem Item { get; }

    public string FileName => Item.File.Scan.FileName;
    public string RelativePath => Item.File.Scan.RelativePath;
    public string SizeText => $"{Item.File.Scan.FileSize / 1024} KB";
    public bool IsUpdate => Item.Bucket == CaptureBucket.UpdateProposal;
    public bool IsDuplicate => Item.Bucket == CaptureBucket.Duplicate;
    public bool IsConflict => Item.Bucket == CaptureBucket.Conflict;
    public string? Reason => Item.Reason;

    public string CandidateText
    {
        get
        {
            var c = Item.Candidates;
            if (IsUpdate)
                return $"⬆ Index {c.Index ?? "—"} neu — bekannter Plan";
            var type = c.TypeKeywords.FirstOrDefault();
            return type is null ? "—" : $"Kandidat: {type}{(c.Level is not null ? $" / {c.Level}" : "")}";
        }
    }

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Pending-Zielordner (NULL = nicht zugeordnet) — steuert die Gelb-Markierung.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPending))]
    [NotifyPropertyChangedFor(nameof(PendingText))]
    private string? _pendingTarget;

    public bool IsPending => PendingTarget is not null;
    public string PendingText => PendingTarget is null ? "" : $"⏳ {PendingTarget}";
}

/// <summary>
/// ViewModel der manuellen Plan-Erfassung (BPM-111.05 Slice 2b, ADR-059):
/// Eingangs-Tabelle aus den ManualFirstCapture-Buckets, Radial-Ebenenlogik
/// via <see cref="RadialSelectionController"/>, Pending Assignments +
/// Bestaetigen ueber die 111.04-Strecke, Undo letzter Import.
/// Gesten (Hold/Capture/Release) liegen im View-Code-Behind.
/// </summary>
public partial class ManualCaptureViewModel : ObservableObject
{
    private readonly ManualFirstCaptureService _capture;
    private readonly PendingAssignmentStore _pending;
    private readonly CaptureConfirmService _confirm;
    private readonly ImportUndoService _undo;
    private readonly ProjectDatabase _bpmDb;
    private readonly DocumentTypeSeedService _seed;

    private string _projectId = string.Empty;
    private string _projectRootPath = string.Empty;
    private string _inboxRelativePath = "_Eingang";
    private string _plansRelativePath = "Pläne";

    private IReadOnlyList<PlanDocumentType> _types = [];
    private IReadOnlyList<BuildingPart> _parts = [];

    public ManualCaptureViewModel(
        PlanManagerDatabase planDb, ProjectDatabase bpmDb, IIdGenerator idGenerator)
    {
        _capture = new ManualFirstCaptureService(planDb);
        _pending = new PendingAssignmentStore();
        _confirm = new CaptureConfirmService(planDb, idGenerator, _pending);
        _undo = new ImportUndoService(planDb);
        _bpmDb = bpmDb;
        _seed = new DocumentTypeSeedService(bpmDb);
    }

    public ObservableCollection<CaptureRowViewModel> Rows { get; } = [];

    [ObservableProperty]
    private string _statusText = "bereit";

    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private bool _canUndoLastImport;

    /// <summary>Projekt-Kontext setzen und Eingang analysieren.</summary>
    public async Task InitializeAsync(
        string projectId, string projectRootPath,
        string inboxRelativePath, string plansRelativePath)
    {
        _projectId = projectId;
        _projectRootPath = projectRootPath;
        _inboxRelativePath = inboxRelativePath;
        _plansRelativePath = plansRelativePath;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _seed.EnsureSeeded(_projectId);
        _types = _bpmDb.GetDocumentTypes(_projectId);
        _parts = _bpmDb.GetBuildingParts(_projectId);

        var result = await _capture.AnalyzeAsync(_projectRootPath, _inboxRelativePath);
        Rows.Clear();
        foreach (var item in result.Items)
        {
            var row = new CaptureRowViewModel(item)
            {
                PendingTarget = _pending.Get(item.File.Scan.RelativePath)?.TargetRelativeDirectory
            };
            Rows.Add(row);
        }
        UpdatePendingState();
        StatusText = $"{result.TotalFiles} Dateien — {result.DuplicateCount} Dubletten, " +
                     $"{result.UpdateProposalCount} Updates, {result.NewCaptureCount} neu, " +
                     $"{result.ConflictCount} Konflikte";
        CanUndoLastImport = _undo.Preflight(_projectRootPath).CanUndo;
    }

    // ── Radial-Orchestrierung (vom Gesten-Host aufgerufen) ─────────

    /// <summary>Startet einen Capture-Vorgang fuer die uebergebene (Anker-)Zeile.</summary>
    public RadialSelectionController BeginCapture(CaptureRowViewModel anchorRow)
    {
        if (!anchorRow.IsSelected)
        {
            foreach (var r in Rows) r.IsSelected = false;
            anchorRow.IsSelected = true;
        }
        var controller = new RadialSelectionController(_types, _parts);
        controller.Reset();
        return controller;
    }

    public IReadOnlyList<CaptureRowViewModel> SelectedRows =>
        [.. Rows.Where(r => r.IsSelected)];

    // ── Schnellanlage „+ Neu…" (Slice 3) ────────────────────────────
    // Stammdaten direkt aus dem Radial anlegen; Feinpflege in den Projekt-
    // Einstellungen. Jede Methode schreibt in die DB und laedt die betroffene
    // Stammdatenliste neu, sodass der Controller (RefreshStammdaten) und der
    // naechste Capture die Neuanlage sehen.

    private const string DefaultTypeColor = "#6E6E6E";

    public IReadOnlyList<PlanDocumentType> Types => _types;
    public IReadOnlyList<BuildingPart> Parts => _parts;

    public PlanDocumentType AddDocumentType(string name)
    {
        var id = _bpmDb.InsertDocumentType(
            _projectId, name, folderName: null, colorHex: DefaultTypeColor,
            Ring2Source.None, sortOrder: _types.Count * 10, isBuiltin: false);
        _types = _bpmDb.GetDocumentTypes(_projectId);
        return _types.First(t => t.Id == id);
    }

    public PlanDocumentType AddCategory(PlanDocumentType type, string name)
    {
        _bpmDb.InsertDocumentTypeCategory(
            type.Id, name, folderName: null, sortOrder: type.Categories.Count * 10);
        _types = _bpmDb.GetDocumentTypes(_projectId);
        return _types.First(t => t.Id == type.Id);
    }

    public BuildingPart AddBuildingPart(string shortName)
    {
        var id = _bpmDb.InsertBuildingPart(_projectId, shortName);
        _parts = _bpmDb.GetBuildingParts(_projectId);
        return _parts.First(p => p.Id == id);
    }

    public BuildingPart AddBuildingLevel(BuildingPart part, string levelName)
    {
        _bpmDb.InsertBuildingLevel(part.Id, levelName);
        _parts = _bpmDb.GetBuildingParts(_projectId);
        return _parts.First(p => p.Id == part.Id);
    }

    /// <summary>
    /// Release im Radial: Pending Assignment fuer alle ausgewaehlten Zeilen
    /// (PlanNr/Index je Zeile aus den eigenen Kandidaten — ManualConfirmed
    /// erfolgt spaeter beim Bestaetigen/Panel-Edit).
    /// </summary>
    public void CompleteCapture(RadialSelectionController controller)
    {
        if (controller.SelectedType is null)
            return;

        var targetDir = controller.BuildTargetDirectory(_plansRelativePath);
        foreach (var row in SelectedRows.Where(r => !r.IsDuplicate && !r.IsUpdate))
        {
            var c = row.Item.Candidates;
            _pending.Assign(new PendingAssignment(
                row.Item.File, row.Item.Bucket,
                controller.SelectedType.Id, controller.SelectedType.Name,
                controller.SelectedPart, controller.SelectedLevel,
                c.PlanNumber, c.Index, targetDir, Match: null));
            row.PendingTarget = targetDir;
            row.IsSelected = false;
        }
        UpdatePendingState();
    }

    [RelayCommand]
    private void TakeUpdate(CaptureRowViewModel row)
    {
        if (row.Item.Match is null)
            return;
        var c = row.Item.Candidates;
        _pending.Assign(new PendingAssignment(
            row.Item.File, row.Item.Bucket,
            row.Item.Match.DocumentId, row.Item.Match.DocumentType,
            BuildingPart: null, Level: null,
            c.PlanNumber ?? row.Item.Match.PlanNumber, c.Index,
            row.Item.Match.RelativeDirectory, row.Item.Match));
        row.PendingTarget = row.Item.Match.RelativeDirectory;
        UpdatePendingState();
    }

    [RelayCommand]
    private void DiscardPending()
    {
        _pending.Clear();
        foreach (var row in Rows)
            row.PendingTarget = null;
        UpdatePendingState();
        StatusText = "Pending verworfen";
    }

    [RelayCommand]
    private async Task ConfirmImportAsync()
    {
        var result = _confirm.ConfirmAll(_projectRootPath, _inboxRelativePath);
        StatusText = result.Failed == 0
            ? $"✓ {result.Succeeded} Datei(en) importiert (Journal → Move → DB)"
            : $"⚠ {result.Failed} von {result.Succeeded + result.Failed} Aktionen fehlgeschlagen";
        Log.Information("ManualCapture-Bestaetigung: {Ok} OK / {Fail} Fehler",
            result.Succeeded, result.Failed);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task UndoLastImportAsync()
    {
        var result = _undo.UndoLastImport(_projectRootPath);
        StatusText = result.Success
            ? $"↩ Import rückgängig: {result.RestoredFiles} Datei(en) zurück im Eingang"
            : $"⚠ Undo nicht möglich: {result.Errors.FirstOrDefault() ?? "Preflight-Konflikt"}";
        await RefreshAsync();
    }

    private void UpdatePendingState() => PendingCount = _pending.Count;
}
