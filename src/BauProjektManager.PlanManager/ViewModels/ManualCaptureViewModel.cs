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

    /// <summary>Kombi-Plan erkannt (mehrere Plantyp-Keywords im Namen, 111.07 Slice C) — Badge + Panel-Warnhinweis.</summary>
    public bool IsCombi => Item.Candidates.IsCombi;

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

    /// <summary>Vom User erfasste Bezeichnung (Slice A3) — fliesst bei Erstaufnahmen in plan_documents.title.</summary>
    [ObservableProperty]
    private string? _title;

    /// <summary>Änderungshinweis der einlaufenden Revision (BPM-118, → plan_revisions.change_note).</summary>
    [ObservableProperty]
    private string? _changeNote;

    /// <summary>Index-Datum der einlaufenden Revision als ISO-UTC (BPM-118, → plan_revisions.released_at).</summary>
    [ObservableProperty]
    private string? _releasedAtIso;

    /// <summary>Per Text-Zuweisung vorgemerkte Segmentwerte (Key = SegmentTypeId, BPM-118).</summary>
    public Dictionary<string, AssignedSegmentValue> AssignedSegments { get; } = new();

    /// <summary>Dateiname des PDF/DWG-Partners (111.07 Slice A) — NULL = kein Paar. Steuert Badge + Panel-Hinweis.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPaired))]
    private string? _pairedFileName;

    public bool IsPaired => PairedFileName is not null;

    /// <summary>Pending-Zielordner (NULL = nicht zugeordnet) — steuert die Gelb-Markierung.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPending))]
    [NotifyPropertyChangedFor(nameof(PendingText))]
    private string? _pendingTarget;

    public bool IsPending => PendingTarget is not null;
    public string PendingText => PendingTarget is null ? "" : $"⏳ {PendingTarget}";
}

/// <summary>
/// Quelle einer Plan-Vorschau (BPM-111.06 Slice C3): welcher relative Pfad
/// gezeigt wird und welche Zeile Ziel der Text-Zuweisung ist (NULL bei
/// Archiv-Anzeige — dort ist Zuweisen bewusst inaktiv, die gezeigte Datei
/// ist die ALTE Revision). Note = Statuszeilen-Hinweis wenn nicht die
/// geklickte Datei selbst gezeigt wird.
/// </summary>
public sealed record PreviewSource(
    CaptureRowViewModel? Row,
    string RelativePath,
    string? Note);

/// <summary>
/// ViewModel der manuellen Plan-Erfassung (BPM-111.05 Slice 2b, ADR-059):
/// Eingangs-Tabelle aus den ManualFirstCapture-Buckets, Radial-Ebenenlogik
/// via <see cref="RadialSelectionController"/>, Pending Assignments +
/// Bestaetigen ueber die 111.04-Strecke, Undo letzter Import.
/// Gesten (Hold/Capture/Release) liegen im View-Code-Behind.
/// </summary>
public partial class ManualCaptureViewModel : ObservableObject
{
    private readonly PlanManagerDatabase _planDb;
    private readonly ManualFirstCaptureService _capture;
    private readonly PendingAssignmentStore _pending;
    private readonly CaptureConfirmService _confirm;
    private readonly ImportUndoService _undo;
    private readonly ArchiveMoveService _move;
    private readonly PreImportRecoveryCheck _preImportCheck = new();
    private readonly ProjectDatabase _bpmDb;
    private readonly DocumentTypeSeedService _seed;
    private readonly DocumentTypeCreationService _creation;

    private string _projectId = string.Empty;
    private string _projectRootPath = string.Empty;
    private string _inboxRelativePath = "_Eingang";
    private string _plansRelativePath = "Pläne";

    private IReadOnlyList<PlanDocumentType> _types = [];
    private IReadOnlyList<BuildingPart> _parts = [];

