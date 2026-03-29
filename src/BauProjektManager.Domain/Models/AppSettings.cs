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
    /// Reihenfolge bestimmt die Nummerierung (00, 01, 02...).
    /// Änderbar vom User beim Projekt-Anlegen und in den Einstellungen.
    /// </summary>
    public List<FolderTemplateEntry> FolderTemplate { get; set; } = GetDefaultFolderTemplate();

    /// <summary>
    /// Standard-Ordnerstruktur für neue Bauprojekte.
    /// Namen OHNE Nummer — die Nummerierung entsteht automatisch aus der Reihenfolge.
    /// </summary>
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
/// Die Nummer wird NICHT gespeichert — sie entsteht aus der Position in der Liste.
/// Kann Unterordner haben, die optional auch nummeriert werden (HasPrefix).
/// </summary>
public class FolderTemplateEntry
{
    /// <summary>
    /// Ordnername OHNE Nummer (z.B. "Planunterlagen", "Fotos").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ob ein _Eingang Unterordner erstellt werden soll (für PlanManager-Import).
    /// </summary>
    public bool HasInbox { get; set; }

    /// <summary>
    /// Unterordner dieses Hauptordners.
    /// </summary>
    public List<SubFolderEntry> SubFolders { get; set; } = [];

    public FolderTemplateEntry() { }

    public FolderTemplateEntry(string name, bool hasInbox)
    {
        Name = name;
        HasInbox = hasInbox;
    }

    public FolderTemplateEntry(string name, bool hasInbox, List<SubFolderEntry> subFolders)
    {
        Name = name;
        HasInbox = hasInbox;
        SubFolders = subFolders;
    }

    /// <summary>
    /// Generiert den nummerierten Ordnernamen aus der Position.
    /// z.B. Position 2 + Name "Fotos" → "02 Fotos"
    /// </summary>
    public string GetNumberedName(int position) => $"{position:D2} {Name}";
}

/// <summary>
/// Ein Unterordner innerhalb eines Hauptordners.
/// Kann optional nummeriert werden (HasPrefix = true → "00 Name", false → "Name").
/// </summary>
public class SubFolderEntry
{
    /// <summary>
    /// Unterordner-Name (z.B. "Polierpläne", "Absteckpläne").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ob der Unterordner eine Nummer bekommt (00, 01, 02...).
    /// true  → "01 Polierpläne"
    /// false → "Baustelleneinrichtung" (ohne Nummer)
    /// </summary>
    public bool HasPrefix { get; set; } = true;

    public SubFolderEntry() { }

    public SubFolderEntry(string name, bool hasPrefix)
    {
        Name = name;
        HasPrefix = hasPrefix;
    }

    /// <summary>
    /// Generiert den Ordnernamen — mit oder ohne Nummer.
    /// </summary>
    public string GetDisplayName(int position) =>
        HasPrefix ? $"{position:D2} {Name}" : Name;
}
