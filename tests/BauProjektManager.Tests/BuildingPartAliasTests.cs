using System.IO;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;
using BauProjektManager.Tests.Fakes;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// Bauteil-Aliasse (BPM-121.03, Seitenliste 3.3 v1.2): Tabelle building_part_aliases, Aufloesung Hinweis → Bauteil
/// (Alias vor Stammdatum, Normalisierung, UNIQUE-Kollision), Radial-Kandidat und Vorschlag nach dem Zuordnen.
/// </summary>
public class BuildingPartAliasTests : IDisposable
{
    private sealed class FakeUserContext : IUserContext
    {
        public string UserId => "TEST\\user";
        public string DisplayName => "Test User";
        public UserContextSource Source => UserContextSource.Local;
    }

    private sealed class FakeDeviceContext : IDeviceContext
    {
        public string DeviceId => "01TESTDEVICE";
        public string DeviceName => "TestDevice";
    }

    private readonly string _bpmDbPath = Path.Combine(Path.GetTempPath(), $"bpm-alias-{Guid.NewGuid():N}.db");
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"bpm-alias-root-{Guid.NewGuid():N}");
    private readonly ProjectDatabase _bpmDb;
    private readonly PlanManagerDatabase _planDb;
    private readonly string _projectId;
    private readonly string _haus2;

    public BuildingPartAliasTests()
    {
        _bpmDb = new ProjectDatabase(new UlidIdGenerator(), new FakeUserContext(),
            new FakeDeviceContext(), persistenceRegistry: null, dbPathOverride: _bpmDbPath);
        var project = new Project { Name = "Testprojekt" };
        _bpmDb.SaveProject(project);
        _projectId = project.Id;
        _bpmDb.InsertBuildingPart(_projectId, "Haus 1");
        _haus2 = _bpmDb.InsertBuildingPart(_projectId, "Haus 2");
        IIdGenerator idGen = new UlidIdGenerator();
        _planDb = new PlanManagerDatabase(_projectId, idGen, dbPathOverride: TempDb.NewTempDbPath(idGen.NewId()));
        Directory.CreateDirectory(Path.Combine(_root, "_Eingang"));
    }

    public void Dispose()
    {
        _bpmDb.Dispose();
        using (var pc = new SqliteConnection($"Data Source={_bpmDbPath}"))
            SqliteConnection.ClearPool(pc);
        TempDb.Delete(_bpmDbPath);
        var planPath = _planDb.GetDatabasePath();
        _planDb.Dispose();
        using (var pc = new SqliteConnection($"Data Source={planPath}"))
            SqliteConnection.ClearPool(pc);
        TempDb.Delete(planPath);
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    private BuildingPartHintResolver Resolver()
        => new(_bpmDb.GetBuildingParts(_projectId), _bpmDb.GetBuildingPartAliases(_projectId), new PlanValueNormalizer());

    // ── Tabelle + Aufloesung ─────────────────────────────────────

    [Fact]
    public void AddAlias_IsNormalized_AndResolvesToThePart()
    {
        Assert.True(_bpmDb.AddBuildingPartAlias(_projectId, _haus2, " H2 "));

        Assert.Equal("Haus 2", Resolver().Resolve("h-2")!.ShortName); // gleiche Normalform wie „H2“
        Assert.True(Resolver().IsAlias("H2"));
    }

    [Fact]
    public void AddAlias_SamePartTwice_IsNoOp_OtherPart_Throws()
    {
        _bpmDb.AddBuildingPartAlias(_projectId, _haus2, "H2");

        Assert.False(_bpmDb.AddBuildingPartAlias(_projectId, _haus2, "h2"));
        var haus1 = _bpmDb.GetBuildingParts(_projectId).Single(p => p.ShortName == "Haus 1").Id;
        Assert.Throws<InvalidOperationException>(() => _bpmDb.AddBuildingPartAlias(_projectId, haus1, "H2"));
    }

    [Fact]
    public void Resolve_AliasWinsOverMasterDataName()
    {
        // „Haus 1“ als Schreibweise bewusst auf Haus 2 gemerkt: der Alias wird vor dem Stammdatum geprueft
        _bpmDb.AddBuildingPartAlias(_projectId, _haus2, "Haus 1");

        Assert.Equal(_haus2, Resolver().Resolve("Haus 1")!.Id);
    }

    [Fact]
    public void Resolve_WithoutAlias_MatchesMasterDataTolerant_ElseNull()
    {
        Assert.Equal(_haus2, Resolver().Resolve("HAUS_2")!.Id);
        Assert.Null(Resolver().Resolve("H2"));
        Assert.Null(Resolver().Resolve(null));
    }

    // ── Radial + Vorschlag ───────────────────────────────────────

    private static CaptureRowViewModel Row(string fileName, string? hint)
    {
        var scan = new ScannedFile($"_Eingang/{fileName}", fileName, ".pdf", 100, DateTime.UtcNow);
        var candidates = new PlanFileCandidates(fileName, null, null, RevisionKind.None, null, hint, [], null, false, false);
        return new CaptureRowViewModel(new CaptureItem(
            new FingerprintedFile(scan, "md5-" + fileName), candidates, CaptureBucket.NewCapture, null, null));
    }

    private async Task<ManualCaptureViewModel> Vm()
    {
        var vm = new ManualCaptureViewModel(_planDb, _bpmDb, new UlidIdGenerator());
        await vm.InitializeAsync(_projectId, _root, "_Eingang", "Plaene");
        return vm;
    }

    private static void Zuordnen(ManualCaptureViewModel vm, CaptureRowViewModel anker, string teil)
    {
        var controller = vm.BeginCapture(anker)!;
        controller.Commit(1, vm.Types.First(t => t.Ring2Source == Ring2Source.BuildingParts).Name, anker.Item.Candidates);
        controller.Commit(2, teil, anker.Item.Candidates);
        vm.CompleteCapture(controller);
    }

    [Fact]
    public async Task Radial_MarksThePartFromAliasAsCandidate()
    {
        _bpmDb.AddBuildingPartAlias(_projectId, _haus2, "H2");
        var vm = await Vm();
        var row = Row("S-201_Schalung_H2_EG.pdf", "H2");
        vm.Rows.Add(row);
        row.IsSelected = true;

        var controller = vm.BeginCapture(row)!;
        var update = controller.Commit(1, vm.Types.First(t => t.Ring2Source == Ring2Source.BuildingParts).Name,
            row.Item.Candidates);

        Assert.Equal(["Haus 2"], update.Ring2!.Where(i => i.IsCandidate).Select(i => i.Name));
    }

    [Fact]
    public async Task AfterAssigning_UnknownHint_GetsSuggestion_RememberStoresAlias()
    {
        var vm = await Vm();
        var a = Row("S-201_Schalung_H2_EG.pdf", "H2");
        var b = Row("S-202_Schalung_H2_OG1.pdf", "H2");
        vm.Rows.Add(a);
        vm.Rows.Add(b);
        a.IsSelected = true;
        b.IsSelected = true;

        Zuordnen(vm, a, "Haus 2");

        Assert.Equal(("H2", "Haus 2"), (a.AliasSuggestion!.Hint, a.AliasSuggestion.PartName));
        a.IsSelected = true;
        vm.SetSelectedRow();
        Assert.Equal("„H2“ → Haus 2 — als Alias merken?", vm.SelectedDetail!.AliasQuestion);
        Assert.Contains("anderen 1 Datei(en)", vm.SelectedDetail.AliasExplanation);

        vm.RememberAliasCommand.Execute(null);

        Assert.Equal(_haus2, _bpmDb.GetBuildingPartAliases(_projectId)["h2"]);
        Assert.Null(a.AliasSuggestion);
        Assert.Null(b.AliasSuggestion); // gilt fuer alle Dateien mit „H2“
        Assert.Equal("Bauteil aus dem Dateinamen: „H2“ = Haus 2 (Alias)", vm.SelectedDetail!.HintText);
    }

    [Fact]
    public async Task Decline_RemovesSuggestion_AndDoesNotAskAgain()
    {
        var vm = await Vm();
        var a = Row("S-201_Schalung_H2_EG.pdf", "H2");
        vm.Rows.Add(a);
        a.IsSelected = true;
        Zuordnen(vm, a, "Haus 2");
        a.IsSelected = true;
        vm.SetSelectedRow();

        vm.DeclineAliasCommand.Execute(null);

        Assert.Null(a.AliasSuggestion);
        Assert.Empty(_bpmDb.GetBuildingPartAliases(_projectId));
        a.IsSelected = true;
        Zuordnen(vm, a, "Haus 2");
        Assert.Null(a.AliasSuggestion);
    }

    [Fact]
    public async Task KnownHint_GetsNoSuggestion()
    {
        var vm = await Vm();
        var a = Row("S-201_Schalung_Haus2_EG.pdf", "Haus2"); // passt schon zum Stammdatum „Haus 2“
        vm.Rows.Add(a);
        a.IsSelected = true;

        Zuordnen(vm, a, "Haus 2");

        Assert.Null(a.AliasSuggestion);
    }
}
