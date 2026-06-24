using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BauProjektManager.Domain.Enums.PlanManager;
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

    private ManualCaptureViewModel ViewModel => (ManualCaptureViewModel)DataContext;

    public ManualCaptureView()
    {
        InitializeComponent();
        _holdTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(HoldMilliseconds)
        };
        _holdTimer.Tick += OnHoldElapsed;

        Radial.SegmentCommitted += OnSegmentCommitted;
        FileList.SelectionChanged += (_, _) => UpdateSelectionInfo();
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

        var p = e.GetPosition(RootGrid);
        Radial.UpdateHoverFromHost(e.GetPosition(Radial));
        Canvas.SetLeft(Ghost, p.X + 16);
        Canvas.SetTop(Ghost, p.Y + 14);
    }

    protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseUp(e);
        _holdTimer.Stop();
        _downRow = null;

        if (_controller is null)
            return;

        var hit = Radial.HitTestSegment(e.GetPosition(Radial));
        if (hit is not null)
        {
            if (hit.Item.IsAddItem)
                TryQuickAdd(hit.RingIndex); // „+ Neu…": Schnellanlage + Pending (Slice 3)
            else
            {
                // Release-Commit auf dem getroffenen Segment, dann Pending setzen
                _controller.Commit(hit.RingIndex, hit.Item.Name, _captureAnchor?.Item.Candidates);
                ViewModel.CompleteCapture(_controller);
            }
        }
        CloseRadial();
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
                var name = PromptName("Neuer Dokumenttyp", "Name des Dokumenttyps:");
                if (name is null) return;
                committedName = ViewModel.AddDocumentType(name).Name;
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
    }

    private void OnHoldElapsed(object? sender, EventArgs e)
    {
        _holdTimer.Stop();
        if (_downRow is null || DataContext is not ManualCaptureViewModel vm)
            return;

        _captureAnchor = _downRow;
        _controller = vm.BeginCapture(_downRow);

        var candidates = _captureAnchor.Item.Candidates;
        Radial.SetRing(1, _controller.BuildRing1(candidates), selectedName: null, animate: true);
        Radial.ClearRing(2);
        Radial.ClearRing(3);
        var count = vm.SelectedRows.Count;
        Radial.SetCenter(count == 1 ? _captureAnchor.FileName : $"{count} Dateien", "");

        // Overlay am Cursor positionieren (in RootGrid geklemmt)
        var half = RadialSize / 2;
        var x = Math.Clamp(_downPoint.X, half, Math.Max(half, RootGrid.ActualWidth - half));
        var y = Math.Clamp(_downPoint.Y, half, Math.Max(half, RootGrid.ActualHeight - half));
        Canvas.SetLeft(Radial, x - half);
        Canvas.SetTop(Radial, y - half);

        GhostText.Text = count == 1 ? _captureAnchor.FileName : $"{count} Dateien";
        Canvas.SetLeft(Ghost, _downPoint.X + 16);
        Canvas.SetTop(Ghost, _downPoint.Y + 14);

        OverlayCanvas.Visibility = Visibility.Visible;
        Mouse.Capture(this, CaptureMode.SubTree);
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
        Radial.ResetInteraction();
        _controller = null;
        _captureAnchor = null;
        UpdateSelectionInfo();
    }

    // ── Detail-Panel (Slice 2b minimal) ─────────────────────────────

    private void UpdateSelectionInfo()
    {
        var selected = ViewModel.SelectedRows;
        if (selected.Count == 1)
        {
            var row = selected[0];
            SelectionInfo.Text = row.IsPending
                ? $"{row.FileName}\n{row.PendingText}"
                : $"{row.FileName}\n{row.CandidateText}{(row.Reason is not null ? $"\n⚠ {row.Reason}" : "")}";
            TakeUpdateButton.Visibility = row.IsUpdate && !row.IsPending
                ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            SelectionInfo.Text = selected.Count == 0
                ? "Keine Auswahl"
                : $"{selected.Count} Datei(en) ausgewählt — halten & ziehen";
            TakeUpdateButton.Visibility = Visibility.Collapsed;
        }
    }

    private void OnTakeUpdateClick(object sender, RoutedEventArgs e)
    {
        var row = ViewModel.SelectedRows.FirstOrDefault();
        if (row is not null && ViewModel.TakeUpdateCommand.CanExecute(row))
            ViewModel.TakeUpdateCommand.Execute(row);
        UpdateSelectionInfo();
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

    private Brush ThemeBrush(string key) =>
        TryFindResource(key) as Brush ?? Brushes.Gray;
}
