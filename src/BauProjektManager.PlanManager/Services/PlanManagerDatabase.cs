using System.IO;
using Microsoft.Data.Sqlite;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// SQLite database service for planmanager.db — per project, local only.
/// Manages plan cache (documents, revisions, files, links) and import journal.
/// Created lazily when PlanManager module opens a project.
/// Schema v2.0 (BPM-109): Drei-Ebenen-Modell (plan_documents → plan_revisions → revision_file_links)
/// + plan_document_segments / plan_revision_events / plan_context_links.
/// Siehe DB-SCHEMA.md Kap. 6.7 + ADR-058 + ADR-058-Addendum (Cross-DB Soft References).
/// BPM-107: Registriert sich bei IPersistenceRegistry beim ersten Connection-Open.
/// </summary>
public class PlanManagerDatabase : IDisposable
{
    private readonly string _dbPath;
    private readonly string _projectId;
    private readonly IIdGenerator _idGenerator;
    private readonly IPersistenceRegistry? _persistenceRegistry;
    private SqliteConnection? _connection;

    /// <summary>Projekt-ID dieser DB (für plan_documents.project_id, BPM-109).</summary>
    public string ProjectId => _projectId;

    public PlanManagerDatabase(string projectId, IIdGenerator idGenerator, IPersistenceRegistry? persistenceRegistry = null)
    {
        _projectId = projectId;
        _idGenerator = idGenerator;
        _persistenceRegistry = persistenceRegistry;
        var projectDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BauProjektManager", "Projects", projectId);
        Directory.CreateDirectory(projectDir);
        _dbPath = Path.Combine(projectDir, "planmanager.db");
    }

    private SqliteConnection GetConnection()
    {
        if (_connection is null)
        {
            _connection = new SqliteConnection($"Data Source={_dbPath}");
            _connection.Open();
            Log.Debug("planmanager.db initialized at {Path}", _dbPath);
            using var walCmd = _connection.CreateCommand();
            walCmd.CommandText = "PRAGMA journal_mode=WAL;";
            walCmd.ExecuteNonQuery();
            using var fkCmd = _connection.CreateCommand();
            fkCmd.CommandText = "PRAGMA foreign_keys=ON;";
            fkCmd.ExecuteNonQuery();
            EnsureTables();

            // BPM-107: bei IPersistenceRegistry registrieren (optional fuer Tests)
            _persistenceRegistry?.Register(new PersistenceEntry(
                DisplayName: "PlanManager-DB",
                AbsolutePath: _dbPath,
                Type: PersistenceType.Database,
                Scope: PersistenceScope.Local,
                Description: "SQLite — plan-Revisionen, Files, Journal pro Projekt"));
        }
        return _connection;
    }

