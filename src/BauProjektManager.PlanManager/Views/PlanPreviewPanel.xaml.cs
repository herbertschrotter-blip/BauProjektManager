using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Rectangle = System.Windows.Shapes.Rectangle;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using Serilog;

namespace BauProjektManager.PlanManager.Views;

/// <summary>Ziel-Art einer Text-Zuweisung aus der Vorschau (BPM-118).</summary>
public enum PdfAssignKind
{
    /// <summary>Änderungshinweis der einlaufenden Revision (plan_revisions.change_note).</summary>
    ChangeNote,
    /// <summary>Index-Datum der einlaufenden Revision (plan_revisions.released_at).</summary>
    ReleasedAt,
    /// <summary>Segmenttyp aus dem Katalog (BPM-108).</summary>
    Segment
}

/// <summary>Vom User gewählte Zuweisung: markierter Text → Ziel.</summary>
public sealed class PdfTextAssignedEventArgs : EventArgs
{
    public required PdfAssignKind Kind { get; init; }
    public string? SegmentTypeId { get; init; }
    public string? SegmentTypeName { get; init; }
    /// <summary>token_key des Segmenttyps (Denormalisierung für plan_document_segments.segment_key, BPM-118 Teil 3).</summary>
    public string? SegmentTypeTokenKey { get; init; }
    public required string Text { get; init; }
}

/// <summary>
/// Integriertes PDF-Vorschau-Panel (BPM-111.06 Slice C, Variante B — Teil 47):
/// lebt als rechte Spalte im Tab "Manuell sortieren" (Tabelle | Detail-Panel |
/// Vorschau-Panel), KEIN separates Fenster. Rendert Seiten über den zentralen
/// <see cref="IPdfRenderService"/> (ADR-062).
/// Startansicht = PLANKOPF: rechte untere Blattecke im A4-Ausschnitt.
/// Mausrad = Zoom (cursorzentriert) · mittlere Maustaste = Verschieben ·
/// ◀/▶ = Seiten (Zoom/Position bleiben) · "Blatt" = Fit.
/// "↗" öffnet die Datei in der Windows-Standard-App (<see cref="IFileLauncher"/>,
/// ADR-060) — Bearbeiten passiert bewusst extern, nie in-app.
/// </summary>
public partial class PlanPreviewPanel : UserControl
{
    private const int RenderPixelWidth = 3600;
    private const double TargetPixelsPerMm = 7.0; // ~180 DPI — scharf bis in den Plankopf
    private const int MaxRenderWidth = 7200;      // Speicher-Deckel (A0 ≈ 146 MB BGRA)
    private const double A4WidthMm = 210;
    private const double A4HeightMm = 297;
    private const double MinZoom = 0.05;
    private const double MaxZoom = 8.0;
    private const string InteractionHint = "Rad = Zoom · Mitteltaste = Pan";

    /// <summary>PDF-Render-Port (ADR-062) — vom Host gesetzt, bevor ShowFileAsync läuft.</summary>
    public IPdfRenderService? PdfRenderService { get; set; }

    /// <summary>Shell-Launcher (ADR-060) — "↗ In Standard-App öffnen".</summary>
    public IFileLauncher? FileLauncher { get; set; }

    /// <summary>PDF-Text-Port (ADR-063) — null = kein Markieren möglich.</summary>
    public IPdfTextService? PdfTextService { get; set; }

    /// <summary>Segmenttyp-Katalog (BPM-108) für das Zuweisungs-Menü.</summary>
    public ISegmentTypeCatalog? SegmentTypeCatalog { get; set; }

    /// <summary>Vom ✕-Button ausgelöst — der Host blendet die Panel-Spalte aus.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>Markierter Text wurde einem Ziel zugewiesen (BPM-118).</summary>
    public event EventHandler<PdfTextAssignedEventArgs>? AssignRequested;

    private string? _currentPath;
    private int _renderGeneration;
    private int _pageCount;
    private int _currentPage; // 0-basiert
    private PdfPageRender? _page;

    private bool _panning;
    private Point _panStart;
    private double _panStartH;
    private double _panStartV;

