using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Manages RecognitionProfiles per project.
/// Profiles are stored as individual JSON files in .bpm/profiles/ (ADR-046).
/// </summary>
public class ProfileManager
{
    private readonly IIdGenerator _idGenerator;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public ProfileManager(IIdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
    }

    /// <summary>
    /// Returns the .bpm/profiles/ directory path for a project.
    /// Creates the directory if it does not exist.
    /// </summary>
    private static string GetProfilesDirectory(string projectRootPath)
    {
        var dir = Path.Combine(projectRootPath, ".bpm", "profiles");
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Loads all profiles for a project from .bpm/profiles/*.json.
    /// </summary>
    public List<RecognitionProfile> LoadAll(string projectRootPath)
    {
        var dir = GetProfilesDirectory(projectRootPath);
        var profiles = new List<RecognitionProfile>();

        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var profile = JsonSerializer.Deserialize<RecognitionProfile>(json, JsonOptions);
                if (profile is not null)
                    profiles.Add(profile);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Profil konnte nicht geladen werden: {File}", file);
            }
        }

        Log.Information("ProfileManager: {Count} Profile geladen aus {Path}",
            profiles.Count, dir);
        return profiles.OrderBy(p => p.DocumentTypeName).ToList();
    }

    /// <summary>
    /// Loads a single profile by ID.
    /// </summary>
    public RecognitionProfile? LoadById(string projectRootPath, string profileId)
    {
        var filePath = Path.Combine(GetProfilesDirectory(projectRootPath),
            $"{profileId}.json");

        if (!File.Exists(filePath))
        {
            Log.Warning("Profil nicht gefunden: {Id}", profileId);
            return null;
        }

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<RecognitionProfile>(json, JsonOptions);
    }

    /// <summary>
    /// Saves a profile to .bpm/profiles/{id}.json.
    /// Generates a new ULID if the profile has no ID yet.
    /// </summary>
    public void Save(string projectRootPath, RecognitionProfile profile)
    {
        if (string.IsNullOrEmpty(profile.Id))
            profile.Id = _idGenerator.NewId();

        var now = DateTime.UtcNow.ToString("o");
        if (string.IsNullOrEmpty(profile.CreatedAt))
            profile.CreatedAt = now;
        profile.UpdatedAt = now;

        var dir = GetProfilesDirectory(projectRootPath);
        var filePath = Path.Combine(dir, $"{profile.Id}.json");

        // Atomic write: temp file → replace
        var tempPath = filePath + ".tmp";
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, filePath, overwrite: true);

        Log.Information("Profil gespeichert: {Name} ({Id}) → {Path}",
            profile.DocumentTypeName, profile.Id, filePath);
    }

    /// <summary>
    /// Deletes a profile by ID.
    /// </summary>
    public bool Delete(string projectRootPath, string profileId)
    {
        var filePath = Path.Combine(GetProfilesDirectory(projectRootPath),
            $"{profileId}.json");

        if (!File.Exists(filePath))
        {
            Log.Warning("Profil zum Loeschen nicht gefunden: {Id}", profileId);
            return false;
        }

        File.Delete(filePath);
        Log.Information("Profil geloescht: {Id}", profileId);
        return true;
    }

    /// <summary>
    /// Builds a RecognitionProfile from the current wizard state.
    /// Called by ProfileWizardViewModel.SaveProfile().
    /// </summary>
    public RecognitionProfile BuildFromWizard(
        string documentTypeName,
        string targetFolder,
        IndexSourceType indexSource,
        bool indexModeOptional,
        bool indexCaseInsensitive,
        List<FileNameSegment> segments,
        List<string> delimiters,
        List<string> folderHierarchy,
        List<RecognitionRule> recognition,
        int recognitionPriority,
        string? existingProfileId = null)
    {
        var profileSegments = segments
            .Where(s => s.FieldType is not null)
            .Select(s => new ProfileSegment
            {
                Position = s.Position,
                FieldType = s.FieldType == FieldType.Custom
                    ? s.CustomFieldName ?? "custom"
                    : s.FieldType.ToString()!,
                Label = s.DisplayName,
                Required = s.FieldType == FieldType.PlanNumber
            })
            .ToList();

        // Build identityFields from segments that define document identity
        var identityFields = new List<string> { "documentType" };
        foreach (var seg in segments.Where(s => s.FieldType is not null))
        {
            if (seg.FieldType is FieldType.PlanNumber or FieldType.Haus
                or FieldType.Bauteil or FieldType.Bauabschnitt)
                identityFields.Add(seg.FieldType.ToString()!.ToLowerInvariant());
        }

        return new RecognitionProfile
        {
            Id = existingProfileId ?? string.Empty,
            SchemaVersion = 1,
            DocumentTypeName = documentTypeName,
            TargetFolder = targetFolder,
            IndexSource = indexSource,
            IndexMode = indexModeOptional ? "optional" : "required",
            IndexComparison = new IndexComparisonConfig
            {
                Mode = "alphabetic",
                CaseInsensitive = indexCaseInsensitive
            },
            Delimiters = delimiters,
            Segments = profileSegments,
            IdentityFields = identityFields,
            Recognition = recognition,
            RecognitionPriority = recognitionPriority,
            ConflictPolicy = "askUser",
            Grouping = new GroupingConfig { Mode = "baseFileName" },
            FolderHierarchy = folderHierarchy
        };
    }
}
