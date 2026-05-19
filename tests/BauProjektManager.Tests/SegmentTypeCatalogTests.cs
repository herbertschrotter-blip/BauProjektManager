using System.IO;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// Unit-Tests fuer <see cref="SegmentTypeCatalog"/> (BPM-108 Phase A).
/// Cache-Verhalten, GetEffectiveActive-Sortierung, Changed-Event.
/// </summary>
public class SegmentTypeCatalogTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteConnection _schemaConn;
    private readonly SegmentTypeRepository _repo;

    private sealed class FakeUserContext : IUserContext
    {
        public string UserId => "TEST\\user";
        public string DisplayName => "Test User";
        public UserContextSource Source => UserContextSource.Local;
    }

    public SegmentTypeCatalogTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bpm-cat-{Guid.NewGuid():N}.db");
        var cs = $"Data Source={_dbPath}";
        _schemaConn = new SqliteConnection(cs);
        _schemaConn.Open();
        SegmentTypeRepository.CreateTables(_schemaConn);
        _repo = new SegmentTypeRepository(cs, new FakeUserContext());
        new SegmentTypeSeedService(_repo).Seed();
    }

    public void Dispose()
    {
        _schemaConn.Dispose();
        SqliteConnection.ClearAllPools();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { /* ignore */ }
    }

    [Fact]
    public void GetEffectiveActive_AfterSeed_Returns16Types()
    {
        var cat = new SegmentTypeCatalog(_repo);

        var active = cat.GetEffectiveActive();

        Assert.Equal(16, active.Count);
    }

    [Fact]
    public void GetEffectiveActive_OrdersByGroupSortThenTypeSort()
    {
        var cat = new SegmentTypeCatalog(_repo);

        var active = cat.GetEffectiveActive();

        // erste 3 Typen sind in Identifikation (group sort 10): plan_number(10), plan_index(20), project_number(30)
        Assert.Equal("plan_number",    active[0].Id);
        Assert.Equal("plan_index",     active[1].Id);
        Assert.Equal("project_number", active[2].Id);

        // dann Spatial (group sort 20), geschoss zuerst (type sort 10)
        Assert.Equal("geschoss", active[3].Id);
    }

    [Fact]
    public void GetEffectiveActive_ExcludesDeactivatedType()
    {
        var t = _repo.GetType("planart")!;
        t.IsActive = false;
        _repo.SaveType(t);

        var cat = new SegmentTypeCatalog(_repo);
        var active = cat.GetEffectiveActive();

        Assert.DoesNotContain(active, x => x.Id == "planart");
    }

    [Fact]
    public void GetEffectiveActive_ExcludesTypesInDeactivatedGroup()
    {
        var g = _repo.GetGroup(SegmentTypeSeedService.GroupSonstiges)!;
        g.IsActive = false;
        _repo.SaveGroup(g);

        var cat = new SegmentTypeCatalog(_repo);
        var active = cat.GetEffectiveActive();

        Assert.DoesNotContain(active, x => x.Id == "datum");
        Assert.DoesNotContain(active, x => x.Id == "ignore");
    }

    [Fact]
    public void GetIncludingDeleted_FindsSoftDeletedType()
    {
        _repo.SoftDeleteType("planart");

        var cat = new SegmentTypeCatalog(_repo);
        var found = cat.GetIncludingDeleted("planart");

        Assert.NotNull(found);
        Assert.True(found!.IsDeleted);
        Assert.DoesNotContain(cat.GetEffectiveActive(), x => x.Id == "planart");
    }

    [Fact]
    public void Invalidate_FiresChangedEvent()
    {
        var cat = new SegmentTypeCatalog(_repo);
        var fired = 0;
        cat.Changed += (_, _) => fired++;

        cat.Invalidate();

        Assert.Equal(1, fired);
    }

    [Fact]
    public void Invalidate_RefreshesCache()
    {
        var cat = new SegmentTypeCatalog(_repo);
        var initial = cat.GetEffectiveActive().Count;
        Assert.Equal(16, initial);

        _repo.SoftDeleteType("ignore");
        // Ohne Invalidate: alter Cache liefert noch 16
        Assert.Equal(16, cat.GetEffectiveActive().Count);

        cat.Invalidate();

        Assert.Equal(15, cat.GetEffectiveActive().Count);
    }

    [Fact]
    public void SnapshotIncludingDeleted_ContainsAllTypes()
    {
        _repo.SoftDeleteType("planart");

        var cat = new SegmentTypeCatalog(_repo);
        var snap = cat.SnapshotIncludingDeleted();

        Assert.Equal(16, snap.Count); // auch geloeschte
        Assert.True(snap["planart"].IsDeleted);
    }

    [Fact]
    public void GetActiveGroups_OrdersBySortOrder()
    {
        var cat = new SegmentTypeCatalog(_repo);
        var groups = cat.GetActiveGroups();

        Assert.Equal(5, groups.Count);
        Assert.Equal(SegmentTypeSeedService.GroupIdentifikation, groups[0].Id);
        Assert.Equal(SegmentTypeSeedService.GroupRaeumlich,      groups[1].Id);
        Assert.Equal(SegmentTypeSeedService.GroupInhaltlich,     groups[2].Id);
        Assert.Equal(SegmentTypeSeedService.GroupSonstiges,      groups[3].Id);
        Assert.Equal(SegmentTypeSeedService.GroupEigene,         groups[4].Id);
    }
}
