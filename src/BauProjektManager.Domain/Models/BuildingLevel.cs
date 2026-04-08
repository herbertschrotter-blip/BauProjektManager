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
    /// Wird automatisch aus der Reihenfolge berechnet.
    /// </summary>
    public int Prefix { get; set; }

    /// <summary>
    /// Geschoss-Bezeichnung (z.B. UG2, UG, EG, OG1, DG).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Beschreibung (z.B. "Tiefgarage", "Erdgeschoss", "1. Obergeschoss").
    /// Wird automatisch aus der Geschoss-Liste (settings.json) abgeleitet.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    // === 3 Eingabewerte (von ± 0,00, in Meter) ===

    public double Rdok { get; set; }
    public double Fbok { get; set; }
    public double? Rduk { get; set; }

    /// <summary>
    /// Display-Property für RDUK im DataGrid — zeigt leer statt null.
    /// Akzeptiert Komma und Punkt bei Eingabe.
    /// </summary>
    public string RdukDisplay
    {
        get => Rduk.HasValue ? Rduk.Value.ToString("F2") : "";
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                Rduk = null;
            else if (double.TryParse(value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture, out var v))
                Rduk = v;
        }
    }

    public int SortOrder { get; set; }

    // === 4 errechnete Werte (NICHT in DB) ===

    /// <summary>
    /// Deckenstärke = RDOK(n+1) − RDUK(n).
    /// Wird von RecalculateLevels gesetzt, NICHT selbst berechnet.
    /// </summary>
    public double? DeckThickness { get; set; }
    public double FloorBuildup => Math.Round(Fbok - Rdok, 3);
    public double? StoryHeight { get; set; }
    public double? RawHeight { get; set; }

    public double GetAbsoluteHeight(double zeroLevelAbsolute) =>
        Math.Round(zeroLevelAbsolute + Fbok, 3);

    public string PrefixString => Prefix >= 0 ? $"{Prefix:D2}" : $"-{Math.Abs(Prefix):D2}";

    /// <summary>
    /// Gibt die Langbezeichnung für einen Geschoss-Namen aus der Settings-Liste zurück.
    /// </summary>
    public static string GetAutoDescription(string levelName, List<LevelNameEntry> levelNames)
    {
        var entry = levelNames.FirstOrDefault(ln =>
            ln.ShortName.Equals(levelName.Trim(), StringComparison.OrdinalIgnoreCase));
        return entry?.LongName ?? "";
    }

    /// <summary>
    /// Gibt das nächste logische Geschoss zurück basierend auf der Settings-Liste.
    /// z.B. wenn "UG" das letzte war → "EG", wenn "EG" → "OG1", etc.
    /// </summary>
    public static string GetNextLevelName(string lastLevelName, List<LevelNameEntry> levelNames)
    {
        if (levelNames.Count == 0) return "EG";
        var idx = levelNames.FindIndex(ln =>
            ln.ShortName.Equals(lastLevelName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (idx >= 0 && idx < levelNames.Count - 1)
            return levelNames[idx + 1].ShortName;
        // Kein Match oder letztes → erstes zurück
        return levelNames[0].ShortName;
    }
}
