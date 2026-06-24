using BauProjektManager.Domain.Enums.PlanManager;

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
    /// Technische User-ID für Modus A (Offline).
    /// Default: "MachineName\UserName" — automatisch beim ersten Start gesetzt.
    /// Wird in Modus C durch Server-User-ID ersetzt.
    /// </summary>
    public string LocalUserId { get; set; } = $"{Environment.MachineName}\\{Environment.UserName}";

    /// <summary>
    /// Lesbarer Anzeigename für created_by/last_modified_by.
    /// Default: Windows-Benutzername. Änderbar in Einstellungen.
    /// </summary>
    public string LocalUserName { get; set; } = Environment.UserName;

    /// <summary>
    /// Ordner-Template für neue Projekte.
    /// Reihenfolge bestimmt die Nummerierung (00, 01, 02...).
    /// Änderbar vom User beim Projekt-Anlegen und in den Einstellungen.
    /// </summary>
    public List<FolderTemplateEntry> FolderTemplate { get; set; } = GetDefaultFolderTemplate();

    /// <summary>
    /// Editierbare Liste der Projektarten (Dropdown im Projekt-Dialog).
    /// Vom User über ✎-Button anpassbar.
    /// </summary>
    public List<string> ProjectTypes { get; set; } = GetDefaultProjectTypes();

    /// <summary>
    /// Editierbare Liste der Bauwerkstypen (Dropdown pro Bauteil).
    /// Ermöglicht gemischte Projekte (Wohnen + Gewerbe).
    /// </summary>
    public List<string> BuildingTypes { get; set; } = GetDefaultBuildingTypes();

    /// <summary>
    /// Editierbare Geschoss-Bezeichnungen: Kurz (EG) + Lang (Erdgeschoss).
    /// Reihenfolge = logische Bau-Reihenfolge von unten nach oben.
    /// </summary>
    public List<LevelNameEntry> LevelNames { get; set; } = GetDefaultLevelNames();

    /// <summary>
    /// Editierbare Rollen-Liste für Projekt-Beteiligte (Tab 3).
    /// Vom User über ✎-Button anpassbar.
    /// </summary>
    public List<string> ParticipantRoles { get; set; } = GetDefaultParticipantRoles();

    /// <summary>
    /// Editierbare Liste der Bauherren-Portal-Typen (Tab 4).
    /// Vom User über ✎-Button anpassbar.
    /// </summary>
    public List<string> PortalTypes { get; set; } = GetDefaultPortalTypes();

    /// <summary>
    /// Standard-Projektarten.
    /// </summary>
    public static List<string> GetDefaultProjectTypes() =>
    [
        "Neubau", "Sanierung", "Umbau", "Zubau", "Abbruch", "Sonstiges"
    ];

    /// <summary>
    /// Standard-Bauwerkstypen.
    /// </summary>
    public static List<string> GetDefaultBuildingTypes() =>
    [
        "EFH", "MFH", "Wohnanlage", "Gewerbe", "Industrie", "Infrastruktur", "Sonstiges"
    ];

    /// <summary>
    /// Standard-Rollen für Projekt-Beteiligte.
    /// </summary>
    public static List<string> GetDefaultParticipantRoles() =>
    [
        "Bauherr", "Architekt", "Statiker", "Haustechnik", "Bauphysik",
        "ÖBA", "Vermessung", "Elektro", "HKLS", "Bodengutachter",
        "Brandschutz", "Geotechnik", "Sonstiges"
    ];

    /// <summary>
    /// Standard-Bauherren-Portal-Typen.
    /// </summary>
    public static List<string> GetDefaultPortalTypes() =>
    [
        "InfoRaum", "PlanRadar", "PlanFred", "Bau-Master", "Dalux"
    ];

    /// <summary>
    /// Standard-Geschossbezeichnungen: Kurzname + Langname.
    /// </summary>
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

    /// <summary>
    /// Standard-Ordnerstruktur für neue Bauprojekte.
    /// Namen OHNE Nummer — die Nummerierung entsteht automatisch aus der Reihenfolge.
    /// </summary>
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

    // === Typ-Metadaten (ADR-061) — optional, nur Struktur (Slice 0.1) ===
    // Befuellt wird das Template erst beim Seed (Slice 0.4). Regel: dieser Node
    // wird Dokumenttyp GENAU DANN, wenn CreatesDocumentType == true (keine
    // implizite Ableitung aus Name/Prefix/Position/Kategorien).

    /// <summary>True = aus diesem Template-Node wird beim Seed ein Dokumenttyp.</summary>
    public bool CreatesDocumentType { get; set; }

    /// <summary>Stabiler Typ-Key (gesperrt nach Anlage). NULL solange kein Typ.</summary>
    public string? DocumentTypeKey { get; set; }

    /// <summary>Anzeigename des erzeugten Typs (NULL = Ordnername verwenden).</summary>
    public string? DocumentTypeDisplayName { get; set; }

    /// <summary>Unterteilungs-Schema des erzeugten Typs (NULL solange kein Typ).</summary>
    public Ring2Source? Ring2Source { get; set; }

    /// <summary>Typgebundene Kategorien (nur relevant bei Ring2Source=Categories).</summary>
    public List<FolderTemplateCategory> Categories { get; set; } = [];

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

    /// <summary>
    /// Verschachtelte Unterordner (rekursiv).
    /// </summary>
    public List<SubFolderEntry> SubFolders { get; set; } = [];

    // === Typ-Metadaten (ADR-061) — optional, nur Struktur (Slice 0.1) ===
    // Analog zu FolderTemplateEntry: auch ein Unterordner kann ein Dokumenttyp
    // sein (z. B. "Polierpläne" unter "01 Planunterlagen"). Befuellung: Slice 0.4.

    /// <summary>True = aus diesem Template-Node wird beim Seed ein Dokumenttyp.</summary>
    public bool CreatesDocumentType { get; set; }

    /// <summary>Stabiler Typ-Key (gesperrt nach Anlage). NULL solange kein Typ.</summary>
    public string? DocumentTypeKey { get; set; }

    /// <summary>Anzeigename des erzeugten Typs (NULL = Ordnername verwenden).</summary>
    public string? DocumentTypeDisplayName { get; set; }

    /// <summary>Unterteilungs-Schema des erzeugten Typs (NULL solange kein Typ).</summary>
    public Ring2Source? Ring2Source { get; set; }

    /// <summary>Typgebundene Kategorien (nur relevant bei Ring2Source=Categories).</summary>
    public List<FolderTemplateCategory> Categories { get; set; } = [];

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

/// <summary>
/// Typgebundene Kategorie im Ordner-Template (ADR-061) — Bootstrap-Quelle fuer
/// document_type_categories. Pro Kategorie steuert <see cref="HasPrefix"/>, ob
/// der erzeugte Kategorieordner eine laufende Nummer bekommt.
/// </summary>
public class FolderTemplateCategory
{
    /// <summary>Kategoriename (z. B. "Baubesprechung", "Wände").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Ob der Kategorieordner nummeriert wird ("01 Wände" vs. "Wände").</summary>
    public bool HasPrefix { get; set; } = true;

    public FolderTemplateCategory() { }

    public FolderTemplateCategory(string name, bool hasPrefix = true)
    {
        Name = name;
        HasPrefix = hasPrefix;
    }
}
