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

            // Build summary
            var sb = new StringBuilder();
            sb.AppendLine($"Import-Analyse: {result.TotalFiles} Dateien\n");

            if (result.NewCount > 0)
                sb.AppendLine($"✅ {result.NewCount} Neu");
            if (result.UpdateCount > 0)
                sb.AppendLine($"🔄 {result.UpdateCount} Update (neuer Index)");
            if (result.SkipCount > 0)
                sb.AppendLine($"⏭️ {result.SkipCount} Identisch (übersprungen)");
            if (result.WarningCount > 0)
                sb.AppendLine($"⚠️ {result.WarningCount} Warnungen");
            if (result.UnknownCount > 0)
                sb.AppendLine($"❓ {result.UnknownCount} Unbekannt (kein Profil)");
            if (result.ConflictCount > 0)
                sb.AppendLine($"🔀 {result.ConflictCount} Konflikte");

            sb.AppendLine($"\nProfile geladen: {result.UsedProfiles.Count}");

            // Show details for first 10 decisions
            if (result.Decisions.Count > 0)
            {
                sb.AppendLine("\n--- Details (max. 10) ---");
                foreach (var d in result.Decisions.Take(10))
                {
                    var icon = d.Status switch
                    {
                        ImportStatus.New => "✅",
                        ImportStatus.SkipIdentical => "⏭️",
                        ImportStatus.UpdateNewerIndex => "🔄",
                        ImportStatus.Unknown => "❓",
                        ImportStatus.Conflict => "🔀",
                        ImportStatus.ChangedSameIndex => "⚠️",
                        ImportStatus.OlderRevision => "⚠️",
                        _ => "•"
                    };
                    sb.AppendLine($"{icon} {d.File.Parsed.FileName}");
                    if (d.DocumentKey is not null)
                        sb.AppendLine($"   Key: {d.DocumentKey}");
                    if (d.TargetRelativePath is not null)
                        sb.AppendLine($"   Ziel: {d.TargetRelativePath}");
                }
            }

            MessageBox.Show(sb.ToString(), "Import-Analyse",
                MessageBoxButton.OK, MessageBoxImage.Information);
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
