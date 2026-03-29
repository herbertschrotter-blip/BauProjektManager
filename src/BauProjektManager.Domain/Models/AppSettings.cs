namespace BauProjektManager.Domain.Models;

/// <summary>
/// App-Einstellungen — gespeichert in settings.json (lokal pro Rechner).
/// </summary>
public class AppSettings
{
    public string SchemaVersion { get; set; } = "1.0";
    public string MachineName { get; set; } = string.Empty;
    public string OneDrivePath { get; set; } = string.Empty;
    public string BasePath { get; set; } = string.Empty;
    public string ArchivePath { get; set; } = string.Empty;
    public string ExportPath { get; set; } = string.Empty;
    public bool IsFirstRun { get; set; } = true;
    public DateTime? SetupCompletedAt { get; set; }

    /// <summary>
    /// Ordner-Template für neue Projekte.
    /// </summary>
    public List<FolderTemplateEntry> FolderTemplate { get; set; } = GetDefaultFolderTemplate();

    /// <summary>
    /// Editierbare Liste der Projektarten (Dropdown im Projekt-Dialog).
    /// </summary>
    public List<string> ProjectTypes { get; set; } = GetDefaultProjectTypes();

    /// <summary>
    /// Editierbare Liste der Bauwerkstypen (Dropdown pro Bauteil).
    /// </summary>
    public List<string> BuildingTypes { get; set; } = GetDefaultBuildingTypes();

    /// <summary>
    /// Editierbare Liste der Geschoss-Bezeichnungen (Dropdown im Bauwerk-Tab).
    /// </summary>
    public List<string> LevelNames { get; set; } = GetDefaultLevelNames();

    public static List<string> GetDefaultProjectTypes() =>
    [
        "Neubau", "Sanierung", "Umbau", "Zubau", "Abbruch", "Sonstiges"
    ];

    public static List<string> GetDefaultBuildingTypes() =>
    [
        "EFH", "MFH", "Wohnanlage", "Gewerbe", "Industrie", "Infrastruktur", "Sonstiges"
    ];

    public static List<string> GetDefaultLevelNames() =>
    [
        "UG3", "UG2", "UG", "EG", "OG1", "OG2", "OG3", "OG4", "OG5",
        "DG", "Attika", "Staffel"
    ];

    public static List<FolderTemplateEntry> GetDefaultFolderTemplate() =>
    [
        new("Sonstiges",      hasInbox: false),
        new("Planunterlagen", hasInbox: true, subFolders:
        [
            new("Ausschreibungspläne", hasPrefix: true),
            new("Polierpläne",         hasPrefix: true),
            new("Statikpläne - Schalung",   hasPrefix: true),
            new("Statikpläne - Bewehrung",  hasPrefix: true),
            new("Fertigteilpläne",     hasPrefix: true),
            new("Baustelleneinrichtung", hasPrefix: false),
        ]),
        new("Fotos",          hasInbox: false),
        new("Leica",          hasInbox: false, subFolders:
        [
            new("Absteckpläne", hasPrefix: false),
            new("Aufmaß",      hasPrefix: false),
        ]),
        new("DOKA",           hasInbox: false),
        new("LV",             hasInbox: false),
        new("Protokolle",     hasInbox: false),
    ];
}

/// <summary>
/// Ein Hauptordner im Ordner-Template.
/// </summary>
public class FolderTemplateEntry
{
    public string Name { get; set; } = string.Empty;
    public bool HasInbox { get; set; }
    public List<SubFolderEntry> SubFolders { get; set; } = [];

    public FolderTemplateEntry() { }
    public FolderTemplateEntry(string name, bool hasInbox) { Name = name; HasInbox = hasInbox; }
    public FolderTemplateEntry(string name, bool hasInbox, List<SubFolderEntry> subFolders)
    { Name = name; HasInbox = hasInbox; SubFolders = subFolders; }

    public string GetNumberedName(int position) => $"{position:D2} {Name}";
}

/// <summary>
/// Ein Unterordner innerhalb eines Hauptordners.
/// </summary>
public class SubFolderEntry
{
    public string Name { get; set; } = string.Empty;
    public bool HasPrefix { get; set; } = true;

    public SubFolderEntry() { }
    public SubFolderEntry(string name, bool hasPrefix) { Name = name; HasPrefix = hasPrefix; }

    public string GetDisplayName(int position) =>
        HasPrefix ? $"{position:D2} {Name}" : Name;
}
