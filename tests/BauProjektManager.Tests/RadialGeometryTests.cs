using System.Globalization;
using BauProjektManager.PlanManager.Controls;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer die reine Radial-Geometrie (BPM-111.05 Slice 1).
/// Spez-Invariante: n Segmente fuellen immer den vollen Kreis gleichmaessig.
/// </summary>
public class RadialGeometryTests
{
    [Theory]
    [InlineData(8, 45)]
    [InlineData(6, 60)]
    [InlineData(2, 180)]
    [InlineData(9, 40)]
    public void SegmentSweep_DividesFullCircleEvenly(int count, double expected)
    {
        Assert.Equal(expected, RadialGeometry.SegmentSweep(count), precision: 5);
    }

    [Fact]
    public void SegmentStartAngles_CoverFullCircleWithoutOverlap()
    {
        const int count = 7;
        for (var i = 0; i < count; i++)
        {
            var start = RadialGeometry.SegmentStartAngle(i, count);
            Assert.Equal(i * 360.0 / count, start, precision: 5);
        }
        Assert.Equal(360.0,
            RadialGeometry.SegmentStartAngle(count - 1, count) + RadialGeometry.SegmentSweep(count),
            precision: 5);
    }

    [Theory]
    [InlineData(0, 260, 160)]    // 0 Grad = 12 Uhr (oben)
    [InlineData(90, 360, 260)]   // 90 Grad = 3 Uhr (rechts)
    [InlineData(180, 260, 360)]  // 180 Grad = 6 Uhr (unten)
    [InlineData(270, 160, 260)]  // 270 Grad = 9 Uhr (links)
    public void PointOnCircle_UsesClockwiseTopConvention(double angle, double expX, double expY)
    {
        var (x, y) = RadialGeometry.PointOnCircle(260, 260, 100, angle);

        Assert.Equal(expX, x, precision: 6);
        Assert.Equal(expY, y, precision: 6);
    }

    [Fact]
    public void LabelPoint_IsAtSegmentMiddleOnMidRadius()
    {
        // Segment 0..90 Grad, Radien 100..200 -> Label bei 45 Grad auf r=150
        var (x, y) = RadialGeometry.LabelPoint(260, 260, 100, 200, 0, 90);
        var expected = RadialGeometry.PointOnCircle(260, 260, 150, 45);

        Assert.Equal(expected.X, x, precision: 6);
        Assert.Equal(expected.Y, y, precision: 6);
    }

    [Fact]
    public void BuildSegmentPathData_ProducesParsableInvariantPath()
    {
        var data = RadialGeometry.BuildSegmentPathData(260, 260, 72, 138, 0, 45);

        Assert.StartsWith("M ", data);
        Assert.EndsWith("Z", data);
        Assert.Contains("A 138,138", data);
        Assert.Contains("A 72,72", data);
        // InvariantCulture: Dezimalpunkt, kein Komma als Dezimaltrenner
        Assert.DoesNotContain(";", data);
        Assert.False(data.Contains(',') && CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator != ".",
            "Pfad muss kulturinvariant sein");
    }

    [Fact]
    public void BuildSegmentPathData_LargeArcFlag_SetForSweepOver180()
    {
        var small = RadialGeometry.BuildSegmentPathData(260, 260, 72, 138, 0, 90);
        var large = RadialGeometry.BuildSegmentPathData(260, 260, 72, 138, 0, 270);

        Assert.Contains(" 0 0 1 ", small);
        Assert.Contains(" 0 1 1 ", large);
    }

    [Fact]
    public void BuildSegmentPathData_SingleSegment_BuildsClosedFullRing()
    {
        // Sonderfall n=1: voller Ring aus zwei Halbboegen (ein Arc kann keine 360 Grad)
        var data = RadialGeometry.BuildSegmentPathData(260, 260, 72, 138, 0, 360);

        Assert.Equal(2, data.Split('M').Length - 1);
        Assert.Equal(4, data.Split('A').Length - 1);
    }
}
