using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Persistenz-Primitive des journalisierten Archiv-Moves (R01-008, ADR-064 +
/// Addendum P.4/P.5, ADR-061 P.6). Eigene Teildatei, damit die Kern-DB-Klasse
/// nicht weiter waechst.
/// </summary>
public partial class PlanManagerDatabase
{
    /// <summary>Action-Typ eines Archiv-Moves im Journal (Journal-Status endet mit 'moved').</summary>
    public const string MovedActionType = "moved";

    /// <summary>Laedt ein (nicht geloeschtes) Dokument per Id oder null.</summary>
    public PlanDocument? GetDocumentById(string documentId)
    {
        var cmd = GetConnection().CreateCommand();
        cmd.CommandText = """
            SELECT id, project_id, document_key, document_type_id, plan_number, document_type,
                   title, target_folder, relative_directory, building_part_id, building_level_id
            FROM plan_documents WHERE id = @id AND is_deleted = 0
            """;
        cmd.Parameters.AddWithValue("@id", documentId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new PlanDocument(
            reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7),
            reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.IsDBNull(10) ? null : reader.GetString(10));
    }

    /// <summary>
    /// Schreibt Ablage UND Klassifikation eines Dokuments nach einem Archiv-Move
    /// (ADR-061: DB = Ordner-Wahrheit; Addendum P.5: Radial-Move schreibt die
    /// Klassifikation mit). Sync-Felder nach CODING_STANDARDS 19.
    /// </summary>
    public void UpdateDocumentPlacement(string documentId, ArchiveMovePlacement placement)
    {
        var cmd = GetConnection().CreateCommand();
        cmd.CommandText = """
            UPDATE plan_documents SET document_key = @dk, document_type_id = @dti,
                document_type = @dt, building_part_id = @bp, building_level_id = @bl,
                target_folder = @tf, relative_directory = @rd,
                last_modified_at = @ua, sync_version = sync_version + 1
            WHERE id = @id
            """;
        cmd.Parameters.AddWithValue("@id", documentId);
        cmd.Parameters.AddWithValue("@dk", placement.DocumentKey);
        cmd.Parameters.AddWithValue("@dti", placement.DocumentTypeId);
        cmd.Parameters.AddWithValue("@dt", placement.DocumentType);
        cmd.Parameters.AddWithValue("@bp", (object?)placement.BuildingPartId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@bl", (object?)placement.BuildingLevelId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@tf", placement.TargetFolder);
        cmd.Parameters.AddWithValue("@rd", placement.RelativeDirectory);
        cmd.Parameters.AddWithValue("@ua", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Dokument, an dessen Revision eine Datei mit diesem relativen Pfad haengt
    /// (Recovery eines Archiv-Moves: Quelle ODER Ziel). Null, wenn nicht getrackt.
    /// </summary>
    public string? FindDocumentIdByFilePath(string relativePath)
    {
        var cmd = GetConnection().CreateCommand();
        cmd.CommandText = """
            SELECT pr.document_id FROM plan_files pf
            JOIN revision_file_links rfl ON rfl.file_id = pf.id
            JOIN plan_revisions pr ON pr.id = rfl.revision_id
            WHERE pf.relative_path = @rp
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@rp", relativePath);
        return cmd.ExecuteScalar() as string;
    }

    /// <summary>
    /// Id des Dokuments mit diesem document_key — auch soft-geloeschte (UNIQUE gilt
    /// ueber alle Zeilen). Null, wenn der Key frei ist.
    /// </summary>
    public string? FindDocumentIdByKeyIncludingDeleted(string documentKey)
    {
        var cmd = GetConnection().CreateCommand();
        cmd.CommandText = "SELECT id FROM plan_documents WHERE document_key = @dk";
        cmd.Parameters.AddWithValue("@dk", documentKey);
        return cmd.ExecuteScalar() as string;
    }

    /// <summary>
    /// True, wenn unter diesem relativen Pfad eine getrackte Datei gefuehrt wird
    /// (R01-008.01): skipDuplicate darf eine solche Datei nie loeschen — sonst ginge
    /// ein in den Eingang geratener Bestandsplan als „Dublette“ verloren.
    /// Vergleich unabhaengig vom Trennzeichen.
    /// </summary>
    public bool IsTrackedFilePath(string relativePath)
    {
        var cmd = GetConnection().CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM plan_files
            WHERE REPLACE(relative_path, '\', '/') = REPLACE(@rp, '\', '/') COLLATE NOCASE
            """;
        cmd.Parameters.AddWithValue("@rp", relativePath);
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    /// <summary>
    /// True, wenn ein spaeterer Archiv-Move (nicht verworfen) Dateien dieses Imports
    /// verschoben hat (ADR-064-Addendum P.4): dann ist das Undo des Imports gesperrt.
    /// Erkennung ueber die Journal-Pfade: Quelle des Moves = Ziel einer Import-Action.
    /// </summary>
    public bool HasArchiveMoveAfterImport(string importId)
    {
        var cmd = GetConnection().CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM import_actions m
            JOIN import_journal mj ON mj.id = m.import_id
            WHERE m.action_type = @at AND mj.status <> 'failed'
              AND mj.timestamp >= (SELECT timestamp FROM import_journal WHERE id = @id)
              AND mj.id <> @id
              AND REPLACE(m.source_path, '\', '/') COLLATE NOCASE IN (
                  SELECT REPLACE(a.destination_path, '\', '/') FROM import_actions a
                  WHERE a.import_id = @id AND a.action_status = 'completed'
                    AND a.destination_path IS NOT NULL)
            """;
        cmd.Parameters.AddWithValue("@at", MovedActionType);
        cmd.Parameters.AddWithValue("@id", importId);
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    /// <summary>
    /// Archivierte Vorgaenger-Datei auf ihren _Archiv-Pfad setzen (R01-007.03, ADR-061).
    /// Trifft nur Dateien, die an KEINER current-Revision haengen — idempotent, und bei
    /// gleichem Dateinamen bleibt die neue Datei am Zielpfad unberuehrt.
    /// </summary>
    public void RelocateSupersededFile(string originalRelPath, string archiveRelPath)
    {
        var cmd = GetConnection().CreateCommand();
        cmd.CommandText = """
            UPDATE plan_files SET relative_path = @ap, updated_at = @ua
            WHERE REPLACE(relative_path, '', '/') = REPLACE(@op, '', '/') COLLATE NOCASE
              AND id NOT IN (
                  SELECT rfl.file_id FROM revision_file_links rfl
                  JOIN plan_revisions pr ON pr.id = rfl.revision_id
                  WHERE pr.revision_status = 'current' AND pr.is_deleted = 0)
            """;
        cmd.Parameters.AddWithValue("@ap", archiveRelPath);
        cmd.Parameters.AddWithValue("@op", originalRelPath);
        cmd.Parameters.AddWithValue("@ua", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    /// <summary>Rueckbau zu <see cref="RelocateSupersededFile"/> (Undo/Rollback, R01-007.03).</summary>
    public void RestoreArchivedFilePath(string archiveRelPath, string originalRelPath)
    {
        var cmd = GetConnection().CreateCommand();
        cmd.CommandText = """
            UPDATE plan_files SET relative_path = @op, updated_at = @ua
            WHERE REPLACE(relative_path, '', '/') = REPLACE(@ap, '', '/') COLLATE NOCASE
            """;
        cmd.Parameters.AddWithValue("@ap", archiveRelPath);
        cmd.Parameters.AddWithValue("@op", originalRelPath);
        cmd.Parameters.AddWithValue("@ua", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    /// <summary>True, wenn das Journal ein Archiv-Move ist (Actions vom Typ 'moved').</summary>
    public bool IsMoveJournal(string importId)
    {
        var cmd = GetConnection().CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM import_actions
            WHERE import_id = @id AND action_type = @at
            """;
        cmd.Parameters.AddWithValue("@id", importId);
        cmd.Parameters.AddWithValue("@at", MovedActionType);
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }
}

/// <summary>
/// Ziel-Ablage + Ziel-Klassifikation eines Dokuments nach einem Archiv-Move
/// (R01-008). Ohne Umklassifizierung entspricht die Klassifikation der bisherigen.
/// </summary>
public sealed record ArchiveMovePlacement(
    string DocumentKey,
    string DocumentTypeId,
    string DocumentType,
    string? BuildingPartId,
    string? BuildingLevelId,
    string TargetFolder,
    string RelativeDirectory);