    private void EnsureTables()
    {
        Log.Debug("Creating planmanager.db tables (Schema v2.0, 11 tables)");
        var conn = _connection!;
        var cmd = conn.CreateCommand();
        // === Schema v2.0 (BPM-109 Drei-Ebenen-Modell) ===
        // Frühphasen-Regel: keine Migration. Bei Schema-Wechsel planmanager.db löschen → wird neu erstellt.
        // Cross-DB Soft References (ADR-058-Addendum): building_part_id / building_level_id /
        // segment_type_id zeigen logisch auf bpm.db-Tabellen, sind aber reine TEXT-Spalten OHNE FK
        // (SQLite erzwingt keine FK über getrennte DB-Dateien). Harte FKs nur innerhalb planmanager.db.
        cmd.CommandText = """
            -- Plan Documents (NEU v2.0) — logisches Dokument über alle Revisionen hinweg
            CREATE TABLE IF NOT EXISTS plan_documents (
                id TEXT PRIMARY KEY,
                project_id TEXT NOT NULL,
                document_key TEXT NOT NULL UNIQUE,
                document_type_id TEXT NOT NULL,
                plan_number TEXT NOT NULL,
                document_type TEXT NOT NULL,
                title TEXT NOT NULL DEFAULT '',
                target_folder TEXT NOT NULL,
                relative_directory TEXT NOT NULL,
                building_part_id TEXT,              -- SoftRef bpm.db.building_parts(id), kein FK (Cross-DB)
                building_level_id TEXT,             -- SoftRef bpm.db.building_levels(id), kein FK (Cross-DB)
                created_at TEXT NOT NULL,
                created_by TEXT,
                last_modified_at TEXT NOT NULL,
                last_modified_by TEXT,
                sync_version INTEGER NOT NULL DEFAULT 0,
                is_deleted INTEGER NOT NULL DEFAULT 0
                -- Keine FK auf building_parts/building_levels: Cross-DB Soft Reference (ADR-058-Addendum)
            );

            CREATE INDEX IF NOT EXISTS idx_plan_documents_lookup
            ON plan_documents(project_id, building_part_id, building_level_id, document_type_id, is_deleted);

            CREATE INDEX IF NOT EXISTS idx_plan_documents_key ON plan_documents(document_key);

            -- Plan Revisions (UMGEBAUT v2.0) — versionierte Revision mit Zeitstempeln für Zeitreise
            CREATE TABLE IF NOT EXISTS plan_revisions (
                id TEXT PRIMARY KEY,
                document_id TEXT NOT NULL,
                plan_index TEXT,
                index_source TEXT NOT NULL,
                revision_status TEXT NOT NULL
                    CHECK (revision_status IN ('current', 'superseded', 'rejected')),
                current_from TEXT NOT NULL,
                superseded_at TEXT,
                received_at TEXT NOT NULL,
                released_at TEXT,                   -- Freigabedatum d. Index (BPM-109.04b); NULL bis OCR/manuell (post-V1)
                change_note TEXT NOT NULL DEFAULT '', -- Änderungshinweis d. Revision (ADR-063, befüllt via BPM-118 Text-Zuweisung)
                last_import_id TEXT,
                created_at TEXT NOT NULL,
                created_by TEXT,
                last_modified_at TEXT NOT NULL,
                last_modified_by TEXT,
                sync_version INTEGER NOT NULL DEFAULT 0,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (document_id) REFERENCES plan_documents(id),
                FOREIGN KEY (last_import_id) REFERENCES import_journal(id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_revisions_current
            ON plan_revisions(document_id)
            WHERE revision_status = 'current' AND is_deleted = 0;

            CREATE INDEX IF NOT EXISTS idx_plan_revisions_timetravel
            ON plan_revisions(document_id, current_from, superseded_at, is_deleted);

            -- Plan Files Cache (unverändert v1.0)
            CREATE TABLE IF NOT EXISTS plan_files (
                id TEXT PRIMARY KEY,
                file_name TEXT NOT NULL,
                relative_path TEXT NOT NULL,
                file_type TEXT NOT NULL,
                md5_hash TEXT NOT NULL,
                file_size INTEGER NOT NULL,
                origin_mode TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            -- Revision-File Links (n:m, unverändert v1.0)
            CREATE TABLE IF NOT EXISTS revision_file_links (
                revision_id TEXT NOT NULL,
                file_id TEXT NOT NULL,
                link_mode TEXT NOT NULL,
                is_primary INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (revision_id, file_id),
                FOREIGN KEY (revision_id) REFERENCES plan_revisions(id),
                FOREIGN KEY (file_id) REFERENCES plan_files(id)
            );

            -- Plan Document Segments (NEU v2.0) — extrahierte Segmentwerte als KV-Tabelle
            CREATE TABLE IF NOT EXISTS plan_document_segments (
                id TEXT PRIMARY KEY,
                document_id TEXT NOT NULL,
                segment_type_id TEXT NOT NULL,      -- SoftRef bpm.db.segment_types(id), kein FK (Cross-DB)
                segment_key TEXT NOT NULL,
                raw_value TEXT NOT NULL,
                normalized_value TEXT NOT NULL,
                created_at TEXT NOT NULL,
                created_by TEXT,
                last_modified_at TEXT NOT NULL,
                last_modified_by TEXT,
                sync_version INTEGER NOT NULL DEFAULT 0,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (document_id) REFERENCES plan_documents(id),
                -- Keine FK auf segment_types: Cross-DB Soft Reference (ADR-058-Addendum)
                UNIQUE (document_id, segment_type_id)
            );

            CREATE INDEX IF NOT EXISTS idx_plan_document_segments_lookup
            ON plan_document_segments(segment_type_id, normalized_value, is_deleted);

            -- Plan Revision Events (NEU v2.0) — minimaler Audit-Trail für Statuswechsel
            CREATE TABLE IF NOT EXISTS plan_revision_events (
                id TEXT PRIMARY KEY,
                revision_id TEXT NOT NULL,
                import_id TEXT,
                event_type TEXT NOT NULL
                    CHECK (event_type IN ('created', 'made_current', 'superseded', 'file_linked', 'manual_override')),
                event_at TEXT NOT NULL,
                event_by TEXT,
                note TEXT NOT NULL DEFAULT '',
                created_at TEXT NOT NULL,
                created_by TEXT,
                last_modified_at TEXT NOT NULL,
                last_modified_by TEXT,
                sync_version INTEGER NOT NULL DEFAULT 0,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (revision_id) REFERENCES plan_revisions(id),
                FOREIGN KEY (import_id) REFERENCES import_journal(id)
            );

            CREATE INDEX IF NOT EXISTS idx_plan_revision_events_revision
            ON plan_revision_events(revision_id, event_at);

            -- Plan Context Links (NEU v2.0) — Cross-Modul-Verknüpfung (fixed_revision Pflicht)
            CREATE TABLE IF NOT EXISTS plan_context_links (
                id TEXT PRIMARY KEY,
                source_module TEXT NOT NULL,
                source_id TEXT NOT NULL,
                target_document_id TEXT NOT NULL,
                target_revision_id TEXT,
                resolution_mode TEXT NOT NULL
                    CHECK (resolution_mode IN ('fixed_revision')),
                context_time TEXT NOT NULL,
                link_type TEXT NOT NULL
                    CHECK (link_type IN ('auto_reference', 'manual_reference', 'attachment')),
                created_at TEXT NOT NULL,
                created_by TEXT,
                last_modified_at TEXT NOT NULL,
                last_modified_by TEXT,
                sync_version INTEGER NOT NULL DEFAULT 0,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (target_document_id) REFERENCES plan_documents(id),
                FOREIGN KEY (target_revision_id) REFERENCES plan_revisions(id)
            );

            CREATE INDEX IF NOT EXISTS idx_plan_context_links_source
            ON plan_context_links(source_module, source_id, is_deleted);

            CREATE INDEX IF NOT EXISTS idx_plan_context_links_target
            ON plan_context_links(target_document_id, target_revision_id, is_deleted);

            -- Import Journal (unverändert v1.0)
            CREATE TABLE IF NOT EXISTS import_journal (
                id TEXT PRIMARY KEY,
                timestamp TEXT NOT NULL,
                completed_at TEXT,
                status TEXT NOT NULL,
                source_path TEXT NOT NULL,
                file_count INTEGER NOT NULL,
                profile_id TEXT,
                machine_name TEXT,
                error_message TEXT
            );

            -- Import Actions (unverändert v1.0)
            CREATE TABLE IF NOT EXISTS import_actions (
                id TEXT PRIMARY KEY,
                import_id TEXT NOT NULL,
                action_order INTEGER NOT NULL,
                action_type TEXT NOT NULL,
                action_status TEXT NOT NULL,
                document_key TEXT,
                plan_number TEXT NOT NULL,
                plan_index TEXT,
                old_index TEXT,
                source_path TEXT NOT NULL,
                destination_path TEXT,
                archive_path TEXT,
                md5 TEXT,
                file_size INTEGER,
                error_message TEXT,
                FOREIGN KEY (import_id) REFERENCES import_journal(id)
            );

            CREATE INDEX IF NOT EXISTS idx_actions_import ON import_actions(import_id);

            -- Import Action Files (unverändert v1.0)
            CREATE TABLE IF NOT EXISTS import_action_files (
                id TEXT PRIMARY KEY,
                action_id TEXT NOT NULL,
                file_id TEXT,
                file_name TEXT NOT NULL,
                original_file_name TEXT,
                final_file_name TEXT,
                file_type TEXT NOT NULL,
                source_path TEXT NOT NULL,
                destination_path TEXT NOT NULL,
                md5_hash TEXT NOT NULL,
                file_size INTEGER,
                FOREIGN KEY (action_id) REFERENCES import_actions(id)
            );

            CREATE INDEX IF NOT EXISTS idx_action_files_action ON import_action_files(action_id);

            -- Schema Version
            CREATE TABLE IF NOT EXISTS schema_version (
                version TEXT NOT NULL
            );

            INSERT OR REPLACE INTO schema_version (version) VALUES ('2.0');
            """;
        cmd.ExecuteNonQuery();
    }

