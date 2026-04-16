using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// ViewModel für die PlanManager-Hauptseite — Projektliste mit Karten-Layout.
/// Zeigt alle aktiven Projekte mit Eingangs-Badge, Suchfeld und Spalten.
/// </summary>
public partial class PlanManagerViewModel : ObservableObject
{
    private readonly ProjectDatabase _db;

    [ObservableProperty]
    private ObservableCollection<PlanProjectItem> _projects = [];

    [ObservableProperty]
    private ObservableCollection<PlanProjectItem> _filteredProjects = [];

    [ObservableProperty]
    private PlanProjectItem? _selectedProject;

    [ObservableProperty]
    private string _summaryText = string.Empty;

    [ObservableProperty]
    private string _projectCountText = string.Empty;

    [ObservableProperty]
    private int _totalInboxCount;

    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// Wird vom Code-Behind ausgelöst wenn der User ein Projekt anklickt.
    /// </summary>
    public event Action<Project>? ProjectSelected;

    public PlanManagerViewModel(ProjectDatabase db)
    {
        _db = db;
        LoadProjects();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadProjects();
    }

    private void LoadProjects()
    {
        try
        {
            var loaded = _db.LoadAllProjects()
                .Where(p => p.Status == ProjectStatus.Active)
                .ToList();
            var items = new ObservableCollection<PlanProjectItem>();
            var totalInbox = 0;

            foreach (var project in loaded)
            {
                var inboxCount = CountInboxFiles(project);
                totalInbox += inboxCount;
                items.Add(new PlanProjectItem
                {
                    Project = project,
                    InboxCount = inboxCount
                });
            }

            Projects = items;
            TotalInboxCount = totalInbox;
            SummaryText = totalInbox > 0
                ? $"{loaded.Count} aktive Projekte \u00b7 {totalInbox} Dateien im Eingang"
                : $"{loaded.Count} aktive Projekte";

            ApplyFilter();
            Log.Information("PlanManager: {Count} Projekte geladen", loaded.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "PlanManager: Projekte konnten nicht geladen werden");
            Projects = [];
            TotalInboxCount = 0;
            SummaryText = string.Empty;
            ApplyFilter();
        }
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredProjects = new ObservableCollection<PlanProjectItem>(Projects);
        }
        else
        {
            var search = SearchText.Trim();
            FilteredProjects = new ObservableCollection<PlanProjectItem>(
                Projects.Where(p =>
                    p.Project.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Project.ProjectNumber.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        ProjectCountText = $"{FilteredProjects.Count} Projekte geladen";
    }

    private static int CountInboxFiles(Project project)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(project.Paths.Root))
                return 0;

            var inboxPath = Path.Combine(project.Paths.Root, project.Paths.Inbox);
            if (!Directory.Exists(inboxPath))
                return 0;

            return Directory.GetFiles(inboxPath, "*", SearchOption.AllDirectories).Length;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Eingang konnte nicht gelesen werden für {Project}", project.Name);
            return 0;
        }
    }

    public void OnProjectDoubleClicked(PlanProjectItem item)
    {
        ProjectSelected?.Invoke(item.Project);
    }
}

/// <summary>
/// Wrapper um Project mit Eingangs-Zähler für die Kartenanzeige.
/// </summary>
public class PlanProjectItem
{
    public Project Project { get; set; } = new();
    public int InboxCount { get; set; }
    public bool HasInbox => InboxCount > 0;
    public string InboxBadge => InboxCount > 0 ? $"{InboxCount} unsortiert" : "";
}
