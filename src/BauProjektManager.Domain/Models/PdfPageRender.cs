namespace BauProjektManager.Domain.Models;

/// <summary>
/// Ergebnis eines Seiten-Renderings über den PDF-Render-Port (ADR-062,
/// Addendum Teil 47): rohe BGRA-Pixel plus physische Blattgröße in mm.
/// Pixel und Text-Koordinaten (IPdfTextService) stammen aus DERSELBEN
/// Engine-Pipeline (PDFium) — Viewer-Pixel ↔ mm ist eine einzige lineare
/// Umrechnung, ohne eigenes Koordinaten-Mapping.
/// </summary>
/// <param name="PixelsBgra">Seite als BGRA32-Pixel (Zeilen top-down, Stride = PixelWidth*4).</param>
/// <param name="PixelWidth">Bildbreite in Pixel.</param>
/// <param name="PixelHeight">Bildhöhe in Pixel.</param>
/// <param name="PageWidthMm">Blattbreite in mm (Anzeige-Orientierung).</param>
/// <param name="PageHeightMm">Blatthöhe in mm (Anzeige-Orientierung).</param>
public sealed record PdfPageRender(
    byte[] PixelsBgra,
    int PixelWidth,
    int PixelHeight,
    double PageWidthMm,
    double PageHeightMm);
