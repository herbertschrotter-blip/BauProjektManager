using System.Windows.Controls;
using BauProjektManager.Domain.Models;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

public partial class ProjectDetailView : UserControl
{
    private readonly ProfileManager _profileManager;

    public ProjectDetailView(Project project, BoolToVisConverter boolToVis, ProfileManager profileManager)
    {
        _profileManager = profileManager;
        Resources.Add("BoolToVis", boolToVis);
        InitializeComponent();

        var vm = new ProjectDetailViewModel(project);
        DataContext = vm;
    }

    private void OnNewProfile(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new ProfileWizardDialog(ViewModel.Project, _profileManager);
        dialog.Owner = System.Windows.Window.GetWindow(this);
        dialog.ShowDialog();
    }

    /// <summary>
    /// Zugriff auf das ViewModel für Event-Verdrahtung.
    /// </summary>
    public ProjectDetailViewModel ViewModel
        => (ProjectDetailViewModel)DataContext;
}
