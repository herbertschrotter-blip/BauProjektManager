using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests für die ADR-061-Slice-0.6a-Mapping-Regel (<see cref="ImportPlanBuilder.MapRings"/>):
/// erkannte Felder → Ring2/Ring3-Tokens. Ring3 = geschoss; Ring2(BuildingParts) =
/// erster Bauteil-Id-Wert; Ring2(Categories) = erster Nicht-geschoss-Wert.
/// </summary>
public class ImportPlanBuilderMappingTests
{
    [Fact]
    public void BuildingParts_MapsBuildingPartToRing2_AndGeschossToRing3()
    {
        var (ring2, ring3) = ImportPlanBuilder.MapRings(
            Ring2Source.BuildingParts,
            [SegmentTypeIds.Haus, SegmentTypeIds.Geschoss],
            new Dictionary<string, string>
            {
                [SegmentTypeIds.Haus] = "Haus A",
                [SegmentTypeIds.Geschoss] = "EG",
            });

        Assert.Equal("Haus A", ring2);
        Assert.Equal("EG", ring3);
    }

    [Fact]
    public void BuildingParts_WithoutGeschoss_Ring3Null()
    {
        var (ring2, ring3) = ImportPlanBuilder.MapRings(
            Ring2Source.BuildingParts,
            [SegmentTypeIds.Bauteil],
            new Dictionary<string, string> { [SegmentTypeIds.Bauteil] = "B1" });

        Assert.Equal("B1", ring2);
        Assert.Null(ring3);
    }

    [Fact]
    public void BuildingParts_PicksFirstBuildingPartId_RegardlessOfHierarchyOrder()
    {
        var (ring2, ring3) = ImportPlanBuilder.MapRings(
            Ring2Source.BuildingParts,
            [SegmentTypeIds.Geschoss, SegmentTypeIds.Block],
            new Dictionary<string, string>
            {
                [SegmentTypeIds.Block] = "Block 2",
                [SegmentTypeIds.Geschoss] = "OG1",
            });

        Assert.Equal("Block 2", ring2);
        Assert.Equal("OG1", ring3);
    }

    [Fact]
    public void Categories_PicksFirstNonGeschossValue()
    {
        var (ring2, ring3) = ImportPlanBuilder.MapRings(
            Ring2Source.Categories,
            ["kategorie"],
            new Dictionary<string, string> { ["kategorie"] = "Baubesprechung" });

        Assert.Equal("Baubesprechung", ring2);
        Assert.Null(ring3);
    }

    [Fact]
    public void None_ReturnsNoTokens()
    {
        var (ring2, ring3) = ImportPlanBuilder.MapRings(
            Ring2Source.None,
            [SegmentTypeIds.Haus],
            new Dictionary<string, string> { [SegmentTypeIds.Haus] = "Haus A" });

        Assert.Null(ring2);
        Assert.Null(ring3);
    }
}
