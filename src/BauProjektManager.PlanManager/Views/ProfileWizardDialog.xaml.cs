using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

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

    // ── Schritt 2: Drag&Drop Token-Zuweisung (BPM-080.05) ──

    private void OnFieldChipMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe
            && fe.DataContext is FieldTypeOption option
            && option.Value.HasValue)
        {
            DragDrop.DoDragDrop(fe, option, DragDropEffects.Copy);
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
