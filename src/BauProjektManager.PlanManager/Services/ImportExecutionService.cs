using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Executes the import plan: moves files, creates _Archiv/, writes journal.
/// Invariant: Journal is written BEFORE files are moved (pending → completed/failed).
/// BPM-120 T1 (ADR-060/064): alle Datei-/Pfadoperationen laufen ueber die
/// FS-Ports — testbar mit fault-faehigem FakeFileStore.
/// </summary>
public class ImportExecutionService
{
    private static readonly IPlanValueNormalizer _normalizer = new PlanValueNormalizer();
    private readonly PlanManagerDatabase _db;
    private readonly IIdGenerator _idGenerator;
    private readonly IFileSystemReader _reader;
    private readonly IFileSystemWriter _writer;
    private readonly IPathService _path;

    public ImportExecutionService(
        PlanManagerDatabase db, IIdGenerator idGenerator,
        IFileSystemReader reader, IFileSystemWriter writer, IPathService path)
    {
        _db = db;
        _idGenerator = idGenerator;
        _reader = reader;
        _writer = writer;
        _path = path;
    }

    /// <summary>
    /// Executes the import: journal first, then move files, then update DB.
    /// BPM-120 T2 (ADR-064 P.2): ZWEIPHASIG — Phase 1 plant alle Actions
    /// (inkl. deterministischem archive_path) und journalisiert Header + ALLE
    /// Actions vor der ersten Mutation; Phase 2 fuehrt aus. Bestaetigte
    /// MD5-Dubletten laufen als echte skipDuplicate-Actions (Bucket A, P.7) —
    /// journalisiert + recovery-faehig, bewusst NICHT undo-bar.
    /// </summary>
    public ImportExecutionResult Execute(
        List<ImportDecision> decisions,
        string projectRootPath,
        string inboxRelativePath)
    {
        var actionable = decisions
            .Where(d => d.Status is ImportStatus.New or ImportStatus.UpdateNewerIndex
                or ImportStatus.ChangedNoIndex or ImportStatus.LearnIndex)
            .ToList();
        var duplicates = decisions
            .Where(d => d.Status == ImportStatus.SkipIdentical)
            .ToList();

        int succeeded = 0;
        int failed = 0;
        int skipped = 0;
        var errors = new List<string>();

        // Planbarkeits-Check VOR der Journalisierung: ohne Zielpfad keine Action
        // (zaehlt wie bisher als Fehler, mutiert nichts, journalisiert nichts).
        var plannable = new List<ImportDecision>();
        foreach (var decision in actionable)
        {
            if (string.IsNullOrEmpty(decision.TargetRelativePath))
            {
                failed++;
                errors.Add($"{decision.File.Parsed.FileName}: Kein Zielpfad berechnet");
                continue;
            }
            plannable.Add(decision);
        }

        if (plannable.Count == 0 && duplicates.Count == 0)
        {
            Log.Information("Import: keine Aktionen auszuführen");
            return new ImportExecutionResult(0, failed, 0, errors);
        }

        Log.Information("Import-Ausführung: {Count} Aktionen, {Skips} Dubletten",
            plannable.Count, duplicates.Count);

        // ── Phase 1: Journal-Header + ALLE Actions VOR der ersten Mutation (AK 4) ──
        var importId = _db.CreateImportJournal(
            inboxRelativePath, plannable.Count + duplicates.Count, profileId: null);

        var planned = new List<(ImportDecision Decision, string ActionId, string? ArchiveRelPath)>();
        var order = 0;
        foreach (var decision in plannable)
        {
            var targetRelPath = decision.TargetRelativePath!;
            var actionType = decision.Status switch
            {
                ImportStatus.New => "new",
                ImportStatus.UpdateNewerIndex => "indexUpdate",
                ImportStatus.ChangedNoIndex => "changed",
                ImportStatus.LearnIndex => "learnIndex",
                _ => "unknown"
            };

            // Deterministischer Archivpfad (AK 5): VOR der ersten Mutation
            // festgelegt und journalisiert — kein ad-hoc-Name bei der Ausfuehrung.
            string? archiveRelPath = null;
            if (decision.Status == ImportStatus.UpdateNewerIndex
                && _reader.FileExists(_path.Combine(projectRootPath, targetRelPath)))
                archiveRelPath = BuildArchiveRelPath(targetRelPath);

            var actionId = _db.InsertImportAction(
                importId, order++, actionType,
                decision.DocumentKey,
                decision.File.PlanNumber ?? "",
                decision.File.RevisionToken,
                oldIndex: null,
                decision.File.Parsed.RelativePath,
                targetRelPath,
                archiveRelPath,
                decision.File.Parsed.Md5,
                decision.File.Parsed.FileSize);
            planned.Add((decision, actionId, archiveRelPath));
        }

        var plannedSkips = new List<(ImportDecision Decision, string ActionId)>();
        foreach (var dup in duplicates)
        {
            var actionId = _db.InsertImportAction(
                importId, order++, "skipDuplicate",
                dup.DocumentKey,
                dup.File.PlanNumber ?? "",
                dup.File.RevisionToken,
                oldIndex: null,
                dup.File.Parsed.RelativePath,
                destinationPath: null,
                archivePath: null,
                dup.File.Parsed.Md5,
                dup.File.Parsed.FileSize);
            plannedSkips.Add((dup, actionId));
        }

        // ── Phase 2: Ausfuehrung ──
        foreach (var (decision, actionId, archiveRelPath) in planned)
        {
            var actionResult = ExecuteSingleAction(
                decision, projectRootPath, importId, actionId, archiveRelPath);
            if (actionResult.Success)
                succeeded++;
            else
            {
                failed++;
                errors.Add($"{decision.File.Parsed.FileName}: {actionResult.Error}");
            }
        }

        foreach (var (dup, actionId) in plannedSkips)
        {
            var skipResult = ExecuteSkipDuplicate(dup, projectRootPath, actionId);
            if (skipResult.Success)
                skipped++;
            else
            {
                failed++;
                errors.Add($"{dup.File.Parsed.FileName}: {skipResult.Error}");
            }
        }

        // Journal abschliessen
        _db.CompleteImportJournal(importId, failed == 0,
            failed > 0 ? $"{failed} Aktionen fehlgeschlagen" : null);

        Log.Information("Import abgeschlossen: {Ok} OK, {Fail} Fehler, {Skip} übersprungen",
            succeeded, failed, skipped);

        return new ImportExecutionResult(succeeded, failed, skipped, errors);
    }

