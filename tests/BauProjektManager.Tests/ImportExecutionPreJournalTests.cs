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
/// BPM-120 T2: Beweise der Vorab-Journalisierung (ADR-064 AK 4/5/6 + P.7).
/// FakeFileStore (virtuelle Dateien) + echte SQLite-Temp-DB. Der ProbeWriter
/// zaehlt IM MOMENT der ersten Dateimutation die bereits journalisierten
/// Actions — der harte AK-4-Beweis statt einer Endzustands-Naeherung.
/// </summary>
public class ImportExecutionPreJournalTests
{
    private sealed class TestEnv : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        /// <summary>Virtueller Projekt-Root — existiert NUR im FakeFileStore.</summary>
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "bpm-t2-virtual");

        public TestEnv()
        {
            IIdGenerator idGen = new UlidIdGenerator();
            var projectId = idGen.NewId();
            // BPM-123: Test-DB unter %TEMP% via dbPathOverride — nie in LocalAppData\Projects.
            Repo = new PlanManagerDatabase(projectId, idGen,
                dbPathOverride: TempDb.NewTempDbPath(projectId));
        }

        public int CountJournaledActions()
        {
            using var conn = new SqliteConnection($"Data Source={Repo.GetDatabasePath()}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM import_actions";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public (string Status, string? Archive, string? Md5, long? Size) GetAction(string actionType)
        {
            using var conn = new SqliteConnection($"Data Source={Repo.GetDatabasePath()}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT action_status, archive_path, md5, file_size
                FROM import_actions WHERE action_type = @at
                """;
            cmd.Parameters.AddWithValue("@at", actionType);
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read(), $"Keine Action vom Typ {actionType} journalisiert");
            return (reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetInt64(3));
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

    /// <summary>Writer-Dekorator: haelt fest, wie viele Actions bei der ERSTEN Mutation journalisiert waren.</summary>
    private sealed class ProbeWriter(FakeFileStore inner, Func<int> countActions) : IFileSystemWriter
    {
        public int? ActionsAtFirstMutation { get; private set; }

        private void Record() => ActionsAtFirstMutation ??= countActions();

        public void CreateDirectory(string path) { Record(); inner.CreateDirectory(path); }
        public void MoveFile(string s, string d, bool overwrite = false) { Record(); inner.MoveFile(s, d, overwrite); }
        public void CopyFile(string s, string d, bool overwrite = false) { Record(); inner.CopyFile(s, d, overwrite); }
        public void DeleteFile(string path) { Record(); inner.DeleteFile(path); }
        public void WriteAllText(string path, string content) { Record(); inner.WriteAllText(path, content); }
    }

    private static PendingAssignment NewPending(string fileName, string md5, string planNumber)
    {
        var scan = new ScannedFile(
            Path.Combine("_Eingang", fileName), fileName,
            Path.GetExtension(fileName), 12, DateTime.UtcNow);
        return new PendingAssignment(
            new FingerprintedFile(scan, md5), CaptureBucket.NewCapture,
            "polierplan", "Polierplan", "Haus 2", "OG2", planNumber, null,
            Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2"), Match: null);
    }

    private static CaptureItem DuplicateItem(string fileName, string md5)
    {
        var scan = new ScannedFile(
            Path.Combine("_Eingang", fileName), fileName,
            Path.GetExtension(fileName), 34, DateTime.UtcNow);
        var candidates = new PlanFileCandidates(
            fileName, PlanNumber: null, Index: null, RevisionKind.None,
            Level: null, BuildingPartHint: null, TypeKeywords: [],
            DateCandidate: null, HasCopyMarker: false, IsCombi: false);
        return new CaptureItem(
            new FingerprintedFile(scan, md5), candidates,
            CaptureBucket.Duplicate, Match: null, Reason: "MD5-Dublette");
    }

    [Fact]
    public void Execute_AllActionsJournaledBeforeFirstMutation()
    {
        // AK 4: Bei N geplanten Actions (inkl. skipDuplicate) stehen Header +
        // alle N import_actions im Journal, BEVOR die erste Datei angefasst wird.
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        fake.AddFile(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf"));
        fake.AddFile(Path.Combine(env.Root, "_Eingang", "5998-301_OG2.pdf"));
        fake.AddFile(Path.Combine(env.Root, "_Eingang", "5998-310_dup.pdf"));
        var probe = new ProbeWriter(fake, env.CountJournaledActions);

        var decisions = CaptureConfirmService.BuildDecisions(
            [NewPending("5998-300_OG2.pdf", "md5-a", "5998-300"),
             NewPending("5998-301_OG2.pdf", "md5-b", "5998-301")],
            new PlanValueNormalizer());
        decisions.AddRange(CaptureConfirmService.BuildSkipDecisions(
            [DuplicateItem("5998-310_dup.pdf", "md5-dup")]));

        var result = new ImportExecutionService(
            env.Repo, new UlidIdGenerator(), fake, probe, fake)
            .Execute(decisions, env.Root, "_Eingang");

        Assert.Equal(0, result.Failed);
        Assert.Equal(3, probe.ActionsAtFirstMutation); // ALLE 3 vorab journalisiert
    }

    [Fact]
    public void Execute_UpdateSameFileName_ArchivePathJournaledUpfrontAndUsed()
    {
        // AK 5: archive_path steht VOR der ersten Mutation im Journal, und die
        // Ausführung legt den Vorgänger EXAKT dort ab (kein ad-hoc-Name).
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        var targetRelDir = Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2");
        fake.AddFile(Path.Combine(env.Root, targetRelDir, "5998-300_OG2.pdf"),
            [1, 2, 3]); // alter Inhalt
        fake.AddFile(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf"),
            [9, 9, 9]); // neuer Inhalt

        const string key = "polierplan|5998_300|haus_2|og2";
        var docId = env.Repo.ResolveOrCreateDocument("proj1", key,
            "polierplan", "5998-300", "Polierplan", "", "Pläne", targetRelDir, null, null);
        var now = DateTime.UtcNow.ToString("o");
        var oldImportId = env.Repo.CreateImportJournal("_Eingang", 1, profileId: null);
        env.Repo.CompleteImportJournal(oldImportId, success: true);
        var revAId = env.Repo.InsertRevision(docId, "A", "FileName",
            PlanArchive.Status.Current, now, null, now, lastImportId: oldImportId);

        var match = new KnownPlanDocument(docId, key, "5998-300", "Polierplan",
            "Pläne", targetRelDir, "A", revAId);
        var scan = new ScannedFile(
            Path.Combine("_Eingang", "5998-300_OG2.pdf"), "5998-300_OG2.pdf",
            ".pdf", 3, DateTime.UtcNow);
        var pending = new PendingAssignment(
            new FingerprintedFile(scan, "md5-b"), CaptureBucket.UpdateProposal,
            docId, "Polierplan", null, null, "5998-300", "B", targetRelDir, match);

        var decisions = CaptureConfirmService.BuildDecisions(
            [pending], new PlanValueNormalizer());
        var result = new ImportExecutionService(
            env.Repo, new UlidIdGenerator(), fake, fake, fake)
            .Execute(decisions, env.Root, "_Eingang");

        Assert.Equal(0, result.Failed);

        var action = env.GetAction("indexUpdate");
        Assert.Equal("completed", action.Status);
        Assert.NotNull(action.Archive); // vorab journalisiert
        Assert.False(Path.IsPathRooted(action.Archive));
        // Vorgänger liegt EXAKT am journalisierten Pfad, Ziel hat den neuen Inhalt
        Assert.True(fake.FileExists(Path.Combine(env.Root, action.Archive!)));
        Assert.True(fake.FileExists(Path.Combine(env.Root, targetRelDir, "5998-300_OG2.pdf")));
    }

    [Fact]
    public void Execute_PureDuplicateImport_JournalsAndDeletes()
    {
        // AK 6: Reiner Dubletten-Import ist seit T2 eine echte journalisierte
        // Strecke (der alte Early-Return liess die Datei unjournalisiert liegen).
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        var dupAbs = Path.Combine(env.Root, "_Eingang", "5998-310_dup.pdf");
        fake.AddFile(dupAbs);

        var decisions = CaptureConfirmService.BuildSkipDecisions(
            [DuplicateItem("5998-310_dup.pdf", "md5-dup")]);
        var result = new ImportExecutionService(
            env.Repo, new UlidIdGenerator(), fake, fake, fake)
            .Execute(decisions, env.Root, "_Eingang");

        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Failed);
        Assert.False(fake.FileExists(dupAbs));

        var action = env.GetAction("skipDuplicate");
        Assert.Equal("completed", action.Status);
        Assert.Equal("md5-dup", action.Md5);
        Assert.Equal(34, action.Size);
    }

    [Theory]
    [InlineData(true, true, true)]    // Source da + MD5 im Bestand -> löschen + completed
    [InlineData(false, true, true)]   // Source weg + MD5 im Bestand -> idempotent completed
    [InlineData(false, false, false)] // kein Bestandsnachweis -> RecoveryConflict, nie completed
    public void RecoveryForward_SkipDuplicate_FollowsP7(
        bool sourceExists, bool md5Tracked, bool expectCompleted)
    {
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        var sourceRel = Path.Combine("_Eingang", "5998-310_dup.pdf");
        if (sourceExists)
            fake.AddFile(Path.Combine(env.Root, sourceRel));

        if (md5Tracked)
        {
            // Bestand: Dokument + Revision + Datei mit md5-dup (getrackte Teilmenge)
            var docId = env.Repo.ResolveOrCreateDocument("proj1", "polierplan|5998_310",
                "polierplan", "5998-310", "Polierplan", "", "Pläne", "Pläne/Polierplan", null, null);
            var now = DateTime.UtcNow.ToString("o");
            var revId = env.Repo.InsertRevision(docId, null, "None",
                PlanArchive.Status.Current, now, null, now, lastImportId: null);
            env.Repo.InsertFileForRevision(revId, "5998-310.pdf",
                "Pläne/Polierplan/5998-310.pdf", ".pdf", "md5-dup", 34, isPrimary: true);
        }

        var importId = env.Repo.CreateImportJournal("_Eingang", 1, profileId: null);
        env.Repo.InsertImportAction(importId, 0, "skipDuplicate", null, "5998-310",
            null, oldIndex: null, sourceRel, destinationPath: null, archivePath: null,
            md5: "md5-dup", fileSize: 34);

        var result = new RecoveryExecutorService(env.Repo, fake, fake, fake,
                new ImportExecutionService(env.Repo, new UlidIdGenerator(), fake, fake, fake))
            .ExecuteForward(importId, env.Root);

        var action = env.GetAction("skipDuplicate");
        if (expectCompleted)
        {
            Assert.Equal(0, result.FailedCount);
            Assert.Equal("completed", action.Status);
            Assert.False(fake.FileExists(Path.Combine(env.Root, sourceRel)));
        }
        else
        {
            Assert.Equal(1, result.FailedCount);
            Assert.NotEqual("completed", action.Status); // nie blind completed
            Assert.Contains(result.Errors, e => e.Contains("RecoveryConflict"));
        }
    }

    [Fact]
    public void Undo_PureSkipDuplicateImport_NotOfferedAsUndoable()
    {
        // AK 15: Ein Import, der nur aus skipDuplicate besteht, wird nicht als
        // undo-fähig angeboten (kein Papierkorb, nichts wiederherstellbar).
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        var importId = env.Repo.CreateImportJournal("_Eingang", 1, profileId: null);
        var actionId = env.Repo.InsertImportAction(importId, 0, "skipDuplicate", null,
            "5998-310", null, oldIndex: null, Path.Combine("_Eingang", "dup.pdf"),
            destinationPath: null, archivePath: null, md5: "md5-dup", fileSize: 34);
        env.Repo.CompleteImportAction(actionId, success: true);
        env.Repo.CompleteImportJournal(importId, success: true);

        var report = new ImportUndoService(env.Repo, fake, fake, fake).Preflight(env.Root);

        Assert.False(report.CanUndo);
        Assert.Equal(0, report.ActionCount);
    }
}
