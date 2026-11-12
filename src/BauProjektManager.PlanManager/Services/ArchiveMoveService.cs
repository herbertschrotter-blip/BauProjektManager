using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Journalisierter Archiv-Move getrackter Plaene (R01-008, ADR-064 + Addendum
/// P.4/P.5, ADR-061 P.5/P.6, ADR-060): alle Dateien der current-Revision ziehen
/// gemeinsam um. Ablauf wie ein Import:
/// 1. Preflight (Zielordner-Schutz, Dateien vorhanden, Ziel frei, Key frei) — mutiert nichts.
/// 2. Journal-Header + ALLE Actions (Typ 'moved') VOR der ersten Mutation.
/// 3. Disk-Phase ueber .bpm_tmp + atomaren Rename + Lock-Retry (FS-Ports).
/// 4. DB-Phase in EINER Transaction: Dateipfade, Ablage + Klassifikation,
///    Event, Actions completed, Journal 'moved'.
/// Scheitert Schritt 3/4, werden die bereits verschobenen Dateien sofort
/// zurueckgeschoben (Journal 'failed'); klappt das nicht, bleibt das Journal
/// 'pending' und die Recovery uebernimmt (siehe ArchiveMoveService.Recovery.cs).
/// Journal-Status 'moved' statt 'completed': ein Move ist nie „letzter Import".
/// </summary>
public partial class ArchiveMoveService
{
    private static readonly IPlanValueNormalizer _normalizer = new PlanValueNormalizer();
    private readonly PlanManagerDatabase _db;
    private readonly IFileSystemReader _reader;
    private readonly IFileSystemWriter _writer;
    private readonly IPathService _path;

    public ArchiveMoveService(
        PlanManagerDatabase db, IFileSystemReader reader, IFileSystemWriter writer, IPathService path)
    {
        _db = db;
        _reader = reader;
        _writer = writer;
        _path = path;
    }

    /// <summary>
    /// Ablageordner, innerhalb dessen der Explorer einen Plan verschieben darf
    /// (Entscheidung Herbert zu R01-008: nur eigener Ordner + Unterordner; Umklassifizieren
    /// nur ueber das Radial im Archiv-Tab, ADR-064 P.11). Null, wenn das Dokument fehlt.
    /// </summary>
    public string? GetExplorerMoveArea(PlanArchiveEntry entry)
        => _db.GetDocumentById(entry.DocumentId)?.RelativeDirectory;

    /// <summary>
    /// Verschiebt alle Dateien der current-Revision in den Zielordner. Ohne
    /// <paramref name="reclassification"/> (Explorer) bleibt die Klassifikation, das
    /// Ziel muss im eigenen Ablageordner liegen; mit (Radial) wird umklassifiziert.
    /// Journalisiert, nicht undo-bar.
    /// </summary>
    public MoveResult MoveDocument(
        PlanArchiveEntry entry, string targetRelativeDirectory, string projectRootPath,
        ArchiveReclassification? reclassification = null)
    {
        var doc = _db.GetDocumentById(entry.DocumentId);
        if (doc is null)
            return Fail("Dokument nicht gefunden");
        var files = _db.GetFilesForRevision(entry.RevisionId);
        if (files.Count == 0)
            return Fail("Keine Dateien mit der Revision verknüpft");

        var target = ArchiveMoveTargetGuard.TrimDirectory(targetRelativeDirectory);
        var guardError = ArchiveMoveTargetGuard.Check(
            target, reclassification is null ? doc.RelativeDirectory : null);
        if (guardError is not null)
            return Fail(guardError);

        var placement = BuildPlacement(doc, files, target, reclassification);
        var moves = files
            .Select(f => new PlannedMove(f, _path.Combine(target, f.FileName)))
            .ToList();
        var preflightError = Preflight(doc, placement, moves, projectRootPath);
        if (preflightError is not null)
            return Fail(preflightError);

        // ── Phase 1: Journal-Header + ALLE Actions VOR der ersten Mutation (P.2) ──
        var sourceDir = _path.GetDirectoryName(files[0].RelativePath) ?? "";
        var importId = _db.CreateImportJournal(sourceDir, moves.Count, profileId: null);
        var order = 0;
        foreach (var move in moves)
            move.ActionId = _db.InsertImportAction(
                importId, order++, PlanManagerDatabase.MovedActionType,
                placement.DocumentKey, doc.PlanNumber, entry.PlanIndex, oldIndex: null,
                move.File.RelativePath, move.TargetRelPath, archivePath: null,
                documentTypeId: placement.DocumentTypeId, documentType: placement.DocumentType,
                buildingPartId: placement.BuildingPartId, buildingLevelId: placement.BuildingLevelId);
        Log.Information("Archiv-Move: {Plan} ({Count} Datei(en)) -> {Target}",
            entry.PlanNumber, moves.Count, target);

        try
        {
            // ── Phase 2: Disk (.bpm_tmp + Rename + Lock-Retry, P.3/AK 7) ──
            _writer.CreateDirectory(_path.Combine(projectRootPath, target));
            foreach (var move in moves.Where(m => !m.IsInPlace))
                MoveViaTmp(_path.Combine(projectRootPath, move.File.RelativePath),
                    _path.Combine(projectRootPath, move.TargetRelPath));

            // ── Phase 3: DB in EINER Transaction inkl. Journal 'moved' (P.3) ──
            _db.ExecuteInTransaction(() => ApplyToDatabase(importId, doc, entry.RevisionId,
                [.. moves.Select(m => (m.ActionId!, m.File.FileId, m.TargetRelPath))], placement));
            return new MoveResult(true, moves.Count, null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Archiv-Move fehlgeschlagen: {Plan}", entry.PlanNumber);
            return AbortMove(importId, projectRootPath, ex.Message);
        }
    }

    /// <summary>Preflight ohne Mutation: Key frei, nicht schon dort, Quellen da, Ziele frei.</summary>
    private string? Preflight(PlanDocument doc, ArchiveMovePlacement placement,
        List<PlannedMove> moves, string projectRootPath)
    {
        var keyOwner = _db.FindDocumentIdByKeyIncludingDeleted(placement.DocumentKey);
        if (keyOwner is not null && keyOwner != doc.Id)
            return "Die Zielklassifikation ist bereits durch ein anderes Dokument belegt";

        if (moves.All(m => m.IsInPlace) && placement == CurrentPlacement(doc))
            return "Dokument liegt bereits in diesem Ordner";

        foreach (var move in moves.Where(m => !m.IsInPlace))
        {
            if (!_reader.FileExists(_path.Combine(projectRootPath, move.File.RelativePath)))
                return $"Datei fehlt: {move.File.FileName}";
            if (_reader.FileExists(_path.Combine(projectRootPath, move.TargetRelPath)))
                return $"Im Zielordner liegt bereits {move.File.FileName}";
        }
        return null;
    }

    /// <summary>
    /// Ziel-Ablage + Klassifikation. Ohne Umklassifizierung bleibt die Klassifikation;
    /// der Key wird bei Umklassifizierung aus den Stammdaten-IDs neu gebildet (ADR-059 P.3).
    /// </summary>
    private ArchiveMovePlacement BuildPlacement(PlanDocument doc, List<PlanRevisionFile> files,
        string target, ArchiveReclassification? reclassification)
    {
        if (reclassification is null)
            return ResolvePlacement(doc, target, doc.DocumentKey, doc.DocumentTypeId,
                doc.DocumentType, doc.BuildingPartId, doc.BuildingLevelId);

        var number = string.IsNullOrEmpty(doc.PlanNumber)
            ? _path.GetFileNameWithoutExtension(files[0].FileName)
            : doc.PlanNumber;
        var key = CaptureConfirmService.BuildDocumentKey(reclassification.DocumentTypeId, number,
            reclassification.BuildingPartId ?? reclassification.CategoryId,
            reclassification.BuildingLevelId, _normalizer);
        return ResolvePlacement(doc, target, key, reclassification.DocumentTypeId,
            reclassification.DocumentTypeName, reclassification.BuildingPartId,
            reclassification.BuildingLevelId);
    }

    /// <summary>
    /// Gemeinsame Ablage-Regel fuer normalen Move UND Recovery: relative_directory
    /// folgt dem Ziel, wenn umklassifiziert wird (Key aendert sich) oder das Ziel
    /// ausserhalb des bisherigen Ablageordners liegt; ein Explorer-Move in einen
    /// Unterordner laesst den Ablageordner (= Ort kuenftiger Revisionen) stehen.
    /// </summary>
    private static ArchiveMovePlacement ResolvePlacement(PlanDocument doc, string target,
        string key, string typeId, string typeName, string? partId, string? levelId)
    {
        var relDir = key != doc.DocumentKey
                     || !ArchiveMoveTargetGuard.IsWithin(target, doc.RelativeDirectory)
            ? target
            : doc.RelativeDirectory;
        var rootParts = relDir.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        return new ArchiveMovePlacement(key, typeId, typeName, partId, levelId,
            rootParts.Length > 0 ? rootParts[0] : "", relDir);
    }

    private static ArchiveMovePlacement CurrentPlacement(PlanDocument doc) =>
        new(doc.DocumentKey, doc.DocumentTypeId, doc.DocumentType, doc.BuildingPartId,
            doc.BuildingLevelId, doc.TargetFolder, doc.RelativeDirectory);

    /// <summary>
    /// Fachlicher DB-Apply (laeuft IMMER in einer Transaction, auch aus der Recovery):
    /// Dateipfade, Ablage + Klassifikation, Event, Actions completed, Journal 'moved'.
    /// </summary>
    private void ApplyToDatabase(string importId, PlanDocument doc, string revisionId,
        IReadOnlyList<(string ActionId, string FileId, string TargetRelPath)> moves,
        ArchiveMovePlacement placement)
    {
        foreach (var (actionId, fileId, targetRelPath) in moves)
        {
            _db.UpdateFilePath(fileId, targetRelPath);
            _db.CompleteImportAction(actionId, true);
        }
        _db.UpdateDocumentPlacement(doc.Id, placement);
        var targetDir = _path.GetDirectoryName(moves[0].TargetRelPath) ?? "";
        _db.InsertRevisionEvent(revisionId, importId, PlanArchive.EventType.ManualOverride,
            placement.DocumentKey == doc.DocumentKey
                ? $"Verschoben nach {targetDir}"
                : $"Umklassifiziert ({placement.DocumentType}) und verschoben nach {targetDir}");
        _db.MarkJournalMoved(importId);
    }

    /// <summary>
    /// Abbruch nach Journal-Anlage: bereits verschobene Dateien sofort zurueck.
    /// Gelingt das, ist der Move sauber verworfen (failed terminal, P.6); sonst
    /// bleibt das Journal 'pending' und die Recovery entscheidet.
    /// </summary>
    private MoveResult AbortMove(string importId, string projectRootPath, string error)
    {
        var rollbackErrors = RevertDisk(importId, projectRootPath);
        if (rollbackErrors.Count == 0)
        {
            foreach (var action in _db.GetImportActions(importId))
                _db.CompleteImportAction(action.Id, false, "rolled back: " + error);
            _db.CompleteImportJournal(importId, success: false,
                "Archiv-Move abgebrochen und zurueckgesetzt: " + error);
            return Fail(error);
        }

        _db.SetImportJournalError(importId,
            "Archiv-Move unvollstaendig — Recovery erforderlich: " + error);
        return Fail($"{error} — Recovery erforderlich");
    }

    private void MoveViaTmp(string sourceAbs, string targetAbs)
    {
        var tmpAbs = targetAbs + ImportExecutionService.TmpSuffix;
        ImportExecutionService.WithLockRetry(() => _writer.MoveFile(sourceAbs, tmpAbs, overwrite: true));
        ImportExecutionService.WithLockRetry(() => _writer.MoveFile(tmpAbs, targetAbs, overwrite: false));
    }

    private static MoveResult Fail(string error) => new(false, 0, error);

    /// <summary>Geplanter Einzel-Move einer Revisionsdatei.</summary>
    private sealed class PlannedMove(PlanRevisionFile file, string targetRelPath)
    {
        public PlanRevisionFile File { get; } = file;
        public string TargetRelPath { get; } = targetRelPath;
        public string? ActionId { get; set; }

        /// <summary>Datei liegt schon am Ziel (reine Umklassifizierung ohne Ordnerwechsel).</summary>
        public bool IsInPlace => ArchiveMoveTargetGuard.SamePath(File.RelativePath, TargetRelPath);
    }
}

/// <summary>Ergebnis eines Archiv-Moves.</summary>
public sealed record MoveResult(bool Success, int MovedFiles, string? Error);

/// <summary>
/// Ziel-Klassifikation eines Radial-Moves (ADR-064-Addendum P.5): Dokumenttyp,
/// Bauteil bzw. Kategorie, Geschoss — Stammdaten-IDs aus der bpm.db.
/// </summary>
public sealed record ArchiveReclassification(
    string DocumentTypeId,
    string DocumentTypeName,
    string? BuildingPartId,
    string? CategoryId,
    string? BuildingLevelId);
