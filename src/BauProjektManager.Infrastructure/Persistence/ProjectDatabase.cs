using System.IO;
using Microsoft.Data.Sqlite;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using Serilog;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// SQLite database service — manages bpm.db in %LocalAppData%\BauProjektManager\.
/// Schema v2.1: ULID + Sync-Spalten (created_by, last_modified_at/by, sync_version, is_deleted) auf allen Tabellen.
/// ID generation via IIdGenerator (ADR-039 v2).
/// </summary>
public class ProjectDatabase : IDisposable
{
    private readonly string _dbPath;
    private readonly IIdGenerator _idGenerator;
    private readonly IUserContext _userContext;
    private SqliteConnection? _connection;

    public ProjectDatabase(IIdGenerator idGenerator, IUserContext userContext)
    {
        _idGenerator = idGenerator;
        _userContext = userContext;
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BauProjektManager");
        Directory.CreateDirectory(appData);
        _dbPath = Path.Combine(appData, "bpm.db");
    }

    private SqliteConnection GetConnection()
    {
        if (_connection is null)
        {
            _connection = new SqliteConnection($"Data Source={_dbPath}");
            _connection.Open();
            Log.Debug("Database initialized at {Path}", _dbPath);
            using var walCmd = _connection.CreateCommand();
            walCmd.CommandText = "PRAGMA journal_mode=WAL;";
            walCmd.ExecuteNonQuery();
            using var fkCmd = _connection.CreateCommand();
            fkCmd.CommandText = "PRAGMA foreign_keys=ON;";
            fkCmd.ExecuteNonQuery();
            EnsureTables();
            MigrateSchema();
        }
        return _connection;
    }

