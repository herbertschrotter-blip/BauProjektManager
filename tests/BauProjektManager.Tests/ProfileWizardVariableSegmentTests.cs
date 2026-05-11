using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.Tests;

/// <summary>
/// Unit-Tests für <see cref="ProfileWizardViewModel.IsLikelyVariableSegment"/>
/// (BPM-082.04, Konsens R2).
///
/// Heuristik aus Review R2:
/// - FieldType ist PlanNumber, PlanIndex oder Datum → variabel
/// - Token besteht nur aus Ziffern → variabel
/// - Token parst als Datum → variabel
/// - sonst → nicht variabel
/// </summary>
public class ProfileWizardVariableSegmentTests
{
    private static (ProfileWizardViewModel vm, RecognitionSegment seg) MakeVm(
        int position,
        string rawValue,
        FieldType? fieldType = null)
    {
        var vm = new ProfileWizardViewModel();
        // Segments-Liste mit FieldType am gleichen Position-Index
        vm.Segments.Add(new FileNameSegment
        {
            Position = position,
            RawValue = rawValue,
            FieldType = fieldType
        });
        var seg = new RecognitionSegment(position, rawValue);
        return (vm, seg);
    }

    // === FieldType-Trigger ===

    [Theory]
    [InlineData(FieldType.PlanNumber)]
    [InlineData(FieldType.PlanIndex)]
    [InlineData(FieldType.Datum)]
    public void IsLikelyVariable_VariableFieldTypes_ReturnTrue(FieldType ft)
    {
        var (vm, seg) = MakeVm(0, "irgendwas", ft);

        Assert.True(vm.IsLikelyVariableSegment(seg));
    }

    [Theory]
    [InlineData(FieldType.ProjectNumber)]
    [InlineData(FieldType.Planart)]
    [InlineData(FieldType.Haus)]
    [InlineData(FieldType.Geschoss)]
    [InlineData(FieldType.Bauteil)]
    public void IsLikelyVariable_StableFieldTypes_NonNumericValue_ReturnFalse(FieldType ft)
    {
        var (vm, seg) = MakeVm(0, "P", ft);

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
    [InlineData("PP01")]   // gemischt — nicht rein numerisch
    [InlineData("H64")]    // gemischt
    [InlineData("002a")]   // gemischt
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
        // VM hat KEIN Segment an Position 5 — FieldType-Lookup liefert null,
        // dann greift der Token-Check. "PROT" ist nicht numerisch / kein Datum → false.
        var vm = new ProfileWizardViewModel();
        var seg = new RecognitionSegment(5, "PROT");

        Assert.False(vm.IsLikelyVariableSegment(seg));
    }

    [Fact]
    public void IsLikelyVariable_NoMatchingSegmentInVm_NumericToken_StillTrue()
    {
        // Auch ohne FieldType-Mapping: rein numerischer Token → variabel
        var vm = new ProfileWizardViewModel();
        var seg = new RecognitionSegment(99, "12345");

        Assert.True(vm.IsLikelyVariableSegment(seg));
    }

    // === Reale Wizard-Szenarien aus PlanListe ===

    [Fact]
    public void IsLikelyVariable_RealCase_OWG_Polierplan_P_NotVariable()
    {
        // 202401_P_011_Haus64: User markiert Pos 1 = "P" als Erkennungsmuster
        // → keine Warnung
        var (vm, seg) = MakeVm(1, "P", FieldType.Planart);

        Assert.False(vm.IsLikelyVariableSegment(seg));
    }

    [Fact]
    public void IsLikelyVariable_RealCase_OWG_Polierplan_PlanNumber_Variable()
    {
        // User markiert Pos 2 = "011" — das ist die Plannummer → variabel
        var (vm, seg) = MakeVm(2, "011", FieldType.PlanNumber);

        Assert.True(vm.IsLikelyVariableSegment(seg));
    }

    [Fact]
    public void IsLikelyVariable_RealCase_Schlosspark_AP_NotVariable()
    {
        // 21005_101_AP_H1_GR_U1: Pos 2 = "AP" als Erkennungsmuster
        var (vm, seg) = MakeVm(2, "AP", FieldType.Planart);

        Assert.False(vm.IsLikelyVariableSegment(seg));
    }

    [Fact]
    public void IsLikelyVariable_RealCase_Schlosspark_PlanIndex_Variable()
    {
        // 21005_101_AP_H1_GR_U1_03: Pos 6 = "03" (Index) → variabel
        var (vm, seg) = MakeVm(6, "03", FieldType.PlanIndex);

        Assert.True(vm.IsLikelyVariableSegment(seg));
    }
}
