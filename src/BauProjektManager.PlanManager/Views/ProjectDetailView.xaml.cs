using System.Windows.Controls;
using BauProjektManager.Domain.Models;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

public partial class ProjectDetailView : UserControl
{
    public ProjectDetailView(Project project, BoolToVisConverter boolToVis)
    {
        Resources.Add("BoolToVis", boolToVis);
        InitializeComponent();

        var vm = new ProjectDetailViewModel(project);
        DataContext = vm;
    }

    /// <summary>
    /// Zugriff auf das ViewModel für Event-Verdrahtung.
    /// </summary>
    public ProjectDetailViewModel ViewModel
        => (ProjectDetailViewModel)DataContext;
}
