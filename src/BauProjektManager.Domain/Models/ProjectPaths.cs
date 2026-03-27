namespace BauProjektManager.Domain.Models;

/// <summary>
/// Pfade eines Bauprojekts (relativ zu Root).
/// </summary>
public class ProjectPaths
{
    public string Root { get; set; } = string.Empty;
    public string Plans { get; set; } = "Pläne";
    public string Inbox { get; set; } = @"Pläne\_Eingang";
    public string Photos { get; set; } = "Fotos";
    public string Documents { get; set; } = "Dokumente";
    public string Protocols { get; set; } = "Protokolle";
    public string Invoices { get; set; } = "Rechnungen";
}
