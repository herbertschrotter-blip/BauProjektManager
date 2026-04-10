using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// Count > 0 -> Visible, Count == 0 -> Collapsed.
/// </summary>
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

public partial class ProfileWizardDialog : Window
{
    private readonly ProfileWizardViewModel _vm;

    public ProfileWizardDialog(Project? project = null)
    {
        Resources.Add("CountToVisInverse", new CountToVisInverseConverter());
        Resources.Add("BoolToVis", new BoolToVisConverter());
        InitializeComponent();

        _vm = new ProfileWizardViewModel(project);
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
            // Letzter Schritt: Dialog abschliessen
            DialogResult = true;
            Close();
            return;
        }

        _vm.GoNextCommand.Execute(null);
        UpdateStepVisibility();
    }

    /// <summary>
    /// Blendet Step-Panels um und aktualisiert Progress Dots.
    /// </summary>
    private void UpdateStepVisibility()
    {
        Step1Panel.Visibility = _vm.CurrentStep == 1
            ? Visibility.Visible : Visibility.Collapsed;
        Step2Panel.Visibility = _vm.CurrentStep == 2
            ? Visibility.Visible : Visibility.Collapsed;

        // Progress Dots
        var accent = FindResource("BpmAccentPrimary");
        var inactive = FindResource("BpmBorderDefault");
        Dot2.Fill = _vm.CurrentStep >= 2
            ? (System.Windows.Media.Brush)accent
            : (System.Windows.Media.Brush)inactive;
        Dot3.Fill = _vm.CurrentStep >= 3
            ? (System.Windows.Media.Brush)accent
            : (System.Windows.Media.Brush)inactive;
        Dot4.Fill = _vm.CurrentStep >= 4
            ? (System.Windows.Media.Brush)accent
            : (System.Windows.Media.Brush)inactive;

        StepCounter.Text = $"Schritt {_vm.CurrentStep} von {_vm.TotalSteps}";
    }
}
