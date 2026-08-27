using BauProjektManager.Domain.Models;

namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// Zentraler PDF-Render-Port (ADR-062, Addendum Teil 47): alle Module rendern
/// PDFs ausschließlich über diesen Port — nie direkt über eine Rendering-API.
/// Pure .NET-Signaturen (Roh-Pixel statt UI-Typen). Einzige Implementierung:
/// PdfiumPdfService (Infrastructure, PDFium via Docnet) — dieselbe Engine
/// bedient auch IPdfTextService, Pixel und Text-Boxen teilen den Koordinatenraum.
///
/// PDF-Bearbeitung ist bewusst NICHT Teil dieses Ports (post-V1, eigener Port
/// + Engine-Entscheidung — siehe ADR-062).
/// </summary>
public interface IPdfRenderService
{
    /// <summary>Liefert die Seitenanzahl des PDF-Dokuments.</summary>
    Task<int> GetPageCountAsync(Stream pdf, CancellationToken ct = default);

    /// <summary>
    /// Rendert eine Seite (0-basiert) mit der gewünschten Pixelbreite
    /// (Höhe folgt dem Seitenverhältnis) und liefert die physische Blattgröße
    /// in mm mit (für viewer-seitige Ausschnitte, z. B. Plankopf-Start).
    /// Wirft bei ungültigem Seitenindex.
    /// </summary>
    Task<PdfPageRender> RenderPageAsync(Stream pdf, int pageIndex, int pixelWidth, CancellationToken ct = default);
}
