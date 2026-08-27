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
/// BPM-120 T1 (AK 3): beweist, dass der Importpfad ueber den fault-faehigen
/// <see cref="FakeFileStore"/> testbar ist — Dateioperationen schlagen gezielt
/// fehl, ohne echte Disk. DB bleibt echte SQLite (Temp), Dateien sind rein
/// virtuell. Journal-Terminal-Semantik (T6) wird bewusst NICHT gepinnt.
/// </summary>
public class ImportExecutionFaultInjectionTests
{
    private sealed class TestEnv : IDisposable
    {
        public PlanManagerDatabase Repo { get; }
        /// <summary>Virtueller Projekt-Root — existiert NUR im FakeFileStore.</summary>
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "bpm-fault-virtual");
        private readonly string _dbFolder;

        public TestEnv()
        {
            IIdGenerator idGen = new UlidIdGenerator();
            var projectId = idGen.NewId();
            _dbFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BauProjektManager", "Projects", projectId);
            Repo = new PlanManagerDatabase(projectId, idGen);
        }

        public void Dispose()
        {
            var dbPath = Repo.GetDatabasePath();
            Repo.Dispose();
            using (var pc = new SqliteConnection($"Data Source={dbPath}"))
                SqliteConnection.ClearPool(pc);
            try { if (Directory.Exists(_dbFolder)) Directory.Delete(_dbFolder, recursive: true); } catch { }
        }
    }

    private static PendingAssignment NewPending(string fileName, string md5)
    {
        var scan = new ScannedFile(
            Path.Combine("_Eingang", fileName), fileName,
            Path.GetExtension(fileName), 12, DateTime.UtcNow);
        return new PendingAssignment(
            new FingerprintedFile(scan, md5), CaptureBucket.NewCapture,
            "polierplan", "Polierplan", "Haus 2", "OG2", "5998-300", null,
            Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2"), Match: null);
    }

    [Fact]
    public void Execute_MoveFault_ActionFailedNoFileLossNoDocument()
    {
        using var env = new TestEnv();
        var fake = new FakeFileStore();
        var inboxAbs = Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf");
        fake.AddFile(inboxAbs);
        fake.FailNext(FakeFileStore.FileOp.Move, "5998-300_OG2.pdf");

        var decisions = CaptureConfirmService.BuildDecisions(
            [NewPending("5998-300_OG2.pdf", "md5-pdf")], new PlanValueNormalizer());
        var result = new ImportExecutionService(
            env.Repo, new UlidIdGenerator(), fake, fake, fake)
            .Execute(decisions, env.Root, "_Eingang");

        // Fehler sauber gemeldet, nicht durchgeschlagen
        Assert.Equal(1, result.Failed);
        Assert.Equal(0, result.Succeeded);
        Assert.Single(result.Errors);

        // Kein Datenverlust: Quelldatei liegt weiter im (virtuellen) Eingang,
        // am Ziel ist nichts entstanden
        Assert.True(fake.FileExists(inboxAbs));
        Assert.False(fake.FileExists(Path.Combine(
            env.Root, "Pläne", "Polierplan", "Haus 2", "OG2", "5998-300_OG2.pdf")));

        // Kein Plan-Cache-Eintrag (DB-Writes liegen hinter dem Move)
        Assert.Null(env.Repo.GetDocumentByKey(decisions[0].DocumentKey!));

        // Action als failed markiert, mit Fehlermeldung
        using var conn = new SqliteConnection($"Data Source={env.Repo.GetDatabasePath()}");
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT action_status, error_message FROM import_actions";
        using var reader = cmd.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal("failed", reader.GetString(0));
        Assert.False(reader.IsDBNull(1));
    }

    [Fact]
    public void FakeFileStore_FailNext_FailsExactlyNTimesThenRecovers()
    {
        var fake = new FakeFileStore();
        fake.AddFile(@"C:\virt\in\a.pdf");
        fake.CreateDirectory(@"C:\virt\out");
        fake.FailNext(FakeFileStore.FileOp.Move, @"a.pdf",
            new IOException("Datei gesperrt"), times: 2);

        Assert.Throws<IOException>(() => fake.MoveFile(@"C:\virt\in\a.pdf", @"C:\virt\out\a.pdf"));
        Assert.Throws<IOException>(() => fake.MoveFile(@"C:\virt\in\a.pdf", @"C:\virt\out\a.pdf"));

        // Dritter Versuch geht durch — Store blieb durch die Faults unveraendert
        fake.MoveFile(@"C:\virt\in\a.pdf", @"C:\virt\out\a.pdf");
        Assert.True(fake.FileExists(@"C:\virt\out\a.pdf"));
        Assert.False(fake.FileExists(@"C:\virt\in\a.pdf"));

        // Andere Operationen bleiben von der Move-Regel unberuehrt
        fake.DeleteFile(@"C:\virt\out\a.pdf");
        Assert.False(fake.FileExists(@"C:\virt\out\a.pdf"));
    }
}
