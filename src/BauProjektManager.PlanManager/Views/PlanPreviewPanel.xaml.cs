using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using Serilog;

namespace BauProjektManager.PlanManager.Views;

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
    private const double A4WidthMm = 210;
    private const double A4HeightMm = 297;
    private const double MinZoom = 0.05;
    private const double MaxZoom = 8.0;
    private const string InteractionHint = "Rad = Zoom · Mitteltaste = Pan";

    /// <summary>PDF-Render-Port (ADR-062) — vom Host gesetzt, bevor ShowFileAsync läuft.</summary>
    public IPdfRenderService? PdfRenderService { get; set; }

    /// <summary>Shell-Launcher (ADR-060) — "↗ In Standard-App öffnen".</summary>
    public IFileLauncher? FileLauncher { get; set; }

    /// <summary>Vom ✕-Button ausgelöst — der Host blendet die Panel-Spalte aus.</summary>
    public event EventHandler? CloseRequested;

    private string? _currentPath;
    private int _renderGeneration;
    private int _pageCount;
    private int _currentPage; // 0-basiert
    private PdfPageRender? _page;

    private bool _panning;
    private Point _panStart;
    private double _panStartH;
    private double _panStartV;

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
        StatusText.Text = "Rendere …";

        PdfPageRender page;
        await using (var stream = File.OpenRead(_currentPath))
            page = await PdfRenderService.RenderPageAsPngAsync(stream, _currentPage, RenderPixelWidth);
        if (generation != _renderGeneration)
            return;
        _page = page;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = new MemoryStream(page.Png);
        image.EndInit();
        image.Freeze();
        PageImage.Source = image;

        PageLabel.Text = $"{_currentPage + 1}/{_pageCount}";
        PrevPageButton.IsEnabled = _currentPage > 0;
        NextPageButton.IsEnabled = _currentPage < _pageCount - 1;
        StatusText.Text = $"{Path.GetFileName(_currentPath)} · Seite {_currentPage + 1} von {_pageCount} · {InteractionHint}";

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

    private void OnImageMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
            return;
        _panning = true;
        _panStart = e.GetPosition(ScrollHost);
        _panStartH = ScrollHost.HorizontalOffset;
        _panStartV = ScrollHost.VerticalOffset;
        PageImage.CaptureMouse();
        e.Handled = true;
    }

    private void OnImageMouseMove(object sender, MouseEventArgs e)
    {
        if (!_panning)
            return;
        var pos = e.GetPosition(ScrollHost);
        ScrollHost.ScrollToHorizontalOffset(_panStartH - (pos.X - _panStart.X));
        ScrollHost.ScrollToVerticalOffset(_panStartV - (pos.Y - _panStart.Y));
    }

    private void OnImageMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
            return;
        _panning = false;
        PageImage.ReleaseMouseCapture();
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
