using System.Text;
using System.Windows;
using System.Windows.Controls;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;
using Serilog;

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

    private async void OnStartImport(object sender, RoutedEventArgs e)
    {
        var project = ViewModel.Project;
        if (string.IsNullOrWhiteSpace(project.Paths.Root))
        {
            MessageBox.Show("Projektpfad nicht gesetzt.", "Import",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var workflow = new ImportWorkflowService(_profileManager);
            var result = await workflow.AnalyzeAsync(
                project.Paths.Root,
                project.Paths.Inbox,
                project.Paths.Plans);

            if (result.TotalFiles == 0)
            {
                MessageBox.Show("Keine Dateien im Eingang.", "Import",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Show preview dialog
            var vm = new ImportPreviewViewModel(result);
            var dialog = new ImportPreviewDialog(vm);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();

            // TODO Phase H: if (dialog.ExecuteRequested) → Import ausführen
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Import-Analyse fehlgeschlagen");
            MessageBox.Show($"Fehler bei Import-Analyse:\n{ex.Message}",
                "Import", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Zugriff auf das ViewModel für Event-Verdrahtung.
    /// </summary>
    public ProjectDetailViewModel ViewModel
        => (ProjectDetailViewModel)DataContext;
}
