using System.IO;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.Tests.Fakes;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// BPM-120 T4 (ADR-064 AK 10): fachliche DB-Writes einer Action +
/// action_status=completed laufen in DERSELBEN SQLite-Transaction. Der
/// injizierte Fehler (NULL-Segmenttyp wirft beim Upsert — NACH Revision/File)
/// darf weder partielle Aenderungen noch eine completed-Action hinterlassen.
/// </summary>
public class ImportExecutionDbTransactionTests
{
    private sealed class TestEnv : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "bpm-t4-virtual");

        public TestEnv()
        {
            IIdGenerator idGen = new UlidIdGenerator();
            var projectId = idGen.NewId();
            Repo = new PlanManagerDatabase(projectId, idGen,
                dbPathOverride: TempDb.NewTempDbPath(projectId));
        }

        public (string Status, string? Error) GetSingleActionStatus()
        {
            using var conn = new SqliteConnection($"Data Source={Repo.GetDatabasePath()}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT action_status, error_message FROM import_actions";
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            return (reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1));
        }

        public int CountRevisions()
        {
            using var conn = new SqliteConnection($"Data Source={Repo.GetDatabasePath()}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM plan_revisions";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void Dispose()
        {
            var dbPath = Repo.GetDatabasePath();
            Repo.Dispose();
            using (var pc = new SqliteConnection($"Data Source={dbPath}"))
                SqliteConnection.ClearPool(pc);
            TempDb.Delete(dbPath);
        }
    }

    // Segment mit NULL-Typ: wirft beim UpsertSegment — dem LETZTEN fachlichen
    // Write vor dem Action-Abschluss. Alles davor muss zurueckrollen.
    private static readonly AssignedSegmentValue PoisonSegment = new(null!, "tok", "Wert");

    private static PendingAssignment NewPending(string fileName, bool poisoned)
    {
        var scan = new ScannedFile(
            Path.Combine("_Eingang", fileName), fileName,
            Path.GetExtension(fileName), 12, DateTime.UtcNow);
        return new PendingAssignment(
            new FingerprintedFile(scan, "md5-" + fileName), CaptureBucket.NewCapture,
            "polierplan", "Polierplan", "Haus 2", "OG2", "5998-300", null,
            Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2"), Match: null,
            AssignedSegments: poisoned ? [PoisonSegment] : null);
    }

    [Fact]
    public void Execute_DbWriteFails_RollsBackDocumentRevisionAndActionNotCompleted()
    {
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        fake.AddFile(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf"));

        var decisions = CaptureConfirmService.BuildDecisions(
            [NewPending("5998-300_OG2.pdf", poisoned: true)], new PlanValueNormalizer());
        var result = new ImportExecutionService(env.Repo, new UlidIdGenerator(), fake, fake, fake)
            .Execute(decisions, env.Root, "_Eingang");

        Assert.Equal(1, result.Failed);
        Assert.Equal(0, result.Succeeded);

        // AK 10: KEINE partiellen Aenderungen — Document, Revision, File sind weg
        Assert.Null(env.Repo.GetDocumentByKey(decisions[0].DocumentKey!));
        Assert.Equal(0, env.CountRevisions());

        // ... und keine completed-Action: failed + Fehlermeldung
        var action = env.GetSingleActionStatus();
        Assert.Equal("failed", action.Status);
        Assert.False(string.IsNullOrEmpty(action.Error));
    }

    [Fact]
    public void Execute_UpdateDbFails_SupersedeIsRolledBack()
    {
        // Der Supersede ist der ERSTE fachliche Write der Transaction — schlaegt
        // ein spaeterer Write fehl, muss die alte Revision wieder current sein.
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        var targetRelDir = Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2");
        fake.AddFile(Path.Combine(env.Root, "_Eingang", "5998-300-B_OG2.pdf"));

        const string key = "polierplan|5998_300|haus_2|og2";
        var docId = env.Repo.ResolveOrCreateDocument("proj1", key,
            "polierplan", "5998-300", "Polierplan", "", "Pläne", targetRelDir, null, null);
        var now = DateTime.UtcNow.ToString("o");
        var oldImportId = env.Repo.CreateImportJournal("_Eingang", 1, profileId: null);
        env.Repo.CompleteImportJournal(oldImportId, success: true);
        var revAId = env.Repo.InsertRevision(docId, "A", "FileName",
            PlanArchive.Status.Current, now, null, now, lastImportId: oldImportId);

        var match = new KnownPlanDocument(docId, key, "5998-300", "Polierplan",
            "Pläne", targetRelDir, "A", revAId);
        var scan = new ScannedFile(
            Path.Combine("_Eingang", "5998-300-B_OG2.pdf"), "5998-300-B_OG2.pdf",
            ".pdf", 12, DateTime.UtcNow);
        var pending = new PendingAssignment(
            new FingerprintedFile(scan, "md5-b"), CaptureBucket.UpdateProposal,
            docId, "Polierplan", null, null, "5998-300", "B", targetRelDir, match,
            AssignedSegments: [PoisonSegment]);

        var decisions = CaptureConfirmService.BuildDecisions(
            [pending], new PlanValueNormalizer());
        var result = new ImportExecutionService(env.Repo, new UlidIdGenerator(), fake, fake, fake)
            .Execute(decisions, env.Root, "_Eingang");

        Assert.Equal(1, result.Failed);

        // Rollback: Revision A ist WIEDER current, keine zweite Revision entstanden
        var revisions = env.Repo.GetRevisionsForDocument(docId);
        var revA = Assert.Single(revisions);
        Assert.Equal(revAId, revA.Id);
        Assert.Equal(PlanArchive.Status.Current, revA.RevisionStatus);
        Assert.Null(revA.SupersededAt);
    }
}
