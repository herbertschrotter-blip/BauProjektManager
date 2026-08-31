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
/// BPM-120 T7 (ADR-064 P.5, AK 14): Undo-Haertung — scheitert irgendein
/// erforderlicher Disk-Reverse, bleibt die DB unangetastet (keine Revision
/// soft-deleted/restored, KEIN MarkImportUndone). Erst nach vollstaendig
/// erfolgreicher Disk-Phase laeuft der DB-Rollback in einer Transaction.
/// E2E ueber echten Import (Execute) + FakeFileStore-Fault-Injection.
/// </summary>
public class ImportUndoHardeningTests
{
    private sealed class TestEnv : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "bpm-t7-virtual");

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

    private static PendingAssignment NewPending(string fileName, string planNumber)
    {
        var scan = new ScannedFile(
            Path.Combine("_Eingang", fileName), fileName,
            Path.GetExtension(fileName), 12, DateTime.UtcNow);
        return new PendingAssignment(
            new FingerprintedFile(scan, "md5-" + fileName), CaptureBucket.NewCapture,
            "polierplan", "Polierplan", "Haus 2", "OG2", planNumber, null,
            Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2"), Match: null);
    }

    private static void Import(TestEnv env, FakeFileStore fake, params string[] fileNames)
    {
        foreach (var name in fileNames)
            fake.AddFile(Path.Combine(env.Root, "_Eingang", name));
        var pendings = fileNames
            .Select((name, i) => NewPending(name, $"5998-30{i}"))
            .ToList();
        var decisions = CaptureConfirmService.BuildDecisions(pendings, new PlanValueNormalizer());
        var result = new ImportExecutionService(env.Repo, new UlidIdGenerator(), fake, fake, fake)
            .Execute(decisions, env.Root, "_Eingang");
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public void Undo_DiskReverseFails_DbUntouchedAndImportNotUndone()
    {
        // AK 14: Der Disk-Reverse scheitert (Datei gesperrt) — DB-Zustand und
        // Journal muessen exakt so bleiben, als waere kein Undo versucht worden.
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        Import(env, fake, "5998-300_OG2.pdf");
        var importId = env.Repo.GetLastCompletedImportId();
        Assert.NotNull(importId);
        var doc = env.Repo.GetDocumentByKey("polierplan|5998_300|haus_2|og2");
        Assert.NotNull(doc);

        fake.FailNext(FakeFileStore.FileOp.Move, "5998-300_OG2.pdf", times: 1);
        var result = new ImportUndoService(env.Repo, fake, fake, fake)
            .UndoLastImport(env.Root);

        Assert.False(result.Success);
        // DB unangetastet: Revision weiter current, Dokument da, Import NICHT undone
        Assert.NotNull(env.Repo.GetCurrentRevisionForDocument(doc!.Id));
        Assert.Equal(importId, env.Repo.GetLastCompletedImportId());
        // Datei liegt weiter am Ziel
        Assert.True(fake.FileExists(Path.Combine(
            env.Root, "Pläne", "Polierplan", "Haus 2", "OG2", "5998-300_OG2.pdf")));
    }

    [Fact]
    public void Undo_FirstReverseFails_StopsBeforeFurtherDiskChanges()
    {
        // T7: beim ERSTEN Reverse-Fehler wird abgebrochen — die uebrigen Dateien
        // bleiben am Ziel (minimale Drift), DB komplett unangetastet.
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        Import(env, fake, "5998-300_OG2.pdf", "5998-301_OG2.pdf");

        // LIFO: die zuletzt journalisierte Action (5998-301) wird zuerst reversiert
        fake.FailNext(FakeFileStore.FileOp.Move, "5998-301_OG2.pdf", times: 1);
        var result = new ImportUndoService(env.Repo, fake, fake, fake)
            .UndoLastImport(env.Root);

        Assert.False(result.Success);
        Assert.Equal(0, result.RestoredFiles);
        var targetDir = Path.Combine(env.Root, "Pläne", "Polierplan", "Haus 2", "OG2");
        Assert.True(fake.FileExists(Path.Combine(targetDir, "5998-300_OG2.pdf")));
        Assert.True(fake.FileExists(Path.Combine(targetDir, "5998-301_OG2.pdf")));
        Assert.NotNull(env.Repo.GetLastCompletedImportId()); // nicht undone
    }

    [Fact]
    public void Undo_AfterFixedCause_SecondAttemptSucceedsCompletely()
    {
        // Reparierbarkeit: nach behobener Ursache laeuft das Undo vollstaendig —
        // Dateien zurueck im Eingang, DB-Rollback + undone in einer Transaction.
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        Import(env, fake, "5998-300_OG2.pdf");
        var doc = env.Repo.GetDocumentByKey("polierplan|5998_300|haus_2|og2")!;

        fake.FailNext(FakeFileStore.FileOp.Move, "5998-300_OG2.pdf", times: 1);
        var undo = new ImportUndoService(env.Repo, fake, fake, fake);
        Assert.False(undo.UndoLastImport(env.Root).Success);

        var retry = undo.UndoLastImport(env.Root);

        Assert.True(retry.Success);
        Assert.True(fake.FileExists(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf")));
        Assert.Null(env.Repo.GetCurrentRevisionForDocument(doc.Id));
        Assert.Null(env.Repo.GetLastCompletedImportId()); // undone
    }
}
