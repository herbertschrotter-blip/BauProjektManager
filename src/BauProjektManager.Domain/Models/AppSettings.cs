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

    public List<FolderTemplateEntry> FolderTemplate { get; set; } = GetDefaultFolderTemplate();
    public List<string> ProjectTypes { get; set; } = GetDefaultProjectTypes();
    public List<string> BuildingTypes { get; set; } = GetDefaultBuildingTypes();

    /// <summary>
    /// Editierbare Geschoss-Bezeichnungen: Kurz (EG) + Lang (Erdgeschoss).
    /// Reihenfolge = logische Bau-Reihenfolge von unten nach oben.
    /// </summary>
    public List<LevelNameEntry> LevelNames { get; set; } = GetDefaultLevelNames();

    public static List<string> GetDefaultProjectTypes() =>
        ["Neubau", "Sanierung", "Umbau", "Zubau", "Abbruch", "Sonstiges"];

    public static List<string> GetDefaultBuildingTypes() =>
        ["EFH", "MFH", "Wohnanlage", "Gewerbe", "Industrie", "Infrastruktur", "Sonstiges"];

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
/// Geschoss-Bezeichnung: Kurzname (z.B. "EG") + Langname (z.B. "Erdgeschoss").
/// Beide vom User editierbar.
/// </summary>
public class LevelNameEntry
{
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;

    public LevelNameEntry() { }
    public LevelNameEntry(string shortName, string longName)
    {
        ShortName = shortName;
        LongName = longName;
    }

    public override string ToString() => ShortName;
}

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

public class SubFolderEntry
{
    public string Name { get; set; } = string.Empty;
    public bool HasPrefix { get; set; } = true;

    public SubFolderEntry() { }
    public SubFolderEntry(string name, bool hasPrefix) { Name = name; HasPrefix = hasPrefix; }

    public string GetDisplayName(int position) =>
        HasPrefix ? $"{position:D2} {Name}" : Name;
}
