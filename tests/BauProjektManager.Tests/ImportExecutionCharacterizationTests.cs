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
/// BPM-120 T0: Characterization-Tests des Importpfads (ADR-064). Pinnen das
/// beobachtbare Verhalten von <see cref="ImportExecutionService.Execute"/>,
/// damit Verhaltensänderungen bei der Transaktions-Härtung sichtbar werden.
///
/// Asserts NUR auf härtungsstabile Endzustände (Dateien am Ziel, DB-Struktur,
/// Journal completed, Zeitinvariante). Stand nach T2 (Vorab-Journalisierung):
/// archive_path deterministisch journalisiert, skipDuplicate = echte Action
/// (Bucket A) — siehe ImportExecutionPreJournalTests für die AK-4/5/6-Beweise.
/// Bekannte Restfehler bewusst NICHT gepinnt: failed-Semantik terminal ohne
/// Rollback (T6), Undo-DB-Rollback nach Disk-Fehlern (T7).
/// </summary>
public class ImportExecutionCharacterizationTests
{
    // BPM-120 T1: E2E-Tests laufen bewusst auf echter Disk — eine Port-Instanz.
    private static readonly LocalFileSystem Fs = new();

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
            Root = Path.Combine(Path.GetTempPath(), "bpm-t0-test-" + projectId);
            Directory.CreateDirectory(Path.Combine(Root, "_Eingang"));
        }

        public void Dispose()
        {
            var dbPath = Repo.GetDatabasePath();
            Repo.Dispose();
            // Gezielt NUR den Pool dieser DB leeren — ClearAllPools würde unter
            // xunit-Parallellast die Pools fremder Test-Klassen mitreissen
            // (Flaky-Befund BPM-120 T0).
            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                SqliteConnection.ClearPool(conn);
            TempDb.Delete(dbPath);
            try { if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true); } catch { }
        }
    }

    private static PendingAssignment NewPending(
        string fileName, string md5, string planNumber = "5998-300")
    {
        var scan = new ScannedFile(
            Path.Combine("_Eingang", fileName), fileName,
            Path.GetExtension(fileName), 12, DateTime.UtcNow);
        return new PendingAssignment(
            new FingerprintedFile(scan, md5), CaptureBucket.NewCapture,
            "polierplan", "Polierplan", "Haus 2", "OG2", planNumber, null,
            Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2"), Match: null);
    }

    private static List<(string Id, string Status)> GetJournalRows(TestEnv env)
    {
        using var conn = new SqliteConnection($"Data Source={env.Repo.GetDatabasePath()}");
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, status FROM import_journal ORDER BY timestamp";
        using var reader = cmd.ExecuteReader();
        var rows = new List<(string, string)>();
        while (reader.Read())
            rows.Add((reader.GetString(0), reader.GetString(1)));
        return rows;
    }

    private static List<(string Type, string Status, string Source, string? Destination)>
        GetActionRows(TestEnv env, string importId)
    {
        using var conn = new SqliteConnection($"Data Source={env.Repo.GetDatabasePath()}");
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT action_type, action_status, source_path, destination_path
            FROM import_actions WHERE import_id = @iid ORDER BY action_order
            """;
        cmd.Parameters.AddWithValue("@iid", importId);
        using var reader = cmd.ExecuteReader();
        var rows = new List<(string, string, string, string?)>();
        while (reader.Read())
            rows.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3)));
        return rows;
    }

    [Fact]
    public void Execute_SingleNewCapture_MovesFileWritesArchiveAndCompletesJournal()
    {
        using var env = new TestEnv();
        File.WriteAllText(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf"), "pdf-content");

        var decisions = CaptureConfirmService.BuildDecisions(
            [NewPending("5998-300_OG2.pdf", "md5-pdf")], new PlanValueNormalizer());
        var result = new ImportExecutionService(env.Repo, new UlidIdGenerator(), Fs, Fs, Fs)
            .Execute(decisions, env.Root, "_Eingang");

        Assert.Equal(1, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Equal(0, result.Skipped);

        // Disk: Datei am Ziel, Eingang leer
        var targetRel = Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2", "5998-300_OG2.pdf");
        Assert.True(File.Exists(Path.Combine(env.Root, targetRel)));
        Assert.False(File.Exists(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf")));

        // Plan-Cache: Dokument + genau eine current-Revision mit Primärdatei
        var doc = env.Repo.GetDocumentByKey(decisions[0].DocumentKey!);
        Assert.NotNull(doc);
        var rev = Assert.Single(env.Repo.GetRevisionsForDocument(doc!.Id));
        Assert.Equal(PlanArchive.Status.Current, rev.RevisionStatus);
        Assert.Null(rev.SupersededAt);
        Assert.False(string.IsNullOrEmpty(rev.CurrentFrom));

        // Journal: ein completed-Vorgang, eine completed-Action, Pfade relativ
        var journal = Assert.Single(GetJournalRows(env));
        Assert.Equal("completed", journal.Status);
        var action = Assert.Single(GetActionRows(env, journal.Id));
        Assert.Equal("new", action.Type);
        Assert.Equal("completed", action.Status);
        Assert.Equal(Path.Combine("_Eingang", "5998-300_OG2.pdf"), action.Source);
        Assert.Equal(targetRel, action.Destination);
        Assert.False(Path.IsPathRooted(action.Source));
        Assert.False(Path.IsPathRooted(action.Destination));
    }

    [Fact]
    public void Execute_UpdateNewerIndex_SupersededAtEqualsCurrentFrom()
    {
        // Zeitinvariante old.superseded_at == new.current_from über den
        // Importpfad (bisher nur auf Repository-Ebene getestet).
        using var env = new TestEnv();
        var targetRelDir = Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2");
        Directory.CreateDirectory(Path.Combine(env.Root, targetRelDir));

        const string key = "polierplan|5998_300|haus_2|og2";
        var docId = env.Repo.ResolveOrCreateDocument("proj1", key,
            "polierplan", "5998-300", "Polierplan", "", "Pläne", targetRelDir, null, null);
        var now = DateTime.UtcNow.ToString("o");
        var oldImportId = env.Repo.CreateImportJournal("_Eingang", 1, profileId: null);
        env.Repo.CompleteImportJournal(oldImportId, success: true);
        var revAId = env.Repo.InsertRevision(docId, "A", "FileName",
            PlanArchive.Status.Current, now, null, now, lastImportId: oldImportId);
        File.WriteAllText(
            Path.Combine(env.Root, targetRelDir, "5998-300_OG2.pdf"), "old-content");

        File.WriteAllText(Path.Combine(env.Root, "_Eingang", "5998-300-B_OG2.pdf"), "new-content");
        var match = new KnownPlanDocument(docId, key, "5998-300", "Polierplan",
            "Pläne", targetRelDir, "A", revAId);
        var scan = new ScannedFile(
            Path.Combine("_Eingang", "5998-300-B_OG2.pdf"), "5998-300-B_OG2.pdf",
            ".pdf", 12, DateTime.UtcNow);
        var pending = new PendingAssignment(
            new FingerprintedFile(scan, "md5-b"), CaptureBucket.UpdateProposal,
            docId, "Polierplan", null, null, "5998-300", "B", targetRelDir, match);

        var decisions = CaptureConfirmService.BuildDecisions(
            [pending], new PlanValueNormalizer());
        var result = new ImportExecutionService(env.Repo, new UlidIdGenerator(), Fs, Fs, Fs)
            .Execute(decisions, env.Root, "_Eingang");

        Assert.Equal(0, result.Failed);

        var revisions = env.Repo.GetRevisionsForDocument(docId);
        Assert.Equal(2, revisions.Count);
        var revA = revisions.Single(r => r.Id == revAId);
        var revB = revisions.Single(r => r.Id != revAId);
        Assert.Equal(PlanArchive.Status.Superseded, revA.RevisionStatus);
        Assert.Equal(PlanArchive.Status.Current, revB.RevisionStatus);
        Assert.NotNull(revA.SupersededAt);
        Assert.Equal(revA.SupersededAt, revB.CurrentFrom);

        // Kein Datenverlust: neue Datei am Ziel, alte Datei bleibt erhalten
        // (anderer Dateiname → heute kein Archiv-Move; siehe SameFileName-Test).
        Assert.True(File.Exists(Path.Combine(env.Root, targetRelDir, "5998-300-B_OG2.pdf")));
        Assert.True(File.Exists(Path.Combine(env.Root, targetRelDir, "5998-300_OG2.pdf")));

        // Journal des Update-Imports: completed, Action-Typ indexUpdate
        var updateJournal = GetJournalRows(env).Single(j => j.Id != oldImportId);
        Assert.Equal("completed", updateJournal.Status);
        var action = Assert.Single(GetActionRows(env, updateJournal.Id));
        Assert.Equal("indexUpdate", action.Type);
        Assert.Equal("completed", action.Status);
    }

    [Fact]
    public void Execute_UpdateSameFileName_ArchivesOldFileBeforeOverwrite()
    {
        // Namenskollision am Ziel: ArchiveExistingFile schützt den alten Inhalt
        // vor dem überschreibenden Move (_Archiv-Unterordner). T3 macht den
        // Archivpfad deterministisch — dieser Test pinnt nur "kein Datenverlust".
        using var env = new TestEnv();
        var targetRelDir = Path.Combine("Pläne", "Polierplan", "Haus 2", "OG2");
        Directory.CreateDirectory(Path.Combine(env.Root, targetRelDir));

        const string key = "polierplan|5998_300|haus_2|og2";
        var docId = env.Repo.ResolveOrCreateDocument("proj1", key,
            "polierplan", "5998-300", "Polierplan", "", "Pläne", targetRelDir, null, null);
        var now = DateTime.UtcNow.ToString("o");
        var oldImportId = env.Repo.CreateImportJournal("_Eingang", 1, profileId: null);
        env.Repo.CompleteImportJournal(oldImportId, success: true);
        var revAId = env.Repo.InsertRevision(docId, "A", "FileName",
            PlanArchive.Status.Current, now, null, now, lastImportId: oldImportId);
        File.WriteAllText(
            Path.Combine(env.Root, targetRelDir, "5998-300_OG2.pdf"), "old-content");

        File.WriteAllText(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf"), "new-content");
        var match = new KnownPlanDocument(docId, key, "5998-300", "Polierplan",
            "Pläne", targetRelDir, "A", revAId);
        var scan = new ScannedFile(
            Path.Combine("_Eingang", "5998-300_OG2.pdf"), "5998-300_OG2.pdf",
            ".pdf", 12, DateTime.UtcNow);
        var pending = new PendingAssignment(
            new FingerprintedFile(scan, "md5-b"), CaptureBucket.UpdateProposal,
            docId, "Polierplan", null, null, "5998-300", "B", targetRelDir, match);

        var decisions = CaptureConfirmService.BuildDecisions(
            [pending], new PlanValueNormalizer());
        var result = new ImportExecutionService(env.Repo, new UlidIdGenerator(), Fs, Fs, Fs)
            .Execute(decisions, env.Root, "_Eingang");

        Assert.Equal(0, result.Failed);

        // Ziel hat den NEUEN Inhalt, der alte liegt gesichert im _Archiv
        var targetAbs = Path.Combine(env.Root, targetRelDir, "5998-300_OG2.pdf");
        Assert.Equal("new-content", File.ReadAllText(targetAbs));
        var archiveDir = Path.Combine(env.Root, targetRelDir, "_Archiv");
        var archived = Assert.Single(Directory.GetFiles(archiveDir));
        Assert.Equal("old-content", File.ReadAllText(archived));
    }

    [Fact]
    public void Execute_MixedNewAndSkipIdentical_DeletesDuplicateFromInbox()
    {
        // Gemischter Import (seit T2/AK 6): New und skipDuplicate werden
        // GEMEINSAM vorab journalisiert; die bestätigte Dublette wird beim
        // Confirm aus dem Eingang entfernt (Skipped=1, kein Papierkorb).
        using var env = new TestEnv();
        File.WriteAllText(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf"), "pdf-content");
        File.WriteAllText(Path.Combine(env.Root, "_Eingang", "5998-310_OG2.pdf"), "dup-content");

        var decisions = CaptureConfirmService.BuildDecisions(
            [NewPending("5998-300_OG2.pdf", "md5-new"),
             NewPending("5998-310_OG2.pdf", "md5-dup", planNumber: "5998-310")],
            new PlanValueNormalizer());
        var skipDecision = decisions.Single(d => d.File.Parsed.FileName == "5998-310_OG2.pdf")
            with { Status = ImportStatus.SkipIdentical };
        var mixed = new List<ImportDecision>
        {
            decisions.Single(d => d.File.Parsed.FileName == "5998-300_OG2.pdf"),
            skipDecision
        };

        var result = new ImportExecutionService(env.Repo, new UlidIdGenerator(), Fs, Fs, Fs)
            .Execute(mixed, env.Root, "_Eingang");

        Assert.Equal(1, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Equal(1, result.Skipped);

        // Eingang komplett leer: New verschoben, Dublette gelöscht (kein Papierkorb)
        Assert.False(File.Exists(Path.Combine(env.Root, "_Eingang", "5998-300_OG2.pdf")));
        Assert.False(File.Exists(Path.Combine(env.Root, "_Eingang", "5998-310_OG2.pdf")));
        Assert.True(File.Exists(Path.Combine(
            env.Root, "Pläne", "Polierplan", "Haus 2", "OG2", "5998-300_OG2.pdf")));

        // Journal completed mit BEIDEN Actions (T2/AK 6: gemeinsam journalisiert)
        var journal = Assert.Single(GetJournalRows(env));
        Assert.Equal("completed", journal.Status);
        var actions = GetActionRows(env, journal.Id);
        Assert.Equal(2, actions.Count);
        Assert.Contains(actions, a => a.Type == "new" && a.Status == "completed");
        Assert.Contains(actions, a =>
            a.Type == "skipDuplicate" && a.Status == "completed" && a.Destination is null);
    }
}
