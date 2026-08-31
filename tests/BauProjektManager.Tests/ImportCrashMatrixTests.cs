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
/// BPM-120 T8 (ADR-064): Fault-/Crash-Matrix — Abbruch nach jedem Schritt einer
/// Update-Action (Journal → Archiv → tmp-Move → Rename → DB-Commit) × Forward/
/// Rollback/Undo. Jeder Zwischenzustand wird direkt konstruiert (FakeFileStore
/// + echte SQLite-Temp-DB); Recovery Forward muss aus JEDEM Zustand denselben
/// Endzustand herstellen (AK 4–13), Undo darf die DB nie inkonsistent
/// hinterlassen (AK 14). Alter Inhalt = 3 Bytes, neuer Inhalt = 4 Bytes —
/// die Laenge identifiziert die Datei.
/// </summary>
public class ImportCrashMatrixTests
{
    public enum CrashPoint
    {
        AfterJournal,    // C0: nichts mutiert
        AfterArchive,    // C1: Vorgaenger im Archiv, Ziel leer, Quelle im Eingang
        AfterTmpMove,    // C2: Quelle weg, <ziel>.bpm_tmp liegt
        AfterRename,     // C3: neue Datei am Ziel, DB fehlt
        AfterDbCommit    // C4: alles da, nur Journal-Abschluss fehlt
    }

    private static readonly byte[] OldContent = [1, 1, 1];
    private static readonly byte[] NewContent = [9, 9, 9, 9];

    private const string Key = "polierplan|5998_300|haus_2|og2";
    private static readonly string TargetRelDir =
        Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2");
    private static readonly string TargetRel =
        Path.Combine(TargetRelDir, "5998-300_OG2.pdf");
    private static readonly string ArchiveRel =
        Path.Combine(TargetRelDir, "_Archiv", "5998-300_OG2_20260827_120000.pdf");
    private static readonly string SourceRel =
        Path.Combine("_Eingang", "5998-300_OG2.pdf");

