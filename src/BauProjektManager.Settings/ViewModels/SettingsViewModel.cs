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
/// ViewModel for the Settings page — manages project list, paths display,
/// and global default folder template configuration.
/// </summary>
public partial class SettingsViewModel : ObservableObject
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

    // Paths display
    [ObservableProperty]
    private string _basePath = "";

    [ObservableProperty]
    private string _archivePath = "";

    [ObservableProperty]
    private string _oneDrivePath = "";

    // Default folder template
    [ObservableProperty]
    private ObservableCollection<FolderDisplayItem> _defaultFolderItems = [];

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
            {
                return current.FullName;
            }
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

        // Load default folder template
        DefaultFolderItems = new ObservableCollection<FolderDisplayItem>(
            settings.FolderTemplate.Select((f, i) => new FolderDisplayItem
            {
                Name = f.Name,
                HasInbox = f.HasInbox,
                Position = i
            }));
    }

    private void SaveFolderTemplate()
    {
        var settings = _settingsService.Load();
        settings.FolderTemplate = DefaultFolderItems
            .Select(f => new FolderTemplateEntry(f.Name, f.HasInbox))
            .ToList();
        _settingsService.Save(settings);
        Log.Information("Default folder template saved ({Count} entries)", settings.FolderTemplate.Count);
    }

    private void RefreshDefaultFolderNumbers()
    {
        for (int i = 0; i < DefaultFolderItems.Count; i++)
        {
            DefaultFolderItems[i].Position = i;
        }
        var items = DefaultFolderItems.ToList();
        DefaultFolderItems.Clear();
        foreach (var item in items)
        {
            DefaultFolderItems.Add(item);
        }
    }

    // === Default folder template commands ===

    [RelayCommand]
    private void DefaultFolderMoveUp(FolderDisplayItem? item)
    {
        if (item is null) return;
        var index = DefaultFolderItems.IndexOf(item);
        if (index <= 0) return;
        DefaultFolderItems.RemoveAt(index);
        DefaultFolderItems.Insert(index - 1, item);
        RefreshDefaultFolderNumbers();
        SaveFolderTemplate();
    }

    [RelayCommand]
    private void DefaultFolderMoveDown(FolderDisplayItem? item)
    {
        if (item is null) return;
        var index = DefaultFolderItems.IndexOf(item);
        if (index < 0 || index >= DefaultFolderItems.Count - 1) return;
        DefaultFolderItems.RemoveAt(index);
        DefaultFolderItems.Insert(index + 1, item);
        RefreshDefaultFolderNumbers();
        SaveFolderTemplate();
    }

    [RelayCommand]
    private void DefaultFolderAdd()
    {
        var inputWindow = new Window
        {
            Title = "Ordner hinzufügen",
            Width = 350,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current.MainWindow,
            ResizeMode = ResizeMode.NoResize,
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D30"))
        };

        var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(15) };
        var label = new System.Windows.Controls.TextBlock
        {
            Text = "Ordnername (ohne Nummer):",
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

        stack.Children.Add(label);
        stack.Children.Add(textBox);
        stack.Children.Add(btnOk);
        inputWindow.Content = stack;
        inputWindow.ContentRendered += (_, _) => textBox.Focus();

        if (inputWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            DefaultFolderItems.Add(new FolderDisplayItem
            {
                Name = textBox.Text.Trim(),
                HasInbox = false,
                Position = DefaultFolderItems.Count
            });
            RefreshDefaultFolderNumbers();
            SaveFolderTemplate();
        }
    }

    [RelayCommand]
    private void DefaultFolderRemove(FolderDisplayItem? item)
    {
        if (item is null) return;
        if (MessageBox.Show($"Ordner \"{item.Name}\" aus Standard-Template entfernen?",
                "Ordner entfernen", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            DefaultFolderItems.Remove(item);
            RefreshDefaultFolderNumbers();
            SaveFolderTemplate();
        }
    }

    [RelayCommand]
    private void DefaultFolderToggleInbox(FolderDisplayItem? item)
    {
        if (item is null) return;
        item.HasInbox = !item.HasInbox;
        RefreshDefaultFolderNumbers();
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
                    Projects[index] = dialog.Project;
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
}
