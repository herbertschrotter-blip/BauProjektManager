using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BauProjektManager.Domain.Models;
using Serilog;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// Manages device-settings.json (local, per machine) and shared-config.json (cloud, synced).
/// Migrates from legacy settings.json on first run after update.
/// </summary>
public class AppSettingsService
{
    private readonly string _localDir;
    private readonly string _deviceSettingsPath;
    private readonly string _legacySettingsPath;

    private DeviceSettings? _cachedDevice;
    private SharedConfig? _cachedShared;

    // Legacy compatibility: old AppSettings cache for callers not yet migrated
    private AppSettings? _cachedLegacy;

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

        // Migration: alte settings.json → neue Struktur
        if (!File.Exists(_deviceSettingsPath) && File.Exists(_legacySettingsPath))
        {
            Log.Information("Legacy settings.json found — migrating to split format");
            MigrateFromLegacy();
        }

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
                Log.Information("shared-config.json missing, creating from defaults/legacy");
                var shared = TryLoadSharedFromLegacy() ?? new SharedConfig();
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
        Log.Debug("Saving device-settings to {Path}", _deviceSettingsPath);
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

    // ═══════════════════════════════════════════════════════════
    //  Legacy Migration (settings.json → split)
    // ═══════════════════════════════════════════════════════════

    private void MigrateFromLegacy()
    {
        try
        {
            var legacy = LoadLegacySettingsFile();
            if (legacy is null) return;

            // Device-Settings aus Legacy extrahieren
            var device = new DeviceSettings
            {
                SchemaVersion = "1.0",
                MachineName = legacy.MachineName,
                CloudStoragePath = legacy.OneDrivePath,
                BasePath = legacy.BasePath,
                ArchivePath = legacy.ArchivePath,
                ExportPath = legacy.ExportPath,
                IsFirstRun = legacy.IsFirstRun,
                SetupCompletedAt = legacy.SetupCompletedAt
            };
            SaveDevice(device);

            // SharedConfig aus Legacy extrahieren
            var shared = new SharedConfig
            {
                SchemaVersion = "1.0",
                FolderTemplate = legacy.FolderTemplate,
                ProjectTypes = legacy.ProjectTypes,
                BuildingTypes = legacy.BuildingTypes,
                LevelNames = legacy.LevelNames,
                ParticipantRoles = legacy.ParticipantRoles,
                PortalTypes = legacy.PortalTypes
            };

            // SharedConfig nur speichern wenn BasePath bekannt
            if (!string.IsNullOrEmpty(device.BasePath))
            {
                SaveShared(shared, device.BasePath);
            }

            _cachedDevice = device;
            _cachedShared = shared;

            Log.Information("Legacy settings.json migrated to device-settings.json + shared-config.json");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to migrate legacy settings.json");
        }
    }

    private AppSettings? LoadLegacySettingsFile()
    {
        if (!File.Exists(_legacySettingsPath)) return null;
        try
        {
            var json = File.ReadAllText(_legacySettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to read legacy settings.json");
            return null;
        }
    }

    private SharedConfig? TryLoadSharedFromLegacy()
    {
        var legacy = LoadLegacySettingsFile();
        if (legacy is null) return null;

        return new SharedConfig
        {
            FolderTemplate = legacy.FolderTemplate,
            ProjectTypes = legacy.ProjectTypes,
            BuildingTypes = legacy.BuildingTypes,
            LevelNames = legacy.LevelNames,
            ParticipantRoles = legacy.ParticipantRoles,
            PortalTypes = legacy.PortalTypes
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  Legacy Compatibility (für Caller die noch AppSettings nutzen)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Legacy: Load old-style AppSettings. Builds from DeviceSettings + SharedConfig.
    /// Used by callers not yet migrated to the split model.
    /// </summary>
    public AppSettings Load()
    {
        if (_cachedLegacy is not null)
            return _cachedLegacy;

        var device = LoadDevice();
        var shared = !string.IsNullOrEmpty(device.BasePath)
            ? LoadShared(device.BasePath)
            : new SharedConfig();

        _cachedLegacy = new AppSettings
        {
            SchemaVersion = device.SchemaVersion,
            MachineName = device.MachineName,
            OneDrivePath = device.CloudStoragePath,
            BasePath = device.BasePath,
            ArchivePath = device.ArchivePath,
            ExportPath = device.ExportPath,
            IsFirstRun = device.IsFirstRun,
            SetupCompletedAt = device.SetupCompletedAt,
            FolderTemplate = shared.FolderTemplate,
            ProjectTypes = shared.ProjectTypes,
            BuildingTypes = shared.BuildingTypes,
            LevelNames = shared.LevelNames,
            ParticipantRoles = shared.ParticipantRoles,
            PortalTypes = shared.PortalTypes
        };

        return _cachedLegacy;
    }

    /// <summary>
    /// Legacy: Save old-style AppSettings. Splits into DeviceSettings + SharedConfig.
    /// Used by callers not yet migrated to the split model.
    /// </summary>
    public void Save(AppSettings settings)
    {
        // BPM-096: DeviceId und WorkspaceId aus bestehenden DeviceSettings übernehmen.
        // AppSettings hat diese Felder nicht — ohne Preservation würde jeder Save()
        // sie auf "" zurücksetzen und beim nächsten LoadDevice() neu generieren.
        var existing = LoadDevice();

        var device = new DeviceSettings
        {
            SchemaVersion = settings.SchemaVersion,
            DeviceId = existing.DeviceId,
            WorkspaceId = existing.WorkspaceId,
            MachineName = settings.MachineName,
            CloudStoragePath = settings.OneDrivePath,
            BasePath = settings.BasePath,
            ArchivePath = settings.ArchivePath,
            ExportPath = settings.ExportPath,
            IsFirstRun = settings.IsFirstRun,
            SetupCompletedAt = settings.SetupCompletedAt
        };
        SaveDevice(device);

        var shared = new SharedConfig
        {
            FolderTemplate = settings.FolderTemplate,
            ProjectTypes = settings.ProjectTypes,
            BuildingTypes = settings.BuildingTypes,
            LevelNames = settings.LevelNames,
            ParticipantRoles = settings.ParticipantRoles,
            PortalTypes = settings.PortalTypes
        };

        if (!string.IsNullOrEmpty(device.BasePath))
        {
            SaveShared(shared, device.BasePath);
        }

        _cachedLegacy = settings;
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

    /// <summary>
    /// Legacy alias for DetectCloudStoragePath.
    /// </summary>
    public static string? DetectOneDrivePath() => DetectCloudStoragePath();

    // ═══════════════════════════════════════════════════════════
    //  Pfad-Validierung
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Check if all required paths exist and are accessible.
    /// Returns list of problems (empty = all OK).
    /// </summary>
    public static List<string> ValidatePaths(AppSettings settings)
    {
        var problems = new List<string>();

        if (string.IsNullOrEmpty(settings.BasePath))
            problems.Add("Arbeitsordner ist nicht konfiguriert");
        else if (!Directory.Exists(settings.BasePath))
            problems.Add($"Arbeitsordner nicht gefunden: {settings.BasePath}");

        if (!string.IsNullOrEmpty(settings.ArchivePath) && !Directory.Exists(settings.ArchivePath))
            problems.Add($"Archiv-Ordner nicht gefunden: {settings.ArchivePath}");

        return problems;
    }

    /// <summary>
    /// Overload for DeviceSettings validation.
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
