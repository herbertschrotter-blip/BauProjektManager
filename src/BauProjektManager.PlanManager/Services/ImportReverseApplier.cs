using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Gemeinsamer DB-Rueckbau eines Imports (ADR-064 P.5 „ApplyReverse“, R01-006.03) —
/// genutzt von Undo und Recovery-Rollback, damit beide denselben Plan-Cache-
/// Endzustand erreichen. Laeuft IMMER innerhalb einer Transaction des Aufrufers und
/// erst NACH vollstaendig erfolgreichem Disk-Reverse.
/// </summary>
public sealed class ImportReverseApplier
{
    // Reine Pfad-String-Operationen fuer das Archiv-Namensschema (ADR-060-Ausnahme Pfad-Strings).
    private static readonly BauProjektManager.Infrastructure.Services.LocalFileSystem _path = new();
    private readonly PlanManagerDatabase _db;

    public ImportReverseApplier(PlanManagerDatabase db) => _db = db;

    /// <summary>
    /// Nimmt alle DB-Writes des Imports zurueck: angelegte Revisionen soft-deleten,
    /// an fremde Revisionen angehaengte Zusatzdateien entknuepfen, superseded
    /// Revisionen wieder current setzen, Dokumente ohne Revision soft-deleten.
    /// Danach kennt der MD5-Lookup die zurueckgeschobenen Dateien nicht mehr — sie
    /// gelten beim naechsten Erfassen nicht als Dublette (Bucket A).
    /// </summary>
    public void RevertDatabase(string importId, string reason)
    {
        var created = _db.GetRevisionsCreatedByImport(importId);
        var createdIds = created.Select(c => c.RevisionId).ToHashSet();

        foreach (var (revisionId, fileName) in _db.GetFilesLinkedByImport(importId))
            if (!createdIds.Contains(revisionId))
                _db.UnlinkFileFromRevision(revisionId, fileName);

        foreach (var revisionId in createdIds)
            _db.SoftDeleteRevision(revisionId);

        // R01-007.03: archivierte Vorgaenger-Dateien zeigen wieder auf ihren alten Pfad
        foreach (var action in _db.GetImportActions(importId).Where(a => a.ArchivePath is not null))
            _db.RestoreArchivedFilePath(action.ArchivePath!, ArchivePathScheme.OriginalOf(_path, action.ArchivePath!));

        foreach (var revisionId in _db.GetRevisionIdsSupersededByImport(importId))
        {
            _db.RestoreRevisionToCurrent(revisionId);
            _db.InsertRevisionEvent(revisionId, importId, PlanArchive.EventType.MadeCurrent, reason);
        }

        foreach (var documentId in created.Select(c => c.DocumentId).Distinct())
            _db.SoftDeleteDocumentIfNoRevisions(documentId);
    }
}