    // Text-Markierung (BPM-118): Wort-Cache je Seite + klassische Auswahl
    // (Anker → Cursor als zusammenhängender Bereich in Leserichtung)
    private IReadOnlyList<PdfWord>? _words;
    private readonly List<PdfWord> _selectedWords = [];
    private string _selectedText = string.Empty;
    private bool _selecting;
    private int _selAnchor = -1;
    private int _selCursor = -1;

    public PlanPreviewPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Zeigt die Datei an — Startansicht Plankopf (A4 rechts unten). Bei erneutem
    /// Aufruf mit anderer Datei gewinnt der letzte Aufruf (Generation-Guard).
    /// </summary>
    public async Task ShowFileAsync(string absolutePath)
    {
        if (PdfRenderService is null)
        {
            StatusText.Text = "⚠ Kein PDF-Renderer verfügbar";
            return;
        }

        _currentPath = absolutePath;
        OpenExternalButton.IsEnabled = FileLauncher is not null;
        PageImage.Source = null;
        StatusText.Text = "Rendere …";

        var generation = ++_renderGeneration;
        try
        {
            // Direktes File.OpenRead: System.IO-Migration des PlanManagers folgt mit BPM-112.
            await using (var stream = File.OpenRead(absolutePath))
                _pageCount = await PdfRenderService.GetPageCountAsync(stream);
            if (generation != _renderGeneration)
                return;

            _currentPage = 0;
            await RenderCurrentPageAsync(generation, startWithPlankopf: true);
        }
        catch (Exception ex)
        {
            if (generation != _renderGeneration)
                return;
            Log.Warning(ex, "PDF-Vorschau fehlgeschlagen fuer {Name}", Path.GetFileName(absolutePath));
            StatusText.Text = $"⚠ Vorschau nicht möglich: {ex.Message}";
        }
    }

    /// <summary>Rendert die aktuelle Seite; bei Seitenwechsel bleiben Zoom/Position erhalten.</summary>
    private async Task RenderCurrentPageAsync(int generation, bool startWithPlankopf)
    {
        if (PdfRenderService is null || _currentPath is null)
            return;
        _words = null; // Wort-Cache gilt pro Seite/Datei
        ClearSelection();
        StatusText.Text = "Rendere …";

        PdfPageRender page;
        await using (var stream = File.OpenRead(_currentPath))
            page = await PdfRenderService.RenderPageAsync(stream, _currentPage, RenderPixelWidth);
        if (generation != _renderGeneration)
            return;

        // Große Blätter (A1/A0) brauchen mehr Pixel, sonst wird der Plankopf-Zoom
        // matschig — einmalig auf Zieldichte nachrendern (Viewer-Verhalten).
        var targetWidth = Math.Clamp(
            (int)(page.PageWidthMm * TargetPixelsPerMm), RenderPixelWidth, MaxRenderWidth);
        if (targetWidth > page.PixelWidth * 12 / 10)
        {
            await using var stream = File.OpenRead(_currentPath);
            page = await PdfRenderService.RenderPageAsync(stream, _currentPage, targetWidth);
            if (generation != _renderGeneration)
                return;
        }
        _page = page;

        var image = BitmapSource.Create(
            page.PixelWidth, page.PixelHeight, 96, 96,
            PixelFormats.Bgra32, null, page.PixelsBgra, page.PixelWidth * 4);
        image.Freeze();
        PageImage.Source = image;

        PageLabel.Text = $"{_currentPage + 1}/{_pageCount}";
        PrevPageButton.IsEnabled = _currentPage > 0;
        NextPageButton.IsEnabled = _currentPage < _pageCount - 1;
        StatusText.Text = $"{Path.GetFileName(_currentPath)} · Seite {_currentPage + 1} von {_pageCount} · {InteractionHint}";
        _ = EnsureWordsAsync(); // Textebene proaktiv laden → Markieren startet ohne Verzögerung

        if (startWithPlankopf)
            await Dispatcher.InvokeAsync(ApplyPlankopfView, DispatcherPriority.Loaded);
    }

    // ── Ansichten ───────────────────────────────────────────────────

