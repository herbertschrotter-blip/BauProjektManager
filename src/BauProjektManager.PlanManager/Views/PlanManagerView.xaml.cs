using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// BoolToVisibility — true=Visible, false=Collapsed.
/// </summary>
public class BoolToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Count==0 → Visible (Empty State), Count>0 → Collapsed.
/// </summary>
public class CountToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int count && count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public partial class PlanManagerView : UserControl
{
    private readonly PlanManagerViewModel _vm;
    private readonly BoolToVisConverter _boolToVis = new();
    private readonly IIdGenerator _idGenerator = new UlidIdGenerator();
    private readonly ProfileManager _profileManager;
    private readonly PatternTemplateService _templateService;

    public PlanManagerView()
    {
        _profileManager = new ProfileManager(_idGenerator);
        _templateService = new PatternTemplateService(_idGenerator);
        Resources.Add("BoolToVis", _boolToVis);
        Resources.Add("CountToVis", new CountToVisConverter());
        InitializeComponent();

        _vm = new PlanManagerViewModel();
        _vm.ProjectSelected += NavigateToDetail;
        DataContext = _vm;
    }

    private void OnProjectDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_vm.SelectedProject is not null)
            _vm.OnProjectDoubleClicked(_vm.SelectedProject);
    }

    private void NavigateToDetail(Project project)
    {
        // AppData path for pattern-templates.json: BasePath/.AppData/BauProjektManager/
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
