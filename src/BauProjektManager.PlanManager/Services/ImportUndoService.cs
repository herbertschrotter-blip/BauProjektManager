using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Undo Stufe 2 (BPM-111.04): macht den LETZTEN abgeschlossenen Import
/// rueckgaengig — Dateien zurueck in den Eingang, Archiv-Dateien zurueck ans
/// Ziel, DB-Revisionen zurueckgesetzt (Soft Delete + Supersede-Restore).
///
/// Invarianten (PlanManager.md Kap. 11 + ADR-064 P.5/P.7): NUR letzter Import,
/// IMMER Preflight-Trockenlauf vor der Ausfuehrung; skipDuplicate-Actions sind
/// journalisiert aber bewusst NICHT undo-bar (kein Papierkorb). DB-Rollback +
/// MarkImportUndone laufen NUR nach vollstaendig erfolgreichem Disk-Reverse,
/// in einer SQLite-Transaction (BPM-120 T7).
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

        // BPM-120 T2/AK 15: skipDuplicate ist bewusst NICHT undo-bar (kein
        // Papierkorb) — zaehlt nicht als undo-faehige Action. Ein reiner
        // skipDuplicate-Import wird damit gar nicht als undo-faehig angeboten.
        var actions = _db.GetImportActions(importId, statusFilter: "completed")
            .Where(a => a.ActionType != "skipDuplicate")
            .ToList();
        var conflicts = new List<UndoActionConflict>();

        foreach (var action in actions)
        {
            var destination = _path.Combine(projectRootPath, action.DestinationPath!);
            var source = _path.Combine(projectRootPath, action.SourcePath);
            var fileName = _path.GetFileName(action.DestinationPath!);

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
        // T2: skipDuplicate bleibt geloescht — gemischter Import darf trotzdem
        // 'undone' werden (ADR-064 P.7).
        var actions = _db.GetImportActions(importId, statusFilter: "completed")
            .Where(a => a.ActionType != "skipDuplicate")
            .ToList();
        var errors = new List<string>();
        var restored = 0;

        // 1. Dateioperationen rueckwaerts (letzte Aktion zuerst). BPM-120 T7:
        //    beim ERSTEN Fehler abbrechen — je weniger Teil-Reverse, desto
        //    kleiner die Disk/DB-Drift des reparierbaren Zwischenzustands.
        foreach (var action in actions.AsEnumerable().Reverse())
        {
            try
            {
                var destination = _path.Combine(projectRootPath, action.DestinationPath!);
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
                errors.Add($"{_path.GetFileName(action.DestinationPath!)}: {ex.Message}");
                break;
            }
        }

        // T7/AK 14 (ADR-064 P.5): Scheitert IRGENDEIN erforderlicher Disk-Reverse,
        // wird die DB NICHT angefasst — keine Revision soft-deleted/restored,
        // KEIN MarkImportUndone. Der Import bleibt 'completed' und der Vorgang
        // reparierbar (fruehere Fassung rollte hier bedingungslos zurueck —
        // Disk halb zurueck, DB komplett zurueck, Import "undone").
        if (errors.Count > 0)
        {
            Log.Warning("Undo abgebrochen: {Errors} Disk-Reverse-Fehler — DB bleibt unangetastet",
                errors.Count);
            return new UndoResult(false, importId, restored, errors, preflight);
        }

        // 2. DB-Rollback in EINER SQLite-Transaction inkl. 'undone' (T7/AK 14):
        //    angelegte Revisionen soft-deleten, Dokumente ohne Revisionen
        //    soft-deleten, superseded Revisionen restaurieren, Journal markieren.
        _db.ExecuteInTransaction(() =>
        {
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

            _db.MarkImportUndone(importId);
        });

        Log.Information("Undo abgeschlossen: Import {ImportId}, {Restored} Dateien zurueck, {Errors} Fehler",
            importId, restored, errors.Count);
        return new UndoResult(errors.Count == 0, importId, restored, errors, preflight);
    }
}