    /// <summary>
    /// Plankopf-Startansicht (Spez): rechte untere Blattecke, Ausschnitt in
    /// A4-Größe (bzw. das ganze Blatt, wenn es kleiner als A4 ist).
    /// </summary>
    private void ApplyPlankopfView()
    {
        if (PageImage.Source is not BitmapSource bmp || _page is null)
            return;

        var density = bmp.PixelWidth / _page.PageWidthMm; // Pixel je mm
        var cutWidthPx = Math.Min(A4WidthMm, _page.PageWidthMm) * density;
        var cutHeightPx = Math.Min(A4HeightMm, _page.PageHeightMm) * density;

        var (vw, vh) = ViewportSize();
        SetZoom(Math.Clamp(Math.Min(vw / cutWidthPx, vh / cutHeightPx), MinZoom, MaxZoom));
        ScrollHost.UpdateLayout();
        ScrollHost.ScrollToRightEnd();
        ScrollHost.ScrollToBottom();
    }

    /// <summary>Ganzes Blatt einpassen.</summary>
    private void ApplyFitPageView()
    {
        if (PageImage.Source is not BitmapSource bmp)
            return;

        var (vw, vh) = ViewportSize();
        SetZoom(Math.Clamp(Math.Min(vw / bmp.PixelWidth, vh / bmp.PixelHeight), MinZoom, MaxZoom));
        ScrollHost.UpdateLayout();
        ScrollHost.ScrollToHorizontalOffset(0);
        ScrollHost.ScrollToVerticalOffset(0);
    }

    private (double W, double H) ViewportSize()
    {
        var vw = ScrollHost.ViewportWidth > 1 ? ScrollHost.ViewportWidth : ScrollHost.ActualWidth;
        var vh = ScrollHost.ViewportHeight > 1 ? ScrollHost.ViewportHeight : ScrollHost.ActualHeight;
        return (Math.Max(1, vw), Math.Max(1, vh));
    }

    private void SetZoom(double zoom)
    {
        ZoomTransform.ScaleX = zoom;
        ZoomTransform.ScaleY = zoom;
    }

    // ── Text-Markierung + Zuweisung (BPM-118, ADR-063) ──────────────

    /// <summary>Pixel des gerenderten Bilds ↔ mm des Blatts (eine lineare Umrechnung).</summary>
    private double MmPerPixel()
        => PageImage.Source is BitmapSource bmp && _page is not null && bmp.PixelWidth > 0
            ? _page.PageWidthMm / bmp.PixelWidth
            : 0;

    private void ClearSelection()
    {
        _selectedWords.Clear();
        _selectedText = string.Empty;
        _selecting = false;
        _selAnchor = -1;
        _selCursor = -1;
        SelectionCanvas.Children.Clear();
    }

