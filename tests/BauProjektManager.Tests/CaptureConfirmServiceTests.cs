using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer das reine Pending-&gt;ImportDecision-Mapping des
/// <see cref="CaptureConfirmService"/> (BPM-111.04). Ohne DB/Dateisystem —
/// die Ausfuehrung selbst laeuft ueber die bestehende (separat getestete)
/// ImportExecutionService-Strecke.
/// </summary>
public class CaptureConfirmServiceTests
{
    private static readonly PlanValueNormalizer _normalizer = new();

    private static FingerprintedFile File(string fileName) =>
        new(new ScannedFile($"_Eingang/{fileName}", fileName,
            System.IO.Path.GetExtension(fileName), 100, DateTime.UtcNow), "md5-1");

    [Fact]
    public void BuildDecisions_NewCapture_BuildsManualKeyAndStatusNew()
    {
        var p = new PendingAssignment(
            File("5998-300_OG2.pdf"), CaptureBucket.NewCapture,
            "polierplan", "Polierplan", "Haus 2", "OG2", "5998-300", null,
            "Pläne/Polierplan/Haus 2/OG2", Match: null);

        var d = Assert.Single(CaptureConfirmService.BuildDecisions([p], _normalizer));

        Assert.Equal(ImportStatus.New, d.Status);
        Assert.Equal("polierplan|5998_300|haus_2|og2", d.DocumentKey);
        Assert.Equal(System.IO.Path.Combine("Pläne/Polierplan/Haus 2/OG2", "5998-300_OG2.pdf"),
            d.TargetRelativePath);
        Assert.Equal(IndexSourceType.None, d.File.RevisionSource);
        Assert.Equal("5998-300", d.File.IdentityFields[SegmentTypeIds.PlanNumber]);
        Assert.Equal("Haus 2", d.File.IdentityFields[SegmentTypeIds.Bauteil]);
    }

    [Fact]
    public void BuildDecisions_UpdateProposal_UsesKnownDocumentKeyAndFolder()
    {
        var match = new KnownPlanDocument(
            "doc1", "polierplan|5998_100|haus_1", "5998-100", "Polierplan",
            "Polierplan", "Pläne/Polierplan/Haus 1/KG", "A", "rev1");
        var p = new PendingAssignment(
            File("5998-100-B_KG.pdf"), CaptureBucket.UpdateProposal,
            "polierplan", "Polierplan", "Haus 1", "KG", "5998-100", "B",
            "egal/wird/ignoriert", match);

        var d = Assert.Single(CaptureConfirmService.BuildDecisions([p], _normalizer));

        Assert.Equal(ImportStatus.UpdateNewerIndex, d.Status);
        Assert.Equal(match.DocumentKey, d.DocumentKey);
        Assert.Equal("rev1", d.ExistingRevisionId);
        Assert.StartsWith("Pläne/Polierplan/Haus 1/KG", d.TargetRelativePath);
        Assert.Equal("B", d.File.RevisionToken);
        Assert.Equal(RevisionKind.Alphabetic, d.File.RevisionKind);
        Assert.Equal(IndexSourceType.FileName, d.File.RevisionSource);
    }

    [Fact]
    public void BuildManualDocumentKey_WithoutPlanNumber_FallsBackToFileName()
    {
        // Protokolle ohne Plannummer: Dateiname sichert Eindeutigkeit
        var p = new PendingAssignment(
            File("BB_2026-04-15_Baubesprechung.pdf"), CaptureBucket.NewCapture,
            "protokolle", "Protokolle", "Baubesprechung", null, null, null,
            "Pläne/Protokolle/Baubesprechung", Match: null);

        var key = CaptureConfirmService.BuildManualDocumentKey(p, _normalizer);

        Assert.Equal("protokolle|bb_2026_04_15_baubesprechung|baubesprechung", key);
    }

    [Fact]
    public void BuildDecisions_KeyIsIndexFree()
    {
        // Invariante: Index ist NIE Teil des document_key
        var p = new PendingAssignment(
            File("5998-300-B_OG2.pdf"), CaptureBucket.NewCapture,
            "polierplan", "Polierplan", "Haus 2", "OG2", "5998-300", "B",
            "Pläne/Polierplan/Haus 2/OG2", Match: null);

        var d = Assert.Single(CaptureConfirmService.BuildDecisions([p], _normalizer));

        Assert.DoesNotContain("|b", d.DocumentKey);
        Assert.Equal("B", d.File.RevisionToken);
    }
}
