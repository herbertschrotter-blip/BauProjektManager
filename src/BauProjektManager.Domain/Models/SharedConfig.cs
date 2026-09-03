using BauProjektManager.Domain.Enums.PlanManager;
namespace BauProjektManager.Domain.Models;

/// <summary>
/// Geteilte Konfiguration — gespeichert in shared-config.json
/// unter CloudStorage/.AppData/BauProjektManager/ (synct zwischen Geräten).
/// Enthält Listen, Templates und Rollen die auf allen Geräten gleich sein sollen.
/// </summary>
public class SharedConfig
{
    public string SchemaVersion { get; set; } = "1.1";

    /// <summary>
    /// Stabile Workspace-ID (GUID). Identifiziert den gemeinsamen Datenbestand.
    /// Wird beim ersten Erstellen der shared-config generiert.
    /// </summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>
    /// Revisionsnummer. Wird bei jedem Speichern inkrementiert.
    /// Basis für Optimistic Concurrency bei Multi-Device-Zugriff.
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// Zeitpunkt der letzten Änderung (UTC).
    /// </summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// DeviceId des Geräts das zuletzt geschrieben hat.
    /// </summary>
    public string UpdatedByDeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Ordner-Template für neue Projekte.
    /// Reihenfolge bestimmt die Nummerierung (00, 01, 02...).
    /// </summary>
    public List<FolderTemplateEntry> FolderTemplate { get; set; }
        = SharedConfigDefaults.GetDefaultFolderTemplate();

    /// <summary>
    /// Editierbare Liste der Projektarten (Dropdown im Projekt-Dialog).
    /// </summary>
    public List<string> ProjectTypes { get; set; }
        = SharedConfigDefaults.GetDefaultProjectTypes();

    /// <summary>
    /// Editierbare Liste der Bauwerkstypen (Dropdown pro Bauteil).
    /// </summary>
    public List<string> BuildingTypes { get; set; }
        = SharedConfigDefaults.GetDefaultBuildingTypes();

    /// <summary>
    /// Editierbare Geschoss-Bezeichnungen: Kurz (EG) + Lang (Erdgeschoss).
    /// </summary>
    public List<LevelNameEntry> LevelNames { get; set; }
        = SharedConfigDefaults.GetDefaultLevelNames();

    /// <summary>
    /// Editierbare Rollen-Liste für Projekt-Beteiligte.
    /// </summary>
    public List<string> ParticipantRoles { get; set; }
        = SharedConfigDefaults.GetDefaultParticipantRoles();

    /// <summary>
    /// Editierbare Liste der Bauherren-Portal-Typen.
    /// </summary>
    public List<string> PortalTypes { get; set; }
        = SharedConfigDefaults.GetDefaultPortalTypes();
}

/// <summary>
/// Default-Werte für SharedConfig — zentral definiert,
/// wiederverwendbar für Reset und Erstinitialisierung.
/// </summary>
public static class SharedConfigDefaults
{
    public static List<string> GetDefaultProjectTypes() =>
    [
        "Neubau", "Sanierung", "Umbau", "Zubau", "Abbruch", "Sonstiges"
    ];

    public static List<string> GetDefaultBuildingTypes() =>
    [
        "EFH", "MFH", "Wohnanlage", "Gewerbe", "Industrie", "Infrastruktur", "Sonstiges"
    ];

    public static List<string> GetDefaultParticipantRoles() =>
    [
        "Bauherr", "Architekt", "Statiker", "Haustechnik", "Bauphysik",
        "ÖBA", "Vermessung", "Elektro", "HKLS", "Bodengutachter",
        "Brandschutz", "Geotechnik", "Sonstiges"
    ];

    public static List<string> GetDefaultPortalTypes() =>
    [
        "InfoRaum", "PlanRadar", "PlanFred", "Bau-Master", "Dalux"
    ];

    public static List<LevelNameEntry> GetDefaultLevelNames() =>
    [
        new("FU",  "Fundament"),
        new("UG3", "3. Untergeschoss"),
        new("UG2", "2. Untergeschoss"),
        new("UG",  "Untergeschoss"),
        new("EG",  "Erdgeschoss"),
        new("OG1", "1. Obergeschoss"),
        new("OG2", "2. Obergeschoss"),
        new("OG3", "3. Obergeschoss"),
        new("OG4", "4. Obergeschoss"),
        new("OG5", "5. Obergeschoss"),
        new("DG",  "Dachgeschoss"),
    ];

    public static List<FolderTemplateEntry> GetDefaultFolderTemplate() =>
    [
        new("Sonstiges",      hasInbox: false),
        new("Planunterlagen", hasInbox: true, subFolders:
        [
            // ADR-061: Plan-Unterordner = Dokumenttypen (Ring 2 = Bauteile/Geschosse).
            new("Ausschreibungspläne", hasPrefix: true)
            {
                CreatesDocumentType = true, DocumentTypeKey = "ausschreibungsplan",
                DocumentTypeDisplayName = "Ausschreibungsplan", Ring2Source = Ring2Source.BuildingParts,
            },
            new("Polierpläne", hasPrefix: true)
            {
                CreatesDocumentType = true, DocumentTypeKey = "polierplan",
                DocumentTypeDisplayName = "Polierplan", Ring2Source = Ring2Source.BuildingParts,
            },
            new("Statikpläne - Schalung", hasPrefix: true)
            {
                CreatesDocumentType = true, DocumentTypeKey = "schalung",
                DocumentTypeDisplayName = "Schalung", Ring2Source = Ring2Source.BuildingParts,
            },
            new("Statikpläne - Bewehrung", hasPrefix: true)
            {
                CreatesDocumentType = true, DocumentTypeKey = "bewehrung",
                DocumentTypeDisplayName = "Bewehrung", Ring2Source = Ring2Source.BuildingParts,
            },
            new("Fertigteilpläne", hasPrefix: true)
            {
                CreatesDocumentType = true, DocumentTypeKey = "fertigteile",
                DocumentTypeDisplayName = "Fertigteile", Ring2Source = Ring2Source.Categories,
                Categories =
                [
                    new("Wände", hasPrefix: false),
                    new("Decken", hasPrefix: false),
                    new("Stiegen", hasPrefix: false),
                ],
            },
            new("Baustelleneinrichtung", hasPrefix: false)
            {
                CreatesDocumentType = true, DocumentTypeKey = "baustelleneinrichtung",
                DocumentTypeDisplayName = "Baustelleneinrichtung", Ring2Source = Ring2Source.None,
            },
        ]),
        new("Fotos",          hasInbox: false),
        new("Leica",          hasInbox: false, subFolders:
        [
            new("Absteckpläne", hasPrefix: false),
            new("Aufmaß",      hasPrefix: false),
        ]),
        new("DOKA",           hasInbox: false),
        new("LV",             hasInbox: false),
        // ADR-061: Protokolle = eigener Root-Typ (folder_name leer, Ring 2 = Kategorien).
        new("Protokolle",     hasInbox: false)
        {
            CreatesDocumentType = true, DocumentTypeKey = "protokolle",
            DocumentTypeDisplayName = "Protokolle", Ring2Source = Ring2Source.Categories,
            Categories =
            [
                new("Baubesprechung", hasPrefix: false),
                new("Bautagesbericht", hasPrefix: false),
                new("Sicherheit", hasPrefix: false),
                new("Abnahme", hasPrefix: false),
            ],
        },
    ];
}
