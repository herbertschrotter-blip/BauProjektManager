using System.Collections.ObjectModel;
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
/// ViewModel for the Settings page — manages project list via SQLite.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ProjectDatabase _db = new();

    [ObservableProperty]
    private ObservableCollection<Project> _projects = [];

    [ObservableProperty]
    private Project? _selectedProject;

    public SettingsViewModel()
    {
        LoadProjects();
    }

    private void LoadProjects()
    {
        try
        {
            var loaded = _db.LoadAllProjects();
            Projects = new ObservableCollection<Project>(loaded);
            Log.Information("Loaded {Count} projects from database", loaded.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load projects from database");
            Projects = [];
        }
    }

    [RelayCommand]
    private void AddProject()
    {
        var newProject = new Project
        {
            Id = $"proj_{DateTime.Now:yyyyMMdd_HHmmss}",
            Status = ProjectStatus.Active,
            Timeline = new ProjectTimeline
            {
                ProjectStart = DateTime.Today
            }
        };
        newProject.UpdateProjectNumberFromStart();

        var dialog = new ProjectEditDialog(newProject);
        dialog.Owner = Application.Current.MainWindow;
        if (dialog.ShowDialog() == true)
        {
            try
            {
                _db.SaveProject(dialog.Project);
                Projects.Add(dialog.Project);
                SelectedProject = dialog.Project;
                Log.Information("Project added: {Name} ({Number})", dialog.Project.Name, dialog.Project.ProjectNumber);
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
                // ObservableCollection refreshen
                int index = Projects.IndexOf(SelectedProject);
                if (index >= 0)
                {
                    Projects[index] = dialog.Project;
                    SelectedProject = dialog.Project;
                }
                Log.Information("Project updated: {Name} ({Number})", dialog.Project.Name, dialog.Project.ProjectNumber);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to update project");
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
