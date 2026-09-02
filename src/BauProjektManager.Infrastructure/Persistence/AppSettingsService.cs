using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BauProjektManager.Domain.Models;
using Serilog;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// Manages device-settings.json (local, per machine) and shared-config.json (cloud, synced).
/// BPM-069: Die AppSettings-Fassade (Load()/Save(AppSettings)) und die settings.json-Migration
/// sind entfernt — Aufrufer arbeiten direkt mit LoadDevice/SaveDevice bzw.
/// LoadSharedOrDefault/SaveSharedOrDefault. Eine noch vorhandene Legacy-Datei wird beim
/// ersten Start geloescht (Fruehphase, keine Migration mehr noetig).
/// </summary>
public class AppSettingsService
{
    private readonly string _localDir;
    private readonly string _deviceSettingsPath;
    private readonly string _legacySettingsPath;

    private DeviceSettings? _cachedDevice;
    private SharedConfig? _cachedShared;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AppSettingsService()
    {
        _localDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BauProjektManager");
        Directory.CreateDirectory(_localDir);
        _deviceSettingsPath = Path.Combine(_localDir, "device-settings.json");
        _legacySettingsPath = Path.Combine(_localDir, "settings.json");
    }

    // ═══════════════════════════════════════════════════════════
    //  DeviceSettings (lokal, pro Gerät)
    // ═══════════════════════════════════════════════════════════

