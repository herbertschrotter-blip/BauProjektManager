using System.IO;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// Unit-Tests fuer <see cref="SegmentTypeSeedService"/> (BPM-108 Phase A).
/// Built-in Seed-Verhalten + <c>user_modified_*</c>-Schutz.
/// </summary>
public class SegmentTypeSeedServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteConnection _schemaConn;
    private readonly SegmentTypeRepository _repo;
    private readonly SegmentTypeSeedService _sut;

    private sealed class FakeUserContext : IUserContext
    {
        public string UserId => "TEST\\user";
        public string DisplayName => "Test User";
        public UserContextSource Source => UserContextSource.Local;
    }

    public SegmentTypeSeedServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bpm-seed-{Guid.NewGuid():N}.db");
        var cs = $"Data Source={_dbPath}";
        _schemaConn = new SqliteConnection(cs);
        _schemaConn.Open();
        SegmentTypeRepository.CreateTables(_schemaConn);
        _repo = new SegmentTypeRepository(cs, new FakeUserContext());
        _sut = new SegmentTypeSeedService(_repo);
    }

    public void Dispose()
    {
        _schemaConn.Dispose();
        SqliteConnection.ClearAllPools();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { /* ignore */ }
    }

    [Fact]
    public void Seed_OnEmptyDb_Creates5GroupsAnd16Types()
    {
        _sut.Seed();

        Assert.Equal(5, _repo.LoadAllGroups().Count);
        Assert.Equal(16, _repo.LoadAllTypes().Count);
    }

    [Fact]
    public void Seed_AssignsSemanticRoles_ForBuiltins()
    {
        _sut.Seed();

        Assert.Equal(SegmentSemanticRole.PlanNumber,    _repo.GetType("plan_number")!.SemanticRole);
        Assert.Equal(SegmentSemanticRole.PlanIndex,     _repo.GetType("plan_index")!.SemanticRole);
        Assert.Equal(SegmentSemanticRole.ProjectNumber, _repo.GetType("project_number")!.SemanticRole);
        Assert.Equal(SegmentSemanticRole.Date,          _repo.GetType("datum")!.SemanticRole);
        Assert.Equal(SegmentSemanticRole.Ignore,        _repo.GetType("ignore")!.SemanticRole);
        Assert.Equal(SegmentSemanticRole.Description,   _repo.GetType("description")!.SemanticRole);
        Assert.Equal(SegmentSemanticRole.None,          _repo.GetType("planart")!.SemanticRole);

        // Alle 9 Spatial-Built-ins
        string[] spatialIds = ["geschoss", "haus", "bauteil", "bauabschnitt", "stiege", "achse", "zone", "block", "objekt"];
        foreach (var id in spatialIds)
        {
            Assert.Equal(SegmentSemanticRole.Spatial, _repo.GetType(id)!.SemanticRole);
        }
    }

    [Fact]
    public void Seed_TokenKey_EqualsId_ForBuiltins()
    {
        _sut.Seed();

        foreach (var t in _repo.LoadAllTypes())
        {
            Assert.Equal(t.Id, t.TokenKey);
        }
    }

    [Fact]
    public void Seed_AllBuiltinsHaveIsBuiltinTrue()
    {
        _sut.Seed();

        foreach (var t in _repo.LoadAllTypes())
        {
            Assert.True(t.IsBuiltin);
        }
        foreach (var g in _repo.LoadAllGroups())
        {
            Assert.True(g.IsBuiltin);
        }
    }

    [Fact]
    public void Seed_Idempotent_SecondRun_DoesNotDuplicate()
    {
        _sut.Seed();
        _sut.Seed();

        Assert.Equal(5, _repo.LoadAllGroups().Count);
        Assert.Equal(16, _repo.LoadAllTypes().Count);
    }

    [Fact]
    public void Seed_UserModifiedName_NotOverwrittenOnReseed()
    {
        _sut.Seed();

        var t = _repo.GetType("plan_number")!;
        t.Name = "Plan-Nr.";
        t.UserModifiedName = true;
        _repo.SaveType(t);

        _sut.Seed();

        var reloaded = _repo.GetType("plan_number")!;
        Assert.Equal("Plan-Nr.", reloaded.Name);
        Assert.True(reloaded.UserModifiedName);
    }

    [Fact]
    public void Seed_UserModifiedColor_NotOverwrittenOnReseed()
    {
        _sut.Seed();

        var t = _repo.GetType("geschoss")!;
        t.Color = "#FF00FF";
        t.UserModifiedColor = true;
        _repo.SaveType(t);

        _sut.Seed();

        var reloaded = _repo.GetType("geschoss")!;
        Assert.Equal("#FF00FF", reloaded.Color);
    }

    [Fact]
    public void Seed_UserModifiedGroup_NotOverwrittenOnReseed()
    {
        _sut.Seed();

        var t = _repo.GetType("planart")!;
        var originalGroup = t.GroupId;
        t.GroupId = SegmentTypeSeedService.GroupSonstiges;
        t.UserModifiedGroup = true;
        _repo.SaveType(t);

        _sut.Seed();

        var reloaded = _repo.GetType("planart")!;
        Assert.Equal(SegmentTypeSeedService.GroupSonstiges, reloaded.GroupId);
        Assert.NotEqual(originalGroup, reloaded.GroupId);
    }

    [Fact]
    public void Seed_SemanticRole_AlwaysOverwritten_EvenIfUserChangedName()
    {
        // SemanticRole bei Built-ins ist unveraenderlich — seed darf sie korrigieren,
        // selbst wenn der Name user-modifiziert ist.
        _sut.Seed();

        var t = _repo.GetType("geschoss")!;
        t.Name = "Akustik-Klasse";
        t.UserModifiedName = true;
        // simuliere defekten Eintrag: SemanticRole auf None gesetzt
        t.SemanticRole = SegmentSemanticRole.None;
        _repo.SaveType(t);

        _sut.Seed();

        var reloaded = _repo.GetType("geschoss")!;
        Assert.Equal("Akustik-Klasse", reloaded.Name); // Name bleibt user-modifiziert
        Assert.Equal(SegmentSemanticRole.Spatial, reloaded.SemanticRole); // Rolle wieder korrigiert
    }
}
