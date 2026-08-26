using BauProjektManager.Domain.Models;

namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// Zentraler PDF-Render-Port (ADR-062): alle Module rendern PDFs ausschließlich
/// über diesen Port — nie direkt über eine Rendering-API. Pure .NET-Signaturen
/// (PNG-Bytes statt UI-Typen), damit weder WPF noch WinRT in den Domain-Layer
/// oder die Module lecken. Einzige Implementierung: WindowsPdfRenderService
/// (App/Composition Root, Windows.Data.Pdf).
///
/// PDF-Bearbeitung ist bewusst NICHT Teil dieses Ports (post-V1, eigener Port
/// + Engine-Entscheidung — siehe ADR-062).
/// </summary>
public interface IPdfRenderService
{
    /// <summary>Liefert die Seitenanzahl des PDF-Dokuments.</summary>
    Task<int> GetPageCountAsync(Stream pdf, CancellationToken ct = default);

    /// <summary>
    /// Rendert eine Seite (0-basiert) als PNG mit der gewünschten Pixelbreite
    /// (Höhe folgt dem Seitenverhältnis) und liefert die physische Blattgröße
    /// in mm mit (für viewer-seitige Ausschnitte, z. B. Plankopf-Start).
    /// Wirft bei ungültigem Seitenindex.
    /// </summary>
    Task<PdfPageRender> RenderPageAsPngAsync(Stream pdf, int pageIndex, int pixelWidth, CancellationToken ct = default);
}
