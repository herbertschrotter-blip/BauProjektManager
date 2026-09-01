using System.IO;
using System.Security.Cryptography;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.Tests.Fakes;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests für <see cref="PlanReconcileService"/> (BPM-112.06c, ADR-061 P.6):
/// Drift der getrackten Teilmenge — Exists+Size zuerst, MD5 nur für die
/// Relink-Suche. Sandbox = Temp-DB (BPM-123) + Temp-Projektordner.
/// </summary>
public class PlanReconcileServiceTests
{
    private sealed class TestEnv : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        public string Root { get; }

        public TestEnv()
        {
            IIdGenerator idGen = new UlidIdGenerator();
            var projectId = idGen.NewId();
            Repo = new PlanManagerDatabase(projectId, idGen,
                dbPathOverride: TempDb.NewTempDbPath(projectId));
            Root = Path.Combine(Path.GetTempPath(), "bpm-reconcile-test-" + projectId);
            Directory.CreateDirectory(Root);
        }

        /// <summary>Getrackte Datei anlegen: Disk-Datei + Dokument/Revision/File in der DB.</summary>
        public string SeedTrackedFile(string relativeDir, string fileName, string content,
            long? dbSizeOverride = null, bool writeToDisk = true)
        {
            var relPath = Path.Combine(relativeDir, fileName);
            if (writeToDisk)
            {
                Directory.CreateDirectory(Path.Combine(Root, relativeDir));
                File.WriteAllText(Path.Combine(Root, relPath), content);
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var md5 = Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant();
            var docId = Repo.ResolveOrCreateDocument("proj1", $"polierplan|{fileName}|h1",
                "polierplan", fileName, "Polierplan", "", "Pläne", relativeDir, null, null);
            var now = DateTime.UtcNow.ToString("o");
            var revId = Repo.InsertRevision(docId, "A", "FileName",
                PlanArchive.Status.Current, now, null, now, lastImportId: null);
            Repo.InsertFileForRevision(revId, fileName, relPath, ".pdf",
                md5, dbSizeOverride ?? bytes.Length, isPrimary: true);
            return relPath;
        }

        public void Dispose()
        {
            var dbPath = Repo.GetDatabasePath();
            Repo.Dispose();
            using (var pc = new SqliteConnection($"Data Source={dbPath}"))
                SqliteConnection.ClearPool(pc);
            TempDb.Delete(dbPath);
            try { if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true); } catch { }
        }
    }

    private static PlanReconcileService CreateSut(TestEnv env)
        => new(env.Repo, new LocalFileSystem());

    [Fact]
    public void AllFilesPresent_NoDrift()
    {
        using var env = new TestEnv();
        env.SeedTrackedFile(Path.Combine("Pläne", "KG"), "5998-100.pdf", "inhalt-a");

        var result = CreateSut(env).Reconcile(env.Root);

        Assert.Equal(1, result.CheckedFiles);
        Assert.Empty(result.Drift);
    }

    [Fact]
    public void MissingFile_ReportsMissingOnDisk()
    {
        using var env = new TestEnv();
        env.SeedTrackedFile(Path.Combine("Pläne", "KG"), "5998-100.pdf", "inhalt-a",
            writeToDisk: false);

        var result = CreateSut(env).Reconcile(env.Root);

        var drift = Assert.Single(result.Drift);
        Assert.Equal(DriftKind.MissingOnDisk, drift.Kind);
        Assert.Null(drift.RelinkPath);
    }

    [Fact]
    public void MissingButSameContentElsewhere_ReportsRelinkCandidate()
    {
        using var env = new TestEnv();
        env.SeedTrackedFile(Path.Combine("Pläne", "KG"), "5998-100.pdf", "inhalt-a",
            writeToDisk: false);
        // Gleicher Name + Inhalt an anderem Ort — der einzige Hash-Einsatz im Reconcile
        var otherDir = Path.Combine(env.Root, "Pläne", "EG");
        Directory.CreateDirectory(otherDir);
        File.WriteAllText(Path.Combine(otherDir, "5998-100.pdf"), "inhalt-a");

        var result = CreateSut(env).Reconcile(env.Root);

        var drift = Assert.Single(result.Drift);
        Assert.Equal(DriftKind.RelinkCandidate, drift.Kind);
        Assert.Equal(Path.Combine("Pläne", "EG", "5998-100.pdf"), drift.RelinkPath);
    }

    [Fact]
    public void SizeMismatch_ReportsChangedOnDisk()
    {
        using var env = new TestEnv();
        env.SeedTrackedFile(Path.Combine("Pläne", "KG"), "5998-100.pdf", "inhalt-a",
            dbSizeOverride: 999);

        var result = CreateSut(env).Reconcile(env.Root);

        var drift = Assert.Single(result.Drift);
        Assert.Equal(DriftKind.ChangedOnDisk, drift.Kind);
    }
}
