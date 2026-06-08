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
    private readonly IIdGenerator _idGenerator;
    private readonly IPersistenceRegistry? _persistenceRegistry;
    private SqliteConnection? _connection;

    public PlanManagerDatabase(string projectId, IIdGenerator idGenerator, IPersistenceRegistry? persistenceRegistry = null)
    {
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
                destination_path TEXT NOT NULL,
                archive_path TEXT,
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

    // === PLAN REVISIONS (Cache-Schicht) ===
    //
    // BPM-109.01: plan_revisions wurde auf das Drei-Ebenen-Schema v2.0 umgebaut
    // (document_key → document_id-FK auf plan_documents, neue Status-Enum + Zeitstempel).
    // Die folgenden Cache-Repository-Methoden sind damit gegen das alte Schema nicht mehr gültig
    // und werden in BPM-109.02 (Domain Models + Repository) gegen das neue Modell reimplementiert
    // (inkl. Document-Resolve über document_key → plan_documents.id).
    // Bis dahin Fail-Fast statt stiller Falsch-SQL — schützt die Import-Journal-Invariante
    // (kein halb-geschriebener Cache-Zustand). Signaturen bleiben erhalten, damit die Aufrufer
    // (ImportExecutionService / ImportWorkflowService) kompilieren.

    private const string NotImplV2Message =
        "BPM-109.01: plan_revisions wurde auf das Drei-Ebenen-Schema v2.0 umgebaut (document_id). " +
        "Die Cache-Repository-Logik wird in BPM-109.02 gegen das neue Modell reimplementiert.";

    /// <summary>
    /// Gets the current revision for a document_key (if exists).
    /// BPM-109.02: gegen plan_documents/plan_revisions (document_id) reimplementieren.
    /// </summary>
    public ExistingRevision? GetCurrentRevision(string documentKey)
        => throw new NotSupportedException(NotImplV2Message);

    /// <summary>
    /// Gets all existing revisions as a lookup for the decision service.
    /// BPM-109.02: gegen plan_documents/plan_revisions (document_id) reimplementieren.
    /// </summary>
    public Dictionary<string, ExistingRevision> GetAllCurrentRevisions()
        => throw new NotSupportedException(NotImplV2Message);

    /// <summary>
    /// Inserts a new revision + file + link after import.
    /// BPM-109.02: Document-Resolve (document_key → plan_documents.id) + Revision mit document_id-FK.
    /// </summary>
    public void InsertRevisionWithFile(
        string documentKey, string documentTypeId, string planNumber,
        string? planIndex, string documentType, string targetFolder,
        string relativeDirectory, string indexSource, string importId,
        string fileName, string relativePath, string fileType,
        string md5Hash, long fileSize)
        => throw new NotSupportedException(NotImplV2Message);

    /// <summary>
    /// Archives an existing revision (Schema v2.0: superseded statt archived).
    /// BPM-109.02: revision_status='superseded' + superseded_at + plan_revision_events.
    /// </summary>
    public void ArchiveRevision(string revisionId)
        => throw new NotSupportedException(NotImplV2Message);

    /// <summary>
    /// Adds a file to an existing current revision (e.g. DWG to existing PDF revision).
    /// BPM-109.02: über document_id-Auflösung statt document_key.
    /// </summary>
    public void AddFileToExistingRevision(
        string documentKey,
        string fileName, string relativePath, string fileType,
        string md5Hash, long fileSize)
        => throw new NotSupportedException(NotImplV2Message);

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
        string receivedAt, string? lastImportId)
    {
        var conn = GetConnection();
        var id = _idGenerator.NewId();
        var now = DateTime.UtcNow.ToString("o");
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO plan_revisions (id, document_id, plan_index, index_source, revision_status,
                current_from, superseded_at, received_at, last_import_id,
                created_at, last_modified_at, sync_version, is_deleted)
            VALUES (@id, @did, @pi, @is, @st, @cf, @sa, @ra, @ii, @ca, @ua, 0, 0)
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@did", documentId);
        cmd.Parameters.AddWithValue("@pi", (object?)planIndex ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@is", indexSource);
        cmd.Parameters.AddWithValue("@st", revisionStatus);
        cmd.Parameters.AddWithValue("@cf", currentFrom);
        cmd.Parameters.AddWithValue("@sa", (object?)supersededAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ra", receivedAt);
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

    /// <summary>Lädt die aktuelle (current) Revision eines Dokuments (oder null).</summary>
    public PlanRevision? GetCurrentRevisionForDocument(string documentId)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, document_id, plan_index, index_source, revision_status,
                   current_from, superseded_at, received_at, last_import_id
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
            reader.IsDBNull(8) ? null : reader.GetString(8));
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
    /// </summary>
    public string InsertImportAction(
        string importId, int actionOrder, string actionType,
        string? documentKey, string planNumber, string? planIndex,
        string? oldIndex, string sourcePath, string destinationPath,
        string? archivePath)
    {
        var conn = GetConnection();
        var id = _idGenerator.NewId();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO import_actions (id, import_id, action_order, action_type,
                action_status, document_key, plan_number, plan_index, old_index,
                source_path, destination_path, archive_path)
            VALUES (@id, @iid, @ao, @at, 'pending', @dk, @pn, @pi, @oi, @sp, @dp, @ap)
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
        cmd.Parameters.AddWithValue("@dp", destinationPath);
        cmd.Parameters.AddWithValue("@ap", (object?)archivePath ?? DBNull.Value);
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
                SELECT id, action_type, action_status, source_path, destination_path, archive_path
                FROM import_actions WHERE import_id = @iid
                ORDER BY action_order ASC
                """;
        }
        else
        {
            cmd.CommandText = """
                SELECT id, action_type, action_status, source_path, destination_path, archive_path
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
                DestinationPath: reader.GetString(reader.GetOrdinal("destination_path")),
                ArchivePath: reader.IsDBNull(reader.GetOrdinal("archive_path"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("archive_path"))));
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

    public string GetDatabasePath() => _dbPath;

    public void Dispose()
    {
        Log.Debug("planmanager.db connection disposed");
        _connection?.Close();
        _connection?.Dispose();
        _connection = null;
    }
}
