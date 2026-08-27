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
            var dbPath = Repo.GetDatabasePath();
            Repo.Dispose();
            // BPM-120 T0: gezielter Pool-Clear statt ClearAllPools (Parallellast-Flaky)
            using (var pc = new SqliteConnection($"Data Source={dbPath}"))
                SqliteConnection.ClearPool(pc);
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
    public void UpsertSegment_SecondWriteSameType_ReplacesValue()
    {
        using var f = new TestDb();
        var docId = CreateDoc(f.Repo);
        f.Repo.UpsertSegment(docId, segmentTypeId: "haus", segmentKey: "haus", rawValue: "H5", normalizedValue: "h5");
        f.Repo.UpsertSegment(docId, "haus", "haus", "H6", "h6");

        // BPM-118: letzte Zuweisung gewinnt — genau eine Zeile je Segmenttyp.
        var seg = Assert.Single(f.Repo.GetSegmentsForDocument(docId));
        Assert.Equal("H6", seg.RawValue);
        Assert.Equal("h6", seg.NormalizedValue);
    }

    [Fact]
    public void GetPdfPathForRevision_PrefersPrimaryPdf_IgnoresDwg()
    {
        using var f = new TestDb();
        var docId = CreateDoc(f.Repo);
        var now = DateTime.UtcNow.ToString("o");
        var revId = f.Repo.InsertRevision(docId, "A", "FileName", PlanArchive.Status.Current, now, null, now, null);
        f.Repo.InsertFileForRevision(revId, "103_H5.dwg", "Plans/H5/103_H5.dwg", ".dwg", "md5-dwg", 10, isPrimary: false);
        f.Repo.InsertFileForRevision(revId, "103_H5.pdf", "Plans/H5/103_H5.pdf", ".pdf", "md5-pdf", 20, isPrimary: true);

        Assert.Equal("Plans/H5/103_H5.pdf", f.Repo.GetPdfPathForRevision(revId));
    }

    [Fact]
    public void GetPdfPathForRevision_NoPdfLinked_ReturnsNull()
    {
        using var f = new TestDb();
        var docId = CreateDoc(f.Repo);
        var now = DateTime.UtcNow.ToString("o");
        var revId = f.Repo.InsertRevision(docId, "A", "FileName", PlanArchive.Status.Current, now, null, now, null);
        f.Repo.InsertFileForRevision(revId, "103_H5.dwg", "Plans/H5/103_H5.dwg", ".dwg", "md5-dwg", 10, isPrimary: true);

        Assert.Null(f.Repo.GetPdfPathForRevision(revId));
    }

    [Fact]
    public void GetArchiveEntries_ReturnsCurrentRevisionWithPrimaryFile()
    {
        using var f = new TestDb();
        var docId = CreateDoc(f.Repo);
        var now = DateTime.UtcNow.ToString("o");
        var revId = f.Repo.InsertRevision(docId, "A", "FileName",
            PlanArchive.Status.Current, now, null, now, null);
        f.Repo.InsertFileForRevision(revId, "103_H5.dwg", "Plans/H5/103_H5.dwg", ".dwg", "md5-dwg", 10, isPrimary: false);
        f.Repo.InsertFileForRevision(revId, "103_H5.pdf", "Plans/H5/103_H5.pdf", ".pdf", "md5-pdf", 20, isPrimary: true);

        var entry = Assert.Single(f.Repo.GetArchiveEntries());

        Assert.Equal("103", entry.PlanNumber);
        Assert.Equal(revId, entry.RevisionId);
        Assert.Equal("A", entry.PlanIndex);
        Assert.Equal("103_H5.pdf", entry.FileName);            // Primärdatei, nicht die DWG
        Assert.Equal("Plans/H5/103_H5.pdf", entry.RelativePath);
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

    // === BPM-109.04 Revision-Zeitlogik + Events ===

    [Fact]
    public void Lifecycle_Supersede_OldSuperseded_NewCurrent_TimeTravelConsistent()
    {
        using var f = new TestDb();
        var docId = CreateDoc(f.Repo);

        var t0 = "2025-06-01T00:00:00.0000000Z";
        var rev1 = f.Repo.InsertRevision(docId, "A", "FileName", PlanArchive.Status.Current, t0, null, t0, null);
        f.Repo.InsertRevisionEvent(rev1, null, PlanArchive.EventType.Created);

        // Update: alte ablösen + neue current — EIN Zeitstempel t1 (wie ExecuteSingleAction.actionTime)
        var t1 = "2025-06-15T00:00:00.0000000Z";
        f.Repo.SupersedeCurrentRevision(docId, t1);
        f.Repo.InsertRevisionEvent(rev1, null, PlanArchive.EventType.Superseded);
        var rev2 = f.Repo.InsertRevision(docId, "B", "FileName", PlanArchive.Status.Current, t1, null, t1, null);
        f.Repo.InsertRevisionEvent(rev2, null, PlanArchive.EventType.Created);

        // current ist jetzt rev2
        var cur = f.Repo.GetCurrentRevisionForDocument(docId);
        Assert.Equal(rev2, cur!.Id);

        // Zeitreise-Konsistenz: superseded_at(alt) == current_from(neu) == t1 (kein Loch/keine Überlappung)
        var all = f.Repo.GetRevisionsForDocument(docId);
        var r1 = all.Single(r => r.Id == rev1);
        var r2 = all.Single(r => r.Id == rev2);
        Assert.Equal(PlanArchive.Status.Superseded, r1.RevisionStatus);
        Assert.Equal(t1, r1.SupersededAt);
        Assert.Equal(t1, r2.CurrentFrom);
        Assert.Equal(r1.SupersededAt, r2.CurrentFrom);

        // Event-Trail rev1: created + superseded
        var ev1 = f.Repo.GetRevisionEvents(rev1);
        Assert.Equal(2, ev1.Count);
        Assert.Contains(ev1, e => e.EventType == PlanArchive.EventType.Created);
        Assert.Contains(ev1, e => e.EventType == PlanArchive.EventType.Superseded);
    }

    [Fact]
    public void GetRevisionEvents_ReturnsFileLinkedEvent()
    {
        using var f = new TestDb();
        var docId = CreateDoc(f.Repo);
        var now = DateTime.UtcNow.ToString("o");
        var revId = f.Repo.InsertRevision(docId, "A", "FileName", PlanArchive.Status.Current, now, null, now, null);
        f.Repo.InsertRevisionEvent(revId, null, PlanArchive.EventType.FileLinked, "zusatz.dwg");

        var events = f.Repo.GetRevisionEvents(revId);
        Assert.Single(events);
        Assert.Equal(PlanArchive.EventType.FileLinked, events[0].EventType);
        Assert.Equal("zusatz.dwg", events[0].Note);
    }

    [Fact]
    public void InsertRevision_ReleasedAt_DefaultsNull_AndRoundTrips()
    {
        using var f = new TestDb();
        var docId = CreateDoc(f.Repo);
        var now = DateTime.UtcNow.ToString("o");

        // V1-Standardpfad: kein Freigabedatum → released_at bleibt NULL
        var rev1 = f.Repo.InsertRevision(docId, "A", "FileName", PlanArchive.Status.Superseded, now, now, now, null);
        // Späterer Pfad (OCR/manuell): Freigabedatum gesetzt
        var released = "2025-07-14T00:00:00.0000000Z";
        var rev2 = f.Repo.InsertRevision(docId, "B", "FileName", PlanArchive.Status.Current, now, null, now, null, releasedAt: released);

        var all = f.Repo.GetRevisionsForDocument(docId);
        Assert.Null(all.Single(r => r.Id == rev1).ReleasedAt);
        Assert.Equal(released, all.Single(r => r.Id == rev2).ReleasedAt);
        // received_at ist immer gesetzt (Fallback fürs Bautagebuch wenn released_at NULL)
        Assert.Equal(now, all.Single(r => r.Id == rev1).ReceivedAt);
    }

    [Fact]
    public void InsertRevision_ChangeNote_DefaultsEmpty_AndRoundTrips()
    {
        // Slice D (ADR-063): change_note der Revision — leer per Default,
        // befüllt via BPM-118 Text-Zuweisung aus der PDF-Vorschau
        using var f = new TestDb();
        var docId = CreateDoc(f.Repo);
        var now = DateTime.UtcNow.ToString("o");

        var rev1 = f.Repo.InsertRevision(docId, "A", "FileName", PlanArchive.Status.Superseded, now, now, now, null);
        var rev2 = f.Repo.InsertRevision(docId, "B", "FileName", PlanArchive.Status.Current, now, null, now, null,
            changeNote: "erg. Deckendurchbruch 20/20cm (S2)");

        var all = f.Repo.GetRevisionsForDocument(docId);
        Assert.Equal(string.Empty, all.Single(r => r.Id == rev1).ChangeNote);
        Assert.Equal("erg. Deckendurchbruch 20/20cm (S2)", all.Single(r => r.Id == rev2).ChangeNote);
        Assert.Equal("erg. Deckendurchbruch 20/20cm (S2)",
            f.Repo.GetCurrentRevisionForDocument(docId)!.ChangeNote);
    }
}
