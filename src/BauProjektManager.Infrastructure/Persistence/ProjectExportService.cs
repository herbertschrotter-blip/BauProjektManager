using System.IO;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using Serilog;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// Schreibt und liest den Vollexport .bpm/project.json (ADR-046, BPM-046):
/// alle Projektdaten für Import/Übergabe, eigene DTOs, keine DB-IDs.
/// Wird nur beim Speichern geschrieben, nicht bei jedem Zugriff.
/// </summary>
public class ProjectExportService
{
    private readonly IPersistenceRegistry? _persistenceRegistry;

    public ProjectExportService(IPersistenceRegistry? persistenceRegistry = null)
    {
        _persistenceRegistry = persistenceRegistry;
    }

    // === Schreiben ===

    public void WriteExport(Project project, string projectRootPath)
    {
        if (string.IsNullOrEmpty(projectRootPath) || !Directory.Exists(projectRootPath))
        {
            Log.Warning("Cannot write project export: directory does not exist {Path}", projectRootPath);
            return;
        }

        WriteExport(ProjectToExport(project), projectRootPath);
    }

    /// <summary>Schreibt einen bereits vorhandenen Export (Migrationspfad).</summary>
    public void WriteExport(ProjectExport export, string projectRootPath)
    {
        var exportPath = BpmFolder.ExportPath(projectRootPath);
        try
        {
            BpmFolder.EnsureFolder(projectRootPath);
            BpmFolder.WriteJsonAtomic(exportPath, export);

            _persistenceRegistry?.Register(new PersistenceEntry(
                DisplayName: ".bpm/project.json",
                AbsolutePath: exportPath,
                Type: PersistenceType.ProjectData,
                Scope: PersistenceScope.ProjectLocal,
                Description: "Vollstaendiger Projektexport fuer Import/Uebergabe (ADR-046)"));

            Log.Information("Project export written: {Path}", exportPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to write project export to {Path}", exportPath);
        }
    }

    // === Lesen ===

    /// <summary>Liest project.json; null wenn nicht vorhanden oder nicht lesbar.</summary>
    public ProjectExport? ReadExport(string projectRootPath)
    {
        var path = BpmFolder.ExportPath(projectRootPath);
        return File.Exists(path) ? BpmFolder.ReadJson<ProjectExport>(path) : null;
    }

    // === Import: Export → Project ===

    /// <summary>
    /// Erstellt ein Project aus einem gelesenen Export.
    /// IDs bleiben leer — werden beim Speichern in der DB vergeben.
    /// </summary>
    public Project ExportToProject(ProjectExport export, string projectRootPath)
    {
        var project = new Project
        {
            Id = string.Empty,
            ProjectNumber = export.ProjectNumber,
            Name = export.Name,
            FullName = export.FullName,
            Status = Enum.TryParse<ProjectStatus>(export.Status, out var status)
                ? status
                : ProjectStatus.Active,
            ProjectType = export.ProjectType,
            Tags = export.Tags,
            Notes = export.Notes,
            Client = new Client
            {
                Company = export.Client.Company,
                ContactPerson = export.Client.ContactPerson,
                Phone = export.Client.Phone,
                Email = export.Client.Email,
                Notes = export.Client.Notes
            },
            Location = new ProjectLocation
            {
                Street = export.Location.Street,
                HouseNumber = export.Location.HouseNumber,
                PostalCode = export.Location.PostalCode,
                City = export.Location.City,
                Municipality = export.Location.Municipality,
                District = export.Location.District,
                State = export.Location.State,
                CoordinateSystem = export.Location.CoordinateSystem,
                CoordinateEast = export.Location.CoordinateEast,
                CoordinateNorth = export.Location.CoordinateNorth,
                CadastralKg = export.Location.CadastralKg,
                CadastralKgName = export.Location.CadastralKgName,
                CadastralGst = export.Location.CadastralGst
            },
            Timeline = new ProjectTimeline
            {
                ProjectStart = ParseDate(export.Timeline.ProjectStart),
                ConstructionStart = ParseDate(export.Timeline.ConstructionStart),
                PlannedEnd = ParseDate(export.Timeline.PlannedEnd),
                ActualEnd = ParseDate(export.Timeline.ActualEnd)
            },
            Paths = new ProjectPaths
            {
                Root = projectRootPath,
                Plans = export.Paths.Plans,
                Inbox = export.Paths.Inbox,
                Photos = export.Paths.Photos,
                Documents = export.Paths.Documents,
                Protocols = export.Paths.Protocols,
                Invoices = export.Paths.Invoices
            }
        };

        foreach (var mp in export.BuildingParts)
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

        foreach (var mpart in export.Participants)
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

        foreach (var ml in export.Links)
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

    // === Mapping: Project → Export ===

    private static ProjectExport ProjectToExport(Project project)
    {
        var export = new ProjectExport
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

            export.BuildingParts.Add(mp);
        }

        foreach (var p in project.Participants)
        {
            export.Participants.Add(new ManifestParticipant
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
            export.Links.Add(new ManifestLink
            {
                Name = link.Name,
                Url = link.Url,
                LinkType = link.LinkType,
                SortOrder = link.SortOrder
            });
        }

        return export;
    }

    private static string? FormatDate(DateTime? date) => date?.ToString("yyyy-MM-dd");

    private static DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) return null;
        return DateTime.TryParse(dateStr, out var date) ? date : null;
    }
}
