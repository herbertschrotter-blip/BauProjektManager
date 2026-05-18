using Microsoft.Data.Sqlite;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// SQLite-Repository fuer <c>segment_types</c> und <c>segment_type_groups</c> in bpm.db (BPM-108 Phase A).
/// Soft-Delete: setzt <c>is_deleted = 1</c>. Built-ins werden nie hart geloescht.
/// </summary>
public class SegmentTypeRepository : ISegmentTypeRepository
{
    private readonly string _connectionString;
    private readonly IUserContext _userContext;

    public SegmentTypeRepository(ProjectDatabase database, IUserContext userContext)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(userContext);
        database.EnsureInitialized();
        _connectionString = $"Data Source={database.GetDatabasePath()}";
        _userContext = userContext;
    }

    /// <summary>
    /// Konstruktor fuer Tests / isolierte Anwendungen: nutzt eine Connection-String direkt.
    /// Aufrufer muss vorher <see cref="CreateTables"/> ausgefuehrt haben.
    /// </summary>
    public SegmentTypeRepository(string connectionString, IUserContext userContext)
    {
        _connectionString = connectionString;
        _userContext = userContext;
    }

    /// <summary>
    /// Legt <c>segment_type_groups</c>, <c>segment_types</c> + Indizes an.
    /// Idempotent — fuer Tests oder isolierte Anwendungen ohne <see cref="ProjectDatabase"/>.
    /// </summary>
    public static void CreateTables(SqliteConnection conn)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS segment_type_groups (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                sort_order INTEGER NOT NULL DEFAULT 0,
                is_active INTEGER NOT NULL DEFAULT 1,
                is_builtin INTEGER NOT NULL DEFAULT 0,
                builtin_version INTEGER NOT NULL DEFAULT 1,
                user_modified_name INTEGER NOT NULL DEFAULT 0,
                user_modified_sort INTEGER NOT NULL DEFAULT 0,
                user_modified_active INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                created_by TEXT NOT NULL DEFAULT '',
                last_modified_at TEXT NOT NULL,
                last_modified_by TEXT NOT NULL DEFAULT '',
                sync_version INTEGER NOT NULL DEFAULT 0,
                is_deleted INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS segment_types (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                color TEXT NOT NULL,
                token_key TEXT NOT NULL,
                semantic_role TEXT,
                group_id TEXT NOT NULL,
                sort_order INTEGER NOT NULL DEFAULT 0,
                is_active INTEGER NOT NULL DEFAULT 1,
                is_builtin INTEGER NOT NULL DEFAULT 0,
                builtin_version INTEGER NOT NULL DEFAULT 1,
                user_modified_name INTEGER NOT NULL DEFAULT 0,
                user_modified_color INTEGER NOT NULL DEFAULT 0,
                user_modified_sort INTEGER NOT NULL DEFAULT 0,
                user_modified_active INTEGER NOT NULL DEFAULT 0,
                user_modified_group INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                created_by TEXT NOT NULL DEFAULT '',
                last_modified_at TEXT NOT NULL,
                last_modified_by TEXT NOT NULL DEFAULT '',
                sync_version INTEGER NOT NULL DEFAULT 0,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (group_id) REFERENCES segment_type_groups(id)
            );

            CREATE INDEX IF NOT EXISTS idx_segment_types_group_id ON segment_types(group_id);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_segment_types_token_key_active
                ON segment_types(token_key) WHERE is_deleted = 0;
            """;
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys=ON;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    // === GROUPS ===

    public IReadOnlyList<SegmentTypeGroupDefinition> LoadAllGroups(bool includeDeleted = false)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = includeDeleted
            ? "SELECT * FROM segment_type_groups ORDER BY sort_order"
            : "SELECT * FROM segment_type_groups WHERE is_deleted = 0 ORDER BY sort_order";
        Log.Verbose("Executing SQL: {Operation} on {Table}", "SELECT", "segment_type_groups");
        var result = new List<SegmentTypeGroupDefinition>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) result.Add(ReadGroup(reader));
        return result;
    }

    public SegmentTypeGroupDefinition? GetGroup(string id, bool includeDeleted = false)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = includeDeleted
            ? "SELECT * FROM segment_type_groups WHERE id = @id"
            : "SELECT * FROM segment_type_groups WHERE id = @id AND is_deleted = 0";
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadGroup(reader) : null;
    }

    public void SaveGroup(SegmentTypeGroupDefinition group)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO segment_type_groups (
                id, name, sort_order, is_active, is_builtin, builtin_version,
                user_modified_name, user_modified_sort, user_modified_active,
                created_at, created_by, last_modified_at, last_modified_by, sync_version, is_deleted
            ) VALUES (
                @id, @name, @sort_order, @is_active, @is_builtin, @builtin_version,
                @user_modified_name, @user_modified_sort, @user_modified_active,
                @now, @user, @now, @user, 0, 0
            )
            ON CONFLICT(id) DO UPDATE SET
                name = @name,
                sort_order = @sort_order,
                is_active = @is_active,
                builtin_version = @builtin_version,
                user_modified_name = @user_modified_name,
                user_modified_sort = @user_modified_sort,
                user_modified_active = @user_modified_active,
                last_modified_at = @now,
                last_modified_by = @user,
                sync_version = sync_version + 1
            """;
        var utcNow = DateTime.UtcNow.ToString("o");
        cmd.Parameters.AddWithValue("@now", utcNow);
        cmd.Parameters.AddWithValue("@user", _userContext.DisplayName);
        cmd.Parameters.AddWithValue("@id", group.Id);
        cmd.Parameters.AddWithValue("@name", group.Name);
        cmd.Parameters.AddWithValue("@sort_order", group.SortOrder);
        cmd.Parameters.AddWithValue("@is_active", group.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@is_builtin", group.IsBuiltin ? 1 : 0);
        cmd.Parameters.AddWithValue("@builtin_version", group.BuiltinVersion);
        cmd.Parameters.AddWithValue("@user_modified_name", group.UserModifiedName ? 1 : 0);
        cmd.Parameters.AddWithValue("@user_modified_sort", group.UserModifiedSort ? 1 : 0);
        cmd.Parameters.AddWithValue("@user_modified_active", group.UserModifiedActive ? 1 : 0);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "UPSERT", "segment_type_groups");
        cmd.ExecuteNonQuery();
    }

    public void SoftDeleteGroup(string id)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE segment_type_groups
            SET is_deleted = 1,
                last_modified_at = @now,
                last_modified_by = @user,
                sync_version = sync_version + 1
            WHERE id = @id AND is_deleted = 0
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@user", _userContext.DisplayName);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "SOFT-DELETE", "segment_type_groups");
        cmd.ExecuteNonQuery();
    }

    private static SegmentTypeGroupDefinition ReadGroup(SqliteDataReader r) => new()
    {
        Id = r.GetString(r.GetOrdinal("id")),
        Name = r.GetString(r.GetOrdinal("name")),
        SortOrder = r.GetInt32(r.GetOrdinal("sort_order")),
        IsActive = r.GetInt32(r.GetOrdinal("is_active")) == 1,
        IsBuiltin = r.GetInt32(r.GetOrdinal("is_builtin")) == 1,
        BuiltinVersion = r.GetInt32(r.GetOrdinal("builtin_version")),
        UserModifiedName = r.GetInt32(r.GetOrdinal("user_modified_name")) == 1,
        UserModifiedSort = r.GetInt32(r.GetOrdinal("user_modified_sort")) == 1,
        UserModifiedActive = r.GetInt32(r.GetOrdinal("user_modified_active")) == 1,
        CreatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("created_at"))),
        CreatedBy = r.GetString(r.GetOrdinal("created_by")),
        LastModifiedAt = DateTime.Parse(r.GetString(r.GetOrdinal("last_modified_at"))),
        LastModifiedBy = r.GetString(r.GetOrdinal("last_modified_by")),
        SyncVersion = r.GetInt32(r.GetOrdinal("sync_version")),
        IsDeleted = r.GetInt32(r.GetOrdinal("is_deleted")) == 1
    };

    // === TYPES ===

    public IReadOnlyList<SegmentTypeDefinition> LoadAllTypes(bool includeDeleted = false)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = includeDeleted
            ? "SELECT * FROM segment_types ORDER BY sort_order"
            : "SELECT * FROM segment_types WHERE is_deleted = 0 ORDER BY sort_order";
        Log.Verbose("Executing SQL: {Operation} on {Table}", "SELECT", "segment_types");
        var result = new List<SegmentTypeDefinition>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) result.Add(ReadType(reader));
        return result;
    }

    public SegmentTypeDefinition? GetType(string id, bool includeDeleted = false)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = includeDeleted
            ? "SELECT * FROM segment_types WHERE id = @id"
            : "SELECT * FROM segment_types WHERE id = @id AND is_deleted = 0";
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadType(reader) : null;
    }

    public void SaveType(SegmentTypeDefinition type)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO segment_types (
                id, name, color, token_key, semantic_role, group_id, sort_order, is_active,
                is_builtin, builtin_version,
                user_modified_name, user_modified_color, user_modified_sort,
                user_modified_active, user_modified_group,
                created_at, created_by, last_modified_at, last_modified_by, sync_version, is_deleted
            ) VALUES (
                @id, @name, @color, @token_key, @semantic_role, @group_id, @sort_order, @is_active,
                @is_builtin, @builtin_version,
                @user_modified_name, @user_modified_color, @user_modified_sort,
                @user_modified_active, @user_modified_group,
                @now, @user, @now, @user, 0, 0
            )
            ON CONFLICT(id) DO UPDATE SET
                name = @name,
                color = @color,
                group_id = @group_id,
                sort_order = @sort_order,
                is_active = @is_active,
                builtin_version = @builtin_version,
                user_modified_name = @user_modified_name,
                user_modified_color = @user_modified_color,
                user_modified_sort = @user_modified_sort,
                user_modified_active = @user_modified_active,
                user_modified_group = @user_modified_group,
                last_modified_at = @now,
                last_modified_by = @user,
                sync_version = sync_version + 1
            """;
        var utcNow = DateTime.UtcNow.ToString("o");
        cmd.Parameters.AddWithValue("@now", utcNow);
        cmd.Parameters.AddWithValue("@user", _userContext.DisplayName);
        cmd.Parameters.AddWithValue("@id", type.Id);
        cmd.Parameters.AddWithValue("@name", type.Name);
        cmd.Parameters.AddWithValue("@color", type.Color);
        cmd.Parameters.AddWithValue("@token_key", type.TokenKey);
        cmd.Parameters.AddWithValue("@semantic_role", (object?)type.SemanticRole?.ToString() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@group_id", type.GroupId);
        cmd.Parameters.AddWithValue("@sort_order", type.SortOrder);
        cmd.Parameters.AddWithValue("@is_active", type.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@is_builtin", type.IsBuiltin ? 1 : 0);
        cmd.Parameters.AddWithValue("@builtin_version", type.BuiltinVersion);
        cmd.Parameters.AddWithValue("@user_modified_name", type.UserModifiedName ? 1 : 0);
        cmd.Parameters.AddWithValue("@user_modified_color", type.UserModifiedColor ? 1 : 0);
        cmd.Parameters.AddWithValue("@user_modified_sort", type.UserModifiedSort ? 1 : 0);
        cmd.Parameters.AddWithValue("@user_modified_active", type.UserModifiedActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@user_modified_group", type.UserModifiedGroup ? 1 : 0);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "UPSERT", "segment_types");
        cmd.ExecuteNonQuery();
    }

    public void SoftDeleteType(string id)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE segment_types
            SET is_deleted = 1,
                last_modified_at = @now,
                last_modified_by = @user,
                sync_version = sync_version + 1
            WHERE id = @id AND is_deleted = 0
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@user", _userContext.DisplayName);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "SOFT-DELETE", "segment_types");
        cmd.ExecuteNonQuery();
    }

    public bool TokenKeyExists(string tokenKey, string? excludingId = null)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        if (excludingId is null)
        {
            cmd.CommandText = "SELECT COUNT(*) FROM segment_types WHERE token_key = @token_key AND is_deleted = 0";
        }
        else
        {
            cmd.CommandText = "SELECT COUNT(*) FROM segment_types WHERE token_key = @token_key AND id != @id AND is_deleted = 0";
            cmd.Parameters.AddWithValue("@id", excludingId);
        }
        cmd.Parameters.AddWithValue("@token_key", tokenKey);
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        return count > 0;
    }

    private static SegmentTypeDefinition ReadType(SqliteDataReader r)
    {
        var roleOrdinal = r.GetOrdinal("semantic_role");
        SegmentSemanticRole? role = r.IsDBNull(roleOrdinal)
            ? null
            : Enum.TryParse<SegmentSemanticRole>(r.GetString(roleOrdinal), out var parsed) ? parsed : SegmentSemanticRole.None;

        return new SegmentTypeDefinition
        {
            Id = r.GetString(r.GetOrdinal("id")),
            Name = r.GetString(r.GetOrdinal("name")),
            Color = r.GetString(r.GetOrdinal("color")),
            TokenKey = r.GetString(r.GetOrdinal("token_key")),
            SemanticRole = role,
            GroupId = r.GetString(r.GetOrdinal("group_id")),
            SortOrder = r.GetInt32(r.GetOrdinal("sort_order")),
            IsActive = r.GetInt32(r.GetOrdinal("is_active")) == 1,
            IsBuiltin = r.GetInt32(r.GetOrdinal("is_builtin")) == 1,
            BuiltinVersion = r.GetInt32(r.GetOrdinal("builtin_version")),
            UserModifiedName = r.GetInt32(r.GetOrdinal("user_modified_name")) == 1,
            UserModifiedColor = r.GetInt32(r.GetOrdinal("user_modified_color")) == 1,
            UserModifiedSort = r.GetInt32(r.GetOrdinal("user_modified_sort")) == 1,
            UserModifiedActive = r.GetInt32(r.GetOrdinal("user_modified_active")) == 1,
            UserModifiedGroup = r.GetInt32(r.GetOrdinal("user_modified_group")) == 1,
            CreatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("created_at"))),
            CreatedBy = r.GetString(r.GetOrdinal("created_by")),
            LastModifiedAt = DateTime.Parse(r.GetString(r.GetOrdinal("last_modified_at"))),
            LastModifiedBy = r.GetString(r.GetOrdinal("last_modified_by")),
            SyncVersion = r.GetInt32(r.GetOrdinal("sync_version")),
            IsDeleted = r.GetInt32(r.GetOrdinal("is_deleted")) == 1
        };
    }
}
