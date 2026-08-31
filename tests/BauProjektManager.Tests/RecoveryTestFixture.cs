using System.IO;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.Tests.Fakes;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// Test-Fixture für Integration-Tests des <see cref="RecoveryExecutorService"/>.
/// Erzeugt:
/// - Temp-ProjectRoot (Inbox + Plans + _Archiv Subfolders)
/// - Eigene PlanManagerDatabase (ueber eindeutige projectId)
/// - RecoveryExecutorService unter Test
///
/// Nach Test: Cleanup von Temp-Folder + LocalAppData-DB-Pfad.
/// </summary>
public sealed class RecoveryTestFixture : IDisposable
{
    public string ProjectId { get; }
    public string ProjectRoot { get; }
    public string InboxRel { get; } = "_Eingang";
    public string PlansRel { get; } = "Plans";
    public string ArchiveRel { get; } = Path.Combine("Plans", "_Archiv");

    public PlanManagerDatabase Db { get; }
    public RecoveryExecutorService Executor { get; }
    public IIdGenerator IdGenerator { get; }


    public RecoveryTestFixture()
    {
        IdGenerator = new UlidIdGenerator();
        ProjectId = IdGenerator.NewId();

        // Temp Project-Root für Disk-Operations
        ProjectRoot = Path.Combine(Path.GetTempPath(), "bpm-test-" + ProjectId);
        Directory.CreateDirectory(ProjectRoot);
        Directory.CreateDirectory(Path.Combine(ProjectRoot, InboxRel));
        Directory.CreateDirectory(Path.Combine(ProjectRoot, PlansRel));

        // BPM-123: Test-DB unter %TEMP% via dbPathOverride — nie in LocalAppData\Projects.
        Db = new PlanManagerDatabase(ProjectId, IdGenerator,
            dbPathOverride: TempDb.NewTempDbPath(ProjectId));
        var fs = new LocalFileSystem();
        Executor = new RecoveryExecutorService(Db, fs, fs, fs,
            new ImportExecutionService(Db, IdGenerator, fs, fs, fs));
    }

    /// <summary>
    /// Erzeugt im Inbox-Folder eine Datei mit gegebenem Namen + Inhalt.
    /// </summary>
    public string SeedInboxFile(string fileName, string content = "test")
    {
        var rel = Path.Combine(InboxRel, fileName);
        var abs = Path.Combine(ProjectRoot, rel);
        File.WriteAllText(abs, content);
        return rel;
    }

    /// <summary>
    /// Erzeugt im Plans-Folder eine Datei (für Tests die "schon verschoben" simulieren).
    /// </summary>
    public string SeedPlansFile(string fileName, string content = "test")
    {
        var rel = Path.Combine(PlansRel, fileName);
        var abs = Path.Combine(ProjectRoot, rel);
        File.WriteAllText(abs, content);
        return rel;
    }

    /// <summary>
    /// Legt einen Journal-Eintrag mit Status 'pending' an.
    /// </summary>
    public string CreateJournal(int fileCount = 5)
    {
        return Db.CreateImportJournal(InboxRel, fileCount, profileId: null);
    }

    /// <summary>
    /// Direkter SQL-Update auf import_actions (für Test-Setup von Mix-States).
    /// </summary>
    public void SetActionStatus(string actionId, string newStatus, string? errorMessage = null)
    {
        var dbPath = Db.GetDatabasePath();
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE import_actions SET action_status = @s, error_message = @e WHERE id = @id";
        cmd.Parameters.AddWithValue("@s", newStatus);
        cmd.Parameters.AddWithValue("@e", (object?)errorMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", actionId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Liest Journal-Status direkt aus der DB für Assertions.
    /// </summary>
    public string GetJournalStatus(string importId)
    {
        var dbPath = Db.GetDatabasePath();
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT status FROM import_journal WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", importId);
        return (string)cmd.ExecuteScalar()!;
    }

    public bool FileExistsInInbox(string fileName)
        => File.Exists(Path.Combine(ProjectRoot, InboxRel, fileName));

    public bool FileExistsInPlans(string fileName)
        => File.Exists(Path.Combine(ProjectRoot, PlansRel, fileName));

    public void Dispose()
    {
        var dbPath = Db.GetDatabasePath();
        Db.Dispose();

        // Microsoft.Data.Sqlite hat Connection-Pooling — Db.Dispose() schliesst
        // nur die Connection-Instanz, nicht den Pool. Ohne Pool-Clear haelt
        // der Pool das DB-File offen und Directory.Delete schlaegt mit
        // IOException fehl (File-Lock). BPM-120 T0: gezielt NUR den Pool dieser
        // DB leeren — das fruehere ClearAllPools riss unter xunit-Parallellast
        // die Pools fremder Test-Klassen mit (Flaky-Ursache).
        using (var pc = new SqliteConnection($"Data Source={dbPath}"))
            SqliteConnection.ClearPool(pc);

        try
        {
            if (Directory.Exists(ProjectRoot))
                Directory.Delete(ProjectRoot, recursive: true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"RecoveryTestFixture: ProjectRoot cleanup fehlgeschlagen ({ProjectRoot}): {ex.Message}");
        }

        TempDb.Delete(dbPath);
    }
}
