using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Führt Recovery-Aktionen für einen pending Import aus (Disk + DB).
///
/// Drei Modi:
/// - <see cref="ExecuteForward"/>: pending Aktionen abarbeiten, Journal abschließen
/// - <see cref="ExecuteRollback"/>: bereits ausgeführte Aktionen rückwärts machen, Journal als 'failed'
/// - <see cref="ExecuteCleanup"/>: nur Journal als 'failed' setzen, Disk unangetastet
///
/// Siehe BPM-016 / 016.03. Empfehlung welche Aktion sinnvoll ist:
/// <see cref="RecoveryDecisionService"/>. Aufruf-Trigger: Recovery-Dialog (016.04).
/// </summary>
public class RecoveryExecutorService
{
    private readonly PlanManagerDatabase _db;
    private readonly IFileSystemReader _reader;
    private readonly IFileSystemWriter _writer;
    private readonly IPathService _path;

    public RecoveryExecutorService(
        PlanManagerDatabase db,
        IFileSystemReader reader, IFileSystemWriter writer, IPathService path)
    {
        _db = db;
        _reader = reader;
        _writer = writer;
        _path = path;
    }

    /// <summary>
    /// Forward: alle pending Aktionen abarbeiten (Datei verschieben), Journal als 'completed' setzen.
    /// Wenn eine Aktion fehlschlägt, wird sie als 'failed' markiert und das Journal als 'failed'
    /// abgeschlossen mit Fehler-Liste.
    /// </summary>
    public RecoveryResult ExecuteForward(string importId, string projectRootPath)
    {
        Log.Information("Recovery Forward gestartet fuer Import {Id}", importId);
        var pending = _db.GetImportActions(importId, statusFilter: "pending");

        int processed = 0;
        int failed = 0;
        var errors = new List<string>();

        foreach (var action in pending)
        {
            var error = TryMoveSourceToDestination(action, projectRootPath);
            if (error is null)
            {
                _db.CompleteImportAction(action.Id, success: true);
                processed++;
            }
            else
            {
                _db.CompleteImportAction(action.Id, success: false, errorMessage: error);
                failed++;
                errors.Add($"{action.SourcePath}: {error}");
            }
        }

        var journalSuccess = failed == 0;
        var journalError = failed > 0 ? $"Recovery Forward: {failed} Aktion(en) fehlgeschlagen." : null;
        _db.CompleteImportJournal(importId, journalSuccess, journalError);

        Log.Information("Recovery Forward fertig: {OK} ok, {Fail} fehler", processed, failed);
        return new RecoveryResult(RecoveryAction.Forward, importId, processed, failed, errors);
    }

    /// <summary>
    /// Rollback: alle completed Aktionen rückwärts (destination → source), bei archive_path
    /// die archivierte Datei zurück nach destination. Journal als 'failed' setzen mit
    /// Rollback-Notiz. Pending Aktionen werden ebenfalls als 'failed' markiert.
    /// </summary>
    public RecoveryResult ExecuteRollback(string importId, string projectRootPath)
    {
        Log.Information("Recovery Rollback gestartet fuer Import {Id}", importId);

        // Completed-Actions rückwärts abarbeiten (LIFO — letzte zuerst rückgängig)
        var completed = _db.GetImportActions(importId, statusFilter: "completed");
        completed.Reverse();

        int processed = 0;
        int failed = 0;
        var errors = new List<string>();

        foreach (var action in completed)
        {
            var error = TryRollbackSingle(action, projectRootPath);
            if (error is null)
            {
                _db.CompleteImportAction(action.Id, success: false, errorMessage: "rolled back");
                processed++;
            }
            else
            {
                failed++;
                errors.Add($"{action.DestinationPath}: {error}");
                Log.Warning("Rollback fehlgeschlagen fuer Action {Id}: {Error}", action.Id, error);
            }
        }

        // Pending Aktionen ebenfalls als failed markieren (sie wurden nie ausgeführt → einfach Status setzen)
        var pending = _db.GetImportActions(importId, statusFilter: "pending");
        foreach (var action in pending)
            _db.CompleteImportAction(action.Id, success: false, errorMessage: "cancelled by rollback");

        var journalError = failed > 0
            ? $"Recovery Rollback: {failed} Aktion(en) konnten nicht zurueckgesetzt werden."
            : "Recovery Rollback erfolgreich.";
        _db.CompleteImportJournal(importId, success: false, errorMessage: journalError);

        Log.Information("Recovery Rollback fertig: {OK} ok, {Fail} fehler", processed, failed);
        return new RecoveryResult(RecoveryAction.Rollback, importId, processed, failed, errors);
    }

