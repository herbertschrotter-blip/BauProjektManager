namespace BauProjektManager.Domain.Enums;

/// <summary>
/// Status eines Bauprojekts.
/// Nur Aktiv und Abgeschlossen — kein Archiviert mehr als separater Status.
/// </summary>
public enum ProjectStatus
{
    Active,
    Completed
}
