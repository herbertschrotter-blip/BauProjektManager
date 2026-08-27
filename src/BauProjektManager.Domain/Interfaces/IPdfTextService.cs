using BauProjektManager.Domain.Models;

namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// PDF-Text-Port (ADR-063): liest Wörter MIT Koordinaten aus der Textebene
/// eines PDFs — Grundlage für "Text markieren → zuweisen" (BPM-118) und
/// später die Plankopf-Extraktion (ADR-045). Pure .NET-Signaturen, kein
/// UI-Typ. Einzige Implementierung: PdfPigTextService (Infrastructure).
///
/// KEIN OCR: PDFs ohne Textebene liefern eine leere Liste — die UI zeigt
/// dann einen Hinweis (manuelle Eingabe).
/// </summary>
public interface IPdfTextService
{
    /// <summary>
    /// Liefert alle Wörter einer Seite (0-basiert) mit BoundingBox in mm,
    /// Ursprung links oben, rotationsbereinigt (passend zu PdfPageRender).
    /// Leer wenn die Seite keine Textebene hat. Wirft bei ungültigem Index.
    /// </summary>
    Task<IReadOnlyList<PdfWord>> GetWordsAsync(Stream pdf, int pageIndex, CancellationToken ct = default);
}
