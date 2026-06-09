using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// Öffentliche API des PlanManager für konsumierende Module (BPM-056 Bautagebuch, BPM-057 Foto,
/// BPM-061 Vorlagen). Schema v2.0 / ADR-058. Konsumenten verwenden NICHT direkten SQL-Zugriff auf
/// plan_documents oder LIKE '%H1%' auf document_key — Filter laufen typsicher über diese API,
/// damit die Persistenz-Schicht refaktorierbar bleibt.
///
/// BPM-109.05a: Interface-Vertrag (Foundation Slice). Implementation = BPM-109.05 (post-V1,
/// parallel zu BPM-056).
/// </summary>
public interface IPlanLookupService
{
    /// <summary>
    /// Zeitreise-Query: welche Plan-Revisionen waren zum Zeitpunkt <paramref name="atUtc"/> für die
    /// angegebene Gebäude-Hierarchie + Dokumenttypen aktuell? (current_from &lt;= atUtc &lt; superseded_at).
    /// </summary>
    /// <param name="projectId">Projekt-ID.</param>
    /// <param name="buildingPartId">SoftRef bpm.db.building_parts(id); null = nicht filtern.</param>
    /// <param name="buildingLevelId">SoftRef bpm.db.building_levels(id); null = nicht filtern.</param>
    /// <param name="documentTypeIds">Dokumenttyp-IDs (Profil-IDs); leer = alle Typen.</param>
    /// <param name="atUtc">Stichzeitpunkt (UTC), z.B. Berichtsdatum des Bautagebuchs.</param>
    Task<IReadOnlyList<PlanLookupResult>> FindCurrentPlansAsync(
        string projectId,
        string? buildingPartId,
        string? buildingLevelId,
        IReadOnlyList<string> documentTypeIds,
        DateTime atUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Friert die zum Zeitpunkt <paramref name="atUtc"/> aktuellen Revisionen als
    /// <c>plan_context_links</c> mit <c>resolution_mode = 'fixed_revision'</c> fest (ADR-058
    /// fachliche Invariante) — historische Berichte zeigen immer dieselbe Revision, auch nach
    /// späterer Korrektur. Aufgerufen beim Speichern eines Bautagebuch-/Foto-/Vorlagen-Eintrags.
    /// </summary>
    /// <param name="sourceModule">z.B. "bautagebuch", "foto", "vorlage".</param>
    /// <param name="sourceId">ID des Eintrags im Quell-Modul.</param>
    /// <param name="atUtc">Stichzeitpunkt (UTC), zu dem festgezogen wird.</param>
    /// <param name="filters">Gebäude-Hierarchie + Dokumenttyp-Filter für die festzuziehenden Pläne.</param>
    Task CreatePlanContextSnapshotAsync(
        string sourceModule,
        string sourceId,
        DateTime atUtc,
        PlanContextFilter filters,
        CancellationToken cancellationToken = default);
}
