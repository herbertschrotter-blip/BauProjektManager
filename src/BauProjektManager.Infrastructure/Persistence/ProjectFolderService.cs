using System.IO;
using BauProjektManager.Domain.Models;
using Serilog;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// Creates project folder structures on disk.
/// Uses FolderTemplate entries and generates numbered folders (00, 01, 02...).
/// Supports subfolders with optional prefix numbering.
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
    /// Returns the absolute path to the project root folder.
    /// </summary>
    public string CreateProjectFolders(Project project, List<FolderTemplateEntry>? folderTemplate = null)
    {
        var settings = _settingsService.Load();

        if (string.IsNullOrEmpty(settings.BasePath))
        {
            throw new InvalidOperationException(
                "Arbeitsordner (BasePath) ist nicht konfiguriert. Bitte zuerst Ersteinrichtung durchführen.");
        }

        var template = folderTemplate ?? settings.FolderTemplate;
        var projectRoot = Path.Combine(settings.BasePath, project.FolderName);

        Log.Debug("Creating folder structure for project {ProjectId} at {Path}", project.Id, projectRoot);

        if (Directory.Exists(projectRoot))
        {
            Log.Warning("Project folder already exists: {Path}", projectRoot);
        }
        else
        {
            Directory.CreateDirectory(projectRoot);
            Log.Information("Project folder created: {Path}", projectRoot);
        }

        // Create numbered main folders
        for (int i = 0; i < template.Count; i++)
        {
            var entry = template[i];
            var numberedName = entry.GetNumberedName(i);
            var subPath = Path.Combine(projectRoot, numberedName);

            if (!Directory.Exists(subPath))
            {
                Directory.CreateDirectory(subPath);
                Log.Information("  Folder created: {Name}", numberedName);
            }

            // Create _Eingang subfolder if configured
            if (entry.HasInbox)
            {
                var inboxPath = Path.Combine(subPath, "_Eingang");
                if (!Directory.Exists(inboxPath))
                {
                    Directory.CreateDirectory(inboxPath);
                    Log.Information("  Inbox created: {Name}/_Eingang", numberedName);
                }
            }

            // Create subfolders
            int subPosition = 0;
            foreach (var sub in entry.SubFolders)
            {
                var subName = sub.GetDisplayName(subPosition);
                var subFolderPath = Path.Combine(subPath, subName);

                if (!Directory.Exists(subFolderPath))
                {
                    Directory.CreateDirectory(subFolderPath);
                    Log.Information("    Subfolder created: {Parent}/{Name}", numberedName, subName);
                    Log.Debug("Created subfolder {Folder}", subName);
                }

                // Only increment position for prefixed subfolders
                if (sub.HasPrefix)
                    subPosition++;
            }
        }

        Log.Information("Project folder structure complete: {Count} folders in {Root}",
            template.Count, projectRoot);

        return projectRoot;
    }

    /// <summary>
    /// Syncs new folders to an existing project root.
    /// Idempotent: matched bestehende Ordner per Name (ohne Prefix), legt nur fehlende
    /// Template-Einträge an. Bestehende Ordner-Prefixes werden NICHT umnummeriert.
    /// </summary>
    public void SyncNewFolders(Project project, List<FolderTemplateEntry> template)
    {
        var root = project.Paths?.Root;
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
        {
            Log.Warning("SyncNewFolders skipped — root path does not exist: {Path}", root);
            return;
        }

        Log.Debug("Syncing new folders for project {Id} at {Path}", project.Id, root);

        // Existierende Hauptordner einmalig sammeln (Name ohne Prefix als Match-Key)
        var existingMains = Directory.GetDirectories(root)
            .Select(p => new DirectoryInfo(p).Name)
            .ToList();

        int mainPos = 0;
        foreach (var entry in template)
        {
            // Match per Namens-Vergleich (ohne Prefix), nicht per Prefix-Position.
            // So werden vorhandene Ordner mit alten Prefixes wiedergefunden, statt
            // unter neuer Prefix-Nummer doppelt anzulegen (BPM-094).
            var existingName = existingMains.FirstOrDefault(n =>
                StripFolderPrefix(n).Equals(entry.Name, StringComparison.OrdinalIgnoreCase));

            string mainPath;
            if (existingName is not null)
            {
                mainPath = Path.Combine(root, existingName);
            }
            else
            {
                var newName = $"{mainPos:D2} {entry.Name}";
                mainPath = Path.Combine(root, newName);
                Directory.CreateDirectory(mainPath);
                Log.Debug("Created main folder: {Path}", mainPath);
            }

            if (entry.HasInbox)
            {
                var inboxPath = Path.Combine(mainPath, "_Eingang");
                if (!Directory.Exists(inboxPath))
                {
                    Directory.CreateDirectory(inboxPath);
                    Log.Debug("Created inbox: {Path}", inboxPath);
                }
            }

            SyncSubFolders(mainPath, entry.SubFolders);
            mainPos++;
        }
    }

    private void SyncSubFolders(string parentPath, List<SubFolderEntry> subs)
    {
        if (!Directory.Exists(parentPath))
            return;

        var existingSubs = Directory.GetDirectories(parentPath)
            .Select(p => new DirectoryInfo(p).Name)
            .ToList();

        int subPos = 0;
        foreach (var sub in subs)
        {
            // Match per Name (ohne Prefix) — analog zu SyncNewFolders.
            var existingName = existingSubs.FirstOrDefault(n =>
                StripFolderPrefix(n).Equals(sub.Name, StringComparison.OrdinalIgnoreCase));

            string subPath;
            if (existingName is not null)
            {
                subPath = Path.Combine(parentPath, existingName);
            }
            else
            {
                var newName = sub.HasPrefix ? $"{subPos:D2} {sub.Name}" : sub.Name;
                subPath = Path.Combine(parentPath, newName);
                Directory.CreateDirectory(subPath);
                Log.Debug("Created subfolder: {Path}", subPath);
            }

            if (sub.SubFolders.Count > 0)
                SyncSubFolders(subPath, sub.SubFolders);

            if (sub.HasPrefix) subPos++;
        }
    }

    /// <summary>
    /// Entfernt Prefix-Pattern "NN " (zwei Ziffern + Leerzeichen) vom Anfang eines
    /// Ordnernamens für Name-basiertes Matching beim Sync. "01 Sonstiges" -> "Sonstiges".
    /// "Baustelleneinrichtung" (kein Prefix) -> "Baustelleneinrichtung".
    /// ".bpm" -> ".bpm".
    /// </summary>
    private static string StripFolderPrefix(string folderName)
    {
        if (folderName.Length >= 3
            && char.IsDigit(folderName[0])
            && char.IsDigit(folderName[1])
            && folderName[2] == ' ')
        {
            return folderName.Substring(3);
        }
        return folderName;
    }

    /// <summary>
    /// Generates a preview of what folders would be created.
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
                result.Add($"  └── _Eingang");

            int subPos = 0;
            foreach (var sub in entry.SubFolders)
            {
                result.Add($"  └── {sub.GetDisplayName(subPos)}");
                if (sub.HasPrefix) subPos++;
            }
        }
        return result;
    }
}
