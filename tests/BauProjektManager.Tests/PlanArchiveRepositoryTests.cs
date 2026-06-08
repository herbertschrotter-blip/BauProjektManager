using System.IO;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// Unit-Tests für die Schema-v2.0 Repository-Primitive in <see cref="PlanManagerDatabase"/> (BPM-109.02).
/// Echte SQLite-DB pro Test (planmanager.db unter LocalAppData), Cleanup analog RecoveryTestFixture.
/// Verifiziert Document-Resolve, Revision/Segment-Insert, Current-Lookup + die Schema-Constraints
/// (Unique-current-Revision, Unique-Segmenttyp pro Dokument).
/// </summary>
public class PlanArchiveRepositoryTests
{
    private const string Key = "polierplan|103|h5";

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
            catch { /* Pool-Lock unter Windows — best effort, wie RecoveryTestFixture */ }
        }
    }

    private static string CreateDoc(PlanManagerDatabase db, string key = Key)
        => db.ResolveOrCreateDocument(
            projectId: "proj1", documentKey: key, documentTypeId: "polierplan",
            planNumber: "103", documentType: "Polierplan", title: "",
            targetFolder: "Plans", relativeDirectory: "Plans/H5",
            buildingPartId: null, buildingLevelId: null);

    [Fact]
    public void ResolveOrCreateDocument_IsIdempotent_ByDocumentKey()
    {
        using var f = new TestDb();
        var id1 = CreateDoc(f.Repo);
        var id2 = CreateDoc(f.Repo);
        Assert.Equal(id1, id2);

        var doc = f.Repo.GetDocumentByKey(Key);
        Assert.NotNull(doc);
        Assert.Equal(id1, doc!.Id);
        Assert.Equal("103", doc.PlanNumber);
        Assert.Null(doc.BuildingPartId);   // SoftRef, nicht gemappt
    }

    [Fact]
    public void InsertRevision_GetCurrentRevisionForDocument_RoundTrips()
    {
        using var f = new TestDb();
        var docId = CreateDoc(f.Repo);
        var now = DateTime.UtcNow.ToString("o");
        var revId = f.Repo.InsertRevision(docId, planIndex: "A", indexSource: "FileName",
            revisionStatus: PlanArchive.Status.Current, currentFrom: now, supersededAt: null,
            receivedAt: now, lastImportId: null);

        var rev = f.Repo.GetCurrentRevisionForDocument(docId);
        Assert.NotNull(rev);
        Assert.Equal(revId, rev!.Id);
        Assert.Equal("A", rev.PlanIndex);
        Assert.Equal(PlanArchive.Status.Current, rev.RevisionStatus);
        Assert.Null(rev.SupersededAt);
    }

    [Fact]
    public void GetCurrentRevisionLookup_ReturnsCurrentRevision_KeyedByDocumentKey()
    {
        using var f = new TestDb();
        var docId = CreateDoc(f.Repo);
        var now = DateTime.UtcNow.ToString("o");
        var revId = f.Repo.InsertRevision(docId, "A", "FileName",
            PlanArchive.Status.Current, now, null, now, null);

        var lookup = f.Repo.GetCurrentRevisionLookup();
        Assert.True(lookup.ContainsKey(Key));
        Assert.Equal(revId, lookup[Key].RevisionId);
        Assert.Equal("A", lookup[Key].LatestIndex);
        Assert.Equal("", lookup[Key].Md5);   // noch keine Datei verknüpft (LEFT JOIN)
    }

    [Fact]
    public void UxCurrentRevision_SecondCurrentForSameDocument_Throws()
    {
        using var f = new TestDb();
        var docId = CreateDoc(f.Repo);
        var now = DateTime.UtcNow.ToString("o");
        f.Repo.InsertRevision(docId, "A", "FileName", PlanArchive.Status.Current, now, null, now, null);

        // ux_plan_revisions_current: max. eine 'current' Revision pro document_id.
        Assert.Throws<SqliteException>(() =>
            f.Repo.InsertRevision(docId, "B", "FileName", PlanArchive.Status.Current, now, null, now, null));
    }

    [Fact]
    public void Superseded_And_Current_CoexistForSameDocument()
    {
        using var f = new TestDb();
        var docId = CreateDoc(f.Repo);
        var now = DateTime.UtcNow.ToString("o");
        // Unique-Index gilt nur für 'current' — eine superseded + eine current koexistieren.
        f.Repo.InsertRevision(docId, "A", "FileName", PlanArchive.Status.Superseded, now, now, now, null);
        var curId = f.Repo.InsertRevision(docId, "B", "FileName", PlanArchive.Status.Current, now, null, now, null);

        var current = f.Repo.GetCurrentRevisionForDocument(docId);
        Assert.NotNull(current);
        Assert.Equal(curId, current!.Id);
        Assert.Equal("B", current.PlanIndex);
    }

    [Fact]
    public void InsertSegment_DuplicateTypePerDocument_Throws()
    {
        using var f = new TestDb();
        var docId = CreateDoc(f.Repo);
        f.Repo.InsertSegment(docId, segmentTypeId: "haus", segmentKey: "haus", rawValue: "H5", normalizedValue: "h5");

        // UNIQUE (document_id, segment_type_id): pro Dokument ein Wert je Segmenttyp.
        Assert.Throws<SqliteException>(() =>
            f.Repo.InsertSegment(docId, "haus", "haus", "H6", "h6"));
    }

    [Fact]
    public void InsertRevisionEvent_PersistsWithoutError()
    {
        using var f = new TestDb();
        var docId = CreateDoc(f.Repo);
        var now = DateTime.UtcNow.ToString("o");
        var revId = f.Repo.InsertRevision(docId, "A", "FileName", PlanArchive.Status.Current, now, null, now, null);

        var eventId = f.Repo.InsertRevisionEvent(revId, importId: null,
            eventType: PlanArchive.EventType.Created, note: "Erstausgabe");
        Assert.False(string.IsNullOrEmpty(eventId));
    }
}