    // === PLAN ARCHIVE v2.0 (BPM-109.02 Repository-Primitive) ===
    //
    // Additive Document-zentrische Primitive gegen das Drei-Ebenen-Schema. Die Pipeline-Verdrahtung
    // (ImportExecutionService/ImportWorkflowService auf diese Methoden) erfolgt in BPM-109.03, die
    // Revision-Zeitlogik (Supersede-Übergänge) in BPM-109.04. Cross-DB-Bezüge sind Soft References
    // (kein FK), Validierung service-seitig. Audit-/Sync-Spalten werden hier gesetzt.

    /// <summary>
    /// Sucht ein plan_documents per document_key (find) oder legt es neu an (create).
    /// Gibt die document_id zurück. Idempotent bzgl. document_key (UNIQUE).
    /// </summary>
    public string ResolveOrCreateDocument(
        string projectId, string documentKey, string documentTypeId,
        string planNumber, string documentType, string title,
        string targetFolder, string relativeDirectory,
        string? buildingPartId, string? buildingLevelId)
    {
        var conn = GetConnection();
        var findCmd = conn.CreateCommand();
        findCmd.CommandText = "SELECT id FROM plan_documents WHERE document_key = @dk AND is_deleted = 0";
        findCmd.Parameters.AddWithValue("@dk", documentKey);
        if (findCmd.ExecuteScalar() is string existingId)
            return existingId;

        var id = _idGenerator.NewId();
        var now = DateTime.UtcNow.ToString("o");
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO plan_documents (id, project_id, document_key, document_type_id,
                plan_number, document_type, title, target_folder, relative_directory,
                building_part_id, building_level_id,
                created_at, last_modified_at, sync_version, is_deleted)
            VALUES (@id, @pid, @dk, @dti, @pn, @dt, @ti, @tf, @rd, @bp, @bl, @ca, @ua, 0, 0)
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@pid", projectId);
        cmd.Parameters.AddWithValue("@dk", documentKey);
        cmd.Parameters.AddWithValue("@dti", documentTypeId);
        cmd.Parameters.AddWithValue("@pn", planNumber);
        cmd.Parameters.AddWithValue("@dt", documentType);
        cmd.Parameters.AddWithValue("@ti", title);
        cmd.Parameters.AddWithValue("@tf", targetFolder);
        cmd.Parameters.AddWithValue("@rd", relativeDirectory);
        cmd.Parameters.AddWithValue("@bp", (object?)buildingPartId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@bl", (object?)buildingLevelId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ca", now);
        cmd.Parameters.AddWithValue("@ua", now);
        cmd.ExecuteNonQuery();
        Log.Debug("plan_documents angelegt: {Key} -> {Id}", documentKey, id);
        return id;
    }

