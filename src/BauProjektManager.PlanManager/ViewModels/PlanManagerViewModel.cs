using System.Collections.ObjectModel;
using System.IO;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// ViewModel für die PlanManager-Hauptseite — Projektliste mit Karten-Layout.
/// Zeigt alle aktiven Projekte mit Eingangs-Badge und Subtext.
/// </summary>
public partial class PlanManagerViewModel : ObservableObject
{
    private readonly ProjectDatabase _db = new(new Infrastructure.Services.UlidIdGenerator());

    [ObservableProperty]
    private ObservableCollection<PlanProjectItem> _projects = [];

    [ObservableProperty]
    private PlanProjectItem? _selectedProject;

    [ObservableProperty]
    private string _summaryText = string.Empty;

    [ObservableProperty]
    private int _totalInboxCount;

    /// <summary>
    /// Wird vom Code-Behind ausgelöst wenn der User ein Projekt anklickt.
    /// </summary>
    public event Action<Project>? ProjectSelected;

    public PlanManagerViewModel()
    {
        LoadProjects();
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
            var loaded = _db.LoadAllProjects();
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

            Log.Information("PlanManager: {Count} Projekte geladen", loaded.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "PlanManager: Projekte konnten nicht geladen werden");
            Projects = [];
            TotalInboxCount = 0;
            SummaryText = string.Empty;
        }
    }

    /// <summary>
    /// Zählt die Dateien im _Eingang/-Ordner eines Projekts.
    /// Gibt 0 zurück wenn Ordner nicht existiert oder Pfad fehlt.
    /// </summary>
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
    public string Subtext => HasInbox ? $"{InboxCount} Dateien im Eingang" : "Kein Eingang";
}
