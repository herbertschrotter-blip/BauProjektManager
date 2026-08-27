using System.IO;
using BauProjektManager.Infrastructure.Services;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests für <see cref="PdfiumPdfService"/> (ADR-062/063 Addendum Teil 47):
/// EINE Engine (PDFium/Docnet) für Rendern + Text-Extraktion. Das Test-PDF
/// wird mit PdfPigs Builder in-memory erzeugt (echte Textebene, keine Fixture) —
/// PdfPig ist hier nur noch Test-Werkzeug.
/// </summary>
public class PdfiumPdfServiceTests
{
    private readonly PdfiumPdfService _sut = new();

    /// <summary>A4 hoch, eine Textzeile bei x=50pt, Baseline y=700pt (von unten).</summary>
    private static byte[] BuildTestPdf()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(PageSize.A4);
        page.AddText("Plannummer 5998-100 Index B", 12, new PdfPoint(50, 700), font);
        return builder.Build();
    }

    [Fact]
    public async Task GetWords_ReadsWordsWithMmCoordinates()
    {
        var words = await _sut.GetWordsAsync(new MemoryStream(BuildTestPdf()), pageIndex: 0);

        Assert.Equal(["Plannummer", "5998-100", "Index", "B"],
            words.Select(w => w.Text).ToArray());

        // Alle Boxen liegen innerhalb des A4-Blatts (210 x 297 mm)
        Assert.All(words, w =>
        {
            Assert.InRange(w.XMm, 0, 210.5);
            Assert.InRange(w.YMm, 0, 297.5);
            Assert.True(w.WidthMm > 0 && w.HeightMm > 0);
        });

        // Baseline 700pt von unten → Oberkante ~47 mm von oben (Ursprung links oben)
        var nummer = words.Single(w => w.Text == "5998-100");
        Assert.InRange(nummer.YMm, 42, 52);

        // Leserichtung: "5998-100" beginnt rechts von "Plannummer", gleiche Zeile
        var erstes = words.Single(w => w.Text == "Plannummer");
        Assert.True(nummer.XMm > erstes.XMm);
        Assert.True(Math.Abs(nummer.YMm - erstes.YMm) < 2);
    }

    [Fact]
    public async Task RenderPage_DeliversBgraPixelsAndMmSize()
    {
        var page = await _sut.RenderPageAsync(new MemoryStream(BuildTestPdf()), pageIndex: 0, pixelWidth: 800);

        Assert.InRange(page.PixelWidth, 795, 805);
        Assert.True(page.PixelHeight > page.PixelWidth); // A4 hoch
        Assert.Equal(page.PixelWidth * page.PixelHeight * 4, page.PixelsBgra.Length);
        Assert.InRange(page.PageWidthMm, 209, 211);
        Assert.InRange(page.PageHeightMm, 296, 298);
    }

    [Fact]
    public async Task GetPageCount_And_InvalidPage()
    {
        var pdf = BuildTestPdf();
        Assert.Equal(1, await _sut.GetPageCountAsync(new MemoryStream(pdf)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _sut.GetWordsAsync(new MemoryStream(pdf), pageIndex: 1));
    }
}
