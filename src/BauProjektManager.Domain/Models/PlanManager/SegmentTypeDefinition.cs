using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Segmenttyp im PlanManager (BPM-108 Phase A).
/// Persistente, user-erweiterbare Klassifikation fuer Dateinamen-Segmente.
/// </summary>
/// <remarks>
/// <para>Built-ins: <c>id</c> = snake_case String (z. B. <c>plan_number</c>), seed-definierte
/// <see cref="SemanticRole"/>.</para>
/// <para>Custom: <c>id</c> = ULID, <see cref="SemanticRole"/> immer <see cref="SegmentSemanticRole.None"/>.</para>
/// <para>Immutable nach Anlage: <c>Id</c>, <c>TokenKey</c>, <c>SemanticRole</c> (bei Built-ins), <c>IsBuiltin</c>.</para>
/// </remarks>
public class SegmentTypeDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string TokenKey { get; set; } = string.Empty;
    public SegmentSemanticRole? SemanticRole { get; set; }
    public string GroupId { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsBuiltin { get; set; }
    public int BuiltinVersion { get; set; } = 1;
    public bool UserModifiedName { get; set; }
    public bool UserModifiedColor { get; set; }
    public bool UserModifiedSort { get; set; }
    public bool UserModifiedActive { get; set; }
    public bool UserModifiedGroup { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime LastModifiedAt { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
    public int SyncVersion { get; set; }
    public bool IsDeleted { get; set; }
}
