using System.IO;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// E2E-Test der BPM-118-Persistenz (Teil 3): Pending Assignment mit
/// Text-Zuweisungen (change_note / released_at / Segmente) → BuildDecisions →
/// Execute → plan_revisions + plan_document_segments. Echte SQLite-DB +
/// echtes Temp-Projektverzeichnis, analog ImportUndoServiceTests.
/// </summary>
public class ImportExecutionBpm118Tests
{
    // BPM-120 T1: E2E-Tests laufen bewusst auf echter Disk — eine Port-Instanz.
    private static readonly LocalFileSystem Fs = new();

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
            Root = Path.Combine(Path.GetTempPath(), "bpm-118-test-" + projectId);
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

    [Fact]
    public void Execute_PersistsChangeNoteReleasedAtAndSegments()
    {
        using var env = new TestEnv();
        const string fileName = "5998-300-B_OG2.pdf";
        File.WriteAllText(Path.Combine(env.Root, "_Eingang", fileName), "plan-content");

        var scan = new ScannedFile(
            Path.Combine("_Eingang", fileName), fileName, ".pdf", 12, DateTime.UtcNow);
        var pending = new PendingAssignment(
            new FingerprintedFile(scan, "md5-x"), CaptureBucket.NewCapture,
            "polierplan", "Polierplan", "Haus 2", "OG2", "5998-300", "B",
            Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2"), Match: null,
            Title: "Grundriss OG2",
            ChangeNote: "Achsraster geändert",
            ReleasedAt: "2026-08-20T00:00:00Z",
            AssignedSegments: [new AssignedSegmentValue("planart", "planart", "Statik")]);

        var decisions = CaptureConfirmService.BuildDecisions([pending], new PlanValueNormalizer());
        var result = new ImportExecutionService(env.Repo, new UlidIdGenerator(), Fs, Fs, Fs)
            .Execute(decisions, env.Root, "_Eingang");

        Assert.Equal(0, result.Failed);
        Assert.Equal(1, result.Succeeded);

        var doc = env.Repo.GetDocumentByKey(decisions[0].DocumentKey!);
        Assert.NotNull(doc);
        var rev = Assert.Single(env.Repo.GetRevisionsForDocument(doc!.Id));
        Assert.Equal("Achsraster geändert", rev.ChangeNote);
        Assert.Equal("2026-08-20T00:00:00Z", rev.ReleasedAt);

        var seg = Assert.Single(env.Repo.GetSegmentsForDocument(doc.Id));
        Assert.Equal("planart", seg.SegmentTypeId);
        Assert.Equal("planart", seg.SegmentKey);
        Assert.Equal("Statik", seg.RawValue);
        Assert.Equal("statik", seg.NormalizedValue);
    }

    [Fact]
    public void Execute_WithoutBpm118Fields_KeepsDefaults()
    {
        // Regressionsschutz: Strecke ohne Text-Zuweisungen bleibt unverändert
        // (change_note leer, released_at NULL, keine Segmente).
        using var env = new TestEnv();
        const string fileName = "5998-301_OG3.pdf";
        File.WriteAllText(Path.Combine(env.Root, "_Eingang", fileName), "plan-content");

        var scan = new ScannedFile(
            Path.Combine("_Eingang", fileName), fileName, ".pdf", 12, DateTime.UtcNow);
        var pending = new PendingAssignment(
            new FingerprintedFile(scan, "md5-y"), CaptureBucket.NewCapture,
            "polierplan", "Polierplan", "Haus 2", "OG3", "5998-301", null,
            Path.Combine("Pläne", "Polierplan", "Haus 2", "OG3"), Match: null);

        var decisions = CaptureConfirmService.BuildDecisions([pending], new PlanValueNormalizer());
        var result = new ImportExecutionService(env.Repo, new UlidIdGenerator(), Fs, Fs, Fs)
            .Execute(decisions, env.Root, "_Eingang");

        Assert.Equal(0, result.Failed);
        var doc = env.Repo.GetDocumentByKey(decisions[0].DocumentKey!);
        Assert.NotNull(doc);
        var rev = Assert.Single(env.Repo.GetRevisionsForDocument(doc!.Id));
        Assert.Equal("", rev.ChangeNote);
        Assert.Null(rev.ReleasedAt);
        Assert.Empty(env.Repo.GetSegmentsForDocument(doc.Id));
    }
}
