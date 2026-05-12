using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// Native WPF DragAdorner fuer Wizard-Schritt 2 (Field-Type-Chip-Drag).
/// Rendert eine semi-transparente Kopie des Source-Chips an der Maus-Position,
/// versetzt um den Klick-Offset (Maus haengt exakt dort wo der User geklickt hat).
/// </summary>
public class DragAdorner : Adorner
{
    private readonly Rectangle _content;
    private readonly Size _adornerSize;
    private readonly Point _clickOffset;
    private Point _currentMousePos;

    public DragAdorner(UIElement adornedElement, FrameworkElement source, Point clickOffsetInSource)
        : base(adornedElement)
    {
        _adornerSize = new Size(
            source.ActualWidth > 0 ? source.ActualWidth : 80,
            source.ActualHeight > 0 ? source.ActualHeight : 24);
        _clickOffset = clickOffsetInSource;

        // VisualBrush rendert das Source-Element als Brush — bei statischen Elementen sicher.
        var brush = new VisualBrush(source)
        {
            Stretch = Stretch.None,
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top
        };

        _content = new Rectangle
        {
            Width = _adornerSize.Width,
            Height = _adornerSize.Height,
            Fill = brush,
            RadiusX = 3,
            RadiusY = 3,
            Opacity = 0.85,
            IsHitTestVisible = false
        };
        AddVisualChild(_content);
        IsHitTestVisible = false;
    }

    public void UpdatePosition(Point mousePos)
    {
        _currentMousePos = mousePos;
        InvalidateArrange();
    }

    protected override int VisualChildrenCount => 1;
    protected override Visual GetVisualChild(int index) => _content;

    protected override Size MeasureOverride(Size constraint)
    {
        _content.Measure(_adornerSize);
        return _adornerSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var topLeft = new Point(
            _currentMousePos.X - _clickOffset.X,
            _currentMousePos.Y - _clickOffset.Y);
        _content.Arrange(new Rect(topLeft, _adornerSize));
        return finalSize;
    }
}

public class CountToVisInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
        => value is int count && count > 0
            ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CountToVisZeroConverter : IValueConverter
{
    public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
        => value is int count && count == 0
            ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
        => value is bool b ? !b : true;

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
        => value is bool b ? !b : true;
}

