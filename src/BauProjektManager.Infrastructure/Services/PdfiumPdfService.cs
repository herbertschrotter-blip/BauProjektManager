using System.IO;
using System.Text;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using Docnet.Core;
using Docnet.Core.Models;
using Docnet.Core.Readers;

namespace BauProjektManager.Infrastructure.Services;

/// <summary>
/// EINE Engine für beide PDF-Ports (ADR-062/063, Addendum Teil 47):
/// PDFium (Chrome-PDF-Engine) via Docnet.Core (MIT). Bild-Pixel
/// (<see cref="IPdfRenderService"/>) und Zeichen-Boxen
/// (<see cref="IPdfTextService"/>) entstehen in derselben Engine-Pipeline —
/// Rotation/Crop/Skalierung macht PDFium, es gibt KEIN eigenes
/// Koordinaten-Mapping (der "Acrobat-Weg").
/// mm-Basis: Punkte (1/72") der Engine × 25,4/72 — für Bild und Text identisch.
/// </summary>
public sealed class PdfiumPdfService : IPdfRenderService, IPdfTextService
{
    private const double PtToMm = 25.4 / 72.0;

    /// <summary>PDFium ist nicht threadsicher — alle Engine-Zugriffe serialisieren.</summary>
    private static readonly object _pdfiumLock = new();

    public async Task<int> GetPageCountAsync(Stream pdf, CancellationToken ct = default)
    {
        var bytes = await ToBytesAsync(pdf, ct);
        return await Task.Run(() =>
        {
            lock (_pdfiumLock)
            {
                using var reader = DocLib.Instance.GetDocReader(bytes, new PageDimensions(1.0));
                return reader.GetPageCount();
            }
        }, ct);
    }

    public async Task<PdfPageRender> RenderPageAsync(
        Stream pdf, int pageIndex, int pixelWidth, CancellationToken ct = default)
    {
        if (pixelWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(pixelWidth));
        var bytes = await ToBytesAsync(pdf, ct);

        return await Task.Run(() =>
        {
            lock (_pdfiumLock)
            {
                // Pass 1 (Faktor 1): Blattgröße in Punkten → mm + Zielfaktor
                double ptsWidth, ptsHeight;
                using (var probe = DocLib.Instance.GetDocReader(bytes, new PageDimensions(1.0)))
                {
                    EnsurePage(probe, pageIndex);
                    using var probePage = probe.GetPageReader(pageIndex);
                    ptsWidth = probePage.GetPageWidth();
                    ptsHeight = probePage.GetPageHeight();
                }

                // Pass 2: auf Zielbreite skaliert rendern; danach sauber auf Weiß
                // compositen (der naive Transparenz-Ersatz der Lib lässt die
                // halbtransparenten AA-Kanten gegen Schwarz stehen → Säume).
                var factor = pixelWidth / ptsWidth;
                using var reader = DocLib.Instance.GetDocReader(bytes, new PageDimensions(factor));
                using var page = reader.GetPageReader(pageIndex);
                var pixels = page.GetImage();
                CompositeOnWhite(pixels);
                return new PdfPageRender(
                    pixels,
                    page.GetPageWidth(),
                    page.GetPageHeight(),
                    ptsWidth * PtToMm,
                    ptsHeight * PtToMm);
            }
        }, ct);
    }

    public async Task<IReadOnlyList<PdfWord>> GetWordsAsync(
        Stream pdf, int pageIndex, CancellationToken ct = default)
    {
        var bytes = await ToBytesAsync(pdf, ct);

        return await Task.Run<IReadOnlyList<PdfWord>>(() =>
        {
            lock (_pdfiumLock)
            {
                // Faktor 1 → Zeichen-Boxen direkt in Punkten (Engine-Anzeigeraum,
                // Ursprung links oben) → mm. Identische lineare Basis wie das Bild.
                using var reader = DocLib.Instance.GetDocReader(bytes, new PageDimensions(1.0));
                EnsurePage(reader, pageIndex);
                using var page = reader.GetPageReader(pageIndex);
                return AssembleWords(page);
            }
        }, ct);
    }

    /// <summary>Zeichen der Engine zu Wörtern gruppieren (Trennung an Whitespace).</summary>
    private static List<PdfWord> AssembleWords(IPageReader page)
    {
        var words = new List<PdfWord>();
        var text = new StringBuilder();
        double left = 0, top = 0, right = 0, bottom = 0;
        var open = false;

        foreach (var ch in page.GetCharacters())
        {
            if (char.IsWhiteSpace(ch.Char))
            {
                Flush();
                continue;
            }
            // Top/Bottom (bzw. Left/Right bei rotiertem Text) können von der
            // Engine vertauscht kommen → normalisieren, sonst kollabiert die
            // Box zu einem Strich (Höhe ~0).
            var b = ch.Box;
            var bl = Math.Min(b.Left, b.Right);
            var br = Math.Max(b.Left, b.Right);
            var bt = Math.Min(b.Top, b.Bottom);
            var bb = Math.Max(b.Top, b.Bottom);
            if (!open)
            {
                left = bl; top = bt; right = br; bottom = bb;
                open = true;
            }
            else
            {
                left = Math.Min(left, bl);
                top = Math.Min(top, bt);
                right = Math.Max(right, br);
                bottom = Math.Max(bottom, bb);
            }
            text.Append(ch.Char);
        }
        Flush();
        return words;

        void Flush()
        {
            if (!open || text.Length == 0)
            {
                text.Clear();
                open = false;
                return;
            }
            words.Add(new PdfWord(
                text.ToString(),
                left * PtToMm,
                top * PtToMm,
                Math.Max(0, right - left) * PtToMm,
                Math.Max(0, bottom - top) * PtToMm));
            text.Clear();
            open = false;
        }
    }

    /// <summary>
    /// BGRA-Alpha korrekt auf weißen Hintergrund verrechnen:
    /// out = c·a + 255·(1−a). Erhält die Anti-Aliasing-Kanten der Schrift.
    /// </summary>
    private static void CompositeOnWhite(byte[] bgra)
    {
        for (var i = 0; i < bgra.Length; i += 4)
        {
            int a = bgra[i + 3];
            if (a == 255)
                continue;
            if (a == 0)
            {
                bgra[i] = bgra[i + 1] = bgra[i + 2] = 255;
            }
            else
            {
                var inv = 255 - a;
                bgra[i] = (byte)((bgra[i] * a + 255 * inv) / 255);
                bgra[i + 1] = (byte)((bgra[i + 1] * a + 255 * inv) / 255);
                bgra[i + 2] = (byte)((bgra[i + 2] * a + 255 * inv) / 255);
            }
            bgra[i + 3] = 255;
        }
    }

    private static void EnsurePage(IDocReader reader, int pageIndex)
    {
        var count = reader.GetPageCount();
        if (pageIndex < 0 || pageIndex >= count)
            throw new ArgumentOutOfRangeException(
                nameof(pageIndex), $"Seite {pageIndex} existiert nicht (0..{count - 1}).");
    }

    private static async Task<byte[]> ToBytesAsync(Stream pdf, CancellationToken ct)
    {
        var buffer = new MemoryStream();
        await pdf.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }
}
