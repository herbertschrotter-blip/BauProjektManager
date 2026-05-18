using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// CRUD-Zugriff auf <c>segment_types</c> und <c>segment_type_groups</c> in bpm.db (BPM-108).
/// Soft-Delete: Loeschen setzt <c>is_deleted = 1</c>. Built-ins werden nie hart geloescht.
/// </summary>
public interface ISegmentTypeRepository
{
    // === GROUPS ===

    IReadOnlyList<SegmentTypeGroupDefinition> LoadAllGroups(bool includeDeleted = false);
    SegmentTypeGroupDefinition? GetGroup(string id, bool includeDeleted = false);
    void SaveGroup(SegmentTypeGroupDefinition group);
    void SoftDeleteGroup(string id);

    // === TYPES ===

    IReadOnlyList<SegmentTypeDefinition> LoadAllTypes(bool includeDeleted = false);
    SegmentTypeDefinition? GetType(string id, bool includeDeleted = false);
    void SaveType(SegmentTypeDefinition type);
    void SoftDeleteType(string id);

    /// <summary>
    /// Ermittelt, ob ein <see cref="SegmentTypeDefinition.TokenKey"/> in einer
    /// nicht-geloeschten Zeile bereits belegt ist (UNIQUE-Constraint-Hilfe).
    /// </summary>
    bool TokenKeyExists(string tokenKey, string? excludingId = null);
}
