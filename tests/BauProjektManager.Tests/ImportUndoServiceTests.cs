using System.IO;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.Tests.Fakes;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer <see cref="ImportUndoService"/> (BPM-111.04, Kap. 11):
/// Preflight-Trockenlauf + Undo des letzten Imports (Dateien zurueck,
/// Revision-Rollback, Supersede-Restore, Journal 'undone').
/// Echte SQLite-DB + echtes Temp-Projektverzeichnis pro Test.
/// </summary>
public class ImportUndoServiceTests
{
    private sealed class TestEnv : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        public string Root { get; }

        public TestEnv()
        {
            IIdGenerator idGen = new UlidIdGenerator();
            var projectId = idGen.NewId();
            // BPM-123: Test-DB unter %TEMP% via dbPathOverride — nie in LocalAppData\Projects.
            Repo = new PlanManagerDatabase(projectId, idGen,
                dbPathOverride: TempDb.NewTempDbPath(projectId));
            Root = Path.Combine(Path.GetTempPath(), "bpm-undo-test-" + projectId);
            Directory.CreateDirectory(Path.Combine(Root, "_Eingang"));
        }

        public void Dispose()
        {
            var dbPath = Repo.GetDatabasePath();
            Repo.Dispose();
            // BPM-120 T0: gezielter Pool-Clear statt ClearAllPools (Parallellast-Flaky)
            using (var pc = new SqliteConnection($"Data Source={dbPath}"))
                SqliteConnection.ClearPool(pc);
            TempDb.Delete(dbPath);
            try { if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true); } catch { }
        }
    }

    // BPM-120 T1: E2E-Tests laufen bewusst auf echter Disk — eine Port-Instanz.
    private static readonly LocalFileSystem Fs = new();

    /// <summary>Simuliert einen abgeschlossenen Import: Datei am Ziel + Journal + Revision.</summary>
    private static (string ImportId, string DocId, string RevId) SeedCompletedImport(
        TestEnv env, string fileName = "5998-200_EG.pdf")
    {
        var sourceRel = Path.Combine("_Eingang", fileName);
        var targetRel = Path.Combine("Pläne", "Polierplan", "Haus 1", fileName);
        var targetAbs = Path.Combine(env.Root, targetRel);
        Directory.CreateDirectory(Path.GetDirectoryName(targetAbs)!);
        File.WriteAllText(targetAbs, "plan-content");

        var importId = env.Repo.CreateImportJournal("_Eingang", 1, profileId: null);
        var actionId = env.Repo.InsertImportAction(importId, 0, "new",
            "polierplan|5998_200|haus_1", "5998-200", null, oldIndex: null,
            sourceRel, targetRel, archivePath: null);
        env.Repo.CompleteImportAction(actionId, true);

        var docId = env.Repo.ResolveOrCreateDocument("proj1", "polierplan|5998_200|haus_1",
            "polierplan", "5998-200", "Polierplan", "", "Polierplan",
            Path.GetDirectoryName(targetRel)!, null, null);
        var revId = env.Repo.InsertRevision(docId, planIndex: null, indexSource: "None",
            PlanArchive.Status.Current, "2026-06-11T00:00:00Z", null,
            "2026-06-11T00:00:00Z", lastImportId: importId);

        env.Repo.CompleteImportJournal(importId, success: true);
        return (importId, docId, revId);
    }

    [Fact]
    public void Preflight_AllFilesInPlace_CanUndo()
    {
        using var env = new TestEnv();
        SeedCompletedImport(env);

        var report = new ImportUndoService(env.Repo, Fs, Fs, Fs).Preflight(env.Root);

        Assert.True(report.CanUndo);
        Assert.Equal(1, report.ActionCount);
        Assert.Empty(report.Conflicts);
    }

    [Fact]
    public void Preflight_DestinationMissing_BlocksUndo()
    {
        using var env = new TestEnv();
        SeedCompletedImport(env);
        File.Delete(Path.Combine(env.Root, "Pläne", "Polierplan", "Haus 1", "5998-200_EG.pdf"));

        var report = new ImportUndoService(env.Repo, Fs, Fs, Fs).Preflight(env.Root);

        Assert.False(report.CanUndo);
        Assert.Contains(report.Conflicts, c => c.Issue.Contains("extern"));
    }

    [Fact]
    public void UndoLastImport_MovesFileBack_RollsBackDb_MarksUndone()
    {
        using var env = new TestEnv();
        var (_, docId, _) = SeedCompletedImport(env);

        var result = new ImportUndoService(env.Repo, Fs, Fs, Fs).UndoLastImport(env.Root);

        Assert.True(result.Success);
        Assert.Equal(1, result.RestoredFiles);
        // Datei zurueck im Eingang, Ziel leer
        Assert.True(File.Exists(Path.Combine(env.Root, "_Eingang", "5998-200_EG.pdf")));
        Assert.False(File.Exists(Path.Combine(env.Root, "Pläne", "Polierplan", "Haus 1", "5998-200_EG.pdf")));
        // DB: Revision weg, Dokument weg, kein completed-Import mehr
        Assert.Null(env.Repo.GetCurrentRevisionForDocument(docId));
        Assert.Empty(env.Repo.GetCurrentDocumentLookup());
        Assert.Null(env.Repo.GetLastCompletedImportId());
    }

    [Fact]
    public void UndoLastImport_RestoresSupersededRevision()
    {
        using var env = new TestEnv();

        // Bestand: Dokument mit Revision A (aus frueherem Import)
        var oldImport = env.Repo.CreateImportJournal("_Eingang", 1, null);
        env.Repo.CompleteImportJournal(oldImport, true);
        var docId = env.Repo.ResolveOrCreateDocument("proj1", "polierplan|5998_100|haus_1",
            "polierplan", "5998-100", "Polierplan", "", "Polierplan", "Pläne/Polierplan/Haus 1", null, null);
        var revA = env.Repo.InsertRevision(docId, "A", "FileName", PlanArchive.Status.Current,
            "2026-06-01T00:00:00Z", null, "2026-06-01T00:00:00Z", lastImportId: oldImport);

        // Neuer Import bringt Index B: A superseded, B current + Datei am Ziel
        var fileName = "5998-100-B_KG.pdf";
        var targetRel = Path.Combine("Pläne", "Polierplan", "Haus 1", fileName);
        Directory.CreateDirectory(Path.Combine(env.Root, "Pläne", "Polierplan", "Haus 1"));
        File.WriteAllText(Path.Combine(env.Root, targetRel), "B-content");

        var importId = env.Repo.CreateImportJournal("_Eingang", 1, null);
        var actionId = env.Repo.InsertImportAction(importId, 0, "indexUpdate",
            "polierplan|5998_100|haus_1", "5998-100", "B", oldIndex: "A",
            Path.Combine("_Eingang", fileName), targetRel, archivePath: null);
        env.Repo.CompleteImportAction(actionId, true);
        env.Repo.SupersedeCurrentRevision(docId, "2026-06-11T00:00:00Z");
        env.Repo.InsertRevisionEvent(revA, importId, PlanArchive.EventType.Superseded, "Test");
        env.Repo.InsertRevision(docId, "B", "FileName", PlanArchive.Status.Current,
            "2026-06-11T00:00:00Z", null, "2026-06-11T00:00:00Z", lastImportId: importId);
        env.Repo.CompleteImportJournal(importId, true);

        var result = new ImportUndoService(env.Repo, Fs, Fs, Fs).UndoLastImport(env.Root);

        Assert.True(result.Success);
        // Revision A ist wieder current, B ist weg
        var current = env.Repo.GetCurrentRevisionForDocument(docId);
        Assert.NotNull(current);
        Assert.Equal(revA, current!.Id);
        Assert.Equal("A", current.PlanIndex);
    }
}
