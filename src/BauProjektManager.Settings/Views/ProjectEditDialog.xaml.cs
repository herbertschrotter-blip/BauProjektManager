using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace BauProjektManager.Settings.Views;

/// <summary>
/// Projekt-Dialog (5 Tabs). BPM-070: Code-Behind nach Tabs in Partial Classes geteilt —
/// Stammdaten, Bauwerk (+BauwerkDialoge), Beteiligte, PortaleLinks, Ordnerstruktur, Helpers.
/// Diese Datei: Zustand, Konstruktoren, Speichern/Abbrechen.
/// </summary>
public partial class ProjectEditDialog : Window
{
    public Project Project { get; private set; }
    public List<FolderTemplateEntry>? FolderTemplate { get; private set; }

    private readonly bool _isNewProject;
    private readonly AppSettingsService _settingsService;
    private ObservableCollection<BuildingPart> _buildingParts = [];
    private ObservableCollection<ProjectParticipant> _participants = [];
    private ObservableCollection<ProjectLink> _portalLinks = [];
    private ObservableCollection<ProjectLink> _customLinks = [];
    private FileSystemWatcher? _folderWatcher;
    private bool _isGlobalZeroActive;

    // BPM-112.05: FS-Ports fuer die Zugriffe (Beteiligte-Import, Ordner-Watcher).
    private static readonly Infrastructure.Services.LocalFileSystem _fs = new();

    public ProjectEditDialog(Project project, AppSettingsService settingsService)
        : this(project, null, settingsService) { }

    public ProjectEditDialog(Project project, List<FolderTemplateEntry>? folderTemplate, AppSettingsService settingsService)
    {
        InitializeComponent();
        Project = project;
        _settingsService = settingsService;
        _isNewProject = folderTemplate is not null;
        RestoreDialogLayout();

        ProjectFolderTemplate.IsProjectMode = !_isNewProject;

        if (_isNewProject && folderTemplate is not null)
        {
            TxtDialogTitle.Text = "Neues Projekt anlegen";
            ProjectFolderTemplate.LoadFromTemplate(folderTemplate);
        }
        else if (!string.IsNullOrEmpty(project.Paths.Root))
        {
            TxtDialogTitle.Text = "Projekt bearbeiten";
            ProjectFolderTemplate.LoadFromDisk(project.Paths.Root);
            StartFolderWatcher(project.Paths.Root);
        }
        else
        {
            TxtDialogTitle.Text = "Projekt bearbeiten";
            ProjectFolderTemplate.LoadFromTemplate(_settingsService.LoadSharedOrDefault().FolderTemplate);
        }

        ProjectFolderTemplate.PreviewRootName = $"{project.ProjectNumber}_{project.Name}";

        // Globales Nullniveau laden
        _isGlobalZeroActive = project.UseGlobalZeroLevel;
        if (_isGlobalZeroActive)
        {
            TxtGlobalZero.Text = project.GlobalZeroLevel.ToString("F2");
            TxtGlobalZero.Visibility = Visibility.Visible;
            TxtGlobalZeroHint.Visibility = Visibility.Visible;
            UpdateToggleVisual();
        }

        _buildingParts = new ObservableCollection<BuildingPart>(project.BuildingParts);
        DgParts.ItemsSource = _buildingParts;

        // Tab 3: Load participants
        _participants = new ObservableCollection<ProjectParticipant>(project.Participants);
        DgParticipants.ItemsSource = _participants;

        // Tab 4: Load links (split into portals and custom)
        _portalLinks = new ObservableCollection<ProjectLink>(project.Links.Where(l => l.LinkType == "Portal"));
        _customLinks = new ObservableCollection<ProjectLink>(project.Links.Where(l => l.LinkType != "Portal"));
        DgPortals.ItemsSource = _portalLinks;
        DgCustomLinks.ItemsSource = _customLinks;
        RefreshLinkPreview();

        var settings = _settingsService.LoadSharedOrDefault();
        ColLevelName.ItemsSource = settings.LevelNames.Select(l => l.ShortName).ToList();

        LoadDropdowns();
        LoadProjectData();
    }

