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
/// BPM-120 T3 (ADR-064 AK 7): Disk-Protokoll — eingehende Datei via
/// <c>.bpm_tmp</c> + atomarem final Rename, Lock-Retry max. 3 Versuche,
/// Recovery holt den finalen Rename nach einem Crash zwischen tmp-Move und
/// Rename idempotent nach. FakeFileStore + echte SQLite-Temp-DB.
/// </summary>
public class ImportExecutionDiskProtocolTests
{
    private sealed class TestEnv : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "bpm-t3-virtual");

        public TestEnv()
        {
            IIdGenerator idGen = new UlidIdGenerator();
            var projectId = idGen.NewId();
            Repo = new PlanManagerDatabase(projectId, idGen,
                dbPathOverride: TempDb.NewTempDbPath(projectId));
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

    private static PendingAssignment NewPending(string fileName, string md5)
    {
        var scan = new ScannedFile(
            Path.Combine("_Eingang", fileName), fileName,
            Path.GetExtension(fileName), 12, DateTime.UtcNow);
        return new PendingAssignment(
            new FingerprintedFile(scan, md5), CaptureBucket.NewCapture,
            "polierplan", "Polierplan", "Haus 2", "OG2", "5998-300", null,
            Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2"), Match: null);
    }

    private static ImportExecutionResult Execute(TestEnv env, FakeFileStore fake)
    {
        var decisions = CaptureConfirmService.BuildDecisions(
            [NewPending("5998-300_OG2.pdf", "md5-pdf")], new PlanValueNormalizer());
        return new ImportExecutionService(env.Repo, new UlidIdGenerator(), fake, fake, fake)
            .Execute(decisions, env.Root, "_Eingang");
    }

    [Fact]
    public void Execute_HappyPath_PublishesFileAndLeavesNoTmpBehind()
    {
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        fake.AddFile(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf"));

        var result = Execute(env, fake);

        Assert.Equal(1, result.Succeeded);
        Assert.True(fake.FileExists(Path.Combine(
            env.Root, "Pläne", "Polierplan", "Haus 2", "OG2", "5998-300_OG2.pdf")));
        Assert.Empty(fake.EnumerateFiles(env.Root, "*.bpm_tmp", recursive: true));
    }

    [Fact]
    public void Execute_MoveLockedTwice_ThirdAttemptSucceeds()
    {
        // Lock-Retry (AK 7): zwei Sharing-Verletzungen, der dritte Versuch greift.
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        fake.AddFile(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf"));
        fake.FailNext(FakeFileStore.FileOp.Move, "5998-300_OG2.pdf",
            new IOException("Sharing-Verletzung"), times: 2);

        var result = Execute(env, fake);

        Assert.Equal(1, result.Succeeded);
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public void Execute_MoveLockedThreeTimes_FailsWithoutFourthAttempt()
    {
        // Max. 3 Versuche — der vierte findet nicht statt, Action wird failed.
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        var inboxAbs = Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf");
        fake.AddFile(inboxAbs);
        fake.FailNext(FakeFileStore.FileOp.Move, "5998-300_OG2.pdf",
            new IOException("Sharing-Verletzung"), times: 3);

        var result = Execute(env, fake);

        Assert.Equal(1, result.Failed);
        Assert.Equal(0, result.Succeeded);
        Assert.True(fake.FileExists(inboxAbs)); // Quelle unangetastet, kein Datenverlust
    }

    [Fact]
    public void RecoveryForward_TmpLeftover_FinalizesRenameIdempotently()
    {
        // Crash zwischen tmp-Move und finalem Rename: Quelle weg, <ziel>.bpm_tmp da.
        // Recovery Forward stellt den Endzustand her, ohne die Datei neu zu verschieben.
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        var destRel = Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2", "5998-300_OG2.pdf");
        var destAbs = Path.Combine(env.Root, destRel);
        fake.AddFile(destAbs + ImportExecutionService.TmpSuffix, [7, 7, 7]);

        var importId = env.Repo.CreateImportJournal("_Eingang", 1, profileId: null);
        env.Repo.InsertImportAction(importId, 0, "new", "polierplan|5998_300",
            "5998-300", null, oldIndex: null,
            Path.Combine("_Eingang", "5998-300_OG2.pdf"), destRel,
            archivePath: null, md5: "md5-pdf", fileSize: 3);

        var result = new RecoveryExecutorService(env.Repo, fake, fake, fake)
            .ExecuteForward(importId, env.Root);

        Assert.Equal(0, result.FailedCount);
        Assert.True(fake.FileExists(destAbs));
        Assert.False(fake.FileExists(destAbs + ImportExecutionService.TmpSuffix));
    }
}
