using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Settings.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.Settings.ViewModels;

/// <summary>
/// Tree item for displaying folder template in TreeView.
/// Represents either a main folder or a subfolder.
/// </summary>
public class FolderTreeItem
{
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public bool IsMainFolder { get; set; }
    public bool HasInbox { get; set; }
    public bool HasPrefix { get; set; } = true;
    public ObservableCollection<FolderTreeItem> Children { get; set; } = [];

    /// <summary>
    /// Display name with number prefix for main folders, optional prefix for subs.
    /// </summary>
    public string DisplayName => IsMainFolder
        ? $"{Position:D2} {Name}"
        : (HasPrefix ? $"{Position:D2} {Name}" : Name);

    public Visibility InboxVisibility => HasInbox ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Shows "Präfix: an/aus" label for subfolders only.
    /// </summary>
    public string PrefixLabel => IsMainFolder ? "" : (HasPrefix ? "Präfix: an" : "Präfix: aus");
}

/// <summary>
/// ViewModel for the Settings page — manages project list, paths display,
/// and global default folder template with subfolders and prefix toggle.
/// </summary>
public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly ProjectDatabase _db = new();
    private readonly RegistryJsonExporter _exporter;
    private readonly AppSettingsService _settingsService = new();
    private readonly ProjectFolderService _folderService;

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
    private string _oneDrivePath = "";

    // Folder template tree
    [ObservableProperty]
    private ObservableCollection<FolderTreeItem> _folderTreeItems = [];

    // Selected item in TreeView (set from code-behind)
    public FolderTreeItem? SelectedTreeItem { get; set; }

    public SettingsViewModel()
    {
        var appRoot = FindAppRoot();
        var exportDir = Path.Combine(appRoot, "Export");
        var registryPath = Path.Combine(exportDir, "registry.json");

        _exporter = new RegistryJsonExporter(registryPath);
        _folderService = new ProjectFolderService(_settingsService);

        Log.Information("App root: {Root}", appRoot);
        Log.Information("Registry export path: {Path}", registryPath);

        LoadProjects();
        LoadSettings();
    }

    private static string FindAppRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        var current = new DirectoryInfo(dir);
        while (current is not null)
        {
            if (current.GetFiles("*.slnx").Length > 0 ||
                current.GetFiles("*.sln").Length > 0)
                return current.FullName;
            current = current.Parent;
        }
        return dir;
    }

    private void LoadProjects()
    {
        try
        {
            var loaded = _db.LoadAllProjects();
            Projects = new ObservableCollection<Project>(loaded);
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
        var settings = _settingsService.Load();
        BasePath = settings.BasePath;
        ArchivePath = settings.ArchivePath;
        OneDrivePath = settings.OneDrivePath;
        BuildFolderTree(settings.FolderTemplate);
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
        // Find the parent main folder
        FolderTreeItem? parent = null;
        if (SelectedTreeItem is not null)
        {
            if (SelectedTreeItem.IsMainFolder)
                parent = SelectedTreeItem;
            else
            {
                // Selected item is a subfolder — find its parent
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
            MessageBox.Show("Bitte zuerst einen Hauptordner auswählen.",
                "Unterordner hinzufügen", MessageBoxButton.OK, MessageBoxImage.Information);
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

        if (MessageBox.Show($"{displayName} aus dem Template entfernen?",
                "Entfernen", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
        RefreshTreePositions();
        SaveFolderTemplate();
    }

    [RelayCommand]
    private void TemplateTogglePrefix()
    {
        if (SelectedTreeItem is null || SelectedTreeItem.IsMainFolder)
        {
            MessageBox.Show("Präfix kann nur für Unterordner umgeschaltet werden.",
                "Präfix", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SelectedTreeItem.HasPrefix = !SelectedTreeItem.HasPrefix;
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
                Projects.Add(dialog.Project);
                SelectedProject = dialog.Project;
                ExportRegistry();
                Log.Information("Project added: {Name} ({Number}) at {Path}",
                    dialog.Project.Name, dialog.Project.ProjectNumber, projectRoot);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save new project");
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void EditProject()
    {
        if (SelectedProject is null) return;

        var dialog = new ProjectEditDialog(SelectedProject);
        dialog.Owner = Application.Current.MainWindow;
        if (dialog.ShowDialog() == true)
        {
            try
            {
                _db.SaveProject(dialog.Project);
                int index = Projects.IndexOf(SelectedProject);
                if (index >= 0)
                {
                    Projects.RemoveAt(index);
                    Projects.Insert(index, dialog.Project);
                    SelectedProject = dialog.Project;
                }
                ExportRegistry();
                Log.Information("Project updated: {Name} ({Number})",
                    dialog.Project.Name, dialog.Project.ProjectNumber);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update project");
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public void UpdateBasePath(string path)
    {
        var settings = _settingsService.Load();
        settings.BasePath = path;
        _settingsService.Save(settings);
        BasePath = path;
        Log.Information("BasePath changed to {Path}", path);
    }

    public void UpdateArchivePath(string path)
    {
        var settings = _settingsService.Load();
        settings.ArchivePath = path;
        _settingsService.Save(settings);
        ArchivePath = path;
        Log.Information("ArchivePath changed to {Path}", path);
    }

    [RelayCommand]
    private void DeleteProject()
    {
        if (SelectedProject is null) return;

        var result = MessageBox.Show(
            $"Projekt \"{SelectedProject.Name}\" ({SelectedProject.ProjectNumber}) wirklich löschen?\n\n" +
            "Das Projekt wird aus der Datenbank entfernt.\n" +
            "Der Projektordner auf der Festplatte wird NICHT gelöscht.",
            "Projekt löschen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var name = SelectedProject.Name;
                var number = SelectedProject.ProjectNumber;

                _db.DeleteProject(SelectedProject.Id);
                Projects.Remove(SelectedProject);
                SelectedProject = null;
                ExportRegistry();
                Log.Information("Project deleted: {Name} ({Number})", name, number);
                RegistryStatus = $"Projekt {name} gelöscht";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete project");
                MessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
