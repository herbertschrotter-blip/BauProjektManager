using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Manages the global pattern-templates.json in Cloud .AppData/.
/// Extracts reusable templates from profiles and suggests them for new projects.
/// </summary>
public class PatternTemplateService
{
    private readonly IIdGenerator _idGenerator;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public PatternTemplateService(IIdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
    }

    /// <summary>
    /// Loads all templates from pattern-templates.json.
    /// </summary>
    public List<PatternTemplate> LoadAll(string appDataPath)
    {
        var filePath = GetFilePath(appDataPath);
        if (!File.Exists(filePath))
            return [];

        try
        {
            var json = File.ReadAllText(filePath);
            var templates = JsonSerializer.Deserialize<List<PatternTemplate>>(json, JsonOptions);
            Log.Information("PatternTemplates: {Count} Templates geladen", templates?.Count ?? 0);
            return templates ?? [];
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "pattern-templates.json konnte nicht geladen werden");
            return [];
        }
    }

    /// <summary>
    /// Saves all templates to pattern-templates.json (atomic write).
    /// </summary>
    public void SaveAll(string appDataPath, List<PatternTemplate> templates)
    {
        var filePath = GetFilePath(appDataPath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var tempPath = filePath + ".tmp";
        var json = JsonSerializer.Serialize(templates, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, filePath, overwrite: true);

        Log.Information("PatternTemplates: {Count} Templates gespeichert → {Path}",
            templates.Count, filePath);
    }

    /// <summary>
    /// Extracts a reusable template from a saved profile.
    /// Called after ProfileManager.Save() to update the global library.
    /// </summary>
    public PatternTemplate ExtractFromProfile(RecognitionProfile profile, string projectName)
    {
        return new PatternTemplate
        {
            Id = _idGenerator.NewId(),
            DocumentTypeName = profile.DocumentTypeName,
            TargetFolder = profile.TargetFolder,
            Delimiters = profile.Delimiters,
            Segments = profile.Segments,
            Recognition = profile.Recognition,
            IndexSource = profile.IndexSource,
            IndexMode = profile.IndexMode,
            FolderHierarchy = profile.FolderHierarchy,
            SourceProjectName = projectName,
            CreatedAt = DateTime.UtcNow.ToString("o")
        };
    }

    /// <summary>
    /// Adds or updates a template in the global library.
    /// Updates if same DocumentTypeName already exists, otherwise adds.
    /// </summary>
    public void AddOrUpdate(string appDataPath, PatternTemplate template)
    {
        var templates = LoadAll(appDataPath);
        var existing = templates.FindIndex(t =>
            t.DocumentTypeName.Equals(template.DocumentTypeName,
                StringComparison.OrdinalIgnoreCase));

        if (existing >= 0)
            templates[existing] = template;
        else
            templates.Add(template);

        SaveAll(appDataPath, templates);
    }

    /// <summary>
    /// Returns templates that could be suggested for a new project.
    /// Filters out templates that already exist as profiles in the project.
    /// </summary>
    public List<PatternTemplate> GetSuggestions(
        string appDataPath, List<RecognitionProfile> existingProfiles)
    {
        var templates = LoadAll(appDataPath);
        return templates
            .Where(t => !existingProfiles.Any(p =>
                p.DocumentTypeName.Equals(t.DocumentTypeName,
                    StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static string GetFilePath(string appDataPath)
    {
        return Path.Combine(appDataPath, "pattern-templates.json");
    }
}
