using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// Count > 0 → Visible, Count == 0 → Collapsed.
/// Inverse des CountToVisConverter in PlanManagerView.
/// </summary>
public class CountToVisInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int count && count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public partial class ProfileWizardDialog : Window
{
    private readonly ProfileWizardViewModel _vm;

    public ProfileWizardDialog()
    {
        Resources.Add("CountToVisInverse", new CountToVisInverseConverter());
        InitializeComponent();

        _vm = new ProfileWizardViewModel();
        DataContext = _vm;
    }

    private void OnFileNameKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _vm.ParseFileNameCommand.Execute(null);
    }

    private void OnFieldTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo
            && combo.DataContext is FileNameSegment segment
            && combo.SelectedItem is FieldTypeOption option)
        {
            _vm.OnFieldTypeChanged(segment, option);
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnNext(object sender, RoutedEventArgs e)
    {
        // Schritt 2–4 kommen später.
        // Für jetzt: Dialog schließen mit Erfolg.
        DialogResult = true;
        Close();
    }
}
