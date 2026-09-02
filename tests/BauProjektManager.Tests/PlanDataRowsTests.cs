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
/// Plandaten-Ansicht (BPM-126): Der Import reicht die Stammdaten-IDs der
/// Radial-Zuordnung bis in plan_documents durch (Cross-DB SoftRef), und
/// GetPlanDataRows liefert die Anzeige-Felder inkl. Dateitypen und
/// Segment-Anzahl.
/// </summary>
public class PlanDataRowsTests
{
    private static readonly LocalFileSystem Fs = new();

    private sealed class TestEnv : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        public string Root { get; }

        public TestEnv()
        {
            IIdGenerator idGen = new UlidIdGenerator();
            var projectId = idGen.NewId();
            Repo = new PlanManagerDatabase(projectId, idGen,
                dbPathOverride: TempDb.NewTempDbPath(projectId));
            Root = Path.Combine(Path.GetTempPath(), "bpm-126-test-" + projectId);
            Directory.CreateDirectory(Path.Combine(Root, "_Eingang"));
        }

        public void Dispose()
        {
            var dbPath = Repo.GetDatabasePath();
            Repo.Dispose();
            using (var pc = new SqliteConnection($"Data Source={dbPath}"))
                SqliteConnection.ClearPool(pc);
            TempDb.Delete(dbPath);
            try { if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true); } catch { }
        }
    }

    private static PendingAssignment BuildPending(
        string fileName, string planNumber, string? index,
        string? partId, string? levelId,
        IReadOnlyList<AssignedSegmentValue>? segments = null)
    {
        var scan = new ScannedFile(
            Path.Combine("_Eingang", fileName), fileName,
            Path.GetExtension(fileName), 12, DateTime.UtcNow);
        return new PendingAssignment(
            new FingerprintedFile(scan, "md5-" + planNumber + Path.GetExtension(fileName)),
            CaptureBucket.NewCapture,
            "polierplan", "Polierplan", "H1", "EG", planNumber, index,
            Path.Combine("Pläne", "Polierplan", "H1", "EG"), Match: null,
            Title: "Grundriss EG",
            ChangeNote: index is null ? null : "Durchbruch ergaenzt",
            ReleasedAt: index is null ? null : "2026-08-20T00:00:00Z",
            AssignedSegments: segments,
            BuildingPartId: partId,
            BuildingLevelId: levelId);
    }

    [Fact]
    public void Import_PersistsBuildingPartAndLevelIds()
    {
        using var env = new TestEnv();
        const string fileName = "6100-102_Polierplan_H1_EG.pdf";
        File.WriteAllText(Path.Combine(env.Root, "_Eingang", fileName), "plan");

        var pending = BuildPending(fileName, "6100-102", null, "part-h1", "level-eg");
        var decisions = CaptureConfirmService.BuildDecisions([pending], new PlanValueNormalizer());
        var result = new ImportExecutionService(env.Repo, new UlidIdGenerator(), Fs, Fs, Fs)
            .Execute(decisions, env.Root, "_Eingang");

        Assert.Equal(0, result.Failed);
        var row = Assert.Single(env.Repo.GetPlanDataRows());
        Assert.Equal("part-h1", row.BuildingPartId);
        Assert.Equal("level-eg", row.BuildingLevelId);
    }

    [Fact]
    public void GetPlanDataRows_ReturnsDisplayFields()
    {
        using var env = new TestEnv();
        const string fileName = "6100-140_B_Polierplan_H1_EG.pdf";
        File.WriteAllText(Path.Combine(env.Root, "_Eingang", fileName), "plan");

        var pending = BuildPending(fileName, "6100-140", "B", "part-h1", "level-eg",
            [new AssignedSegmentValue("planart", "planart", "Polierplan")]);
        var decisions = CaptureConfirmService.BuildDecisions([pending], new PlanValueNormalizer());
        new ImportExecutionService(env.Repo, new UlidIdGenerator(), Fs, Fs, Fs)
            .Execute(decisions, env.Root, "_Eingang");

        var row = Assert.Single(env.Repo.GetPlanDataRows());
        Assert.Equal("6100-140", row.PlanNumber);
        Assert.Equal("B", row.PlanIndex);
        Assert.Equal("Grundriss EG", row.Title);
        Assert.Equal("Polierplan", row.DocumentType);
        Assert.Equal("Durchbruch ergaenzt", row.ChangeNote);
        Assert.Equal("2026-08-20T00:00:00Z", row.ReleasedAt);
        Assert.Equal("PDF", row.FileTypes);
        Assert.Equal(1, row.SegmentCount);
    }

    [Fact]
    public void GetPlanDataRows_PairShowsBothFileTypes()
    {
        using var env = new TestEnv();
        foreach (var name in new[] { "6100-130.pdf", "6100-130.dwg" })
            File.WriteAllText(Path.Combine(env.Root, "_Eingang", name), "plan-" + name);

        var pendings = new[]
        {
            BuildPending("6100-130.pdf", "6100-130", null, "part-h1", "level-eg"),
            BuildPending("6100-130.dwg", "6100-130", null, "part-h1", "level-eg")
        };
        var decisions = CaptureConfirmService.BuildDecisions(pendings, new PlanValueNormalizer());
        new ImportExecutionService(env.Repo, new UlidIdGenerator(), Fs, Fs, Fs)
            .Execute(decisions, env.Root, "_Eingang");

        var row = Assert.Single(env.Repo.GetPlanDataRows());
        Assert.NotNull(row.FileTypes);
        Assert.Contains("PDF", row.FileTypes);
        Assert.Contains("DWG", row.FileTypes);
    }

    [Fact]
    public void GetPlanDataRows_EmptyDatabase_ReturnsNothing()
    {
        using var env = new TestEnv();
        Assert.Empty(env.Repo.GetPlanDataRows());
    }
}
