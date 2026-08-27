using System.IO;

namespace BauProjektManager.Tests.Fakes;

/// <summary>
/// BPM-123: Test-Datenbanken liegen via dbPathOverride unter %TEMP% — nie im
/// echten App-Datenbereich (LocalAppData\Projects). Datei-Cleanup inkl. der
/// WAL-/SHM-Begleitdateien (journal_mode=WAL haelt sonst Locks).
/// </summary>
internal static class TempDb
{
    public static string NewTempDbPath(string projectId)
        => Path.Combine(Path.GetTempPath(), $"bpm-plandb-{projectId}.db");

    public static void Delete(string dbPath)
    {
        foreach (var p in new[] { dbPath, dbPath + "-wal", dbPath + "-shm" })
        {
            try { if (File.Exists(p)) File.Delete(p); }
            catch { /* best effort — liegt ohnehin in %TEMP% */ }
        }
    }
}
