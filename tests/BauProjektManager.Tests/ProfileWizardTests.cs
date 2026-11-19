using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.Tests;

/// <summary>
/// Profil-Wizard nach Mockup 3.8 v0.2 (BPM-130): 4 Schritte, Segment-Editor, Kopiermarker, Index optional,
/// positionsfreie Erkennung, Speichern am Dokumenttyp (ersetzt ein vorhandenes Profil).
/// </summary>
public class ProfileWizardTests
{
    private sealed class FakeCatalog : ISegmentTypeCatalog
    {
        private readonly Dictionary<string, SegmentTypeDefinition> _byId = new()
        {
            ["plan_number"] = new() { Id = "plan_number", Name = "Plannummer", TokenKey = "plannummer", SemanticRole = SegmentSemanticRole.PlanNumber },
            ["plan_index"] = new() { Id = "plan_index", Name = "Index", TokenKey = "index", SemanticRole = SegmentSemanticRole.PlanIndex },
            ["planart"] = new() { Id = "planart", Name = "Planart", TokenKey = "planart" },
            ["geschoss"] = new() { Id = "geschoss", Name = "Geschoss", TokenKey = "geschoss", SemanticRole = SegmentSemanticRole.Spatial },
            [SegmentTypeIds.Ignore] = new() { Id = SegmentTypeIds.Ignore, Name = "Ignorieren", TokenKey = "ignore", SemanticRole = SegmentSemanticRole.Ignore },
        };

        public IReadOnlyList<SegmentTypeDefinition> GetEffectiveActive() => [.. _byId.Values];
        public SegmentTypeDefinition? GetIncludingDeleted(string id) => _byId.GetValueOrDefault(id);
        public IReadOnlyDictionary<string, SegmentTypeDefinition> SnapshotIncludingDeleted() => _byId;
        public IReadOnlyList<SegmentTypeGroupDefinition> GetActiveGroups() => [];
        public void Invalidate() { }
        public event EventHandler? Changed { add { } remove { } }
    }

    private sealed class FakeProfileManager(params RecognitionProfile[] vorhanden) : IProfileManager
    {
        public RecognitionProfile? Saved { get; private set; }
        public Exception? Fehler { get; init; }
        public List<RecognitionProfile> LoadAll(string projectRootPath) => [.. vorhanden];
        public RecognitionProfile? LoadById(string projectRootPath, string profileId) => null;
        public bool Delete(string projectRootPath, string profileId) => false;

        public void Save(string projectRootPath, RecognitionProfile profile)
        {
            if (Fehler is not null)
                throw Fehler;
            Saved = profile;
        }
    }

    private static readonly FakeCatalog Catalog = new();

    private static readonly PlanDocumentType Polierplan =
        new("TYP-01", "Polierplan", "Polierplaene", null, Ring2Source.BuildingParts, 10, true, []);

    private static ProfileWizardViewModel Wizard(FakeProfileManager? pm = null)
    {
        var project = new Project { Id = "P1", Name = "Test", Paths = new ProjectPaths { Root = @"C:\bpm-wizard-test-nicht-da" } };
        return new ProfileWizardViewModel(project, pm ?? new FakeProfileManager(), segmentTypeCatalog: Catalog);
    }

    private static readonly (string, string?)[] VollZugewiesen =
    [
        ("6100-140", "plan_number"), ("B", "plan_index"), ("Polierplan", "planart"), ("EG", "geschoss")
    ];

    /// <summary>Wizard bis Schritt 2 mit Editor-Stand.</summary>
    private static ProfileWizardViewModel ImSchritt2(FakeProfileManager? pm = null)
    {
        var vm = Wizard(pm);
        vm.SelectedDocumentType = Polierplan;
        vm.SampleFileName = "6100-140_B_Polierplan_EG_(1).pdf";
        vm.GoNextCommand.Execute(null);
        return vm;
    }

    // ── Kopiermarker ─────────────────────────────────────────────