    // ═══════════════════════════════════════════
    // SAVE / CANCEL
    // ═══════════════════════════════════════════

    private void OnSave(object sender, RoutedEventArgs e)
    {
        Project.Name = TxtName.Text;
        Project.FullName = TxtFullName.Text;
        Project.Timeline.ProjectStart = DpProjectStart.SelectedDate;
        Project.Status = (ProjectStatus)CmbStatus.SelectedItem;
        Project.ProjectType = CmbProjectType.SelectedItem as string ?? "";
        Project.UpdateProjectNumberFromStart();
        Project.Client.Company = TxtClientCompany.Text;
        Project.Client.ContactPerson = TxtClientContact.Text;
        Project.Client.Phone = TxtClientPhone.Text;
        Project.Client.Email = TxtClientEmail.Text;
        Project.Location.Street = TxtStreet.Text;
        Project.Location.HouseNumber = TxtHouseNumber.Text;
        Project.Location.PostalCode = TxtPostalCode.Text;
        Project.Location.City = TxtCity.Text;
        Project.Location.Municipality = TxtMunicipality.Text;
        Project.Location.District = TxtDistrict.Text;
        Project.Location.State = TxtState.Text;
        Project.Location.CoordinateSystem = TxtCoordSystem.Text;
        if (double.TryParse(TxtCoordEast.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var east)) Project.Location.CoordinateEast = east;
        if (double.TryParse(TxtCoordNorth.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var north)) Project.Location.CoordinateNorth = north;
        Project.Location.CadastralKg = TxtCadastralKg.Text;
        Project.Location.CadastralKgName = TxtCadastralKgName.Text;
        Project.Location.CadastralGst = TxtCadastralGst.Text;
        Project.Timeline.ConstructionStart = DpConstructionStart.SelectedDate;
        Project.Timeline.PlannedEnd = DpPlannedEnd.SelectedDate;
        Project.Timeline.ActualEnd = DpActualEnd.SelectedDate;
        Project.Tags = TxtTags.Text;
        Project.Notes = TxtNotes.Text;
        Project.BuildingParts = _buildingParts.ToList();
        Project.Participants = _participants.ToList();
        Project.Links = _portalLinks.Concat(_customLinks).ToList();

        Project.UseGlobalZeroLevel = _isGlobalZeroActive;
        if (Project.UseGlobalZeroLevel && double.TryParse(TxtGlobalZero.Text.Replace(',', '.'),
            CultureInfo.InvariantCulture, out var gz))
            Project.GlobalZeroLevel = gz;

        FolderTemplate = ProjectFolderTemplate.ToTemplate();

        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    // ── Dialoggroesse + Baum/Vorschau-Verhaeltnis geraetelokal (BPM-073) ──

    private void RestoreDialogLayout()
    {
        var ui = _settingsService.LoadDevice().UiLayout;
        if (ui.ProjectEditDialogWidth is { } w && w >= MinWidth) Width = w;
        if (ui.ProjectEditDialogHeight is { } h && h >= MinHeight) Height = h;
        ProjectFolderTemplate.TreeSplitRatio = ui.FolderTemplateTreeRatio ?? 0.6;
        ProjectFolderTemplate.SplitChanged += SaveDialogLayout;
        Closing += (_, _) => SaveDialogLayout();
    }

    private void SaveDialogLayout()
    {
        try
        {
            var device = _settingsService.LoadDevice();
            var ui = device.UiLayout;
            if (WindowState == WindowState.Normal && ActualWidth > 0)
            {
                ui.ProjectEditDialogWidth = ActualWidth;
                ui.ProjectEditDialogHeight = ActualHeight;
            }
            ui.FolderTemplateTreeRatio = ProjectFolderTemplate.TreeSplitRatio;
            _settingsService.SaveDevice(device);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Dialog-Layout konnte nicht gespeichert werden");
        }
    }
}
