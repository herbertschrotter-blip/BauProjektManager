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
/// Tag-System (BPM-127): freie Schlagworte je Dokument — getrennt von den
/// Dateinamens-Segmenten (BPM-108). Normalisierung schuetzt vor Duplikaten,
/// Entfernen ist Soft Delete, Vorschlaege kommen nach Haeufigkeit.
/// </summary>
public class PlanDocumentTagsTests
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
            Root = Path.Combine(Path.GetTempPath(), "bpm-127-test-" + projectId);
            Directory.CreateDirectory(Path.Combine(Root, "_Eingang"));
        }

        /// <summary>Legt ein importiertes Dokument an und liefert dessen Id.</summary>
        public string SeedDocument(string planNumber)
        {
            var fileName = planNumber + ".pdf";
            File.WriteAllText(Path.Combine(Root, "_Eingang", fileName), "plan-" + planNumber);
            var scan = new ScannedFile(
                Path.Combine("_Eingang", fileName), fileName, ".pdf", 12, DateTime.UtcNow);
            var pending = new PendingAssignment(
                new FingerprintedFile(scan, "md5-" + planNumber), CaptureBucket.NewCapture,
                "polierplan", "Polierplan", "H1", "EG", planNumber, null,
                Path.Combine("Pläne", "H1", "EG"), Match: null);

            var decisions = CaptureConfirmService.BuildDecisions([pending], new PlanValueNormalizer());
            new ImportExecutionService(Repo, new UlidIdGenerator(), Fs, Fs, Fs)
                .Execute(decisions, Root, "_Eingang");
            return Repo.GetDocumentByKey(decisions[0].DocumentKey!)!.Id;
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

    [Fact]
    public void AddTag_StoresAndReadsBack()
    {
        using var env = new TestEnv();
        var docId = env.SeedDocument("6100-101");

        Assert.True(env.Repo.AddTag(docId, "Beton C25/30"));
        Assert.True(env.Repo.AddTag(docId, "Deckendurchbruch"));

        Assert.Equal(["Beton C25/30", "Deckendurchbruch"], env.Repo.GetTagsForDocument(docId));
    }

    [Fact]
    public void AddTag_SameTagDifferentCase_StaysSingleEntry()
    {
        using var env = new TestEnv();
        var docId = env.SeedDocument("6100-102");

        env.Repo.AddTag(docId, "Fundament");
        env.Repo.AddTag(docId, "  FUNDAMENT  ");

        // Normalisiert identisch -> ein Eintrag, Anzeigetext ist der letzte
        var tag = Assert.Single(env.Repo.GetTagsForDocument(docId));
        Assert.Equal("FUNDAMENT", tag);
    }

    [Fact]
    public void AddTag_EmptyOrWhitespace_IsIgnored()
    {
        using var env = new TestEnv();
        var docId = env.SeedDocument("6100-103");

        Assert.False(env.Repo.AddTag(docId, "   "));
        Assert.False(env.Repo.AddTag(docId, ""));
        Assert.Empty(env.Repo.GetTagsForDocument(docId));
    }

    [Fact]
    public void RemoveTag_RemovesFromDocument_AndCanBeReAdded()
    {
        using var env = new TestEnv();
        var docId = env.SeedDocument("6100-104");
        env.Repo.AddTag(docId, "Wanddurchbruch");

        env.Repo.RemoveTag(docId, "wanddurchbruch"); // Gross-/Kleinschreibung egal
        Assert.Empty(env.Repo.GetTagsForDocument(docId));

        // Soft Delete darf das erneute Setzen nicht blockieren (ON CONFLICT reaktiviert)
        env.Repo.AddTag(docId, "Wanddurchbruch");
        Assert.Equal(["Wanddurchbruch"], env.Repo.GetTagsForDocument(docId));
    }

    [Fact]
    public void GetAllTags_ReturnsProjectWideSuggestions_MostUsedFirst()
    {
        using var env = new TestEnv();
        var doc1 = env.SeedDocument("6100-105");
        var doc2 = env.SeedDocument("6100-106");

        env.Repo.AddTag(doc1, "Beton");
        env.Repo.AddTag(doc2, "Beton");
        env.Repo.AddTag(doc1, "Aussparung");

        Assert.Equal(["Beton", "Aussparung"], env.Repo.GetAllTags());
    }

    [Fact]
    public void GetPlanDataRows_ExposesTagsForTheTable()
    {
        using var env = new TestEnv();
        var docId = env.SeedDocument("6100-107");
        env.Repo.AddTag(docId, "Beton C25/30");
        env.Repo.AddTag(docId, "Deckendurchbruch");

        var row = Assert.Single(env.Repo.GetPlanDataRows());
        Assert.NotNull(row.Tags);
        Assert.Contains("Beton C25/30", row.Tags);
        Assert.Contains("Deckendurchbruch", row.Tags);
    }

    [Fact]
    public void GetPlanDataRows_WithoutTags_LeavesTagsNull()
    {
        using var env = new TestEnv();
        env.SeedDocument("6100-108");

        var row = Assert.Single(env.Repo.GetPlanDataRows());
        Assert.Null(row.Tags);
    }
}
