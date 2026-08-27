using System.IO;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.ViewModels;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer <see cref="SegmentTypeManagerViewModel"/> (BPM-108 Phase C Teil 3).
/// Edit / Save / Toggle / Delete / Create.
/// </summary>
public class SegmentTypeManagerViewModelTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteConnection _schemaConn;
    private readonly SegmentTypeRepository _repo;
    private readonly SegmentTypeCatalog _catalog;
    private readonly SegmentTypeManagerViewModel _sut;

    private sealed class FixedIdGenerator : IIdGenerator
    {
        private int _i;
        public string NewId() => $"CUST-{++_i:D2}";
    }

    private sealed class FakeUserContext : IUserContext
    {
        public string UserId => "TEST";
        public string DisplayName => "Test";
        public UserContextSource Source => UserContextSource.Local;
    }

    public SegmentTypeManagerViewModelTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bpm-mgr-{Guid.NewGuid():N}.db");
        var cs = $"Data Source={_dbPath}";
        _schemaConn = new SqliteConnection(cs);
        _schemaConn.Open();
        SegmentTypeRepository.CreateTables(_schemaConn);
        _repo = new SegmentTypeRepository(cs, new FakeUserContext());
        new SegmentTypeSeedService(_repo).Seed();
        _catalog = new SegmentTypeCatalog(_repo);
        _sut = new SegmentTypeManagerViewModel(_repo, _catalog, new FixedIdGenerator());
    }

    public void Dispose()
    {
        _schemaConn.Dispose();
        // BPM-120 T0: gezielter Pool-Clear statt ClearAllPools (Parallellast-Flaky)
        using (var pc = new SqliteConnection($"Data Source={_dbPath}"))
            SqliteConnection.ClearPool(pc);
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void OnConstruction_GroupsBuiltFromSeed()
    {
        Assert.Equal(5, _sut.Groups.Count); // 4 fachliche + grp_eigene
        Assert.Equal(5, _sut.AvailableGroups.Count);
        Assert.Equal(16, _sut.Groups.Sum(g => g.Items.Count));
    }

    [Fact]
    public void SelectType_BuiltIn_ExposesRoleAndToken()
    {
        var planNumber = _repo.GetType("plan_number")!;

        _sut.SelectType(planNumber);

        Assert.True(_sut.IsSelectionBuiltin);
        Assert.False(_sut.IsSelectionCustom);
        Assert.Equal("Plannummer", _sut.SemanticRoleDisplay);
        Assert.Contains("erforderlich", _sut.SemanticRoleInfo);
        Assert.Equal("plan_number", _sut.TokenKeyDisplay);
        Assert.Equal(planNumber.Name, _sut.NameDraft);
        Assert.False(_sut.IsDirty);
    }

    [Fact]
    public void SaveDraft_NameChange_BuiltIn_SetsUserModifiedNameFlag()
    {
        var planNumber = _repo.GetType("plan_number")!;
        _sut.SelectType(planNumber);
        _sut.NameDraft = "Plan-Nr.";

        Assert.True(_sut.IsDirty);

        _sut.SaveDraftCommand.Execute(null);

        var reloaded = _repo.GetType("plan_number")!;
        Assert.Equal("Plan-Nr.", reloaded.Name);
        Assert.True(reloaded.UserModifiedName);
        Assert.False(reloaded.UserModifiedColor);
        Assert.False(_sut.IsDirty);
    }

    [Fact]
    public void CancelDraft_ResetsToCurrentValues()
    {
        var planNumber = _repo.GetType("plan_number")!;
        _sut.SelectType(planNumber);
        _sut.NameDraft = "XYZ";
        Assert.True(_sut.IsDirty);

        _sut.CancelDraftCommand.Execute(null);

        Assert.Equal("Plannummer", _sut.NameDraft);
        Assert.False(_sut.IsDirty);
    }

    [Fact]
    public void ToggleTypeActive_FlipsAndSetsUserModifiedActive()
    {
        var ignore = _repo.GetType("ignore")!;
        Assert.True(ignore.IsActive);

        _sut.ToggleTypeActive(ignore);

        var reloaded = _repo.GetType("ignore")!;
        Assert.False(reloaded.IsActive);
        Assert.True(reloaded.UserModifiedActive);
    }

    [Fact]
    public void ToggleGroupActive_FlipsAndSetsFlag()
    {
        var raeumlich = _repo.GetGroup(SegmentTypeSeedService.GroupRaeumlich)!;
        Assert.True(raeumlich.IsActive);

        _sut.ToggleGroupActive(raeumlich);

        var reloaded = _repo.GetGroup(SegmentTypeSeedService.GroupRaeumlich)!;
        Assert.False(reloaded.IsActive);
        Assert.True(reloaded.UserModifiedActive);
    }

    [Fact]
    public void CreateNewCustomCommand_PersistsAndAutoSelectsNewType()
    {
        var beforeCount = _repo.LoadAllTypes().Count;

        _sut.CreateNewCustomCommand.Execute(null);

        var afterCount = _repo.LoadAllTypes().Count;
        Assert.Equal(beforeCount + 1, afterCount);
        Assert.NotNull(_sut.SelectedType);
        Assert.False(_sut.SelectedType!.IsBuiltin);
        Assert.Equal(SegmentTypeSeedService.GroupEigene, _sut.SelectedType.GroupId);
    }

    [Fact]
    public void CreateNewCustom_TwoTimes_TokenKeySuffixed()
    {
        _sut.CreateNewCustomCommand.Execute(null);
        _sut.CreateNewCustomCommand.Execute(null);

        var customs = _repo.LoadAllTypes()
            .Where(t => t.GroupId == SegmentTypeSeedService.GroupEigene)
            .OrderBy(t => t.TokenKey)
            .ToList();
        Assert.Equal(2, customs.Count);
        Assert.Equal("neuer_segmenttyp", customs[0].TokenKey);
        Assert.Equal("neuer_segmenttyp_2", customs[1].TokenKey);
    }

    [Fact]
    public void DeleteSelectedCustomCommand_SoftDeletesAndClearsSelection()
    {
        _sut.CreateNewCustomCommand.Execute(null);
        var created = _sut.SelectedType!;
        var id = created.Id;

        _sut.DeleteSelectedCustomCommand.Execute(null);

        Assert.Null(_sut.SelectedType);
        // Soft-delete: row exists, is_deleted=1
        var stillThere = _repo.GetType(id, includeDeleted: true);
        Assert.NotNull(stillThere);
        Assert.True(stillThere!.IsDeleted);
        // EffectiveActive ohne deleted
        Assert.DoesNotContain(_repo.LoadAllTypes(), t => t.Id == id);
    }

    [Fact]
    public void DeleteSelectedCustomCommand_OnBuiltin_NoOp()
    {
        var planNumber = _repo.GetType("plan_number")!;
        _sut.SelectType(planNumber);

        _sut.DeleteSelectedCustomCommand.Execute(null);

        // Built-in nicht geloescht
        var stillThere = _repo.GetType("plan_number");
        Assert.NotNull(stillThere);
        Assert.False(stillThere!.IsDeleted);
    }
}
