using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BauProjektManager.Domain.Models;
using Serilog;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// Exports project data from SQLite to registry.json for VBA/Excel consumption.
/// The JSON is flat (no nested objects) for VBA compatibility.
/// Written atomically (temp file → rename) to prevent corruption.
/// </summary>
public class RegistryJsonExporter
{
    private readonly string _registryPath;

    /// <summary>
    /// Creates exporter. Registry path should be on OneDrive for sync.
    /// Default: OneDrive/02Arbeit/.AppData/BauProjektManager/registry.json
    /// </summary>
    public RegistryJsonExporter(string registryPath)
    {
        _registryPath = registryPath;
    }

    /// <summary>
    /// Exports all projects to registry.json (atomic write).
    /// </summary>
    public void Export(List<Project> projects)
    {
        Log.Debug("Exporting registry to {Path}", _registryPath);

        var registry = new RegistryRoot
        {
            RegistryVersion = "1.0",
            GeneratedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            PlanTypes = new List<string>
            {
                "Polierplan", "Schalungsplan", "Bewehrungsplan", "Elektroplan",
                "HKLS-Plan", "Detailplan", "Architekturplan", "Lageplan",
                "Grundrissplan", "Schnittplan"
            },
            CustomPlanTypes = new List<string>(),
            Projects = projects.Select(MapToFlat).ToList()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(registry, options);

        // Atomic write: temp file → rename
        var directory = Path.GetDirectoryName(_registryPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = _registryPath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _registryPath, overwrite: true);

        Log.Information("Registry exported: {Count} projects → {Path}", projects.Count, _registryPath);
        Log.Debug("Registry exported — {Count} projects", projects.Count);
    }

    /// <summary>
    /// Maps a Project to a flat structure for VBA compatibility.
    /// No nested objects — VBA JSON parsers are simple.
    /// </summary>
    private static FlatProject MapToFlat(Project p)
    {
        // BuildingParts als Pipe-String: "BT-A:Stiege 1+2:Wohnanlage|GEW:Gewerbe:Gewerbe"
        var buildingsStr = string.Join("|",
            p.BuildingParts.Select(bp =>
                $"{bp.ShortName}:{bp.Description}:{bp.BuildingType}"));

        return new FlatProject
        {
            Id = p.Id,
            ProjectNumber = p.ProjectNumber,
            Name = p.Name,
            FullName = p.FullName,
            Status = p.Status.ToString().ToLower(),
            // Client
            ClientCompany = p.Client.Company,
            ClientContact = p.Client.ContactPerson,
            ClientPhone = p.Client.Phone,
            ClientEmail = p.Client.Email,
            // Address (formatted for backward compatibility)
            Address = p.Location.FormattedAddress,
            Street = p.Location.Street,
            HouseNumber = p.Location.HouseNumber,
            PostalCode = p.Location.PostalCode,
            City = p.Location.City,
            // Verwaltung
            Municipality = p.Location.Municipality,
            District = p.Location.District,
            State = p.Location.State,
            // Koordinaten
            CoordinateSystem = p.Location.CoordinateSystem,
            CoordinateEast = p.Location.CoordinateEast,
            CoordinateNorth = p.Location.CoordinateNorth,
            // Grundstück
            CadastralKg = p.Location.CadastralKg,
            CadastralKgName = p.Location.CadastralKgName,
            CadastralGst = p.Location.CadastralGst,
            // Timeline
            ProjectStart = p.Timeline.ProjectStart?.ToString("yyyy-MM-dd"),
            ConstructionStart = p.Timeline.ConstructionStart?.ToString("yyyy-MM-dd"),
            PlannedEnd = p.Timeline.PlannedEnd?.ToString("yyyy-MM-dd"),
            ActualEnd = p.Timeline.ActualEnd?.ToString("yyyy-MM-dd"),
            // Paths
            RootPath = p.Paths.Root,
            PlansPath = p.Paths.Plans,
            InboxPath = p.Paths.Inbox,
            PhotosPath = p.Paths.Photos,
            DocumentsPath = p.Paths.Documents,
            ProtocolsPath = p.Paths.Protocols,
            InvoicesPath = p.Paths.Invoices,
            // Meta
            Buildings = buildingsStr,
            Tags = p.Tags,
            Notes = p.Notes
        };
    }
}

// === JSON Models (flat for VBA) ===

internal class RegistryRoot
{
    public string RegistryVersion { get; set; } = "1.0";
    public string GeneratedAt { get; set; } = "";
    public List<string> PlanTypes { get; set; } = [];
    public List<string> CustomPlanTypes { get; set; } = [];
    public List<FlatProject> Projects { get; set; } = [];
}

internal class FlatProject
{
    public string Id { get; set; } = "";
    public string ProjectNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Status { get; set; } = "";
    // Client
    public string ClientCompany { get; set; } = "";
    public string ClientContact { get; set; } = "";
    public string ClientPhone { get; set; } = "";
    public string ClientEmail { get; set; } = "";
    // Address
    public string Address { get; set; } = "";
    public string Street { get; set; } = "";
    public string HouseNumber { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    // Verwaltung
    public string Municipality { get; set; } = "";
    public string District { get; set; } = "";
    public string State { get; set; } = "";
    // Koordinaten
    public string CoordinateSystem { get; set; } = "";
    public double CoordinateEast { get; set; }
    public double CoordinateNorth { get; set; }
    // Grundstück
    public string CadastralKg { get; set; } = "";
    public string CadastralKgName { get; set; } = "";
    public string CadastralGst { get; set; } = "";
    // Timeline
    public string? ProjectStart { get; set; }
    public string? ConstructionStart { get; set; }
    public string? PlannedEnd { get; set; }
    public string? ActualEnd { get; set; }
    // Paths
    public string RootPath { get; set; } = "";
    public string PlansPath { get; set; } = "";
    public string InboxPath { get; set; } = "";
    public string PhotosPath { get; set; } = "";
    public string DocumentsPath { get; set; } = "";
    public string ProtocolsPath { get; set; } = "";
    public string InvoicesPath { get; set; } = "";
    // Meta
    public string Buildings { get; set; } = "";
    public string Tags { get; set; } = "";
    public string Notes { get; set; } = "";
}
