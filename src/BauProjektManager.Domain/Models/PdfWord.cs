namespace BauProjektManager.Domain.Models;

/// <summary>
/// Ein Wort aus der PDF-Textebene mit BoundingBox in Millimetern (ADR-063).
/// Koordinatensystem: Ursprung LINKS OBEN, rotationsbereinigt — deckungsgleich
/// mit der gerenderten Seite aus <see cref="PdfPageRender"/> (ADR-062), damit
/// Viewer-Pixel ↔ PDF-mm eine einzige lineare Umrechnung bleibt.
/// </summary>
/// <param name="Text">Wort-Text (wie in der Textebene).</param>
/// <param name="XMm">Linke Kante in mm (von links).</param>
/// <param name="YMm">Obere Kante in mm (von oben).</param>
/// <param name="WidthMm">Breite in mm.</param>
/// <param name="HeightMm">Höhe in mm.</param>
public sealed record PdfWord(
    string Text,
    double XMm,
    double YMm,
    double WidthMm,
    double HeightMm);
