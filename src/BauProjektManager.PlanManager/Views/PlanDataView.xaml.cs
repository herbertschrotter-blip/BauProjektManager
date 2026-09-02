using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;
using Serilog;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// Plandaten-Tab (BPM-126 Slice a+b+c): tabellarische DB-Sicht auf den kuratierten
/// Planindex, Detail-Panel (Dokument / Ablage+Dateien / Revisionen) und der
/// wiederverwendbare Segment-Editor (BPM-126 komplett).
/// </summary>
public partial class PlanDataView : UserControl
{
    // BPM-112.05 (ADR-060 Slice 5): FS-Port statt direktem System.IO in der View.
    private static readonly Infrastructure.Services.LocalFileSystem _fs = new();

    private PlanManagerDatabase? _planDb;
    private ISegmentTypeCatalog? _catalog;
    private ISegmentTypeRepository? _segmentTypeRepository;
    private IIdGenerator? _idGenerator;
    private IFileLauncher? _launcher;
    private AppSettingsService? _settingsService;
    private string _projectRootPath = "";

    public PlanDataView()
    {
        Resources.Add("BoolToVis", new BoolToVisConverter());
        Resources.Add("InverseBoolToVis", new InverseBoolToVisConverter());
        InitializeComponent();
        SegmentEditor.AssignmentChanged += OnSegmentAssignmentChanged;
        SegmentEditor.ManageTypesRequested += OnManageTypesRequested;
    }

    public PlanDataViewModel? ViewModel => DataContext as PlanDataViewModel;