    private async Task EnsureWordsAsync()
    {
        if (_words is not null || PdfTextService is null || _currentPath is null)
            return;
        try
        {
            await using var stream = File.OpenRead(_currentPath);
            _words = await PdfTextService.GetWordsAsync(stream, _currentPage);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "PDF-Textebene nicht lesbar fuer {Name}", Path.GetFileName(_currentPath));
            _words = [];
        }
    }

    /// <summary>
    /// Wort unter dem Cursor (Bild-Pixel). strict: nur bei (fast) direktem
    /// Treffer — für den Auswahl-START. Sonst nächstliegendes Wort (Zeile
    /// stark gewichtet) — fürs ZIEHEN, wie in klassischen Viewern.
    /// </summary>
    private int HitWordIndex(Point px, bool strict)
    {
        var k = MmPerPixel();
        if (k <= 0 || _words is null || _words.Count == 0)
            return -1;

        var pxMmX = px.X * k;
        var pxMmY = px.Y * k;
        const double pad = 0.8; // mm Toleranz um die Wortbox

        var best = -1;
        var bestScore = double.MaxValue;
        for (var i = 0; i < _words.Count; i++)
        {
            var w = _words[i];
            var dx = Math.Max(0, Math.Max(w.XMm - pxMmX, pxMmX - (w.XMm + w.WidthMm)));
            var dy = Math.Max(0, Math.Max(w.YMm - pxMmY, pxMmY - (w.YMm + w.HeightMm)));
            if (strict)
            {
                if (dx <= pad && dy <= pad)
                    return i;
                continue;
            }
            var score = dy * 4 + dx; // Zeilenabstand dominiert
            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        return strict ? -1 : best;
    }

    /// <summary>
    /// Auswahlbereich (Anker→Cursor, Leserichtung) hervorheben — wie in
    /// Word/Browser: EIN durchgehender Balken je Zeile in einheitlicher Höhe
    /// (Wortzwischenräume inklusive), Theme-Akzent halbtransparent.
    /// </summary>
    private void UpdateSelectionVisual()
    {
        SelectionCanvas.Children.Clear();
        var k = MmPerPixel();
        if (k <= 0 || _words is null || _selAnchor < 0 || _selCursor < 0)
            return;

        var accent = TryFindResource("BpmAccentPrimary") as SolidColorBrush;
        var c = accent?.Color ?? Color.FromRgb(0, 120, 212);
        var fill = new SolidColorBrush(Color.FromArgb(90, c.R, c.G, c.B));
        fill.Freeze();

        var from = Math.Min(_selAnchor, _selCursor);
        var to = Math.Max(_selAnchor, _selCursor);

        // Wörter zu Zeilen clustern (vertikale Überlappung mit der laufenden Zeile)
        double lineTop = 0, lineBottom = 0, lineLeft = 0, lineRight = 0;
        var lineOpen = false;
        for (var i = from; i <= to; i++)
        {
            var w = _words[i];
            var overlaps = lineOpen
                && w.YMm < lineBottom - 0.3
                && w.YMm + w.HeightMm > lineTop + 0.3;
            if (!overlaps)
            {
                FlushLine();
                lineTop = w.YMm;
                lineBottom = w.YMm + w.HeightMm;
                lineLeft = w.XMm;
                lineRight = w.XMm + w.WidthMm;
                lineOpen = true;
            }
            else
            {
                lineTop = Math.Min(lineTop, w.YMm);
                lineBottom = Math.Max(lineBottom, w.YMm + w.HeightMm);
                lineLeft = Math.Min(lineLeft, w.XMm);
                lineRight = Math.Max(lineRight, w.XMm + w.WidthMm);
            }
        }
        FlushLine();

        void FlushLine()
        {
            if (!lineOpen)
                return;
            const double padMm = 0.4; // etwas Luft wie bei echter Textauswahl
            var r = new Rectangle
            {
                Width = Math.Max(1, (lineRight - lineLeft + 2 * padMm) / k),
                Height = Math.Max(1, (lineBottom - lineTop + 2 * padMm) / k),
                Fill = fill,
                RadiusX = 1,
                RadiusY = 1
            };
            Canvas.SetLeft(r, (lineLeft - padMm) / k);
            Canvas.SetTop(r, (lineTop - padMm) / k);
            SelectionCanvas.Children.Add(r);
            lineOpen = false;
        }
    }

    /// <summary>Auswahl abschließen: Text des Bereichs übernehmen + Status.</summary>
    private void FinalizeSelection()
    {
        _selectedWords.Clear();
        _selectedText = string.Empty;
        if (_words is null || _selAnchor < 0 || _selCursor < 0)
            return;

        var from = Math.Min(_selAnchor, _selCursor);
        var to = Math.Max(_selAnchor, _selCursor);
        for (var i = from; i <= to; i++)
            _selectedWords.Add(_words[i]);
        _selectedText = string.Join(" ", _selectedWords.Select(w => w.Text));

        var preview = _selectedText.Length > 48 ? _selectedText[..48] + "…" : _selectedText;
        StatusText.Text = $"„{preview}\" markiert · Rechtsklick = Zuweisen";
    }

    /// <summary>
    /// Rechtsklick bei bestehender Auswahl: Zuweisungs-Menü — gut lesbar
    /// (größere Schrift/Abstände), mit Gruppen-Überschriften "Revision" und
    /// "Zuweisen als Segment" (Katalog BPM-108).
    /// </summary>
    private void OpenAssignMenu()
    {
        var itemStyle = TryFindResource("BpmMenuItem") as Style;
        MenuItem Item(string header) => new()
        {
            Header = header,
            Style = itemStyle,
            FontSize = 14,
            Padding = new Thickness(16, 8, 16, 8)
        };
        MenuItem Caption(string text) => new()
        {
            Header = text.ToUpperInvariant(),
            Style = itemStyle,
            IsEnabled = false,
            FontSize = 11,
            Padding = new Thickness(16, 8, 16, 2)
        };

        var menu = new ContextMenu { PlacementTarget = SheetHost, MinWidth = 300 };
        if (TryFindResource("BpmContextMenu") is Style menuStyle)
            menu.Style = menuStyle;

        var preview = _selectedText.Length > 34 ? _selectedText[..34] + "…" : _selectedText;
        var header = new MenuItem
        {
            Header = $"„{preview}\"",
            IsEnabled = false,
            Style = itemStyle,
            FontSize = 13,
            FontStyle = FontStyles.Italic,
            Padding = new Thickness(16, 8, 16, 8)
        };
        menu.Items.Add(header);
        AddSeparator(menu);

        menu.Items.Add(Caption("Revision"));
        var changeItem = Item("Änderungshinweis übernehmen");
        changeItem.Click += (_, _) => RaiseAssign(PdfAssignKind.ChangeNote, null, null);
        menu.Items.Add(changeItem);
        var dateItem = Item("Index-Datum übernehmen");
        dateItem.Click += (_, _) => RaiseAssign(PdfAssignKind.ReleasedAt, null, null);
        menu.Items.Add(dateItem);
        AddSeparator(menu);

        menu.Items.Add(Caption("Zuweisen als Segment"));
        foreach (var seg in SegmentTypeCatalog?.GetEffectiveActive() ?? [])
        {
            var segItem = Item(seg.Name);
            var id = seg.Id;
            var name = seg.Name;
            var tokenKey = seg.TokenKey;
            segItem.Click += (_, _) => RaiseAssign(PdfAssignKind.Segment, id, name, tokenKey);
            menu.Items.Add(segItem);
        }

        menu.IsOpen = true;

        void AddSeparator(ContextMenu m)
        {
            var s = new Separator();
            if (TryFindResource("BpmMenuSeparator") is Style st)
                s.Style = st;
            m.Items.Add(s);
        }
    }

    private void RaiseAssign(
        PdfAssignKind kind, string? segmentTypeId, string? segmentTypeName,
        string? segmentTypeTokenKey = null)
    {
        if (_selectedText.Length == 0)
            return;
        AssignRequested?.Invoke(this, new PdfTextAssignedEventArgs
        {
            Kind = kind,
            SegmentTypeId = segmentTypeId,
            SegmentTypeName = segmentTypeName,
            SegmentTypeTokenKey = segmentTypeTokenKey,
            Text = _selectedText
        });
    }

    // ── Interaktion: Zoom (Mausrad) + Pan (mittlere Maustaste) ──────

    private void OnScrollPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true; // Mausrad zoomt immer — kein Scrollen per Rad
        var oldZoom = ZoomTransform.ScaleX;
        var newZoom = Math.Clamp(oldZoom * (e.Delta > 0 ? 1.2 : 1 / 1.2), MinZoom, MaxZoom);
        if (Math.Abs(newZoom - oldZoom) < 0.0001)
            return;

        // Cursorzentriert: Bildpunkt unter der Maus bleibt unter der Maus
        var pos = e.GetPosition(ScrollHost);
        var contentX = (ScrollHost.HorizontalOffset + pos.X) / oldZoom;
        var contentY = (ScrollHost.VerticalOffset + pos.Y) / oldZoom;

        SetZoom(newZoom);
        ScrollHost.UpdateLayout();
        ScrollHost.ScrollToHorizontalOffset(contentX * newZoom - pos.X);
        ScrollHost.ScrollToVerticalOffset(contentY * newZoom - pos.Y);
    }

    private void OnSheetMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle)
        {
            _panning = true;
            _panStart = e.GetPosition(ScrollHost);
            _panStartH = ScrollHost.HorizontalOffset;
            _panStartV = ScrollHost.VerticalOffset;
            SheetHost.CaptureMouse();
            e.Handled = true;
            return;
        }

        if (e.ChangedButton == MouseButton.Left)
        {
            // Klassische Textauswahl: auf einem Wort starten, ziehen erweitert
            // den Bereich in Leserichtung (Anker → Cursor)
            ClearSelection();
            if (_words is null)
            {
                _ = EnsureWordsAsync(); // Hintergrund-Nachladen, nächster Versuch trifft
                if (PdfTextService is null)
                    StatusText.Text = "⚠ Kein Text-Dienst verfügbar";
                return;
            }
            if (_words.Count == 0)
            {
                StatusText.Text = "⚠ Keine Textebene in dieser PDF — Werte bitte manuell eintragen";
                return;
            }

            var idx = HitWordIndex(e.GetPosition(SheetHost), strict: true);
            if (idx < 0)
                return; // Klick neben den Text = Auswahl aufheben

            _selecting = true;
            _selAnchor = idx;
            _selCursor = idx;
            UpdateSelectionVisual();
            SheetHost.CaptureMouse();
            e.Handled = true;
        }
    }

    private void OnSheetMouseMove(object sender, MouseEventArgs e)
    {
        if (_panning)
        {
            var pos = e.GetPosition(ScrollHost);
            ScrollHost.ScrollToHorizontalOffset(_panStartH - (pos.X - _panStart.X));
            ScrollHost.ScrollToVerticalOffset(_panStartV - (pos.Y - _panStart.Y));
            return;
        }

        if (_selecting)
        {
            var idx = HitWordIndex(e.GetPosition(SheetHost), strict: false);
            if (idx >= 0 && idx != _selCursor)
            {
                _selCursor = idx;
                UpdateSelectionVisual();
            }
        }
    }

    private void OnSheetMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle && _panning)
        {
            _panning = false;
            SheetHost.ReleaseMouseCapture();
            return;
        }

        if (e.ChangedButton == MouseButton.Left && _selecting)
        {
            _selecting = false;
            SheetHost.ReleaseMouseCapture();
            FinalizeSelection();
            return;
        }

        if (e.ChangedButton == MouseButton.Right)
        {
            if (_selectedWords.Count > 0)
                OpenAssignMenu();
            else
                StatusText.Text = "Erst Text markieren (linke Taste ziehen), dann Rechtsklick = Zuweisen";
            e.Handled = true;
        }
    }

    // ── Toolbar ─────────────────────────────────────────────────────

    private void OnPlankopfClick(object sender, RoutedEventArgs e) => ApplyPlankopfView();

    private void OnFitPageClick(object sender, RoutedEventArgs e) => ApplyFitPageView();

    private void OnCloseClick(object sender, RoutedEventArgs e)
        => CloseRequested?.Invoke(this, EventArgs.Empty);

    private async void OnPrevPageClick(object sender, RoutedEventArgs e)
        => await ChangePageAsync(-1);

    private async void OnNextPageClick(object sender, RoutedEventArgs e)
        => await ChangePageAsync(+1);

    private async Task ChangePageAsync(int delta)
    {
        var target = _currentPage + delta;
        if (_currentPath is null || target < 0 || target >= _pageCount)
            return;

        _currentPage = target;
        var generation = ++_renderGeneration;
        try
        {
            await RenderCurrentPageAsync(generation, startWithPlankopf: false);
        }
        catch (Exception ex)
        {
            if (generation != _renderGeneration)
                return;
            Log.Warning(ex, "Seitenwechsel fehlgeschlagen (Seite {Page})", target + 1);
            StatusText.Text = $"⚠ Seite {target + 1} nicht darstellbar: {ex.Message}";
        }
    }

    private void OnOpenExternalClick(object sender, RoutedEventArgs e)
    {
        if (_currentPath is not null && FileLauncher?.OpenFile(_currentPath) != true)
            StatusText.Text = "⚠ Öffnen in Standard-App fehlgeschlagen";
    }
}
