namespace BauProjektManager.Domain.Models;

/// <summary>
/// Ergebnis eines Seiten-Renderings über den PDF-Render-Port (ADR-062):
/// PNG-Bytes plus physische Blattgröße in Millimetern (rotationsbereinigt).
/// Die mm-Maße erlauben viewer-seitige Ausschnitte in realen Größen —
/// z. B. den Plankopf-Start (A4 rechts unten, BPM-111.06 Slice C2).
/// </summary>
/// <param name="Png">Gerenderte Seite als PNG.</param>
/// <param name="PageWidthMm">Blattbreite in mm (nach Seitenrotation).</param>
/// <param name="PageHeightMm">Blatthöhe in mm (nach Seitenrotation).</param>
public sealed record PdfPageRender(
    byte[] Png,
    double PageWidthMm,
    double PageHeightMm);