    private ActionResult ExecuteSingleAction(
        ImportDecision decision, string projectRootPath,
        string importId, string actionId, string? archiveRelPath)
    {
        var sourcePath = _path.Combine(projectRootPath, decision.File.Parsed.RelativePath);
        var targetRelPath = decision.TargetRelativePath!;
        var targetPath = _path.Combine(projectRootPath, targetRelPath);

        try
        {
            // Create target directory
            var targetDir = _path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir))
                _writer.CreateDirectory(targetDir);

            // --- EIN Zeitstempel pro Aktion (Zeitreise-Konsistenz, BPM-109.04):
            //     superseded_at der alten == current_from der neuen → kein Loch, keine Überlappung. ---
            var actionTime = DateTime.UtcNow.ToString("o");

            // --- Supersede + Archiv VOR dem Move (Parität zur bisherigen Reihenfolge) ---
            // Schema v2.0: bei Index-Update alte current-Revision auf superseded setzen + Event.
            if (decision.Status == ImportStatus.UpdateNewerIndex)
            {
                // T2/AK 5: Archiv-Ziel kommt aus dem Journal (Planung), nie ad hoc.
                if (archiveRelPath is not null)
                    ArchiveExistingFile(targetPath, _path.Combine(projectRootPath, archiveRelPath));
                var existingDoc = _db.GetDocumentByKey(decision.DocumentKey!);
                if (existingDoc is not null)
                {
                    var oldCurrent = _db.GetCurrentRevisionForDocument(existingDoc.Id);
                    // 111.07 Slice A2: Zweite Datei desselben Dokuments im SELBEN
                    // Import (PDF+DWG-Paar) darf die gerade angelegte Revision
                    // nicht gleich wieder ablösen — sie dockt unten als
                    // Zusatzdatei an (FileLinked-Zweig).
                    if (oldCurrent is not null && oldCurrent.LastImportId != importId)
                    {
                        _db.SupersedeCurrentRevision(existingDoc.Id, actionTime);
                        _db.InsertRevisionEvent(oldCurrent.Id, importId,
                            PlanArchive.EventType.Superseded, "Durch neue Revision ersetzt");
                    }
                }
            }

