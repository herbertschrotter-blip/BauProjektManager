using System.IO;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests des Archiv-Moves (BPM-111.07 Slice D): alle Revisions-Dateien ziehen
/// gemeinsam um, DB bleibt Ordner-Wahrheit (ADR-061), Journal-Status 'moved'
/// hält Import-Undo und „letzter Import"-Kennzeichnung unberührt.
/// </summary>
public class ArchiveMoveServiceTests
{
    private sealed class TestEnv : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        public string Root { get; }
        private readonly string _dbFolder;

        public TestEnv()
        {
            IIdGenerator idGen = new UlidIdGenerator();
            var projectId = idGen.NewId();
            _dbFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BauProjektManager", "Projects", projectId);
            Repo = new PlanManagerDatabase(projectId, idGen);
            Root = Path.Combine(Path.GetTempPath(), "bpm-move-test-" + projectId);
            Directory.CreateDirectory(Root);
        }

        public void Dispose()
        {
            Repo.Dispose();
            SqliteConnection.ClearAllPools();
            try { if (Directory.Exists(_dbFolder)) Directory.Delete(_dbFolder, recursive: true); } catch { }
            try { if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void MoveDocument_MovesAllFiles_UpdatesDb_KeepsUndoUntouched()
    {
        using var env = new TestEnv();
        var sourceDir = Path.Combine("Pläne", "Polierplan", "Haus 1", "KG");
        Directory.CreateDirectory(Path.Combine(env.Root, sourceDir));
        File.WriteAllText(Path.Combine(env.Root, sourceDir, "5998-100.pdf"), "pdf");
        File.WriteAllText(Path.Combine(env.Root, sourceDir, "5998-100.dwg"), "dwg");

        // Bestand: Import-Journal (completed) + Dokument + Revision + 2 Dateien
        var importId = env.Repo.CreateImportJournal("_Eingang", 2, profileId: null);
        env.Repo.CompleteImportJournal(importId, success: true);
        var docId = env.Repo.ResolveOrCreateDocument("proj1", "polierplan|5998_100|haus_1",
            "polierplan", "5998-100", "Polierplan", "", "Pläne", sourceDir, null, null);
        var now = DateTime.UtcNow.ToString("o");
        var revId = env.Repo.InsertRevision(docId, "A", "FileName",
            PlanArchive.Status.Current, now, null, now, lastImportId: importId);
        env.Repo.InsertFileForRevision(revId, "5998-100.pdf",
            Path.Combine(sourceDir, "5998-100.pdf"), ".pdf", "md5-p", 3, isPrimary: true);
        env.Repo.InsertFileForRevision(revId, "5998-100.dwg",
            Path.Combine(sourceDir, "5998-100.dwg"), ".dwg", "md5-d", 3, isPrimary: false);

        var entry = new PlanArchiveEntry(docId, "5998-100", "", "Polierplan",
            revId, "A", now, importId, "5998-100.pdf", Path.Combine(sourceDir, "5998-100.pdf"));
        var targetDir = Path.Combine("Pläne", "Polierplan", "Haus 2", "EG");

        var result = new ArchiveMoveService(env.Repo)
            .MoveDocument(entry, targetDir, env.Root);

        Assert.True(result.Success);
        Assert.Equal(2, result.MovedFiles);

        // Dateien physisch am neuen Ort
        Assert.True(File.Exists(Path.Combine(env.Root, targetDir, "5998-100.pdf")));
        Assert.True(File.Exists(Path.Combine(env.Root, targetDir, "5998-100.dwg")));
        Assert.False(File.Exists(Path.Combine(env.Root, sourceDir, "5998-100.pdf")));

        // DB = Ordner-Wahrheit: Pfade + Dokument-Ablage aktualisiert
        var files = env.Repo.GetFilesForRevision(revId);
        Assert.All(files, f => Assert.StartsWith(targetDir, f.RelativePath));
        var doc = env.Repo.GetDocumentByKey("polierplan|5998_100|haus_1");
        Assert.Equal(targetDir, doc!.RelativeDirectory);
        Assert.Equal("Pläne", doc.TargetFolder);

        // Undo-Sicherheit: der Move ist NICHT der „letzte Import"
        Assert.Equal(importId, env.Repo.GetLastCompletedImportId());

        // Audit: ManualOverride-Event an der Revision
        Assert.Contains(env.Repo.GetRevisionEvents(revId),
            ev => ev.EventType == PlanArchive.EventType.ManualOverride);
    }

    [Fact]
    public void MoveDocument_SameDirectory_FailsWithoutJournal()
    {
        using var env = new TestEnv();
        var sourceDir = Path.Combine("Pläne", "Polierplan", "Haus 1", "KG");
        Directory.CreateDirectory(Path.Combine(env.Root, sourceDir));
        File.WriteAllText(Path.Combine(env.Root, sourceDir, "5998-100.pdf"), "pdf");

        var docId = env.Repo.ResolveOrCreateDocument("proj1", "polierplan|5998_100|haus_1",
            "polierplan", "5998-100", "Polierplan", "", "Pläne", sourceDir, null, null);
        var now = DateTime.UtcNow.ToString("o");
        var revId = env.Repo.InsertRevision(docId, "A", "FileName",
            PlanArchive.Status.Current, now, null, now, null);
        env.Repo.InsertFileForRevision(revId, "5998-100.pdf",
            Path.Combine(sourceDir, "5998-100.pdf"), ".pdf", "md5-p", 3, isPrimary: true);

        var entry = new PlanArchiveEntry(docId, "5998-100", "", "Polierplan",
            revId, "A", now, null, "5998-100.pdf", Path.Combine(sourceDir, "5998-100.pdf"));

        var result = new ArchiveMoveService(env.Repo)
            .MoveDocument(entry, sourceDir, env.Root);

        Assert.False(result.Success);
        Assert.Contains("bereits", result.Error);
    }
}
