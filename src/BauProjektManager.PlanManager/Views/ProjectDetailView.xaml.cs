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
    private readonly Infrastructure.Persistence.ProjectDatabase? _bpmDb;
    private readonly IPdfRenderService? _pdfRenderService;
    private readonly IFileLauncher? _fileLauncher;
    private readonly Infrastructure.Persistence.AppSettingsService? _settingsService;
    private readonly IPdfTextService? _pdfTextService;
    private PlanManagerDatabase? _manualSortDb;
    private bool _manualSortInitialized;

    public ProjectDetailView(
        Project project, BoolToVisConverter boolToVis, IProfileManager profileManager,
        IIdGenerator idGenerator,
        PatternTemplateService? templateService = null, string? appDataPath = null,
        IPersistenceRegistry? persistenceRegistry = null,
        ISegmentTypeCatalog? segmentTypeCatalog = null,
        ISegmentTypeRepository? segmentTypeRepository = null,
        Infrastructure.Persistence.ProjectDatabase? bpmDb = null,
        IPdfRenderService? pdfRenderService = null,
        IFileLauncher? fileLauncher = null,
        Infrastructure.Persistence.AppSettingsService? settingsService = null,
        IPdfTextService? pdfTextService = null)
    {
        _profileManager = profileManager;
        _idGenerator = idGenerator;
        _templateService = templateService;
        _appDataPath = appDataPath;
        _persistenceRegistry = persistenceRegistry;
        _segmentTypeCatalog = segmentTypeCatalog;
        _segmentTypeRepository = segmentTypeRepository;
        _bpmDb = bpmDb;
        _pdfRenderService = pdfRenderService;
        _fileLauncher = fileLauncher;
        _settingsService = settingsService;
        _pdfTextService = pdfTextService;
        Resources.Add("BoolToVis", boolToVis);
        InitializeComponent();

        var vm = new ProjectDetailViewModel(project);
        DataContext = vm;

        // BPM-112.06a: Explorer ist Start-Tab — Live-FS-Baum laedt sofort
        // (nur Top-Level, lazy); die Eingang-Analyse bleibt beim
        // ManuellSortieren-Tab (lazy via OnTabChanged).
        ExplorerHost.Initialize(project.Paths.Root, _fileLauncher, project.Paths.Inbox);

        // PlanManagerDatabase des ManuellSortieren-Tabs beim Verlassen schliessen
        Unloaded += (_, _) => { _manualSortDb?.Dispose(); _manualSortDb = null; };
    }

    /// <summary>
    /// BPM-111.05 Slice 2c: ManuellSortieren-Tab lazy initialisieren —
    /// die Eingang-Analyse laeuft erst, wenn der Tab wirklich geoeffnet wird.
    /// </summary>
    private void OnTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, DetailTabs))
            return; // SelectionChanged von inneren Listen (ListBox) ignorieren
        if (_manualSortInitialized || !ReferenceEquals(DetailTabs.SelectedItem, ManualSortTab))
            return;

        var project = ViewModel.Project;
        if (_bpmDb is null || string.IsNullOrWhiteSpace(project.Paths.Root))
            return; // Platzhalter-Hinweis im Tab bleibt stehen

        _manualSortInitialized = true;
        _manualSortDb = new PlanManagerDatabase(project.Id, _idGenerator, _persistenceRegistry);

        // BPM-120 H0: Der Recovery-Einstieg (BPM-016) haengt seit dem Alt-Import-Cutover
        // an der Radial-Strecke — pending Imports (App-Crash, via Cloud gesyncter
        // Fremd-Stand) werden beim Oeffnen des Tabs behandelt. Bei "Spaeter" laedt
        // der Tab trotzdem; das Bestaetigen bleibt durch den PreImportCheck blockiert.
        HandleRecoveryIfPending(_manualSortDb, project.Paths.Root);
        ViewModel.RefreshInboxCommand.Execute(null);

        var captureVm = new ManualCaptureViewModel(_manualSortDb, _bpmDb, _idGenerator);
        captureVm.RecoveryRequested += (_, _) => OnManualSortRecoveryRequested(captureVm, project);
        captureVm.InboxChanged += (_, _) =>
        {
            ViewModel.RefreshInboxCommand.Execute(null);
            // Eingang-Zaehler im Explorer-Baum mitziehen (112.06a)
            ExplorerHost.ViewModel?.RefreshCommand.Execute(null);
        };
        ManualSortHost.Content = new ManualCaptureView
        {
            DataContext = captureVm,
            PdfRenderService = _pdfRenderService,
            FileLauncher = _fileLauncher,
            SettingsService = _settingsService,
            PdfTextService = _pdfTextService,
            SegmentTypeCatalog = _segmentTypeCatalog
        };

        _ = InitializeManualSortAsync(captureVm, project);
    }

    private static async Task InitializeManualSortAsync(ManualCaptureViewModel vm, Project project)
    {
        try
        {
            await vm.InitializeAsync(
                project.Id, project.Paths.Root, project.Paths.Inbox, project.Paths.Plans);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ManuellSortieren-Initialisierung fehlgeschlagen");
            vm.StatusText = $"Fehler: {ex.Message}";
        }
    }

    private void OnNewProfile(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new ProfileWizardDialog(
            ViewModel.Project, _profileManager, _templateService, _appDataPath,
            _segmentTypeCatalog, _segmentTypeRepository, _idGenerator, _bpmDb);
        dialog.Owner = System.Windows.Window.GetWindow(this);
        dialog.ShowDialog();
    }

    /// <summary>
    /// BPM-120 H0: Fallback-Recovery beim blockierten Bestaetigen — deckt pending
    /// Imports ab, die erst NACH dem Tab-Oeffnen auftauchen (z.B. via Cloud-Sync).
    /// Nach erfolgreicher Behandlung wird die Eingangs-Tabelle neu geladen;
    /// das Bestaetigen loest der User danach erneut aus.
    /// </summary>
    private void OnManualSortRecoveryRequested(ManualCaptureViewModel vm, Project project)
    {
        if (_manualSortDb is null)
            return;
        if (!HandleRecoveryIfPending(_manualSortDb, project.Paths.Root))
            return; // "Spaeter" — Bestaetigen bleibt blockiert

        ViewModel.RefreshInboxCommand.Execute(null);
        _ = ReloadManualSortAsync(vm);
    }

    private static async Task ReloadManualSortAsync(ManualCaptureViewModel vm)
    {
        try
        {
            await vm.RefreshCommand.ExecuteAsync(null);
            vm.StatusText = "Wiederherstellung abgeschlossen — bitte erneut bestätigen.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ManuellSortieren-Aktualisierung nach Recovery fehlgeschlagen");
            vm.StatusText = $"Fehler: {ex.Message}";
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
        var fs = new Infrastructure.Services.LocalFileSystem();
        // T5: Recovery Forward laeuft ueber denselben Executor wie der Import.
        var executor = new RecoveryExecutorService(db, fs, fs, fs,
            new ImportExecutionService(db, _idGenerator, fs, fs, fs));

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
