namespace BauProjektManager.PlanManager.Controls;

/// <summary>
/// Ein Segment eines Radial-Rings (BPM-111.05). Das Control ist daten-dumm:
/// Inhalte und Farben kommen vom Host (Stammdaten/ViewModel), das Control
/// rendert nur.
/// </summary>
/// <param name="Name">Anzeigename und Identitaet innerhalb des Rings.</param>
/// <param name="ColorHex">Segmentfarbe (z. B. Plantyp-Farbe aus segment_types); NULL = Theme-Default (BpmBgElevated).</param>
/// <param name="IsAddItem">True fuer "+ Neu…"-Segmente (gestrichelt, kein Dwell-Commit — loest AddRequested aus).</param>
/// <param name="IsCandidate">True wenn Extractor-Kandidat (gestrichelte Warning-Markierung).</param>
/// <param name="IsInert">True fuer reine Anzeige-Segmente ohne Interaktion (z. B. "Mehr …" bis Pagination kommt).</param>
public sealed record RadialSegmentItem(
    string Name,
    string? ColorHex = null,
    bool IsAddItem = false,
    bool IsCandidate = false,
    bool IsInert = false);

/// <summary>Treffer eines Segment-Hit-Tests (fuer Release-Handling des Gesten-Hosts).</summary>
public sealed record RadialSegmentHit(int RingIndex, RadialSegmentItem Item);

/// <summary>Event-Args fuer Dwell-Commit und Add-Anforderung.</summary>
public sealed class RadialSegmentEventArgs : EventArgs
{
    public RadialSegmentEventArgs(int ringIndex, RadialSegmentItem item)
    {
        RingIndex = ringIndex;
        Item = item;
    }

    public int RingIndex { get; }
    public RadialSegmentItem Item { get; }
}
