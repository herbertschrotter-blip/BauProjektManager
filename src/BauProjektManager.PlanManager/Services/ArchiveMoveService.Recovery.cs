using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Recovery eines unterbrochenen Archiv-Moves (R01-008, ADR-064 P.1/P.6): eigene
/// Semantik statt des Import-Zweigs — der DB-Apply schreibt alle Pfade und die
/// Klassifikation in EINER Transaction, Forward und Rollback sind idempotent aus
/// jedem Zwischenzustand (Quelle / .bpm_tmp / Ziel).
/// </summary>
public partial class ArchiveMoveService
{
    /// <summary>
    /// Forward: fehlende Disk-Schritte nachholen, dann den gemeinsamen DB-Apply.
    /// Erfolg → Journal 'moved'; Konflikt → Journal bleibt 'pending' (retry-faehig).
    /// </summary>
    public RecoveryResult RecoverForward(string importId, string projectRootPath)
    {
        var actions = _db.GetImportActions(importId)
            .Where(a => a.ActionType == PlanManagerDatabase.MovedActionType)
            .ToList();
        var errors = new List<string>();
        foreach (var action in actions)
        {
            var error = ForwardDisk(action, projectRootPath);
            if (error is not null)
            {
                _db.CompleteImportAction(action.Id, false, error);
                errors.Add($"{action.SourcePath}: {error}");
            }
        }

        if (errors.Count == 0)
        {
            var dbError = TryApplyRecoveredMove(importId, actions);
            if (dbError is not null)
                errors.Add(dbError);
        }

        if (errors.Count > 0)
            _db.SetImportJournalError(importId,
                $"Recovery Forward (Archiv-Move): {errors.Count} Konflikt(e) — erneute Recovery erforderlich");
        Log.Information("Archiv-Move-Recovery Forward {Id}: {Errors} Fehler", importId, errors.Count);
        return new RecoveryResult(RecoveryAction.Forward, importId,
            errors.Count == 0 ? actions.Count : 0, errors.Count, errors);
    }

    /// <summary>
    /// Rollback: Dateien zurueck an die Quelle. Die DB ist vor dem Abschluss nie
    /// geschrieben worden (ein Apply endet atomar mit Journal 'moved'), daher genuegt der Disk-Reverse.
    /// </summary>
    public RecoveryResult RollbackMove(string importId, string projectRootPath)
    {
        var errors = RevertDisk(importId, projectRootPath);
        var actions = _db.GetImportActions(importId);
        if (errors.Count == 0)
        {
            _db.ExecuteInTransaction(() =>
            {
                foreach (var action in actions)
                    _db.CompleteImportAction(action.Id, false, "rolled back");
                _db.CompleteImportJournal(importId, success: false,
                    "Recovery Rollback (Archiv-Move) erfolgreich.");
            });
        }
        else
            _db.SetImportJournalError(importId,
                $"Recovery Rollback (Archiv-Move): {errors.Count} Datei(en) nicht zurueckgesetzt — Vorgang bleibt recovery-pflichtig");
        return new RecoveryResult(RecoveryAction.Rollback, importId,
            errors.Count == 0 ? actions.Count : 0, errors.Count, errors);
    }

