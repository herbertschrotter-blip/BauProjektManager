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
/// Archiv-Move (BPM-111.07 Slice D, R01-008): alle Revisions-Dateien ziehen gemeinsam
/// um, Journal VOR der ersten Mutation, .bpm_tmp, DB in einer Transaction, eigene
/// Recovery, Zielordner-Schutz, Umklassifizierung per Radial (ADR-064 + Addendum P.4/P.5).
/// </summary>
public class ArchiveMoveServiceTests
{
    private const string Key = "polierplan|5998_100|haus_1";
    private static readonly string SourceDir = Path.Combine("Pläne", "Polierplan", "Haus 1", "KG");

    private sealed class TestEnv : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        public FakeFileStore Fake { get; } = new();
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "bpm-move-virtual");
        public string ImportId { get; private set; } = "";
        public string DocId { get; private set; } = "";
        public string RevId { get; private set; } = "";

        public TestEnv()
        {
            IIdGenerator idGen = new UlidIdGenerator();
            var projectId = idGen.NewId();
            // BPM-123: Test-DB unter %TEMP% via dbPathOverride — nie in LocalAppData\Projects.
            Repo = new PlanManagerDatabase(projectId, idGen,
                dbPathOverride: TempDb.NewTempDbPath(projectId));
        }

        public ArchiveMoveService Service() => new(Repo, Fake, Fake, Fake);

        public RecoveryExecutorService Recovery() =>
            new(Repo, Fake, Fake, Fake, new ImportExecutionService(Repo, new UlidIdGenerator(), Fake, Fake, Fake));

        public string Abs(params string[] parts) => Path.Combine([Root, .. parts]);

        /// <summary>Bestand: completed Import + Dokument + Revision mit PDF (primaer) und DWG.</summary>
        public PlanArchiveEntry Seed()
        {
            Fake.AddFile(Abs(SourceDir, "5998-100.pdf"));
            Fake.AddFile(Abs(SourceDir, "5998-100.dwg"));
            ImportId = Repo.CreateImportJournal("_Eingang", 2, profileId: null);
            Repo.CompleteImportJournal(ImportId, success: true);
            DocId = Repo.ResolveOrCreateDocument("proj1", Key, "polierplan", "5998-100",
                "Polierplan", "", "Pläne", SourceDir, "bt-haus1", "gs-kg");
            var now = DateTime.UtcNow.ToString("o");
            RevId = Repo.InsertRevision(DocId, "A", "FileName",
                PlanArchive.Status.Current, now, null, now, lastImportId: ImportId);
            Repo.InsertFileForRevision(RevId, "5998-100.pdf",
                Path.Combine(SourceDir, "5998-100.pdf"), ".pdf", "md5-p", 3, isPrimary: true);
            Repo.InsertFileForRevision(RevId, "5998-100.dwg",
                Path.Combine(SourceDir, "5998-100.dwg"), ".dwg", "md5-d", 3, isPrimary: false);
            return new PlanArchiveEntry(DocId, "5998-100", "", "Polierplan", RevId, "A", now,
                ImportId, "5998-100.pdf", Path.Combine(SourceDir, "5998-100.pdf"));
        }

        public string? JournalStatus(string importId)
        {
            using var conn = new SqliteConnection($"Data Source={Repo.GetDatabasePath()}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT status FROM import_journal WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", importId);
            return cmd.ExecuteScalar() as string;
        }

        public string? LatestJournalId()
        {
            using var conn = new SqliteConnection($"Data Source={Repo.GetDatabasePath()}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id FROM import_journal WHERE id <> @id ORDER BY timestamp DESC LIMIT 1";
            cmd.Parameters.AddWithValue("@id", ImportId);
            return cmd.ExecuteScalar() as string;
        }

        public void Dispose()
        {
            var dbPath = Repo.GetDatabasePath();
            Repo.Dispose();
            // BPM-120 T0: gezielter Pool-Clear statt ClearAllPools (Parallellast-Flaky)
            using (var pc = new SqliteConnection($"Data Source={dbPath}"))
                SqliteConnection.ClearPool(pc);
            TempDb.Delete(dbPath);
        }
    }

    // ── R01-008.02 / PM-03: Ports, Journal vorab, .bpm_tmp, Transaction ──

    [Fact]
    public void ExplorerMove_IntoSubfolder_MovesAllFiles_KeepsClassificationAndUndo()
    {
        using var env = new TestEnv();
        var entry = env.Seed();
        var targetDir = Path.Combine(SourceDir, "Vorabzug");

        var result = env.Service().MoveDocument(entry, targetDir, env.Root);

        Assert.True(result.Success, result.Error);
        Assert.Equal(2, result.MovedFiles);
        Assert.True(env.Fake.FileExists(env.Abs(targetDir, "5998-100.pdf")));
        Assert.True(env.Fake.FileExists(env.Abs(targetDir, "5998-100.dwg")));
        Assert.False(env.Fake.FileExists(env.Abs(SourceDir, "5998-100.pdf")));
        Assert.False(env.Fake.FileExists(env.Abs(targetDir, "5998-100.pdf" + ImportExecutionService.TmpSuffix)));

        // DB = Ordner-Wahrheit fuer die Dateien; Ablageordner + Klassifikation bleiben
        Assert.All(env.Repo.GetFilesForRevision(env.RevId), f => Assert.StartsWith(targetDir, f.RelativePath));
        var doc = env.Repo.GetDocumentById(env.DocId)!;
        Assert.Equal(Key, doc.DocumentKey);
        Assert.Equal(SourceDir, doc.RelativeDirectory);

        // Journal 'moved', alle Actions completed — nie „letzter Import"
        var moveId = env.LatestJournalId()!;
        Assert.Equal("moved", env.JournalStatus(moveId));
        Assert.All(env.Repo.GetImportActions(moveId), a => Assert.Equal("completed", a.ActionStatus));
        Assert.Equal(env.ImportId, env.Repo.GetLastCompletedImportId());
        Assert.Contains(env.Repo.GetRevisionEvents(env.RevId),
            ev => ev.EventType == PlanArchive.EventType.ManualOverride);
    }

    [Fact]
    public void Move_SecondFileFails_FirstFileRolledBack_JournalFailed_DbUntouched()
    {
        using var env = new TestEnv();
        var entry = env.Seed();
        var targetDir = Path.Combine(SourceDir, "Vorabzug");
        env.Fake.FailNext(FakeFileStore.FileOp.Move, "5998-100.dwg", new UnauthorizedAccessException("gesperrt"));

        var result = env.Service().MoveDocument(entry, targetDir, env.Root);

        Assert.False(result.Success);
        // Journal stand vollstaendig VOR der ersten Mutation (beide Actions da)
        var moveId = env.LatestJournalId()!;
        Assert.Equal(2, env.Repo.GetImportActions(moveId).Count);
        Assert.Equal("failed", env.JournalStatus(moveId));
        // Sofort-Rollback: PDF wieder an der Quelle, DB unveraendert
        Assert.True(env.Fake.FileExists(env.Abs(SourceDir, "5998-100.pdf")));
        Assert.True(env.Fake.FileExists(env.Abs(SourceDir, "5998-100.dwg")));
        Assert.All(env.Repo.GetFilesForRevision(env.RevId), f => Assert.StartsWith(SourceDir, f.RelativePath));
        Assert.False(env.Repo.HasPendingImports());
    }

    /// <summary>Crash nach PDF-Move, DWG haengt als .bpm_tmp, DB nie geschrieben.</summary>
    private static string SimulateCrashedMove(TestEnv env, string targetDir)
    {
        var moveId = env.Repo.CreateImportJournal(SourceDir, 2, profileId: null);
        foreach (var (name, order) in new[] { ("5998-100.pdf", 0), ("5998-100.dwg", 1) })
            env.Repo.InsertImportAction(moveId, order, PlanManagerDatabase.MovedActionType, Key,
                "5998-100", "A", null, Path.Combine(SourceDir, name), Path.Combine(targetDir, name),
                null, documentTypeId: "polierplan", documentType: "Polierplan",
                buildingPartId: "bt-haus1", buildingLevelId: "gs-kg");
        env.Fake.CreateDirectory(env.Abs(targetDir));
        env.Fake.MoveFile(env.Abs(SourceDir, "5998-100.pdf"), env.Abs(targetDir, "5998-100.pdf"));
        env.Fake.MoveFile(env.Abs(SourceDir, "5998-100.dwg"),
            env.Abs(targetDir, "5998-100.dwg" + ImportExecutionService.TmpSuffix));
        return moveId;
    }

    [Fact]
    public void Recovery_Forward_CompletesCrashedMove_AsMovedNotCompleted()
    {
        using var env = new TestEnv();
        env.Seed();
        var targetDir = Path.Combine(SourceDir, "Vorabzug");
        var moveId = SimulateCrashedMove(env, targetDir);
        Assert.True(env.Repo.HasPendingImports());

        var result = env.Recovery().ExecuteForward(moveId, env.Root);

        Assert.Equal(0, result.FailedCount);
        Assert.True(env.Fake.FileExists(env.Abs(targetDir, "5998-100.dwg")));
        Assert.All(env.Repo.GetFilesForRevision(env.RevId), f => Assert.StartsWith(targetDir, f.RelativePath));
        Assert.Equal("moved", env.JournalStatus(moveId));
        Assert.Equal(env.ImportId, env.Repo.GetLastCompletedImportId()); // Move ist nie „letzter Import"
        Assert.False(env.Repo.HasPendingImports());
    }

    [Fact]
    public void Recovery_Rollback_RestoresFilesAtSource_DbUntouched()
    {
        using var env = new TestEnv();
        env.Seed();
        var moveId = SimulateCrashedMove(env, Path.Combine(SourceDir, "Vorabzug"));

        var result = env.Recovery().ExecuteRollback(moveId, env.Root);

        Assert.Equal(0, result.FailedCount);
        Assert.True(env.Fake.FileExists(env.Abs(SourceDir, "5998-100.pdf")));
        Assert.True(env.Fake.FileExists(env.Abs(SourceDir, "5998-100.dwg")));
        Assert.All(env.Repo.GetFilesForRevision(env.RevId), f => Assert.StartsWith(SourceDir, f.RelativePath));
        Assert.Equal("failed", env.JournalStatus(moveId));
    }

    // ── R01-008.01 / K25-01: Zielordner-Schutz ──

    [Theory]
    [InlineData("_Eingang")]
    [InlineData("Pläne/Polierplan/Haus 1/KG/_Archiv")]
    [InlineData("Pläne/Polierplan/Haus 2/EG")]
    [InlineData("../Fremd")]
    public void ExplorerMove_ForbiddenTarget_RejectedWithoutJournal(string target)
    {
        using var env = new TestEnv();
        var entry = env.Seed();

        var result = env.Service().MoveDocument(entry, target, env.Root);

        Assert.False(result.Success);
        Assert.Null(env.LatestJournalId());
        Assert.True(env.Fake.FileExists(env.Abs(SourceDir, "5998-100.pdf")));
    }

    [Fact]
    public void Move_SameDirectory_FailsWithoutJournal()
    {
        using var env = new TestEnv();
        var entry = env.Seed();

        var result = env.Service().MoveDocument(entry, SourceDir, env.Root);

        Assert.False(result.Success);
        Assert.Contains("bereits", result.Error);
        Assert.Null(env.LatestJournalId());
    }

    [Fact]
    public void Guard_AllowsOwnFolderAndSubfolders_BlocksUnderscoreAndOutside()
    {
        Assert.Null(ArchiveMoveTargetGuard.Check("Pläne/A/KG", "Pläne\\A\\KG"));
        Assert.Null(ArchiveMoveTargetGuard.Check("Pläne/A/KG/Alt/", "Pläne/A/KG"));
        Assert.NotNull(ArchiveMoveTargetGuard.Check("Pläne/A", "Pläne/A/KG"));
        Assert.NotNull(ArchiveMoveTargetGuard.Check("Pläne/A/KG/_Archiv", null));
        Assert.NotNull(ArchiveMoveTargetGuard.Check("C:/Temp", null));
        Assert.NotNull(ArchiveMoveTargetGuard.Check("  ", null));
    }

    [Fact]
    public void SkipDuplicate_NeverDeletesTrackedFile()
    {
        using var env = new TestEnv();
        env.Seed();
        // Altbestand: getrackter Plan liegt (z.B. nach altem Explorer-Move) im Eingang
        var trackedInInbox = Path.Combine("_Eingang", "5998-200.pdf");
        env.Fake.AddFile(env.Abs(trackedInInbox));
        env.Repo.InsertFileForRevision(env.RevId, "5998-200.pdf", trackedInInbox, ".pdf", "md5-p", 3, isPrimary: false);

        var scan = new ScannedFile(trackedInInbox, "5998-200.pdf", ".pdf", 3, DateTime.UtcNow);
        var dup = new PendingAssignment(new FingerprintedFile(scan, "md5-p"), CaptureBucket.Duplicate,
            "polierplan", "Polierplan", "Haus 1", "KG", "5998-200", null, SourceDir, Match: null);
        var decisions = CaptureConfirmService.BuildDecisions([dup], new PlanValueNormalizer());
        var result = new ImportExecutionService(env.Repo, new UlidIdGenerator(), env.Fake, env.Fake, env.Fake)
            .Execute([.. decisions.Select(d => d with { Status = ImportStatus.SkipIdentical })],
                env.Root, "_Eingang");

        Assert.Equal(0, result.Failed); // Action erledigt, Datei bleibt (kein haengendes Journal)
        Assert.True(env.Fake.FileExists(env.Abs(trackedInInbox)));
    }

    // ── R01-008 / LU5-02: Radial-Move klassifiziert mit (ADR-064-Addendum P.5) ──

    [Fact]
    public void RadialMove_Reclassifies_TypePartLevelKeyAndDirectory()
    {
        using var env = new TestEnv();
        var entry = env.Seed();
        var targetDir = Path.Combine("Pläne", "Schalplan", "Haus 2", "EG");
        var reclass = new ArchiveReclassification("schalplan", "Schalplan", "bt-haus2", null, "gs-eg");

        var result = env.Service().MoveDocument(entry, targetDir, env.Root, reclass);

        Assert.True(result.Success, result.Error);
        var doc = env.Repo.GetDocumentById(env.DocId)!;
        Assert.Equal("schalplan|5998_100|bt-haus2|gs-eg", doc.DocumentKey);
        Assert.Equal("schalplan", doc.DocumentTypeId);
        Assert.Equal("Schalplan", doc.DocumentType);
        Assert.Equal("bt-haus2", doc.BuildingPartId);
        Assert.Equal("gs-eg", doc.BuildingLevelId);
        Assert.Equal(targetDir, doc.RelativeDirectory);
        Assert.Equal("Pläne", doc.TargetFolder);
        Assert.True(env.Fake.FileExists(env.Abs(targetDir, "5998-100.dwg")));
        // Journal fuehrt die Ziel-Klassifikation (Recovery stellt denselben Endzustand her)
        var action = env.Repo.GetImportActions(env.LatestJournalId()!)[0];
        Assert.Equal(doc.DocumentKey, action.DocumentKey);
        Assert.Equal("bt-haus2", action.BuildingPartId);
    }

    [Fact]
    public void RadialMove_TargetKeyTakenByOtherDocument_RejectedWithoutJournal()
    {
        using var env = new TestEnv();
        var entry = env.Seed();
        env.Repo.ResolveOrCreateDocument("proj1", "schalplan|5998_100|bt-haus2|gs-eg", "schalplan",
            "5998-100", "Schalplan", "", "Pläne", "x", "bt-haus2", "gs-eg");
        var reclass = new ArchiveReclassification("schalplan", "Schalplan", "bt-haus2", null, "gs-eg");

        var result = env.Service().MoveDocument(entry,
            Path.Combine("Pläne", "Schalplan", "Haus 2", "EG"), env.Root, reclass);

        Assert.False(result.Success);
        Assert.Contains("belegt", result.Error);
        Assert.Null(env.LatestJournalId());
    }
}
