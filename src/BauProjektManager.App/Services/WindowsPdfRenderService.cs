using System.IO;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using Windows.Data.Pdf;
using Windows.Storage.Streams;

namespace BauProjektManager.App.Services;

/// <summary>
/// Einzige Implementierung des PDF-Render-Ports (ADR-062) via Windows.Data.Pdf
/// (WinRT, kein Drittanbieter). Lebt bewusst im Composition Root: nur die App
/// trägt das Windows-SDK-TFM, Module kennen ausschließlich
/// <see cref="IPdfRenderService"/>.
/// </summary>
public sealed class WindowsPdfRenderService : IPdfRenderService
{
    public async Task<int> GetPageCountAsync(Stream pdf, CancellationToken ct = default)
    {
        var doc = await LoadDocumentAsync(pdf, ct);
        return (int)doc.PageCount;
    }

    public async Task<PdfPageRender> RenderPageAsPngAsync(
        Stream pdf, int pageIndex, int pixelWidth, CancellationToken ct = default)
    {
        if (pixelWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(pixelWidth));

        var doc = await LoadDocumentAsync(pdf, ct);
        if (pageIndex < 0 || pageIndex >= doc.PageCount)
            throw new ArgumentOutOfRangeException(
                nameof(pageIndex), $"Seite {pageIndex} existiert nicht (0..{doc.PageCount - 1}).");

        using var page = doc.GetPage((uint)pageIndex);

        // Physische Blattgröße: MediaBox in PDF-Punkten (1/72 Zoll) -> mm.
        // Bei 90°/270°-Rotation rendert WinRT gedreht -> Maße tauschen.
        const double PtToMm = 25.4 / 72.0;
        var box = page.Dimensions.MediaBox;
        var widthMm = box.Width * PtToMm;
        var heightMm = box.Height * PtToMm;
        if (page.Rotation is PdfPageRotation.Rotate90 or PdfPageRotation.Rotate270)
            (widthMm, heightMm) = (heightMm, widthMm);

        using var target = new InMemoryRandomAccessStream();
        await page.RenderToStreamAsync(target, new PdfPageRenderOptions
        {
            DestinationWidth = (uint)pixelWidth
        });

        ct.ThrowIfCancellationRequested();
        var result = new MemoryStream((int)target.Size);
        var readStream = target.AsStream();
        readStream.Position = 0;
        await readStream.CopyToAsync(result, ct);
        return new PdfPageRender(result.ToArray(), widthMm, heightMm);
    }

    /// <summary>
    /// WinRT braucht einen IRandomAccessStream — der Input wird in Memory
    /// gepuffert (Vorschau rendert einzelne Plandateien, keine Massen).
    /// </summary>
    private static async Task<PdfDocument> LoadDocumentAsync(Stream pdf, CancellationToken ct)
    {
        var buffer = new MemoryStream();
        await pdf.CopyToAsync(buffer, ct);
        buffer.Position = 0;
        return await PdfDocument.LoadFromStreamAsync(buffer.AsRandomAccessStream());
    }
}