    /// <summary>Disk-Forward einer Action (idempotent). Null bei Erfolg, sonst Konflikttext.</summary>
    private string? ForwardDisk(ImportActionRow action, string projectRootPath)
    {
        try
        {
            var sourceAbs = _path.Combine(projectRootPath, action.SourcePath);
            var targetAbs = _path.Combine(projectRootPath, action.DestinationPath!);
            var tmpAbs = targetAbs + ImportExecutionService.TmpSuffix;
            if (ArchiveMoveTargetGuard.SamePath(action.SourcePath, action.DestinationPath!))
                return null;

            if (_reader.FileExists(sourceAbs))
            {
                if (_reader.FileExists(targetAbs))
                    return $"RecoveryConflict: Datei liegt an Quelle UND Ziel ({action.DestinationPath})";
                _writer.CreateDirectory(_path.GetDirectoryName(targetAbs)!);
                MoveViaTmp(sourceAbs, targetAbs);
            }
            else if (_reader.FileExists(tmpAbs))
                ImportExecutionService.WithLockRetry(() => _writer.MoveFile(tmpAbs, targetAbs, overwrite: false));
            else if (!_reader.FileExists(targetAbs))
                return $"RecoveryConflict: Datei weder an Quelle noch am Ziel ({action.SourcePath})";
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>
    /// Gemeinsamer DB-Apply aus dem Journal: Dokument ueber den Dateipfad finden,
    /// Ablage-Regel wie beim normalen Move, alles in einer Transaction.
    /// </summary>
    private string? TryApplyRecoveredMove(string importId, List<ImportActionRow> actions)
    {
        try
        {
            var docId = actions
                .Select(a => _db.FindDocumentIdByFilePath(a.SourcePath)
                             ?? _db.FindDocumentIdByFilePath(a.DestinationPath!))
                .FirstOrDefault(id => id is not null);
            var doc = docId is null ? null : _db.GetDocumentById(docId);
            var revision = doc is null ? null : _db.GetCurrentRevisionForDocument(doc.Id);
            if (doc is null || revision is null)
                return "RecoveryConflict: verschobenes Dokument nicht mehr im Bestand";

            var files = _db.GetFilesForRevision(revision.Id);
            var mapped = new List<(string ActionId, string FileId, string TargetRelPath)>();
            foreach (var action in actions)
            {
                var file = files.FirstOrDefault(f =>
                    ArchiveMoveTargetGuard.SamePath(f.RelativePath, action.SourcePath)
                    || ArchiveMoveTargetGuard.SamePath(f.RelativePath, action.DestinationPath!));
                if (file is null)
                    return $"RecoveryConflict: Datei nicht an der current-Revision ({action.SourcePath})";
                mapped.Add((action.Id, file.FileId, action.DestinationPath!));
            }

            var first = actions[0];
            var target = _path.GetDirectoryName(first.DestinationPath!) ?? "";
            var placement = ResolvePlacement(doc, target, first.DocumentKey ?? doc.DocumentKey,
                first.DocumentTypeId ?? doc.DocumentTypeId, first.DocumentType ?? doc.DocumentType,
                first.BuildingPartId, first.BuildingLevelId);
            _db.ExecuteInTransaction(() => ApplyToDatabase(importId, doc, revision.Id, mapped, placement));
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Archiv-Move-Recovery: DB-Apply fehlgeschlagen ({Id})", importId);
            return ex.Message;
        }
    }

    /// <summary>
    /// Disk-Reverse aller Move-Actions (LIFO, idempotent): Ziel bzw. .bpm_tmp zurueck
    /// an die Quelle. Liefert die Fehler; leer = alles wieder an der Quelle.
    /// </summary>
    private List<string> RevertDisk(string importId, string projectRootPath)
    {
        var errors = new List<string>();
        var actions = _db.GetImportActions(importId);
        actions.Reverse();
        foreach (var action in actions.Where(a => a.DestinationPath is not null
                     && !ArchiveMoveTargetGuard.SamePath(a.SourcePath, a.DestinationPath)))
        {
            try
            {
                var sourceAbs = _path.Combine(projectRootPath, action.SourcePath);
                var targetAbs = _path.Combine(projectRootPath, action.DestinationPath!);
                var tmpAbs = targetAbs + ImportExecutionService.TmpSuffix;
                if (_reader.FileExists(sourceAbs))
                    continue;
                var from = _reader.FileExists(targetAbs) ? targetAbs
                    : _reader.FileExists(tmpAbs) ? tmpAbs : null;
                if (from is null)
                {
                    errors.Add($"{action.SourcePath}: Datei weder am Ziel noch an der Quelle");
                    continue;
                }
                ImportExecutionService.WithLockRetry(() => _writer.MoveFile(from, sourceAbs, overwrite: false));
            }
            catch (Exception ex)
            {
                errors.Add($"{action.SourcePath}: {ex.Message}");
            }
        }
        return errors;
    }
}
