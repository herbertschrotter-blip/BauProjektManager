using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Enums;
using Serilog;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// Liest und schreibt .bpm-manifest Dateien in Projektordnern.
/// Das Manifest dient als Ausweis (Ordner-Wiedererkennung) und als
/// portabler Projekt-Snapshot für Import/Übergabe/Backup.
/// Hidden + ReadOnly Attribute schützen vor versehentlichem Löschen.
/// </summary>
public class BpmManifestService
{
    private const string ManifestFileName = ".bpm-manifest";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // === Schreiben ===

    /// <summary>
    /// Schreibt das Manifest in den Projektordner.
    /// Atomic Write (temp → rename), setzt Hidden + ReadOnly.
    /// </summary>
    public void WriteManifest(Project project, string projectRootPath)
    {
        if (string.IsNullOrEmpty(projectRootPath) || !Directory.Exists(projectRootPath))
        {
            Log.Warning("Cannot write manifest: directory does not exist {Path}", projectRootPath);
            return;
        }

        var manifest = ProjectToManifest(project);
        var manifestPath = Path.Combine(projectRootPath, ManifestFileName);

        try
        {
            RemoveReadOnly(manifestPath);

            var tempPath = manifestPath + ".tmp";
            var json = JsonSerializer.Serialize(manifest, JsonOptions);
            File.WriteAllText(tempPath, json);

            if (File.Exists(manifestPath))
            {
                File.Delete(manifestPath);
            }

            File.Move(tempPath, manifestPath);
            File.SetAttributes(manifestPath, FileAttributes.Hidden | FileAttributes.ReadOnly);

            Log.Information("Manifest written: {Path}", manifestPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to write manifest to {Path}", manifestPath);
        }
    }

    // === Lesen ===

    /// <summary>
    /// Liest ein Manifest aus dem Projektordner.
    /// Gibt null zurück wenn kein Manifest vorhanden oder nicht lesbar.
    /// </summary>
    public BpmManifest? ReadManifest(string projectRootPath)
    {
        var manifestPath = Path.Combine(projectRootPath, ManifestFileName);

        if (!File.Exists(manifestPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<BpmManifest>(json, JsonOptions);
            Log.Debug("Manifest read from {Path}", manifestPath);
            return manifest;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to read manifest from {Path}", manifestPath);
            return null;
        }
    }

    /// <summary>
    /// Prüft ob ein Manifest im Ordner existiert.
    /// </summary>
    public bool HasManifest(string projectRootPath)
    {
        return File.Exists(Path.Combine(projectRootPath, ManifestFileName));
    }

    // === Import: Manifest → Project ===

    /// <summary>
    /// Erstellt ein Project aus einem gelesenen Manifest.
    /// ID bleibt leer — wird beim Speichern in der DB vergeben.
    /// </summary>
    public Project ManifestToProject(BpmManifest manifest, string projectRootPath)
    {
        var project = new Project
        {
            Id = string.Empty,
            ProjectNumber = manifest.ProjectNumber,
            Name = manifest.Name,
            FullName = manifest.FullName,
            Status = Enum.TryParse<ProjectStatus>(manifest.Status, out var status)
                ? status
                : ProjectStatus.Active,
            ProjectType = manifest.ProjectType,
            Tags = manifest.Tags,
            Notes = manifest.Notes,
            Client = new Client
            {
                Company = manifest.Client.Company,
                ContactPerson = manifest.Client.ContactPerson,
                Phone = manifest.Client.Phone,
                Email = manifest.Client.Email,
                Notes = manifest.Client.Notes
            },
            Location = new ProjectLocation
            {
                Street = manifest.Location.Street,
                HouseNumber = manifest.Location.HouseNumber,
                PostalCode = manifest.Location.PostalCode,
                City = manifest.Location.City,
                Municipality = manifest.Location.Municipality,
                District = manifest.Location.District,
                State = manifest.Location.State,
                CoordinateSystem = manifest.Location.CoordinateSystem,
                CoordinateEast = manifest.Location.CoordinateEast,
                CoordinateNorth = manifest.Location.CoordinateNorth,
                CadastralKg = manifest.Location.CadastralKg,
                CadastralKgName = manifest.Location.CadastralKgName,
                CadastralGst = manifest.Location.CadastralGst
            },
            Timeline = new ProjectTimeline
            {
                ProjectStart = ParseDate(manifest.Timeline.ProjectStart),
                ConstructionStart = ParseDate(manifest.Timeline.ConstructionStart),
                PlannedEnd = ParseDate(manifest.Timeline.PlannedEnd),
                ActualEnd = ParseDate(manifest.Timeline.ActualEnd)
            },
            Paths = new ProjectPaths
            {
                Root = projectRootPath,
                Plans = manifest.Paths.Plans,
                Inbox = manifest.Paths.Inbox,
                Photos = manifest.Paths.Photos,
                Documents = manifest.Paths.Documents,
                Protocols = manifest.Paths.Protocols,
                Invoices = manifest.Paths.Invoices
            }
        };

        foreach (var mp in manifest.BuildingParts)
        {
            var part = new BuildingPart
            {
                Id = string.Empty,
                ShortName = mp.ShortName,
                Description = mp.Description,
                BuildingType = mp.BuildingType,
                ZeroLevelAbsolute = mp.ZeroLevelAbsolute,
                SortOrder = mp.SortOrder
            };

            foreach (var ml in mp.Levels)
            {
                part.Levels.Add(new BuildingLevel
                {
                    Id = string.Empty,
                    Prefix = ml.Prefix,
                    Name = ml.Name,
                    Description = ml.Description,
                    Rdok = ml.Rdok,
                    Fbok = ml.Fbok,
                    Rduk = ml.Rduk,
                    SortOrder = ml.SortOrder
                });
            }

            project.BuildingParts.Add(part);
        }

        foreach (var mpart in manifest.Participants)
        {
            project.Participants.Add(new ProjectParticipant
            {
                Id = string.Empty,
                Role = mpart.Role,
                Company = mpart.Company,
                ContactPerson = mpart.ContactPerson,
                Phone = mpart.Phone,
                Email = mpart.Email,
                SortOrder = mpart.SortOrder
            });
        }

        foreach (var ml in manifest.Links)
        {
            project.Links.Add(new ProjectLink
            {
                Id = string.Empty,
                Name = ml.Name,
                Url = ml.Url,
                LinkType = ml.LinkType,
                SortOrder = ml.SortOrder
            });
        }

        return project;
    }

    // === Import: Ordner scannen (ohne Manifest) ===

    /// <summary>
    /// Scannt einen bestehenden Projektordner und erstellt ein minimales Project.
    /// Erkennt Projektnummer + Kurzname aus dem Ordnernamen (Format: YYYYMM_Name).
    /// Erkennt bekannte Unterordner (Pläne, Fotos, Dokumente etc.).
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

        // Projektnummer + Name aus Ordnername parsen
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

        // Unterordner scannen und bekannte Pfade zuweisen
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

    /// <summary>
    /// Gibt die Anzahl erkannter Hauptordner und Unterordner zurück.
    /// </summary>
    public (int mainFolders, int subFolders) CountFolders(string folderPath)
    {
        var dirInfo = new DirectoryInfo(folderPath);
        if (!dirInfo.Exists) return (0, 0);

        var mainDirs = dirInfo.GetDirectories()
            .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden))
            .ToList();

        int subCount = 0;
        foreach (var main in mainDirs)
        {
            subCount += main.GetDirectories()
                .Count(d => !d.Attributes.HasFlag(FileAttributes.Hidden));
        }

        return (mainDirs.Count, subCount);
    }

