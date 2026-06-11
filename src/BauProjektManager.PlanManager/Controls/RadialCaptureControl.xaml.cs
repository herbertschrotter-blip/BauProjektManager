using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BauProjektManager.PlanManager.Controls;

/// <summary>
/// Konzentrisches Radial-Menue fuer die manuelle Plan-Erfassung
/// (BPM-111.05, ADR-059). Verbindliche Interaktionsspez: Header von
/// Docs/Mockups/PlanManager/02_Projektdetail/02_ManuellSortieren.html.
///
/// Das Control ist DATEN-DUMM und besitzt keine Ebenenlogik: Es rendert
/// Ringe, meldet Dwell-Commits (<see cref="SegmentCommitted"/>) und bietet
/// <see cref="HitTestSegment"/> fuer das Release-Handling des Gesten-Hosts
/// (Maus-Capture liegt beim Host, Slice 2). Der Host entscheidet typabhaengig,
/// welche Items Ring 2/3 bekommen, und steuert Erscheinen/Verschwinden.
///
/// Spez-Invarianten hier umgesetzt:
/// - Dwell TIMERBASIERT (DispatcherTimer pro Segment-Eintritt, default 110 ms)
/// - Ring-Animation (Scale 0.7-&gt;1 + Fade, 140 ms ease-out) NUR wenn der
///   Host animate=true uebergibt (= Erscheinen; Inhaltswechsel stumm)
/// - n Segmente fuellen immer 360 Grad (RadialGeometry)
/// - Segmente halbtransparent (Fill 0.78, Hover/Auswahl 0.95)
/// </summary>
public partial class RadialCaptureControl : UserControl
{
    private const double Cx = 260;
    private const double CenterRadius = 62;
    private static readonly (double Inner, double Outer)[] _ringRadii =
        [(72, 138), (146, 204), (212, 256)];

    private const double FillOpacityDefault = 0.78;
    private const double FillOpacityActive = 0.95;
    private static readonly Duration _appearDuration = new(TimeSpan.FromMilliseconds(140));

    public static readonly DependencyProperty DwellMillisecondsProperty =
        DependencyProperty.Register(nameof(DwellMilliseconds), typeof(int),
            typeof(RadialCaptureControl), new PropertyMetadata(110));

    /// <summary>Verweildauer bis ein Segment uebernommen wird (konfigurierbar, Spez: 110 ms).</summary>
    public int DwellMilliseconds
    {
        get => (int)GetValue(DwellMillisecondsProperty);
        set => SetValue(DwellMillisecondsProperty, value);
    }

    /// <summary>Dwell abgelaufen auf einem normalen Segment — Host setzt daraufhin Ring n+1 (oder raeumt ab).</summary>
    public event EventHandler<RadialSegmentEventArgs>? SegmentCommitted;

    private sealed class RingState
    {
        public Canvas Group { get; init; } = new();
        public IReadOnlyList<RadialSegmentItem> Items { get; set; } = [];
        public string? SelectedName { get; set; }
    }

    private readonly RingState[] _rings;
    private readonly Ellipse _centerCircle;
    private readonly TextBlock _centerPrimary;
    private readonly TextBlock _centerSecondary;
    private readonly DispatcherTimer _dwellTimer;
    private RadialSegmentHit? _hover;

    public RadialCaptureControl()
    {
        InitializeComponent();

        _rings = [new RingState(), new RingState(), new RingState()];
        foreach (var ring in _rings)
            RootCanvas.Children.Add(ring.Group);

        _centerCircle = new Ellipse
        {
            Width = CenterRadius * 2,
            Height = CenterRadius * 2,
            Fill = CloneWithOpacity(FindBrush("BpmBgActive"), 0.9),
            Stroke = FindBrush("BpmAccentPrimary"),
            StrokeThickness = 1.5
        };
        Canvas.SetLeft(_centerCircle, Cx - CenterRadius);
        Canvas.SetTop(_centerCircle, Cx - CenterRadius);
        RootCanvas.Children.Add(_centerCircle);

        _centerPrimary = MakeLabel(string.Empty, 13, FindBrush("BpmTextBright"));
        _centerSecondary = MakeLabel(string.Empty, 11, FindBrush("BpmTextSecondary"));
        PlaceLabel(_centerPrimary, Cx, Cx - 8);
        PlaceLabel(_centerSecondary, Cx, Cx + 10);
        RootCanvas.Children.Add(_centerPrimary);
        RootCanvas.Children.Add(_centerSecondary);

        _dwellTimer = new DispatcherTimer();
        _dwellTimer.Tick += OnDwellElapsed;

        MouseMove += (_, e) => UpdateHover(e.GetPosition(RootCanvas));
        MouseLeave += (_, _) => SetHover(null);
    }

