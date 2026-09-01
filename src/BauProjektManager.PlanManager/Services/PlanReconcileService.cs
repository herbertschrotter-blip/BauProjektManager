using System.IO;
using System.Security.Cryptography;
using BauProjektManager.Domain.Interfaces;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Startup-Reconcile (BPM-112.06c, ADR-061 P.6): prüft NUR die getrackte
/// Teilmenge (current-Revisionen) gegen die Disk — Exists + Size zuerst,
/// MD5 nur bei Bedarf (Relink-Suche für fehlende Dateien über den Dateinamen).
/// Relink ist immer nur ein VORSCHLAG, nie eine automatische Aktion.
/// System.IO.Path nur fuer pure Pfad-String-Ops (ADR-060-Praezisierung);
/// alle Disk-Zugriffe laufen ueber den injizierten IFileSystemReader.
/// </summary>
public class PlanReconcileService
{
    private readonly PlanManagerDatabase _db;
    private readonly IFileSystemReader _fs;

    public PlanReconcileService(PlanManagerDatabase db, IFileSystemReader fs)
    {
        _db = db;
        _fs = fs;
    }

    public ReconcileResult Reconcile(string projectRootPath)
    {
        var drift = new List<DriftEntry>();
        List<TrackedFileRecord> tracked;
        try
        {
            tracked = _db.GetTrackedFilesForReconcile();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Reconcile: getrackte Dateien nicht ladbar");
            return new ReconcileResult(0, drift);
        }

        foreach (var file in tracked)
        {
            var fullPath = Path.Combine(projectRootPath, file.RelativePath);
            var info = _fs.GetFileInfo(fullPath);

            if (!info.Exists)
            {
                var relink = FindRelinkCandidate(projectRootPath, file);
                drift.Add(new DriftEntry(
                    file.RelativePath, file.FileName,
                    relink is null ? DriftKind.MissingOnDisk : DriftKind.RelinkCandidate,
                    file.PlanNumber, file.PlanIndex, relink));
                continue;
            }

            if (file.FileSize > 0 && info.Length != file.FileSize)
                drift.Add(new DriftEntry(
                    file.RelativePath, file.FileName, DriftKind.ChangedOnDisk,
                    file.PlanNumber, file.PlanIndex, RelinkPath: null));
        }

        if (drift.Count > 0)
            Log.Information("Reconcile: {Checked} getrackte Dateien, {Drift} Drift-Hinweis(e)",
                tracked.Count, drift.Count);
        return new ReconcileResult(tracked.Count, drift);
    }

    /// <summary>
    /// Relink-Vorschlag für eine fehlende Datei: gleicher Dateiname im Projektbaum,
    /// dann Size-Check, dann MD5 (einziger Hash-Einsatz im Reconcile).
    /// </summary>
    private string? FindRelinkCandidate(string projectRootPath, TrackedFileRecord file)
    {
        if (file.Md5.Length == 0)
            return null;
        try
        {
            foreach (var candidate in _fs.EnumerateFiles(projectRootPath, file.FileName, recursive: true))
            {
                var info = _fs.GetFileInfo(candidate);
                if (file.FileSize > 0 && info.Length != file.FileSize)
                    continue;
                if (string.Equals(ComputeMd5(candidate), file.Md5, StringComparison.OrdinalIgnoreCase))
                    return candidate.Length > projectRootPath.Length
                        ? candidate[projectRootPath.Length..].TrimStart('\\', '/')
                        : candidate;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Reconcile: Relink-Suche fehlgeschlagen fuer {Name}", file.FileName);
        }
        return null;
    }

    private string ComputeMd5(string fullPath)
    {
        using var md5 = MD5.Create();
        using var stream = _fs.OpenRead(fullPath);
        return Convert.ToHexString(md5.ComputeHash(stream));
    }
}

/// <summary>Getrackte Datei mit Fingerprint — Zeile der Reconcile-Grundlage.</summary>
public sealed record TrackedFileRecord(
    string RelativePath, string FileName, string Md5, long FileSize,
    string PlanNumber, string? PlanIndex);

/// <summary>Drift-Arten nach ADR-061 P.6.</summary>
public enum DriftKind
{
    MissingOnDisk,
    ChangedOnDisk,
    RelinkCandidate
}

/// <summary>Ein Drift-Hinweis; RelinkPath nur bei <see cref="DriftKind.RelinkCandidate"/>.</summary>
public sealed record DriftEntry(
    string RelativePath, string FileName, DriftKind Kind,
    string PlanNumber, string? PlanIndex, string? RelinkPath);

/// <summary>Reconcile-Ergebnis: geprüfte Dateien + Drift-Liste.</summary>
public sealed record ReconcileResult(int CheckedFiles, IReadOnlyList<DriftEntry> Drift);
