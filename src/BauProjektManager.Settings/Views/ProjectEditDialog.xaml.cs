using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;

namespace BauProjektManager.Settings.Views;

/// <summary>
/// Helper class for displaying folder entries in the ListBox with numbered preview.
/// </summary>
public class FolderDisplayItem
{
    public string Name { get; set; } = string.Empty;
    public bool HasInbox { get; set; }
    public int Position { get; set; }
    public string NumberedDisplay => $"{Position:D2} {Name}";
    public Visibility InboxVisibility => HasInbox ? Visibility.Visible : Visibility.Collapsed;
}

public partial class ProjectEditDialog : Window
{
    public Project Project { get; private set; }
    public List<FolderTemplateEntry>? FolderTemplate { get; private set; }

    private ObservableCollection<FolderDisplayItem> _folderItems = [];
    private readonly bool _isNewProject;

    public ProjectEditDialog(Project project) : this(project, null) { }

    public ProjectEditDialog(Project project, List<FolderTemplateEntry>? folderTemplate)
    {
        InitializeComponent();
        Project = project;
        _isNewProject = folderTemplate is not null;

        if (_isNewProject && folderTemplate is not null)
        {
            TxtDialogTitle.Text = "Neues Projekt anlegen";

            _folderItems = new ObservableCollection<FolderDisplayItem>(
                folderTemplate.Select((f, i) => new FolderDisplayItem
                {
                    Name = f.Name,
                    HasInbox = f.HasInbox,
                    Position = i
                }));

            PanelFolders.Visibility = Visibility.Visible;
            LstFolders.ItemsSource = _folderItems;
            UpdateFolderPreview();
        }
        else
        {
            PanelPaths.Visibility = Visibility.Visible;
        }

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

        // Koordinaten
        TxtCoordSystem.Text = Project.Location.CoordinateSystem;
        TxtCoordEast.Text = Project.Location.CoordinateEast != 0
            ? Project.Location.CoordinateEast.ToString(CultureInfo.InvariantCulture) : "";
        TxtCoordNorth.Text = Project.Location.CoordinateNorth != 0
            ? Project.Location.CoordinateNorth.ToString(CultureInfo.InvariantCulture) : "";

        // Grundstück
        TxtCadastralKg.Text = Project.Location.CadastralKg;
        TxtCadastralKgName.Text = Project.Location.CadastralKgName;
        TxtCadastralGst.Text = Project.Location.CadastralGst;

        // Laufzeit
        DpConstructionStart.SelectedDate = Project.Timeline.ConstructionStart;
        DpPlannedEnd.SelectedDate = Project.Timeline.PlannedEnd;
        DpActualEnd.SelectedDate = Project.Timeline.ActualEnd;

        // Sonstiges
        TxtTags.Text = Project.Tags;
        TxtNotes.Text = Project.Notes;

        // Pfade (nur bei bestehendem Projekt)
        if (!_isNewProject)
        {
            TxtPathRoot.Text = Project.Paths.Root;
            TxtPathPlans.Text = Project.Paths.Plans;
            TxtPathInbox.Text = Project.Paths.Inbox;
            TxtPathPhotos.Text = Project.Paths.Photos;
            TxtPathDocs.Text = Project.Paths.Documents;
            TxtPathProtocols.Text = Project.Paths.Protocols;
        }
    }

