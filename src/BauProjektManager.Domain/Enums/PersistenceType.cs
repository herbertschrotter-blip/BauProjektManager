namespace BauProjektManager.Domain.Enums;

/// <summary>
/// Typ einer persistierten Datei oder Datenbank.
/// Wird in DevTools zur gruppierten Anzeige im Persistenz-Inventar verwendet.
/// </summary>
public enum PersistenceType
{
    Database,      // SQLite (bpm.db, planmanager.db)
    Config,        // JSON (device-settings, shared-config, manifest, project, profiles)
    Log,           // Serilog (BPM_*.log)
    ProjectData,   // Sonstige Projekt-Files
    Cache,         // Cache-Files (z.B. registry.json, pattern-templates.json)
    Other
}
