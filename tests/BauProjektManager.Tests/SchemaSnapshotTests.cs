using System.IO;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.Tests.Fakes;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// Seitenliste, Reiter „Daten" (mockup-erstellen, Standard Seitenliste): Der Aufbau von bpm.db und
/// planmanager.db steht als leeres Schema in Docs/Mockups/schema-*.sql. tools/schema_leer.py baut daraus
/// die Datenliste, ohne je eine echte Datenbank zu lesen. Weicht das frisch erzeugte Schema ab, schreibt
/// der Test die Datei neu und schlaegt fehl: dann python tools/datenliste.py und python tools/seitenliste.py
/// ausfuehren und alles zusammen committen.
/// </summary>
public class SchemaSnapshotTests
{
    private sealed class FakeUserContext : IUserContext
    {
        public string UserId => "TEST\\user";
        public string DisplayName => "Test User";
        public UserContextSource Source => UserContextSource.Local;
    }

    private sealed class FakeDeviceContext : IDeviceContext
    {
        public string DeviceId => "01TESTDEVICE";
        public string DeviceName => "TestDevice";
    }

    [Fact]
    public void BpmDb_Schema_MatchesSnapshot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bpm-schema-{Guid.NewGuid():N}.db");
        try
        {
            using (var db = new ProjectDatabase(new UlidIdGenerator(), new FakeUserContext(),
                       new FakeDeviceContext(), persistenceRegistry: null, dbPathOverride: path))
                db.LoadAllProjects(); // legt das Schema an
            AssertSnapshot(path, "schema-bpm.sql");
        }
        finally { TempDb.Delete(path); }
    }

    [Fact]
    public void PlanManagerDb_Schema_MatchesSnapshot()
    {
        IIdGenerator idGen = new UlidIdGenerator();
        var projectId = idGen.NewId();
        var path = TempDb.NewTempDbPath(projectId);
        try
        {
            using (var db = new PlanManagerDatabase(projectId, idGen, dbPathOverride: path))
                db.HasPendingImports(); // legt das Schema an
            AssertSnapshot(path, "schema-planmanager.sql");
        }
        finally { TempDb.Delete(path); }
    }

    private static void AssertSnapshot(string dbPath, string fileName)
    {
        var actual = ReadSchema(dbPath);
        var target = Path.Combine(RepoRoot(), "Docs", "Mockups", fileName);
        var expected = File.Exists(target) ? File.ReadAllText(target).Replace("\r\n", "\n") : null;
        if (expected == actual)
            return;
        File.WriteAllText(target, actual);
        Assert.Fail($"Schema geaendert: Docs/Mockups/{fileName} neu geschrieben. " +
                    "python tools/datenliste.py und python tools/seitenliste.py ausfuehren, dann erneut testen.");
    }

    /// <summary>CREATE TABLE je Tabelle, nach Name sortiert, ohne Inhalte.</summary>
    private static string ReadSchema(string dbPath)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
        var lines = new List<string>
        {
            "-- Erzeugt von SchemaSnapshotTests aus einer frischen, leeren Datenbank - nicht von Hand aendern.",
        };
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            lines.Add(reader.GetString(0).Replace("\r\n", "\n") + ";");
        return string.Join("\n", lines) + "\n";
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "BauProjektManager.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repo-Wurzel (BauProjektManager.slnx) nicht gefunden.");
    }
}
