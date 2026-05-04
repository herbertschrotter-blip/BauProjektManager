using System.Collections.Concurrent;
using System.IO;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using Serilog;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// In-memory Persistenz-Inventar mit FS-Scan-Ergaenzung.
/// Singleton via DI. Thread-safe (ConcurrentDictionary).
/// BPM-104.01.
/// </summary>
public sealed class PersistenceRegistry : IPersistenceRegistry
{
    private readonly ConcurrentDictionary<string, PersistenceEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public void Register(PersistenceEntry entry)
    {
        if (string.IsNullOrEmpty(entry.AbsolutePath)) return;
        _entries[entry.AbsolutePath] = entry;
        Log.Verbose("PersistenceRegistry: registered {Type} at {Path}", entry.Type, entry.AbsolutePath);
    }

    public void Unregister(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath)) return;
        _entries.TryRemove(absolutePath, out _);
    }

    public IReadOnlyList<PersistenceEntry> GetAll()
    {
        return _entries.Values
            .OrderBy(e => e.Type)
            .ThenBy(e => e.Scope)
            .ThenBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IEnumerable<PersistenceEntry> GetByType(PersistenceType type)
    {
        return _entries.Values.Where(e => e.Type == type);
    }

    public void RescanFilesystem(string? basePath, IEnumerable<string> projectRoots)
    {
        try
        {
            var localAppData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BauProjektManager");

            // Local: LocalAppData root files (device-settings.json, bpm.db*)
            ScanDirectory(localAppData, "*", PersistenceScope.Local, recursive: false);

            // Local: Logs
            var logsDir = Path.Combine(localAppData, "Logs");
            ScanDirectory(logsDir, "BPM_*.log", PersistenceScope.Local, recursive: false);

            // Local: PlanManager-DBs pro Projekt-ID-Ordner
            var projectsDir = Path.Combine(localAppData, "Projects");
            if (Directory.Exists(projectsDir))
            {
                foreach (var projectIdDir in Directory.GetDirectories(projectsDir))
                {
                    ScanDirectory(projectIdDir, "*.db*", PersistenceScope.Local, recursive: false);
                }
            }

            // CloudShared: BasePath\.AppData\BauProjektManager\
            if (!string.IsNullOrEmpty(basePath))
            {
                var sharedDir = Path.Combine(basePath, ".AppData", "BauProjektManager");
                ScanDirectory(sharedDir, "*", PersistenceScope.CloudShared, recursive: false);
            }

            // ProjectLocal: jeder ProjectRoot\.bpm\
            foreach (var projectRoot in projectRoots ?? Array.Empty<string>())
            {
                if (string.IsNullOrEmpty(projectRoot)) continue;
                var bpmDir = Path.Combine(projectRoot, ".bpm");
                ScanDirectory(bpmDir, "*", PersistenceScope.ProjectLocal, recursive: true);
            }
        }
        catch (Exception ex)
        {
            Log.Warning("PersistenceRegistry: FS-Scan fehlgeschlagen: {Error}", ex.Message);
        }
    }

    private void ScanDirectory(string dir, string pattern, PersistenceScope scope, bool recursive)
    {
        if (!Directory.Exists(dir)) return;
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        try
        {
            foreach (var file in Directory.GetFiles(dir, pattern, searchOption))
            {
                if (_entries.ContainsKey(file)) continue; // bereits registriert

                var entry = ClassifyFile(file, scope);
                if (entry is not null)
                    _entries[file] = entry;
            }
        }
        catch (Exception ex)
        {
            Log.Verbose("PersistenceRegistry: Scan {Dir} fehlgeschlagen: {Error}", dir, ex.Message);
        }
    }

    private static PersistenceEntry? ClassifyFile(string absolutePath, PersistenceScope scope)
    {
        var name = Path.GetFileName(absolutePath);
        var nameLower = name.ToLowerInvariant();
        var ext = Path.GetExtension(absolutePath).ToLowerInvariant();

        // Skip transiente SQLite-Hilfsdateien — werden via Haupt-DB mit-getrackt
        if (ext == "-wal" || ext == "-shm") return null;
        if (nameLower.EndsWith(".db-wal") || nameLower.EndsWith(".db-shm")) return null;
        if (nameLower.EndsWith(".tmp")) return null;

        var (type, displayName) = nameLower switch
        {
            "bpm.db" => (PersistenceType.Database, "Hauptdatenbank"),
            "planmanager.db" => (PersistenceType.Database, "PlanManager-DB"),
            "device-settings.json" => (PersistenceType.Config, "device-settings.json"),
            "shared-config.json" => (PersistenceType.Config, "shared-config.json"),
            "settings.json" => (PersistenceType.Config, "settings.json (legacy)"),
            "registry.json" => (PersistenceType.Cache, "registry.json"),
            "pattern-templates.json" => (PersistenceType.Cache, "pattern-templates.json"),
            "manifest.json" => (PersistenceType.Config, ".bpm/manifest.json"),
            "project.json" => (PersistenceType.ProjectData, ".bpm/project.json"),
            _ when ext == ".log" => (PersistenceType.Log, name),
            _ when ext == ".json" && absolutePath.Contains(".bpm" + Path.DirectorySeparatorChar + "profiles", StringComparison.OrdinalIgnoreCase)
                => (PersistenceType.Config, $".bpm/profiles/{name}"),
            _ when ext == ".db" => (PersistenceType.Database, name),
            _ when ext == ".json" => (PersistenceType.Config, name),
            _ => (PersistenceType.Other, name)
        };

        return new PersistenceEntry(displayName, absolutePath, type, scope);
    }
}
