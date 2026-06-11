using System.IO;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// DB-Tests fuer die beiden read-only Capture-Lookups in <see cref="PlanManagerDatabase"/>
/// (BPM-111.03): GetCurrentDocumentLookup + GetKnownMd5Lookup.
/// Echte SQLite-DB pro Test, Muster analog PlanArchiveRepositoryTests.
/// </summary>
public class ManualCaptureLookupTests
{
    private sealed class TestDb : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        private readonly string _folder;

        public TestDb()
        {
            IIdGenerator idGen = new UlidIdGenerator();
            var projectId = idGen.NewId();
            _folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BauProjektManager", "Projects", projectId);
            Repo = new PlanManagerDatabase(projectId, idGen);
        }

        public void Dispose()
        {
            Repo.Dispose();
            SqliteConnection.ClearAllPools();
            try { if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true); }
            catch { /* Pool-Lock unter Windows — best effort */ }
        }
    }

    [Fact]
    public void GetCurrentDocumentLookup_ReturnsDocumentWithCurrentIndex()
    {
        using var f = new TestDb();
        var docId = f.Repo.ResolveOrCreateDocument(
            projectId: "proj1", documentKey: "polierplan|5998_100|h1",
            documentTypeId: "polierplan", planNumber: "5998-100",
            documentType: "Polierplan", title: "",
            targetFolder: "Polierplan", relativeDirectory: "Pläne/Polierplan/Haus 1",
            buildingPartId: null, buildingLevelId: null);
        var revId = f.Repo.InsertRevision(docId, planIndex: "A", indexSource: "FileName",
            PlanArchive.Status.Current, "2026-06-01T00:00:00Z", null, "2026-06-01T00:00:00Z", lastImportId: null);

        var lookup = f.Repo.GetCurrentDocumentLookup();

        var doc = Assert.Single(lookup);
        Assert.Equal(docId, doc.DocumentId);
        Assert.Equal("5998-100", doc.PlanNumber);
        Assert.Equal("A", doc.CurrentIndex);
        Assert.Equal(revId, doc.CurrentRevisionId);
        Assert.Equal("Pläne/Polierplan/Haus 1", doc.RelativeDirectory);
    }

    [Fact]
    public void GetKnownMd5Lookup_ReturnsLinkedFileHashes()
    {
        using var f = new TestDb();
        var docId = f.Repo.ResolveOrCreateDocument(
            projectId: "proj1", documentKey: "polierplan|103|h5",
            documentTypeId: "polierplan", planNumber: "103",
            documentType: "Polierplan", title: "",
            targetFolder: "Plans", relativeDirectory: "Plans/H5",
            buildingPartId: null, buildingLevelId: null);
        var revId = f.Repo.InsertRevision(docId, planIndex: null, indexSource: "None",
            PlanArchive.Status.Current, "2026-06-01T00:00:00Z", null, "2026-06-01T00:00:00Z", lastImportId: null);
        f.Repo.InsertFileForRevision(revId, "103_EG.pdf", "Plans/H5/103_EG.pdf",
            ".pdf", "abc123def", 1234, isPrimary: true);

        var lookup = f.Repo.GetKnownMd5Lookup();

        Assert.True(lookup.TryGetValue("abc123def", out var key));
        Assert.Equal("polierplan|103|h5", key);
    }
}
