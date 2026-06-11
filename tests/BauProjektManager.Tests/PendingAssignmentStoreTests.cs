using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer den In-Memory <see cref="PendingAssignmentStore"/> (BPM-111.04).
/// Undo Stufe 1 = Discard/Clear, Re-Assign ersetzt bestehende Zuordnung.
/// </summary>
public class PendingAssignmentStoreTests
{
    private static PendingAssignment Pa(string fileName, string target = "Pläne/Polierplan/Haus 1") =>
        new(new FingerprintedFile(
                new ScannedFile($"_Eingang/{fileName}", fileName, ".pdf", 100, DateTime.UtcNow), "md5"),
            CaptureBucket.NewCapture, "polierplan", "Polierplan",
            "Haus 1", null, "5998-200", null, target, Match: null);

    [Fact]
    public void Assign_ReassignSameFile_Replaces()
    {
        var store = new PendingAssignmentStore();
        store.Assign(Pa("a.pdf", "Pläne/Polierplan/Haus 1"));
        store.Assign(Pa("a.pdf", "Pläne/Polierplan/Haus 2"));

        Assert.Equal(1, store.Count);
        Assert.Equal("Pläne/Polierplan/Haus 2", store.Get("_Eingang/a.pdf")!.TargetRelativeDirectory);
    }

    [Fact]
    public void Discard_RemovesSingleAssignment()
    {
        var store = new PendingAssignmentStore();
        store.Assign(Pa("a.pdf"));
        store.Assign(Pa("b.pdf"));

        Assert.True(store.Discard("_Eingang/a.pdf"));
        Assert.False(store.Discard("_Eingang/a.pdf"));
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Clear_RemovesAll()
    {
        var store = new PendingAssignmentStore();
        store.Assign(Pa("a.pdf"));
        store.Assign(Pa("b.pdf"));

        store.Clear();

        Assert.Equal(0, store.Count);
        Assert.Empty(store.Snapshot());
    }
}
