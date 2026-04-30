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
        InitializeComponent();

        _vm = new ProfileWizardViewModel(project, profileManager, templateService, appDataPath);
        DataContext = _vm;
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

        // Progress Dots
        var accent = FindResource("BpmAccentPrimary");
        var inactive = FindResource("BpmBorderDefault");
        var a = (System.Windows.Media.Brush)accent;
        var i = (System.Windows.Media.Brush)inactive;
        Dot2.Fill = _vm.CurrentStep >= 2 ? a : i;
        Dot3.Fill = _vm.CurrentStep >= 3 ? a : i;
        Dot4.Fill = _vm.CurrentStep >= 4 ? a : i;
        Dot5.Fill = _vm.CurrentStep >= 5 ? a : i;

        StepCounter.Text =
            $"Schritt {_vm.CurrentStep} von {_vm.TotalSteps}";

        NextButton.Content = _vm.CurrentStep >= _vm.TotalSteps
            ? "Speichern" : "Weiter";
    }
}
