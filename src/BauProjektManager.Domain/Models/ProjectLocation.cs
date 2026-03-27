namespace BauProjektManager.Domain.Models;

/// <summary>
/// Adresse und Koordinaten eines Bauprojekts.
/// </summary>
public class ProjectLocation
{
    // Adresse
    public string Street { get; set; } = string.Empty;
    public string HouseNumber { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    // Verwaltung
    public string Municipality { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string State { get; set; } = "Steiermark";

    // Koordinaten
    public string CoordinateSystem { get; set; } = "EPSG:31258";
    public double CoordinateEast { get; set; }
    public double CoordinateNorth { get; set; }

    // Grundstück
    public string CadastralKg { get; set; } = string.Empty;
    public string CadastralKgName { get; set; } = string.Empty;
    public string CadastralGst { get; set; } = string.Empty;

    /// <summary>
    /// Formatierte Adresse: Straße Nr, PLZ Ort
    /// </summary>
    public string FormattedAddress
    {
        get
        {
            var streetPart = string.IsNullOrEmpty(HouseNumber)
                ? Street
                : $"{Street} {HouseNumber}";
            var cityPart = string.IsNullOrEmpty(PostalCode)
                ? City
                : $"{PostalCode} {City}";

            if (string.IsNullOrEmpty(streetPart)) return cityPart;
            if (string.IsNullOrEmpty(cityPart)) return streetPart;
            return $"{streetPart}, {cityPart}";
        }
    }
}
