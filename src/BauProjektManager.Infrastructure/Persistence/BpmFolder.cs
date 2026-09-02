using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// Gemeinsame Pfad- und Schreibhelfer für den versteckten .bpm/-Ordner (ADR-046).
/// Genutzt von <see cref="ManifestService"/> und <see cref="ProjectExportService"/>.
/// Infrastructure bleibt laut ADR-060 P.4 auf echtem System.IO.
/// </summary>
internal static class BpmFolder
{
    public const string FolderName = ".bpm";
    public const string ManifestFileName = "manifest.json";
    public const string ExportFileName = "project.json";
    public const string LegacyManifestFileName = ".bpm-manifest";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string FolderPath(string projectRootPath)
        => Path.Combine(projectRootPath, FolderName);

    public static string ManifestPath(string projectRootPath)
        => Path.Combine(projectRootPath, FolderName, ManifestFileName);

    public static string ExportPath(string projectRootPath)
        => Path.Combine(projectRootPath, FolderName, ExportFileName);

    public static string LegacyManifestPath(string projectRootPath)
        => Path.Combine(projectRootPath, LegacyManifestFileName);

    /// <summary>Legt .bpm/ an (Hidden), falls noch nicht vorhanden.</summary>
    public static void EnsureFolder(string projectRootPath)
    {
        var bpmDir = FolderPath(projectRootPath);
        if (Directory.Exists(bpmDir)) return;

        Directory.CreateDirectory(bpmDir);
        File.SetAttributes(bpmDir, FileAttributes.Hidden | FileAttributes.Directory);
        Log.Debug("Created .bpm/ folder at {Path}", bpmDir);
    }

    /// <summary>
    /// Atomic Write (temp → rename). Entfernt ein eventuelles ReadOnly-Attribut
    /// (alte Einzeldatei-Ära) vor dem Überschreiben.
    /// </summary>
    public static void WriteJsonAtomic<T>(string path, T value)
    {
        RemoveReadOnly(path);

        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(value, JsonOptions));

        if (File.Exists(path))
            File.Delete(path);

        File.Move(tempPath, path);
    }

    public static T? ReadJson<T>(string path) where T : class
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to read {Path}", path);
            return null;
        }
    }

    /// <summary>Liest nur das Feld schemaVersion; 0 wenn nicht lesbar oder nicht vorhanden.</summary>
    public static int ReadSchemaVersion(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty("schemaVersion", out var v) && v.TryGetInt32(out var version)
                ? version
                : 0;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not read schemaVersion from {Path}", path);
            return 0;
        }
    }

    private static void RemoveReadOnly(string path)
    {
        if (!File.Exists(path)) return;

        var attrs = File.GetAttributes(path);
        if (attrs.HasFlag(FileAttributes.ReadOnly))
            File.SetAttributes(path, attrs & ~FileAttributes.ReadOnly);
    }
}
