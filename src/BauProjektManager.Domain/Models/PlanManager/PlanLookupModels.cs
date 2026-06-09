namespace BauProjektManager.Domain.Models.PlanManager;

// Public-API-DTOs für IPlanLookupService (BPM-109.05a, ADR-058 + Addendum).

/// <summary>
/// Ergebnis einer Zeitreise-Abfrage: eine zum Stichzeitpunkt aktuelle Plan-Revision samt
/// Dokument-Basisdaten und dem Drei-Zeiten-Modell (BPM-109.04b).
/// </summary>
/// <param name="DocumentId">plan_documents.id.</param>
/// <param name="DocumentKey">Natural Key (kuratiert, index-frei).</param>
/// <param name="PlanNumber">Plannummer.</param>
/// <param name="DocumentType">Dokumenttyp-Anzeigename.</param>
/// <param name="PlanIndex">Index dieser Revision (NULL bei Erstausgabe).</param>
/// <param name="RevisionId">plan_revisions.id der zum Stichzeitpunkt aktuellen Revision.</param>
/// <param name="PrimaryFileRelativePath">Relativer Pfad der primären Datei (NULL wenn keine).</param>
/// <param name="ReleasedAt">Freigabedatum des Index (UTC), NULL wenn unbekannt.</param>
/// <param name="ReceivedAt">Hinzufügedatum (Import, UTC), immer gesetzt.</param>
/// <param name="CurrentFrom">Ab wann die Revision im Archiv gültig wurde (UTC).</param>
public sealed record PlanLookupResult(
    string DocumentId,
    string DocumentKey,
    string PlanNumber,
    string DocumentType,
    string? PlanIndex,
    string RevisionId,
    string? PrimaryFileRelativePath,
    DateTime? ReleasedAt,
    DateTime ReceivedAt,
    DateTime CurrentFrom)
{
    /// <summary>
    /// Fürs Bautagebuch maßgebliches Datum: Freigabedatum wenn vorhanden, sonst Hinzufügedatum
    /// (ADR-058-Addendum, BPM-109.04b).
    /// </summary>
    public DateTime EffectiveDate => ReleasedAt ?? ReceivedAt;

    /// <summary>
    /// True, wenn kein Freigabedatum hinterlegt ist und deshalb auf das Hinzufügedatum
    /// zurückgegriffen wird. Die UI markiert das <see cref="EffectiveDate"/> dann visuell
    /// (andere Farbe + Hinweis „Importdatum, kein Freigabedatum hinterlegt").
    /// </summary>
    public bool IsDateFallback => ReleasedAt is null;
}

/// <summary>
/// Filter für <see cref="BauProjektManager.Domain.Interfaces.IPlanLookupService.CreatePlanContextSnapshotAsync"/>:
/// welche Pläne festgezogen werden sollen.
/// </summary>
/// <param name="BuildingPartId">SoftRef bpm.db.building_parts(id); null = nicht filtern.</param>
/// <param name="BuildingLevelId">SoftRef bpm.db.building_levels(id); null = nicht filtern.</param>
/// <param name="DocumentTypeIds">Dokumenttyp-IDs; leer = alle Typen.</param>
public sealed record PlanContextFilter(
    string? BuildingPartId,
    string? BuildingLevelId,
    IReadOnlyList<string> DocumentTypeIds);