/// <summary>
/// FieldType -> Brush fuer farbige Segment-Tokens (Wizard Schritt 2).
/// null/unbekannt -> BpmBgElevated (neutral grau).
/// </summary>
public class FieldTypeToBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
    {
        var key = value is FieldType ft
            ? ft switch
            {
                FieldType.PlanNumber => "BpmFieldPlanNumber",
                FieldType.PlanIndex => "BpmFieldPlanIndex",
                FieldType.ProjectNumber => "BpmFieldProjectNumber",
                FieldType.Geschoss => "BpmFieldGeschoss",
                FieldType.Planart => "BpmFieldPlanart",
                FieldType.Description => "BpmFieldDescription",
                FieldType.Ignore => "BpmFieldIgnore",
                _ => "BpmFieldDefault"
            }
            : "BpmBgElevated";
        return Application.Current.TryFindResource(key);
    }

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// FieldType -> true wenn nicht zugewiesen. Triggert dashed border + unset-Label.
/// </summary>
public class FieldTypeIsUnsetConverter : IValueConverter
{
    public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
        => value is not FieldType;

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// FieldType -> kurzes UI-Label fuer Token-Unterzeile.
/// Pflicht-Marker (★) bei PlanNumber. null -> "? Typ waehlen".
/// </summary>
public class FieldTypeToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
        => value is FieldType ft
            ? ft switch
            {
                FieldType.PlanNumber => "Plannr. ★",
                FieldType.PlanIndex => "Index",
                FieldType.ProjectNumber => "Projektnr.",
                FieldType.Description => "Bezeichnung",
                FieldType.Datum => "Datum",
                FieldType.Geschoss => "Geschoss",
                FieldType.Haus => "Haus",
                FieldType.Planart => "Planart",
                FieldType.Objekt => "Objekt",
                FieldType.Bauteil => "Bauteil",
                FieldType.Bauabschnitt => "Bauabschnitt",
                FieldType.Stiege => "Stiege",
                FieldType.Achse => "Achse",
                FieldType.Zone => "Zone",
                FieldType.Block => "Block",
                FieldType.Ignore => "Ignorieren",
                FieldType.Custom => "Eigener Typ",
                _ => ft.ToString()
            }
            : "? Typ waehlen";

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// FieldType -> Opacity. Ignore-Felder sind gedaempft (0.55).
/// </summary>
public class FieldTypeToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
        => value is FieldType.Ignore ? 0.55 : 1.0;

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public partial class ProfileWizardDialog : Window
{
    private readonly ProfileWizardViewModel _vm;

    public ProfileWizardDialog(
        Project? project = null,
        IProfileManager? profileManager = null,
        PatternTemplateService? templateService = null,
        string? appDataPath = null)
    {
        Resources.Add("CountToVisInverse", new CountToVisInverseConverter());
        Resources.Add("CountToVisZero", new CountToVisZeroConverter());
        Resources.Add("BoolToVis", new BoolToVisConverter());
        Resources.Add("BoolToVisInverse2", new InverseBoolConverter());
        Resources.Add("FieldTypeToBrush", new FieldTypeToBrushConverter());
        Resources.Add("FieldTypeIsUnset", new FieldTypeIsUnsetConverter());
        Resources.Add("FieldTypeToLabel", new FieldTypeToLabelConverter());
        Resources.Add("FieldTypeToOpacity", new FieldTypeToOpacityConverter());
        InitializeComponent();

        _vm = new ProfileWizardViewModel(project, profileManager, templateService, appDataPath);
        DataContext = _vm;

        Loaded += (_, _) => UpdateStepVisibility();
    }

    private void OnFileNameKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _vm.ParseFileNameCommand.Execute(null);
    }

