using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Undo Stufe 2 (BPM-111.04): macht den LETZTEN abgeschlossenen Import
/// rueckgaengig — Dateien zurueck in den Eingang, Archiv-Dateien zurueck ans
/// Ziel, DB-Revisionen zurueckgesetzt (Soft Delete + Supersede-Restore).
///
/// Invarianten (PlanManager.md Kap. 11): NUR letzter Import, IMMER
/// Preflight-Trockenlauf vor der Ausfuehrung, SKIP-Aktionen sind nicht
/// undo-bar (werden gar nicht journaliert).
/// </summary>
public class ImportUndoService
{
    private readonly PlanManagerDatabase _db;
    private readonly IFileSystemReader _reader;
    private readonly IFileSystemWriter _writer;
    private readonly IPathService _path;

    public ImportUndoService(
        PlanManagerDatabase db,
        IFileSystemReader reader, IFileSystemWriter writer, IPathService path)
    {
        _db = db;
        _reader = reader;
        _writer = writer;
        _path = path;
    }

    /// <summary>
    /// Trockenlauf: prueft ob der letzte Import rueckgaengig gemacht werden
    /// kann (liegen alle Dateien noch dort, wo das Journal sie erwartet?).
    /// </summary>
    public UndoPreflightReport Preflight(string projectRootPath)
    {
        var importId = _db.GetLastCompletedImportId();
        if (importId is null)
            return new UndoPreflightReport(null, 0, []);

        var actions = _db.GetImportActions(importId, statusFilter: "completed");
        var conflicts = new List<UndoActionConflict>();

        foreach (var action in actions)
        {
            var destination = _path.Combine(projectRootPath, action.DestinationPath);
            var source = _path.Combine(projectRootPath, action.SourcePath);
            var fileName = _path.GetFileName(action.DestinationPath);

            if (!_reader.FileExists(destination))
                conflicts.Add(new UndoActionConflict(action.Id, fileName,
                    "Zieldatei wurde extern verschoben oder geloescht"));

            if (_reader.FileExists(source))
                conflicts.Add(new UndoActionConflict(action.Id, fileName,
                    "Eingangs-Pfad ist bereits wieder belegt"));

            if (action.ArchivePath is not null
                && !_reader.FileExists(_path.Combine(projectRootPath, action.ArchivePath)))
                conflicts.Add(new UndoActionConflict(action.Id, fileName,
                    "Archivierte Vorgaenger-Datei fehlt"));
        }

        return new UndoPreflightReport(importId, actions.Count, conflicts);
    }

    /// <summary>
    /// Fuehrt das Undo des letzten Imports aus (nach erfolgreichem Preflight).
    /// Reihenfolge: Dateien rueckwaerts zurueck -> Archiv-Restore -> DB-Rollback
    /// -> Journal als 'undone' markieren.
    /// </summary>
    public UndoResult UndoLastImport(string projectRootPath)
    {
        var preflight = Preflight(projectRootPath);
        if (!preflight.CanUndo)
        {
            Log.Warning("Undo abgebrochen: {Conflicts} Konflikte im Preflight",
                preflight.Conflicts.Count);
            return new UndoResult(false, preflight.ImportId, 0,
                [.. preflight.Conflicts.Select(c => $"{c.FileName}: {c.Issue}")], preflight);
        }

        var importId = preflight.ImportId!;
        var actions = _db.GetImportActions(importId, statusFilter: "completed");
        var errors = new List<string>();
        var restored = 0;

        // 1. Dateioperationen rueckwaerts (letzte Aktion zuerst)
        foreach (var action in actions.AsEnumerable().Reverse())
        {
            try
            {
                var destination = _path.Combine(projectRootPath, action.DestinationPath);
                var source = _path.Combine(projectRootPath, action.SourcePath);

                var sourceDir = _path.GetDirectoryName(source);
                if (!string.IsNullOrEmpty(sourceDir))
                    _writer.CreateDirectory(sourceDir);

                _writer.MoveFile(destination, source);
                restored++;

                // Archivierte Vorgaenger-Revision zurueck an den Zielort
                if (action.ArchivePath is not null)
                {
                    var archive = _path.Combine(projectRootPath, action.ArchivePath);
                    _writer.MoveFile(archive, destination);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Undo-Dateioperation fehlgeschlagen: {Path}", action.DestinationPath);
                errors.Add($"{_path.GetFileName(action.DestinationPath)}: {ex.Message}");
            }
        }

        // 2. DB-Rollback: angelegte Revisionen soft-deleten, Dokumente ohne
        //    Revisionen soft-deleten, superseded Revisionen restaurieren
        var created = _db.GetRevisionsCreatedByImport(importId);
        foreach (var (revisionId, _) in created)
            _db.SoftDeleteRevision(revisionId);

        foreach (var revisionId in _db.GetRevisionIdsSupersededByImport(importId))
        {
            _db.RestoreRevisionToCurrent(revisionId);
            _db.InsertRevisionEvent(revisionId, importId,
                PlanArchive.EventType.MadeCurrent, "Undo letzter Import");
        }

        foreach (var documentId in created.Select(c => c.DocumentId).Distinct())
            _db.SoftDeleteDocumentIfNoRevisions(documentId);

        // 3. Journal abschliessen
        _db.MarkImportUndone(importId);

        Log.Information("Undo abgeschlossen: Import {ImportId}, {Restored} Dateien zurueck, {Errors} Fehler",
            importId, restored, errors.Count);
        return new UndoResult(errors.Count == 0, importId, restored, errors, preflight);
    }
}
