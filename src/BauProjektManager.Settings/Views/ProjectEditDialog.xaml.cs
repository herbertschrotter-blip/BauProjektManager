using System.Windows;
using System.Windows.Controls;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;

namespace BauProjektManager.Settings.Views;

public partial class ProjectEditDialog : Window
{
    public Project Project { get; private set; }

    public ProjectEditDialog(Project project)
    {
        InitializeComponent();
        Project = project;
        LoadProjectData();
    }

    private void LoadProjectData()
    {
        // Stammdaten
        TxtName.Text = Project.Name;
        TxtFullName.Text = Project.FullName;
        DpProjectStart.SelectedDate = Project.Timeline.ProjectStart;
        TxtNumberPreview.Text = Project.ProjectNumber;

        CmbStatus.ItemsSource = Enum.GetValues<ProjectStatus>();
        CmbStatus.SelectedItem = Project.Status;

        // Auftraggeber
        TxtClientCompany.Text = Project.Client.Company;
        TxtClientContact.Text = Project.Client.ContactPerson;
        TxtClientPhone.Text = Project.Client.Phone;
        TxtClientEmail.Text = Project.Client.Email;

        // Adresse
        TxtStreet.Text = Project.Location.Street;
        TxtHouseNumber.Text = Project.Location.HouseNumber;
        TxtPostalCode.Text = Project.Location.PostalCode;
        TxtCity.Text = Project.Location.City;

        // Verwaltung
        TxtMunicipality.Text = Project.Location.Municipality;
        TxtDistrict.Text = Project.Location.District;
        TxtState.Text = Project.Location.State;

        // Sonstiges
        TxtTags.Text = Project.Tags;
        TxtNotes.Text = Project.Notes;
    }

    private void OnProjectStartChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DpProjectStart.SelectedDate.HasValue)
        {
            TxtNumberPreview.Text = DpProjectStart.SelectedDate.Value.ToString("yyyyMM");
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        // Stammdaten
        Project.Name = TxtName.Text;
        Project.FullName = TxtFullName.Text;
        Project.Timeline.ProjectStart = DpProjectStart.SelectedDate;
        Project.Status = (ProjectStatus)CmbStatus.SelectedItem;

        // Nummer aus Startdatum
        Project.UpdateProjectNumberFromStart();

        // Auftraggeber
        Project.Client.Company = TxtClientCompany.Text;
        Project.Client.ContactPerson = TxtClientContact.Text;
        Project.Client.Phone = TxtClientPhone.Text;
        Project.Client.Email = TxtClientEmail.Text;

        // Adresse
        Project.Location.Street = TxtStreet.Text;
        Project.Location.HouseNumber = TxtHouseNumber.Text;
        Project.Location.PostalCode = TxtPostalCode.Text;
        Project.Location.City = TxtCity.Text;

        // Verwaltung
        Project.Location.Municipality = TxtMunicipality.Text;
        Project.Location.District = TxtDistrict.Text;
        Project.Location.State = TxtState.Text;

        // Sonstiges
        Project.Tags = TxtTags.Text;
        Project.Notes = TxtNotes.Text;

        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
