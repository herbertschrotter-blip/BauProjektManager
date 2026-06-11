using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer die typabhaengige Radial-Ebenenlogik (BPM-111.05 Slice 2b).
/// Spez: Ring 2 je ring2_source, Ring 3 nur raeumlich, zurueck auf Ring 1
/// schliesst Ring 3, Animation nur beim Erscheinen eines Rings.
/// </summary>
public class RadialSelectionControllerTests
{
    private static PlanDocumentType Typ(
        string name, Ring2Source ring2, params string[] categories) =>
        new("id-" + name, name, name, "#185FA5", ring2, 10, true,
            [.. categories.Select((c, i) => new PlanDocumentTypeCategory("c" + i, c, c, i))]);

    private static BuildingPart Part(string shortName, string folder, params string[] levels) => new()
    {
        Id = "p-" + shortName,
        ShortName = shortName,
        FolderName = folder,
        Levels = [.. levels.Select(l => new BuildingLevel { Name = l })]
    };

    private static RadialSelectionController Make() => new(
        [
            Typ("Polierplan", Ring2Source.BuildingParts),
            Typ("Protokolle", Ring2Source.Categories, "Baubesprechung", "Sicherheit"),
            Typ("Lageplan", Ring2Source.None)
        ],
        [
            Part("Haus 1", "Haus 1", "KG", "EG", "OG1"),
            Part("Haus 3", "Haus 3", "EG", "OG1"),
            Part("Allgemein", "Allgemein")
        ]);

    [Fact]
    public void Commit_SpatialType_Ring2AreBuildingParts_Animated()
    {
        var sut = Make();
        var update = sut.Commit(1, "Polierplan", null);

        Assert.NotNull(update.Ring2);
        Assert.Equal(["Haus 1", "Haus 3", "Allgemein"], update.Ring2!.Select(i => i.Name));
        Assert.True(update.Ring2Animate);
        Assert.Equal([], update.Ring3);
    }

    [Fact]
    public void Commit_CategoryType_Ring2AreCategories()
    {
        var sut = Make();
        var update = sut.Commit(1, "Protokolle", null);

        Assert.Equal(["Baubesprechung", "Sicherheit"], update.Ring2!.Select(i => i.Name));
    }

    [Fact]
    public void Commit_NoneType_HasNoRing2()
    {
        var sut = Make();
        var update = sut.Commit(1, "Lageplan", null);

        Assert.Equal([], update.Ring2);
        Assert.False(update.Ring2Animate);
    }

    [Fact]
    public void Commit_TypeContentSwitch_DoesNotReanimateRing2()
    {
        // Spez: Inhaltswechsel bei sichtbarem Ring 2 bleibt stumm
        var sut = Make();
        Assert.True(sut.Commit(1, "Polierplan", null).Ring2Animate);
        var second = sut.Commit(1, "Protokolle", null);

        Assert.False(second.Ring2Animate);
        Assert.Equal("Baubesprechung", second.Ring2![0].Name);
    }

    [Fact]
    public void Commit_BackToRing1_ClosesRing3()
    {
        var sut = Make();
        sut.Commit(1, "Polierplan", null);
        var ring3 = sut.Commit(2, "Haus 1", null);
        Assert.Equal(3, ring3.Ring3!.Count);
        Assert.True(ring3.Ring3Animate);

        // zurueck auf Ring 1 (gleicher Typ): Ring 3 schliesst, Ring 2 bleibt stumm
        var back = sut.Commit(1, "Polierplan", null);
        Assert.Equal([], back.Ring3);
        Assert.False(back.Ring2Animate);
        Assert.Null(sut.SelectedPart);
    }

    [Fact]
    public void Commit_Ring3ReappearsAnimated_AfterClose()
    {
        var sut = Make();
        sut.Commit(1, "Polierplan", null);
        sut.Commit(2, "Haus 1", null);
        sut.Commit(1, "Polierplan", null);          // Ring 3 zu
        var again = sut.Commit(2, "Haus 3", null);  // Ring 3 wieder auf

        Assert.True(again.Ring3Animate);
        Assert.Equal(2, again.Ring3!.Count);
    }

    [Fact]
    public void Commit_PartWithoutLevels_HasNoRing3()
    {
        var sut = Make();
        sut.Commit(1, "Polierplan", null);
        var update = sut.Commit(2, "Allgemein", null);

        Assert.Equal([], update.Ring3);
        Assert.False(update.Ring3Animate);
    }

    [Fact]
    public void BuildRing1_MarksExtractorCandidate()
    {
        var sut = Make();
        var candidates = new PlanFileCandidates("x.pdf", "5998-200", null,
            RevisionKind.None, "OG3", null, ["Polierplan"], null, false, false);

        var ring1 = sut.BuildRing1(candidates);

        Assert.True(ring1.Single(i => i.Name == "Polierplan").IsCandidate);
        Assert.False(ring1.Single(i => i.Name == "Protokolle").IsCandidate);
    }

    [Fact]
    public void BuildTargetDirectory_Spatial_UsesFolderNames()
    {
        var sut = Make();
        sut.Commit(1, "Polierplan", null);
        sut.Commit(2, "Haus 1", null);
        sut.Commit(3, "OG1", null);

        Assert.Equal("Pläne/Polierplan/Haus 1/OG1", sut.BuildTargetDirectory("Pläne"));
    }

    [Fact]
    public void BuildTargetDirectory_Category_NoLevelSegment()
    {
        var sut = Make();
        sut.Commit(1, "Protokolle", null);
        sut.Commit(2, "Baubesprechung", null);

        Assert.Equal("Pläne/Protokolle/Baubesprechung", sut.BuildTargetDirectory("Pläne"));
    }
}
