namespace BauProjektManager.Domain.Models;

/// <summary>
/// Ein Bauteil/Bauabschnitt innerhalb eines Bauprojekts.
/// Jedes Bauteil kann einen eigenen Bauwerkstyp und ein eigenes ± 0,00 Niveau haben.
/// Beispiele: "BT-A" (Stiege 1+2, Wohnanlage), "GEW" (Gewerbe EG).
/// </summary>
public class BuildingPart
{
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Kürzel des Bauteils (z.B. "BT-A", "GEW", "TG").
    /// </summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>
    /// Beschreibung (z.B. "Stiege 1+2", "Gewerbe EG", "Tiefgarage").
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Bauwerkstyp dieses Bauteils (z.B. "Wohnanlage", "Gewerbe", "EFH").
    /// Editierbare Liste aus settings.json (BuildingTypes).
    /// Ermöglicht gemischte Projekte (Wohnen + Gewerbe im selben Projekt).
    /// </summary>
    public string BuildingType { get; set; } = string.Empty;

    /// <summary>
    /// ± 0,00 Absolutniveau in Meter über Adria.
    /// Kann pro Bauteil unterschiedlich sein.
    /// </summary>
    public double ZeroLevelAbsolute { get; set; }

    /// <summary>
    /// Sortierreihenfolge im UI.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Geschosse dieses Bauteils (von unten nach oben sortiert).
    /// </summary>
    public List<BuildingLevel> Levels { get; set; } = [];
}