    public ManualCaptureViewModel(
        PlanManagerDatabase planDb, ProjectDatabase bpmDb, IIdGenerator idGenerator)
    {
        _planDb = planDb;
        _capture = new ManualFirstCaptureService(planDb);
        _pending = new PendingAssignmentStore();
        // BPM-120 T1: eine FS-Port-Instanz-Welt fuer Execution/Undo (ADR-060/064,
        // lokale Constructor Injection — DI-Container kommt post-V1).
        var fs = new LocalFileSystem();
        _confirm = new CaptureConfirmService(
            new ImportExecutionService(planDb, idGenerator, fs, fs, fs), _pending);
        _undo = new ImportUndoService(planDb, fs, fs, fs);
        _move = new ArchiveMoveService(planDb);
        _bpmDb = bpmDb;
        _seed = new DocumentTypeSeedService(bpmDb);
        _creation = new DocumentTypeCreationService(bpmDb, new PlanValueNormalizer());
    }

    public ObservableCollection<CaptureRowViewModel> Rows { get; } = [];

    /// <summary>Archiv-Bestand (Sub-Tab „Archiv", 111.07 Slice D) — read-only aus der DB.</summary>
    public ObservableCollection<ArchiveRowViewModel> ArchiveRows { get; } = [];

    /// <summary>Tab-Header „Neue Pläne (N)" (111.07 Slice D).</summary>
    [ObservableProperty]
    private string _inboxTabHeader = "Neue Pläne";

    /// <summary>Tab-Header „Archiv (M)" (111.07 Slice D).</summary>
    [ObservableProperty]
    private string _archiveTabHeader = "Archiv";

    /// <summary>Projekt-Root für absolute Pfade (Vorschau/Launcher, BPM-111.06 Slice C).</summary>
    public string ProjectRootPath => _projectRootPath;

    [ObservableProperty]
    private string _statusText = "bereit";

    /// <summary>Neutrale Zusammenfassung des letzten Refresh — Restore-Ziel für Bulk-Hinweise (BPM-122).</summary>
    private string _summaryStatusText = "bereit";

    /// <summary>True solange StatusText einen Bulk-Hinweis aus BeginCapture zeigt (BPM-122).</summary>
    private bool _statusIsBulkHint;

    // Jede andere Meldung beendet den Bulk-Hinweis-Zustand automatisch.
    partial void OnStatusTextChanged(string value) => _statusIsBulkHint = false;

    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private bool _canUndoLastImport;

    /// <summary>Detail-Panel-Inhalt der einzeln gewählten Zeile (null = keine/mehrere Auswahl).</summary>
    [ObservableProperty]
    private CaptureDetailViewModel? _selectedDetail;

    /// <summary>Platzhaltertext im Detail-Panel bei keiner oder mehrfacher Auswahl.</summary>
    [ObservableProperty]
    private string _detailPlaceholder = "Keine Auswahl";

