using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.Infrastructure.Services;

/// <summary>
/// In-Memory-Katalog fuer Segmenttypen (BPM-108 Phase A).
/// Lazy Load aus <see cref="ISegmentTypeRepository"/>, invalidiert nach Mutationen,
/// feuert <see cref="Changed"/>.
/// </summary>
public class SegmentTypeCatalog : ISegmentTypeCatalog
{
    private readonly ISegmentTypeRepository _repository;
    private readonly object _lock = new();

    private List<SegmentTypeDefinition>? _typesAll;          // inkl. deleted, fuer Lookup
    private List<SegmentTypeGroupDefinition>? _groupsAll;     // inkl. deleted

    public SegmentTypeCatalog(ISegmentTypeRepository repository)
    {
        _repository = repository;
    }

    public event EventHandler? Changed;

    public IReadOnlyList<SegmentTypeDefinition> GetEffectiveActive()
    {
        EnsureLoaded();
        // Snapshot lokal um Race zu vermeiden — _typesAll/_groupsAll werden bei Invalidate ersetzt
        List<SegmentTypeDefinition> types;
        List<SegmentTypeGroupDefinition> groups;
        lock (_lock)
        {
            types = _typesAll!;
            groups = _groupsAll!;
        }

        var activeGroupIds = groups
            .Where(g => g.IsActive && !g.IsDeleted)
            .ToDictionary(g => g.Id, g => g.SortOrder);

        return types
            .Where(t => t.IsActive && !t.IsDeleted && activeGroupIds.ContainsKey(t.GroupId))
            .OrderBy(t => activeGroupIds[t.GroupId])
            .ThenBy(t => t.SortOrder)
            .ToList();
    }

    public SegmentTypeDefinition? GetIncludingDeleted(string id)
    {
        EnsureLoaded();
        lock (_lock)
        {
            return _typesAll!.FirstOrDefault(t => t.Id == id);
        }
    }

    public IReadOnlyDictionary<string, SegmentTypeDefinition> SnapshotIncludingDeleted()
    {
        EnsureLoaded();
        lock (_lock)
        {
            return _typesAll!.ToDictionary(t => t.Id);
        }
    }

    public IReadOnlyList<SegmentTypeGroupDefinition> GetActiveGroups()
    {
        EnsureLoaded();
        lock (_lock)
        {
            return _groupsAll!
                .Where(g => g.IsActive && !g.IsDeleted)
                .OrderBy(g => g.SortOrder)
                .ToList();
        }
    }

    public void Invalidate()
    {
        lock (_lock)
        {
            _typesAll = null;
            _groupsAll = null;
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void EnsureLoaded()
    {
        lock (_lock)
        {
            if (_typesAll is not null && _groupsAll is not null) return;
            _typesAll = _repository.LoadAllTypes(includeDeleted: true).ToList();
            _groupsAll = _repository.LoadAllGroups(includeDeleted: true).ToList();
        }
    }
}
