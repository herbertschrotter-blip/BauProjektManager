using System.IO;
using BauProjektManager.Domain.Models;
using Serilog;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// Creates project folder structures on disk.
/// Uses FolderTemplate entries and generates numbered folders (00, 01, 02...).
/// </summary>
public class ProjectFolderService
{
    private readonly AppSettingsService _settingsService;

    public ProjectFolderService(AppSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Creates the project folder with all subfolders from the given template.
    /// The project root folder name is Project.FolderName (e.g. "202512_ÖWG-Dobl-Zwaring").
    /// Returns the absolute path to the project root folder.
    /// </summary>
    /// <param name="project">The project (needs FolderName and Paths.Root).</param>
    /// <param name="folderTemplate">
    /// The folder template to use. If null, uses the default from settings.json.
    /// This allows per-project customization from the dialog.
    /// </param>
    /// <returns>Absolute path to the created project root folder.</returns>
    public string CreateProjectFolders(Project project, List<FolderTemplateEntry>? folderTemplate = null)
    {
        var settings = _settingsService.Load();

        if (string.IsNullOrEmpty(settings.BasePath))
        {
            throw new InvalidOperationException(
                "Arbeitsordner (BasePath) ist nicht konfiguriert. Bitte zuerst Ersteinrichtung durchführen.");
        }

        // Use provided template or fall back to settings default
        var template = folderTemplate ?? settings.FolderTemplate;

        // Build project root path: BasePath / FolderName
        var projectRoot = Path.Combine(settings.BasePath, project.FolderName);

        if (Directory.Exists(projectRoot))
        {
            Log.Warning("Project folder already exists: {Path}", projectRoot);
            // Don't throw — just create missing subfolders
        }
        else
        {
            Directory.CreateDirectory(projectRoot);
            Log.Information("Project folder created: {Path}", projectRoot);
        }

        // Create numbered subfolders from template
        for (int i = 0; i < template.Count; i++)
        {
            var entry = template[i];
            var numberedName = entry.GetNumberedName(i);
            var subPath = Path.Combine(projectRoot, numberedName);

            if (!Directory.Exists(subPath))
            {
                Directory.CreateDirectory(subPath);
                Log.Information("  Subfolder created: {Name}", numberedName);
            }

            // Create _Eingang subfolder if configured (for PlanManager import)
            if (entry.HasInbox)
            {
                var inboxPath = Path.Combine(subPath, "_Eingang");
                if (!Directory.Exists(inboxPath))
                {
                    Directory.CreateDirectory(inboxPath);
                    Log.Information("  Inbox created: {Name}/_Eingang", numberedName);
                }
            }
        }

        Log.Information("Project folder structure complete: {Count} folders in {Root}",
            template.Count, projectRoot);

        return projectRoot;
    }

    /// <summary>
    /// Generates a preview of what folders would be created.
    /// Useful for the project creation dialog to show the user.
    /// </summary>
    public static List<string> PreviewFolderNames(List<FolderTemplateEntry> template)
    {
        var result = new List<string>();
        for (int i = 0; i < template.Count; i++)
        {
            var entry = template[i];
            var name = entry.GetNumberedName(i);
            result.Add(name);
            if (entry.HasInbox)
            {
                result.Add($"  └── _Eingang");
            }
        }
        return result;
    }
}