    // === Hilfsmethoden ===

    private BpmManifest ProjectToManifest(Project project)
    {
        var manifest = new BpmManifest
        {
            UpdatedAtUtc = DateTime.UtcNow,
            CreatedByMachine = Environment.MachineName,
            ProjectNumber = project.ProjectNumber,
            Name = project.Name,
            FullName = project.FullName,
            Status = project.Status.ToString(),
            ProjectType = project.ProjectType,
            Tags = project.Tags,
            Notes = project.Notes,
            Client = new ManifestClient
            {
                Company = project.Client.Company,
                ContactPerson = project.Client.ContactPerson,
                Phone = project.Client.Phone,
                Email = project.Client.Email,
                Notes = project.Client.Notes
            },
            Location = new ManifestLocation
            {
                Street = project.Location.Street,
                HouseNumber = project.Location.HouseNumber,
                PostalCode = project.Location.PostalCode,
                City = project.Location.City,
                Municipality = project.Location.Municipality,
                District = project.Location.District,
                State = project.Location.State,
                CoordinateSystem = project.Location.CoordinateSystem,
                CoordinateEast = project.Location.CoordinateEast,
                CoordinateNorth = project.Location.CoordinateNorth,
                CadastralKg = project.Location.CadastralKg,
                CadastralKgName = project.Location.CadastralKgName,
                CadastralGst = project.Location.CadastralGst
            },
            Timeline = new ManifestTimeline
            {
                ProjectStart = FormatDate(project.Timeline.ProjectStart),
                ConstructionStart = FormatDate(project.Timeline.ConstructionStart),
                PlannedEnd = FormatDate(project.Timeline.PlannedEnd),
                ActualEnd = FormatDate(project.Timeline.ActualEnd)
            },
            Paths = new ManifestPaths
            {
                Plans = project.Paths.Plans,
                Inbox = project.Paths.Inbox,
                Photos = project.Paths.Photos,
                Documents = project.Paths.Documents,
                Protocols = project.Paths.Protocols,
                Invoices = project.Paths.Invoices
            }
        };

        foreach (var part in project.BuildingParts)
        {
            var mp = new ManifestBuildingPart
            {
                ShortName = part.ShortName,
                Description = part.Description,
                BuildingType = part.BuildingType,
                ZeroLevelAbsolute = part.ZeroLevelAbsolute,
                SortOrder = part.SortOrder
            };

            foreach (var level in part.Levels)
            {
                mp.Levels.Add(new ManifestBuildingLevel
                {
                    Prefix = level.Prefix,
                    Name = level.Name,
                    Description = level.Description,
                    Rdok = level.Rdok,
                    Fbok = level.Fbok,
                    Rduk = level.Rduk,
                    SortOrder = level.SortOrder
                });
            }

            manifest.BuildingParts.Add(mp);
        }

        foreach (var p in project.Participants)
        {
            manifest.Participants.Add(new ManifestParticipant
            {
                Role = p.Role,
                Company = p.Company,
                ContactPerson = p.ContactPerson,
                Phone = p.Phone,
                Email = p.Email,
                SortOrder = p.SortOrder
            });
        }

        foreach (var link in project.Links)
        {
            manifest.Links.Add(new ManifestLink
            {
                Name = link.Name,
                Url = link.Url,
                LinkType = link.LinkType,
                SortOrder = link.SortOrder
            });
        }

        return manifest;
    }

    private static void RemoveReadOnly(string path)
    {
        if (!File.Exists(path)) return;

        var attrs = File.GetAttributes(path);
        if (attrs.HasFlag(FileAttributes.ReadOnly))
        {
            File.SetAttributes(path, attrs & ~FileAttributes.ReadOnly);
        }
    }

    private static void ScanForInbox(DirectoryInfo plansDir, Project project)
    {
        var inbox = plansDir.GetDirectories()
            .FirstOrDefault(d => d.Name.StartsWith("_Eingang", StringComparison.OrdinalIgnoreCase));

        if (inbox is not null)
        {
            project.Paths.Inbox = Path.Combine(plansDir.Name, inbox.Name);
        }
    }

    /// <summary>
    /// Entfernt nummerischen Präfix: "01 Fotos" → "Fotos", "00 Pläne" → "Pläne".
    /// </summary>
    private static string StripPrefix(string name)
    {
        var match = Regex.Match(name, @"^\d+\s+(.+)$");
        return match.Success ? match.Groups[1].Value : name;
    }

    private static string? FormatDate(DateTime? date)
    {
        return date?.ToString("yyyy-MM-dd");
    }

    private static DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) return null;
        return DateTime.TryParse(dateStr, out var date) ? date : null;
    }
}
