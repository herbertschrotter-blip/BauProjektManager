using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Unit-Tests für <see cref="ManualFirstCaptureService.MatchByNumber"/> (BPM-111.06 Slice A2):
/// pure Matching-Logik für den Einzel-Re-Match nach Panel-Edit der Plannummer/Index.
/// Kein Setup nötig, keine Disk/DB.
/// </summary>
public class ManualFirstCaptureMatchTests
{
    private static readonly IPlanValueNormalizer _norm = new PlanValueNormalizer();

    private static KnownPlanDocument Doc(
        string number, string? currentIndex, string key = "k", string id = "doc-1")
        => new(
            DocumentId: id,
            DocumentKey: key,
            PlanNumber: number,
            DocumentType: "Polierplan",
            TargetFolder: "Pläne/Polierplan",
            RelativeDirectory: "Pläne/Polierplan",
            CurrentIndex: currentIndex,
            CurrentRevisionId: "rev-1");

    private static IReadOnlyDictionary<string, List<KnownPlanDocument>> Lookup(
        params KnownPlanDocument[] docs)
        => docs.Where(d => !string.IsNullOrWhiteSpace(d.PlanNumber))
               .GroupBy(d => _norm.NormalizeForMatch(d.PlanNumber))
               .ToDictionary(g => g.Key, g => g.ToList());

    [Fact]
    public void MatchByNumber_UnknownNumber_ReturnsNewCapture()
    {
        var result = ManualFirstCaptureService.MatchByNumber(
            "9999-999", "A", Lookup(Doc("5998-201", "A")), _norm);

        Assert.Equal(CaptureBucket.NewCapture, result.Bucket);
        Assert.Null(result.Match);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void MatchByNumber_NoNumber_ReturnsNewCapture()
    {
        var result = ManualFirstCaptureService.MatchByNumber(
            null, null, Lookup(Doc("5998-201", "A")), _norm);

        Assert.Equal(CaptureBucket.NewCapture, result.Bucket);
    }

    [Fact]
    public void MatchByNumber_KnownNumber_NewerIndex_ReturnsUpdateProposal()
    {
        // bekannt: 5998-201 Index A; Edit -> Index B (neuer) => Update, keine OLDER-Warnung
        var result = ManualFirstCaptureService.MatchByNumber(
            "5998-201", "B", Lookup(Doc("5998-201", "A")), _norm);

        Assert.Equal(CaptureBucket.UpdateProposal, result.Bucket);
        Assert.NotNull(result.Match);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void MatchByNumber_KnownNumber_SameIndex_ReturnsConflict()
    {
        var result = ManualFirstCaptureService.MatchByNumber(
            "5998-201", "A", Lookup(Doc("5998-201", "A")), _norm);

        Assert.Equal(CaptureBucket.Conflict, result.Bucket);
        Assert.NotNull(result.Match);
        Assert.Contains("Gleicher Index", result.Reason);
    }

    [Fact]
    public void MatchByNumber_KnownNumber_OlderIndex_ReturnsUpdateWithOlderWarning()
    {
        // bekannt: Index C; Edit -> Index A (niedriger) => Update + OLDER_REVISION-Hinweis
        var result = ManualFirstCaptureService.MatchByNumber(
            "5998-201", "A", Lookup(Doc("5998-201", "C")), _norm);

        Assert.Equal(CaptureBucket.UpdateProposal, result.Bucket);
        Assert.Contains("OLDER_REVISION", result.Reason);
    }

    [Fact]
    public void MatchByNumber_NumberInMultipleDocs_ReturnsConflict()
    {
        var lookup = Lookup(
            Doc("5998-201", "A", key: "k1", id: "d1"),
            Doc("5998-201", "B", key: "k2", id: "d2"));

        var result = ManualFirstCaptureService.MatchByNumber("5998-201", "C", lookup, _norm);

        Assert.Equal(CaptureBucket.Conflict, result.Bucket);
        Assert.Null(result.Match);
        Assert.Contains("Auswahl", result.Reason);
    }
}
