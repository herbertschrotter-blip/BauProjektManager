using System.Globalization;

namespace BauProjektManager.PlanManager.Controls;

/// <summary>
/// Reine Geometrie-Mathematik fuer das Radial-Menue (BPM-111.05).
/// Winkelkonvention wie Mockup: 0 Grad = 12 Uhr, im Uhrzeigersinn.
/// Liefert WPF-Path-Mini-Language-Strings (kompatibel zu Geometry.Parse) —
/// dadurch ohne UI-Abhaengigkeit testbar.
/// </summary>
public static class RadialGeometry
{
    /// <summary>Standard-Luecke zwischen Segmenten in Grad (Mockup: ~0.7 Grad).</summary>
    public const double DefaultGapDegrees = 0.7;

    /// <summary>
    /// Gleichmaessige 360-Grad-Teilung: Startwinkel des Segments <paramref name="index"/>
    /// bei <paramref name="count"/> Segmenten (Invariante aus der Mockup-Spez:
    /// n Segmente fuellen IMMER den vollen Kreis).
    /// </summary>
    public static double SegmentStartAngle(int index, int count) =>
        index * 360.0 / count;

    /// <summary>Winkel-Spannweite eines Segments bei gleichmaessiger Teilung.</summary>
    public static double SegmentSweep(int count) => 360.0 / count;

    /// <summary>Punkt auf dem Kreis (0 Grad = oben, im Uhrzeigersinn).</summary>
    public static (double X, double Y) PointOnCircle(
        double cx, double cy, double radius, double angleDegrees)
    {
        var rad = angleDegrees * Math.PI / 180.0;
        return (cx + radius * Math.Sin(rad), cy - radius * Math.Cos(rad));
    }

    /// <summary>Label-Position: Mitte des Segments auf mittlerem Radius.</summary>
    public static (double X, double Y) LabelPoint(
        double cx, double cy, double innerRadius, double outerRadius,
        double startAngle, double sweep) =>
        PointOnCircle(cx, cy, (innerRadius + outerRadius) / 2.0, startAngle + sweep / 2.0);

    /// <summary>
    /// Pfad-Daten eines Donut-Segments (WPF Path-Mini-Language).
    /// Sonderfall: ein einziges Segment (sweep ~360) wird als geschlossener
    /// Ring aus zwei Halbboegen gebaut (ein Arc kann keine 360 Grad zeichnen).
    /// </summary>
    public static string BuildSegmentPathData(
        double cx, double cy, double innerRadius, double outerRadius,
        double startAngle, double sweep, double gapDegrees = DefaultGapDegrees)
    {
        if (sweep >= 359.9)
            return BuildFullRingPathData(cx, cy, innerRadius, outerRadius);

        var a0 = startAngle + gapDegrees / 2.0;
        var a1 = startAngle + sweep - gapDegrees / 2.0;
        var largeArc = (a1 - a0) > 180.0 ? 1 : 0;

        var outerStart = PointOnCircle(cx, cy, outerRadius, a0);
        var outerEnd = PointOnCircle(cx, cy, outerRadius, a1);
        var innerEnd = PointOnCircle(cx, cy, innerRadius, a1);
        var innerStart = PointOnCircle(cx, cy, innerRadius, a0);

        return string.Create(CultureInfo.InvariantCulture,
            $"M {outerStart.X:0.##},{outerStart.Y:0.##} " +
            $"A {outerRadius:0.##},{outerRadius:0.##} 0 {largeArc} 1 {outerEnd.X:0.##},{outerEnd.Y:0.##} " +
            $"L {innerEnd.X:0.##},{innerEnd.Y:0.##} " +
            $"A {innerRadius:0.##},{innerRadius:0.##} 0 {largeArc} 0 {innerStart.X:0.##},{innerStart.Y:0.##} Z");
    }

    private static string BuildFullRingPathData(
        double cx, double cy, double innerRadius, double outerRadius)
    {
        var oTop = PointOnCircle(cx, cy, outerRadius, 0);
        var oBottom = PointOnCircle(cx, cy, outerRadius, 180);
        var iTop = PointOnCircle(cx, cy, innerRadius, 0);
        var iBottom = PointOnCircle(cx, cy, innerRadius, 180);

        return string.Create(CultureInfo.InvariantCulture,
            $"M {oTop.X:0.##},{oTop.Y:0.##} " +
            $"A {outerRadius:0.##},{outerRadius:0.##} 0 0 1 {oBottom.X:0.##},{oBottom.Y:0.##} " +
            $"A {outerRadius:0.##},{outerRadius:0.##} 0 0 1 {oTop.X:0.##},{oTop.Y:0.##} Z " +
            $"M {iTop.X:0.##},{iTop.Y:0.##} " +
            $"A {innerRadius:0.##},{innerRadius:0.##} 0 0 1 {iBottom.X:0.##},{iBottom.Y:0.##} " +
            $"A {innerRadius:0.##},{innerRadius:0.##} 0 0 1 {iTop.X:0.##},{iTop.Y:0.##} Z");
    }
}
