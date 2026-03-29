namespace BauProjektManager.Domain.Models;

/// <summary>
/// Ein Geschoss innerhalb eines Bauteils.
/// Speichert 3 Eingabewerte (RDOK, FBOK, RDUK) von ± 0,00.
/// 4 weitere Werte werden live errechnet und NICHT in der DB gespeichert.
/// </summary>
public class BuildingLevel
{
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Sortier-Präfix für Dateinamen und Excel-Export.
    /// EG = 0, darunter negativ (-01, -02...), darüber positiv (01, 02...).
    /// </summary>
    public int Prefix { get; set; }

    /// <summary>
    /// Geschoss-Bezeichnung (z.B. UG2, UG, EG, OG1, DG).
    /// Auswahl per editierbarem Dropdown (LevelNames in settings.json).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Beschreibung (z.B. "Tiefgarage", "Erdgeschoss", "1. Obergeschoss").
    /// </summary>
    public string Description { get; set; } = string.Empty;

    // === 3 Eingabewerte (von ± 0,00, in Meter) ===

    /// <summary>
    /// Rohdecken-Oberkante (RDOK) in Meter von ± 0,00.
    /// Beim untersten Geschoss = Bodenplatten-Oberkante (BPOK).
    /// </summary>
    public double Rdok { get; set; }

    /// <summary>
    /// Fertigfußboden-Oberkante (FBOK) in Meter von ± 0,00.
    /// EG hat typischerweise FBOK = 0.00.
    /// </summary>
    public double Fbok { get; set; }

    /// <summary>
    /// Rohdecken-Unterkante (RDUK) in Meter von ± 0,00.
    /// Beim untersten Geschoss null (keine Decke darunter).
    /// </summary>
    public double? Rduk { get; set; }

    /// <summary>
    /// Sortierreihenfolge im UI (von unten nach oben).
    /// </summary>
    public int SortOrder { get; set; }

    // === 4 errechnete Werte (NICHT in DB) ===

    /// <summary>
    /// Deckenstärke = RDOK - RDUK (in Meter).
    /// null wenn RDUK nicht gesetzt (unterstes Geschoss).
    /// </summary>
    public double? DeckThickness => Rduk.HasValue ? Math.Round(Rdok - Rduk.Value, 3) : null;

    /// <summary>
    /// Fußbodenaufbau = FBOK - RDOK (in Meter).
    /// </summary>
    public double FloorBuildup => Math.Round(Fbok - Rdok, 3);

    /// <summary>
    /// Geschosshöhe — wird von außen gesetzt (braucht das nächste Geschoss darüber).
    /// FBOK(n+1) - FBOK(n). null beim obersten Geschoss.
    /// </summary>
    public double? StoryHeight { get; set; }

    /// <summary>
    /// Rohbauhöhe — wird von außen gesetzt (braucht das nächste Geschoss darüber).
    /// RDOK(n+1) - RDOK(n). null beim obersten Geschoss.
    /// </summary>
    public double? RawHeight { get; set; }

    /// <summary>
    /// Berechnet die Absolutnhöhe (FBOK + ± 0,00 absolut des Bauteils).
    /// </summary>
    public double GetAbsoluteHeight(double zeroLevelAbsolute) =>
        Math.Round(zeroLevelAbsolute + Fbok, 3);

    /// <summary>
    /// Generiert den Präfix-String für Sortierung (z.B. "-02", "-01", "00", "01", "02").
    /// </summary>
    public string PrefixString => Prefix >= 0 ? $"{Prefix:D2}" : $"-{Math.Abs(Prefix):D2}";
}
