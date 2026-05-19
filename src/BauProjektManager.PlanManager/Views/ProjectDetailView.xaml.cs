using System.Windows;
using System.Windows.Controls;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;
using Serilog;

namespace BauProjektManager.PlanManager.Views;

public partial class ProjectDetailView : UserControl
{
    private readonly IProfileManager _profileManager;
    private readonly PatternTemplateService? _templateService;
    private readonly IIdGenerator _idGenerator;
    private readonly string? _appDataPath;
    private readonly IPersistenceRegistry? _persistenceRegistry;
    private readonly ISegmentTypeCatalog? _segmentTypeCatalog;
    private readonly ISegmentTypeRepository? _segmentTypeRepository;

    public ProjectDetailView(
        Project project, BoolToVisConverter boolToVis, IProfileManager profileManager,
        IIdGenerator idGenerator,
        PatternTemplateService? templateService = null, string? appDataPath = null,
        IPersistenceRegistry? persistenceRegistry = null,
        ISegmentTypeCatalog? segmentTypeCatalog = null,
        ISegmentTypeRepository? segmentTypeRepository = null)
    {
        _profileManager = profileManager;
        _idGenerator = idGenerator;
        _templateService = templateService;
        _appDataPath = appDataPath;
        _persistenceRegistry = persistenceRegistry;
        _segmentTypeCatalog = segmentTypeCatalog;
        _segmentTypeRepository = segmentTypeRepository;
        Resources.Add("BoolToVis", boolToVis);
        InitializeComponent();

        var vm = new ProjectDetailViewModel(project);
        DataContext = vm;
    }

    private void OnNewProfile(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new ProfileWizardDialog(
            ViewModel.Project, _profileManager, _templateService, _appDataPath,
            _segmentTypeCatalog, _segmentTypeRepository, _idGenerator);
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
            using var db = new PlanManagerDatabase(project.Id, _idGenerator, _persistenceRegistry);

            // BPM-016 / 016.04: Recovery-Check vor neuem Import.
            // Nicht abgeschlossene Vorgänge (App-Crash, Power-Off etc.) müssen erst
            // behandelt werden bevor ein neuer Import läuft — sonst kollidieren
            // pending Aktionen mit dem neuen Import-Plan.
            if (!HandleRecoveryIfPending(db, project.Paths.Root))
            {
                // User wählte "Später" — neuer Import wird nicht gestartet
                return;
            }

            var workflow = new ImportWorkflowService(_profileManager, db);
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

            // Phase H: Execute import if user confirmed
            if (dialog.ExecuteRequested)
            {
                var executor = new ImportExecutionService(db, _idGenerator);
                var execResult = executor.Execute(
                    result.Decisions, project.Paths.Root, project.Paths.Inbox);

                var msg = $"Import abgeschlossen:\n\n" +
                    $"✅ {execResult.Succeeded} sortiert\n" +
                    $"⏭️ {execResult.Skipped} identisch (entfernt)\n" +
                    $"❌ {execResult.Failed} fehlgeschlagen";

                if (execResult.Errors.Count > 0)
                    msg += "\n\nFehler:\n" + string.Join("\n", execResult.Errors.Take(5));

                MessageBox.Show(msg, "Import", MessageBoxButton.OK,
                    execResult.Failed > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

                // Refresh inbox count
                ViewModel.RefreshInboxCommand.Execute(null);
            }
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

    /// <summary>
    /// Recovery-Hook (BPM-016 / 016.04): prüft ob pending Imports existieren und
    /// zeigt pro Import den Recovery-Dialog. User wählt Forward/Rollback/Cleanup/Später.
    /// Returns true wenn alles abgehandelt ist (oder keine pending vorhanden) und
    /// der reguläre Import-Workflow weiterlaufen darf. Returns false wenn User
    /// "Später" gewählt hat — Caller sollte dann abbrechen.
    /// </summary>
    private bool HandleRecoveryIfPending(PlanManagerDatabase db, string projectRootPath)
    {
        if (!db.HasPendingImports())
            return true;

        var pending = db.GetPendingImports();
        var decisionService = new RecoveryDecisionService();
        var executor = new RecoveryExecutorService(db);

        Log.Information("Recovery: {Count} pending Imports gefunden", pending.Count);

        foreach (var info in pending)
        {
            var recommendation = decisionService.Recommend(info);
            var dialog = new RecoveryDialog(info, recommendation);
            dialog.Owner = Window.GetWindow(this);
            var ok = dialog.ShowDialog() == true;

            if (!ok || dialog.SelectedAction is null)
            {
                // User wählte "Später" — abbrechen, beim nächsten Import nochmal fragen
                Log.Information("Recovery uebersprungen ('Spaeter') fuer Import {Id}", info.Id);
                return false;
            }

            RecoveryResult result = dialog.SelectedAction.Value switch
            {
                RecoveryAction.Forward => executor.ExecuteForward(info.Id, projectRootPath),
                RecoveryAction.Rollback => executor.ExecuteRollback(info.Id, projectRootPath),
                RecoveryAction.Cleanup => executor.ExecuteCleanup(info.Id, "user choice"),
                _ => executor.ExecuteCleanup(info.Id, "fallback")
            };

            if (!result.IsSuccess)
            {
                var errMsg = $"Recovery {result.Action} mit Fehlern abgeschlossen:\n\n" +
                             string.Join("\n", result.Errors.Take(5));
                MessageBox.Show(errMsg, "Recovery", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                Log.Information("Recovery {Action} erfolgreich fuer Import {Id}", result.Action, info.Id);
            }
        }

        return true;
    }
}
