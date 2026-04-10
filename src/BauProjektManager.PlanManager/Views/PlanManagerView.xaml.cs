using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// BoolToVisibility — true=Visible, false=Collapsed.
/// </summary>
public class BoolToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Count==0 → Visible (Empty State), Count>0 → Collapsed.
/// </summary>
public class CountToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int count && count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public partial class PlanManagerView : UserControl
{
    private readonly PlanManagerViewModel _vm;

    public PlanManagerView()
    {
        Resources.Add("BoolToVis", new BoolToVisConverter());
        Resources.Add("CountToVis", new CountToVisConverter());
        InitializeComponent();

        _vm = new PlanManagerViewModel();
        DataContext = _vm;
    }

    private void OnProjectDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_vm.SelectedProject is not null)
        {
            _vm.OnProjectDoubleClicked(_vm.SelectedProject);
        }
    }
}