    private void OnFieldTypeSelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo
            && combo.DataContext is FileNameSegment segment
            && combo.SelectedItem is FieldTypeOption option)
        {
            _vm.OnFieldTypeChanged(segment, option);
        }
    }

    // ── Schritt 2: Native WPF Drag&Drop mit eigenem Adorner ──
    // Drag-Source: PreviewMouseDown speichert Startpunkt + Klick-Offset.
    // PreviewMouseMove erkennt Threshold und startet DragDrop.DoDragDrop mit Adorner.
    // Window's PreviewDragOver updated die Adorner-Position kontinuierlich.

    private Point _dragStartPoint;
    private FrameworkElement? _dragSourceChip;
    private Point _clickOffsetInChip;
    private DragAdorner? _currentAdorner;
    private AdornerLayer? _adornerLayer;

    private void OnChipPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe
            && fe.DataContext is FieldTypeOption opt
            && opt.Value.HasValue)
        {
            _dragStartPoint = e.GetPosition(this);
            _dragSourceChip = fe;
            _clickOffsetInChip = e.GetPosition(fe);
        }
    }

    private void OnChipPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragSourceChip is null) return;

        var current = e.GetPosition(this);
        if (Math.Abs(current.X - _dragStartPoint.X) <= SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(current.Y - _dragStartPoint.Y) <= SystemParameters.MinimumVerticalDragDistance)
            return;

        if (_dragSourceChip.DataContext is not FieldTypeOption opt) return;

        var chip = _dragSourceChip;
        var offset = _clickOffsetInChip;
        _dragSourceChip = null;

        // Adorner-Layer aus mehreren Quellen versuchen — Window's AdornerDecorator
        // ist nicht immer direkt erreichbar.
        var contentRoot = (UIElement)Content;
        _adornerLayer = AdornerLayer.GetAdornerLayer(contentRoot)
                        ?? AdornerLayer.GetAdornerLayer(chip);

        if (_adornerLayer != null)
        {
            _currentAdorner = new DragAdorner(contentRoot, chip, offset);
            _currentAdorner.UpdatePosition(current);
            _adornerLayer.Add(_currentAdorner);
        }

        try
        {
            DragDrop.DoDragDrop(chip,
                new DataObject(typeof(FieldTypeOption), opt),
                DragDropEffects.Copy);
        }
        finally
        {
            if (_currentAdorner != null && _adornerLayer != null)
            {
                _adornerLayer.Remove(_currentAdorner);
            }
            _currentAdorner = null;
            _adornerLayer = null;
        }
    }

    /// <summary>Updated die Adorner-Position waehrend des Drags (Window-Level Event).</summary>
    private void OnWindowPreviewDragOver(object sender, DragEventArgs e)
    {
        if (_currentAdorner != null)
        {
            var pos = e.GetPosition((IInputElement)Content);
            _currentAdorner.UpdatePosition(pos);
        }
    }

    private void OnSegmentDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(FieldTypeOption))
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnSegmentDrop(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement fe
            && fe.DataContext is FileNameSegment segment
            && e.Data.GetData(typeof(FieldTypeOption)) is FieldTypeOption option)
        {
            _vm.OnFieldTypeChanged(segment, option);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Rechtsklick auf Token -> Segment-Zuweisung zuruecksetzen (FieldType auf null).
    /// </summary>
    private void OnTokenRightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe
            && fe.DataContext is FileNameSegment segment)
        {
            _vm.ResetSegmentFieldType(segment);
            e.Handled = true;
        }
    }

    private void OnIndexSourceChecked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb
            && rb.DataContext is IndexSourceOption option)
        {
            _vm.SelectedIndexSource = option.Value;
        }
    }

    private void OnHierarchyCheckChanged(object sender, RoutedEventArgs e)
    {
        _vm.OnHierarchyLevelChanged();
    }

    private void OnRecognitionSegmentClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn
            && btn.DataContext is RecognitionSegment seg)
        {
            seg.IsSelected = !seg.IsSelected;
            _vm.OnRecognitionSegmentToggled();
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        _vm.GoBackCommand.Execute(null);
        UpdateStepVisibility();
    }

    private void OnNext(object sender, RoutedEventArgs e)
    {
        if (_vm.CurrentStep >= _vm.TotalSteps)
        {
            _vm.SaveProfileCommand.Execute(null);
            DialogResult = _vm.ProfileSaved;
            Close();
            return;
        }

        _vm.GoNextCommand.Execute(null);
        UpdateStepVisibility();
    }

    private void UpdateStepVisibility()
    {
        Step1Panel.Visibility = _vm.CurrentStep == 1
            ? Visibility.Visible : Visibility.Collapsed;
        Step2Panel.Visibility = _vm.CurrentStep == 2
            ? Visibility.Visible : Visibility.Collapsed;
        Step3Panel.Visibility = _vm.CurrentStep == 3
            ? Visibility.Visible : Visibility.Collapsed;
        Step4Panel.Visibility = _vm.CurrentStep == 4
            ? Visibility.Visible : Visibility.Collapsed;
        Step5Panel.Visibility = _vm.CurrentStep == 5
            ? Visibility.Visible : Visibility.Collapsed;

        // Progress Dots — 3-stufig: done (vergangen) / active (aktuell) / inactive (zukuenftig)
        var done = (System.Windows.Media.Brush)FindResource("BpmBgActive");
        var active = (System.Windows.Media.Brush)FindResource("BpmAccentPrimary");
        var inactive = (System.Windows.Media.Brush)FindResource("BpmBorderDefault");

        System.Windows.Media.Brush DotBrush(int dotIndex) =>
            _vm.CurrentStep == dotIndex ? active
            : _vm.CurrentStep > dotIndex ? done
            : inactive;

        Dot1.Fill = DotBrush(1);
        Dot2.Fill = DotBrush(2);
        Dot3.Fill = DotBrush(3);
        Dot4.Fill = DotBrush(4);
        Dot5.Fill = DotBrush(5);

        StepCounter.Text =
            $"Schritt {_vm.CurrentStep} von {_vm.TotalSteps}";

        NextButton.Content = _vm.CurrentStep >= _vm.TotalSteps
            ? "Speichern" : "Weiter";
    }
}