    /// <summary>
    /// Cleanup: keine Disk-Operation, nur Journal als 'failed' setzen mit Begruendung.
    /// Pending und completed Aktionen werden ebenfalls als 'failed' markiert (Status-Konsistenz).
    /// </summary>
    public RecoveryResult ExecuteCleanup(string importId, string reason)
    {
        Log.Information("Recovery Cleanup gestartet fuer Import {Id}: {Reason}", importId, reason);

        var allActions = _db.GetImportActions(importId);
        foreach (var action in allActions)
        {
            if (action.ActionStatus == "pending")
                _db.CompleteImportAction(action.Id, success: false, errorMessage: "cleanup: " + reason);
        }

        _db.CompleteImportJournal(importId, success: false, errorMessage: "Cleanup: " + reason);

        Log.Information("Recovery Cleanup fertig fuer Import {Id}", importId);
        return new RecoveryResult(RecoveryAction.Cleanup, importId, 0, 0, []);
    }

    // === Disk-Helpers ===

    /// <summary>
    /// Forward-Move: Source → Destination. Bei archive_path: existing destination zuerst dorthin
    /// archivieren. Returns null bei Erfolg, Fehler-Text bei Problem.
    /// </summary>
    private string? TryMoveSourceToDestination(ImportActionRow action, string projectRootPath)
    {
        try
        {
            var sourceAbs = _path.Combine(projectRootPath, action.SourcePath);
            var destAbs = _path.Combine(projectRootPath, action.DestinationPath);

            if (!_reader.FileExists(sourceAbs))
                return $"Source-Datei nicht mehr da: {action.SourcePath}";

            // Archive vorhandene Destination falls verlangt
            if (!string.IsNullOrEmpty(action.ArchivePath) && _reader.FileExists(destAbs))
            {
                var archiveAbs = _path.Combine(projectRootPath, action.ArchivePath);
                _writer.CreateDirectory(_path.GetDirectoryName(archiveAbs)!);
                _writer.MoveFile(destAbs, archiveAbs, overwrite: false);
            }

            _writer.CreateDirectory(_path.GetDirectoryName(destAbs)!);
            _writer.MoveFile(sourceAbs, destAbs, overwrite: false);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>
    /// Rollback-Move: Destination → Source. Bei archive_path: archivierte Datei zurück nach Destination.
    /// Returns null bei Erfolg, Fehler-Text bei Problem.
    /// </summary>
    private string? TryRollbackSingle(ImportActionRow action, string projectRootPath)
    {
        try
        {
            var sourceAbs = _path.Combine(projectRootPath, action.SourcePath);
            var destAbs = _path.Combine(projectRootPath, action.DestinationPath);

            if (!_reader.FileExists(destAbs))
                return $"Destination-Datei nicht mehr da: {action.DestinationPath}";

            // Erst Datei zurück nach source
            _writer.CreateDirectory(_path.GetDirectoryName(sourceAbs)!);
            _writer.MoveFile(destAbs, sourceAbs, overwrite: false);

            // Archive-File zurück an destination
            if (!string.IsNullOrEmpty(action.ArchivePath))
            {
                var archiveAbs = _path.Combine(projectRootPath, action.ArchivePath);
                if (_reader.FileExists(archiveAbs))
                    _writer.MoveFile(archiveAbs, destAbs, overwrite: false);
            }

            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
