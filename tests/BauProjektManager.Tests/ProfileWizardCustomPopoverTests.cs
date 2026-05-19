using System.IO;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.ViewModels;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer den Inline-Popover "+ Eigenes" im ProfileWizardViewModel
/// (BPM-108 Phase C Teil 2).
/// </summary>
public class ProfileWizardCustomPopoverTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteConnection _schemaConn;
    private readonly SegmentTypeRepository _repo;
    private readonly SegmentTypeCatalog _catalog;
    private readonly ProfileWizardViewModel _vm;

    private sealed class FixedIdGenerator : IIdGenerator
    {
        private int _counter;
        public string NewId() => $"CUSTOM-{++_counter:D2}";
    }

    private sealed class FakeUserContext : IUserContext
    {
        public string UserId => "TEST";
        public string DisplayName => "Test";
        public UserContextSource Source => UserContextSource.Local;
    }

    public ProfileWizardCustomPopoverTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bpm-popover-{Guid.NewGuid():N}.db");
        var cs = $"Data Source={_dbPath}";
        _schemaConn = new SqliteConnection(cs);
        _schemaConn.Open();
        SegmentTypeRepository.CreateTables(_schemaConn);
        _repo = new SegmentTypeRepository(cs, new FakeUserContext());
        new SegmentTypeSeedService(_repo).Seed();
        _catalog = new SegmentTypeCatalog(_repo);

        _vm = new ProfileWizardViewModel(
            segmentTypeCatalog: _catalog,
            segmentTypeRepository: _repo,
            idGenerator: new FixedIdGenerator());
    }

    public void Dispose()
    {
        _schemaConn.Dispose();
        SqliteConnection.ClearAllPools();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void OpenCustomPopover_SetsShowFlag_AndResetsFields()
    {
        _vm.CustomTypeName = "alter wert";
        _vm.CustomTypeError = "alter fehler";

        _vm.OpenCustomPopover();

        Assert.True(_vm.ShowCustomPopover);
        Assert.Equal(string.Empty, _vm.CustomTypeName);
        Assert.Equal(string.Empty, _vm.CustomTypeError);
    }

    [Fact]
    public void CustomTypeTokenPreview_UpdatesLive()
    {
        _vm.CustomTypeName = "Akustik-Klasse";

        Assert.Equal("akustik_klasse", _vm.CustomTypeTokenPreview);
    }

    [Fact]
    public void CreateCustomType_EmptyName_SetsError()
    {
        _vm.OpenCustomPopover();
        _vm.CustomTypeName = "";

        _vm.CreateCustomTypeCommand.Execute(null);

        Assert.True(_vm.ShowCustomPopover);
        Assert.Equal("Name ist erforderlich.", _vm.CustomTypeError);
    }

    [Fact]
    public void CreateCustomType_ValidName_AddsTypeAndClosesPopover()
    {
        _vm.OpenCustomPopover();
        _vm.CustomTypeName = "Akustik-Klasse";
        _vm.CustomTypeColor = "#A87142";

        _vm.CreateCustomTypeCommand.Execute(null);

        Assert.False(_vm.ShowCustomPopover);
        var saved = _repo.LoadAllTypes().FirstOrDefault(t => t.TokenKey == "akustik_klasse");
        Assert.NotNull(saved);
        Assert.Equal("Akustik-Klasse", saved!.Name);
        Assert.Equal("#A87142", saved.Color);
        Assert.Equal(SegmentTypeSeedService.GroupEigene, saved.GroupId);
        Assert.False(saved.IsBuiltin);
        Assert.Null(saved.SemanticRole); // Custom rein dekorativ
    }

    [Fact]
    public void CreateCustomType_TokenConflict_AppendsSuffix()
    {
        // erst Custom mit "Akustik-Klasse" → token_key = "akustik_klasse"
        _vm.OpenCustomPopover();
        _vm.CustomTypeName = "Akustik-Klasse";
        _vm.CreateCustomTypeCommand.Execute(null);

        // nochmal mit gleichem Namen → suffix _2
        _vm.OpenCustomPopover();
        _vm.CustomTypeName = "Akustik-Klasse";
        _vm.CreateCustomTypeCommand.Execute(null);

        var customs = _repo.LoadAllTypes()
            .Where(t => t.GroupId == SegmentTypeSeedService.GroupEigene)
            .OrderBy(t => t.TokenKey)
            .ToList();
        Assert.Equal(2, customs.Count);
        Assert.Equal("akustik_klasse", customs[0].TokenKey);
        Assert.Equal("akustik_klasse_2", customs[1].TokenKey);
    }

    [Fact]
    public void CreateCustomType_WithAssignmentTarget_AssignsToSegment()
    {
        var seg = new FileNameSegment { Position = 3, RawValue = "OG1", FieldTypeId = null };
        _vm.Segments.Add(seg);

        _vm.OpenCustomPopover(assignmentTarget: seg);
        _vm.CustomTypeName = "Akustik-Klasse";
        _vm.CreateCustomTypeCommand.Execute(null);

        Assert.NotNull(seg.FieldTypeId);
        Assert.StartsWith("CUSTOM-", seg.FieldTypeId);
        // FieldTypeOption muss sofort als IsAssigned markiert sein
        var opt = _vm.FieldTypeOptions.FirstOrDefault(o => o.FieldTypeId == seg.FieldTypeId);
        Assert.NotNull(opt);
        Assert.True(opt!.IsAssigned);
    }

    [Fact]
    public void CreateCustomType_TriggersCatalogInvalidate_AndOptionsUpdate()
    {
        var initialCount = _vm.FieldTypeOptions.Count;

        _vm.OpenCustomPopover();
        _vm.CustomTypeName = "Brandschutz-Klasse";
        _vm.CreateCustomTypeCommand.Execute(null);

        // Catalog wurde invalidiert → ChangedEvent → FieldTypeOptions rebuilt
        Assert.True(_vm.FieldTypeOptions.Count > initialCount);
        Assert.Contains(_vm.FieldTypeOptions, o => o.DisplayName == "Brandschutz-Klasse");
    }

    [Fact]
    public void CancelCustomPopover_ClosesAndDiscardsName()
    {
        _vm.OpenCustomPopover();
        _vm.CustomTypeName = "Brandschutz";

        _vm.CancelCustomPopoverCommand.Execute(null);

        Assert.False(_vm.ShowCustomPopover);
        Assert.Equal(string.Empty, _vm.CustomTypeName);
        // Es wurde nichts persistiert
        Assert.DoesNotContain(_repo.LoadAllTypes(), t => t.Name == "Brandschutz");
    }

    [Fact]
    public void CreateCustomType_WithoutRepository_SetsErrorMessage()
    {
        var isolatedVm = new ProfileWizardViewModel(segmentTypeCatalog: _catalog);
        isolatedVm.OpenCustomPopover();
        isolatedVm.CustomTypeName = "Test";

        isolatedVm.CreateCustomTypeCommand.Execute(null);

        Assert.True(isolatedVm.ShowCustomPopover);
        Assert.Contains("nicht moeglich", isolatedVm.CustomTypeError);
    }
}
