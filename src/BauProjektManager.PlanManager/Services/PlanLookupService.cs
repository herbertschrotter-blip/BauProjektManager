using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Stub-Implementierung von <see cref="IPlanLookupService"/> (BPM-109.05a, Foundation Slice).
/// Definiert nur den Vertrag — die Query-Logik (Zeitreise gegen plan_documents/plan_revisions,
/// plan_context_links-Snapshot mit fixed_revision) folgt in BPM-109.05 (post-V1, parallel zu BPM-056).
///
/// Bewusst Fail-Fast statt stiller Leerresultate: ein versehentlicher Konsument vor der
/// Implementation soll sichtbar scheitern, nicht stumm „keine Pläne" liefern.
/// </summary>
public sealed class PlanLookupService : IPlanLookupService
{
    private const string NotImplMessage =
        "BPM-109.05a: IPlanLookupService ist nur als Vertrag vorhanden. " +
        "Die Query-/Snapshot-Implementation folgt in BPM-109.05 (post-V1, parallel zu BPM-056).";

    public Task<IReadOnlyList<PlanLookupResult>> FindCurrentPlansAsync(
        string projectId,
        string? buildingPartId,
        string? buildingLevelId,
        IReadOnlyList<string> documentTypeIds,
        DateTime atUtc,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException(NotImplMessage);

    public Task CreatePlanContextSnapshotAsync(
        string sourceModule,
        string sourceId,
        DateTime atUtc,
        PlanContextFilter filters,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException(NotImplMessage);
}
