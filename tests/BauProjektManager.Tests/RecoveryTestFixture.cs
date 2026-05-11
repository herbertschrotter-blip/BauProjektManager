using System.IO;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
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

    private readonly string _localAppDataProjectFolder;

    public RecoveryTestFixture()
    {
        IdGenerator = new UlidIdGenerator();
        ProjectId = IdGenerator.NewId();

        // Temp Project-Root für Disk-Operations
        ProjectRoot = Path.Combine(Path.GetTempPath(), "bpm-test-" + ProjectId);
        Directory.CreateDirectory(ProjectRoot);
        Directory.CreateDirectory(Path.Combine(ProjectRoot, InboxRel));
        Directory.CreateDirectory(Path.Combine(ProjectRoot, PlansRel));

        // Db wird unter %LocalAppData%\BauProjektManager\Projects\{projectId}\ angelegt
        _localAppDataProjectFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BauProjektManager", "Projects", ProjectId);

        Db = new PlanManagerDatabase(ProjectId, IdGenerator);
        Executor = new RecoveryExecutorService(Db);
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
        Db.Dispose();

        // Microsoft.Data.Sqlite hat Connection-Pooling — Db.Dispose() schliesst
        // nur die Connection-Instanz, nicht den Pool. Ohne ClearAllPools haelt
        // der Pool das DB-File offen und Directory.Delete schlaegt mit
        // IOException fehl (File-Lock). Frueher wurde der Fehler vom try/catch
        // verschluckt — Resultat: Muell-Ordner unter %LocalAppData%\Projects\.
        SqliteConnection.ClearAllPools();

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

        try
        {
            if (Directory.Exists(_localAppDataProjectFolder))
                Directory.Delete(_localAppDataProjectFolder, recursive: true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"RecoveryTestFixture: LocalAppData cleanup fehlgeschlagen ({_localAppDataProjectFolder}): {ex.Message}");
        }
    }
}
