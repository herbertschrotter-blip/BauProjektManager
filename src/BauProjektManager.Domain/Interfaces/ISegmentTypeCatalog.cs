using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// In-Memory-Katalog fuer Segmenttypen + Gruppen (BPM-108).
/// Lazy Load, invalidiert nach jeder Manager-Mutation, feuert <see cref="Changed"/>.
/// Lookup ungefiltert (inklusive deleted/inactive) ist Pflicht, damit bestehende Profile
/// mit Soft-Delete-Referenzen weiter rendern koennen.
/// </summary>
public interface ISegmentTypeCatalog
{
    /// <summary>
    /// Aktive, nicht geloeschte Typen — sortiert nach Gruppen-SortOrder, dann Typen-SortOrder.
    /// Quelle fuer Wizard-Chips ("+ Eigenes" Drag-Quellen).
    /// </summary>
    IReadOnlyList<SegmentTypeDefinition> GetEffectiveActive();

    /// <summary>
    /// Einzel-Lookup. Auch deleted/inactive werden zurueckgegeben.
    /// </summary>
    SegmentTypeDefinition? GetIncludingDeleted(string id);

    /// <summary>
    /// Snapshot aller Typen (auch deleted) als Dictionary fuer Token-Renderer.
    /// </summary>
    IReadOnlyDictionary<string, SegmentTypeDefinition> SnapshotIncludingDeleted();

    /// <summary>
    /// Aktive Gruppen — sortiert nach SortOrder.
    /// </summary>
    IReadOnlyList<SegmentTypeGroupDefinition> GetActiveGroups();

    /// <summary>
    /// Cache invalidieren (z. B. nach Manager-Save). Triggert <see cref="Changed"/>.
    /// </summary>
    void Invalidate();

    /// <summary>
    /// Wird nach jeder Invalidierung gefeuert (UI-Listener fuer Cache-Refresh).
    /// </summary>
    event EventHandler? Changed;
}
