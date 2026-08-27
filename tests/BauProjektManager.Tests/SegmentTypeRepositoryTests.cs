using System.IO;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// Unit-Tests fuer <see cref="SegmentTypeRepository"/> (BPM-108 Phase A).
/// CRUD + Soft-Delete + TokenKey-UNIQUE.
/// </summary>
public class SegmentTypeRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteConnection _schemaConn;
    private readonly SegmentTypeRepository _sut;

    private sealed class FakeUserContext : IUserContext
    {
        public string UserId => "TEST\\user";
        public string DisplayName => "Test User";
        public UserContextSource Source => UserContextSource.Local;
    }

    public SegmentTypeRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bpm-segtype-{Guid.NewGuid():N}.db");
        var cs = $"Data Source={_dbPath}";
        _schemaConn = new SqliteConnection(cs);
        _schemaConn.Open();
        SegmentTypeRepository.CreateTables(_schemaConn);
        _sut = new SegmentTypeRepository(cs, new FakeUserContext());
    }

    public void Dispose()
    {
        _schemaConn.Dispose();
        // BPM-120 T0: gezielter Pool-Clear statt ClearAllPools (Parallellast-Flaky)
        using (var pc = new SqliteConnection($"Data Source={_dbPath}"))
            SqliteConnection.ClearPool(pc);
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { /* ignore */ }
    }

    // === GROUPS ===

    [Fact]
    public void SaveGroup_NewGroup_PersistsAllFields()
    {
        var g = new SegmentTypeGroupDefinition
        {
            Id = "grp_test",
            Name = "Test-Gruppe",
            SortOrder = 5,
            IsBuiltin = true,
            BuiltinVersion = 1
        };

        _sut.SaveGroup(g);

        var loaded = _sut.GetGroup("grp_test");
        Assert.NotNull(loaded);
        Assert.Equal("Test-Gruppe", loaded!.Name);
        Assert.Equal(5, loaded.SortOrder);
        Assert.True(loaded.IsBuiltin);
        Assert.True(loaded.IsActive);
        Assert.False(loaded.IsDeleted);
        Assert.Equal(0, loaded.SyncVersion);
    }

    [Fact]
    public void SaveGroup_ExistingGroup_BumpsSyncVersion()
    {
        var g = new SegmentTypeGroupDefinition { Id = "grp_a", Name = "A", SortOrder = 1 };
        _sut.SaveGroup(g);

        g.Name = "A2";
        _sut.SaveGroup(g);

        var loaded = _sut.GetGroup("grp_a")!;
        Assert.Equal("A2", loaded.Name);
        Assert.Equal(1, loaded.SyncVersion);
    }

    [Fact]
    public void SoftDeleteGroup_SetsIsDeletedAndBumpsSync()
    {
        _sut.SaveGroup(new SegmentTypeGroupDefinition { Id = "grp_x", Name = "X", SortOrder = 1 });

        _sut.SoftDeleteGroup("grp_x");

        Assert.Null(_sut.GetGroup("grp_x")); // ohne includeDeleted
        var raw = _sut.GetGroup("grp_x", includeDeleted: true);
        Assert.NotNull(raw);
        Assert.True(raw!.IsDeleted);
        Assert.Equal(1, raw.SyncVersion);
    }

    // === TYPES ===

    [Fact]
    public void SaveType_NewType_PersistsAllFields()
    {
        _sut.SaveGroup(new SegmentTypeGroupDefinition { Id = "grp_a", Name = "A", SortOrder = 10 });
        var t = new SegmentTypeDefinition
        {
            Id = "plan_number",
            Name = "Plannummer",
            Color = "#0F6E56",
            TokenKey = "plan_number",
            SemanticRole = SegmentSemanticRole.PlanNumber,
            GroupId = "grp_a",
            SortOrder = 10,
            IsBuiltin = true,
            BuiltinVersion = 1
        };

        _sut.SaveType(t);

        var loaded = _sut.GetType("plan_number");
        Assert.NotNull(loaded);
        Assert.Equal("Plannummer", loaded!.Name);
        Assert.Equal("#0F6E56", loaded.Color);
        Assert.Equal("plan_number", loaded.TokenKey);
        Assert.Equal(SegmentSemanticRole.PlanNumber, loaded.SemanticRole);
        Assert.Equal("grp_a", loaded.GroupId);
        Assert.True(loaded.IsBuiltin);
    }

    [Fact]
    public void SaveType_CustomWithNullSemanticRole_LoadsAsNull()
    {
        _sut.SaveGroup(new SegmentTypeGroupDefinition { Id = "grp_custom", Name = "Custom", SortOrder = 50 });
        _sut.SaveType(new SegmentTypeDefinition
        {
            Id = "01H_CUSTOM",
            Name = "Akustik-Klasse",
            Color = "#A87142",
            TokenKey = "akustik_klasse",
            SemanticRole = null,
            GroupId = "grp_custom",
            SortOrder = 10
        });

        var loaded = _sut.GetType("01H_CUSTOM")!;
        Assert.Null(loaded.SemanticRole);
        Assert.False(loaded.IsBuiltin);
    }

    [Fact]
    public void SoftDeleteType_RemovesFromActiveQuery_ButKeepsRow()
    {
        _sut.SaveGroup(new SegmentTypeGroupDefinition { Id = "grp_a", Name = "A", SortOrder = 1 });
        _sut.SaveType(new SegmentTypeDefinition { Id = "t1", Name = "T1", Color = "#000", TokenKey = "t1", GroupId = "grp_a" });

        _sut.SoftDeleteType("t1");

        Assert.Null(_sut.GetType("t1"));
        var raw = _sut.GetType("t1", includeDeleted: true);
        Assert.NotNull(raw);
        Assert.True(raw!.IsDeleted);
    }

    [Fact]
    public void TokenKeyExists_TrueForActiveDuplicate()
    {
        _sut.SaveGroup(new SegmentTypeGroupDefinition { Id = "grp_a", Name = "A", SortOrder = 1 });
        _sut.SaveType(new SegmentTypeDefinition { Id = "t1", Name = "T1", Color = "#000", TokenKey = "shared", GroupId = "grp_a" });

        Assert.True(_sut.TokenKeyExists("shared"));
        Assert.False(_sut.TokenKeyExists("not_used"));
        Assert.False(_sut.TokenKeyExists("shared", excludingId: "t1"));
    }

    [Fact]
    public void TokenKeyExists_FalseAfterSoftDelete()
    {
        _sut.SaveGroup(new SegmentTypeGroupDefinition { Id = "grp_a", Name = "A", SortOrder = 1 });
        _sut.SaveType(new SegmentTypeDefinition { Id = "t1", Name = "T1", Color = "#000", TokenKey = "freed", GroupId = "grp_a" });
        _sut.SoftDeleteType("t1");

        Assert.False(_sut.TokenKeyExists("freed"));
    }

    [Fact]
    public void LoadAllTypes_OrdersBySortOrder()
    {
        _sut.SaveGroup(new SegmentTypeGroupDefinition { Id = "grp_a", Name = "A", SortOrder = 1 });
        _sut.SaveType(new SegmentTypeDefinition { Id = "c", Name = "C", Color = "#000", TokenKey = "c", GroupId = "grp_a", SortOrder = 30 });
        _sut.SaveType(new SegmentTypeDefinition { Id = "a", Name = "A", Color = "#000", TokenKey = "a", GroupId = "grp_a", SortOrder = 10 });
        _sut.SaveType(new SegmentTypeDefinition { Id = "b", Name = "B", Color = "#000", TokenKey = "b", GroupId = "grp_a", SortOrder = 20 });

        var list = _sut.LoadAllTypes();

        Assert.Equal(new[] { "a", "b", "c" }, list.Select(t => t.Id).ToArray());
    }
}
