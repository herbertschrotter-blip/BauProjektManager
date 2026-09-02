using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.Domain.Models;

// BPM-069: Diese Klassen lebten bisher in AppSettings.cs. Die AppSettings-Fassade ist entfernt,
// die Template-/Listen-Modelle bleiben (genutzt von SharedConfig, FolderTemplateControl, Seed).

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
