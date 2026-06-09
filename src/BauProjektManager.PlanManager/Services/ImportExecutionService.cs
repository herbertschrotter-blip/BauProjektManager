using System.IO;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Executes the import plan: moves files, creates _Archiv/, writes journal.
/// Invariant: Journal is written BEFORE files are moved (pending → completed/failed).
/// </summary>
public class ImportExecutionService
{
    private readonly PlanManagerDatabase _db;
    private readonly IIdGenerator _idGenerator;

    public ImportExecutionService(PlanManagerDatabase db, IIdGenerator idGenerator)
    {
        _db = db;
        _idGenerator = idGenerator;
    }

    /// <summary>
    /// Executes the import: journal first, then move files, then update DB.
    /// Returns summary of what happened.
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

        if (actionable.Count == 0)
        {
            Log.Information("Import: keine Aktionen auszuführen");
            return new ImportExecutionResult(0, 0, 0, []);
        }

        Log.Information("Import-Ausführung: {Count} Aktionen", actionable.Count);

        // 1. Create journal entry (pending)
        var importId = _db.CreateImportJournal(
            inboxRelativePath, actionable.Count, profileId: null);

        int succeeded = 0;
        int failed = 0;
        int skipped = 0;
        var errors = new List<string>();

        // 2. Execute each action
        for (int i = 0; i < actionable.Count; i++)
        {
            var decision = actionable[i];
            var actionResult = ExecuteSingleAction(
                decision, projectRootPath, importId, i);

            if (actionResult.Success)
                succeeded++;
            else
            {
                failed++;
                errors.Add($"{decision.File.Parsed.FileName}: {actionResult.Error}");
            }
        }

        // 3. Handle skipped (identical) — remove from inbox
        var identicals = decisions
            .Where(d => d.Status == ImportStatus.SkipIdentical)
            .ToList();
        foreach (var skip in identicals)
        {
            try
            {
                var sourcePath = Path.Combine(projectRootPath, skip.File.Parsed.RelativePath);
                if (File.Exists(sourcePath))
                {
                    File.Delete(sourcePath);
                    skipped++;
                    Log.Debug("Identische Datei aus Eingang entfernt: {File}", skip.File.Parsed.FileName);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Konnte identische Datei nicht entfernen: {File}", skip.File.Parsed.FileName);
            }
        }

        // 4. Complete journal
        _db.CompleteImportJournal(importId, failed == 0,
            failed > 0 ? $"{failed} Aktionen fehlgeschlagen" : null);

        Log.Information("Import abgeschlossen: {Ok} OK, {Fail} Fehler, {Skip} übersprungen",
            succeeded, failed, skipped);

        return new ImportExecutionResult(succeeded, failed, skipped, errors);
    }

    private ActionResult ExecuteSingleAction(
        ImportDecision decision, string projectRootPath,
        string importId, int actionOrder)
    {
        var sourcePath = Path.Combine(projectRootPath, decision.File.Parsed.RelativePath);
        var targetRelPath = decision.TargetRelativePath;

        if (string.IsNullOrEmpty(targetRelPath))
            return new ActionResult(false, "Kein Zielpfad berechnet");

        var targetPath = Path.Combine(projectRootPath, targetRelPath);
        var profile = decision.File.Parsed.MatchedProfile;

        // Write journal action (pending) BEFORE moving
        var actionType = decision.Status switch
        {
            ImportStatus.New => "new",
            ImportStatus.UpdateNewerIndex => "indexUpdate",
            ImportStatus.ChangedNoIndex => "changed",
            ImportStatus.LearnIndex => "learnIndex",
            _ => "unknown"
        };

        var actionId = _db.InsertImportAction(
            importId, actionOrder, actionType,
            decision.DocumentKey,
            decision.File.PlanNumber ?? "",
            decision.File.RevisionToken,
            oldIndex: null,
            decision.File.Parsed.RelativePath,
            targetRelPath,
            archivePath: null);

        try
        {
            // Create target directory
            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);

            // --- EIN Zeitstempel pro Aktion (Zeitreise-Konsistenz, BPM-109.04):
            //     superseded_at der alten == current_from der neuen → kein Loch, keine Überlappung. ---
            var actionTime = DateTime.UtcNow.ToString("o");

            // --- Supersede + Archiv VOR dem Move (Parität zur bisherigen Reihenfolge) ---
            // Schema v2.0: bei Index-Update alte current-Revision auf superseded setzen + Event.
            if (decision.Status == ImportStatus.UpdateNewerIndex)
            {
                ArchiveExistingFile(targetPath, projectRootPath);
                var existingDoc = _db.GetDocumentByKey(decision.DocumentKey!);
                if (existingDoc is not null)
                {
                    var oldCurrent = _db.GetCurrentRevisionForDocument(existingDoc.Id);
                    _db.SupersedeCurrentRevision(existingDoc.Id, actionTime);
                    if (oldCurrent is not null)
                        _db.InsertRevisionEvent(oldCurrent.Id, importId,
                            PlanArchive.EventType.Superseded, "Durch neue Revision ersetzt");
                }
            }

            // Move file from inbox to target
            if (File.Exists(sourcePath))
            {
                File.Move(sourcePath, targetPath, overwrite: true);
                Log.Information("Datei verschoben: {Source} → {Target}",
                    decision.File.Parsed.FileName, targetRelPath);
            }

            // --- Cache-DB-Write NACH dem Move (Schema v2.0 Drei-Ebenen-Modell, BPM-109.03/.04) ---
            var documentId = _db.ResolveOrCreateDocument(
                _db.ProjectId,
                decision.DocumentKey!,
                decision.File.DocumentTypeId ?? "",
                decision.File.PlanNumber ?? "",
                decision.File.DocumentTypeDisplayName ?? "unknown",
                "",                                          // title
                profile?.TargetFolder ?? "",
                Path.GetDirectoryName(targetRelPath) ?? "",
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
                    importId);                                   // last_import_id
                _db.InsertRevisionEvent(revisionId, importId,
                    PlanArchive.EventType.Created, "Neue Revision angelegt");
                _db.InsertFileForRevision(revisionId,
                    decision.File.Parsed.FileName, targetRelPath,
                    decision.File.Parsed.Extension,
                    decision.File.Parsed.Md5, decision.File.Parsed.FileSize,
                    isPrimary: true);
            }

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
    /// Archives an existing file by moving it to _Archiv/ subfolder.
    /// </summary>
    private static void ArchiveExistingFile(string targetPath, string projectRootPath)
    {
        if (!File.Exists(targetPath))
            return;

        var targetDir = Path.GetDirectoryName(targetPath)!;
        var archiveDir = Path.Combine(targetDir, "_Archiv");
        Directory.CreateDirectory(archiveDir);

        var fileName = Path.GetFileName(targetPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var archiveName = $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}{Path.GetExtension(fileName)}";
        var archivePath = Path.Combine(archiveDir, archiveName);

        File.Move(targetPath, archivePath);
        Log.Information("Datei archiviert: {Source} → {Archive}", fileName, archiveName);
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
