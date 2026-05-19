using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.Tests;

/// <summary>
/// Unit-Tests fuer <see cref="ProfileWizardViewModel.IsLikelyVariableSegment"/>
/// (BPM-082.04, Konsens R2 / BPM-108 Phase C).
/// </summary>
/// <remarks>
/// Heuristik:
/// - Segment-Typ <see cref="SegmentSemanticRole.PlanNumber"/>, <see cref="SegmentSemanticRole.PlanIndex"/>
///   oder <see cref="SegmentSemanticRole.Date"/> → variabel.
/// - Token rein numerisch → variabel.
/// - Token als Datum parsbar → variabel.
/// - Sonst → nicht variabel.
/// </remarks>
public class ProfileWizardVariableSegmentTests
{
    /// <summary>In-memory ISegmentTypeCatalog mit fester Rollen-Map fuer Tests.</summary>
    private sealed class FakeCatalog : ISegmentTypeCatalog
    {
        private readonly Dictionary<string, SegmentTypeDefinition> _byId = new();

        public FakeCatalog Add(string id, SegmentSemanticRole? role)
        {
            _byId[id] = new SegmentTypeDefinition { Id = id, Name = id, TokenKey = id, SemanticRole = role };
            return this;
        }

        public IReadOnlyList<SegmentTypeDefinition> GetEffectiveActive() => _byId.Values.ToList();
        public SegmentTypeDefinition? GetIncludingDeleted(string id) =>
            _byId.TryGetValue(id, out var def) ? def : null;
        public IReadOnlyDictionary<string, SegmentTypeDefinition> SnapshotIncludingDeleted() => _byId;
        public IReadOnlyList<SegmentTypeGroupDefinition> GetActiveGroups() => [];
        public void Invalidate() { }
        public event EventHandler? Changed { add { } remove { } }
    }

    private static FakeCatalog DefaultCatalog() => new FakeCatalog()
        .Add("plan_number", SegmentSemanticRole.PlanNumber)
        .Add("plan_index", SegmentSemanticRole.PlanIndex)
        .Add("project_number", SegmentSemanticRole.ProjectNumber)
        .Add("datum", SegmentSemanticRole.Date)
        .Add("planart", SegmentSemanticRole.None)
        .Add("haus", SegmentSemanticRole.Spatial)
        .Add("geschoss", SegmentSemanticRole.Spatial)
        .Add("bauteil", SegmentSemanticRole.Spatial);

    private static (ProfileWizardViewModel vm, RecognitionSegment seg) MakeVm(
        int position,
        string rawValue,
        string? fieldTypeId = null,
        ISegmentTypeCatalog? catalog = null)
    {
        var vm = new ProfileWizardViewModel(segmentTypeCatalog: catalog ?? DefaultCatalog());
        vm.Segments.Add(new FileNameSegment
        {
            Position = position,
            RawValue = rawValue,
            FieldTypeId = fieldTypeId
        });
        var seg = new RecognitionSegment(position, rawValue);
        return (vm, seg);
    }

    // === SemanticRole-Trigger ===

    [Theory]
    [InlineData("plan_number")]
    [InlineData("plan_index")]
    [InlineData("datum")]
    public void IsLikelyVariable_VariableRoles_ReturnTrue(string fieldTypeId)
    {
        var (vm, seg) = MakeVm(0, "irgendwas", fieldTypeId);

        Assert.True(vm.IsLikelyVariableSegment(seg));
    }

    [Theory]
    [InlineData("project_number")]
    [InlineData("planart")]
    [InlineData("haus")]
    [InlineData("geschoss")]
    [InlineData("bauteil")]
    public void IsLikelyVariable_StableRoles_NonNumericValue_ReturnFalse(string fieldTypeId)
    {
        var (vm, seg) = MakeVm(0, "P", fieldTypeId);

        Assert.False(vm.IsLikelyVariableSegment(seg));
    }

    // === Numerisch-Trigger ===

    [Theory]
    [InlineData("011")]
    [InlineData("5998")]
    [InlineData("1")]
    [InlineData("202401")]
    public void IsLikelyVariable_PurelyNumericToken_ReturnsTrue(string value)
    {
        var (vm, seg) = MakeVm(0, value);

        Assert.True(vm.IsLikelyVariableSegment(seg));
    }

    [Theory]
    [InlineData("PROT")]
    [InlineData("EG")]
    [InlineData("PP01")]
    [InlineData("H64")]
    [InlineData("002a")]
    [InlineData("B-Gew")]
    public void IsLikelyVariable_AlphanumericOrMixed_NoFieldType_ReturnsFalse(string value)
    {
        var (vm, seg) = MakeVm(0, value);

        Assert.False(vm.IsLikelyVariableSegment(seg));
    }

    // === DateTime-Trigger ===

    [Theory]
    [InlineData("2025-10-14")]
    [InlineData("14.07.2025")]
    [InlineData("17.02.2026")]
    public void IsLikelyVariable_ParsableDateToken_ReturnsTrue(string value)
    {
        var (vm, seg) = MakeVm(0, value);

        Assert.True(vm.IsLikelyVariableSegment(seg));
    }

    // === Edge Cases ===

    [Fact]
    public void IsLikelyVariable_EmptyValue_ReturnsFalse()
    {
        var (vm, seg) = MakeVm(0, "");

        Assert.False(vm.IsLikelyVariableSegment(seg));
    }

    [Fact]
    public void IsLikelyVariable_WhitespaceValue_ReturnsFalse()
    {
        var (vm, seg) = MakeVm(0, "   ");

        Assert.False(vm.IsLikelyVariableSegment(seg));
    }

    [Fact]
    public void IsLikelyVariable_NoMatchingSegmentInVm_FallsBackToTokenChecks()
    {
        var vm = new ProfileWizardViewModel(segmentTypeCatalog: DefaultCatalog());
        var seg = new RecognitionSegment(5, "PROT");

        Assert.False(vm.IsLikelyVariableSegment(seg));
    }

    [Fact]
    public void IsLikelyVariable_NoMatchingSegmentInVm_NumericToken_StillTrue()
    {
        var vm = new ProfileWizardViewModel(segmentTypeCatalog: DefaultCatalog());
        var seg = new RecognitionSegment(99, "12345");

        Assert.True(vm.IsLikelyVariableSegment(seg));
    }

    // === Reale Wizard-Szenarien ===

    [Fact]
    public void IsLikelyVariable_RealCase_OWG_Polierplan_P_NotVariable()
    {
        var (vm, seg) = MakeVm(1, "P", "planart");

        Assert.False(vm.IsLikelyVariableSegment(seg));
    }

    [Fact]
    public void IsLikelyVariable_RealCase_OWG_Polierplan_PlanNumber_Variable()
    {
        var (vm, seg) = MakeVm(2, "011", "plan_number");

        Assert.True(vm.IsLikelyVariableSegment(seg));
    }

    [Fact]
    public void IsLikelyVariable_RealCase_Schlosspark_AP_NotVariable()
    {
        var (vm, seg) = MakeVm(2, "AP", "planart");

        Assert.False(vm.IsLikelyVariableSegment(seg));
    }

    [Fact]
    public void IsLikelyVariable_RealCase_Schlosspark_PlanIndex_Variable()
    {
        var (vm, seg) = MakeVm(6, "03", "plan_index");

        Assert.True(vm.IsLikelyVariableSegment(seg));
    }
}
