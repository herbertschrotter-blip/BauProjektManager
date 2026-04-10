using System.IO;
using BauProjektManager.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// ViewModel für die Projektdetail-Ansicht im PlanManager.
/// Zeigt Eingangs-Info, Profile und Tabs.
/// </summary>
public partial class ProjectDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private Project _project;

    [ObservableProperty]
    private int _inboxCount;

    [ObservableProperty]
    private string _inboxInfo = "";

    /// <summary>
    /// Wird ausgelöst wenn der User ← Zurück klickt.
    /// PlanManagerView reagiert darauf und zeigt die Projektliste.
    /// </summary>
    public event Action? NavigateBack;

    public ProjectDetailViewModel(Project project)
    {
        _project = project;
        RefreshInbox();
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigateBack?.Invoke();
    }

    [RelayCommand]
    private void RefreshInbox()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Project.Paths.Root))
            {
                InboxCount = 0;
                InboxInfo = "";
                return;
            }

            var inboxPath = Path.Combine(Project.Paths.Root, Project.Paths.Inbox);
            if (!Directory.Exists(inboxPath))
            {
                InboxCount = 0;
                InboxInfo = "";
                return;
            }

            InboxCount = Directory.GetFiles(inboxPath).Length;
            InboxInfo = InboxCount > 0
                ? $"{InboxCount} Dateien im Eingang"
                : "";
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Eingang nicht lesbar für {Project}", Project.Name);
            InboxCount = 0;
            InboxInfo = "";
        }
    }

    public bool HasInbox => InboxCount > 0;
}
