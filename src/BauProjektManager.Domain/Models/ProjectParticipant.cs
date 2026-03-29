namespace BauProjektManager.Domain.Models;

/// <summary>
/// Ein Beteiligter an einem Bauprojekt (Firma/Person mit Rolle).
/// Daten werden aktuell direkt in project_participants gespeichert.
/// Später: Verknüpfung mit zentralem Adressbuch (contact_id FK)
/// und Outlook-Kontakt-Kompatibilität.
/// </summary>
public class ProjectParticipant
{
    /// <summary>
    /// Eindeutige ID (ppart_001, ppart_002...).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Rolle im Projekt (z.B. "Architekt", "Statiker", "ÖBA", "Haustechnik").
    /// Editierbare Liste aus settings.json (ParticipantRoles).
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Firmenname (z.B. "Arch. Mueller + Partner").
    /// </summary>
    public string Company { get; set; } = string.Empty;

    /// <summary>
    /// Ansprechperson (z.B. "DI Stefan Mueller").
    /// </summary>
    public string ContactPerson { get; set; } = string.Empty;

    /// <summary>
    /// Telefonnummer.
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// E-Mail-Adresse.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Sortierreihenfolge im UI.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Spätere Verknüpfung mit zentralem Adressbuch.
    /// Aktuell leer — wird bei Adressbuch-Feature befüllt.
    /// Ermöglicht projektübergreifende Wiederverwendung von Kontakten.
    /// </summary>
    public string ContactId { get; set; } = string.Empty;
}
