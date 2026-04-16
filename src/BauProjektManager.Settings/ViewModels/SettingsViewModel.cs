using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Settings.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.Settings.ViewModels;

/// <summary>
/// ViewModel for the Settings page — manages project list, paths display,
/// search/filter, and global default folder template.
/// </summary>
public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly ProjectDatabase _db;
    private readonly RegistryJsonExporter _exporter;
    private readonly AppSettingsService _settingsService = new();
    private readonly ProjectFolderService _folderService;
    private readonly BpmManifestService _manifestService = new();
    private readonly IDialogService _dialogService;
    private AppSettings? _settings;

    [ObservableProperty]
    private ObservableCollection<Project> _projects = [];

    [ObservableProperty]
    private Project? _selectedProject;

    [ObservableProperty]
    private string _registryStatus = "";

    [ObservableProperty]
    private string _basePath = "";

    [ObservableProperty]
    private string _archivePath = "";

    [ObservableProperty]
    private string _cloudStoragePath = "";

    // === Suche + Filter ===

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _statusFilter = "Alle";

    [ObservableProperty]
    private string _filterInfo = "";

    private ICollectionView? _projectsView;

    /// <summary>
    /// Gefilterte Ansicht der Projektliste. DataGrid bindet hieran.
    /// </summary>
    public ICollectionView? ProjectsView => _projectsView;

    partial void OnSearchTextChanged(string value)
    {
        _projectsView?.Refresh();
        UpdateFilterInfo();
    }

    partial void OnStatusFilterChanged(string value)
    {
        _projectsView?.Refresh();
        UpdateFilterInfo();
    }

    private void SetupFilter()
    {
        _projectsView = CollectionViewSource.GetDefaultView(Projects);
        _projectsView.Filter = ProjectFilter;
        OnPropertyChanged(nameof(ProjectsView));
        UpdateFilterInfo();
    }

    private bool ProjectFilter(object obj)
    {
        if (obj is not Project project) return false;

        // Statusfilter
        if (StatusFilter == "Aktiv" && project.Status != ProjectStatus.Active) return false;
        if (StatusFilter == "Abgeschlossen" && project.Status != ProjectStatus.Completed) return false;

        // Textsuche
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var search = SearchText.Trim().ToLowerInvariant();
        return MatchesSearch(project, search);
    }

    private static bool MatchesSearch(Project project, string search)
    {
        if (project.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
        if (project.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
        if (project.ProjectNumber.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
        if (project.Tags.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
        if (project.Notes.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
        if (project.ProjectType.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;

        // Auftraggeber
        if (project.Client.Company.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
        if (project.Client.ContactPerson.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;

        // Adresse
        if (project.Location.City.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
        if (project.Location.Street.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
        if (project.Location.Municipality.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }

    private void UpdateFilterInfo()
    {
        if (_projectsView is null) return;

        var visibleCount = _projectsView.Cast<object>().Count();
        var totalCount = Projects.Count;

        if (string.IsNullOrWhiteSpace(SearchText) && StatusFilter == "Alle")
        {
            FilterInfo = $"{totalCount} Projekte geladen";
        }
        else
        {
            FilterInfo = $"{visibleCount} von {totalCount} Projekten";
        }
    }

    // Folder template tree
    [ObservableProperty]
    private ObservableCollection<FolderTreeItem> _folderTreeItems = [];

    // Selected item in TreeView (set from code-behind)
    public FolderTreeItem? SelectedTreeItem { get; set; }

    public SettingsViewModel(ProjectDatabase db, IDialogService dialogService)
    {
        _db = db;
        _dialogService = dialogService;

        // Registry-Exportpfad aus Settings (BasePath/.AppData/BauProjektManager/)
        var settings = _settingsService.Load();
        var exportDir = !string.IsNullOrEmpty(settings.ExportPath)
            ? settings.ExportPath
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BauProjektManager");
        var registryPath = Path.Combine(exportDir, "registry.json");

        _exporter = new RegistryJsonExporter(registryPath);
        _folderService = new ProjectFolderService(_settingsService);

        Log.Information("Registry export path: {Path}", registryPath);

        LoadProjects();
        LoadSettings();
    }

    private void LoadProjects()
    {
        try
        {
            var loaded = _db.LoadAllProjects();
            Projects = new ObservableCollection<Project>(loaded);
            SetupFilter();
            Log.Information("Loaded {Count} projects from database", loaded.Count);
            RegistryStatus = $"{loaded.Count} Projekte geladen";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load projects from database");
            Projects = [];
            RegistryStatus = "Fehler beim Laden!";
        }
    }

    private void LoadSettings()
    {
        Log.Debug("Settings tab loaded: {Tab}", "General");
        _settings = _settingsService.Load();
        BasePath = _settings.BasePath;
        ArchivePath = _settings.ArchivePath;
        CloudStoragePath = _settings.OneDrivePath;
        BuildFolderTree(_settings.FolderTemplate);
    }

    // === Folder Template Tree ===

    private void BuildFolderTree(List<FolderTemplateEntry> template)
    {
        var items = new ObservableCollection<FolderTreeItem>();

        for (int i = 0; i < template.Count; i++)
        {
            var entry = template[i];
            var mainItem = new FolderTreeItem
            {
                Name = entry.Name,
                Position = i,
                IsMainFolder = true,
                HasInbox = entry.HasInbox,
                HasPrefix = true
            };

            int subPos = 0;
            foreach (var sub in entry.SubFolders)
            {
                mainItem.Children.Add(new FolderTreeItem
                {
                    Name = sub.Name,
                    Position = sub.HasPrefix ? subPos : -1,
                    IsMainFolder = false,
                    HasPrefix = sub.HasPrefix
                });
                if (sub.HasPrefix) subPos++;
            }

            items.Add(mainItem);
        }

        FolderTreeItems = items;
    }

    private void SaveFolderTemplate()
    {
        Log.Debug("Saving settings");
        var settings = _settingsService.Load();
        settings.FolderTemplate = FolderTreeItems.Select(main => new FolderTemplateEntry
        {
            Name = main.Name,
            HasInbox = main.HasInbox,
            SubFolders = main.Children.Select(sub => new SubFolderEntry
            {
                Name = sub.Name,
                HasPrefix = sub.HasPrefix
            }).ToList()
        }).ToList();
        _settingsService.Save(settings);
        Log.Information("Folder template saved ({Count} main folders)", settings.FolderTemplate.Count);
    }

    public List<FolderTemplateEntry> GetFolderTemplate()
    {
        return _settings?.FolderTemplate ?? new List<FolderTemplateEntry>();
    }

    public void SaveFolderTemplateFrom(List<FolderTemplateEntry> template)
    {
        if (_settings == null) return;
        _settings.FolderTemplate = template;
        _settingsService.Save(_settings);
        Log.Debug("Folder template saved from FolderTemplateControl");
    }

    private void RefreshTreePositions()
    {
        for (int i = 0; i < FolderTreeItems.Count; i++)
        {
            FolderTreeItems[i].Position = i;
            int subPos = 0;
            foreach (var sub in FolderTreeItems[i].Children)
            {
                sub.Position = sub.HasPrefix ? subPos : -1;
                if (sub.HasPrefix) subPos++;
            }
        }

        // Force UI refresh
        var items = FolderTreeItems.ToList();
        FolderTreeItems.Clear();
        foreach (var item in items)
            FolderTreeItems.Add(item);
    }

    private static string ShowInputDialog(string title, string label, Window? owner)
    {
        var inputWindow = new Window
        {
            Title = title,
            Width = 350,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner,
            ResizeMode = ResizeMode.NoResize,
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D30"))
        };

        var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(15) };
        var lbl = new System.Windows.Controls.TextBlock
        {
            Text = label,
            Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CCCCCC")),
            Margin = new Thickness(0, 0, 0, 5)
        };
        var textBox = new System.Windows.Controls.TextBox
        {
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E1E1E")),
            Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CCCCCC")),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3E3E42")),
            Padding = new Thickness(5, 3, 5, 3),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var btnOk = new System.Windows.Controls.Button
        {
            Content = "OK",
            Width = 80,
            Padding = new Thickness(0, 5, 0, 5),
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#007ACC")),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        btnOk.Click += (_, _) => { inputWindow.DialogResult = true; inputWindow.Close(); };

        stack.Children.Add(lbl);
        stack.Children.Add(textBox);
        stack.Children.Add(btnOk);
        inputWindow.Content = stack;
        inputWindow.ContentRendered += (_, _) => textBox.Focus();

        return inputWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(textBox.Text)
            ? textBox.Text.Trim()
            : "";
    }

    // === Filter Commands ===

    [RelayCommand]
    private void SetFilterAll() => StatusFilter = "Alle";

    [RelayCommand]
    private void SetFilterActive() => StatusFilter = "Aktiv";

    [RelayCommand]
    private void SetFilterCompleted() => StatusFilter = "Abgeschlossen";

    [RelayCommand]
    private void ClearSearch() => SearchText = "";

    // === Template Commands ===

    [RelayCommand]
    private void TemplateMoveUp()
    {
        if (SelectedTreeItem is null || !SelectedTreeItem.IsMainFolder) return;
        var index = FolderTreeItems.IndexOf(SelectedTreeItem);
        if (index <= 0) return;
        var item = FolderTreeItems[index];
        FolderTreeItems.RemoveAt(index);
        FolderTreeItems.Insert(index - 1, item);
        RefreshTreePositions();
        SaveFolderTemplate();
    }

    [RelayCommand]
    private void TemplateMoveDown()
    {
        if (SelectedTreeItem is null || !SelectedTreeItem.IsMainFolder) return;
        var index = FolderTreeItems.IndexOf(SelectedTreeItem);
        if (index < 0 || index >= FolderTreeItems.Count - 1) return;
        var item = FolderTreeItems[index];
        FolderTreeItems.RemoveAt(index);
        FolderTreeItems.Insert(index + 1, item);
        RefreshTreePositions();
        SaveFolderTemplate();
    }

    [RelayCommand]
    private void TemplateAddMain()
    {
        var name = ShowInputDialog("Hauptordner hinzufügen", "Ordnername (ohne Nummer):",
            Application.Current.MainWindow);
        if (string.IsNullOrEmpty(name)) return;

        FolderTreeItems.Add(new FolderTreeItem
        {
            Name = name,
            Position = FolderTreeItems.Count,
            IsMainFolder = true,
            HasInbox = false
        });
        RefreshTreePositions();
        SaveFolderTemplate();
    }

    [RelayCommand]
    private void TemplateAddSub()
    {
        FolderTreeItem? parent = null;
        if (SelectedTreeItem is not null)
        {
            if (SelectedTreeItem.IsMainFolder)
                parent = SelectedTreeItem;
            else
            {
                foreach (var main in FolderTreeItems)
                {
                    if (main.Children.Contains(SelectedTreeItem))
                    {
                        parent = main;
                        break;
                    }
                }
            }
        }

        if (parent is null)
        {
            _dialogService.ShowInfo("Bitte zuerst einen Hauptordner auswählen.",
                "Unterordner hinzufügen");
            return;
        }

        var name = ShowInputDialog("Unterordner hinzufügen",
            $"Unterordner-Name für \"{parent.Name}\":",
            Application.Current.MainWindow);
        if (string.IsNullOrEmpty(name)) return;

        parent.Children.Add(new FolderTreeItem
        {
            Name = name,
            IsMainFolder = false,
            HasPrefix = true,
            Position = parent.Children.Count(c => c.HasPrefix)
        });
        RefreshTreePositions();
        SaveFolderTemplate();
    }

    [RelayCommand]
    private void TemplateRemove()
    {
        if (SelectedTreeItem is null) return;

        var displayName = SelectedTreeItem.IsMainFolder
            ? $"Hauptordner \"{SelectedTreeItem.Name}\""
            : $"Unterordner \"{SelectedTreeItem.Name}\"";

        if (!_dialogService.ShowConfirm($"{displayName} aus dem Template entfernen?", "Entfernen"))
            return;

        if (SelectedTreeItem.IsMainFolder)
        {
            FolderTreeItems.Remove(SelectedTreeItem);
        }
        else
        {
            foreach (var main in FolderTreeItems)
            {
                if (main.Children.Remove(SelectedTreeItem))
                    break;
            }
        }

        RefreshTreePositions();
        SaveFolderTemplate();
    }

    [RelayCommand]
    private void TemplateToggleInbox()
    {
        if (SelectedTreeItem is null || !SelectedTreeItem.IsMainFolder) return;
        SelectedTreeItem.HasInbox = !SelectedTreeItem.HasInbox;
        Log.Debug("Setting changed: {Key}", "FolderTemplate.HasInbox");
        RefreshTreePositions();
        SaveFolderTemplate();
    }

    [RelayCommand]
    private void TemplateTogglePrefix()
    {
        if (SelectedTreeItem is null || SelectedTreeItem.IsMainFolder)
        {
            _dialogService.ShowInfo("Präfix kann nur für Unterordner umgeschaltet werden.", "Präfix");
            return;
        }

        SelectedTreeItem.HasPrefix = !SelectedTreeItem.HasPrefix;
        Log.Debug("Setting changed: {Key}", "FolderTemplate.HasPrefix");
        RefreshTreePositions();
        SaveFolderTemplate();
    }

    // === Project CRUD ===

    private void ExportRegistry()
    {
        try
        {
            _exporter.Export(Projects.ToList());
            RegistryStatus = $"Registry exportiert ({Projects.Count} Projekte)";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to export registry.json");
            RegistryStatus = "Registry-Export fehlgeschlagen!";
        }
    }

    /// <summary>
    /// Aktualisiert den Filter nach CRUD-Operationen.
    /// </summary>
    private void RefreshFilter()
    {
        _projectsView?.Refresh();
        UpdateFilterInfo();
    }

    [RelayCommand]
    private void AddProject()
    {
        var settings = _settingsService.Load();

        var newProject = new Project
        {
            Id = "",
            Status = ProjectStatus.Active,
            Timeline = new ProjectTimeline
            {
                ProjectStart = DateTime.Today
            }
        };
        newProject.UpdateProjectNumberFromStart();

        var dialog = new ProjectEditDialog(newProject, settings.FolderTemplate);
        dialog.Owner = Application.Current.MainWindow;
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var projectRoot = _folderService.CreateProjectFolders(
                    dialog.Project, dialog.FolderTemplate);
                dialog.Project.Paths.Root = projectRoot;

                _db.SaveProject(dialog.Project);
                _manifestService.WriteManifest(dialog.Project, projectRoot);
                Projects.Add(dialog.Project);
                SelectedProject = dialog.Project;
                ExportRegistry();
                RefreshFilter();
                Log.Information("Project added: {Name} ({Number}) at {Path}",
                    dialog.Project.Name, dialog.Project.ProjectNumber, projectRoot);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save new project");
                _dialogService.ShowError($"Fehler beim Speichern: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void EditProject()
    {
        if (SelectedProject is null)
        {
            _dialogService.ShowInfo("Bitte zuerst ein Projekt in der Liste auswählen.",
                "Kein Projekt ausgewählt");
            return;
        }

        var dialog = new ProjectEditDialog(SelectedProject);
        dialog.Owner = Application.Current.MainWindow;
        if (dialog.ShowDialog() == true)
        {
            try
            {
                if (dialog.FolderTemplate != null && !string.IsNullOrEmpty(dialog.Project.Paths?.Root))
                {
                    _folderService.SyncNewFolders(dialog.Project, dialog.FolderTemplate);
                }

                _db.SaveProject(dialog.Project);
                if (!string.IsNullOrEmpty(dialog.Project.Paths?.Root))
                {
                    _manifestService.WriteManifest(dialog.Project, dialog.Project.Paths.Root);
                }
                int index = Projects.IndexOf(SelectedProject);
                if (index >= 0)
                {
                    Projects.RemoveAt(index);
                    Projects.Insert(index, dialog.Project);
                    SelectedProject = dialog.Project;
                }
                ExportRegistry();
                RefreshFilter();
                Log.Information("Project updated: {Name} ({Number})",
                    dialog.Project.Name, dialog.Project.ProjectNumber);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update project");
                _dialogService.ShowError($"Fehler beim Speichern: {ex.Message}");
            }
        }
    }

    public void UpdateBasePath(string path)
    {
        var settings = _settingsService.Load();
        settings.BasePath = path;
        _settingsService.Save(settings);
        BasePath = path;
        Log.Debug("Setting changed: {Key}", "BasePath");
        Log.Information("BasePath changed to {Path}", path);
    }

    public void UpdateArchivePath(string path)
    {
        var settings = _settingsService.Load();
        settings.ArchivePath = path;
        _settingsService.Save(settings);
        ArchivePath = path;
        Log.Debug("Setting changed: {Key}", "ArchivePath");
        Log.Information("ArchivePath changed to {Path}", path);
    }

    [RelayCommand]
    private void DeleteProject()
    {
        if (SelectedProject is null)
        {
            _dialogService.ShowInfo("Bitte zuerst ein Projekt in der Liste auswählen.",
                "Kein Projekt ausgewählt");
            return;
        }

        if (!_dialogService.ShowConfirm(
            $"Projekt \"{SelectedProject.Name}\" ({SelectedProject.ProjectNumber}) wirklich löschen?\n\n" +
            "Das Projekt wird aus der Datenbank entfernt.\n" +
            "Der Projektordner auf der Festplatte wird NICHT gelöscht.",
            "Projekt löschen"))
        {
            return;
        }

        try
        {
            var name = SelectedProject.Name;
            var number = SelectedProject.ProjectNumber;

            _db.DeleteProject(SelectedProject.Id);
            Projects.Remove(SelectedProject);
            SelectedProject = null;
            ExportRegistry();
            RefreshFilter();
            Log.Information("Project deleted: {Name} ({Number})", name, number);
            RegistryStatus = $"Projekt {name} gelöscht";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete project");
            _dialogService.ShowError($"Fehler beim Löschen: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenProjectFolder()
    {
        if (SelectedProject is null)
        {
            _dialogService.ShowInfo("Bitte zuerst ein Projekt in der Liste auswählen.",
                "Kein Projekt ausgewählt");
            return;
        }

        var path = SelectedProject.Paths?.Root;
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            _dialogService.ShowWarning("Der Projektordner existiert nicht oder ist nicht konfiguriert.",
                "Ordner nicht gefunden");
            return;
        }

        try
        {
            System.Diagnostics.Process.Start("explorer.exe", path);
            Log.Information("Opened project folder: {Path}", path);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open project folder");
            _dialogService.ShowError($"Ordner konnte nicht geöffnet werden: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ImportFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Projektordner auswählen"
        };

        if (dialog.ShowDialog() != true) return;

        var folderPath = dialog.FolderName;

        if (_manifestService.HasManifest(folderPath))
        {
            ImportFromManifest(folderPath);
        }
        else
        {
            ImportFromFolder(folderPath);
        }
    }

    private void ImportFromManifest(string folderPath)
    {
        if (_db.ProjectExistsByPath(folderPath))
        {
            _dialogService.ShowWarning("Dieses Projekt ist bereits in der Datenbank vorhanden.");
            return;
        }

        var manifest = _manifestService.ReadManifest(folderPath);
        if (manifest is null)
        {
            _dialogService.ShowError("Manifest konnte nicht gelesen werden.");
            return;
        }

        var project = _manifestService.ManifestToProject(manifest, folderPath);

        var confirmDialog = new ProjectEditDialog(project);
        confirmDialog.Owner = Application.Current.MainWindow;
        if (confirmDialog.ShowDialog() == true)
        {
            try
            {
                _db.SaveProject(confirmDialog.Project);
                _manifestService.WriteManifest(confirmDialog.Project, folderPath);
                Projects.Add(confirmDialog.Project);
                SelectedProject = confirmDialog.Project;
                ExportRegistry();
                RefreshFilter();
                Log.Information("Project imported from manifest: {Name} ({Number})",
                    confirmDialog.Project.Name, confirmDialog.Project.ProjectNumber);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to import project from manifest");
                _dialogService.ShowError($"Fehler beim Import: {ex.Message}");
            }
        }
    }

    private void ImportFromFolder(string folderPath)
    {
        if (_db.ProjectExistsByPath(folderPath))
        {
            _dialogService.ShowWarning("Dieses Projekt ist bereits in der Datenbank vorhanden.");
            return;
        }

        var project = _manifestService.ScanFolder(folderPath);

        var editDialog = new ProjectEditDialog(project);
        editDialog.Owner = Application.Current.MainWindow;
        if (editDialog.ShowDialog() == true)
        {
            try
            {
                _db.SaveProject(editDialog.Project);
                _manifestService.WriteManifest(editDialog.Project, folderPath);
                Projects.Add(editDialog.Project);
                SelectedProject = editDialog.Project;
                ExportRegistry();
                RefreshFilter();
                Log.Information("Project imported from folder: {Name} ({Number}) at {Path}",
                    editDialog.Project.Name, editDialog.Project.ProjectNumber, folderPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to import project from folder");
                _dialogService.ShowError($"Fehler beim Import: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}

/// <summary>
/// Standard-DialogService als Fallback wenn kein BpmDialogService übergeben wird.
/// Verwendet die Windows-MessageBox — wird durch BpmDialogService ersetzt wenn
/// über MainWindow instanziiert.
/// </summary>
internal class DefaultDialogService : IDialogService
{
    public void ShowInfo(string message, string title = "Hinweis") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowWarning(string message, string title = "Warnung") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public void ShowError(string message, string title = "Fehler") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public bool ShowConfirm(string message, string title = "Bestätigung") =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}