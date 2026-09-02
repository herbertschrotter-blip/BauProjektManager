using System.IO;
using System.Text.RegularExpressions;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;
using Serilog;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// Import ohne Ausweis: scannt einen bestehenden Projektordner und leitet ein
/// minimales Project ab (Nummer + Name aus dem Ordnernamen, bekannte Unterordner).
/// Vor BPM-046 Teil des BpmManifestService; eigene Klasse, weil Ordner-Scan
/// nichts mit dem .bpm/-Inhalt zu tun hat.
/// </summary>
public class ProjectFolderScanner
{
    /// <summary>
    /// Erkennt Projektnummer + Kurzname aus dem Ordnernamen (Format: YYYYMM_Name)
    /// und bekannte Unterordner (Pläne, Fotos, Dokumente etc.).
    /// </summary>
    public Project ScanFolder(string folderPath)
    {
        var dirInfo = new DirectoryInfo(folderPath);
        var folderName = dirInfo.Name;

        var project = new Project
        {
            Id = string.Empty,
            Status = ProjectStatus.Active,
            Paths = new ProjectPaths { Root = folderPath }
        };

        // Format: "202312_Reininghaus-BA07" oder "202512_ÖWG-Dobl-Zwaring"
        var match = Regex.Match(folderName, @"^(\d{6})_(.+)$");
        if (match.Success)
        {
            project.ProjectNumber = match.Groups[1].Value;
            project.Name = match.Groups[2].Value;

            if (int.TryParse(match.Groups[1].Value[..4], out var year) &&
                int.TryParse(match.Groups[1].Value[4..], out var month) &&
                month >= 1 && month <= 12)
            {
                project.Timeline.ProjectStart = new DateTime(year, month, 1);
            }
        }
        else
        {
            project.Name = folderName;
        }

        var subDirs = dirInfo.GetDirectories()
            .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden))
            .OrderBy(d => d.Name)
            .ToList();

        foreach (var sub in subDirs)
        {
            var nameClean = StripPrefix(sub.Name).ToLowerInvariant();

            if (nameClean.Contains("plan") || nameClean.Contains("plän"))
            {
                project.Paths.Plans = sub.Name;
                ScanForInbox(sub, project);
            }
            else if (nameClean.Contains("foto") || nameClean.Contains("photo") || nameClean.Contains("bild"))
            {
                project.Paths.Photos = sub.Name;
            }
            else if (nameClean.Contains("dokument") || nameClean.Contains("document"))
            {
                project.Paths.Documents = sub.Name;
            }
            else if (nameClean.Contains("protokoll"))
            {
                project.Paths.Protocols = sub.Name;
            }
            else if (nameClean.Contains("rechnung") || nameClean.Contains("invoice"))
            {
                project.Paths.Invoices = sub.Name;
            }
        }

        Log.Information("Folder scanned: {Path} — Number={Number}, Name={Name}, Subdirs={Count}",
            folderPath, project.ProjectNumber, project.Name, subDirs.Count);

        return project;
    }

    /// <summary>Anzahl erkannter Hauptordner und Unterordner (ohne versteckte).</summary>
    public (int mainFolders, int subFolders) CountFolders(string folderPath)
    {
        var dirInfo = new DirectoryInfo(folderPath);
        if (!dirInfo.Exists) return (0, 0);

        var mainDirs = dirInfo.GetDirectories()
            .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden))
            .ToList();

        var subCount = mainDirs.Sum(main => main.GetDirectories()
            .Count(d => !d.Attributes.HasFlag(FileAttributes.Hidden)));

        return (mainDirs.Count, subCount);
    }

    private static void ScanForInbox(DirectoryInfo plansDir, Project project)
    {
        var inbox = plansDir.GetDirectories()
            .FirstOrDefault(d => d.Name.StartsWith("_Eingang", StringComparison.OrdinalIgnoreCase));

        if (inbox is not null)
            project.Paths.Inbox = Path.Combine(plansDir.Name, inbox.Name);
    }

    /// <summary>Entfernt nummerischen Präfix: "01 Fotos" → "Fotos".</summary>
    private static string StripPrefix(string name)
    {
        var match = Regex.Match(name, @"^\d+\s+(.+)$");
        return match.Success ? match.Groups[1].Value : name;
    }
}