    // ── Public API (Host-Orchestrierung) ────────────────────────────

    /// <summary>Zentrum-Texte (z. B. "4 Dateien" / "Polierplan › Haus 2").</summary>
    public void SetCenter(string primary, string secondary)
    {
        _centerPrimary.Text = primary;
        _centerSecondary.Text = secondary;
    }

    /// <summary>
    /// Setzt den Inhalt eines Rings (1..3). animate=true NUR beim Erscheinen
    /// des Rings — Inhaltswechsel ruft mit animate=false auf (Spez).
    /// </summary>
    public void SetRing(int ringIndex, IReadOnlyList<RadialSegmentItem> items,
        string? selectedName, bool animate)
    {
        var ring = _rings[ringIndex - 1];
        ring.Items = items;
        ring.SelectedName = selectedName;
        RenderRing(ringIndex);

        if (animate)
            PlayAppearAnimation(ring.Group);
    }

    /// <summary>Entfernt einen Ring (Ebenenlogik des Hosts, z. B. zurueck auf Ring 1).</summary>
    public void ClearRing(int ringIndex)
    {
        var ring = _rings[ringIndex - 1];
        ring.Items = [];
        ring.SelectedName = null;
        ring.Group.Children.Clear();
    }

    /// <summary>
    /// Hover-Update fuer den Gesten-Host (bei Maus-Capture erhaelt das Control
    /// keine eigenen MouseMove-Events). Punkt relativ zum Control.
    /// </summary>
    public void UpdateHoverFromHost(Point pointRelativeToControl) =>
        UpdateHover(TranslatePoint(pointRelativeToControl, RootCanvas));

    /// <summary>Segment unter dem Punkt (relativ zum Control) — fuer Release-Handling.</summary>
    public RadialSegmentHit? HitTestSegment(Point pointRelativeToControl) =>
        HitTestCanvas(TranslatePoint(pointRelativeToControl, RootCanvas));

    /// <summary>Setzt Hover + Dwell zurueck (z. B. beim Schliessen des Overlays).</summary>
    public void ResetInteraction()
    {
        _dwellTimer.Stop();
        _hover = null;
    }

    // ── Hover + Dwell (Spez: timerbasiert, nie MouseMove-gekoppelt) ──

    private void UpdateHover(Point canvasPoint) => SetHover(HitTestCanvas(canvasPoint));

    private void SetHover(RadialSegmentHit? hit)
    {
        if (Equals(hit?.RingIndex, _hover?.RingIndex) && hit?.Item.Name == _hover?.Item.Name)
            return;

        var previousRing = _hover?.RingIndex;
        _hover = hit;
        _dwellTimer.Stop();

        if (previousRing is int prev)
            RenderRing(prev);
        if (hit is not null)
        {
            RenderRing(hit.RingIndex);
            if (hit.Item is { IsAddItem: false, IsInert: false })
            {
                _dwellTimer.Interval = TimeSpan.FromMilliseconds(DwellMilliseconds);
                _dwellTimer.Start();
            }
        }
    }

    private void OnDwellElapsed(object? sender, EventArgs e)
    {
        _dwellTimer.Stop();
        if (_hover is null)
            return;

        var ring = _rings[_hover.RingIndex - 1];
        ring.SelectedName = _hover.Item.Name;
        RenderRing(_hover.RingIndex);
        SegmentCommitted?.Invoke(this, new RadialSegmentEventArgs(_hover.RingIndex, _hover.Item));
    }

