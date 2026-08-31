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
/// BPM-120 T6 (ADR-064 P.6, AK 13): 'pending' = recovery-pflichtig und
/// blockiert neuen Confirm; 'failed' erst terminal nach VOLLSTAENDIGEM
/// Rollback oder bewusstem Cleanup. Scheitert ein Rollback, wird der Vorgang
/// nicht faelschlich als sauber markiert; failed-Actions sind wiederholbar.
/// </summary>
public class RecoveryStatusSemanticsTests
{
    private sealed class TestEnv : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "bpm-t6-virtual");

        public TestEnv()
        {
            IIdGenerator idGen = new UlidIdGenerator();
            var projectId = idGen.NewId();
            Repo = new PlanManagerDatabase(projectId, idGen,
                dbPathOverride: TempDb.NewTempDbPath(projectId));
        }

        public ImportExecutionService Executor(FakeFileStore fake) =>
            new(Repo, new UlidIdGenerator(), fake, fake, fake);

        public RecoveryExecutorService Recovery(FakeFileStore fake) =>
            new(Repo, fake, fake, fake, Executor(fake));

        public string GetJournalStatus(string importId)
        {
            using var conn = new SqliteConnection($"Data Source={Repo.GetDatabasePath()}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT status FROM import_journal WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", importId);
            return (string)cmd.ExecuteScalar()!;
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

    private static PendingAssignment NewPending(string fileName)
    {
        var scan = new ScannedFile(
            Path.Combine("_Eingang", fileName), fileName,
            Path.GetExtension(fileName), 12, DateTime.UtcNow);
        return new PendingAssignment(
            new FingerprintedFile(scan, "md5-" + fileName), CaptureBucket.NewCapture,
            "polierplan", "Polierplan", "Haus 2", "OG2", "5998-300", null,
            Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2"), Match: null);
    }

    [Fact]
    public void Execute_WithFailedAction_JournalStaysPendingAndBlocksConfirm()
    {
        // AK 13: Import mit fehlgeschlagener Action bleibt 'pending' —
        // recovery-pflichtig, der naechste Confirm ist blockiert.
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        fake.AddFile(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf"));
        fake.FailNext(FakeFileStore.FileOp.Move, "5998-300_OG2.pdf", times: 3);

        var decisions = CaptureConfirmService.BuildDecisions(
            [NewPending("5998-300_OG2.pdf")], new PlanValueNormalizer());
        var result = env.Executor(fake).Execute(decisions, env.Root, "_Eingang");

        Assert.Equal(1, result.Failed);
        Assert.True(env.Repo.HasPendingImports());
        var pendingImports = env.Repo.GetPendingImports();
        Assert.Single(pendingImports);

        var check = new PreImportRecoveryCheck().Evaluate(pendingImports);
        Assert.False(check.CanConfirm); // blockiert neuen Confirm
    }

    [Fact]
    public void Forward_FailedActionIsRetryable_JournalCompletesAfterFix()
    {
        // failed ist auf Action-Ebene nicht terminal: nach behobener Ursache
        // fuehrt der naechste Forward die Action erneut aus und completed erst dann.
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        fake.AddFile(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf"));
        fake.FailNext(FakeFileStore.FileOp.Move, "5998-300_OG2.pdf", times: 3);

        var decisions = CaptureConfirmService.BuildDecisions(
            [NewPending("5998-300_OG2.pdf")], new PlanValueNormalizer());
        env.Executor(fake).Execute(decisions, env.Root, "_Eingang");
        var importId = Assert.Single(env.Repo.GetPendingImports()).Id;

        // Lauf 1 mit weiter gestoerter Disk: Journal bleibt pending
        fake.FailNext(FakeFileStore.FileOp.Move, "5998-300_OG2.pdf", times: 3);
        var run1 = env.Recovery(fake).ExecuteForward(importId, env.Root);
        Assert.Equal(1, run1.FailedCount);
        Assert.Equal("pending", env.GetJournalStatus(importId));

        // Ursache behoben: Lauf 2 wiederholt die failed-Action und completed
        var run2 = env.Recovery(fake).ExecuteForward(importId, env.Root);
        Assert.Equal(0, run2.FailedCount);
        Assert.Equal("completed", env.GetJournalStatus(importId));
        Assert.False(env.Repo.HasPendingImports());
        Assert.NotNull(env.Repo.GetDocumentByKey(decisions[0].DocumentKey!));
    }

    [Fact]
    public void Rollback_DiskReverseFails_JournalStaysPendingNotFailed()
    {
        // AK 13: Scheitert ein Disk-Reverse, wird der Vorgang NICHT als 'failed'
        // (= sauber abgeraeumt) markiert — er bleibt pending/reparierbar.
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        var destRel = Path.Combine("Pläne", "5998-300_OG2.pdf");
        fake.AddFile(Path.Combine(env.Root, destRel)); // Datei liegt am Ziel

        var importId = env.Repo.CreateImportJournal("_Eingang", 1, profileId: null);
        var actionId = env.Repo.InsertImportAction(importId, 0, "new",
            "polierplan|5998_300", "5998-300", null, oldIndex: null,
            Path.Combine("_Eingang", "5998-300_OG2.pdf"), destRel,
            archivePath: null, md5: "md5-x", fileSize: 12);
        // Action als completed markieren (Import lief durch), Journal noch pending
        using (var conn = new SqliteConnection($"Data Source={env.Repo.GetDatabasePath()}"))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE import_actions SET action_status = 'completed' WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", actionId);
            cmd.ExecuteNonQuery();
        }

        fake.FailNext(FakeFileStore.FileOp.Move, "5998-300_OG2.pdf", times: 1);
        var result = env.Recovery(fake).ExecuteRollback(importId, env.Root);

        Assert.Equal(1, result.FailedCount);
        Assert.Equal("pending", env.GetJournalStatus(importId)); // NICHT failed

        // Zweiter Versuch ohne Stoerung: jetzt vollstaendig -> terminal 'failed'
        var retry = env.Recovery(fake).ExecuteRollback(importId, env.Root);
        Assert.Equal(0, retry.FailedCount);
        Assert.Equal("failed", env.GetJournalStatus(importId));
        Assert.True(fake.FileExists(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf")));
    }
}