    private sealed class TestEnv : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "bpm-t8-virtual");
        public FakeFileStore Fake { get; } = new();
        public string DocId { get; private set; } = "";
        public string RevAId { get; private set; } = "";
        public string ImportId { get; private set; } = "";
        public string ActionId { get; private set; } = "";

        public TestEnv()
        {
            IIdGenerator idGen = new UlidIdGenerator();
            var projectId = idGen.NewId();
            Repo = new PlanManagerDatabase(projectId, idGen,
                dbPathOverride: TempDb.NewTempDbPath(projectId));
        }

        public RecoveryExecutorService Recovery() =>
            new(Repo, Fake, Fake, Fake,
                new ImportExecutionService(Repo, new UlidIdGenerator(), Fake, Fake, Fake));

        /// <summary>Bestand (Doc + Revision A, fremder Import) + pending Update-Action im Journal.</summary>
        public void SeedJournaledUpdateAction()
        {
            DocId = Repo.ResolveOrCreateDocument("proj1", Key,
                "polierplan", "5998-300", "Polierplan", "", "Pläne", TargetRelDir, null, null);
            var now = DateTime.UtcNow.ToString("o");
            var oldImportId = Repo.CreateImportJournal("_Eingang", 1, profileId: null);
            Repo.CompleteImportJournal(oldImportId, success: true);
            RevAId = Repo.InsertRevision(DocId, "A", "FileName",
                PlanArchive.Status.Current, now, null, now, lastImportId: oldImportId);

            ImportId = Repo.CreateImportJournal("_Eingang", 1, profileId: null);
            ActionId = Repo.InsertImportAction(ImportId, 0, "indexUpdate", Key,
                "5998-300", "B", oldIndex: null, SourceRel, TargetRel, ArchiveRel,
                md5: "md5-new", fileSize: NewContent.Length, documentTypeId: "polierplan");
        }

        /// <summary>Stellt den Disk-(+DB-)Zustand des jeweiligen Crash-Punkts her.</summary>
        public void ArrangeCrashState(CrashPoint point)
        {
            switch (point)
            {
                case CrashPoint.AfterJournal:
                    Fake.AddFile(Path.Combine(Root, TargetRel), OldContent);
                    Fake.AddFile(Path.Combine(Root, SourceRel), NewContent);
                    break;
                case CrashPoint.AfterArchive:
                    Fake.AddFile(Path.Combine(Root, ArchiveRel), OldContent);
                    Fake.AddFile(Path.Combine(Root, SourceRel), NewContent);
                    break;
                case CrashPoint.AfterTmpMove:
                    Fake.AddFile(Path.Combine(Root, ArchiveRel), OldContent);
                    Fake.AddFile(Path.Combine(Root, TargetRel) + ImportExecutionService.TmpSuffix, NewContent);
                    break;
                case CrashPoint.AfterRename:
                    Fake.AddFile(Path.Combine(Root, ArchiveRel), OldContent);
                    Fake.AddFile(Path.Combine(Root, TargetRel), NewContent);
                    break;
                case CrashPoint.AfterDbCommit:
                    Fake.AddFile(Path.Combine(Root, ArchiveRel), OldContent);
                    Fake.AddFile(Path.Combine(Root, TargetRel), NewContent);
                    var t = DateTime.UtcNow.ToString("o");
                    Repo.SupersedeCurrentRevision(DocId, t);
                    var revB = Repo.InsertRevision(DocId, "B", "FileName",
                        PlanArchive.Status.Current, t, null, t, lastImportId: ImportId);
                    Repo.InsertFileForRevision(revB, "5998-300_OG2.pdf", TargetRel,
                        ".pdf", "md5-new", NewContent.Length, isPrimary: true);
                    SetActionStatus("completed");
                    break;
            }
        }

        public void SetActionStatus(string status)
        {
            using var conn = new SqliteConnection($"Data Source={Repo.GetDatabasePath()}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE import_actions SET action_status = @s WHERE id = @id";
            cmd.Parameters.AddWithValue("@s", status);
            cmd.Parameters.AddWithValue("@id", ActionId);
            cmd.ExecuteNonQuery();
        }

        public string GetJournalStatus()
        {
            using var conn = new SqliteConnection($"Data Source={Repo.GetDatabasePath()}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT status FROM import_journal WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", ImportId);
            return (string)cmd.ExecuteScalar()!;
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

    [Theory]
    [InlineData(CrashPoint.AfterJournal)]
    [InlineData(CrashPoint.AfterArchive)]
    [InlineData(CrashPoint.AfterTmpMove)]
    [InlineData(CrashPoint.AfterRename)]
    [InlineData(CrashPoint.AfterDbCommit)]
    public void ForwardMatrix_EveryCrashPoint_ReachesSameEndState(CrashPoint point)
    {
        using var env = new TestEnv();
        env.SeedJournaledUpdateAction();
        env.ArrangeCrashState(point);

        var result = env.Recovery().ExecuteForward(env.ImportId, env.Root);

        Assert.Equal(0, result.FailedCount);
        Assert.Equal("completed", env.GetJournalStatus());

        // Disk-Endzustand: neue Datei am Ziel, GENAU eine Archivkopie (alt), kein tmp
        var targetAbs = Path.Combine(env.Root, TargetRel);
        Assert.Equal(NewContent.Length, env.Fake.GetFileInfo(targetAbs).Length);
        var archived = Assert.Single(env.Fake.EnumerateFiles(
            Path.Combine(env.Root, TargetRelDir, "_Archiv"), "*", recursive: true));
        Assert.Equal(OldContent.Length, env.Fake.GetFileInfo(archived).Length);
        Assert.Empty(env.Fake.EnumerateFiles(env.Root, "*.bpm_tmp", recursive: true));

        // DB-Endzustand: A superseded, B current mit genau einer Datei
        var revisions = env.Repo.GetRevisionsForDocument(env.DocId);
        Assert.Equal(2, revisions.Count);
        Assert.Equal(PlanArchive.Status.Superseded,
            revisions.Single(r => r.Id == env.RevAId).RevisionStatus);
        var revB = revisions.Single(r => r.Id != env.RevAId);
        Assert.Equal(PlanArchive.Status.Current, revB.RevisionStatus);
        Assert.Equal("B", revB.PlanIndex);
        Assert.Single(env.Repo.GetFilesForRevision(revB.Id));
    }

    [Fact]
    public void RollbackMatrix_NothingMutated_TerminalFailedWithFilesUntouched()
    {
        // Rollback aus C0 (bewusster Abbruch vor jeder Mutation): Journal wird
        // terminal 'failed', Dateien bleiben exakt wie vorgefunden.
        using var env = new TestEnv();
        env.SeedJournaledUpdateAction();
        env.ArrangeCrashState(CrashPoint.AfterJournal);

        var result = env.Recovery().ExecuteRollback(env.ImportId, env.Root);

        Assert.Equal(0, result.FailedCount);
        Assert.Equal("failed", env.GetJournalStatus());
        Assert.Equal(NewContent.Length,
            env.Fake.GetFileInfo(Path.Combine(env.Root, SourceRel)).Length);
        Assert.Equal(OldContent.Length,
            env.Fake.GetFileInfo(Path.Combine(env.Root, TargetRel)).Length);
        var revA = Assert.Single(env.Repo.GetRevisionsForDocument(env.DocId));
        Assert.Equal(PlanArchive.Status.Current, revA.RevisionStatus);
    }

    [Fact]
    public void UndoMatrix_ArchiveRestoreFails_DbStaysOnImportEndState()
    {
        // Undo eines echten Update-Imports; der ZWEITE Reverse-Schritt
        // (Archiv → Ziel) scheitert. AK 14: die DB bleibt auf dem Import-
        // Endzustand (B current, nicht undone) — kein halber DB-Rollback.
        using var env = new TestEnv();
        env.SeedJournaledUpdateAction();
        env.ArrangeCrashState(CrashPoint.AfterJournal);
        // Import regulaer zu Ende fuehren (Forward aus C0 = kompletter Lauf)
        Assert.Equal(0, env.Recovery().ExecuteForward(env.ImportId, env.Root).FailedCount);

        env.Fake.FailNext(FakeFileStore.FileOp.Move, "_Archiv", times: 1);
        var undo = new ImportUndoService(env.Repo, env.Fake, env.Fake, env.Fake)
            .UndoLastImport(env.Root);

        Assert.False(undo.Success);
        var revisions = env.Repo.GetRevisionsForDocument(env.DocId);
        Assert.Equal(2, revisions.Count); // nichts soft-deleted
        Assert.Equal(PlanArchive.Status.Current,
            revisions.Single(r => r.Id != env.RevAId).RevisionStatus); // B weiter current
        Assert.Equal(env.ImportId, env.Repo.GetLastCompletedImportId()); // nicht undone
    }
}
