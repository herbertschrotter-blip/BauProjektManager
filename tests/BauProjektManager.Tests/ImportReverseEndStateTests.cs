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
/// R01-006 (Audit Review 01, ADR-064 P.1/P.5 + Addendum): Undo, Recovery-Rollback und
/// der normale Import erreichen einen konsistenten Endzustand — Dateisystem UND
/// Plan-Cache. E2E ueber echten Import (Execute) + FakeFileStore + Temp-SQLite.
/// </summary>
public class ImportReverseEndStateTests
{
    private const string FileName = "5998-300_OG2.pdf";
    private const string Key = "polierplan|5998_300|haus_2|og2";
    private static readonly string TargetRelDir = Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2");

    private sealed class TestEnv : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        public FakeFileStore Fake { get; } = new();
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "bpm-r01006-virtual");

        public TestEnv()
        {
            IIdGenerator idGen = new UlidIdGenerator();
            var projectId = idGen.NewId();
            Repo = new PlanManagerDatabase(projectId, idGen,
                dbPathOverride: TempDb.NewTempDbPath(projectId));
        }

        public ImportExecutionService Executor() =>
            new(Repo, new UlidIdGenerator(), Fake, Fake, Fake);

        public ImportUndoService Undo() => new(Repo, Fake, Fake, Fake);

        public RecoveryExecutorService Recovery() =>
            new(Repo, Fake, Fake, Fake, Executor());

        public string InboxAbs(string name) => Path.Combine(Root, "_Eingang", name);
        public string TargetAbs(string name) => Path.Combine(Root, TargetRelDir, name);

        public ImportExecutionResult Import(string fileName, string md5,
            string? buildingPartId = null, string? buildingLevelId = null, bool addFile = true)
        {
            if (addFile)
                Fake.AddFile(InboxAbs(fileName));
            var decisions = CaptureConfirmService.BuildDecisions(
                [NewPending(fileName, md5, buildingPartId, buildingLevelId)], new PlanValueNormalizer());
            return Executor().Execute(decisions, Root, "_Eingang");
        }

        public string? ScalarString(string sql, string id)
        {
            using var conn = new SqliteConnection($"Data Source={Repo.GetDatabasePath()}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteScalar() as string;
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

    private static PendingAssignment NewPending(string fileName, string md5,
        string? buildingPartId, string? buildingLevelId)
    {
        var scan = new ScannedFile(
            Path.Combine("_Eingang", fileName), fileName,
            Path.GetExtension(fileName), 12, DateTime.UtcNow);
        return new PendingAssignment(
            new FingerprintedFile(scan, md5), CaptureBucket.NewCapture,
            "polierplan", "Polierplan", "Haus 2", "OG2", "5998-300", null,
            TargetRelDir, Match: null,
            BuildingPartId: buildingPartId, BuildingLevelId: buildingLevelId);
    }

    // ── 006.01 / DB-01: Re-Import nach Undo ──

    [Fact]
    public void ReimportAfterUndo_ReactivatesSameDocument()
    {
        using var env = new TestEnv();
        Assert.Equal(0, env.Import(FileName, "md5-a").Failed);
        var docId = env.Repo.GetDocumentByKey(Key)!.Id;

        Assert.True(env.Undo().UndoLastImport(env.Root).Success);
        Assert.Null(env.Repo.GetDocumentByKey(Key)); // soft-deleted

        // Datei liegt nach dem Undo wieder im Eingang — derselbe Import erneut
        var again = env.Import(FileName, "md5-a", addFile: false);

        Assert.Equal(0, again.Failed);
        var doc = env.Repo.GetDocumentByKey(Key);
        Assert.NotNull(doc);
        Assert.Equal(docId, doc!.Id); // Dokument-ID stabil (reaktiviert, nicht neu)
        Assert.NotNull(env.Repo.GetCurrentRevisionForDocument(docId));
        Assert.True(env.Fake.FileExists(env.TargetAbs(FileName)));
        Assert.False(env.Repo.HasPendingImports());
    }

    // ── 006.02 / K22-07: fehlende Quelldatei ──

    [Fact]
    public void Import_SourceVanished_ActionFailsWithoutDbEntry()
    {
        using var env = new TestEnv();
        // Datei zwischen Analyse und Bestaetigen verschwunden: nicht im Eingang anlegen
        var result = env.Import(FileName, "md5-a", addFile: false);

        Assert.Equal(1, result.Failed);
        Assert.Contains("RecoveryConflict", result.Errors.Single());
        Assert.Null(env.Repo.GetDocumentByKey(Key));          // kein DB-Eintrag ohne Datei
        Assert.True(env.Repo.HasPendingImports());             // Journal bleibt pending
        Assert.False(env.Fake.FileExists(env.TargetAbs(FileName)));
    }

    [Fact]
    public void IndexUpdate_SourceVanished_PredecessorStaysAndRecoveryReportsConflict()
    {
        using var env = new TestEnv();
        Assert.Equal(0, env.Import(FileName, "md5-a").Failed);
        var docId = env.Repo.GetDocumentByKey(Key)!.Id;
        var revA = env.Repo.GetCurrentRevisionForDocument(docId)!;

        // Index-Update planen, Quelle verschwindet vor der Ausfuehrung
        var update = new ImportDecision(
            new ClassifiedImportFile(
                new ParsedImportFile(Path.Combine("_Eingang", FileName), FileName, ".pdf", 12, "md5-b",
                    null, new Dictionary<string, string>(), ParseConfidence.High, []),
                "polierplan", "Polierplan", Key, "5998-300", "B", RevisionKind.Alphabetic,
                IndexSourceType.FileName, ImportStage.Unknown, new Dictionary<string, string>(), []),
            ImportStatus.UpdateNewerIndex, Key, revA.Id, Path.Combine(TargetRelDir, FileName), []);
        var result = env.Executor().Execute([update], env.Root, "_Eingang");

        Assert.Equal(1, result.Failed);
        Assert.True(env.Fake.FileExists(env.TargetAbs(FileName)));     // Vorgaenger nicht archiviert
        Assert.DoesNotContain(env.Fake.EnumerateFiles(env.Root, "*", recursive: true), f => f.Contains("_Archiv"));
        Assert.Equal(revA.Id, env.Repo.GetCurrentRevisionForDocument(docId)!.Id);

        // Recovery Forward darf den Vorgaenger nicht als neue Revision uebernehmen
        var importId = env.Repo.GetPendingImports().Single().Id;
        var forward = env.Recovery().ExecuteForward(importId, env.Root);
        Assert.Equal(1, forward.FailedCount);
        Assert.Equal(revA.Id, env.Repo.GetCurrentRevisionForDocument(docId)!.Id);
    }

    // ── 006.03 / PM-02: Rollback setzt den Plan-Cache zurueck ──

    private static ImportDecision LinkDecision(string fileName, string md5) => new(
        new ClassifiedImportFile(
            new ParsedImportFile(Path.Combine("_Eingang", fileName), fileName,
                Path.GetExtension(fileName), 12, md5,
                null, new Dictionary<string, string>(), ParseConfidence.High, []),
            "polierplan", "Polierplan", Key, "5998-300", null, RevisionKind.None,
            IndexSourceType.None, ImportStage.Unknown, new Dictionary<string, string>(), []),
        ImportStatus.ChangedNoIndex, Key, null, Path.Combine(TargetRelDir, fileName), []);

    [Fact]
    public void Rollback_AfterCompletedNewAction_RevertsPlanCacheAndAllowsReimport()
    {
        using var env = new TestEnv();
        Assert.Equal(0, env.Import(FileName, "md5-a").Failed);
        var importId = env.Repo.GetLastCompletedImportId()!;

        var rollback = env.Recovery().ExecuteRollback(importId, env.Root);

        Assert.Equal(0, rollback.FailedCount);
        Assert.True(env.Fake.FileExists(env.InboxAbs(FileName)));
        Assert.Null(env.Repo.GetDocumentByKey(Key));                       // Dokument zurueckgenommen
        Assert.False(env.Repo.GetKnownMd5Lookup().ContainsKey("md5-a"));   // keine Dublette mehr

        // Folge-Import derselben Datei laeuft sauber (kein Dublettenloeschen, kein UNIQUE-Fehler)
        Assert.Equal(0, env.Import(FileName, "md5-a", addFile: false).Failed);
        Assert.NotNull(env.Repo.GetDocumentByKey(Key));
    }

    [Fact]
    public void Undo_OfLinkedAdditionalFile_UnlinksItFromExistingRevision()
    {
        using var env = new TestEnv();
        Assert.Equal(0, env.Import(FileName, "md5-pdf").Failed);
        var docId = env.Repo.GetDocumentByKey(Key)!.Id;
        var rev = env.Repo.GetCurrentRevisionForDocument(docId)!;

        // DWG wird in einem zweiten Import an die bestehende Revision angehaengt
        const string dwg = "5998-300_OG2.dwg";
        env.Fake.AddFile(env.InboxAbs(dwg));
        Assert.Equal(0, env.Executor().Execute([LinkDecision(dwg, "md5-dwg")], env.Root, "_Eingang").Failed);
        Assert.Equal(2, env.Repo.GetFilesForRevision(rev.Id).Count);

        Assert.True(env.Undo().UndoLastImport(env.Root).Success);

        Assert.True(env.Fake.FileExists(env.InboxAbs(dwg)));
        Assert.Equal(rev.Id, env.Repo.GetCurrentRevisionForDocument(docId)!.Id); // PDF-Revision bleibt
        Assert.Single(env.Repo.GetFilesForRevision(rev.Id));                      // DWG entknuepft
        var known = env.Repo.GetKnownMd5Lookup();
        Assert.False(known.ContainsKey("md5-dwg"));
        Assert.True(known.ContainsKey("md5-pdf"));
    }

    // ── DB-34: Recovery Forward stellt die volle Klassifikation her (ADR-064-Addendum P.2) ──

    [Fact]
    public void RecoveryForward_RestoresTypeAndBuildingIdsFromJournal()
    {
        using var env = new TestEnv();
        // Datei verschwindet waehrend des Imports nicht — aber der Prozess stirbt nach der
        // Journalisierung: Action von Hand auf pending zuruecksetzen und DB-Writes fehlen lassen.
        env.Fake.AddFile(env.InboxAbs(FileName));
        var pending = NewPending(FileName, "md5-a", "bp-ulid-1", "bl-ulid-1");
        var decision = CaptureConfirmService.BuildDecisions([pending], new PlanValueNormalizer()).Single();
        var importId = env.Repo.CreateImportJournal("_Eingang", 1, profileId: null);
        env.Repo.InsertImportAction(importId, 0, "new", decision.DocumentKey, "5998-300", null, null,
            decision.File.Parsed.RelativePath, decision.TargetRelativePath, null, "md5-a", 12,
            decision.File.DocumentTypeId, decision.File.DocumentTypeDisplayName,
            decision.File.BuildingPartId, decision.File.BuildingLevelId);

        var forward = env.Recovery().ExecuteForward(importId, env.Root);

        Assert.Equal(0, forward.FailedCount);
        var doc = env.Repo.GetDocumentByKey(decision.DocumentKey!);
        Assert.NotNull(doc);
        Assert.Equal("Polierplan", doc!.DocumentType);        // nicht mehr 'unknown'
        Assert.Equal("bp-ulid-1", doc.BuildingPartId);
        Assert.Equal("bl-ulid-1", doc.BuildingLevelId);
    }

    // ── PM-39: Undo-Kette mit Preflight (ADR-064-Addendum P.3) ──

    [Fact]
    public void UndoChain_EachStepSeparatelyWithPreflight()
    {
        using var env = new TestEnv();
        const string second = "5998-301_OG2.pdf";
        Assert.Equal(0, env.Import(FileName, "md5-a").Failed);
        var first = env.Repo.GetLastCompletedImportId();
        env.Fake.AddFile(env.InboxAbs(second));
        var pending2 = NewPending(second, "md5-b", null, null) with { PlanNumber = "5998-301" };
        Assert.Equal(0, env.Executor().Execute(
            CaptureConfirmService.BuildDecisions([pending2], new PlanValueNormalizer()), env.Root, "_Eingang").Failed);

        // Schritt 1: nur der letzte Import
        Assert.True(env.Undo().UndoLastImport(env.Root).Success);
        Assert.True(env.Fake.FileExists(env.InboxAbs(second)));
        Assert.True(env.Fake.FileExists(env.TargetAbs(FileName)));     // erster Import unberuehrt
        Assert.Equal(first, env.Repo.GetLastCompletedImportId());       // jetzt der naechste Kandidat

        // Schritt 2: eigener Preflight, dann der davorliegende Import
        Assert.True(env.Undo().Preflight(env.Root).CanUndo);
        Assert.True(env.Undo().UndoLastImport(env.Root).Success);
        Assert.True(env.Fake.FileExists(env.InboxAbs(FileName)));
        Assert.Null(env.Repo.GetLastCompletedImportId());
    }

    // ── R01-008 / K28-02: Undo nach Archiv-Move gesperrt (ADR-064-Addendum P.4) ──

    private static PlanArchiveEntry EntryFor(TestEnv env)
    {
        var doc = env.Repo.GetDocumentByKey(Key)!;
        var rev = env.Repo.GetCurrentRevisionForDocument(doc.Id)!;
        return new PlanArchiveEntry(doc.Id, doc.PlanNumber, "", doc.DocumentType, rev.Id, null,
            "", rev.LastImportId, FileName, Path.Combine(TargetRelDir, FileName));
    }

    [Fact]
    public void Undo_AfterArchiveMove_BlockedWithHint_NothingTouched()
    {
        using var env = new TestEnv();
        Assert.Equal(0, env.Import(FileName, "md5-a").Failed);
        var moved = new ArchiveMoveService(env.Repo, env.Fake, env.Fake, env.Fake)
            .MoveDocument(EntryFor(env), Path.Combine(TargetRelDir, "Vorabzug"), env.Root);
        Assert.True(moved.Success, moved.Error);

        var preflight = env.Undo().Preflight(env.Root);
        Assert.False(preflight.CanUndo);
        Assert.Equal(ImportUndoService.ArchiveMoveBlocksUndo, Assert.Single(preflight.Conflicts).Issue);

        var result = env.Undo().UndoLastImport(env.Root);
        Assert.False(result.Success);
        Assert.True(env.Fake.FileExists(Path.Combine(env.Root, TargetRelDir, "Vorabzug", FileName)));
        Assert.NotNull(env.Repo.GetDocumentByKey(Key));
    }

    [Fact]
    public void Undo_AfterRolledBackArchiveMove_StillAllowed()
    {
        using var env = new TestEnv();
        Assert.Equal(0, env.Import(FileName, "md5-a").Failed);
        env.Fake.FailNext(FakeFileStore.FileOp.Move, "Vorabzug", new UnauthorizedAccessException("gesperrt"));
        var moved = new ArchiveMoveService(env.Repo, env.Fake, env.Fake, env.Fake)
            .MoveDocument(EntryFor(env), Path.Combine(TargetRelDir, "Vorabzug"), env.Root);
        Assert.False(moved.Success);

        Assert.True(env.Undo().Preflight(env.Root).CanUndo);
    }
}
