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
        new("Planunterlagen", hasInbox: true),
        new("Fotos",          hasInbox: false),
        new("Leica",          hasInbox: false),
        new("DOKA",           hasInbox: false),
        new("LV",             hasInbox: false),
        new("Protokolle",     hasInbox: false),
    ];
}

/// <summary>
/// Ein Eintrag im Ordner-Template.
/// Die Nummer wird NICHT gespeichert — sie entsteht aus der Position in der Liste.
/// Position 0 → "00_Name", Position 1 → "01_Name", etc.
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

    public FolderTemplateEntry() { }

    public FolderTemplateEntry(string name, bool hasInbox)
    {
        Name = name;
        HasInbox = hasInbox;
    }

    /// <summary>
    /// Generiert den nummerierten Ordnernamen aus der Position.
    /// z.B. Position 2 + Name "Fotos" → "02 Fotos"
    /// </summary>
    public string GetNumberedName(int position) => $"{position:D2} {Name}";
}
