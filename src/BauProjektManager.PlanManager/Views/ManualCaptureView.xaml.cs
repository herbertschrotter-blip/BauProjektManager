using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Controls;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// Gesten-Host der manuellen Plan-Erfassung (BPM-111.05 Slice 2b).
/// Verbindliche Spez: HTML-Header von 02_ManuellSortieren.html.
/// - Maustaste HALTEN (260 ms) auf einer Zeile -> Radial-Overlay am Cursor
///   (Abbruch bei >40 px Bewegung vor Ablauf)
/// - Maus-Capture liegt hier; Hover wird ans Control weitergereicht
///   (UpdateHoverFromHost), Ebenenlogik liegt im RadialSelectionController
/// - Loslassen im Segment = Pending-Zuordnung, ausserhalb = Abbruch
/// - Drag-Ghost (Dateiname bzw. "N Dateien") folgt dem Cursor
/// </summary>
public partial class ManualCaptureView : UserControl
{
    private const int HoldMilliseconds = 260;
    private const double HoldCancelDistance = 40;
    private const double RadialSize = 460;

    private readonly DispatcherTimer _holdTimer;
    private CaptureRowViewModel? _downRow;
    private Point _downPoint;
    private RadialSelectionController? _controller;
    private CaptureRowViewModel? _captureAnchor;

    /// <summary>
    /// Sticky-Radial (Teil 46): True direkt nach dem Einrasten, bis die linke Taste
    /// EINMAL losgelassen wurde. Verhindert, dass das Loslassen der Einrast-Geste
    /// sofort als Commit/Abbruch gilt — das Radial bleibt offen, bis bewusst geklickt wird.
    /// </summary>
    private bool _justLatched;

    /// <summary>
    /// Merkt die Zeile, bei der auf dem Maus-Runter der Selektions-Kollaps der ListBox
    /// unterdrueckt wurde (Klick ohne Modifier auf eine Zeile innerhalb einer Mehrfach-
    /// auswahl). Bleibt es beim reinen Klick (kein Hold/Radial), wird die Einzelauswahl
    /// im MouseUp nachgeholt.
    /// </summary>
    private CaptureRowViewModel? _multiSelectDownRow;

    private ManualCaptureViewModel ViewModel => (ManualCaptureViewModel)DataContext;

    /// <summary>PDF-Render-Port (ADR-062) — vom Host (ProjectDetailView) gesetzt; null = keine Vorschau.</summary>
    public IPdfRenderService? PdfRenderService { get; set; }

    /// <summary>Shell-Launcher (ADR-060) — "In Standard-App öffnen" im Vorschau-Panel.</summary>
    public IFileLauncher? FileLauncher { get; set; }

    /// <summary>Geräte-lokale Settings (device-settings.json) — merkt die Vorschau-Breite.</summary>
    public Infrastructure.Persistence.AppSettingsService? SettingsService { get; set; }

    /// <summary>PDF-Text-Port (ADR-063) — Text markieren + zuweisen in der Vorschau.</summary>
    public IPdfTextService? PdfTextService { get; set; }

    /// <summary>Segmenttyp-Katalog (BPM-108) fürs Zuweisungs-Menü der Vorschau.</summary>
    public ISegmentTypeCatalog? SegmentTypeCatalog { get; set; }

    /// <summary>Die aktuell in der Vorschau gezeigte Zeile (Ziel der Text-Zuweisung).</summary>
    private CaptureRowViewModel? _previewRow;

    private const double DetailDefaultWidth = 320;
    private const double DetailMinWidth = 320;
    private const double DetailMaxWidth = 900;
    private const double PreviewDefaultWidth = 520;
    private const double PreviewMinWidth = 260;
    private const double PreviewMaxWidth = 1600;
    private const double SplitterThickness = 5;

    public ManualCaptureView()
    {
        InitializeComponent();
        PreviewPanel.CloseRequested += (_, _) => { _previewRow = null; SetPreviewVisible(false); };
        PreviewPanel.AssignRequested += OnPreviewTextAssigned;
        PreviewSplitter.DragCompleted += OnPreviewSplitterDragCompleted;
        DetailSplitter.DragCompleted += OnDetailSplitterDragCompleted;
        // Gemerkte Detail-Breite erst nach dem Setzen von SettingsService anwenden
        // (Property kommt per Objekt-Initialisierer nach dem Konstruktor)
        Loaded += (_, _) => ApplyStoredWidths();
        _holdTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(HoldMilliseconds)
        };
        _holdTimer.Tick += OnHoldElapsed;

