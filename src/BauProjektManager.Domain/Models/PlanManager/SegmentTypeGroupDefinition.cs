namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Gruppe fuer Segmenttypen (Identifikation, Raeumlich, Inhaltlich, Sonstiges, Custom).
/// BPM-108 Phase A.
/// </summary>
public class SegmentTypeGroupDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsBuiltin { get; set; }
    public int BuiltinVersion { get; set; } = 1;
    public bool UserModifiedName { get; set; }
    public bool UserModifiedSort { get; set; }
    public bool UserModifiedActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime LastModifiedAt { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
    public int SyncVersion { get; set; }
    public bool IsDeleted { get; set; }
}
