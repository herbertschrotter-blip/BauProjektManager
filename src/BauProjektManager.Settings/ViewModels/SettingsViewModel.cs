using System.Collections.ObjectModel;
using System.Windows;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;
using BauProjektManager.Settings.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BauProjektManager.Settings.ViewModels;

/// <summary>
/// ViewModel for the Settings page — manages project list.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Project> _projects = [];

    [ObservableProperty]
    private Project? _selectedProject;

    public SettingsViewModel()
    {
        // Testdaten — werden später durch SQLite ersetzt
        Projects =
        [
            new Project
            {
                Id = "proj_202512_dobl",
                ProjectNumber = "202512",
                Name = "ÖWG-Dobl-Zwaring",
                FullName = "Gartensiedlung Dobl-Zwaring",
                Status = ProjectStatus.Active,
                Location = new ProjectLocation
                {
                    Street = "Hauptstraße",
                    HouseNumber = "15",
                    PostalCode = "8143",
                    City = "Dobl",
                    Municipality = "Dobl-Zwaring",
                    District = "Graz-Umgebung",
                    State = "Steiermark"
                },
                Client = new Client
                {
                    Company = "ÖWG Wohnbau",
                    ContactPerson = "Ing. Müller",
                    Phone = "0316/12345",
                    Email = "mueller@oewg.at"
                },
                Timeline = new ProjectTimeline
                {
                    ProjectStart = new DateTime(2025, 12, 1),
                    ConstructionStart = new DateTime(2026, 6, 1)
                },
                Buildings =
                [
                    new Building { ShortName = "H64", Name = "Haus Nr. 64", Type = "Reihenhaus", Levels = ["KG","EG","1.OG","2.OG","Dach"] },
                    new Building { ShortName = "H66", Name = "Haus Nr. 66", Type = "Reihenhaus", Levels = ["EG","OG","Dach"] },
                    new Building { ShortName = "H68", Name = "Haus Nr. 68", Type = "Reihenhaus", Levels = ["EG","1.OG","2.OG","Dach"] }
                ],
                Tags = "Wohnbau, Reihenhäuser, ÖWGES",
                Notes = "Bauteil B-13, 3 Häuser"
            },
            new Project
            {
                Id = "proj_202302_kapfenberg",
                ProjectNumber = "202302",
                Name = "Reihenhäuser-Kapfenberg",
                FullName = "Reihenhausanlage Kapfenberg-Süd",
                Status = ProjectStatus.Active,
                Location = new ProjectLocation
                {
                    Municipality = "Kapfenberg",
                    District = "Bruck-Mürzzuschlag",
                    State = "Steiermark"
                },
                Timeline = new ProjectTimeline
                {
                    ProjectStart = new DateTime(2023, 2, 1)
                }
            },
            new Project
            {
                Id = "proj_202201_leoben",
                ProjectNumber = "202201",
                Name = "Sanierung-Leoben",
                FullName = "Altbausanierung Leoben Zentrum",
                Status = ProjectStatus.Completed,
                Location = new ProjectLocation
                {
                    Municipality = "Leoben",
                    District = "Leoben",
                    State = "Steiermark"
                },
                Timeline = new ProjectTimeline
                {
                    ProjectStart = new DateTime(2022, 1, 1),
                    ActualEnd = new DateTime(2024, 6, 30)
                }
            }
        ];
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
            Projects.Add(dialog.Project);
            SelectedProject = dialog.Project;
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
            // ObservableCollection refreshen
            int index = Projects.IndexOf(SelectedProject);
            if (index >= 0)
            {
                Projects[index] = dialog.Project;
                SelectedProject = dialog.Project;
            }
        }
    }
}
