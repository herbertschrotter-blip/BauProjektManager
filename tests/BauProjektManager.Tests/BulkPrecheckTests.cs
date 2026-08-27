using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests der Bulk-Vorprüfung (BPM-111.07 Slice B, „Hinweis + Deckel"):
/// Mengenwarnung ab 9, harter Deckel über 20, Kompatibilitäts-Warnungen
/// (gemischte Dateitypen, Plannummern-Kollision gleicher Dateitypen).
/// </summary>
public class BulkPrecheckTests
{
    private static CaptureRowViewModel Row(string fileName, string extension, string? planNr = null)
    {
        var scan = new ScannedFile($"_Eingang/{fileName}", fileName, extension, 100, DateTime.UtcNow);
        var candidates = new PlanFileCandidates(
            fileName, planNr, Index: null, RevisionKind.None,
            Level: null, BuildingPartHint: null, TypeKeywords: [],
            DateCandidate: null, HasCopyMarker: false, IsCombi: false);
        var item = new CaptureItem(
            new FingerprintedFile(scan, "md5-" + fileName),
            candidates, CaptureBucket.NewCapture, Match: null, Reason: null);
        return new CaptureRowViewModel(item);
    }

    private static List<CaptureRowViewModel> PdfRows(int count) =>
        [.. Enumerable.Range(1, count).Select(i => Row($"5998-{100 + i}_EG.pdf", ".pdf", $"5998-{100 + i}"))];

    [Fact]
    public void Evaluate_UpTo8_AllowedWithoutCountWarning()
    {
        var result = BulkPrecheck.Evaluate(PdfRows(8));

        Assert.Equal(BulkGate.Allowed, result.Gate);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Evaluate_9To20_AllowedWithCountWarning()
    {
        var nine = BulkPrecheck.Evaluate(PdfRows(9));
        var twenty = BulkPrecheck.Evaluate(PdfRows(20));

        Assert.Equal(BulkGate.Allowed, nine.Gate);
        Assert.Contains(nine.Warnings, w => w.StartsWith("9 Dateien"));
        Assert.Equal(BulkGate.Allowed, twenty.Gate);
        Assert.Contains(twenty.Warnings, w => w.StartsWith("20 Dateien"));
    }

    [Fact]
    public void Evaluate_MoreThan20_Blocked()
    {
        var result = BulkPrecheck.Evaluate(PdfRows(21));

        Assert.Equal(BulkGate.Blocked, result.Gate);
        Assert.Contains("21", result.BlockReason);
    }

    [Fact]
    public void Evaluate_MixedForeignExtensions_Warns()
    {
        var rows = new List<CaptureRowViewModel>
        {
            Row("5998-101_EG.pdf", ".pdf", "5998-101"),
            Row("Aufmass.xlsx", ".xlsx"),
        };

        var result = BulkPrecheck.Evaluate(rows);

        Assert.Equal(BulkGate.Allowed, result.Gate);
        Assert.Contains(result.Warnings, w => w.Contains(".xlsx"));
    }

    [Fact]
    public void Evaluate_SamePlanNumberSameExtension_WarnsCollision()
    {
        var rows = new List<CaptureRowViewModel>
        {
            Row("5998-101_EG.pdf", ".pdf", "5998-101"),
            Row("5998-101_EG_Kopie.pdf", ".pdf", "5998-101"),
        };

        var result = BulkPrecheck.Evaluate(rows);

        Assert.Contains(result.Warnings, w => w.Contains("5998-101"));
    }

    [Fact]
    public void Evaluate_PdfDwgPairSameNumber_NoCollisionWarning()
    {
        // Paar (verschiedene Extensions) ist gewollt — keine Kollisionswarnung
        var rows = new List<CaptureRowViewModel>
        {
            Row("5998-101_EG.pdf", ".pdf", "5998-101"),
            Row("5998-101_EG.dwg", ".dwg", "5998-101"),
        };

        var result = BulkPrecheck.Evaluate(rows);

        Assert.Empty(result.Warnings);
    }
}