    /// <summary>Lädt ein plan_documents per document_key (oder null).</summary>
    public PlanDocument? GetDocumentByKey(string documentKey)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, project_id, document_key, document_type_id, plan_number, document_type,
                   title, target_folder, relative_directory, building_part_id, building_level_id
            FROM plan_documents WHERE document_key = @dk AND is_deleted = 0
            """;
        cmd.Parameters.AddWithValue("@dk", documentKey);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new PlanDocument(
            reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7),
            reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.IsDBNull(10) ? null : reader.GetString(10));
    }

    /// <summary>Fügt eine Revision für ein Dokument ein. Gibt die revision_id zurück.</summary>
    public string InsertRevision(
        string documentId, string? planIndex, string indexSource,
        string revisionStatus, string currentFrom, string? supersededAt,
        string receivedAt, string? lastImportId, string? releasedAt = null,
        string changeNote = "")
    {
        var conn = GetConnection();
        var id = _idGenerator.NewId();
        var now = DateTime.UtcNow.ToString("o");
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO plan_revisions (id, document_id, plan_index, index_source, revision_status,
                current_from, superseded_at, received_at, released_at, change_note, last_import_id,
                created_at, last_modified_at, sync_version, is_deleted)
            VALUES (@id, @did, @pi, @is, @st, @cf, @sa, @ra, @rel, @cn, @ii, @ca, @ua, 0, 0)
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@did", documentId);
        cmd.Parameters.AddWithValue("@pi", (object?)planIndex ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@is", indexSource);
        cmd.Parameters.AddWithValue("@st", revisionStatus);
        cmd.Parameters.AddWithValue("@cf", currentFrom);
        cmd.Parameters.AddWithValue("@sa", (object?)supersededAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ra", receivedAt);
        cmd.Parameters.AddWithValue("@rel", (object?)releasedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cn", changeNote);
        cmd.Parameters.AddWithValue("@ii", (object?)lastImportId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ca", now);
        cmd.Parameters.AddWithValue("@ua", now);
        cmd.ExecuteNonQuery();
        return id;
    }

    /// <summary>Fügt einen extrahierten Segmentwert für ein Dokument ein. Gibt die segment-id zurück.</summary>
    public string InsertSegment(
        string documentId, string segmentTypeId, string segmentKey,
        string rawValue, string normalizedValue)
    {
        var conn = GetConnection();
        var id = _idGenerator.NewId();
        var now = DateTime.UtcNow.ToString("o");
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO plan_document_segments (id, document_id, segment_type_id, segment_key,
                raw_value, normalized_value, created_at, last_modified_at, sync_version, is_deleted)
            VALUES (@id, @did, @sti, @sk, @rv, @nv, @ca, @ua, 0, 0)
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@did", documentId);
        cmd.Parameters.AddWithValue("@sti", segmentTypeId);
        cmd.Parameters.AddWithValue("@sk", segmentKey);
        cmd.Parameters.AddWithValue("@rv", rawValue);
        cmd.Parameters.AddWithValue("@nv", normalizedValue);
        cmd.Parameters.AddWithValue("@ca", now);
        cmd.Parameters.AddWithValue("@ua", now);
        cmd.ExecuteNonQuery();
        return id;
    }

    /// <summary>
    /// Setzt einen Segmentwert für ein Dokument (BPM-118): Insert, oder Update bei
    /// bestehendem Wert desselben Segmenttyps (UNIQUE document_id+segment_type_id) —
    /// die letzte User-Zuweisung gewinnt.
    /// </summary>
    public void UpsertSegment(
        string documentId, string segmentTypeId, string segmentKey,
        string rawValue, string normalizedValue)
    {
        var conn = GetConnection();
        var id = _idGenerator.NewId();
        var now = DateTime.UtcNow.ToString("o");
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO plan_document_segments (id, document_id, segment_type_id, segment_key,
                raw_value, normalized_value, created_at, last_modified_at, sync_version, is_deleted)
            VALUES (@id, @did, @sti, @sk, @rv, @nv, @ca, @ua, 0, 0)
            ON CONFLICT (document_id, segment_type_id) DO UPDATE SET
                segment_key = @sk, raw_value = @rv, normalized_value = @nv,
                last_modified_at = @ua, is_deleted = 0
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@did", documentId);
        cmd.Parameters.AddWithValue("@sti", segmentTypeId);
        cmd.Parameters.AddWithValue("@sk", segmentKey);
        cmd.Parameters.AddWithValue("@rv", rawValue);
        cmd.Parameters.AddWithValue("@nv", normalizedValue);
        cmd.Parameters.AddWithValue("@ca", now);
        cmd.Parameters.AddWithValue("@ua", now);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Archiv-Bestandsliste (111.07 Slice D): alle Dokumente mit current-Revision
    /// und Primärdatei, neueste Importe zuerst.
    /// </summary>
    public List<PlanArchiveEntry> GetArchiveEntries()
    {
        var conn = GetConnection();
        var result = new List<PlanArchiveEntry>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT pd.id, pd.plan_number, pd.title, pd.document_type, pr.id, pr.plan_index,
                   pr.received_at, pr.last_import_id, pf.file_name, pf.relative_path
            FROM plan_documents pd
            JOIN plan_revisions pr ON pr.document_id = pd.id
                AND pr.revision_status = 'current' AND pr.is_deleted = 0
            LEFT JOIN revision_file_links rfl ON rfl.revision_id = pr.id AND rfl.is_primary = 1
            LEFT JOIN plan_files pf ON pf.id = rfl.file_id
            WHERE pd.is_deleted = 0
            ORDER BY pr.received_at DESC, pd.plan_number ASC
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new PlanArchiveEntry(
                reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9)));
        }
        return result;
    }

    /// <summary>Alle mit einer Revision verknüpften Dateien (111.07 Slice D, Primärdatei zuerst).</summary>
    public List<PlanRevisionFile> GetFilesForRevision(string revisionId)
    {
        var conn = GetConnection();
        var result = new List<PlanRevisionFile>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT pf.id, pf.file_name, pf.relative_path, rfl.is_primary
            FROM plan_files pf
            JOIN revision_file_links rfl ON rfl.file_id = pf.id
            WHERE rfl.revision_id = @rid
            ORDER BY rfl.is_primary DESC, pf.file_name ASC
            """;
        cmd.Parameters.AddWithValue("@rid", revisionId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(new PlanRevisionFile(
                reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetInt64(3) != 0));
        return result;
    }