    private void EnsureTables()
    {
        Log.Debug("Creating database tables (schema v2.1 Sync)");
        var conn = _connection!;
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS clients (
                id TEXT PRIMARY KEY,
                company TEXT NOT NULL DEFAULT '',
                contact_person TEXT NOT NULL DEFAULT '',
                phone TEXT NOT NULL DEFAULT '',
                email TEXT NOT NULL DEFAULT '',
                notes TEXT NOT NULL DEFAULT '',
                created_at TEXT NOT NULL,
                created_by TEXT NOT NULL DEFAULT '',
                last_modified_at TEXT NOT NULL,
                last_modified_by TEXT NOT NULL DEFAULT '',
                sync_version INTEGER NOT NULL DEFAULT 0,
                is_deleted INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS projects (
                id TEXT PRIMARY KEY,
                project_number TEXT NOT NULL DEFAULT '',
                name TEXT NOT NULL DEFAULT '',
                full_name TEXT NOT NULL DEFAULT '',
                status TEXT NOT NULL DEFAULT 'Active',
                project_type TEXT NOT NULL DEFAULT '',
                client_id TEXT,
                street TEXT NOT NULL DEFAULT '',
                house_number TEXT NOT NULL DEFAULT '',
                postal_code TEXT NOT NULL DEFAULT '',
                city TEXT NOT NULL DEFAULT '',
                municipality TEXT NOT NULL DEFAULT '',
                district TEXT NOT NULL DEFAULT '',
                state TEXT NOT NULL DEFAULT 'Steiermark',
                coordinate_system TEXT NOT NULL DEFAULT 'EPSG:31258',
                coordinate_east REAL NOT NULL DEFAULT 0,
                coordinate_north REAL NOT NULL DEFAULT 0,
                cadastral_kg TEXT NOT NULL DEFAULT '',
                cadastral_kg_name TEXT NOT NULL DEFAULT '',
                cadastral_gst TEXT NOT NULL DEFAULT '',
                project_start TEXT,
                construction_start TEXT,
                planned_end TEXT,
                actual_end TEXT,
                root_path TEXT NOT NULL DEFAULT '',
                plans_path TEXT NOT NULL DEFAULT '',
                inbox_path TEXT NOT NULL DEFAULT '',
                photos_path TEXT NOT NULL DEFAULT '',
                documents_path TEXT NOT NULL DEFAULT '',
                protocols_path TEXT NOT NULL DEFAULT '',
                invoices_path TEXT NOT NULL DEFAULT '',
                use_global_zero_level INTEGER NOT NULL DEFAULT 0,
                global_zero_level REAL NOT NULL DEFAULT 0,
                tags TEXT NOT NULL DEFAULT '',
                notes TEXT NOT NULL DEFAULT '',
                created_at TEXT NOT NULL,
                created_by TEXT NOT NULL DEFAULT '',
                last_modified_at TEXT NOT NULL,
                last_modified_by TEXT NOT NULL DEFAULT '',
                sync_version INTEGER NOT NULL DEFAULT 0,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (client_id) REFERENCES clients(id)
            );

            CREATE TABLE IF NOT EXISTS buildings (
                id TEXT PRIMARY KEY,
                project_id TEXT NOT NULL,
                name TEXT NOT NULL DEFAULT '',
                short_name TEXT NOT NULL DEFAULT '',
                type TEXT NOT NULL DEFAULT '',
                levels TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS building_parts (
                id TEXT PRIMARY KEY,
                project_id TEXT NOT NULL,
                short_name TEXT NOT NULL DEFAULT '',
                description TEXT NOT NULL DEFAULT '',
                building_type TEXT NOT NULL DEFAULT '',
                zero_level_absolute REAL NOT NULL DEFAULT 0,
                sort_order INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                created_by TEXT NOT NULL DEFAULT '',
                last_modified_at TEXT NOT NULL,
                last_modified_by TEXT NOT NULL DEFAULT '',
                sync_version INTEGER NOT NULL DEFAULT 0,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS building_levels (
                id TEXT PRIMARY KEY,
                building_part_id TEXT NOT NULL,
                prefix INTEGER NOT NULL DEFAULT 0,
                name TEXT NOT NULL DEFAULT '',
                description TEXT NOT NULL DEFAULT '',
                rdok REAL NOT NULL DEFAULT 0,
                fbok REAL NOT NULL DEFAULT 0,
                rduk REAL,
                sort_order INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                created_by TEXT NOT NULL DEFAULT '',
                last_modified_at TEXT NOT NULL,
                last_modified_by TEXT NOT NULL DEFAULT '',
                sync_version INTEGER NOT NULL DEFAULT 0,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (building_part_id) REFERENCES building_parts(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS project_participants (
                id TEXT PRIMARY KEY,
                project_id TEXT NOT NULL,
                role TEXT NOT NULL DEFAULT '',
                company TEXT NOT NULL DEFAULT '',
                contact_person TEXT NOT NULL DEFAULT '',
                phone TEXT NOT NULL DEFAULT '',
                email TEXT NOT NULL DEFAULT '',
                contact_id TEXT NOT NULL DEFAULT '',
                sort_order INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                created_by TEXT NOT NULL DEFAULT '',
                last_modified_at TEXT NOT NULL,
                last_modified_by TEXT NOT NULL DEFAULT '',
                sync_version INTEGER NOT NULL DEFAULT 0,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS project_links (
                id TEXT PRIMARY KEY,
                project_id TEXT NOT NULL,
                name TEXT NOT NULL DEFAULT '',
                url TEXT NOT NULL DEFAULT '',
                link_type TEXT NOT NULL DEFAULT 'Custom',
                sort_order INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                created_by TEXT NOT NULL DEFAULT '',
                last_modified_at TEXT NOT NULL,
                last_modified_by TEXT NOT NULL DEFAULT '',
                sync_version INTEGER NOT NULL DEFAULT 0,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS schema_version (
                version TEXT NOT NULL
            );

            -- FK-Indizes (ADR-039 v2)
            CREATE INDEX IF NOT EXISTS idx_building_parts_project_id ON building_parts(project_id);
            CREATE INDEX IF NOT EXISTS idx_building_levels_part_id ON building_levels(building_part_id);
            CREATE INDEX IF NOT EXISTS idx_participants_project_id ON project_participants(project_id);
            CREATE INDEX IF NOT EXISTS idx_links_project_id ON project_links(project_id);
            """;
        cmd.ExecuteNonQuery();
    }

    private void MigrateSchema()
    {
        var conn = _connection!;
        var verCmd = conn.CreateCommand();
        verCmd.CommandText = "DELETE FROM schema_version; INSERT INTO schema_version (version) VALUES ('2.1');";
        Log.Verbose("Executing SQL: {Operation} on {Table}", "UPDATE", "schema_version");
        verCmd.ExecuteNonQuery();
    }

    // === PROJECTS ===

    public List<Project> LoadAllProjects()
    {
        var conn = GetConnection();
        var projects = new List<Project>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT p.*, c.company, c.contact_person, c.phone, c.email, c.notes as client_notes
            FROM projects p
            LEFT JOIN clients c ON p.client_id = c.id
            ORDER BY p.project_number DESC
            """;
        Log.Verbose("Executing SQL: {Operation} on {Table}", "SELECT", "projects");
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var project = ReadProject(reader);
            project.BuildingParts = LoadBuildingParts(project.Id);
            project.Participants = LoadParticipants(project.Id);
            project.Links = LoadLinks(project.Id);
            projects.Add(project);
        }
        return projects;
    }

    public void SaveProject(Project project)
    {
        var conn = GetConnection();
        bool isNew = string.IsNullOrEmpty(project.Id) || !ProjectExists(project.Id);
        if (isNew) project.Id = _idGenerator.NewId();

        string? clientId = null;
        if (!string.IsNullOrEmpty(project.Client.Company) || !string.IsNullOrEmpty(project.Client.ContactPerson))
            clientId = SaveClient(project.Client, project.Id);

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO projects (
                id, project_number, name, full_name, status, project_type, client_id,
                street, house_number, postal_code, city,
                municipality, district, state,
                coordinate_system, coordinate_east, coordinate_north,
                cadastral_kg, cadastral_kg_name, cadastral_gst,
                project_start, construction_start, planned_end, actual_end,
                root_path, plans_path, inbox_path, photos_path,
                documents_path, protocols_path, invoices_path,
                use_global_zero_level, global_zero_level,
                tags, notes, created_at, created_by, last_modified_at, last_modified_by, sync_version
            ) VALUES (
                @id, @project_number, @name, @full_name, @status, @project_type, @client_id,
                @street, @house_number, @postal_code, @city,
                @municipality, @district, @state,
                @coordinate_system, @coordinate_east, @coordinate_north,
                @cadastral_kg, @cadastral_kg_name, @cadastral_gst,
                @project_start, @construction_start, @planned_end, @actual_end,
                @root_path, @plans_path, @inbox_path, @photos_path,
                @documents_path, @protocols_path, @invoices_path,
                @use_global_zero_level, @global_zero_level,
                @tags, @notes, @now, @user, @now, @user, 0
            )
            ON CONFLICT(id) DO UPDATE SET
                project_number=@project_number, name=@name, full_name=@full_name,
                status=@status, project_type=@project_type, client_id=@client_id,
                street=@street, house_number=@house_number,
                postal_code=@postal_code, city=@city,
                municipality=@municipality, district=@district, state=@state,
                coordinate_system=@coordinate_system,
                coordinate_east=@coordinate_east, coordinate_north=@coordinate_north,
                cadastral_kg=@cadastral_kg, cadastral_kg_name=@cadastral_kg_name,
                cadastral_gst=@cadastral_gst,
                project_start=@project_start, construction_start=@construction_start,
                planned_end=@planned_end, actual_end=@actual_end,
                root_path=@root_path, plans_path=@plans_path,
                inbox_path=@inbox_path, photos_path=@photos_path,
                documents_path=@documents_path, protocols_path=@protocols_path,
                invoices_path=@invoices_path,
                use_global_zero_level=@use_global_zero_level,
                global_zero_level=@global_zero_level,
                tags=@tags, notes=@notes,
                last_modified_at=@now, last_modified_by=@user,
                sync_version=sync_version+1
            """;
        var utcNow = DateTime.UtcNow.ToString("o");
        cmd.Parameters.AddWithValue("@now", utcNow);
        cmd.Parameters.AddWithValue("@user", _userContext.DisplayName);
        cmd.Parameters.AddWithValue("@id", project.Id);
        cmd.Parameters.AddWithValue("@project_number", project.ProjectNumber);
        cmd.Parameters.AddWithValue("@name", project.Name);
        cmd.Parameters.AddWithValue("@full_name", project.FullName);
        cmd.Parameters.AddWithValue("@status", project.Status.ToString());
        cmd.Parameters.AddWithValue("@project_type", project.ProjectType);
        cmd.Parameters.AddWithValue("@client_id", (object?)clientId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@street", project.Location.Street);
        cmd.Parameters.AddWithValue("@house_number", project.Location.HouseNumber);
        cmd.Parameters.AddWithValue("@postal_code", project.Location.PostalCode);
        cmd.Parameters.AddWithValue("@city", project.Location.City);
        cmd.Parameters.AddWithValue("@municipality", project.Location.Municipality);
        cmd.Parameters.AddWithValue("@district", project.Location.District);
        cmd.Parameters.AddWithValue("@state", project.Location.State);
        cmd.Parameters.AddWithValue("@coordinate_system", project.Location.CoordinateSystem);
        cmd.Parameters.AddWithValue("@coordinate_east", project.Location.CoordinateEast);
        cmd.Parameters.AddWithValue("@coordinate_north", project.Location.CoordinateNorth);
        cmd.Parameters.AddWithValue("@cadastral_kg", project.Location.CadastralKg);
        cmd.Parameters.AddWithValue("@cadastral_kg_name", project.Location.CadastralKgName);
        cmd.Parameters.AddWithValue("@cadastral_gst", project.Location.CadastralGst);
        cmd.Parameters.AddWithValue("@project_start", (object?)project.Timeline.ProjectStart?.ToString("yyyy-MM-dd") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@construction_start", (object?)project.Timeline.ConstructionStart?.ToString("yyyy-MM-dd") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@planned_end", (object?)project.Timeline.PlannedEnd?.ToString("yyyy-MM-dd") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@actual_end", (object?)project.Timeline.ActualEnd?.ToString("yyyy-MM-dd") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@root_path", project.Paths.Root);
        cmd.Parameters.AddWithValue("@plans_path", project.Paths.Plans);
        cmd.Parameters.AddWithValue("@inbox_path", project.Paths.Inbox);
        cmd.Parameters.AddWithValue("@photos_path", project.Paths.Photos);
        cmd.Parameters.AddWithValue("@documents_path", project.Paths.Documents);
        cmd.Parameters.AddWithValue("@protocols_path", project.Paths.Protocols);
        cmd.Parameters.AddWithValue("@invoices_path", project.Paths.Invoices);
        cmd.Parameters.AddWithValue("@use_global_zero_level", project.UseGlobalZeroLevel ? 1 : 0);
        cmd.Parameters.AddWithValue("@global_zero_level", project.GlobalZeroLevel);
        cmd.Parameters.AddWithValue("@tags", project.Tags);
        cmd.Parameters.AddWithValue("@notes", project.Notes);
        Log.Verbose("Executing SQL: {Operation} on {Table}", isNew ? "INSERT" : "UPDATE", "projects");
        cmd.ExecuteNonQuery();

        SaveBuildingParts(project.Id, project.BuildingParts);
        SaveParticipants(project.Id, project.Participants);
        SaveLinks(project.Id, project.Links);
    }

    public bool ProjectExistsByPath(string rootPath)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM projects WHERE root_path = @path";
        cmd.Parameters.AddWithValue("@path", rootPath);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private bool ProjectExists(string projectId)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM projects WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", projectId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "SELECT", "projects");
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    public void DeleteProject(string projectId)
    {
        var conn = GetConnection();

        var linkCmd = conn.CreateCommand();
        linkCmd.CommandText = "DELETE FROM project_links WHERE project_id = @id";
        linkCmd.Parameters.AddWithValue("@id", projectId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "DELETE", "project_links");
        linkCmd.ExecuteNonQuery();

        var ppartCmd = conn.CreateCommand();
        ppartCmd.CommandText = "DELETE FROM project_participants WHERE project_id = @id";
        ppartCmd.Parameters.AddWithValue("@id", projectId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "DELETE", "project_participants");
        ppartCmd.ExecuteNonQuery();

        var lvlCmd = conn.CreateCommand();
        lvlCmd.CommandText = "DELETE FROM building_levels WHERE building_part_id IN (SELECT id FROM building_parts WHERE project_id = @id)";
        lvlCmd.Parameters.AddWithValue("@id", projectId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "DELETE", "building_levels");
        lvlCmd.ExecuteNonQuery();

        var partCmd = conn.CreateCommand();
        partCmd.CommandText = "DELETE FROM building_parts WHERE project_id = @id";
        partCmd.Parameters.AddWithValue("@id", projectId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "DELETE", "building_parts");
        partCmd.ExecuteNonQuery();

        var bldgCmd = conn.CreateCommand();
        bldgCmd.CommandText = "DELETE FROM buildings WHERE project_id = @id";
        bldgCmd.Parameters.AddWithValue("@id", projectId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "DELETE", "buildings");
        bldgCmd.ExecuteNonQuery();

        var projCmd = conn.CreateCommand();
        projCmd.CommandText = "DELETE FROM projects WHERE id = @id";
        projCmd.Parameters.AddWithValue("@id", projectId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "DELETE", "projects");
        projCmd.ExecuteNonQuery();
    }

    // === CLIENTS ===

    private string SaveClient(Client client, string projectId)
    {
        var conn = GetConnection();
        var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT client_id FROM projects WHERE id = @id";
        checkCmd.Parameters.AddWithValue("@id", projectId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "SELECT", "projects");
        var existingClientId = checkCmd.ExecuteScalar() as string;
        string clientId = !string.IsNullOrEmpty(existingClientId) ? existingClientId : _idGenerator.NewId();

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO clients (id, company, contact_person, phone, email, notes, created_at, created_by, last_modified_at, last_modified_by, sync_version)
            VALUES (@id, @company, @contact_person, @phone, @email, @notes, @now, @user, @now, @user, 0)
            ON CONFLICT(id) DO UPDATE SET
                company=@company, contact_person=@contact_person,
                phone=@phone, email=@email, notes=@notes,
                last_modified_at=@now, last_modified_by=@user,
                sync_version=sync_version+1
            """;
        var utcNow = DateTime.UtcNow.ToString("o");
        cmd.Parameters.AddWithValue("@now", utcNow);
        cmd.Parameters.AddWithValue("@user", _userContext.DisplayName);
        cmd.Parameters.AddWithValue("@id", clientId);
        cmd.Parameters.AddWithValue("@company", client.Company);
        cmd.Parameters.AddWithValue("@contact_person", client.ContactPerson);
        cmd.Parameters.AddWithValue("@phone", client.Phone);
        cmd.Parameters.AddWithValue("@email", client.Email);
        cmd.Parameters.AddWithValue("@notes", client.Notes);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "INSERT", "clients");
        cmd.ExecuteNonQuery();
        return clientId;
    }

    // === BUILDING PARTS + LEVELS ===

    private List<BuildingPart> LoadBuildingParts(string projectId)
    {
        var conn = GetConnection();
        var parts = new List<BuildingPart>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM building_parts WHERE project_id = @pid ORDER BY sort_order";
        cmd.Parameters.AddWithValue("@pid", projectId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "SELECT", "building_parts");
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var part = new BuildingPart
            {
                Id = reader.GetString(reader.GetOrdinal("id")),
                ShortName = reader.GetString(reader.GetOrdinal("short_name")),
                Description = reader.GetString(reader.GetOrdinal("description")),
                BuildingType = reader.GetString(reader.GetOrdinal("building_type")),
                ZeroLevelAbsolute = reader.GetDouble(reader.GetOrdinal("zero_level_absolute")),
                SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order"))
            };
            part.Levels = LoadBuildingLevels(part.Id);
            parts.Add(part);
        }
        return parts;
    }

    private List<BuildingLevel> LoadBuildingLevels(string buildingPartId)
    {
        var conn = GetConnection();
        var levels = new List<BuildingLevel>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM building_levels WHERE building_part_id = @bpid ORDER BY sort_order";
        cmd.Parameters.AddWithValue("@bpid", buildingPartId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "SELECT", "building_levels");
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            levels.Add(new BuildingLevel
            {
                Id = reader.GetString(reader.GetOrdinal("id")),
                Prefix = reader.GetInt32(reader.GetOrdinal("prefix")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.GetString(reader.GetOrdinal("description")),
                Rdok = reader.GetDouble(reader.GetOrdinal("rdok")),
                Fbok = reader.GetDouble(reader.GetOrdinal("fbok")),
                Rduk = reader.IsDBNull(reader.GetOrdinal("rduk")) ? null : reader.GetDouble(reader.GetOrdinal("rduk")),
                SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order"))
            });
        }
        for (int i = 0; i < levels.Count; i++)
        {
            if (i < levels.Count - 1)
            {
                levels[i].StoryHeight = Math.Round(levels[i + 1].Fbok - levels[i].Fbok, 3);
                levels[i].RawHeight = Math.Round(levels[i + 1].Rdok - levels[i].Rdok, 3);
            }
        }
        return levels;
    }

    private void SaveBuildingParts(string projectId, List<BuildingPart> parts)
    {
        var conn = GetConnection();
        var delLvlCmd = conn.CreateCommand();
        delLvlCmd.CommandText = "DELETE FROM building_levels WHERE building_part_id IN (SELECT id FROM building_parts WHERE project_id = @pid)";
        delLvlCmd.Parameters.AddWithValue("@pid", projectId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "DELETE", "building_levels");
        delLvlCmd.ExecuteNonQuery();
        var delPartCmd = conn.CreateCommand();
        delPartCmd.CommandText = "DELETE FROM building_parts WHERE project_id = @pid";
        delPartCmd.Parameters.AddWithValue("@pid", projectId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "DELETE", "building_parts");
        delPartCmd.ExecuteNonQuery();

        for (int i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            var partId = string.IsNullOrEmpty(part.Id) ? _idGenerator.NewId() : part.Id;
            var utcNow = DateTime.UtcNow.ToString("o");
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO building_parts (id, project_id, short_name, description, building_type, zero_level_absolute, sort_order, created_at, created_by, last_modified_at, last_modified_by, sync_version)
                VALUES (@id, @pid, @sn, @desc, @bt, @zla, @so, @now, @user, @now, @user, 0)
                """;
            cmd.Parameters.AddWithValue("@now", utcNow);
            cmd.Parameters.AddWithValue("@user", _userContext.DisplayName);
            cmd.Parameters.AddWithValue("@id", partId); cmd.Parameters.AddWithValue("@pid", projectId);
            cmd.Parameters.AddWithValue("@sn", part.ShortName); cmd.Parameters.AddWithValue("@desc", part.Description);
            cmd.Parameters.AddWithValue("@bt", part.BuildingType); cmd.Parameters.AddWithValue("@zla", part.ZeroLevelAbsolute);
            cmd.Parameters.AddWithValue("@so", i);
            Log.Verbose("Executing SQL: {Operation} on {Table}", "INSERT", "building_parts");
            cmd.ExecuteNonQuery();

            for (int j = 0; j < part.Levels.Count; j++)
            {
                var level = part.Levels[j];
                var levelId = string.IsNullOrEmpty(level.Id) ? _idGenerator.NewId() : level.Id;
                var lvlNow = DateTime.UtcNow.ToString("o");
                var lvlCmd = conn.CreateCommand();
                lvlCmd.CommandText = """
                    INSERT INTO building_levels (id, building_part_id, prefix, name, description, rdok, fbok, rduk, sort_order, created_at, created_by, last_modified_at, last_modified_by, sync_version)
                    VALUES (@id, @bpid, @prefix, @name, @desc, @rdok, @fbok, @rduk, @so, @now, @user, @now, @user, 0)
                    """;
                lvlCmd.Parameters.AddWithValue("@now", lvlNow);
                lvlCmd.Parameters.AddWithValue("@user", _userContext.DisplayName);
                lvlCmd.Parameters.AddWithValue("@id", levelId); lvlCmd.Parameters.AddWithValue("@bpid", partId);
                lvlCmd.Parameters.AddWithValue("@prefix", level.Prefix); lvlCmd.Parameters.AddWithValue("@name", level.Name);
                lvlCmd.Parameters.AddWithValue("@desc", level.Description); lvlCmd.Parameters.AddWithValue("@rdok", level.Rdok);
                lvlCmd.Parameters.AddWithValue("@fbok", level.Fbok); lvlCmd.Parameters.AddWithValue("@rduk", (object?)level.Rduk ?? DBNull.Value);
                lvlCmd.Parameters.AddWithValue("@so", j);
                Log.Verbose("Executing SQL: {Operation} on {Table}", "INSERT", "building_levels");
                lvlCmd.ExecuteNonQuery();
            }
        }
    }

    // === PARTICIPANTS ===

    private List<ProjectParticipant> LoadParticipants(string projectId)
    {
        var conn = GetConnection();
        var list = new List<ProjectParticipant>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM project_participants WHERE project_id = @pid ORDER BY sort_order";
        cmd.Parameters.AddWithValue("@pid", projectId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "SELECT", "project_participants");
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ProjectParticipant
            {
                Id = reader.GetString(reader.GetOrdinal("id")),
                Role = reader.GetString(reader.GetOrdinal("role")),
                Company = reader.GetString(reader.GetOrdinal("company")),
                ContactPerson = reader.GetString(reader.GetOrdinal("contact_person")),
                Phone = reader.GetString(reader.GetOrdinal("phone")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                ContactId = reader.GetString(reader.GetOrdinal("contact_id")),
                SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order"))
            });
        }
        return list;
    }

    private void SaveParticipants(string projectId, List<ProjectParticipant> participants)
    {
        var conn = GetConnection();
        var delCmd = conn.CreateCommand();
        delCmd.CommandText = "DELETE FROM project_participants WHERE project_id = @pid";
        delCmd.Parameters.AddWithValue("@pid", projectId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "DELETE", "project_participants");
        delCmd.ExecuteNonQuery();

        for (int i = 0; i < participants.Count; i++)
        {
            var p = participants[i];
            var pId = string.IsNullOrEmpty(p.Id) ? _idGenerator.NewId() : p.Id;
            var utcNow = DateTime.UtcNow.ToString("o");
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO project_participants (id, project_id, role, company, contact_person, phone, email, contact_id, sort_order, created_at, created_by, last_modified_at, last_modified_by, sync_version)
                VALUES (@id, @pid, @role, @company, @cp, @phone, @email, @cid, @so, @now, @user, @now, @user, 0)
                """;
            cmd.Parameters.AddWithValue("@now", utcNow);
            cmd.Parameters.AddWithValue("@user", _userContext.DisplayName);
            cmd.Parameters.AddWithValue("@id", pId); cmd.Parameters.AddWithValue("@pid", projectId);
            cmd.Parameters.AddWithValue("@role", p.Role); cmd.Parameters.AddWithValue("@company", p.Company);
            cmd.Parameters.AddWithValue("@cp", p.ContactPerson); cmd.Parameters.AddWithValue("@phone", p.Phone);
            cmd.Parameters.AddWithValue("@email", p.Email); cmd.Parameters.AddWithValue("@cid", p.ContactId);
            cmd.Parameters.AddWithValue("@so", i);
            Log.Verbose("Executing SQL: {Operation} on {Table}", "INSERT", "project_participants");
            cmd.ExecuteNonQuery();
        }
    }

    // === LINKS ===

    private List<ProjectLink> LoadLinks(string projectId)
    {
        var conn = GetConnection();
        var list = new List<ProjectLink>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM project_links WHERE project_id = @pid ORDER BY sort_order";
        cmd.Parameters.AddWithValue("@pid", projectId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "SELECT", "project_links");
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ProjectLink
            {
                Id = reader.GetString(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Url = reader.GetString(reader.GetOrdinal("url")),
                LinkType = reader.GetString(reader.GetOrdinal("link_type")),
                SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order"))
            });
        }
        return list;
    }

    private void SaveLinks(string projectId, List<ProjectLink> links)
    {
        var conn = GetConnection();
        var delCmd = conn.CreateCommand();
        delCmd.CommandText = "DELETE FROM project_links WHERE project_id = @pid";
        delCmd.Parameters.AddWithValue("@pid", projectId);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "DELETE", "project_links");
        delCmd.ExecuteNonQuery();

        for (int i = 0; i < links.Count; i++)
        {
            var link = links[i];
            var linkId = string.IsNullOrEmpty(link.Id) ? _idGenerator.NewId() : link.Id;
            var utcNow = DateTime.UtcNow.ToString("o");
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO project_links (id, project_id, name, url, link_type, sort_order, created_at, created_by, last_modified_at, last_modified_by, sync_version)
                VALUES (@id, @pid, @name, @url, @lt, @so, @now, @user, @now, @user, 0)
                """;
            cmd.Parameters.AddWithValue("@now", utcNow);
            cmd.Parameters.AddWithValue("@user", _userContext.DisplayName);
            cmd.Parameters.AddWithValue("@id", linkId); cmd.Parameters.AddWithValue("@pid", projectId);
            cmd.Parameters.AddWithValue("@name", link.Name); cmd.Parameters.AddWithValue("@url", link.Url);
            cmd.Parameters.AddWithValue("@lt", link.LinkType); cmd.Parameters.AddWithValue("@so", i);
            Log.Verbose("Executing SQL: {Operation} on {Table}", "INSERT", "project_links");
            cmd.ExecuteNonQuery();
        }
    }

    // === HELPERS ===

    private static Project ReadProject(SqliteDataReader reader)
    {
        var statusStr = reader.GetString(reader.GetOrdinal("status"));
        Enum.TryParse<ProjectStatus>(statusStr, out var status);
        return new Project
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            ProjectNumber = reader.GetString(reader.GetOrdinal("project_number")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            FullName = reader.GetString(reader.GetOrdinal("full_name")),
            Status = status,
            ProjectType = ReadStringOrDefault(reader, "project_type"),
            Location = new ProjectLocation
            {
                Street = reader.GetString(reader.GetOrdinal("street")),
                HouseNumber = reader.GetString(reader.GetOrdinal("house_number")),
                PostalCode = reader.GetString(reader.GetOrdinal("postal_code")),
                City = reader.GetString(reader.GetOrdinal("city")),
                Municipality = reader.GetString(reader.GetOrdinal("municipality")),
                District = reader.GetString(reader.GetOrdinal("district")),
                State = reader.GetString(reader.GetOrdinal("state")),
                CoordinateSystem = reader.GetString(reader.GetOrdinal("coordinate_system")),
                CoordinateEast = reader.GetDouble(reader.GetOrdinal("coordinate_east")),
                CoordinateNorth = reader.GetDouble(reader.GetOrdinal("coordinate_north")),
                CadastralKg = reader.GetString(reader.GetOrdinal("cadastral_kg")),
                CadastralKgName = reader.GetString(reader.GetOrdinal("cadastral_kg_name")),
                CadastralGst = reader.GetString(reader.GetOrdinal("cadastral_gst"))
            },
            Timeline = new ProjectTimeline
            {
                ProjectStart = ReadNullableDate(reader, "project_start"),
                ConstructionStart = ReadNullableDate(reader, "construction_start"),
                PlannedEnd = ReadNullableDate(reader, "planned_end"),
                ActualEnd = ReadNullableDate(reader, "actual_end")
            },
            Client = new Client
            {
                Company = ReadStringOrDefault(reader, "company"),
                ContactPerson = ReadStringOrDefault(reader, "contact_person"),
                Phone = ReadStringOrDefault(reader, "phone"),
                Email = ReadStringOrDefault(reader, "email"),
                Notes = ReadStringOrDefault(reader, "client_notes")
            },
            Paths = new ProjectPaths
            {
                Root = reader.GetString(reader.GetOrdinal("root_path")),
                Plans = reader.GetString(reader.GetOrdinal("plans_path")),
                Inbox = reader.GetString(reader.GetOrdinal("inbox_path")),
                Photos = reader.GetString(reader.GetOrdinal("photos_path")),
                Documents = reader.GetString(reader.GetOrdinal("documents_path")),
                Protocols = reader.GetString(reader.GetOrdinal("protocols_path")),
                Invoices = reader.GetString(reader.GetOrdinal("invoices_path"))
            },
            Tags = reader.GetString(reader.GetOrdinal("tags")),
            Notes = reader.GetString(reader.GetOrdinal("notes")),
            UseGlobalZeroLevel = ReadIntOrDefault(reader, "use_global_zero_level") == 1,
            GlobalZeroLevel = ReadDoubleOrDefault(reader, "global_zero_level")
        };
    }

    private static DateTime? ReadNullableDate(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        if (reader.IsDBNull(ordinal)) return null;
        var str = reader.GetString(ordinal);
        return DateTime.TryParse(str, out var date) ? date : null;
    }

    private static string ReadStringOrDefault(SqliteDataReader reader, string column)
    {
        try
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }
        catch (Exception ex)
        {
            Log.Warning("Column {Column} not readable: {Error}", column, ex.Message);
            return string.Empty;
        }
    }

    private static int ReadIntOrDefault(SqliteDataReader reader, string column)
    {
        try
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
        }
        catch (Exception ex)
        {
            Log.Warning("Column {Column} not readable: {Error}", column, ex.Message);
            return 0;
        }
    }

    private static double ReadDoubleOrDefault(SqliteDataReader reader, string column)
    {
        try
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? 0.0 : reader.GetDouble(ordinal);
        }
        catch (Exception ex)
        {
            Log.Warning("Column {Column} not readable: {Error}", column, ex.Message);
            return 0.0;
        }
    }

    public string GetDatabasePath() => _dbPath;

    public void Dispose()
    {
        Log.Debug("Database connection disposed");
        _connection?.Close();
        _connection?.Dispose();
        _connection = null;
    }
}
