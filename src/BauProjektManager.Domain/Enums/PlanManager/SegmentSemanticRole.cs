namespace BauProjektManager.Domain.Enums.PlanManager;

/// <summary>
/// Fachliche Sonderrolle eines Segmenttyps (BPM-108).
/// Built-ins haben eine seed-definierte Rolle (read-only im Manager),
/// Custom-Segmenttypen haben immer <see cref="None"/>.
/// </summary>
public enum SegmentSemanticRole
{
    None = 0,
    PlanNumber = 1,
    PlanIndex = 2,
    ProjectNumber = 3,
    Date = 4,
    Description = 5,
    Spatial = 6,
    Ignore = 7
}