    public DeviceSettings LoadDevice()
    {
        if (_cachedDevice is not null)
            return _cachedDevice;

        // BPM-069: Legacy settings.json wird nicht mehr migriert, nur entsorgt.
        RemoveLegacySettingsFile();

        if (File.Exists(_deviceSettingsPath))
        {
            try
            {
                var json = File.ReadAllText(_deviceSettingsPath);
                _cachedDevice = JsonSerializer.Deserialize<DeviceSettings>(json, JsonOptions)
                    ?? new DeviceSettings();
                Log.Debug("DeviceSettings loaded from {Path}", _deviceSettingsPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load device-settings, using defaults");
                _cachedDevice = new DeviceSettings();
            }
        }
        else
        {
            _cachedDevice = new DeviceSettings();
            Log.Information("No device-settings found, first run detected");
        }

        _cachedDevice.MachineName = Environment.MachineName;

        // Ensure stable DeviceId
        if (string.IsNullOrEmpty(_cachedDevice.DeviceId))
        {
            _cachedDevice.DeviceId = Guid.NewGuid().ToString("N")[..12];
            Log.Information("Generated new DeviceId: {DeviceId}", _cachedDevice.DeviceId);
            SaveDevice(_cachedDevice);
        }

        // Ensure shared-config exists if BasePath is known
        if (!string.IsNullOrEmpty(_cachedDevice.BasePath))
        {
            var sharedDir = GetSharedConfigDir(_cachedDevice.BasePath);
            var sharedPath = Path.Combine(sharedDir, "shared-config.json");
            if (!File.Exists(sharedPath))
            {
                Log.Information("shared-config.json missing, creating from defaults");
                var shared = new SharedConfig();
                SaveShared(shared, _cachedDevice.BasePath, _cachedDevice.DeviceId);

                // Bind WorkspaceId to device
                if (string.IsNullOrEmpty(_cachedDevice.WorkspaceId))
                {
                    _cachedDevice.WorkspaceId = shared.WorkspaceId;
                    SaveDevice(_cachedDevice);
                }
            }
            else if (string.IsNullOrEmpty(_cachedDevice.WorkspaceId))
            {
                // shared-config exists but device has no WorkspaceId yet → bind
                var shared = LoadShared(_cachedDevice.BasePath);
                if (!string.IsNullOrEmpty(shared.WorkspaceId))
                {
                    _cachedDevice.WorkspaceId = shared.WorkspaceId;
                    SaveDevice(_cachedDevice);
                    Log.Information("Bound WorkspaceId {WorkspaceId} to device", shared.WorkspaceId);
                }
            }
        }

        return _cachedDevice;
    }

    public void SaveDevice(DeviceSettings device)
    {
        try
        {
            var json = JsonSerializer.Serialize(device, JsonOptions);
            var tempPath = _deviceSettingsPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _deviceSettingsPath, overwrite: true);
            _cachedDevice = device;
            Log.Information("DeviceSettings saved to {Path}", _deviceSettingsPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save device-settings");
            throw;
        }
    }

    /// <summary>
    /// Vergleicht zwei DeviceSettings-Instanzen Feld-fuer-Feld (ohne DeviceId/WorkspaceId/DevTools,
    /// die werden ohnehin aus existing uebernommen). Wenn alle Felder gleich sind, kann SaveDevice
    /// uebersprungen werden — verhindert unnoetige device-settings.json Writes (BPM-102).
    /// </summary>
    private static bool DeviceFieldsEqual(DeviceSettings a, DeviceSettings b)
    {
        return a.SchemaVersion == b.SchemaVersion
            && a.MachineName == b.MachineName
            && a.CloudStoragePath == b.CloudStoragePath
            && a.BasePath == b.BasePath
            && a.ArchivePath == b.ArchivePath
            && a.ExportPath == b.ExportPath
            && a.IsFirstRun == b.IsFirstRun
            && Nullable.Equals(a.SetupCompletedAt, b.SetupCompletedAt);
    }

    // ═══════════════════════════════════════════════════════════
    //  SharedConfig (Cloud, synct zwischen Geräten)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the shared config directory path: BasePath/.AppData/BauProjektManager/
    /// </summary>
    public static string GetSharedConfigDir(string basePath)
    {
        return Path.Combine(basePath, ".AppData", "BauProjektManager");
    }

    public SharedConfig LoadShared(string basePath)
    {
        if (_cachedShared is not null)
            return _cachedShared;

        var sharedDir = GetSharedConfigDir(basePath);
        var sharedPath = Path.Combine(sharedDir, "shared-config.json");

        if (File.Exists(sharedPath))
        {
            try
            {
                var json = File.ReadAllText(sharedPath);
                _cachedShared = JsonSerializer.Deserialize<SharedConfig>(json, JsonOptions)
                    ?? new SharedConfig();
                Log.Debug("SharedConfig loaded from {Path}", sharedPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load shared-config, using defaults");
                _cachedShared = new SharedConfig();
            }
        }
        else
        {
            _cachedShared = new SharedConfig();
            Log.Information("No shared-config found at {Path}, using defaults", sharedPath);
        }

        return _cachedShared;
    }

    public void SaveShared(SharedConfig shared, string basePath, string? deviceId = null)
    {
        var sharedDir = GetSharedConfigDir(basePath);
        Directory.CreateDirectory(sharedDir);
        var sharedPath = Path.Combine(sharedDir, "shared-config.json");

        // WorkspaceId generieren wenn noch leer
        if (string.IsNullOrEmpty(shared.WorkspaceId))
        {
            shared.WorkspaceId = Guid.NewGuid().ToString("N")[..12];
            Log.Information("Generated new WorkspaceId: {WorkspaceId}", shared.WorkspaceId);
        }

        // Revision + Metadaten
        shared.Revision++;
        shared.UpdatedAtUtc = DateTime.UtcNow;
        shared.UpdatedByDeviceId = deviceId ?? _cachedDevice?.DeviceId ?? "";

        Log.Debug("Saving shared-config revision {Revision} to {Path}", shared.Revision, sharedPath);
        try
        {
            var json = JsonSerializer.Serialize(shared, JsonOptions);
            var tempPath = sharedPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, sharedPath, overwrite: true);
            _cachedShared = shared;
            Log.Information("SharedConfig saved to {Path}", sharedPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save shared-config");
            throw;
        }
    }

    /// <summary>
    /// SharedConfig zum BasePath der DeviceSettings. Ohne BasePath (vor der Ersteinrichtung)
    /// liefert sie Defaults, die nicht gecacht werden — nach dem Setup wird echt geladen.
    /// </summary>
    public SharedConfig LoadSharedOrDefault()
    {
        var basePath = LoadDevice().BasePath;
        return string.IsNullOrEmpty(basePath) ? new SharedConfig() : LoadShared(basePath);
    }

    /// <summary>Speichert die SharedConfig zum BasePath der DeviceSettings; ohne BasePath nur Warnung.</summary>
    public void SaveSharedOrDefault(SharedConfig shared)
    {
        var device = LoadDevice();
        if (string.IsNullOrEmpty(device.BasePath))
        {
            Log.Warning("SharedConfig not saved — BasePath not configured yet");
            _cachedShared = shared;
            return;
        }
        SaveShared(shared, device.BasePath, device.DeviceId);
    }

    // ═══════════════════════════════════════════════════════════
    //  Legacy settings.json (BPM-069: nur noch Entsorgung)
    // ═══════════════════════════════════════════════════════════

    private void RemoveLegacySettingsFile()
    {
        if (!File.Exists(_legacySettingsPath)) return;
        try
        {
            File.Delete(_legacySettingsPath);
            Log.Information("Legacy settings.json removed (BPM-069, split format is authoritative)");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Legacy settings.json could not be removed");
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Cloud-Speicher-Erkennung
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Detect cloud storage path from environment variables and common paths.
    /// Supports OneDrive, OneDrive Business, and common mount points.
    /// Returns null if not found.
    /// </summary>
    public static string? DetectCloudStoragePath()
    {
        // 1. Environment variables
        var envVars = new[] { "OneDrive", "OneDriveCommercial", "OneDriveConsumer" };
        foreach (var envVar in envVars)
        {
            var path = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                Log.Information("Cloud storage detected via {EnvVar}: {Path}", envVar, path);
                return path;
            }
        }

        // 2. Dropbox via info.json (offizielle Methode)
        var dropboxInfo = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Dropbox", "info.json");
        if (File.Exists(dropboxInfo))
        {
            try
            {
                var json = File.ReadAllText(dropboxInfo);
                // Simple parse: find "path" value
                var match = System.Text.RegularExpressions.Regex.Match(json, @"""path""\s*:\s*""([^""]+)""");
                if (match.Success)
                {
                    var dbPath = match.Groups[1].Value.Replace("\\\\", "\\");
                    if (Directory.Exists(dbPath))
                    {
                        Log.Information("Cloud storage detected via Dropbox info.json: {Path}", dbPath);
                        return dbPath;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Dropbox info.json parse failed: {Error}", ex.Message);
            }
        }

        // 3. Common filesystem paths
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var commonPaths = new[]
        {
            // OneDrive
            Path.Combine(userProfile, "OneDrive"),
            Path.Combine(userProfile, "OneDrive - Personal"),
            // Dropbox
            Path.Combine(userProfile, "Dropbox"),
            // Google Drive
            Path.Combine(userProfile, "Google Drive"),
            Path.Combine(userProfile, "GoogleDrive"),
            Path.Combine(userProfile, "My Drive"),
            // Drive-Mounts (häufig bei OneDrive/Dropbox auf separatem Laufwerk)
            "D:\\OneDrive",
            "E:\\OneDrive",
            "D:\\Dropbox",
            "E:\\Dropbox",
            "D:\\Google Drive",
            "E:\\Google Drive"
        };

        foreach (var path in commonPaths)
        {
            if (Directory.Exists(path))
            {
                Log.Information("Cloud storage detected via common path: {Path}", path);
                return path;
            }
        }

        Log.Warning("Cloud storage not detected");
        return null;
    }

    /// <summary>
    /// Detect ALL cloud storage paths on this machine.
    /// Returns list of found paths (for display in setup dialog).
    /// </summary>
    public static List<string> DetectAllCloudStoragePaths()
    {
        var found = new List<string>();

        // Environment variables
        var envVars = new[] { "OneDrive", "OneDriveCommercial", "OneDriveConsumer" };
        foreach (var envVar in envVars)
        {
            var path = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path) && !found.Contains(path))
                found.Add(path);
        }

        // Dropbox via info.json
        var dropboxInfo = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Dropbox", "info.json");
        if (File.Exists(dropboxInfo))
        {
            try
            {
                var json = File.ReadAllText(dropboxInfo);
                var match = System.Text.RegularExpressions.Regex.Match(json, @"""path""\s*:\s*""([^""]+)""");
                if (match.Success)
                {
                    var dbPath = match.Groups[1].Value.Replace("\\\\", "\\");
                    if (Directory.Exists(dbPath) && !found.Contains(dbPath))
                        found.Add(dbPath);
                }
            }
            catch { }
        }

        // Common filesystem paths
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var commonPaths = new[]
        {
            Path.Combine(userProfile, "OneDrive"),
            Path.Combine(userProfile, "OneDrive - Personal"),
            Path.Combine(userProfile, "Dropbox"),
            Path.Combine(userProfile, "Google Drive"),
            Path.Combine(userProfile, "GoogleDrive"),
            Path.Combine(userProfile, "My Drive"),
            "D:\\OneDrive", "E:\\OneDrive",
            "D:\\Dropbox", "E:\\Dropbox",
            "D:\\Google Drive", "E:\\Google Drive"
        };

        foreach (var path in commonPaths)
        {
            if (Directory.Exists(path) && !found.Contains(path))
                found.Add(path);
        }

        return found;
    }


    // ═══════════════════════════════════════════════════════════
    //  Pfad-Validierung
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Check if all required paths exist and are accessible.
    /// Returns list of problems (empty = all OK).
    /// </summary>
    public static List<string> ValidatePaths(DeviceSettings device)
    {
        var problems = new List<string>();

        if (string.IsNullOrEmpty(device.BasePath))
            problems.Add("Arbeitsordner ist nicht konfiguriert");
        else if (!Directory.Exists(device.BasePath))
            problems.Add($"Arbeitsordner nicht gefunden: {device.BasePath}");

        if (!string.IsNullOrEmpty(device.ArchivePath) && !Directory.Exists(device.ArchivePath))
            problems.Add($"Archiv-Ordner nicht gefunden: {device.ArchivePath}");

        return problems;
    }
}
