namespace BauProjektManager.Domain.Models;

/// <summary>
/// Schlanker Projekt-Ausweis als .bpm/manifest.json im Projektordner (ADR-046, BPM-046).
/// Enthält NUR Identität + Modul-Flags — keine Stammdaten, keine Personendaten.
/// Der Vollexport liegt getrennt in .bpm/project.json (<see cref="ProjectExport"/>).
/// SchemaVersion 2; Version 1 war der frühere Vollexport unter demselben Dateinamen
/// und wird vom ManifestService beim Zugriff migriert.
/// </summary>
public class ProjectManifest
{
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>ULID des Projekts in der bpm.db; leer, wenn das Manifest aus einer Migration ohne DB-Bezug stammt.</summary>
    public string ProjectId { get; set; } = string.Empty;

    public string ProjectNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Name der BPM-Instanz, die zuletzt geschrieben hat.</summary>
    public string CreatedByMachine { get; set; } = Environment.MachineName;

    /// <summary>Welche Module für dieses Projekt aktiv sind.</summary>
    public ManifestModules Modules { get; set; } = new();
}

/// <summary>Modul-Flags im Projekt-Ausweis. Neue Module ergänzen hier ein Flag (Default false).</summary>
public class ManifestModules
{
    public bool PlanManager { get; set; } = true;
    public bool Foto { get; set; }
    public bool Bautagebuch { get; set; }
}
