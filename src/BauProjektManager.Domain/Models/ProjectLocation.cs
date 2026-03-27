namespace BauProjektManager.Domain.Models;

/// <summary>
/// Adresse und Koordinaten eines Bauprojekts.
/// </summary>
public class ProjectLocation
{
    public string Address { get; set; } = string.Empty;
    public string Municipality { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string State { get; set; } = "Steiermark";
    public string CoordinateSystem { get; set; } = "EPSG:31258";
    public double CoordinateEast { get; set; }
    public double CoordinateNorth { get; set; }
    public string CadastralKg { get; set; } = string.Empty;
    public string CadastralKgName { get; set; } = string.Empty;
    public string CadastralGst { get; set; } = string.Empty;
}