    private void OnProjectStartChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DpProjectStart.SelectedDate.HasValue)
        {
            TxtNumberPreview.Text = DpProjectStart.SelectedDate.Value.ToString("yyyyMM");
            UpdateFolderPreview();
        }
    }

    // === Folder preview tree ===

    private void UpdateFolderPreview()
    {
        if (!_isNewProject || TxtFolderPreview is null) return;

        var projectName = !string.IsNullOrEmpty(TxtName.Text)
            ? TxtName.Text
            : "Projektname";
        var number = !string.IsNullOrEmpty(TxtNumberPreview.Text)
            ? TxtNumberPreview.Text
            : "YYYYMM";

        var sb = new StringBuilder();
        sb.AppendLine($"{number}_{projectName}/");

        for (int i = 0; i < _folderItems.Count; i++)
        {
            var entry = _folderItems[i];
            var prefix = i == _folderItems.Count - 1 ? "└── " : "├── ";
            sb.AppendLine($"{prefix}{entry.Position:D2} {entry.Name}/");

            if (entry.HasInbox)
            {
                var innerPrefix = i == _folderItems.Count - 1 ? "    " : "│   ";
                sb.AppendLine($"{innerPrefix}└── _Eingang/");
            }
        }

        TxtFolderPreview.Text = sb.ToString().TrimEnd();
    }

    // === Folder template buttons ===

    private void RefreshFolderNumbers()
    {
        for (int i = 0; i < _folderItems.Count; i++)
        {
            _folderItems[i].Position = i;
        }
        var items = _folderItems.ToList();
        _folderItems.Clear();
        foreach (var item in items)
        {
            _folderItems.Add(item);
        }
        UpdateFolderPreview();
    }

    private void OnFolderSelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void OnFolderMoveUp(object sender, RoutedEventArgs e)
    {
        var index = LstFolders.SelectedIndex;
        if (index <= 0) return;
        var item = _folderItems[index];
        _folderItems.RemoveAt(index);
        _folderItems.Insert(index - 1, item);
        RefreshFolderNumbers();
        LstFolders.SelectedIndex = index - 1;
    }

    private void OnFolderMoveDown(object sender, RoutedEventArgs e)
    {
        var index = LstFolders.SelectedIndex;
        if (index < 0 || index >= _folderItems.Count - 1) return;
        var item = _folderItems[index];
        _folderItems.RemoveAt(index);
        _folderItems.Insert(index + 1, item);
        RefreshFolderNumbers();
        LstFolders.SelectedIndex = index + 1;
    }

    private void OnFolderAdd(object sender, RoutedEventArgs e)
    {
        var inputWindow = new Window
        {
            Title = "Ordner hinzufügen",
            Width = 350,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D30"))
        };

        var stack = new StackPanel { Margin = new Thickness(15) };
        var label = new TextBlock
        {
            Text = "Ordnername (ohne Nummer):",
            Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CCCCCC")),
            Margin = new Thickness(0, 0, 0, 5)
        };
        var textBox = new TextBox
        {
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E1E1E")),
            Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CCCCCC")),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3E3E42")),
            Padding = new Thickness(5, 3, 5, 3),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var btnOk = new Button
        {
            Content = "OK",
            Width = 80,
            Padding = new Thickness(0, 5, 0, 5),
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#007ACC")),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        btnOk.Click += (_, _) => { inputWindow.DialogResult = true; inputWindow.Close(); };

        stack.Children.Add(label);
        stack.Children.Add(textBox);
        stack.Children.Add(btnOk);
        inputWindow.Content = stack;
        inputWindow.ContentRendered += (_, _) => textBox.Focus();

        if (inputWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            _folderItems.Add(new FolderDisplayItem
            {
                Name = textBox.Text.Trim(),
                HasInbox = false,
                Position = _folderItems.Count
            });
            RefreshFolderNumbers();
            LstFolders.SelectedIndex = _folderItems.Count - 1;
        }
    }

    private void OnFolderRemove(object sender, RoutedEventArgs e)
    {
        var index = LstFolders.SelectedIndex;
        if (index < 0) return;
        var item = _folderItems[index];
        if (MessageBox.Show($"Ordner \"{item.Name}\" entfernen?", "Ordner entfernen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _folderItems.RemoveAt(index);
            RefreshFolderNumbers();
            if (_folderItems.Count > 0)
                LstFolders.SelectedIndex = Math.Min(index, _folderItems.Count - 1);
        }
    }

    private void OnFolderToggleInbox(object sender, RoutedEventArgs e)
    {
        var index = LstFolders.SelectedIndex;
        if (index < 0) return;
        _folderItems[index].HasInbox = !_folderItems[index].HasInbox;
        RefreshFolderNumbers();
        LstFolders.SelectedIndex = index;
    }

    // === Save / Cancel ===

    private void OnSave(object sender, RoutedEventArgs e)
    {
        // Stammdaten
        Project.Name = TxtName.Text;
        Project.FullName = TxtFullName.Text;
        Project.Timeline.ProjectStart = DpProjectStart.SelectedDate;
        Project.Status = (ProjectStatus)CmbStatus.SelectedItem;
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

        // Koordinaten
        Project.Location.CoordinateSystem = TxtCoordSystem.Text;
        if (double.TryParse(TxtCoordEast.Text, CultureInfo.InvariantCulture, out var east))
            Project.Location.CoordinateEast = east;
        if (double.TryParse(TxtCoordNorth.Text, CultureInfo.InvariantCulture, out var north))
            Project.Location.CoordinateNorth = north;

        // Grundstück
        Project.Location.CadastralKg = TxtCadastralKg.Text;
        Project.Location.CadastralKgName = TxtCadastralKgName.Text;
        Project.Location.CadastralGst = TxtCadastralGst.Text;

        // Laufzeit
        Project.Timeline.ConstructionStart = DpConstructionStart.SelectedDate;
        Project.Timeline.PlannedEnd = DpPlannedEnd.SelectedDate;
        Project.Timeline.ActualEnd = DpActualEnd.SelectedDate;

        // Sonstiges
        Project.Tags = TxtTags.Text;
        Project.Notes = TxtNotes.Text;

        // Folder template (only for new projects)
        if (_isNewProject)
        {
            FolderTemplate = _folderItems.Select(f => new FolderTemplateEntry(f.Name, f.HasInbox)).ToList();
        }

        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
