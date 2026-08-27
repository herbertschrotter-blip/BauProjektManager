using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests der DWG-PDF-Paar-Findung fuer die Vorschau (BPM-111.06 Slice C3):
/// pure Logik ohne DB/Dateisystem — der Archiv-Fall (GetPdfPathForRevision)
/// ist in <see cref="PlanArchiveRepositoryTests"/> abgedeckt.
/// </summary>
public class PreviewSourceTests
{
    private static CaptureRowViewModel Row(string fileName, string extension)
    {
        var scan = new ScannedFile($"_Eingang/{fileName}", fileName, extension, 100, DateTime.UtcNow);
        var candidates = new PlanFileCandidates(
            fileName, PlanNumber: null, Index: null, RevisionKind.None,
            Level: null, BuildingPartHint: null, TypeKeywords: [],
            DateCandidate: null, HasCopyMarker: false, IsCombi: false);
        var item = new CaptureItem(
            new FingerprintedFile(scan, "md5-" + fileName),
            candidates, CaptureBucket.NewCapture, Match: null, Reason: null);
        return new CaptureRowViewModel(item);
    }

    [Fact]
    public void FindPairedPdfRow_SameStem_ReturnsPdfPartner()
    {
        var dwg = Row("5998-202_EG.dwg", ".dwg");
        var pdf = Row("5998-202_EG.pdf", ".pdf");
        var other = Row("5998-306_OG1.pdf", ".pdf");

        var partner = ManualCaptureViewModel.FindPairedPdfRow(dwg, [other, pdf, dwg]);

        Assert.Same(pdf, partner);
    }

    [Fact]
    public void FindPairedPdfRow_CaseInsensitiveStemAndExtension()
    {
        var dwg = Row("5998-202_eg.DWG", ".DWG");
        var pdf = Row("5998-202_EG.PDF", ".PDF");

        Assert.Same(pdf, ManualCaptureViewModel.FindPairedPdfRow(dwg, [pdf, dwg]));
    }

    [Fact]
    public void FindPairedPdfRow_NoPdfWithSameStem_ReturnsNull()
    {
        var dwg = Row("5998-202_EG.dwg", ".dwg");
        var otherPdf = Row("5998-306_OG1.pdf", ".pdf");
        var sameStemDwg = Row("5998-202_EG.dxf", ".dxf");

        Assert.Null(ManualCaptureViewModel.FindPairedPdfRow(dwg, [otherPdf, sameStemDwg, dwg]));
    }
}
