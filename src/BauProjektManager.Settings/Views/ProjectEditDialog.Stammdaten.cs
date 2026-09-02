using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace BauProjektManager.Settings.Views;

/// <summary>Tab 1 Stammdaten: Dropdowns, Projektdaten laden, Projektnummer-Vorschau (BPM-070 Partial-Split).</summary>
public partial class ProjectEditDialog
{
    private void LoadDropdowns()
    {
        var settings = _settingsService.LoadSharedOrDefault();
        CmbProjectType.ItemsSource = settings.ProjectTypes;
        if (!string.IsNullOrEmpty(Project.ProjectType))
            CmbProjectType.SelectedItem = Project.ProjectType;
        else if (settings.ProjectTypes.Count > 0)
            CmbProjectType.SelectedIndex = 0;
        CmbStatus.ItemsSource = Enum.GetValues<ProjectStatus>();
        CmbStatus.SelectedItem = Project.Status;
    }

    private void OnEditProjectTypes(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.LoadSharedOrDefault();
        var items = new ObservableCollection<string>(settings.ProjectTypes);
        if (ShowSimpleListEditDialog("Projektarten bearbeiten", items))
        {
            var selected = CmbProjectType.SelectedItem as string;
            settings.ProjectTypes = items.ToList();
            _settingsService.SaveSharedOrDefault(settings);
            CmbProjectType.ItemsSource = settings.ProjectTypes;
            if (selected is not null && settings.ProjectTypes.Contains(selected))
                CmbProjectType.SelectedItem = selected;
            else if (settings.ProjectTypes.Count > 0)
                CmbProjectType.SelectedIndex = 0;
        }
    }

    private void LoadProjectData()
    {
        TxtName.Text = Project.Name;
        TxtFullName.Text = Project.FullName;
        DpProjectStart.SelectedDate = Project.Timeline.ProjectStart;
        TxtNumberPreview.Text = Project.ProjectNumber;
        TxtClientCompany.Text = Project.Client.Company;
        TxtClientContact.Text = Project.Client.ContactPerson;
        TxtClientPhone.Text = Project.Client.Phone;
        TxtClientEmail.Text = Project.Client.Email;
        TxtStreet.Text = Project.Location.Street;
        TxtHouseNumber.Text = Project.Location.HouseNumber;
        TxtPostalCode.Text = Project.Location.PostalCode;
        TxtCity.Text = Project.Location.City;
        TxtMunicipality.Text = Project.Location.Municipality;
        TxtDistrict.Text = Project.Location.District;
        TxtState.Text = Project.Location.State;
        TxtCoordSystem.Text = Project.Location.CoordinateSystem;
        TxtCoordEast.Text = Project.Location.CoordinateEast != 0 ? Project.Location.CoordinateEast.ToString(CultureInfo.InvariantCulture) : "";
        TxtCoordNorth.Text = Project.Location.CoordinateNorth != 0 ? Project.Location.CoordinateNorth.ToString(CultureInfo.InvariantCulture) : "";
        TxtCadastralKg.Text = Project.Location.CadastralKg;
        TxtCadastralKgName.Text = Project.Location.CadastralKgName;
        TxtCadastralGst.Text = Project.Location.CadastralGst;
        DpConstructionStart.SelectedDate = Project.Timeline.ConstructionStart;
        DpPlannedEnd.SelectedDate = Project.Timeline.PlannedEnd;
        DpActualEnd.SelectedDate = Project.Timeline.ActualEnd;
        TxtTags.Text = Project.Tags;
        TxtNotes.Text = Project.Notes;
    }

    private void OnProjectStartChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DpProjectStart.SelectedDate.HasValue)
        {
            TxtNumberPreview.Text = DpProjectStart.SelectedDate.Value.ToString("yyyyMM");
            var projectName = !string.IsNullOrEmpty(TxtName?.Text) ? TxtName.Text : (Project?.Name ?? "Projektname");
            ProjectFolderTemplate.PreviewRootName = $"{TxtNumberPreview.Text}_{projectName}";
        }
    }
}
