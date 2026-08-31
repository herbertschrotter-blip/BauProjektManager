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
/// BPM-120 T5 (ADR-064 P.4, AK 8/9/11/12): Recovery Forward laeuft ueber den
/// gemeinsamen Apply-Pfad des Imports und stellt aus jedem zulaessigen
/// Zwischenzustand idempotent den Endzustand her — Dateisystem UND Plan-Cache.
/// </summary>
public class RecoveryForwardApplyPathTests
{
    private sealed class TestEnv : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "bpm-t5-virtual");

        public TestEnv()
        {
            IIdGenerator idGen = new UlidIdGenerator();
            var projectId = idGen.NewId();
            Repo = new PlanManagerDatabase(projectId, idGen,
                dbPathOverride: TempDb.NewTempDbPath(projectId));
        }

        public RecoveryExecutorService Recovery(FakeFileStore fake) =>
            new(Repo, fake, fake, fake,
                new ImportExecutionService(Repo, new UlidIdGenerator(), fake, fake, fake));

        public void SetActionStatus(string actionId, string status)
        {
            using var conn = new SqliteConnection($"Data Source={Repo.GetDatabasePath()}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE import_actions SET action_status = @s WHERE id = @id";
            cmd.Parameters.AddWithValue("@s", status);
            cmd.Parameters.AddWithValue("@id", actionId);
            cmd.ExecuteNonQuery();
        }

        public int CountEvents()
        {
            using var conn = new SqliteConnection($"Data Source={Repo.GetDatabasePath()}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM plan_revision_events";
            return Convert.ToInt32(cmd.ExecuteScalar());
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

    private const string Key = "polierplan|5998_300|haus_2|og2";
    private static readonly string TargetRelDir =
        Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2");
    private static readonly string TargetRel =
        Path.Combine(TargetRelDir, "5998-300_OG2.pdf");
    private static readonly string SourceRel =
        Path.Combine("_Eingang", "5998-300_OG2.pdf");

    /// <summary>Bestand: Dokument + current-Revision A (fremder Import) mit Datei am Ziel.</summary>
    private static (string DocId, string RevAId) SeedExistingDocument(TestEnv env)
    {
        var docId = env.Repo.ResolveOrCreateDocument("proj1", Key,
            "polierplan", "5998-300", "Polierplan", "", "Pläne", TargetRelDir, null, null);
        var now = DateTime.UtcNow.ToString("o");
        var oldImportId = env.Repo.CreateImportJournal("_Eingang", 1, profileId: null);
        env.Repo.CompleteImportJournal(oldImportId, success: true);
        var revAId = env.Repo.InsertRevision(docId, "A", "FileName",
            PlanArchive.Status.Current, now, null, now, lastImportId: oldImportId);
        return (docId, revAId);
    }

    private static string SeedPendingAction(
        TestEnv env, string importId, string actionType, string? archiveRel) =>
        env.Repo.InsertImportAction(importId, 0, actionType, Key, "5998-300",
            actionType == "indexUpdate" ? "B" : null, oldIndex: null,
            SourceRel, TargetRel, archiveRel,
            md5: "md5-new", fileSize: 3, documentTypeId: "polierplan");

    [Fact]
    public void Forward_CrashAfterRename_WritesFullDbStructure()
    {
        // AK 9: Datei liegt am Ziel (Rename passiert), DB fehlt komplett.
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        fake.AddFile(Path.Combine(env.Root, TargetRel), [9, 9, 9]);
        var importId = env.Repo.CreateImportJournal("_Eingang", 1, profileId: null);
        SeedPendingAction(env, importId, "new", archiveRel: null);

        var result = env.Recovery(fake).ExecuteForward(importId, env.Root);

        Assert.Equal(0, result.FailedCount);
        var doc = env.Repo.GetDocumentByKey(Key);
        Assert.NotNull(doc);
        var rev = Assert.Single(env.Repo.GetRevisionsForDocument(doc!.Id));
        Assert.Equal(PlanArchive.Status.Current, rev.RevisionStatus);
        Assert.True(fake.FileExists(Path.Combine(env.Root, TargetRel)));
    }

    [Fact]
    public void Forward_CrashAfterArchive_NoSecondArchiveCopyAndSupersedes()
    {
        // AK 8: Vorgaenger liegt bereits am journalisierten archive_path, die
        // neue Datei steht noch im Eingang. Forward stellt den Endzustand her —
        // ohne zweite Archivkopie, ohne Verlust des Vorgaengers.
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        var (docId, revAId) = SeedExistingDocument(env);
        var archiveRel = Path.Combine(TargetRelDir, "_Archiv", "5998-300_OG2_20260827_120000.pdf");
        fake.AddFile(Path.Combine(env.Root, archiveRel), [1, 1, 1]); // Vorgaenger archiviert
        fake.AddFile(Path.Combine(env.Root, SourceRel), [9, 9, 9]); // neue Datei wartet

        var importId = env.Repo.CreateImportJournal("_Eingang", 1, profileId: null);
        SeedPendingAction(env, importId, "indexUpdate", archiveRel);

        var result = env.Recovery(fake).ExecuteForward(importId, env.Root);

        Assert.Equal(0, result.FailedCount);
        // Disk: neue Datei am Ziel, GENAU eine Archivdatei mit Vorgaenger-Inhalt
        Assert.True(fake.FileExists(Path.Combine(env.Root, TargetRel)));
        var archived = Assert.Single(fake.EnumerateFiles(
            Path.Combine(env.Root, TargetRelDir, "_Archiv"), "*", recursive: true));
        Assert.Equal(Path.Combine(env.Root, archiveRel),
            Path.TrimEndingDirectorySeparator(archived));
        // DB: Revision A superseded, neue current-Revision B
        var revisions = env.Repo.GetRevisionsForDocument(docId);
        Assert.Equal(2, revisions.Count);
        Assert.Equal(PlanArchive.Status.Superseded,
            revisions.Single(r => r.Id == revAId).RevisionStatus);
        Assert.Equal("B", revisions.Single(r => r.Id != revAId).PlanIndex);
    }

    [Fact]
    public void Forward_RunTwice_NoDuplicateRevisionsFilesOrEvents()
    {
        // AK 12: derselbe Forward laeuft zweimal (Action zwischendurch wieder
        // pending) — keine zusaetzlichen Revisionen, Files, Events, Archivkopien.
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        fake.AddFile(Path.Combine(env.Root, SourceRel), [9, 9, 9]);
        var importId = env.Repo.CreateImportJournal("_Eingang", 1, profileId: null);
        var actionId = SeedPendingAction(env, importId, "new", archiveRel: null);

        var recovery = env.Recovery(fake);
        Assert.Equal(0, recovery.ExecuteForward(importId, env.Root).FailedCount);

        var doc = env.Repo.GetDocumentByKey(Key)!;
        var revisionsAfterFirst = env.Repo.GetRevisionsForDocument(doc.Id).Count;
        var eventsAfterFirst = env.CountEvents();

        // Zwischenzustand "Crash vor Journal-Abschluss": Action wieder pending
        env.SetActionStatus(actionId, "pending");
        Assert.Equal(0, recovery.ExecuteForward(importId, env.Root).FailedCount);

        Assert.Equal(revisionsAfterFirst, env.Repo.GetRevisionsForDocument(doc.Id).Count);
        Assert.Equal(eventsAfterFirst, env.CountEvents());
        var rev = Assert.Single(env.Repo.GetRevisionsForDocument(doc.Id));
        Assert.Single(env.Repo.GetFilesForRevision(rev.Id)); // kein zweites plan_file
    }
}
