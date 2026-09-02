namespace BauProjektManager.Domain.Models;

/// <summary>
/// Vollständiger Projektexport als .bpm/project.json im Projektordner (ADR-046, BPM-046).
/// Enthält alle Metadaten eines Projekts für Import, Übergabe und Backup — eigene DTOs,
/// keine DB-IDs. Wird bei jedem Speichern im ProjectEditDialog aktualisiert.
/// Vor BPM-046 lag dieser Inhalt als „BpmManifest" in .bpm/manifest.json (SchemaVersion 1);
/// der Projekt-Ausweis ist seither <see cref="ProjectManifest"/>.
/// </summary>
public class ProjectExport
{
    /// <summary>Schema-Version des Exports (für Vorwärtskompatibilität).</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Zeitpunkt der letzten Aktualisierung (UTC).</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Name der BPM-Instanz, die zuletzt geschrieben hat.</summary>
    public string CreatedByMachine { get; set; } = Environment.MachineName;

    // === Stammdaten ===

    public string ProjectNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string ProjectType { get; set; } = string.Empty;

    // === Auftraggeber ===

    public ManifestClient Client { get; set; } = new();

    // === Adresse + Koordinaten ===

    public ManifestLocation Location { get; set; } = new();

    // === Zeitplan ===

    public ManifestTimeline Timeline { get; set; } = new();

    // === Bauteile mit Geschossen ===

    public List<ManifestBuildingPart> BuildingParts { get; set; } = [];

    // === Beteiligte ===

    public List<ManifestParticipant> Participants { get; set; } = [];

    // === Portale + Links ===

    public List<ManifestLink> Links { get; set; } = [];

    // === Ordnerstruktur (relative Pfade) ===

    public ManifestPaths Paths { get; set; } = new();

    // === Sonstiges ===

    public string Tags { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

// === Export-DTOs (flach, ohne IDs — nur Daten) ===

public class ManifestClient
{
    public string Company { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class ManifestLocation
{
    public string Street { get; set; } = string.Empty;
    public string HouseNumber { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Municipality { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string State { get; set; } = "Steiermark";
    public string CoordinateSystem { get; set; } = "EPSG:31258";
    public double CoordinateEast { get; set; }
    public double CoordinateNorth { get; set; }
    public string CadastralKg { get; set; } = string.Empty;
    public string CadastralKgName { get; set; } = string.Empty;
    public string CadastralGst { get; set; } = string.Empty;
}

public class ManifestTimeline
{
    public string? ProjectStart { get; set; }
    public string? ConstructionStart { get; set; }
    public string? PlannedEnd { get; set; }
    public string? ActualEnd { get; set; }
}

public class ManifestBuildingPart
{
    public string ShortName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BuildingType { get; set; } = string.Empty;
    public double ZeroLevelAbsolute { get; set; }
    public int SortOrder { get; set; }
    public List<ManifestBuildingLevel> Levels { get; set; } = [];
}

public class ManifestBuildingLevel
{
    public int Prefix { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Rdok { get; set; }
    public double Fbok { get; set; }
    public double? Rduk { get; set; }
    public int SortOrder { get; set; }
}

public class ManifestParticipant
{
    public string Role { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class ManifestLink
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string LinkType { get; set; } = "Custom";
    public int SortOrder { get; set; }
}

public class ManifestPaths
{
    public string Plans { get; set; } = "Pläne";
    public string Inbox { get; set; } = @"Pläne\_Eingang";
    public string Photos { get; set; } = "Fotos";
    public string Documents { get; set; } = "Dokumente";
    public string Protocols { get; set; } = "Protokolle";
    public string Invoices { get; set; } = "Rechnungen";
}
