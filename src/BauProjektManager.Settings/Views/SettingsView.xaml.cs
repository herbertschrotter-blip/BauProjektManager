using System.Windows.Controls;
using System.Windows.Input;
using BauProjektManager.Settings.ViewModels;

namespace BauProjektManager.Settings.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && vm.SelectedProject is not null)
        {
            vm.EditProjectCommand.Execute(null);
        }
    }
}
