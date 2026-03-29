using System.IO;
using Microsoft.Data.Sqlite;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// SQLite database service — manages bpm.db in %LocalAppData%\BauProjektManager\.
/// Schema v1.3: projects, clients, building_parts, building_levels.
/// </summary>
public class ProjectDatabase : IDisposable
{
    private readonly string _dbPath;
    private SqliteConnection? _connection;

    public ProjectDatabase()
    {
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
            using var walCmd = _connection.CreateCommand();
            walCmd.CommandText = "PRAGMA journal_mode=WAL;";
            walCmd.ExecuteNonQuery();
            EnsureTables();
            MigrateSchema();
        }
        return _connection;
    }

    private void EnsureTables()
    {
        var conn = _connection!;
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS clients (
                seq INTEGER PRIMARY KEY AUTOINCREMENT,
                id TEXT UNIQUE NOT NULL,
                company TEXT NOT NULL DEFAULT '',
                contact_person TEXT NOT NULL DEFAULT '',
                phone TEXT NOT NULL DEFAULT '',
                email TEXT NOT NULL DEFAULT '',
                notes TEXT NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS projects (
                seq INTEGER PRIMARY KEY AUTOINCREMENT,
                id TEXT UNIQUE NOT NULL,
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
                tags TEXT NOT NULL DEFAULT '',
                notes TEXT NOT NULL DEFAULT '',
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                updated_at TEXT NOT NULL DEFAULT (datetime('now')),
                FOREIGN KEY (client_id) REFERENCES clients(id)
            );

            CREATE TABLE IF NOT EXISTS buildings (
                seq INTEGER PRIMARY KEY AUTOINCREMENT,
                id TEXT UNIQUE NOT NULL,
                project_id TEXT NOT NULL,
                name TEXT NOT NULL DEFAULT '',
                short_name TEXT NOT NULL DEFAULT '',
                type TEXT NOT NULL DEFAULT '',
                levels TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS building_parts (
                seq INTEGER PRIMARY KEY AUTOINCREMENT,
                id TEXT UNIQUE NOT NULL,
                project_id TEXT NOT NULL,
                short_name TEXT NOT NULL DEFAULT '',
                description TEXT NOT NULL DEFAULT '',
                building_type TEXT NOT NULL DEFAULT '',
                zero_level_absolute REAL NOT NULL DEFAULT 0,
                sort_order INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS building_levels (
                seq INTEGER PRIMARY KEY AUTOINCREMENT,
                id TEXT UNIQUE NOT NULL,
                building_part_id TEXT NOT NULL,
                prefix INTEGER NOT NULL DEFAULT 0,
                name TEXT NOT NULL DEFAULT '',
                description TEXT NOT NULL DEFAULT '',
                rdok REAL NOT NULL DEFAULT 0,
                fbok REAL NOT NULL DEFAULT 0,
                rduk REAL,
                sort_order INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (building_part_id) REFERENCES building_parts(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS schema_version (
                version TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private void MigrateSchema()
    {
        var conn = _connection!;

        if (!ColumnExists("projects", "project_type"))
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE projects ADD COLUMN project_type TEXT NOT NULL DEFAULT ''";
            cmd.ExecuteNonQuery();
        }

        var verCmd = conn.CreateCommand();
        verCmd.CommandText = "DELETE FROM schema_version; INSERT INTO schema_version (version) VALUES ('1.3');";
        verCmd.ExecuteNonQuery();
    }

    private bool ColumnExists(string table, string column)
    {
        var conn = _connection!;
        var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetString(1) == column)
                return true;
        }
        return false;
    }

    // === ID GENERATION ===

    private string GenerateNextId(string prefix, string table)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT MAX(seq) FROM {table}";
        var result = cmd.ExecuteScalar();
        var nextNum = result is DBNull || result is null ? 1 : Convert.ToInt64(result) + 1;
        return $"{prefix}_{nextNum:D3}";
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

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var project = ReadProject(reader);
            project.BuildingParts = LoadBuildingParts(project.Id);
            projects.Add(project);
        }

        return projects;
    }

    public void SaveProject(Project project)
    {
        var conn = GetConnection();

        bool isNew = string.IsNullOrEmpty(project.Id) || !ProjectExists(project.Id);
        if (isNew)
            project.Id = GenerateNextId("proj", "projects");

        string? clientId = null;
        if (!string.IsNullOrEmpty(project.Client.Company) ||
            !string.IsNullOrEmpty(project.Client.ContactPerson))
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
                tags, notes, updated_at
            ) VALUES (
                @id, @project_number, @name, @full_name, @status, @project_type, @client_id,
                @street, @house_number, @postal_code, @city,
                @municipality, @district, @state,
                @coordinate_system, @coordinate_east, @coordinate_north,
                @cadastral_kg, @cadastral_kg_name, @cadastral_gst,
                @project_start, @construction_start, @planned_end, @actual_end,
                @root_path, @plans_path, @inbox_path, @photos_path,
                @documents_path, @protocols_path, @invoices_path,
                @tags, @notes, datetime('now')
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
                tags=@tags, notes=@notes, updated_at=datetime('now')
            """;

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
        cmd.Parameters.AddWithValue("@tags", project.Tags);
        cmd.Parameters.AddWithValue("@notes", project.Notes);
        cmd.ExecuteNonQuery();

        SaveBuildingParts(project.Id, project.BuildingParts);
    }

    private bool ProjectExists(string projectId)
    {
        var conn = GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM projects WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", projectId);
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    public void DeleteProject(string projectId)
    {
        var conn = GetConnection();

        // Delete levels for all parts of this project
        var lvlCmd = conn.CreateCommand();
        lvlCmd.CommandText = """
            DELETE FROM building_levels WHERE building_part_id IN
            (SELECT id FROM building_parts WHERE project_id = @id)
            """;
        lvlCmd.Parameters.AddWithValue("@id", projectId);
        lvlCmd.ExecuteNonQuery();

        var partCmd = conn.CreateCommand();
        partCmd.CommandText = "DELETE FROM building_parts WHERE project_id = @id";
        partCmd.Parameters.AddWithValue("@id", projectId);
        partCmd.ExecuteNonQuery();

        // Keep old buildings table cleanup
        var bldgCmd = conn.CreateCommand();
        bldgCmd.CommandText = "DELETE FROM buildings WHERE project_id = @id";
        bldgCmd.Parameters.AddWithValue("@id", projectId);
        bldgCmd.ExecuteNonQuery();

        var projCmd = conn.CreateCommand();
        projCmd.CommandText = "DELETE FROM projects WHERE id = @id";
        projCmd.Parameters.AddWithValue("@id", projectId);
        projCmd.ExecuteNonQuery();
    }

    // === CLIENTS ===

    private string SaveClient(Client client, string projectId)
    {
        var conn = GetConnection();

        var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT client_id FROM projects WHERE id = @id";
        checkCmd.Parameters.AddWithValue("@id", projectId);
        var existingClientId = checkCmd.ExecuteScalar() as string;

        string clientId = !string.IsNullOrEmpty(existingClientId)
            ? existingClientId
            : GenerateNextId("client", "clients");

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO clients (id, company, contact_person, phone, email, notes)
            VALUES (@id, @company, @contact_person, @phone, @email, @notes)
            ON CONFLICT(id) DO UPDATE SET
                company=@company, contact_person=@contact_person,
                phone=@phone, email=@email, notes=@notes
            """;
        cmd.Parameters.AddWithValue("@id", clientId);
        cmd.Parameters.AddWithValue("@company", client.Company);
        cmd.Parameters.AddWithValue("@contact_person", client.ContactPerson);
        cmd.Parameters.AddWithValue("@phone", client.Phone);
        cmd.Parameters.AddWithValue("@email", client.Email);
        cmd.Parameters.AddWithValue("@notes", client.Notes);
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

        // Calculate story heights and raw heights (need adjacent levels)
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

        // Delete existing levels for this project's parts
        var delLvlCmd = conn.CreateCommand();
        delLvlCmd.CommandText = """
            DELETE FROM building_levels WHERE building_part_id IN
            (SELECT id FROM building_parts WHERE project_id = @pid)
            """;
        delLvlCmd.Parameters.AddWithValue("@pid", projectId);
        delLvlCmd.ExecuteNonQuery();

        // Delete existing parts
        var delPartCmd = conn.CreateCommand();
        delPartCmd.CommandText = "DELETE FROM building_parts WHERE project_id = @pid";
        delPartCmd.Parameters.AddWithValue("@pid", projectId);
        delPartCmd.ExecuteNonQuery();

        // Insert parts + levels
        for (int i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            var partId = string.IsNullOrEmpty(part.Id)
                ? GenerateNextId("bpart", "building_parts")
                : part.Id;

            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO building_parts (id, project_id, short_name, description, building_type, zero_level_absolute, sort_order)
                VALUES (@id, @pid, @sn, @desc, @bt, @zla, @so)
                """;
            cmd.Parameters.AddWithValue("@id", partId);
            cmd.Parameters.AddWithValue("@pid", projectId);
            cmd.Parameters.AddWithValue("@sn", part.ShortName);
            cmd.Parameters.AddWithValue("@desc", part.Description);
            cmd.Parameters.AddWithValue("@bt", part.BuildingType);
            cmd.Parameters.AddWithValue("@zla", part.ZeroLevelAbsolute);
            cmd.Parameters.AddWithValue("@so", i);
            cmd.ExecuteNonQuery();

            // Save levels for this part
            for (int j = 0; j < part.Levels.Count; j++)
            {
                var level = part.Levels[j];
                var levelId = string.IsNullOrEmpty(level.Id)
                    ? GenerateNextId("blvl", "building_levels")
                    : level.Id;

                var lvlCmd = conn.CreateCommand();
                lvlCmd.CommandText = """
                    INSERT INTO building_levels (id, building_part_id, prefix, name, description, rdok, fbok, rduk, sort_order)
                    VALUES (@id, @bpid, @prefix, @name, @desc, @rdok, @fbok, @rduk, @so)
                    """;
                lvlCmd.Parameters.AddWithValue("@id", levelId);
                lvlCmd.Parameters.AddWithValue("@bpid", partId);
                lvlCmd.Parameters.AddWithValue("@prefix", level.Prefix);
                lvlCmd.Parameters.AddWithValue("@name", level.Name);
                lvlCmd.Parameters.AddWithValue("@desc", level.Description);
                lvlCmd.Parameters.AddWithValue("@rdok", level.Rdok);
                lvlCmd.Parameters.AddWithValue("@fbok", level.Fbok);
                lvlCmd.Parameters.AddWithValue("@rduk", (object?)level.Rduk ?? DBNull.Value);
                lvlCmd.Parameters.AddWithValue("@so", j);
                lvlCmd.ExecuteNonQuery();
            }
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
            Notes = reader.GetString(reader.GetOrdinal("notes"))
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
        catch { return string.Empty; }
    }

    public string GetDatabasePath() => _dbPath;

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        _connection = null;
    }
}