    /// <summary>
    /// BPM-120 H0: Bestätigen ist durch pending Imports blockiert — die View
    /// öffnet darauf die Recovery-Strecke (BPM-016). Seit dem Alt-Import-Cutover
    /// ist die Radial-Strecke der einzige Recovery-Einstieg.
    /// </summary>
    public event EventHandler? RecoveryRequested;

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
        UpdatePairFlags();
        UpdatePendingState();
        LoadArchive();
        _summaryStatusText = $"{result.TotalFiles} Dateien — {result.DuplicateCount} Dubletten, " +
                     $"{result.UpdateProposalCount} Updates, {result.NewCaptureCount} neu, " +
                     $"{result.ConflictCount} Konflikte";
        StatusText = _summaryStatusText;
        CanUndoLastImport = _undo.Preflight(_projectRootPath).CanUndo;
        SetSelectedRow();
    }

    /// <summary>Archiv-Bestand + Tab-Header neu laden (111.07 Slice D).</summary>
    private void LoadArchive()
    {
        var lastImportId = _planDb.GetLastCompletedImportId();
        ArchiveRows.Clear();
        foreach (var entry in _planDb.GetArchiveEntries())
            ArchiveRows.Add(new ArchiveRowViewModel(
                entry, lastImportId is not null && entry.LastImportId == lastImportId));
        InboxTabHeader = $"Neue Pläne ({Rows.Count})";
        ArchiveTabHeader = $"Archiv ({ArchiveRows.Count})";
    }

    // ── Radial-Orchestrierung (vom Gesten-Host aufgerufen) ─────────

    /// <summary>
    /// Startet einen Capture-Vorgang fuer die uebergebene (Anker-)Zeile.
    /// 111.07 Slice B: Bulk-Vorprüfung über die effektive Zuordnungsliste
    /// (inkl. Paar-Partner) — NULL wenn geblockt (Radial öffnet nicht),
    /// Warnungen landen in der Statuszeile.
    /// </summary>
    public RadialSelectionController? BeginCapture(CaptureRowViewModel anchorRow)
    {
        if (!anchorRow.IsSelected)
        {
            foreach (var r in Rows) r.IsSelected = false;
            anchorRow.IsSelected = true;
        }

        var selected = SelectedRows.Where(r => !r.IsDuplicate && !r.IsUpdate).ToList();
        var effective = ExpandWithPairedRows(selected, Rows);
        var check = BulkPrecheck.Evaluate(effective);
        if (check.Gate == BulkGate.Blocked)
        {
            StatusText = $"⛔ {check.BlockReason}";
            _statusIsBulkHint = true; // BPM-122: verschwindet bei Auswahländerung
            return null;
        }
        if (check.Warnings.Count > 0)
        {
            StatusText = "⚠ " + string.Join(" · ", check.Warnings);
            _statusIsBulkHint = true;
        }

        var controller = new RadialSelectionController(_types, _parts);
        controller.Reset();
        return controller;
    }

    public IReadOnlyList<CaptureRowViewModel> SelectedRows =>
        [.. Rows.Where(r => r.IsSelected)];

    /// <summary>
    /// Startet einen Verschiebe-Vorgang für eine Archiv-Zeile (111.07 Slice D):
    /// gleiches Radial, aber Ziel = sofortiger Move statt Pending.
    /// </summary>
    public RadialSelectionController BeginMove(ArchiveRowViewModel row)
    {
        foreach (var r in ArchiveRows)
            r.IsSelected = ReferenceEquals(r, row);
        var controller = new RadialSelectionController(_types, _parts);
        controller.Reset();
        return controller;
    }

    /// <summary>
    /// Führt den Archiv-Move aus (111.07 Slice D): journalisiert (Status
    /// 'moved'), sofort — kein Pending, kein Undo (Undo gilt nur dem letzten
    /// Import). Lädt den Archiv-Bestand danach neu.
    /// </summary>
    public void CompleteMove(RadialSelectionController controller, ArchiveRowViewModel row)
    {
        if (controller.SelectedType is null)
            return;

        var targetDir = controller.BuildTargetDirectory(_plansRelativePath);
        var result = _move.MoveDocument(row.Entry, targetDir, _projectRootPath);
        StatusText = result.Success
            ? $"↷ {row.FileName} verschoben nach {targetDir} ({result.MovedFiles} Datei(en), Journal)"
            : $"⚠ Verschieben fehlgeschlagen: {result.Error}";
        LoadArchive();
    }

    // ── Schnellanlage „+ Neu…" (Slice 3) ────────────────────────────
    // Stammdaten direkt aus dem Radial anlegen; Feinpflege in den Projekt-
    // Einstellungen. Jede Methode schreibt in die DB und laedt die betroffene
    // Stammdatenliste neu, sodass der Controller (RefreshStammdaten) und der
    // naechste Capture die Neuanlage sehen.

    private const string DefaultTypeColor = "#6E6E6E";

    public IReadOnlyList<PlanDocumentType> Types => _types;
    public IReadOnlyList<BuildingPart> Parts => _parts;

    /// <summary>
    /// Legt einen Dokumenttyp aus dem "+ Neu…"-Pflichtdialog an (ADR-061): Name +
    /// Ablagebereich (root_relative_path) + Unterteilung (Ring2Source) + optionaler
    /// Ordnername. key/Normalisierung/Eindeutigkeit uebernimmt der CreationService.
    /// </summary>
    public PlanDocumentType AddDocumentType(
        string name, string rootRelativePath, Ring2Source ring2Source, string? folderName)
    {
        var created = _creation.Create(
            _projectId, name, rootRelativePath, ring2Source,
            folderName, colorHex: DefaultTypeColor);
        _types = _bpmDb.GetDocumentTypes(_projectId);
        return _types.First(t => t.Id == created.Id);
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
    /// erfolgt spaeter beim Bestaetigen/Panel-Edit). 111.07 Slice A:
    /// nicht-selektierte PDF/DWG-Partner werden automatisch mit zugeordnet
    /// (gleicher Stamm -> gleiche Identitaet -> EINE Revision, zwei Dateien).
    /// </summary>
    public void CompleteCapture(RadialSelectionController controller)
    {
        if (controller.SelectedType is null)
            return;

        var targetDir = controller.BuildTargetDirectory(_plansRelativePath);
        var selected = SelectedRows.Where(r => !r.IsDuplicate && !r.IsUpdate).ToList();
        var rows = ExpandWithPairedRows(selected, Rows);
        var pairedCount = rows.Count - selected.Count;

        foreach (var row in rows)
        {
            var c = row.Item.Candidates;
            _pending.Assign(new PendingAssignment(
                row.Item.File, row.Item.Bucket,
                controller.SelectedType.Id, controller.SelectedType.Name,
                controller.SelectedPart, controller.SelectedLevel,
                c.PlanNumber, c.Index, targetDir, Match: null,
                Title: NormalizeText(row.Title),
                ChangeNote: NormalizeText(row.ChangeNote),
                ReleasedAt: row.ReleasedAtIso,
                AssignedSegments: [.. row.AssignedSegments.Values]));
            row.PendingTarget = targetDir;
            row.IsSelected = false;
        }
        if (pairedCount > 0)
            StatusText = $"⛓ {pairedCount} gepaarte Datei(en) automatisch mit zugeordnet";
        UpdatePendingState();
    }

    [RelayCommand]
    private void TakeUpdate()
    {
        // Arbeitet auf der aktuell im Detail-Panel gezeigten Zeile — kein
        // CommandParameter (der beim ContentControl-DataContext-Wechsel als null
        // ankommen kann). Button ist ohnehin nur bei CanTakeUpdate sichtbar.
        var row = SelectedDetail?.Row;
        var match = row?.Item.Match;
        if (row is null || match is null)
            return;

        AssignUpdate(row, match);

        // 111.07 Slice A2: PDF/DWG-Partner (gleicher Stamm) mit übernehmen —
        // beide Dateien gehören zur selben neuen Revision (der Import-Guard
        // in der Execution verhindert das Doppel-Supersede).
        var pairedExtension = PairedExtensionFor(row);
        var partner = pairedExtension is null ? null : FindPairedRow(row, Rows, pairedExtension);
        if (partner is not null && !partner.IsDuplicate && !partner.IsPending)
        {
            AssignUpdate(partner, partner.Item.Match ?? match);
            StatusText = $"⛓ Gepaarte Datei mit übernommen: {partner.FileName}";
        }

        UpdatePendingState();
        SetSelectedRow();
    }

    /// <summary>Update-Übernahme einer Zeile auf das bekannte Dokument (Bucket B, 111.04/111.07 A2).</summary>
    private void AssignUpdate(CaptureRowViewModel row, KnownPlanDocument match)
    {
        var c = row.Item.Candidates;
        _pending.Assign(new PendingAssignment(
            row.Item.File, row.Item.Bucket,
            match.DocumentId, match.DocumentType,
            BuildingPart: null, Level: null,
            c.PlanNumber ?? match.PlanNumber, c.Index,
            match.RelativeDirectory, match,
            ChangeNote: NormalizeText(row.ChangeNote),
            ReleasedAt: row.ReleasedAtIso,
            AssignedSegments: [.. row.AssignedSegments.Values]));
        row.PendingTarget = match.RelativeDirectory;
    }

    /// <summary>
    /// Slice A2: Panel-Edit von Plannummer/Index anwenden — Identitätswechsel
    /// (Spez 111.06: "Plannummer-Änderung = Identitätswechsel -> Re-Matching").
    /// Klassifiziert die Zeile per <see cref="ManualFirstCaptureService.RematchByNumber"/>
    /// neu (Bucket B/C/D) und ersetzt sie im Grid. Eine stale Pending-Zuordnung
    /// der alten Identität wird verworfen. MD5-Dubletten (Bucket A) sind vom
    /// Edit ausgenommen (CanEditIdentity im Detail-VM).
    /// </summary>
    [RelayCommand]
    private void ApplyIdentityEdit()
    {
        var detail = SelectedDetail;
        if (detail is null || detail.Row.IsDuplicate)
            return;
        var row = detail.Row;

        var number = string.IsNullOrWhiteSpace(detail.EditPlanNumber)
            ? null : detail.EditPlanNumber.Trim();
        var index = string.IsNullOrWhiteSpace(detail.EditIndex)
            ? null : detail.EditIndex.Trim();

        var (bucket, match, reason) = _capture.RematchByNumber(number, index);

        var newCandidates = row.Item.Candidates with
        {
            PlanNumber = number,
            Index = index,
            RevisionKind = RevisionKindDetector.Detect(index)
        };
        var newItem = new CaptureItem(row.Item.File, newCandidates, bucket, match, reason);

        // Stale Pending der alten Identität verwerfen
        if (_pending.Discard(row.Item.File.Scan.RelativePath))
            UpdatePendingState();

        var newRow = new CaptureRowViewModel(newItem)
        {
            IsSelected = true,
            Title = row.Title,
            ChangeNote = row.ChangeNote,
            ReleasedAtIso = row.ReleasedAtIso
        };
        foreach (var kv in row.AssignedSegments)
            newRow.AssignedSegments[kv.Key] = kv.Value;
        var pos = Rows.IndexOf(row);
        if (pos >= 0) Rows[pos] = newRow;
        else Rows.Add(newRow);
        UpdatePairFlags();

        StatusText = bucket switch
        {
            CaptureBucket.UpdateProposal =>
                $"Re-Match: bekannter Plan {match!.PlanNumber} — Update-Vorschlag",
            CaptureBucket.Conflict => $"Re-Match: Konflikt — {reason}",
            _ => "Re-Match: Erstaufnahme (kein bekanntes Dokument)"
        };
        Log.Information("Panel-Edit Re-Match: {File} -> {Bucket}",
            row.FileName, bucket);
        SetSelectedRow();
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
        // BPM-111.05 Slice 3d + BPM-120 H0: Recovery-Check vor dem Bestätigen. Existiert
        // noch ein pending Import (App-Crash im letzten Confirm, via Cloud gesyncter
        // Fremd-Stand), darf kein neuer Journal-Vorgang starten — sonst kollidieren die
        // pending Aktionen mit dem neuen Import. RecoveryRequested informiert die View,
        // die darauf den BPM-016-Dialog-Flow öffnet (Forward/Rollback/Cleanup/Später).
        var check = _preImportCheck.Evaluate(_planDb.GetPendingImports());
        if (!check.CanConfirm)
        {
            StatusText = $"⛔ {check.Message}";
            Log.Warning("ManualCapture-Bestaetigung blockiert: {Count} pending Import(e)",
                check.BlockingImports.Count);
            RecoveryRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Slice A3 + BPM-118: Panel-/Vorschau-Eingaben in die Pending Assignments
        // uebernehmen (deckt Edits NACH der Radial-Zuordnung ab — Store haelt
        // sonst den Stand vom Zuordnungszeitpunkt).
        foreach (var row in Rows.Where(r => r.IsPending))
        {
            var p = _pending.Get(row.RelativePath);
            if (p is not null)
                _pending.Assign(p with
                {
                    Title = NormalizeText(row.Title),
                    ChangeNote = NormalizeText(row.ChangeNote),
                    ReleasedAt = row.ReleasedAtIso,
                    AssignedSegments = [.. row.AssignedSegments.Values]
                });
        }

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

    // ── Vorschau-Quelle (BPM-111.06 Slice C3) ───────────────────────

    /// <summary>
    /// Löst die Vorschau-Quelle einer Zeile auf: PDFs direkt; für DWGs die
    /// gepaarte PDF — Eingangs-Partner (gleicher Dateinamens-Stamm) vor
    /// Archiv-PDF der aktuellen Revision des bekannten Dokuments.
    /// NULL = keine Vorschau möglich (Menüpunkt bleibt inaktiv).
    /// </summary>
    public PreviewSource? ResolvePreviewSource(CaptureRowViewModel row)
    {
        var extension = row.Item.File.Scan.Extension;
        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return new PreviewSource(row, row.RelativePath, Note: null);

        if (!extension.Equals(".dwg", StringComparison.OrdinalIgnoreCase))
            return null;

        var partner = FindPairedPdfRow(row, Rows);
        if (partner is not null)
            return new PreviewSource(partner, partner.RelativePath,
                $"Vorschau zeigt gepaartes PDF: {partner.FileName}");

        var match = row.Item.Match;
        if (match is not null)
        {
            var archivePdf = _planDb.GetPdfPathForRevision(match.CurrentRevisionId);
            if (archivePdf is not null)
                return new PreviewSource(Row: null, archivePdf,
                    $"Vorschau zeigt aktuelle Archiv-Revision (Index {match.CurrentIndex ?? "Erstausgabe"})");
        }

        return null;
    }

    /// <summary>
    /// Vorschau-Quelle einer Archiv-Zeile (111.07 Slice D): Primärdatei wenn
    /// PDF, sonst die gepaarte PDF der Revision. Immer read-only (Row NULL —
    /// Text-Zuweisung gilt nur für Eingangs-Zeilen). NULL = keine Vorschau.
    /// </summary>
    public PreviewSource? ResolveArchivePreviewSource(ArchiveRowViewModel row)
    {
        var relativePath = row.Entry.RelativePath;
        if (relativePath is not null
            && relativePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return new PreviewSource(Row: null, relativePath, Note: null);

        var pairedPdf = _planDb.GetPdfPathForRevision(row.Entry.RevisionId);
        return pairedPdf is null ? null
            : new PreviewSource(Row: null, pairedPdf, "Vorschau zeigt die gepaarte PDF der Revision");
    }

    /// <summary>
    /// Pure Paar-Findung (Slice C3): PDF-Zeile mit gleichem Dateinamens-Stamm
    /// wie die DWG-Zeile (ordinal, case-insensitiv) — ohne System.IO (ADR-060).
    /// </summary>
    public static CaptureRowViewModel? FindPairedPdfRow(
        CaptureRowViewModel dwgRow, IEnumerable<CaptureRowViewModel> rows)
        => FindPairedRow(dwgRow, rows, ".pdf");

    /// <summary>Paar-Partner mit gewünschter Extension und gleichem Dateinamens-Stamm (111.07 Slice A).</summary>
    public static CaptureRowViewModel? FindPairedRow(
        CaptureRowViewModel row, IEnumerable<CaptureRowViewModel> rows, string pairedExtension)
    {
        var stem = FileNameStem(row.Item.File.Scan);
        return rows.FirstOrDefault(r =>
            !ReferenceEquals(r, row)
            && r.Item.File.Scan.Extension.Equals(pairedExtension, StringComparison.OrdinalIgnoreCase)
            && FileNameStem(r.Item.File.Scan).Equals(stem, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Partner-Extension einer Zeile: .pdf↔.dwg, sonst NULL (111.07 Slice A).</summary>
    private static string? PairedExtensionFor(CaptureRowViewModel row)
    {
        var extension = row.Item.File.Scan.Extension;
        return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) ? ".dwg"
            : extension.Equals(".dwg", StringComparison.OrdinalIgnoreCase) ? ".pdf"
            : null;
    }

    /// <summary>
    /// Setzt die Paar-Kennzeichnung aller Zeilen (⛓-Badge + Panel-Hinweis,
    /// 111.07 Slice A) — nach jeder Rows-Änderung aufrufen.
    /// </summary>
    private void UpdatePairFlags()
    {
        foreach (var row in Rows)
        {
            var pairedExtension = PairedExtensionFor(row);
            row.PairedFileName = pairedExtension is null ? null
                : FindPairedRow(row, Rows, pairedExtension)?.FileName;
        }
    }

    /// <summary>
    /// 111.07 Slice A: ergänzt die Zuordnungs-Liste um nicht-selektierte
    /// PDF/DWG-Partner (gleicher Dateinamens-Stamm) — beide Dateien landen so
    /// in derselben Radial-Zuordnung und werden EIN Dokument mit EINER Revision
    /// und zwei Dateien. Duplikate/Updates werden nicht mitgenommen.
    /// </summary>
    public static List<CaptureRowViewModel> ExpandWithPairedRows(
        IReadOnlyList<CaptureRowViewModel> selected, IEnumerable<CaptureRowViewModel> allRows)
    {
        var result = new List<CaptureRowViewModel>(selected);
        foreach (var row in selected)
        {
            var pairedExtension = PairedExtensionFor(row);
            if (pairedExtension is null)
                continue;

            var partner = FindPairedRow(row, allRows, pairedExtension);
            if (partner is null || partner.IsDuplicate || partner.IsUpdate || result.Contains(partner))
                continue;
            result.Add(partner);
        }
        return result;
    }

    private static string FileNameStem(ScannedFile scan)
        => scan.FileName.Length > scan.Extension.Length
            ? scan.FileName[..^scan.Extension.Length]
            : scan.FileName;

    /// <summary>Leerer/Whitespace-Text -> NULL, sonst getrimmt (Slice A3, BPM-118).</summary>
    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Baut den Detail-Panel-Inhalt für die aktuelle Auswahl (BPM-111.06 Slice A):
    /// genau eine Zeile → Detail-VM inkl. Index-Historie; sonst Platzhalter.
    /// Wird vom Gesten-Host bei Auswahländerung und nach Refresh/TakeUpdate gerufen.
    /// </summary>
    public void SetSelectedRow()
    {
        // BPM-122: Bulk-Hinweise (⚠/⛔ aus BeginCapture) gelten nur für die
        // Auswahl, mit der das Radial gestartet wurde — bei Auswahländerung
        // kehrt die neutrale Zusammenfassung zurück.
        if (_statusIsBulkHint)
            StatusText = _summaryStatusText;

        var selected = SelectedRows;
        if (selected.Count == 1)
        {
            SelectedDetail = new CaptureDetailViewModel(selected[0], BuildHistory(selected[0]));
            DetailPlaceholder = string.Empty;
        }
        else
        {
            SelectedDetail = null;
            DetailPlaceholder = selected.Count == 0
                ? "Keine Auswahl"
                : $"{selected.Count} Datei(en) ausgewählt — halten & ziehen";
        }
    }

    /// <summary>
    /// Index-Historie (Slice D): IMMER befüllt, neueste oben. Erste Zeile = die
    /// einlaufende Datei selbst ("(neu)", hervorgehoben) — außer bei Dubletten,
    /// die nicht importiert werden. Danach die plan_revisions des bekannten
    /// Dokuments: Revision | Datum (released_at, sonst current_from) | Änderung
    /// (change_note, sonst "Erstausgabe"/"—").
    /// </summary>
    private IReadOnlyList<PlanRevisionHistoryRow> BuildHistory(CaptureRowViewModel row)
    {
        var rows = new List<PlanRevisionHistoryRow>();
        var match = row.Item.Match;

        if (!row.IsDuplicate)
        {
            var newIndex = row.Item.Candidates.Index ?? "—";
            var newDate = row.ReleasedAtIso is not null
                ? FormatRevisionDate(row.ReleasedAtIso)
                : "heute";
            var newChange = row.ChangeNote
                ?? (match is null ? "Erstausgabe" : "—");
            rows.Add(new PlanRevisionHistoryRow(
                match is null ? newIndex : $"{newIndex} (neu)",
                newDate,
                newChange,
                IsNew: true));
        }

        if (match is not null)
        {
            var revisions = _planDb.GetRevisionsForDocument(match.DocumentId);
            for (var i = revisions.Count - 1; i >= 0; i--)
            {
                var r = revisions[i];
                var change = !string.IsNullOrWhiteSpace(r.ChangeNote)
                    ? r.ChangeNote
                    : r.PlanIndex is null ? "Erstausgabe" : "—";
                rows.Add(new PlanRevisionHistoryRow(
                    r.PlanIndex ?? "—",
                    FormatRevisionDate(r.ReleasedAt ?? r.CurrentFrom),
                    change,
                    IsNew: false));
            }
        }

        return rows;
    }

    private static string FormatRevisionDate(string isoUtc)
        => DateTime.TryParse(isoUtc, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt.ToLocalTime().ToString("dd.MM.yyyy")
            : string.Empty;
}
