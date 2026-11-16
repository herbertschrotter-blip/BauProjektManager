namespace BauProjektManager.Domain.Enums.PlanManager;

/// <summary>
/// Herkunft einer Pending-Zuordnung (ADR-059 Pending Assignments). Importiert wird
/// in beiden Faellen erst mit "Import bestaetigen" ueber den journalisierten
/// Import (ADR-064) — die Herkunft steuert nur die Kennzeichnung in der Liste.
/// </summary>
public enum PendingAssignmentSource
{
    /// <summary>Vom User im Radial/Panel zugeordnet (Standard).</summary>
    Manual = 0,

    /// <summary>
    /// Vorschlag aus "Mit Claude sortieren": aus dem geprueften entwurf.json
    /// uebernommen, vom User noch nicht einzeln angefasst.
    /// </summary>
    ClaudeSuggestion = 1
}