    [Theory]
    [InlineData("6100-140_B_Polierplan_H1_EG_(1).pdf", "6100-140_B_Polierplan_H1_EG.pdf", "_(1)")]
    [InlineData("Plan (2).dwg", "Plan.dwg", " (2)")]
    [InlineData("Plan - Kopie.pdf", "Plan.pdf", " - Kopie")]
    [InlineData("Plan - kopie (3).pdf", "Plan.pdf", " - kopie (3)")]
    [InlineData("(1).pdf", "(1).pdf", null)]
    [InlineData("6100-101_Polierplan.pdf", "6100-101_Polierplan.pdf", null)]
    public void StripCopyMarker_RemovesOnlyTheMarker(string name, string ohne, string? marker)
        => Assert.Equal((ohne, marker), FileNameSegmentation.StripCopyMarker(name));

    [Fact]
    public void Step1_CopyMarker_IsCutBeforeTheEditor_AndNeedsTypeAndFile()
    {
        var vm = Wizard();
        vm.SampleFileName = "6100-140_B_Polierplan_EG_(1).pdf";

        Assert.Equal("6100-140_B_Polierplan_EG.pdf", vm.EditorFileName);
        Assert.Equal("_(1)", vm.CopyMarker);
        Assert.False(vm.CanGoNext); // Dokumenttyp fehlt

        vm.SelectedDocumentType = Polierplan;
        Assert.True(vm.CanGoNext);
        Assert.Equal("Erkennung für „Polierplan“ einrichten", vm.DialogTitle);
        Assert.Equal(4, vm.TotalSteps);
    }

    // ── Segment-Editor: Trenner fuers Profil, Sammel-Zuweisung ───

    [Fact]
    public void Editor_Delimiters_MixedSeparators_AndAssignAllOpen()
    {
        var editor = new SegmentEditorViewModel();
        editor.Load("61-00-140_B.pdf", Catalog, null);
        Assert.Equal(['-', '_'], editor.ActiveDelimiters().Order());
        Assert.False(editor.HasMixedSeparators());

        editor.ToggleSeparator(0); // „61“ + „00“ verbinden, das zweite „-“ trennt weiter
        Assert.True(editor.HasMixedSeparators());
        Assert.Equal(['_'], editor.ActiveDelimiters());

        editor.ToggleSeparatorChar('-'); // alle „-“ einheitlich
        editor.AssignAllOpen(Catalog.GetIncludingDeleted(SegmentTypeIds.Ignore)!);
        Assert.All(editor.Tokens(), t => Assert.Equal(SegmentTypeIds.Ignore, t.AssignedType?.Id));
    }

    // ── Schritt 2 ────────────────────────────────────────────────

    [Fact]
    public void Step2_OpenTilesBlock_IgnoreUnblocks()
    {
        var vm = ImSchritt2();
        vm.ApplyEditorState([("6100-140", "plan_number"), ("B", null), ("Polierplan", null), ("EG", null)], ['_'], false);

        Assert.False(vm.CanGoNext);
        Assert.Equal(3, vm.OpenTileCount);
        Assert.True(vm.CanIgnoreOpen);
        Assert.Contains("B, Polierplan, EG", vm.Step2Message);

        vm.ApplyEditorState([("6100-140", "plan_number"), ("B", SegmentTypeIds.Ignore), ("Polierplan", SegmentTypeIds.Ignore),
            ("EG", SegmentTypeIds.Ignore)], ['_'], false);
        Assert.True(vm.CanGoNext);
    }

    [Theory]
    [InlineData(true, false, "einheitlich")]
    [InlineData(false, true, "Nur eine Kachel")]
    public void Step2_MixedSeparatorsOrTwoPlanNumbers_Block(bool mixed, bool zweiNummern, string meldung)
    {
        var vm = ImSchritt2();
        (string, string?)[] tokens = zweiNummern
            ? [("6100-140", "plan_number"), ("B", "plan_number"), ("Polierplan", "planart"), ("EG", "geschoss")]
            : VollZugewiesen;

        vm.ApplyEditorState(tokens, ['_'], mixed);

        Assert.False(vm.CanGoNext);
        Assert.Contains(meldung, vm.Step2Message);
    }

