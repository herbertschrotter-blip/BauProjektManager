using System.IO;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// E2E-Test des PDF+DWG-Paar-Imports (BPM-111.07 Slice A): zwei Pending
/// Assignments mit gleicher Identität → EIN Dokument, EINE Revision, zwei
/// Dateien — die PDF legt die Revision an (Sortierung in BuildDecisions),
/// die DWG dockt über den FileLinked-Zweig der Execution an.
/// Echte SQLite-DB + Temp-Projektverzeichnis, analog ImportUndoServiceTests.
/// </summary>
public class ImportExecutionPairImportTests
{
    private sealed class TestEnv : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        public string Root { get; }
        private readonly string _dbFolder;

        public TestEnv()
        {
            IIdGenerator idGen = new UlidIdGenerator();
            var projectId = idGen.NewId();
            _dbFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BauProjektManager", "Projects", projectId);
            Repo = new PlanManagerDatabase(projectId, idGen);
            Root = Path.Combine(Path.GetTempPath(), "bpm-pair-test-" + projectId);
            Directory.CreateDirectory(Path.Combine(Root, "_Eingang"));
        }

        public void Dispose()
        {
            var dbPath = Repo.GetDatabasePath();
            Repo.Dispose();
            // BPM-120 T0: gezielter Pool-Clear statt ClearAllPools (Parallellast-Flaky)
            using (var pc = new SqliteConnection($"Data Source={dbPath}"))
                SqliteConnection.ClearPool(pc);
            try { if (Directory.Exists(_dbFolder)) Directory.Delete(_dbFolder, recursive: true); } catch { }
            try { if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true); } catch { }
        }
    }

    private static PendingAssignment Pending(string fileName, string md5)
    {
        var scan = new ScannedFile(
            Path.Combine("_Eingang", fileName), fileName,
            Path.GetExtension(fileName), 12, DateTime.UtcNow);
        return new PendingAssignment(
            new FingerprintedFile(scan, md5), CaptureBucket.NewCapture,
            "polierplan", "Polierplan", "Haus 2", "OG2", "5998-300", null,
            Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2"), Match: null);
    }

    [Fact]
    public void Execute_PdfDwgPair_CreatesOneRevisionWithBothFiles()
    {
        using var env = new TestEnv();
        File.WriteAllText(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf"), "pdf-content");
        File.WriteAllText(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.dwg"), "dwg-content");

        // DWG bewusst ZUERST im Pending — die PDF-vor-DWG-Sortierung muss greifen.
        var decisions = CaptureConfirmService.BuildDecisions(
            [Pending("5998-300_OG2.dwg", "md5-dwg"), Pending("5998-300_OG2.pdf", "md5-pdf")],
            new PlanValueNormalizer());
        var result = new ImportExecutionService(env.Repo, new UlidIdGenerator())
            .Execute(decisions, env.Root, "_Eingang");

        Assert.Equal(0, result.Failed);
        Assert.Equal(2, result.Succeeded);

        var doc = env.Repo.GetDocumentByKey(decisions[0].DocumentKey!);
        Assert.NotNull(doc);
        var rev = Assert.Single(env.Repo.GetRevisionsForDocument(doc!.Id));
        Assert.Equal(
            Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2", "5998-300_OG2.pdf"),
            env.Repo.GetPdfPathForRevision(rev.Id));

        var targetDir = Path.Combine(env.Root, "Pläne", "Polierplan", "Haus 2", "OG2");
        Assert.True(File.Exists(Path.Combine(targetDir, "5998-300_OG2.pdf")));
        Assert.True(File.Exists(Path.Combine(targetDir, "5998-300_OG2.dwg")));
        Assert.False(File.Exists(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf")));
        Assert.False(File.Exists(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.dwg")));
    }

    [Fact]
    public void Execute_PdfDwgPairAsUpdate_SupersedesOnceAndLinksDwgToNewRevision()
    {
        // Slice A2: Zwei UpdateNewerIndex-Aktionen auf dasselbe Dokument im
        // selben Import (PDF+DWG-Paar) — ohne den Import-Guard würde die zweite
        // Aktion die frisch angelegte Revision gleich wieder superseden.
        using var env = new TestEnv();
        var targetRelDir = Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2");
        var targetAbsDir = Path.Combine(env.Root, targetRelDir);
        Directory.CreateDirectory(targetAbsDir);

        // Bestand: Dokument + current Revision A mit alter PDF+DWG im Ziel
        const string key = "polierplan|5998_300|haus_2|og2";
        var docId = env.Repo.ResolveOrCreateDocument("proj1", key,
            "polierplan", "5998-300", "Polierplan", "", "Pläne", targetRelDir, null, null);
        var now = DateTime.UtcNow.ToString("o");
        var oldImportId = env.Repo.CreateImportJournal("_Eingang", 1, profileId: null);
        env.Repo.CompleteImportJournal(oldImportId, success: true);
        var revA = env.Repo.InsertRevision(docId, "A", "FileName",
            PlanArchive.Status.Current, now, null, now, lastImportId: oldImportId);

        // Eingang: neues Paar mit Index B
        File.WriteAllText(Path.Combine(env.Root, "_Eingang", "5998-300-B_OG2.pdf"), "new-pdf");
        File.WriteAllText(Path.Combine(env.Root, "_Eingang", "5998-300-B_OG2.dwg"), "new-dwg");

        var match = new KnownPlanDocument(docId, key, "5998-300", "Polierplan",
            "Pläne", targetRelDir, "A", revA);
        PendingAssignment UpdatePending(string fileName, string md5)
        {
            var scan = new ScannedFile(
                Path.Combine("_Eingang", fileName), fileName,
                Path.GetExtension(fileName), 12, DateTime.UtcNow);
            return new PendingAssignment(
                new FingerprintedFile(scan, md5), CaptureBucket.UpdateProposal,
                docId, "Polierplan", null, null, "5998-300", "B",
                targetRelDir, match);
        }

        // DWG bewusst zuerst — Sortierung + Guard müssen zusammen greifen.
        var decisions = CaptureConfirmService.BuildDecisions(
            [UpdatePending("5998-300-B_OG2.dwg", "md5-dwg-b"), UpdatePending("5998-300-B_OG2.pdf", "md5-pdf-b")],
            new PlanValueNormalizer());
        var result = new ImportExecutionService(env.Repo, new UlidIdGenerator())
            .Execute(decisions, env.Root, "_Eingang");

        Assert.Equal(0, result.Failed);
        Assert.Equal(2, result.Succeeded);

        var revisions = env.Repo.GetRevisionsForDocument(docId);
        Assert.Equal(2, revisions.Count);
        var current = env.Repo.GetCurrentRevisionForDocument(docId);
        Assert.NotNull(current);
        Assert.Equal("B", current!.PlanIndex);
        Assert.Equal(PlanArchive.Status.Superseded,
            revisions.Single(r => r.Id == revA).RevisionStatus);
        Assert.Equal(
            Path.Combine(targetRelDir, "5998-300-B_OG2.pdf"),
            env.Repo.GetPdfPathForRevision(current.Id));

        // Beide neuen Dateien liegen am Ziel
        Assert.True(File.Exists(Path.Combine(targetAbsDir, "5998-300-B_OG2.pdf")));
        Assert.True(File.Exists(Path.Combine(targetAbsDir, "5998-300-B_OG2.dwg")));
    }
}
