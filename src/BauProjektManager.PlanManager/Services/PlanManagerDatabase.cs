using System.IO;
using Microsoft.Data.Sqlite;
using BauProjektManager.Domain.Interfaces;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// SQLite database service for planmanager.db — per project, local only.
/// Manages plan cache (revisions, files, links) and import journal.
/// Created lazily when PlanManager module opens a project.
/// Schema based on DB-SCHEMA.md Kap. 6 + Cross-Review 15.04.2026.
/// </summary>
public class PlanManagerDatabase : IDisposable
{
    private readonly string _dbPath;
    private readonly IIdGenerator _idGenerator;
    private SqliteConnection? _connection;

    public PlanManagerDatabase(string projectId, IIdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
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
        }
        return _connection;
    }

    private void EnsureTables()
    {
        Log.Debug("Creating planmanager.db tables (6 tables)");
        var conn = _connection!;
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            -- Plan Revisions Cache
            CREATE TABLE IF NOT EXISTS plan_revisions (
                id TEXT PRIMARY KEY,
                document_key TEXT NOT NULL,
                document_type_id TEXT,
                plan_number TEXT NOT NULL,
                plan_index TEXT,
                document_type TEXT NOT NULL,
                target_folder TEXT NOT NULL,
                relative_directory TEXT NOT NULL,
                index_source TEXT NOT NULL,
                revision_status TEXT NOT NULL,
                last_import_id TEXT,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_revision_current
            ON plan_revisions(document_key, revision_status)
            WHERE revision_status = 'current';

            -- Plan Files Cache
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

            -- Revision-File Links (n:m)
            CREATE TABLE IF NOT EXISTS revision_file_links (
                revision_id TEXT NOT NULL,
                file_id TEXT NOT NULL,
                link_mode TEXT NOT NULL,
                is_primary INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (revision_id, file_id),
                FOREIGN KEY (revision_id) REFERENCES plan_revisions(id),
                FOREIGN KEY (file_id) REFERENCES plan_files(id)
            );

            -- Import Journal
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

            -- Import Actions
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

            -- Import Action Files
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

            INSERT OR REPLACE INTO schema_version (version) VALUES ('1.0');
            """;
        cmd.ExecuteNonQuery();
    }

    // === PLAN REVISIONS ===

    /// <summary>
    /// Gets the current revision for a document_key (if exists).
    /// </summary>
    public ExistingRevision? GetCurrentRevision(string documentKey)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT pr.id, pr.plan_index, pf.md5_hash FROM plan_revisions pr
            JOIN plan_files pf ON pf.id = (
                SELECT file_id FROM revision_file_links
                WHERE revision_id = pr.id AND is_primary = 1
                LIMIT 1
            )
            WHERE pr.document_key = @key AND pr.revision_status = 'current'
            """;
        cmd.Parameters.AddWithValue("@key", documentKey);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new ExistingRevision(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.GetString(2));
    }

    /// <summary>
    /// Gets all existing revisions as a lookup for the decision service.
    /// </summary>
    public Dictionary<string, ExistingRevision> GetAllCurrentRevisions()
    {
        var conn = GetConnection();
        var result = new Dictionary<string, ExistingRevision>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT pr.document_key, pr.id, pr.plan_index,
                   COALESCE(pf.md5_hash, '') as md5
            FROM plan_revisions pr
            LEFT JOIN revision_file_links rfl ON rfl.revision_id = pr.id AND rfl.is_primary = 1
            LEFT JOIN plan_files pf ON pf.id = rfl.file_id
            WHERE pr.revision_status = 'current'
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var key = reader.GetString(0);
            result[key] = new ExistingRevision(
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetString(3));
        }
        Log.Debug("planmanager.db: {Count} aktuelle Revisionen geladen", result.Count);
        return result;
    }

    /// <summary>
    /// Inserts a new revision + file + link after import.
    /// </summary>
    public void InsertRevisionWithFile(
        string documentKey, string documentTypeId, string planNumber,
        string? planIndex, string documentType, string targetFolder,
        string relativeDirectory, string indexSource, string importId,
        string fileName, string relativePath, string fileType,
        string md5Hash, long fileSize)
    {
        var conn = GetConnection();
        var now = DateTime.UtcNow.ToString("o");
        var revId = _idGenerator.NewId();
        var fileId = _idGenerator.NewId();

        // Insert revision
        var revCmd = conn.CreateCommand();
        revCmd.CommandText = """
            INSERT INTO plan_revisions (id, document_key, document_type_id, plan_number,
                plan_index, document_type, target_folder, relative_directory,
                index_source, revision_status, last_import_id, created_at, updated_at)
            VALUES (@id, @dk, @dti, @pn, @pi, @dt, @tf, @rd, @is, 'current', @ii, @ca, @ua)
            """;
        revCmd.Parameters.AddWithValue("@id", revId);
        revCmd.Parameters.AddWithValue("@dk", documentKey);
        revCmd.Parameters.AddWithValue("@dti", (object?)documentTypeId ?? DBNull.Value);
        revCmd.Parameters.AddWithValue("@pn", planNumber);
        revCmd.Parameters.AddWithValue("@pi", (object?)planIndex ?? DBNull.Value);
        revCmd.Parameters.AddWithValue("@dt", documentType);
        revCmd.Parameters.AddWithValue("@tf", targetFolder);
        revCmd.Parameters.AddWithValue("@rd", relativeDirectory);
        revCmd.Parameters.AddWithValue("@is", indexSource);
        revCmd.Parameters.AddWithValue("@ii", importId);
        revCmd.Parameters.AddWithValue("@ca", now);
        revCmd.Parameters.AddWithValue("@ua", now);
        revCmd.ExecuteNonQuery();

        // Insert file
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

        // Link revision to file
        var linkCmd = conn.CreateCommand();
        linkCmd.CommandText = """
            INSERT INTO revision_file_links (revision_id, file_id, link_mode, is_primary)
            VALUES (@rid, @fid, 'auto', 1)
            """;
        linkCmd.Parameters.AddWithValue("@rid", revId);
        linkCmd.Parameters.AddWithValue("@fid", fileId);
        linkCmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Archives an existing revision (sets status to 'archived').
    /// </summary>
    public void ArchiveRevision(string revisionId)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE plan_revisions SET revision_status = 'archived',
                updated_at = @ua WHERE id = @id
            """;
        cmd.Parameters.AddWithValue("@id", revisionId);
        cmd.Parameters.AddWithValue("@ua", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Adds a file to an existing current revision (e.g. DWG to existing PDF revision).
    /// Used when document_key already has a 'current' revision.
    /// </summary>
    public void AddFileToExistingRevision(
        string documentKey,
        string fileName, string relativePath, string fileType,
        string md5Hash, long fileSize)
    {
        var conn = GetConnection();
        var now = DateTime.UtcNow.ToString("o");
        var fileId = _idGenerator.NewId();

        // Find existing revision
        var findCmd = conn.CreateCommand();
        findCmd.CommandText = "SELECT id FROM plan_revisions WHERE document_key = @dk AND revision_status = 'current'";
        findCmd.Parameters.AddWithValue("@dk", documentKey);
        var revId = findCmd.ExecuteScalar() as string;
        if (revId is null) return;

        // Insert file
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

        // Link to revision (not primary — primary is the first file)
        var linkCmd = conn.CreateCommand();
        linkCmd.CommandText = """
            INSERT INTO revision_file_links (revision_id, file_id, link_mode, is_primary)
            VALUES (@rid, @fid, 'auto', 0)
            """;
        linkCmd.Parameters.AddWithValue("@rid", revId);
        linkCmd.Parameters.AddWithValue("@fid", fileId);
        linkCmd.ExecuteNonQuery();

        Log.Information("Datei zu bestehender Revision gelinkt: {File} → {Key}", fileName, documentKey);
    }

    // === IMPORT JOURNAL ===

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
    /// Checks for pending import journals (for recovery on app start).
    /// </summary>
    public bool HasPendingImports()
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM import_journal WHERE status = 'pending'";
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
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