    [Fact]
    public void Step2_TilesThatTheImportParserWouldSplitDifferently_Block()
    {
        var vm = ImSchritt2();

        vm.ApplyEditorState(VollZugewiesen, ['-', '_'], false); // „-“ als Trenner zerlegt 6100-140 beim Import

        Assert.False(vm.CanGoNext);
        Assert.Contains("nicht als Erkennung speichern", vm.Step2Message);
    }

    // ── Schritt 3: Index optional (Praxis-Bug) ───────────────────

    [Fact]
    public void Step3_IndexInFileName_WithoutIndexSegment_WarnsButAllowsNext()
    {
        var vm = ImSchritt2();
        vm.ApplyEditorState([("6100-140", "plan_number"), ("B", SegmentTypeIds.Ignore), ("Polierplan", "planart"),
            ("EG", "geschoss")], ['_'], false);
        vm.GoNextCommand.Execute(null);

        Assert.Equal(3, vm.CurrentStep);
        Assert.True(vm.ShowIndexWarning);
        Assert.True(vm.CanGoNext);

        vm.SelectedIndexSource = IndexSourceType.None;
        Assert.False(vm.ShowIndexWarning);
    }

    // ── Schritt 4 + Speichern ────────────────────────────────────

    [Fact]
    public void Step4_PreselectsTypeName_SavesPositionFreeRules_OnDocumentType()
    {
        var pm = new FakeProfileManager();
        var vm = ImSchritt2(pm);
        vm.ApplyEditorState(VollZugewiesen, ['_'], false);
        vm.GoNextCommand.Execute(null);
        vm.GoNextCommand.Execute(null);

        Assert.Equal(4, vm.CurrentStep);
        Assert.Equal(["Polierplan"], vm.RecognitionSegments.Where(s => s.IsSelected).Select(s => s.RawValue));
        Assert.Contains("an beliebiger Stelle", vm.RecognitionPattern);
        Assert.True(vm.PatternTestSuccess);

        vm.SaveProfileCommand.Execute(null);

        Assert.True(vm.ProfileSaved);
        var p = pm.Saved!;
        Assert.Equal(("TYP-01", "Polierplan"), (p.DocumentTypeId, p.DocumentTypeName));
        var rule = Assert.Single(p.Recognition);
        Assert.Equal(("Polierplan", (int?)null), (rule.Pattern, rule.SegmentPosition));
        Assert.Equal(["_"], p.Tokenization!.Delimiters);
        Assert.Equal("optional", p.IndexMode);
        Assert.True(p.Segments.Single(s => s.FieldTypeId == "plan_number").Required);
    }

    [Fact]
    public void Save_ExistingProfileForType_IsReplaced_NotDuplicated()
    {
        var alt = new RecognitionProfile { Id = "ALT-1", DocumentTypeId = "TYP-01", DocumentTypeName = "Polierplan" };
        var pm = new FakeProfileManager(alt);
        var vm = ImSchritt2(pm);
        Assert.Contains("wird beim Speichern ersetzt", vm.ExistingProfileNote);
        vm.ApplyEditorState(VollZugewiesen, ['_'], false);
        vm.GoNextCommand.Execute(null);
        vm.GoNextCommand.Execute(null);

        vm.SaveProfileCommand.Execute(null);

        Assert.Equal("ALT-1", pm.Saved!.Id);
    }

    [Fact]
    public void Save_GuardRejects_ShowsErrorAndStaysOpen()
    {
        var pm = new FakeProfileManager { Fehler = new InvalidOperationException("Profil ist ungueltig: x") };
        var vm = ImSchritt2(pm);
        vm.ApplyEditorState(VollZugewiesen, ['_'], false);
        vm.GoNextCommand.Execute(null);
        vm.GoNextCommand.Execute(null);

        vm.SaveProfileCommand.Execute(null);

        Assert.False(vm.ProfileSaved);
        Assert.Contains("Profil ist ungueltig", vm.SaveError);
    }
}
