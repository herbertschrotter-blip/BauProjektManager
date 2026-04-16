using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// BoolToVisibility — true=Visible, false=Collapsed.
/// </summary>
public sealed class BoolToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Inverse BoolToVisibility — true=Collapsed, false=Visible.
/// </summary>
public sealed class InverseBoolToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Count==0 → Visible (Empty State), Count>0 → Collapsed.
/// </summary>
public sealed class CountToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int count && count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// String leer/null → Visible (Placeholder sichtbar), sonst Collapsed.
/// </summary>
public sealed class EmptyToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public partial class PlanManagerView : UserControl
{
    private readonly PlanManagerViewModel _vm;
    private readonly BoolToVisConverter _boolToVis = new();
    private readonly IIdGenerator _idGenerator;
    private readonly ProfileManager _profileManager;
    private readonly PatternTemplateService _templateService;

    public PlanManagerView(ProjectDatabase db, IIdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
        _profileManager = new ProfileManager(_idGenerator);
        _templateService = new PatternTemplateService(_idGenerator);
        Resources.Add("BoolToVis", _boolToVis);
        Resources.Add("InverseBoolToVis", new InverseBoolToVisConverter());
        Resources.Add("CountToVis", new CountToVisConverter());
        Resources.Add("EmptyToVis", new EmptyToVisConverter());
        InitializeComponent();

        _vm = new PlanManagerViewModel(db);
        _vm.ProjectSelected += NavigateToDetail;
        DataContext = _vm;
    }

    /// <summary>
    /// Gesamtzahl unsortierter Dateien über alle Projekte — für Sidebar-Badge.
    /// </summary>
    public int TotalInboxCount => _vm.TotalInboxCount;

    private void OnProjectSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
            return;

        if (e.AddedItems[0] is not PlanProjectItem item)
            return;

        _vm.SelectedProject = null;
        _vm.OnProjectDoubleClicked(item);
    }

    private void NavigateToDetail(Project project)
    {
        var appDataPath = !string.IsNullOrEmpty(project.Paths.Root)
            ? Path.Combine(Path.GetDirectoryName(project.Paths.Root) ?? "", ".AppData", "BauProjektManager")
            : null;

        var detailView = new ProjectDetailView(
            project, _boolToVis, _profileManager, _idGenerator, _templateService, appDataPath);
        detailView.ViewModel.NavigateBack += NavigateToList;

        ProjectListPanel.Visibility = Visibility.Collapsed;
        DetailHost.Content = detailView;
        DetailHost.Visibility = Visibility.Visible;
    }

    private void NavigateToList()
    {
        DetailHost.Content = null;
        DetailHost.Visibility = Visibility.Collapsed;
        ProjectListPanel.Visibility = Visibility.Visible;

        _vm.RefreshCommand.Execute(null);
    }
}