    private RadialSegmentHit? HitTestCanvas(Point canvasPoint)
    {
        // Aeusserster Ring zuerst (liegt visuell "oben")
        for (var ringIndex = 3; ringIndex >= 1; ringIndex--)
        {
            foreach (var child in _rings[ringIndex - 1].Group.Children)
            {
                if (child is Path { Tag: RadialSegmentItem item } path
                    && path.Data.FillContains(canvasPoint))
                    return new RadialSegmentHit(ringIndex, item);
            }
        }
        return null;
    }

    // ── Rendering ────────────────────────────────────────────────────

    private void RenderRing(int ringIndex)
    {
        var ring = _rings[ringIndex - 1];
        ring.Group.Children.Clear();
        var count = ring.Items.Count;
        if (count == 0)
            return;

        var (inner, outer) = _ringRadii[ringIndex - 1];
        var sweep = RadialGeometry.SegmentSweep(count);

        for (var i = 0; i < count; i++)
        {
            var item = ring.Items[i];
            var start = RadialGeometry.SegmentStartAngle(i, count);
            var isSelected = ring.SelectedName == item.Name;
            var isHover = _hover?.RingIndex == ringIndex && _hover.Item.Name == item.Name;

            var path = new Path
            {
                Data = Geometry.Parse(RadialGeometry.BuildSegmentPathData(
                    Cx, Cx, inner, outer, start, sweep)),
                Fill = BuildFill(item, isSelected, isHover),
                Stroke = BuildStroke(item, isSelected, isHover),
                StrokeThickness = isSelected || isHover ? 2 : 1.5,
                Tag = item
            };
            if (item.IsAddItem || (item.IsCandidate && !isSelected))
                path.StrokeDashArray = [4, 3];
            ring.Group.Children.Add(path);

            var (lx, ly) = RadialGeometry.LabelPoint(Cx, Cx, inner, outer, start, sweep);
            var label = MakeLabel(item.Name, ringIndex == 3 ? 11 : 12,
                item.IsAddItem ? FindBrush("BpmWarning") : FindBrush("BpmTextBright"));
            PlaceLabel(label, lx, ly);
            ring.Group.Children.Add(label);
        }
    }

    private Brush BuildFill(RadialSegmentItem item, bool isSelected, bool isHover)
    {
        if (item.IsAddItem)
            return CloneWithOpacity(FindBrush("BpmBgBase"), 0.7);
        if (isSelected)
            return CloneWithOpacity(FindBrush("BpmBgActive"), FillOpacityActive);

        var baseBrush = item.ColorHex is not null
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(item.ColorHex))
            : FindBrush("BpmBgElevated");
        return CloneWithOpacity(baseBrush, isHover ? FillOpacityActive : FillOpacityDefault);
    }

    private Brush BuildStroke(RadialSegmentItem item, bool isSelected, bool isHover)
    {
        if (isSelected) return FindBrush("BpmAccentPrimary");
        if (isHover) return FindBrush("BpmTextBright");
        if (item.IsAddItem || item.IsCandidate) return FindBrush("BpmWarning");
        return FindBrush("BpmBgBase");
    }

    private static void PlayAppearAnimation(Canvas group)
    {
        var scale = new ScaleTransform(0.7, 0.7, Cx, Cx);
        group.RenderTransform = scale;
        group.Opacity = 0;

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(0.7, 1.0, _appearDuration) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(0.7, 1.0, _appearDuration) { EasingFunction = ease });
        group.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, _appearDuration) { EasingFunction = ease });
    }

    private static TextBlock MakeLabel(string text, double fontSize, Brush foreground) => new()
    {
        Text = text,
        FontSize = fontSize,
        Foreground = foreground,
        TextAlignment = TextAlignment.Center,
        Width = 120,
        IsHitTestVisible = false
    };

    private static void PlaceLabel(TextBlock label, double centerX, double centerY)
    {
        Canvas.SetLeft(label, centerX - label.Width / 2);
        Canvas.SetTop(label, centerY - 9);
    }

    private Brush FindBrush(string key) =>
        TryFindResource(key) as Brush ?? Brushes.Gray;

    private static Brush CloneWithOpacity(Brush brush, double opacity)
    {
        var clone = brush.Clone();
        clone.Opacity = opacity;
        if (clone.CanFreeze)
            clone.Freeze();
        return clone;
    }
}
