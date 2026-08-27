using System.IO;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Verschiebt ein archiviertes Dokument in einen anderen Zielordner
/// (BPM-111.07 Slice D, Radial-Geste im Archiv-Tab): eigene Journal-Aktion
/// mit Status 'moved' — bewusst getrennt von Imports, damit Import-Undo und
/// „letzter Import"-Kennzeichnung unberührt bleiben. Invariante: Journal-
/// Action VOR jedem Move. Alle Dateien der current-Revision (PDF+DWG) ziehen
/// gemeinsam um; die DB bleibt Ordner-Wahrheit (ADR-061).
/// Hinweis: Direktes System.IO wird durch BPM-120/T-Serie gehärtet/portiert.
/// </summary>
public class ArchiveMoveService
{
    private readonly PlanManagerDatabase _db;

    public ArchiveMoveService(PlanManagerDatabase db)
    {
        _db = db;
    }

    /// <summary>Verschiebt alle Dateien der current-Revision in den Zielordner. Journalisiert, nicht undo-bar.</summary>
    public MoveResult MoveDocument(
        PlanArchiveEntry entry, string targetRelativeDirectory, string projectRootPath)
    {
        var files = _db.GetFilesForRevision(entry.RevisionId);
        if (files.Count == 0)
            return new MoveResult(false, 0, "Keine Dateien mit der Revision verknüpft");

        var sourceDir = Path.GetDirectoryName(files[0].RelativePath) ?? "";
        if (string.Equals(sourceDir.Replace('\\', '/'),
                targetRelativeDirectory.Replace('\\', '/').TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase))
            return new MoveResult(false, 0, "Dokument liegt bereits in diesem Ordner");

        var importId = _db.CreateImportJournal(sourceDir, files.Count, profileId: null);
        Log.Information("Archiv-Move: {Plan} ({Count} Datei(en)) -> {Target}",
            entry.PlanNumber, files.Count, targetRelativeDirectory);

        string? actionId = null;
        try
        {
            Directory.CreateDirectory(Path.Combine(projectRootPath, targetRelativeDirectory));

            var order = 0;
            foreach (var file in files)
            {
                var targetRelPath = Path.Combine(targetRelativeDirectory, file.FileName);
                // Invariante: Journal-Action VOR dem Move
                actionId = _db.InsertImportAction(
                    importId, order++, "moved",
                    documentKey: null, entry.PlanNumber, entry.PlanIndex,
                    oldIndex: null, file.RelativePath, targetRelPath, archivePath: null);

                File.Move(
                    Path.Combine(projectRootPath, file.RelativePath),
                    Path.Combine(projectRootPath, targetRelPath),
                    overwrite: false);

                _db.UpdateFilePath(file.FileId, targetRelPath);
                _db.CompleteImportAction(actionId, true);
            }

            var rootParts = targetRelativeDirectory
                .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
            _db.UpdateDocumentDirectory(entry.DocumentId,
                rootParts.Length > 0 ? rootParts[0] : "", targetRelativeDirectory);
            _db.InsertRevisionEvent(entry.RevisionId, importId,
                PlanArchive.EventType.ManualOverride,
                $"Verschoben nach {targetRelativeDirectory}");
            _db.MarkJournalMoved(importId);

            return new MoveResult(true, files.Count, null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Archiv-Move fehlgeschlagen: {Plan}", entry.PlanNumber);
            if (actionId is not null)
                _db.CompleteImportAction(actionId, false, ex.Message);
            _db.CompleteImportJournal(importId, success: false, ex.Message);
            return new MoveResult(false, 0, ex.Message);
        }
    }
}

/// <summary>Ergebnis eines Archiv-Moves (111.07 Slice D).</summary>
public sealed record MoveResult(bool Success, int MovedFiles, string? Error);