        Radial.SegmentCommitted += OnSegmentCommitted;
        FileList.SelectionChanged += (_, _) =>
        {
            if (DataContext is ManualCaptureViewModel vm)
                vm.SetSelectedRow();
        };
    }

    // ── Hold-Erkennung ──────────────────────────────────────────────

    private void OnRowPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var container = ItemsControl.ContainerFromElement(FileList, (DependencyObject)e.OriginalSource) as ListBoxItem;
        if (container?.DataContext is not CaptureRowViewModel row)
            return;
        if (row.IsDuplicate || row.IsUpdate)
            return; // Dubletten/Updates oeffnen das Radial nicht (Buckets A/B)

        _downRow = row;
        _downPoint = e.GetPosition(RootGrid);
        _holdTimer.Start();

        // Mehrfachauswahl fuer das Hold-Verschieben bewahren: Klickt der User OHNE
        // Strg/Shift auf eine bereits markierte Zeile innerhalb einer Mehrfachauswahl,
        // wuerde die ListBox die Selektion beim Maus-Runter auf genau diese Zeile
        // kollabieren -> das Radial saehe nur 1 Datei. Wir unterdruecken den Kollaps
        // (e.Handled) und holen die Einzelauswahl bei einem reinen Klick im MouseUp nach.
        var noModifier = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == 0;
        if (noModifier && row.IsSelected && ViewModel.SelectedRows.Count > 1)
        {
            _multiSelectDownRow = row;
            e.Handled = true;
        }
        else
        {
            _multiSelectDownRow = null;
        }
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);

        if (_holdTimer.IsEnabled
            && (e.GetPosition(RootGrid) - _downPoint).Length > HoldCancelDistance)
        {
            _holdTimer.Stop();
            _downRow = null;
        }

        if (_controller is null)
            return;

        // Hover/Dwell macht das Control jetzt selbst (kein Capture mehr) — hier nur der Ghost.
        var p = e.GetPosition(RootGrid);
        Canvas.SetLeft(Ghost, p.X + 16);
        Canvas.SetTop(Ghost, p.Y + 14);
    }

    protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseUp(e);
        _holdTimer.Stop();
        _downRow = null;

        if (_controller is null)
        {
            // Kein Radial gestartet: War es ein reiner Klick auf eine Zeile innerhalb
            // einer Mehrfachauswahl (Kollaps unterdrueckt), jetzt Einzelauswahl nachholen.
            if (_multiSelectDownRow is not null)
            {
                foreach (var r in ViewModel.Rows)
                    r.IsSelected = ReferenceEquals(r, _multiSelectDownRow);
                _multiSelectDownRow = null;
            }

            // Rechtsklick bei GESCHLOSSENEM Radial = Kontextmenue (Spez 111.06)
            if (e.ChangedButton == MouseButton.Right)
                TryOpenRowContextMenu(e);
            return;
        }

        _multiSelectDownRow = null;

        // Sticky-Radial: Das Loslassen der Einrast-Geste schliesst das Radial NICHT.
        if (_justLatched)
        {
            _justLatched = false;
            return;
        }

        // Rechtsklick bei offenem Radial = Dateien lösen + Radial schließen (Abbruch).
        if (e.ChangedButton == MouseButton.Right)
        {
            CloseRadial();
            e.Handled = true;
            return;
        }

        // Linksklick auf ein Segment = zuordnen. Klick ins Leere lässt das Radial offen.
        if (e.ChangedButton == MouseButton.Left)
        {
            var hit = Radial.HitTestSegment(e.GetPosition(Radial));
            if (hit is null)
                return;
            if (hit.Item.IsAddItem)
            {
                TryQuickAdd(hit.RingIndex); // „+ Neu…": Schnellanlage + Pending; schließt bei Erfolg
                return;
            }
            _controller.Commit(hit.RingIndex, hit.Item.Name, _captureAnchor?.Item.Candidates);
            ViewModel.CompleteCapture(_controller);
            CloseRadial();
        }
    }

    // ── Mausrad: dreht NUR die Ebene unter dem Cursor (BPM-111.05 Slice B) ──
    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        base.OnPreviewMouseWheel(e);
        if (_controller is null)
            return;

        var hit = Radial.HitTestSegment(e.GetPosition(Radial));
        if (hit is null)
            return; // nicht über einem Ring → normales Scrollen zulassen

        e.Handled = true;
        _controller.RotateRing(hit.RingIndex, e.Delta < 0 ? 1 : -1);
        Radial.SetRing(hit.RingIndex,
            _controller.BuildRing(hit.RingIndex, _captureAnchor?.Item.Candidates),
            _controller.SelectedNameFor(hit.RingIndex),
            animate: false);
    }

    /// <summary>
    /// „+ Neu…"-Schnellanlage je Ringebene (Slice 3): Name abfragen → Stammdaten
    /// in der DB anlegen → Controller-Stammdaten auffrischen → wie ein normaler
    /// Release committen und die gezogenen Dateien als Pending zuordnen.
    /// </summary>
    private void TryQuickAdd(int ringIndex)
    {
        if (_controller is null)
            return;

        // Maus-Capture vor dem modalen Dialog freigeben
        Mouse.Capture(null);

        var type = _controller.SelectedType;
        string committedName;
        switch (ringIndex)
        {
            case 1:
            {
                // ADR-061: Pflichtdialog (Name + Ablagebereich + Unterteilung + Ordnername)
                // statt reiner Namens-Abfrage.
                var input = PromptNewDocumentType();
                if (input is null) return;
                var (typeName, root, ring2, folder) = input.Value;
                committedName = ViewModel.AddDocumentType(typeName, root, ring2, folder).Name;
                break;
            }
            case 2 when type?.Ring2Source == Ring2Source.BuildingParts:
            {
                var name = PromptName("Neues Bauteil", "Kürzel des Bauteils:");
                if (name is null) return;
                committedName = ViewModel.AddBuildingPart(name).ShortName;
                break;
            }
            case 2 when type?.Ring2Source == Ring2Source.Categories:
            {
                var name = PromptName("Neue Kategorie", "Name der Kategorie:");
                if (name is null) return;
                ViewModel.AddCategory(type, name);
                committedName = name;
                break;
            }
            case 3 when _controller.SelectedBuildingPart is { } part:
            {
                var name = PromptName("Neues Geschoss", "Geschoss-Bezeichnung:");
                if (name is null) return;
                ViewModel.AddBuildingLevel(part, name);
                committedName = name;
                break;
            }
            default:
                return;
        }

        _controller.RefreshStammdaten(ViewModel.Types, ViewModel.Parts);
        _controller.Commit(ringIndex, committedName, _captureAnchor?.Item.Candidates);
        ViewModel.CompleteCapture(_controller);
        CloseRadial();
    }

    private void OnHoldElapsed(object? sender, EventArgs e)
    {
        _holdTimer.Stop();
        if (_downRow is null || DataContext is not ManualCaptureViewModel vm)
            return;

        _captureAnchor = _downRow;
        _controller = vm.BeginCapture(_downRow);
        if (_controller is null)
        {
            // Slice B: Bulk-Deckel — Radial öffnet nicht, Grund steht in der Statuszeile.
            _captureAnchor = null;
            _downRow = null;
            return;
        }

        var candidates = _captureAnchor.Item.Candidates;
        Radial.SetRing(1, _controller.BuildRing1(candidates), selectedName: null, animate: true);
        Radial.ClearRing(2);
        Radial.ClearRing(3);
        var count = vm.SelectedRows.Count;
        // Slice C: Kombi-Hinweis im Radial-Zentrum, solange kein Typ gewählt ist
        var combiHint = vm.SelectedRows.Any(r => r.IsCombi) ? "⚠ Kombi-Plan" : "";
        Radial.SetCenter(count == 1 ? _captureAnchor.FileName : $"{count} Dateien", combiHint);

        // Overlay am Cursor positionieren (in RootGrid geklemmt)
        var half = RadialSize / 2;
        var x = Math.Clamp(_downPoint.X, half, Math.Max(half, RootGrid.ActualWidth - half));
        var y = Math.Clamp(_downPoint.Y, half, Math.Max(half, RootGrid.ActualHeight - half));
        Canvas.SetLeft(Radial, x - half);
        Canvas.SetTop(Radial, y - half);

        GhostText.Text = count == 1 ? _captureAnchor.FileName : $"{count} Dateien";
        Canvas.SetLeft(Ghost, _downPoint.X + 16);
        Canvas.SetTop(Ghost, _downPoint.Y + 14);

        // Sticky-Radial: KEIN Mouse.Capture mehr. Das Overlay wird hit-test-fähig,
        // sodass das Control eigene Hover/Dwell-Events bekommt und die Maus nach dem
        // Loslassen der Taste frei über die Ringe fährt.
        OverlayCanvas.Visibility = Visibility.Visible;
        OverlayCanvas.IsHitTestVisible = true;
        _justLatched = true;
        _downRow = null;
    }

    // ── Radial-Ebenenlogik (Controller entscheidet, Host rendert) ───

    private void OnSegmentCommitted(object? sender, RadialSegmentEventArgs e)
    {
        if (_controller is null)
            return;

        var update = _controller.Commit(e.RingIndex, e.Item.Name, _captureAnchor?.Item.Candidates);

        if (update.Ring2 is not null)
        {
            if (update.Ring2.Count == 0) Radial.ClearRing(2);
            else Radial.SetRing(2, update.Ring2, _controller.SelectedPart, update.Ring2Animate);
        }
        if (update.Ring3 is not null)
        {
            if (update.Ring3.Count == 0) Radial.ClearRing(3);
            else Radial.SetRing(3, update.Ring3, _controller.SelectedLevel, update.Ring3Animate);
        }
        var count = ViewModel.SelectedRows.Count;
        Radial.SetCenter(count == 1 ? _captureAnchor?.FileName ?? "" : $"{count} Dateien",
            update.CenterSecondary);
    }

    private void CloseRadial()
    {
        Mouse.Capture(null);
        OverlayCanvas.Visibility = Visibility.Collapsed;
        OverlayCanvas.IsHitTestVisible = false;
        Radial.ResetInteraction();
        _controller = null;
        _captureAnchor = null;
        _justLatched = false;
        ViewModel.SetSelectedRow();
    }

    // ── Vorschau (BPM-111.06 Slice C1) ──────────────────────────────

    /// <summary>
    /// Kontextmenue auf einer Eingang-Zeile (Rechtsklick bei geschlossenem Radial,
    /// Spez 111.06): "Vorschau" (Slice C, nur PDF) + "Datei oeffnen" /
    /// "Im Explorer zeigen" (Slice B, via IFileLauncher/ADR-060).
    /// </summary>
    private void TryOpenRowContextMenu(MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source)
            return;
        var container = ItemsControl.ContainerFromElement(FileList, source) as ListBoxItem;
        if (container?.DataContext is not CaptureRowViewModel row)
            return;

        foreach (var r in ViewModel.Rows)
            r.IsSelected = ReferenceEquals(r, row);

        // Theme-Styles (Dialogs.xaml) — keyed, daher explizit zuweisen
        var itemStyle = TryFindResource("BpmMenuItem") as Style;
        MenuItem Item(string header, bool enabled)
            => new() { Header = header, IsEnabled = enabled, Style = itemStyle };

        // Slice C3: PDFs direkt; DWGs über die gepaarte PDF (Eingangs-Partner
        // oder Archiv-Revision) — sonst bleibt der Menüpunkt inaktiv.
        var previewSource = PdfRenderService is null
            ? null : ViewModel.ResolvePreviewSource(row);
        var previewItem = Item("Vorschau", previewSource is not null);
        previewItem.Click += async (_, _) => await OpenPreviewAsync(previewSource!);

        var openItem = Item("Datei öffnen", FileLauncher is not null);
        openItem.Click += (_, _) => LaunchRow(row, reveal: false);

        var revealItem = Item("Im Explorer zeigen", FileLauncher is not null);
        revealItem.Click += (_, _) => LaunchRow(row, reveal: true);

        var menu = new ContextMenu { PlacementTarget = container };
        if (TryFindResource("BpmContextMenu") is Style menuStyle)
            menu.Style = menuStyle;
        var separator = new Separator();
        if (TryFindResource("BpmMenuSeparator") is Style separatorStyle)
            separator.Style = separatorStyle;

        menu.Items.Add(previewItem);
        menu.Items.Add(separator);
        menu.Items.Add(openItem);
        menu.Items.Add(revealItem);
        menu.IsOpen = true;
        e.Handled = true;
    }

    /// <summary>Slice B: Datei in der Standard-App oeffnen bzw. im Explorer zeigen.</summary>
    private void LaunchRow(CaptureRowViewModel row, bool reveal)
    {
        if (FileLauncher is null)
            return;
        var absolutePath = Path.Combine(ViewModel.ProjectRootPath, row.RelativePath);
        var ok = reveal ? FileLauncher.RevealInExplorer(absolutePath) : FileLauncher.OpenFile(absolutePath);
        if (!ok)
            ViewModel.StatusText = $"⚠ {(reveal ? "Explorer" : "Öffnen")} fehlgeschlagen: {row.FileName}";
    }

    /// <summary>
    /// Blendet das Vorschau-Panel ein (Variante B) und zeigt die aufgelöste
    /// Quelle an (Slice C3): bei DWG-Zeilen die gepaarte PDF; Zuweisungsziel
    /// ist source.Row (NULL bei Archiv-Anzeige = Zuweisen inaktiv).
    /// </summary>
    private async Task OpenPreviewAsync(PreviewSource source)
    {
        if (PdfRenderService is null)
            return;

        PreviewPanel.PdfRenderService = PdfRenderService;
        PreviewPanel.FileLauncher = FileLauncher;
        PreviewPanel.PdfTextService = PdfTextService;
        PreviewPanel.SegmentTypeCatalog = SegmentTypeCatalog;
        _previewRow = source.Row;
        SetPreviewVisible(true);

        if (source.Note is not null)
            ViewModel.StatusText = source.Note;

        var absolutePath = Path.Combine(ViewModel.ProjectRootPath, source.RelativePath);
        await PreviewPanel.ShowFileAsync(absolutePath);
    }

    /// <summary>
    /// Text-Zuweisung aus der Vorschau (BPM-118): markierter Text → Ziel der
    /// aktuell gezeigten Zeile. Revisions-Ziele landen am Row-Zustand
    /// (change_note/released_at, sichtbar in der Historie); Identitäts-Segmente
    /// füllen die Edit-Felder des Detail-Panels (Anwenden = Re-Match);
    /// übrige Segmente werden für den Import vorgemerkt.
    /// </summary>
    private void OnPreviewTextAssigned(object? sender, PdfTextAssignedEventArgs e)
    {
        var row = _previewRow;
        if (row is null)
            return;

        // Zeile in den Fokus holen, damit das Detail-Panel das Ziel zeigt
        foreach (var r in ViewModel.Rows)
            r.IsSelected = ReferenceEquals(r, row);
        ViewModel.SetSelectedRow();
        var detail = ViewModel.SelectedDetail;

        var text = e.Text.Trim();
        switch (e.Kind)
        {
            case PdfAssignKind.ChangeNote:
                row.ChangeNote = text;
                ViewModel.StatusText = "✓ Änderungshinweis übernommen";
                break;

            case PdfAssignKind.ReleasedAt:
                if (!TryParseDate(text, out var iso))
                {
                    ViewModel.StatusText = $"⚠ „{text}\" ist kein gültiges Datum";
                    return;
                }
                row.ReleasedAtIso = iso;
                ViewModel.StatusText = "✓ Index-Datum übernommen";
                break;

            case PdfAssignKind.Segment when e.SegmentTypeId == SegmentTypeIds.PlanNumber:
                if (detail is not null) detail.EditPlanNumber = text;
                ViewModel.StatusText = "✓ Plannummer ins Panel übernommen — „Re-Match anwenden\" prüft";
                break;

            case PdfAssignKind.Segment when e.SegmentTypeId == SegmentTypeIds.PlanIndex:
                if (detail is not null) detail.EditIndex = text;
                ViewModel.StatusText = "✓ Index ins Panel übernommen — „Re-Match anwenden\" prüft";
                break;

            case PdfAssignKind.Segment when e.SegmentTypeId == SegmentTypeIds.Description:
                row.Title = text;
                ViewModel.StatusText = "✓ Bezeichnung übernommen";
                break;

            case PdfAssignKind.Segment when e.SegmentTypeId is not null:
                row.AssignedSegments[e.SegmentTypeId] = new AssignedSegmentValue(
                    e.SegmentTypeId,
                    string.IsNullOrEmpty(e.SegmentTypeTokenKey) ? e.SegmentTypeId : e.SegmentTypeTokenKey,
                    text);
                ViewModel.StatusText = $"✓ Segment „{e.SegmentTypeName}\" für den Import vorgemerkt";
                break;
        }

        ViewModel.SetSelectedRow(); // Historie/Panel auffrischen
    }

    /// <summary>Datum aus dem Plankopf: dd.MM.yyyy oder yyyy-MM-dd → ISO-UTC.</summary>
    private static bool TryParseDate(string text, out string isoUtc)
    {
        string[] formats = ["dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd"];
        if (DateTime.TryParseExact(text, formats, null,
                System.Globalization.DateTimeStyles.AssumeUniversal
                | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt))
        {
            isoUtc = dt.ToString("o");
            return true;
        }
        isoUtc = string.Empty;
        return false;
    }

    private double StoredDetailWidth()
        => Math.Clamp(
            SettingsService?.LoadDevice().UiLayout.PlanDetailWidth ?? DetailDefaultWidth,
            DetailMinWidth, DetailMaxWidth);

    private double StoredPreviewWidth()
        => Math.Clamp(
            SettingsService?.LoadDevice().UiLayout.PlanPreviewWidth ?? PreviewDefaultWidth,
            PreviewMinWidth, PreviewMaxWidth);

    /// <summary>Gemerkte Panel-Breiten beim Laden anwenden (Vorschau ist da noch zu).</summary>
    private void ApplyStoredWidths()
        => RightColumn.Width = new GridLength(StoredDetailWidth());

    /// <summary>
    /// Vorschau-Spalte ein-/ausblenden: RightColumn = Detail-Breite plus
    /// Splitter + Vorschau-Breite (beide geräte-lokal gemerkt); der innere
    /// Vorschau-Splitter ist nur bei offener Vorschau sichtbar.
    /// </summary>
    private void SetPreviewVisible(bool visible)
    {
        PreviewHost.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        PreviewSplitter.Visibility = PreviewHost.Visibility;

        var detailWidth = StoredDetailWidth();
        var previewWidth = StoredPreviewWidth();
        PreviewColumnInner.Width = new GridLength(visible ? previewWidth : 0);
        RightColumn.Width = new GridLength(
            visible ? detailWidth + SplitterThickness + previewWidth : detailWidth);
    }

    /// <summary>Aktuelle Detail-Breite aus dem Layout ableiten (RightColumn minus Vorschau-Anteil).</summary>
    private double CurrentDetailWidth()
        => PreviewHost.Visibility == Visibility.Visible
            ? RightColumn.ActualWidth - SplitterThickness - PreviewColumnInner.ActualWidth
            : RightColumn.ActualWidth;

    /// <summary>Detail-Splitter losgelassen: Detail-Breite geräte-lokal merken.</summary>
    private void OnDetailSplitterDragCompleted(
        object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
    {
        if (SettingsService is null)
            return;
        var device = SettingsService.LoadDevice();
        device.UiLayout.PlanDetailWidth =
            Math.Clamp(CurrentDetailWidth(), DetailMinWidth, DetailMaxWidth);
        SettingsService.SaveDevice(device);
    }

    /// <summary>
    /// Vorschau-Splitter losgelassen: er verschiebt die Grenze Detail ↔ Vorschau
    /// innerhalb der rechten Spalte — beide Breiten geräte-lokal merken.
    /// </summary>
    private void OnPreviewSplitterDragCompleted(
        object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
    {
        if (SettingsService is null || PreviewHost.Visibility != Visibility.Visible)
            return;

        var device = SettingsService.LoadDevice();
        device.UiLayout.PlanPreviewWidth =
            Math.Clamp(PreviewColumnInner.ActualWidth, PreviewMinWidth, PreviewMaxWidth);
        device.UiLayout.PlanDetailWidth =
            Math.Clamp(CurrentDetailWidth(), DetailMinWidth, DetailMaxWidth);
        SettingsService.SaveDevice(device);
    }

    // ── Schnellanlage-Dialog (Slice 3) ──────────────────────────────

    /// <summary>
    /// Minimaler modaler Namens-Dialog (Theme-Tokens). Liefert den getrimmten
    /// Namen oder NULL bei Abbruch/leerer Eingabe.
    /// </summary>
    private string? PromptName(string title, string prompt)
    {
        var win = new Window
        {
            Title = title,
            Width = 360,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow,
            Background = ThemeBrush("BpmBgBase")
        };

        var grid = new Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var lbl = new TextBlock
        {
            Text = prompt,
            Foreground = ThemeBrush("BpmTextSecondary"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        Grid.SetRow(lbl, 0);

        var box = new TextBox
        {
            FontSize = 14,
            Padding = new Thickness(6, 4, 6, 4),
            Background = ThemeBrush("BpmBgElevated"),
            Foreground = ThemeBrush("BpmTextBright"),
            BorderBrush = ThemeBrush("BpmAccentPrimary"),
            CaretBrush = ThemeBrush("BpmTextBright")
        };
        Grid.SetRow(box, 1);
        box.Loaded += (_, _) => box.Focus();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };
        var ok = new Button { Content = "Anlegen", Width = 90, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new Button { Content = "Abbrechen", Width = 90, IsCancel = true };
        ok.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(box.Text))
                win.DialogResult = true;
        };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        Grid.SetRow(buttons, 2);

        grid.Children.Add(lbl);
        grid.Children.Add(box);
        grid.Children.Add(buttons);
        win.Content = grid;

        return win.ShowDialog() == true ? box.Text.Trim() : null;
    }

    /// <summary>
    /// Pflichtdialog fuer die Dokumenttyp-Schnellanlage (ADR-061): Name +
    /// Ablagebereich (root_relative_path) + Unterteilung (Ring2Source) +
    /// optionaler Ordnername. Liefert die Eingaben oder NULL bei Abbruch.
    /// </summary>
    private (string Name, string Root, Ring2Source Ring2, string? FolderName)? PromptNewDocumentType()
    {
        var win = new Window
        {
            Title = "Neuer Dokumenttyp",
            Width = 380,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow,
            Background = ThemeBrush("BpmBgBase")
        };

        var panel = new StackPanel { Margin = new Thickness(16) };

        TextBlock Label(string t) => new()
        {
            Text = t,
            Foreground = ThemeBrush("BpmTextSecondary"),
            Margin = new Thickness(0, 0, 0, 4)
        };
        TextBox Field() => new()
        {
            FontSize = 14,
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(0, 0, 0, 12),
            Background = ThemeBrush("BpmBgElevated"),
            Foreground = ThemeBrush("BpmTextBright"),
            BorderBrush = ThemeBrush("BpmAccentPrimary"),
            CaretBrush = ThemeBrush("BpmTextBright")
        };
        ComboBox Combo() => new()
        {
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 12),
            Background = ThemeBrush("BpmBgElevated"),
            Foreground = ThemeBrush("BpmTextBright")
        };

        panel.Children.Add(Label("Name des Dokumenttyps:"));
        var nameBox = Field();
        nameBox.Loaded += (_, _) => nameBox.Focus();
        panel.Children.Add(nameBox);

        panel.Children.Add(Label("Ablagebereich:"));
        var rootCombo = Combo();
        var roots = ViewModel.Types
            .Select(t => t.RootRelativePath)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Concat(new[] { "01 Planunterlagen", "06 Protokolle" })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var r in roots) rootCombo.Items.Add(r);
        rootCombo.SelectedItem = roots.FirstOrDefault(
            r => r.Equals("01 Planunterlagen", StringComparison.OrdinalIgnoreCase)) ?? roots.FirstOrDefault();
        panel.Children.Add(rootCombo);

        panel.Children.Add(Label("Unterteilung:"));
        var ring2Combo = Combo();
        ring2Combo.Items.Add("Bauteil / Geschoss");
        ring2Combo.Items.Add("Kategorien");
        ring2Combo.Items.Add("Keine");
        ring2Combo.SelectedIndex = 0;
        panel.Children.Add(ring2Combo);

        panel.Children.Add(Label("Ordnername (leer = automatisch aus Name):"));
        var folderBox = Field();
        panel.Children.Add(folderBox);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var ok = new Button { Content = "Anlegen", Width = 90, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new Button { Content = "Abbrechen", Width = 90, IsCancel = true };
        ok.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(nameBox.Text) && rootCombo.SelectedItem is not null)
                win.DialogResult = true;
        };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);

        win.Content = panel;

        if (win.ShowDialog() != true)
            return null;

        var ring2 = ring2Combo.SelectedIndex switch
        {
            1 => Ring2Source.Categories,
            2 => Ring2Source.None,
            _ => Ring2Source.BuildingParts
        };
        var folder = string.IsNullOrWhiteSpace(folderBox.Text) ? null : folderBox.Text.Trim();
        return (nameBox.Text.Trim(), (string)rootCombo.SelectedItem!, ring2, folder);
    }

    private Brush ThemeBrush(string key) =>
        TryFindResource(key) as Brush ?? Brushes.Gray;
}
