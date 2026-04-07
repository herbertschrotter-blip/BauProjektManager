using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BauProjektManager.Domain.Models;
using Serilog;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// Manages settings.json in %LocalAppData%\BauProjektManager\.
/// Settings are per-machine (different paths on PC vs laptop).
/// </summary>
public class AppSettingsService
{
    private readonly string _settingsPath;
    private AppSettings? _cachedSettings;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AppSettingsService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BauProjektManager");
        if (!Directory.Exists(appData))
        {
            Log.Debug("Creating settings directory {Dir}", appData);
        }
        Directory.CreateDirectory(appData);
        _settingsPath = Path.Combine(appData, "settings.json");
    }

    /// <summary>
    /// Load settings from disk. Returns default settings if file doesn't exist.
    /// </summary>
    public AppSettings Load()
    {
        if (_cachedSettings is not null)
            return _cachedSettings;

        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                Log.Information("Settings loaded from {Path}", _settingsPath);
                Log.Debug("Settings loaded successfully from {Path}", _settingsPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load settings, using defaults");
                _cachedSettings = new AppSettings();
            }
        }
        else
        {
            _cachedSettings = new AppSettings();
            Log.Information("No settings found, first run detected");
            Log.Debug("Settings file not found at {Path} — using defaults", _settingsPath);
        }

        // Always update machine name
        _cachedSettings.MachineName = Environment.MachineName;

        return _cachedSettings;
    }

    /// <summary>
    /// Save settings to disk (atomic write).
    /// </summary>
    public void Save(AppSettings settings)
    {
        Log.Debug("Saving settings to {Path}", _settingsPath);
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var tempPath = _settingsPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _settingsPath, overwrite: true);
            _cachedSettings = settings;
            Log.Information("Settings saved to {Path}", _settingsPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save settings");
            throw;
        }
    }

    /// <summary>
    /// Detect OneDrive path from environment variable.
    /// Returns null if not found.
    /// </summary>
    public static string? DetectOneDrivePath()
    {
        // Try environment variable first
        var oneDrive = Environment.GetEnvironmentVariable("OneDrive");
        if (!string.IsNullOrEmpty(oneDrive) && Directory.Exists(oneDrive))
        {
            Log.Information("OneDrive detected via env: {Path}", oneDrive);
            return oneDrive;
        }

        // Try OneDriveCommercial (for business accounts)
        oneDrive = Environment.GetEnvironmentVariable("OneDriveCommercial");
        if (!string.IsNullOrEmpty(oneDrive) && Directory.Exists(oneDrive))
        {
            Log.Information("OneDrive Business detected via env: {Path}", oneDrive);
            return oneDrive;
        }

        // Try common paths
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var commonPaths = new[]
        {
            Path.Combine(userProfile, "OneDrive"),
            Path.Combine(userProfile, "OneDrive - Personal"),
            "D:\\OneDrive",
            "E:\\OneDrive"
        };

        foreach (var path in commonPaths)
        {
            if (Directory.Exists(path))
            {
                Log.Information("OneDrive detected via common path: {Path}", path);
                return path;
            }
        }

        Log.Warning("OneDrive not detected");
        return null;
    }

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
}