    /// <summary>Aktualisiert den relativen Pfad einer Datei nach einem Archiv-Move (111.07 Slice D).</summary>
    public void UpdateFilePath(string fileId, string newRelativePath)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE plan_files SET relative_path = @rp, updated_at = @ua WHERE id = @id
            """;
        cmd.Parameters.AddWithValue("@rp", newRelativePath);
        cmd.Parameters.AddWithValue("@ua", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@id", fileId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Aktualisiert die Ablage eines Dokuments nach einem Archiv-Move (111.07 Slice D, ADR-061: DB = Ordner-Wahrheit).</summary>
    public void UpdateDocumentDirectory(string documentId, string targetFolder, string relativeDirectory)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE plan_documents
            SET target_folder = @tf, relative_directory = @rd, last_modified_at = @ua
            WHERE id = @id
            """;
        cmd.Parameters.AddWithValue("@tf", targetFolder);
        cmd.Parameters.AddWithValue("@rd", relativeDirectory);
        cmd.Parameters.AddWithValue("@ua", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@id", documentId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Schließt einen Archiv-Move-Journal-Vorgang mit Status 'moved' ab
    /// (111.07 Slice D): bewusst NICHT 'completed', damit Import-Undo und
    /// „letzter Import"-Kennzeichnung Moves ignorieren.
    /// </summary>
    public void MarkJournalMoved(string importId)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE import_journal SET status = 'moved', completed_at = @ca WHERE id = @id
            """;
        cmd.Parameters.AddWithValue("@ca", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@id", importId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Lädt die Segmentwerte eines Dokuments (BPM-118), sortiert nach segment_key.</summary>
    public List<PlanDocumentSegment> GetSegmentsForDocument(string documentId)
    {
        var conn = GetConnection();
        var result = new List<PlanDocumentSegment>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, document_id, segment_type_id, segment_key, raw_value, normalized_value
            FROM plan_document_segments
            WHERE document_id = @did AND is_deleted = 0
            ORDER BY segment_key ASC
            """;
        cmd.Parameters.AddWithValue("@did", documentId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new PlanDocumentSegment(
                reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5)));
        }
        return result;
    }

    /// <summary>
    /// Relativer Pfad der PDF-Datei einer Revision (BPM-111.06 Slice C3, DWG-Paarung):
    /// bevorzugt die Primärdatei, sonst die zuerst verknüpfte PDF. NULL wenn keine.
    /// </summary>
    public string? GetPdfPathForRevision(string revisionId)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT pf.relative_path
            FROM plan_files pf
            JOIN revision_file_links rfl ON rfl.file_id = pf.id
            WHERE rfl.revision_id = @rid AND LOWER(pf.file_type) = '.pdf'
            ORDER BY rfl.is_primary DESC, pf.created_at ASC
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@rid", revisionId);
        return cmd.ExecuteScalar() as string;
    }

    /// <summary>Fügt einen Revisions-Event ein (Audit-Trail). Gibt die event-id zurück.</summary>
    public string InsertRevisionEvent(
        string revisionId, string? importId, string eventType, string note = "")
    {
        var conn = GetConnection();
        var id = _idGenerator.NewId();
        var now = DateTime.UtcNow.ToString("o");
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO plan_revision_events (id, revision_id, import_id, event_type, event_at,
                note, created_at, last_modified_at, sync_version, is_deleted)
            VALUES (@id, @rid, @iid, @et, @ea, @note, @ca, @ua, 0, 0)
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@rid", revisionId);
        cmd.Parameters.AddWithValue("@iid", (object?)importId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@et", eventType);
        cmd.Parameters.AddWithValue("@ea", now);
        cmd.Parameters.AddWithValue("@note", note);
        cmd.Parameters.AddWithValue("@ca", now);
        cmd.Parameters.AddWithValue("@ua", now);
        cmd.ExecuteNonQuery();
        return id;
    }

    /// <summary>Lädt den Audit-Trail (Events) einer Revision, chronologisch (event_at aufsteigend). BPM-109.04.</summary>
    public List<PlanRevisionEvent> GetRevisionEvents(string revisionId)
    {
        var conn = GetConnection();
        var result = new List<PlanRevisionEvent>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, revision_id, import_id, event_type, event_at, note
            FROM plan_revision_events
            WHERE revision_id = @rid AND is_deleted = 0
            ORDER BY event_at ASC
            """;
        cmd.Parameters.AddWithValue("@rid", revisionId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new PlanRevisionEvent(
                reader.GetString(0), reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5)));
        }
        return result;
    }

    /// <summary>Lädt die aktuelle (current) Revision eines Dokuments (oder null).</summary>
    public PlanRevision? GetCurrentRevisionForDocument(string documentId)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, document_id, plan_index, index_source, revision_status,
                   current_from, superseded_at, received_at, released_at, last_import_id,
                   change_note
            FROM plan_revisions
            WHERE document_id = @did AND revision_status = 'current' AND is_deleted = 0
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@did", documentId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new PlanRevision(
            reader.GetString(0), reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.GetString(3), reader.GetString(4), reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.GetString(10));
    }

    /// <summary>Lädt alle Revisionen eines Dokuments (Historie), sortiert nach current_from. BPM-109.04.</summary>
    public List<PlanRevision> GetRevisionsForDocument(string documentId)
    {
        var conn = GetConnection();
        var result = new List<PlanRevision>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, document_id, plan_index, index_source, revision_status,
                   current_from, superseded_at, received_at, released_at, last_import_id,
                   change_note
            FROM plan_revisions
            WHERE document_id = @did AND is_deleted = 0
            ORDER BY current_from ASC
            """;
        cmd.Parameters.AddWithValue("@did", documentId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new PlanRevision(
                reader.GetString(0), reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.GetString(10)));
        }
        return result;
    }

    /// <summary>
    /// Lookup aller aktuellen Revisionen, keyed by document_key — der v2.0-Ersatz für die in .01
    /// Fail-Fast gesetzte GetAllCurrentRevisions. Wird in BPM-109.03 vom RevisionDecisionService genutzt.
    /// md5 kommt aus der primären Datei (LEFT JOIN — leer wenn noch keine Datei verknüpft).
    /// </summary>
    public Dictionary<string, ExistingRevision> GetCurrentRevisionLookup()
    {
        var conn = GetConnection();
        var result = new Dictionary<string, ExistingRevision>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT pd.document_key, pr.id, pr.plan_index, COALESCE(pf.md5_hash, '') AS md5
            FROM plan_documents pd
            JOIN plan_revisions pr ON pr.document_id = pd.id
                AND pr.revision_status = 'current' AND pr.is_deleted = 0
            LEFT JOIN revision_file_links rfl ON rfl.revision_id = pr.id AND rfl.is_primary = 1
            LEFT JOIN plan_files pf ON pf.id = rfl.file_id
            WHERE pd.is_deleted = 0
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result[reader.GetString(0)] = new ExistingRevision(
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetString(3));
        }
        Log.Debug("planmanager.db: {Count} aktuelle Revisionen (v2.0 Lookup)", result.Count);
        return result;
    }

    /// <summary>
    /// Alle bekannten Dokumente mit ihrer aktuellen Revision — Matching-Grundlage
    /// fuer den ManualFirstCapture-Workflow (BPM-111.03). Read-only.
    /// </summary>
    public List<KnownPlanDocument> GetCurrentDocumentLookup()
    {
        var conn = GetConnection();
        var result = new List<KnownPlanDocument>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT pd.id, pd.document_key, pd.plan_number, pd.document_type,
                   pd.target_folder, pd.relative_directory, pr.plan_index, pr.id
            FROM plan_documents pd
            JOIN plan_revisions pr ON pr.document_id = pd.id
                AND pr.revision_status = 'current' AND pr.is_deleted = 0
            WHERE pd.is_deleted = 0
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new KnownPlanDocument(
                reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetString(7)));
        }
        Log.Debug("planmanager.db: {Count} bekannte Dokumente (Capture-Lookup)", result.Count);
        return result;
    }

    /// <summary>
    /// MD5-Lookup aller verknuepften Bestandsdateien -> document_key.
    /// Dubletten-Erkennung (Bucket A) im ManualFirstCapture-Workflow (BPM-111.03).
    /// </summary>
    public Dictionary<string, string> GetKnownMd5Lookup()
    {
        var conn = GetConnection();
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT pf.md5_hash, pd.document_key
            FROM plan_files pf
            JOIN revision_file_links rfl ON rfl.file_id = pf.id
            JOIN plan_revisions pr ON pr.id = rfl.revision_id AND pr.is_deleted = 0
            JOIN plan_documents pd ON pd.id = pr.document_id AND pd.is_deleted = 0
            WHERE pf.md5_hash <> ''
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result[reader.GetString(0)] = reader.GetString(1);
        Log.Debug("planmanager.db: {Count} bekannte MD5-Hashes", result.Count);
        return result;
    }

    /// <summary>
    /// Legt eine Datei an (plan_files) und verknüpft sie mit einer Revision (revision_file_links).
    /// Gibt die file-id zurück. BPM-109.03.
    /// </summary>
    public string InsertFileForRevision(
        string revisionId, string fileName, string relativePath, string fileType,
        string md5Hash, long fileSize, bool isPrimary)
    {
        var conn = GetConnection();
        var now = DateTime.UtcNow.ToString("o");
        var fileId = _idGenerator.NewId();

        var fileCmd = conn.CreateCommand();
        fileCmd.CommandText = """
            INSERT INTO plan_files (id, file_name, relative_path, file_type,
                md5_hash, file_size, origin_mode, created_at, updated_at)
            VALUES (@id, @fn, @rp, @ft, @md5, @fs, 'autoGrouped', @ca, @ua)
            """;
        fileCmd.Parameters.AddWithValue("@id", fileId);
        fileCmd.Parameters.AddWithValue("@fn", fileName);
        fileCmd.Parameters.AddWithValue("@rp", relativePath);
        fileCmd.Parameters.AddWithValue("@ft", fileType);
        fileCmd.Parameters.AddWithValue("@md5", md5Hash);
        fileCmd.Parameters.AddWithValue("@fs", fileSize);
        fileCmd.Parameters.AddWithValue("@ca", now);
        fileCmd.Parameters.AddWithValue("@ua", now);
        fileCmd.ExecuteNonQuery();

        var linkCmd = conn.CreateCommand();
        linkCmd.CommandText = """
            INSERT INTO revision_file_links (revision_id, file_id, link_mode, is_primary)
            VALUES (@rid, @fid, 'auto', @pr)
            """;
        linkCmd.Parameters.AddWithValue("@rid", revisionId);
        linkCmd.Parameters.AddWithValue("@fid", fileId);
        linkCmd.Parameters.AddWithValue("@pr", isPrimary ? 1 : 0);
        linkCmd.ExecuteNonQuery();
        return fileId;
    }

    /// <summary>
    /// Setzt alle aktuellen (current) Revisionen eines Dokuments auf 'superseded' (minimal, BPM-109.03).
    /// Feinlogik (Events, current_from-Kette) folgt in BPM-109.04. Gibt die Anzahl betroffener Zeilen zurück.
    /// </summary>
    public int SupersedeCurrentRevision(string documentId, string supersededAtUtc)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE plan_revisions
            SET revision_status = 'superseded', superseded_at = @sa, last_modified_at = @sa
            WHERE document_id = @did AND revision_status = 'current' AND is_deleted = 0
            """;
        cmd.Parameters.AddWithValue("@sa", supersededAtUtc);
        cmd.Parameters.AddWithValue("@did", documentId);
        return cmd.ExecuteNonQuery();
    }

    // === IMPORT JOURNAL (unverändert v1.0) ===

    /// <summary>
    /// Creates a new import journal entry with status 'pending'.
    /// Returns the import ID.
    /// </summary>
    public string CreateImportJournal(string sourcePath, int fileCount, string? profileId)
    {
        var conn = GetConnection();
        var id = _idGenerator.NewId();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO import_journal (id, timestamp, status, source_path,
                file_count, profile_id, machine_name)
            VALUES (@id, @ts, 'pending', @sp, @fc, @pid, @mn)
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@sp", sourcePath);
        cmd.Parameters.AddWithValue("@fc", fileCount);
        cmd.Parameters.AddWithValue("@pid", (object?)profileId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@mn", Environment.MachineName);
        cmd.ExecuteNonQuery();
        Log.Information("Import-Journal erstellt: {Id}, {Count} Dateien", id, fileCount);
        return id;
    }

    /// <summary>
    /// Marks an import journal entry as completed or failed.
    /// </summary>
    public void CompleteImportJournal(string importId, bool success, string? errorMessage = null)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE import_journal SET status = @status,
                completed_at = @ca, error_message = @err
            WHERE id = @id
            """;
        cmd.Parameters.AddWithValue("@id", importId);
        cmd.Parameters.AddWithValue("@status", success ? "completed" : "failed");
        cmd.Parameters.AddWithValue("@ca", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@err", (object?)errorMessage ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts an import action (one file operation).
    /// BPM-120 T2: destination_path nullable (skipDuplicate hat kein Ziel);
    /// md5 + file_size fuer Bucket A / Recovery-Verifikation.
    /// </summary>
    public string InsertImportAction(
        string importId, int actionOrder, string actionType,
        string? documentKey, string planNumber, string? planIndex,
        string? oldIndex, string sourcePath, string? destinationPath,
        string? archivePath, string? md5 = null, long? fileSize = null)
    {
        var conn = GetConnection();
        var id = _idGenerator.NewId();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO import_actions (id, import_id, action_order, action_type,
                action_status, document_key, plan_number, plan_index, old_index,
                source_path, destination_path, archive_path, md5, file_size)
            VALUES (@id, @iid, @ao, @at, 'pending', @dk, @pn, @pi, @oi, @sp, @dp, @ap, @md5, @fs)
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@iid", importId);
        cmd.Parameters.AddWithValue("@ao", actionOrder);
        cmd.Parameters.AddWithValue("@at", actionType);
        cmd.Parameters.AddWithValue("@dk", (object?)documentKey ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@pn", planNumber);
        cmd.Parameters.AddWithValue("@pi", (object?)planIndex ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@oi", (object?)oldIndex ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@sp", sourcePath);
        cmd.Parameters.AddWithValue("@dp", (object?)destinationPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ap", (object?)archivePath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@md5", (object?)md5 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@fs", (object?)fileSize ?? DBNull.Value);
        cmd.ExecuteNonQuery();
        return id;
    }

    /// <summary>
    /// Updates an import action's status to completed or failed.
    /// </summary>
    public void CompleteImportAction(string actionId, bool success, string? errorMessage = null)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE import_actions SET action_status = @status,
                error_message = @err WHERE id = @id
            """;
        cmd.Parameters.AddWithValue("@id", actionId);
        cmd.Parameters.AddWithValue("@status", success ? "completed" : "failed");
        cmd.Parameters.AddWithValue("@err", (object?)errorMessage ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Lädt alle Import-Actions zu einem Import. Optional gefiltert nach action_status
    /// ('pending', 'completed', 'failed'). Sortiert nach action_order — wichtig für
    /// Recovery: Forward in Original-Reihenfolge, Rollback in umgekehrter.
    /// Siehe BPM-016 / 016.03.
    /// </summary>
    public List<ImportActionRow> GetImportActions(string importId, string? statusFilter = null)
    {
        var conn = GetConnection();
        var result = new List<ImportActionRow>();
        var cmd = conn.CreateCommand();
        if (statusFilter is null)
        {
            cmd.CommandText = """
                SELECT id, action_type, action_status, source_path, destination_path,
                    archive_path, md5, file_size
                FROM import_actions WHERE import_id = @iid
                ORDER BY action_order ASC
                """;
        }
        else
        {
            cmd.CommandText = """
                SELECT id, action_type, action_status, source_path, destination_path,
                    archive_path, md5, file_size
                FROM import_actions WHERE import_id = @iid AND action_status = @st
                ORDER BY action_order ASC
                """;
            cmd.Parameters.AddWithValue("@st", statusFilter);
        }
        cmd.Parameters.AddWithValue("@iid", importId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "SELECT-ACTIONS", "import_actions");

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ImportActionRow(
                Id: reader.GetString(reader.GetOrdinal("id")),
                ActionType: reader.GetString(reader.GetOrdinal("action_type")),
                ActionStatus: reader.GetString(reader.GetOrdinal("action_status")),
                SourcePath: reader.GetString(reader.GetOrdinal("source_path")),
                DestinationPath: reader.IsDBNull(reader.GetOrdinal("destination_path"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("destination_path")),
                ArchivePath: reader.IsDBNull(reader.GetOrdinal("archive_path"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("archive_path")),
                Md5: reader.IsDBNull(reader.GetOrdinal("md5"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("md5")),
                FileSize: reader.IsDBNull(reader.GetOrdinal("file_size"))
                    ? null
                    : reader.GetInt64(reader.GetOrdinal("file_size"))));
        }
        return result;
    }

    /// <summary>
    /// Checks for pending import journals (for recovery on app start).
    /// Lightweight COUNT-Variante — verwendet keinen JOIN auf import_actions.
    /// Für Detail-Info (Action-Counts, Source-Path, etc.) <see cref="GetPendingImports"/> verwenden.
    /// </summary>
    public bool HasPendingImports()
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM import_journal WHERE status = 'pending'";
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    /// <summary>
    /// Liefert Detail-Infos zu allen Imports mit Status 'pending' (für Recovery-Dialog).
    /// Aggregiert Action-Status-Counts (completed/failed/pending) per Import via JOIN
    /// auf import_actions. Sortiert nach Start-Timestamp absteigend (neueste zuerst).
    /// Siehe BPM-016 / 016.01.
    /// </summary>
    public List<PendingImportInfo> GetPendingImports()
    {
        var conn = GetConnection();
        var result = new List<PendingImportInfo>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                j.id,
                j.timestamp,
                j.source_path,
                j.file_count,
                j.profile_id,
                j.machine_name,
                COALESCE(SUM(CASE WHEN a.action_status = 'completed' THEN 1 ELSE 0 END), 0) AS completed_actions,
                COALESCE(SUM(CASE WHEN a.action_status = 'failed' THEN 1 ELSE 0 END), 0) AS failed_actions,
                COALESCE(SUM(CASE WHEN a.action_status = 'pending' THEN 1 ELSE 0 END), 0) AS pending_actions
            FROM import_journal j
            LEFT JOIN import_actions a ON a.import_id = j.id
            WHERE j.status = 'pending'
            GROUP BY j.id, j.timestamp, j.source_path, j.file_count, j.profile_id, j.machine_name
            ORDER BY j.timestamp DESC
            """;
        Log.Verbose("Executing SQL: {Operation} on {Table}", "SELECT-PENDING", "import_journal");

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var timestampStr = reader.GetString(reader.GetOrdinal("timestamp"));
            var timestamp = DateTime.TryParse(timestampStr,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var ts) ? ts : DateTime.MinValue;

            result.Add(new PendingImportInfo(
                Id: reader.GetString(reader.GetOrdinal("id")),
                Timestamp: timestamp,
                SourcePath: reader.GetString(reader.GetOrdinal("source_path")),
                FileCount: reader.GetInt32(reader.GetOrdinal("file_count")),
                ProfileId: reader.IsDBNull(reader.GetOrdinal("profile_id")) ? null : reader.GetString(reader.GetOrdinal("profile_id")),
                MachineName: reader.IsDBNull(reader.GetOrdinal("machine_name")) ? null : reader.GetString(reader.GetOrdinal("machine_name")),
                CompletedActions: Convert.ToInt32(reader.GetValue(reader.GetOrdinal("completed_actions"))),
                FailedActions: Convert.ToInt32(reader.GetValue(reader.GetOrdinal("failed_actions"))),
                PendingActions: Convert.ToInt32(reader.GetValue(reader.GetOrdinal("pending_actions")))));
        }

        return result;
    }

    // ── Undo-Primitive (BPM-111.04) — nur letzter Import, Kap. 11 ──

    /// <summary>Id des letzten erfolgreich abgeschlossenen Imports (NULL wenn keiner).</summary>
    public string? GetLastCompletedImportId()
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id FROM import_journal
            WHERE status = 'completed'
            ORDER BY timestamp DESC LIMIT 1
            """;
        return cmd.ExecuteScalar() as string;
    }

    /// <summary>Revisionen, die dieser Import angelegt hat (fuer Undo-Soft-Delete).</summary>
    public List<(string RevisionId, string DocumentId)> GetRevisionsCreatedByImport(string importId)
    {
        var conn = GetConnection();
        var result = new List<(string, string)>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, document_id FROM plan_revisions
            WHERE last_import_id = @iid AND is_deleted = 0
            """;
        cmd.Parameters.AddWithValue("@iid", importId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add((reader.GetString(0), reader.GetString(1)));
        return result;
    }

    /// <summary>Revisionen, die durch diesen Import superseded wurden (via Audit-Events, BPM-109.04).</summary>
    public List<string> GetRevisionIdsSupersededByImport(string importId)
    {
        var conn = GetConnection();
        var result = new List<string>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT DISTINCT revision_id FROM plan_revision_events
            WHERE import_id = @iid AND event_type = 'superseded'
            """;
        cmd.Parameters.AddWithValue("@iid", importId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(reader.GetString(0));
        return result;
    }

    /// <summary>Soft Delete einer Revision (Undo: vom Import angelegte Revision zuruecknehmen).</summary>
    public void SoftDeleteRevision(string revisionId)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE plan_revisions SET is_deleted = 1 WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", revisionId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Setzt eine superseded Revision zurueck auf current (Undo des Supersede).</summary>
    public void RestoreRevisionToCurrent(string revisionId)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE plan_revisions
            SET revision_status = 'current', superseded_at = NULL
            WHERE id = @id AND is_deleted = 0
            """;
        cmd.Parameters.AddWithValue("@id", revisionId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Soft Delete eines Dokuments, wenn keine aktive Revision mehr existiert.</summary>
    public void SoftDeleteDocumentIfNoRevisions(string documentId)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE plan_documents SET is_deleted = 1
            WHERE id = @id AND NOT EXISTS (
                SELECT 1 FROM plan_revisions
                WHERE document_id = @id AND is_deleted = 0)
            """;
        cmd.Parameters.AddWithValue("@id", documentId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Markiert einen Import als rueckgaengig gemacht (Undo abgeschlossen).</summary>
    public void MarkImportUndone(string importId)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE import_journal SET status = 'undone' WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", importId);
        cmd.ExecuteNonQuery();
        Log.Information("Import {ImportId} als 'undone' markiert", importId);
    }

    public string GetDatabasePath() => _dbPath;

    public void Dispose()
    {
        Log.Debug("planmanager.db connection disposed");
        _connection?.Close();
        _connection?.Dispose();
        _connection = null;
    }
}
