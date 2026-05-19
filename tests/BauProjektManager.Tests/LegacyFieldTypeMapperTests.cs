using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;
using System.Reflection;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer den BPM-108 Phase-B Compat-Shim <c>LegacyFieldTypeMapper</c>.
/// Verifiziert dass die Wizard-Enum-Werte korrekt auf snake_case-IDs uebersetzt werden.
/// Da der Mapper <c>internal</c> ist, wird er via Reflection getestet.
/// </summary>
public class LegacyFieldTypeMapperTests
{
    private static readonly Type MapperType =
        typeof(ProfileManager).Assembly.GetType("BauProjektManager.PlanManager.Services.LegacyFieldTypeMapper")!;

    private static string EnumToId(FieldType type) =>
        (string)MapperType.GetMethod("EnumToId", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [type])!;

    private static bool IsIdentityRelevant(FieldType? type) =>
        (bool)MapperType.GetMethod("IsIdentityRelevant", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [type])!;

    private static string NormalizeTokenKey(string name) =>
        (string)MapperType.GetMethod("NormalizeTokenKey", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [name])!;

    private static string ToFieldTypeId(FileNameSegment seg) =>
        (string)MapperType.GetMethod("ToFieldTypeId", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [seg])!;

    [Theory]
    [InlineData(FieldType.PlanNumber, "plan_number")]
    [InlineData(FieldType.PlanIndex, "plan_index")]
    [InlineData(FieldType.ProjectNumber, "project_number")]
    [InlineData(FieldType.Description, "description")]
    [InlineData(FieldType.Ignore, "ignore")]
    [InlineData(FieldType.Datum, "datum")]
    [InlineData(FieldType.Geschoss, "geschoss")]
    [InlineData(FieldType.Haus, "haus")]
    [InlineData(FieldType.Planart, "planart")]
    [InlineData(FieldType.Objekt, "objekt")]
    [InlineData(FieldType.Bauteil, "bauteil")]
    [InlineData(FieldType.Bauabschnitt, "bauabschnitt")]
    [InlineData(FieldType.Stiege, "stiege")]
    [InlineData(FieldType.Achse, "achse")]
    [InlineData(FieldType.Zone, "zone")]
    [InlineData(FieldType.Block, "block")]
    public void EnumToId_MappsBuiltinFieldTypes(FieldType type, string expectedId)
    {
        Assert.Equal(expectedId, EnumToId(type));
    }

    [Theory]
    [InlineData(FieldType.PlanNumber, true)]
    [InlineData(FieldType.Geschoss, true)]
    [InlineData(FieldType.Haus, true)]
    [InlineData(FieldType.Bauteil, true)]
    [InlineData(FieldType.Bauabschnitt, true)]
    [InlineData(FieldType.Stiege, true)]
    [InlineData(FieldType.Achse, true)]
    [InlineData(FieldType.Zone, true)]
    [InlineData(FieldType.Block, true)]
    [InlineData(FieldType.Objekt, true)]
    [InlineData(FieldType.PlanIndex, false)]
    [InlineData(FieldType.ProjectNumber, false)]
    [InlineData(FieldType.Description, false)]
    [InlineData(FieldType.Planart, false)]
    [InlineData(FieldType.Datum, false)]
    [InlineData(FieldType.Ignore, false)]
    [InlineData(FieldType.Custom, false)]
    public void IsIdentityRelevant_OnlyPlanNumberAndSpatial(FieldType type, bool expected)
    {
        Assert.Equal(expected, IsIdentityRelevant(type));
    }

    [Fact]
    public void IsIdentityRelevant_Null_False()
    {
        Assert.False(IsIdentityRelevant(null));
    }

    [Theory]
    [InlineData("Akustik-Klasse", "akustik_klasse")]
    [InlineData("Brandschutz Klasse", "brandschutz_klasse")]
    [InlineData("ÄÖÜß", "aeoeuess")]
    [InlineData("  spaces  ", "spaces")]
    [InlineData("multi___underscore", "multi_underscore")]
    public void NormalizeTokenKey_GeneratesSnakeCase(string input, string expected)
    {
        Assert.Equal(expected, NormalizeTokenKey(input));
    }

    [Fact]
    public void ToFieldTypeId_Custom_UsesCustomFieldName()
    {
        var seg = new FileNameSegment
        {
            Position = 0,
            FieldType = FieldType.Custom,
            CustomFieldName = "Akustik-Klasse"
        };

        Assert.Equal("akustik_klasse", ToFieldTypeId(seg));
    }

    [Fact]
    public void ToFieldTypeId_CustomWithoutName_FallbackCustom()
    {
        var seg = new FileNameSegment { FieldType = FieldType.Custom };
        Assert.Equal("custom", ToFieldTypeId(seg));
    }

    [Fact]
    public void ToFieldTypeId_NullFieldType_ReturnsEmpty()
    {
        var seg = new FileNameSegment { FieldType = null };
        Assert.Equal(string.Empty, ToFieldTypeId(seg));
    }

    [Fact]
    public void ToFieldTypeId_KnownEnum_UsesMapping()
    {
        var seg = new FileNameSegment { FieldType = FieldType.PlanNumber };
        Assert.Equal("plan_number", ToFieldTypeId(seg));
    }
}
