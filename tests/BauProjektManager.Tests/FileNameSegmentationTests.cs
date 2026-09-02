using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Pure Zerlegung fuer den Segment-Editor (BPM-126c): Atome + Trenner,
/// Verschmelzen bei deaktiviertem Trenner, stabiler Anker fuer Zuweisungen.
/// </summary>
public class FileNameSegmentationTests
{
    [Fact]
    public void Split_SeparatesAtAllSeparatorChars()
    {
        var result = FileNameSegmentation.Split("6100-140_B_Polierplan_H1_EG.pdf");

        Assert.Equal(
            ["6100", "140", "B", "Polierplan", "H1", "EG", "pdf"],
            result.Atoms.ToArray());
        Assert.Equal(['-', '_', '_', '_', '_', '.'], result.Separators.ToArray());
    }

    [Fact]
    public void Merge_AllSeparatorsActive_KeepsEveryAtomSeparate()
    {
        var source = FileNameSegmentation.Split("6100-140_B.pdf");
        var merged = FileNameSegmentation.Merge(source, [true, true, true]);

        Assert.Equal(["6100", "140", "B", "pdf"], merged.Select(m => m.Text).ToArray());
    }

    [Fact]
    public void Merge_InactiveSeparator_JoinsNeighboursIncludingSeparatorChar()
    {
        // "5998-130" entsteht durch Deaktivieren des '-' — genau der Mockup-Fall
        var source = FileNameSegmentation.Split("6100-140_B.pdf");
        var merged = FileNameSegmentation.Merge(source, [false, true, true]);

        Assert.Equal(["6100-140", "B", "pdf"], merged.Select(m => m.Text).ToArray());
    }

    [Fact]
    public void Merge_StartAtomIndex_StaysStableAcrossMerge()
    {
        var source = FileNameSegmentation.Split("6100-140_B.pdf");

        var split = FileNameSegmentation.Merge(source, [true, true, true]);
        var joined = FileNameSegmentation.Merge(source, [false, true, true]);

        // "B" bleibt an Atom-Index 2 verankert, obwohl sich links etwas aendert
        Assert.Equal(2, split.Single(m => m.Text == "B").StartAtomIndex);
        Assert.Equal(2, joined.Single(m => m.Text == "B").StartAtomIndex);
    }

    [Fact]
    public void Merge_MultipleInactiveInARow_JoinsAll()
    {
        var source = FileNameSegmentation.Split("A-B-C");
        var merged = FileNameSegmentation.Merge(source, [false, false]);

        Assert.Equal(["A-B-C"], merged.Select(m => m.Text).ToArray());
    }

    [Fact]
    public void InitialState_HonoursGlobalSeparatorChoice()
    {
        var source = FileNameSegmentation.Split("6100-140_B.pdf");

        // Nur '_' und '.' global aktiv -> der Bindestrich trennt nicht
        var state = FileNameSegmentation.InitialState(source, "_.");

        Assert.Equal([false, true, true], state.ToArray());
        Assert.Equal(["6100-140", "B", "pdf"],
            FileNameSegmentation.Merge(source, state).Select(m => m.Text).ToArray());
    }

    [Fact]
    public void Split_NameWithoutSeparators_YieldsSingleAtom()
    {
        var source = FileNameSegmentation.Split("Plan");

        Assert.Equal(["Plan"], source.Atoms.ToArray());
        Assert.Empty(source.Separators);
        Assert.Equal(["Plan"], FileNameSegmentation.Merge(source, []).Select(m => m.Text).ToArray());
    }

    [Fact]
    public void Merge_WrongStateLength_Throws()
    {
        var source = FileNameSegmentation.Split("A-B");
        Assert.Throws<ArgumentException>(() => FileNameSegmentation.Merge(source, [true, true]));
    }
}
