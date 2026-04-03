namespace BauProjektManager.Domain.Models;

/// <summary>
/// Auftraggeber/Bauherr eines Bauprojekts.
/// Vorbereitet für späteres Adressbuch und Outlook-Sync.
/// </summary>
public class Client
{
    public string Company { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
