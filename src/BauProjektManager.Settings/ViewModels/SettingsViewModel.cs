using System.Collections.ObjectModel;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;
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
                    Address = "Hauptstraße 15, 8143 Dobl",
                    Municipality = "Dobl-Zwaring",
                    District = "Graz-Umgebung",
                    State = "Steiermark"
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
            ProjectNumber = DateTime.Now.ToString("yyyyMM"),
            Name = "Neues-Projekt",
            FullName = "Neues Projekt",
            Status = ProjectStatus.Active
        };
        Projects.Add(newProject);
        SelectedProject = newProject;
    }
}