    /// <summary>Vom Host (ProjectDetailView) aufgerufen, sobald die planmanager.db steht.</summary>
    public void Initialize(
        PlanManagerDatabase planDb, string projectId, ProjectDatabase? bpmDb,
        string projectRootPath = "", IFileLauncher? fileLauncher = null,
        ISegmentTypeCatalog? catalog = null, ISegmentTypeRepository? segmentTypeRepository = null,
        IIdGenerator? idGenerator = null, AppSettingsService? settingsService = null)
    {
        _planDb = planDb;
        _projectRootPath = projectRootPath;
        _launcher = fileLauncher;
        _catalog = catalog;
        _segmentTypeRepository = segmentTypeRepository;
        _idGenerator = idGenerator;
        _settingsService = settingsService;

        var vm = new PlanDataViewModel(planDb, projectId, bpmDb);
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PlanDataViewModel.SelectedRow))
                { LoadSegmentEditor(); ApplyDetailHeight(); }
        };
        DataContext = vm;
        vm.Load();
    }

    // ── Panel-Hoehe (BPM-126b, geraete-lokal wie die Vorschau-Breiten) ──

    private const double DetailDefaultHeight = 250;
    private const double DetailMinHeight = 120;
    private const double DetailMaxHeight = 600;

    /// <summary>Gemerkte Hoehe anwenden; ohne Auswahl klappt die Zeile zu (Auto = 0).</summary>
    private void ApplyDetailHeight()
    {
        var hasSelection = ViewModel?.SelectedRow is not null;
        DetailRow.Height = hasSelection
            ? new GridLength(StoredDetailHeight())
            : GridLength.Auto;
    }

    private double StoredDetailHeight()
        => Math.Clamp(
            _settingsService?.LoadDevice().UiLayout.PlanDataDetailHeight ?? DetailDefaultHeight,
            DetailMinHeight, DetailMaxHeight);

    /// <summary>Neue Hoehe nach dem Ziehen geraete-lokal merken.</summary>
    private void OnDetailSplitterDragCompleted(
        object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
    {
        if (_settingsService is null)
            return;
        var device = _settingsService.LoadDevice();
        device.UiLayout.PlanDataDetailHeight =
            Math.Clamp(DetailRow.ActualHeight, DetailMinHeight, DetailMaxHeight);
        _settingsService.SaveDevice(device);
    }

    // ── Segment-Editor (BPM-126c) ───────────────────────────────────

    /// <summary>Editor auf die Primaerdatei der gewaehlten Zeile setzen.</summary>
    private void LoadSegmentEditor()
    {
        if (ViewModel is not { SelectedRow: { } row } vm || _planDb is null)
        {
            SegmentEditor.Load("", _catalog, null);
            return;
        }

        var file = vm.DetailFiles.FirstOrDefault(f => f.IsPrimary) ?? vm.DetailFiles.FirstOrDefault();
        var existing = _planDb.GetSegmentsForDocument(row.Row.DocumentId);
        SegmentEditor.Load(file?.FileName ?? "", _catalog, existing);
    }

    /// <summary>
    /// Zuweisung speichern (UpsertSegment — reiner DB-Write, kein Journal;
    /// BPM-118-Metadaten liegen bewusst ausserhalb der Import-Transaktion, ADR-064).
    /// </summary>
    private void OnSegmentAssignmentChanged(object? sender, SegmentAssignmentChangedEventArgs e)
    {
        if (ViewModel is not { SelectedRow: { } row } vm || _planDb is null || e.NewType is null)
            return;
        try
        {
            _planDb.UpsertSegment(row.Row.DocumentId, e.NewType.Id,
                e.NewType.TokenKey, e.RawValue, e.RawValue.ToLowerInvariant());
            vm.StatusText = $"Segment '{e.NewType.Name}' = '{e.RawValue}' gespeichert.";
            // Bewusst KEIN vm.Load(): das wuerde die Liste neu aufbauen, das DataGrid
            // seine Auswahl verwerfen und das Detail-Panel zuklappen. Es aendert sich
            // ohnehin nur die Segment-Anzahl der aktuellen Zeile.
            row.SegmentCount = _planDb.GetSegmentsForDocument(row.Row.DocumentId).Count;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Plandaten: Segment konnte nicht gespeichert werden");
            vm.StatusText = "Segment konnte nicht gespeichert werden.";
        }
    }

    /// <summary>
    /// Neu laden und die Zeilenauswahl behalten. Das DataGrid verwirft seine
    /// Selektion beim Leeren der Liste erst im naechsten UI-Durchlauf — die
    /// Wiederherstellung muss deshalb dahinter laufen, sonst klappt das
    /// Detail-Panel nach jeder Aktion zu.
    /// </summary>
    public void ReloadKeepingSelection(PlanDataViewModel? viewModel = null)
    {
        if ((viewModel ?? ViewModel) is not { } vm)
            return;
        vm.Load();
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            vm.RestoreSelection();
            LoadSegmentEditor();
            ApplyDetailHeight();
        });
    }

    /// <summary>Oeffnet den BESTEHENDEN Segmenttyp-Manager (BPM-108) — nichts Neues.</summary>
    private void OnManageTypesRequested(object? sender, EventArgs e)
    {
        if (_catalog is null || _segmentTypeRepository is null || _idGenerator is null)
            return;
        var dialog = new SegmentTypeManagerDialog(_segmentTypeRepository, _catalog, _idGenerator)
        {
            Owner = Window.GetWindow(this)
        };
        dialog.ShowDialog();
        LoadSegmentEditor(); // Palette nach moeglichen Aenderungen neu aufbauen
    }

    // ── Tags (BPM-127) ──────────────────────────────────────────────

    private void OnAddTagClick(object sender, RoutedEventArgs e) => CommitTagInput();

    /// <summary>Enter im Eingabefeld setzt den Tag (schnelles Erfassen mehrerer Tags).</summary>
    private void OnTagInputKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter)
            return;
        CommitTagInput();
        e.Handled = true;
    }

    private void CommitTagInput()
    {
        var text = TagInput.Text;
        if (ViewModel is not { } vm || string.IsNullOrWhiteSpace(text))
            return;
        vm.AddTag(text);
        TagInput.Text = "";
    }

    /// <summary>Klick auf einen Vorschlag setzt den Tag direkt.</summary>
    private void OnSuggestionClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: string tag } && ViewModel is { } vm)
            vm.AddTag(tag);
    }

    private void OnRemoveTagClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: string tag } && ViewModel is { } vm)
            vm.RemoveTag(tag);
    }

    // ── Aktionen des Detail-Panels ──────────────────────────────────

    /// <summary>Primaerdatei der gewaehlten Revision in der Standard-App oeffnen.</summary>
    private void OnPreviewClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm || _launcher is null)
            return;
        var file = vm.DetailFiles.FirstOrDefault(f => f.IsPrimary) ?? vm.DetailFiles.FirstOrDefault();
        if (file is null)
            return;
        if (!_launcher.OpenFile(_fs.Combine(_projectRootPath, file.RelativePath)))
            vm.StatusText = "Datei konnte nicht geöffnet werden.";
    }

    /// <summary>Ablageordner des gewaehlten Dokuments im Windows-Explorer zeigen.</summary>
    private void OnRevealClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { SelectedRow: not null } vm || _launcher is null)
            return;
        var file = vm.DetailFiles.FirstOrDefault(f => f.IsPrimary) ?? vm.DetailFiles.FirstOrDefault();
        var ok = file is not null
            ? _launcher.RevealInExplorer(_fs.Combine(_projectRootPath, file.RelativePath))
            : _launcher.OpenFolder(_fs.Combine(_projectRootPath, vm.SelectedRow!.Row.RelativeDirectory));
        if (!ok)
            vm.StatusText = "Windows-Explorer konnte nicht geöffnet werden.";
    }
}
