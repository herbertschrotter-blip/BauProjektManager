namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Feldtypen für Dateinamen-Segmente.
/// System-Felder + vordefinierte Bau-Felder + Custom für benutzerdefinierte.
/// </summary>
public enum FieldType
{
    // System-Felder
    PlanNumber,
    PlanIndex,
    ProjectNumber,
    Description,
    Ignore,
    Datum,

    // Bau-spezifische Felder
    Geschoss,
    Haus,
    Planart,
    Objekt,
    Bauteil,
    Bauabschnitt,
    Stiege,
    Achse,
    Zone,
    Block,

    // Benutzerdefiniert
    Custom
}