            // Move file from inbox to target
            if (_reader.FileExists(sourcePath))
            {
                _writer.MoveFile(sourcePath, targetPath, overwrite: true);
                Log.Information("Datei verschoben: {Source} → {Target}",
                    decision.File.Parsed.FileName, targetRelPath);
            }

            // --- Cache-DB-Write NACH dem Move (Schema v2.0 Drei-Ebenen-Modell, BPM-109.03/.04) ---
            // ADR-061 Slice 0.6c: target_folder kommt aus dem aufgeloesten Zielpfad
            // (Root-Segment = root_relative_path des Dokumenttyps), NICHT mehr aus dem
            // entfernten profile.TargetFolder. relative_directory = voller aufgeloester Ordner.
            var resolvedDir = _path.GetDirectoryName(targetRelPath) ?? "";
            var rootParts = resolvedDir.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
            var rootFolder = rootParts.Length > 0 ? rootParts[0] : "";

            var documentId = _db.ResolveOrCreateDocument(
                _db.ProjectId,
                decision.DocumentKey!,
                decision.File.DocumentTypeId ?? "",
                decision.File.PlanNumber ?? "",
                decision.File.DocumentTypeDisplayName ?? "unknown",
                decision.File.Title ?? "",                   // title (Panel-Bezeichnung, Slice A3)
                rootFolder,                                  // target_folder = root_relative_path (ADR-061)
                resolvedDir,                                 // relative_directory (voller aufgeloester Ordner)
                null,                                        // building_part_id — SoftRef-Auflösung post-V1 (BPM-109.06)
                null);                                       // building_level_id — dito

            var currentRev = _db.GetCurrentRevisionForDocument(documentId);
            if (currentRev is not null)
            {
                // Zusatzdatei zur bestehenden current-Revision (ChangedNoIndex/LearnIndex, z.B. DWG nach PDF)
                _db.InsertFileForRevision(currentRev.Id,
                    decision.File.Parsed.FileName, targetRelPath,
                    decision.File.Parsed.Extension,
                    decision.File.Parsed.Md5, decision.File.Parsed.FileSize,
                    isPrimary: false);
                _db.InsertRevisionEvent(currentRev.Id, importId,
                    PlanArchive.EventType.FileLinked, decision.File.Parsed.FileName);
            }
            else
            {
                // Neue current-Revision (New oder nach Supersede bei UpdateNewerIndex)
                var revisionId = _db.InsertRevision(
                    documentId,
                    decision.File.RevisionToken,                 // plan_index
                    decision.File.RevisionSource.ToString(),     // index_source
                    PlanArchive.Status.Current,
                    actionTime,                                  // current_from == actionTime
                    null,                                        // superseded_at
                    actionTime,                                  // received_at
                    importId,                                    // last_import_id
                    decision.File.ReleasedAt,                    // released_at (BPM-118 Text-Zuweisung)
                    decision.File.ChangeNote ?? "");             // change_note (BPM-118 Text-Zuweisung)
                _db.InsertRevisionEvent(revisionId, importId,
                    PlanArchive.EventType.Created, "Neue Revision angelegt");
                _db.InsertFileForRevision(revisionId,
                    decision.File.Parsed.FileName, targetRelPath,
                    decision.File.Parsed.Extension,
                    decision.File.Parsed.Md5, decision.File.Parsed.FileSize,
                    isPrimary: true);
            }

