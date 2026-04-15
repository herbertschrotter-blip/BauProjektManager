using System.Windows.Controls;
using BauProjektManager.Domain.Models;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

public partial class ProjectDetailView : UserControl
{
    private readonly ProfileManager _profileManager;
    private readonly PatternTemplateService? _templateService;
    private readonly string? _appDataPath;

    public ProjectDetailView(
        Project project, BoolToVisConverter boolToVis, ProfileManager profileManager,
        PatternTemplateService? templateService = null, string? appDataPath = null)
    {
        _profileManager = profileManager;
        _templateService = templateService;
        _appDataPath = appDataPath;
        Resources.Add("BoolToVis", boolToVis);
        InitializeComponent();

        var vm = new ProjectDetailViewModel(project);
        DataContext = vm;
    }

    private void OnNewProfile(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new ProfileWizardDialog(
            ViewModel.Project, _profileManager, _templateService, _appDataPath);
        dialog.Owner = System.Windows.Window.GetWindow(this);
        dialog.ShowDialog();
    }

    /// <summary>
    /// Zugriff auf das ViewModel für Event-Verdrahtung.
    /// </summary>
    public ProjectDetailViewModel ViewModel
        => (ProjectDetailViewModel)DataContext;
}