            // Vorgemerkte Segmentwerte (BPM-118 Text-Zuweisung) — haengen am
            // Dokument, nicht an der Revision. Upsert: UNIQUE(document_id,
            // segment_type_id), die letzte User-Zuweisung gewinnt.
            foreach (var seg in decision.File.AssignedSegments ?? [])
                _db.UpsertSegment(documentId, seg.SegmentTypeId, seg.TokenKey,
                    seg.Value, _normalizer.NormalizeForMatch(seg.Value));

            _db.CompleteImportAction(actionId, true);
            return new ActionResult(true, null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Import-Aktion fehlgeschlagen: {File}", decision.File.Parsed.FileName);
            _db.CompleteImportAction(actionId, false, ex.Message);
            return new ActionResult(false, ex.Message);
        }
    }

    /// <summary>
    /// Bucket A (BPM-120 T2, ADR-064 P.7): bestätigte MD5-Dublette — Quelldatei
    /// aus dem Eingang löschen. Journalisiert + recovery-fähig, bewusst NICHT
    /// undo-bar (Inhalt liegt MD5-identisch im Bestand, kein Papierkorb).
    /// </summary>
    private ActionResult ExecuteSkipDuplicate(
        ImportDecision decision, string projectRootPath, string actionId)
    {
        try
        {
            var sourcePath = _path.Combine(projectRootPath, decision.File.Parsed.RelativePath);
            if (_reader.FileExists(sourcePath))
                _writer.DeleteFile(sourcePath);
            _db.CompleteImportAction(actionId, true);
            Log.Debug("Dublette aus Eingang entfernt: {File}", decision.File.Parsed.FileName);
            return new ActionResult(true, null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "skipDuplicate fehlgeschlagen: {File}", decision.File.Parsed.FileName);
            _db.CompleteImportAction(actionId, false, ex.Message);
            return new ActionResult(false, ex.Message);
        }
    }

    /// <summary>
    /// Deterministischer Archivpfad relativ zum Projekt-Root (BPM-120 T2, AK 5):
    /// _Archiv-Unterordner neben dem Ziel, Name + Zeitstempel der PLANUNG.
    /// Recovery und Undo verwenden exakt diesen journalisierten Pfad.
    /// </summary>
    private string BuildArchiveRelPath(string targetRelPath)
    {
        var dir = _path.GetDirectoryName(targetRelPath) ?? "";
        var fileName = _path.GetFileName(targetRelPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var archiveName = $"{_path.GetFileNameWithoutExtension(fileName)}_{timestamp}{_path.GetExtension(fileName)}";
        return _path.Combine(dir, "_Archiv", archiveName);
    }

    /// <summary>
    /// Verschiebt die vorhandene Zieldatei an den journalisierten Archivpfad.
    /// No-Op wenn die Zieldatei nicht (mehr) existiert.
    /// </summary>
    private void ArchiveExistingFile(string targetPath, string archiveAbsPath)
    {
        if (!_reader.FileExists(targetPath))
            return;

        _writer.CreateDirectory(_path.GetDirectoryName(archiveAbsPath)!);
        _writer.MoveFile(targetPath, archiveAbsPath);
        Log.Information("Datei archiviert: {Source} → {Archive}",
            _path.GetFileName(targetPath), _path.GetFileName(archiveAbsPath));
    }

    private sealed record ActionResult(bool Success, string? Error);
}

/// <summary>
/// Summary of a completed import execution.
/// </summary>
public sealed record ImportExecutionResult(
    int Succeeded,
    int Failed,
    int Skipped,
    List<string> Errors);
