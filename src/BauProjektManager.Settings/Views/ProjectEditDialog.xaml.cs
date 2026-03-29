using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;
using BauProjektManager.Settings.ViewModels;

namespace BauProjektManager.Settings.Views;

public partial class ProjectEditDialog : Window
{
    public Project Project { get; private set; }
    public List<FolderTemplateEntry>? FolderTemplate { get; private set; }

    private ObservableCollection<FolderTreeItem> _folderTreeItems = [];
    private FolderTreeItem? _selectedTreeItem;
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
            _folderTreeItems = BuildTreeFromTemplate(folderTemplate);
        }
        else
        {
            TxtDialogTitle.Text = "Projekt bearbeiten";
            _folderTreeItems = LoadFoldersFromDisk(project.Paths.Root);
        }

        TvFolders.ItemsSource = _folderTreeItems;
        UpdateFolderPreview();
        LoadProjectData();
    }

    /// <summary>
    /// Builds FolderTreeItems from a FolderTemplateEntry list (for new projects).
    /// </summary>
    private static ObservableCollection<FolderTreeItem> BuildTreeFromTemplate(List<FolderTemplateEntry> template)
    {
        var items = new ObservableCollection<FolderTreeItem>();
        for (int i = 0; i < template.Count; i++)
        {
            var entry = template[i];
            var mainItem = new FolderTreeItem
            {
                Name = entry.Name,
                Position = i,
                IsMainFolder = true,
                HasInbox = entry.HasInbox
            };

            int subPos = 0;
            foreach (var sub in entry.SubFolders)
            {
                mainItem.Children.Add(new FolderTreeItem
                {
                    Name = sub.Name,
                    Position = sub.HasPrefix ? subPos : -1,
                    IsMainFolder = false,
                    HasPrefix = sub.HasPrefix
                });
                if (sub.HasPrefix) subPos++;
            }
            items.Add(mainItem);
        }
        return items;
    }

    /// <summary>
    /// Reads existing subfolders from the project root path (for editing).
    /// Detects numbered folders and _Eingang subfolders.
    /// Also reads second-level subfolders with prefix detection.
    /// </summary>
    private static ObservableCollection<FolderTreeItem> LoadFoldersFromDisk(string rootPath)
    {
        var items = new ObservableCollection<FolderTreeItem>();
        if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
            return items;

        var subDirs = Directory.GetDirectories(rootPath)
            .Select(d => new DirectoryInfo(d))
            .OrderBy(d => d.Name)
            .ToList();

        int position = 0;
        foreach (var dir in subDirs)
        {
            if (dir.Attributes.HasFlag(FileAttributes.Hidden)) continue;

            var name = dir.Name;
            if (name.Length > 3 && char.IsDigit(name[0]) && char.IsDigit(name[1]) && name[2] == ' ')
                name = name[3..];

            var hasInbox = Directory.Exists(Path.Combine(dir.FullName, "_Eingang"));

            var mainItem = new FolderTreeItem
            {
                Name = name,
                HasInbox = hasInbox,
                Position = position++,
                IsMainFolder = true
            };

            // Read second-level subfolders
            var subSubDirs = Directory.GetDirectories(dir.FullName)
                .Select(d => new DirectoryInfo(d))
                .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden) && d.Name != "_Eingang")
                .OrderBy(d => d.Name)
                .ToList();

            int subPos = 0;
            foreach (var subDir in subSubDirs)
            {
                var subName = subDir.Name;
                bool hasPrefix = subName.Length > 3 && char.IsDigit(subName[0]) && char.IsDigit(subName[1]) && subName[2] == ' ';
                if (hasPrefix)
                    subName = subName[3..];

                mainItem.Children.Add(new FolderTreeItem
                {
                    Name = subName,
                    Position = hasPrefix ? subPos : -1,
                    IsMainFolder = false,
                    HasPrefix = hasPrefix
                });
                if (hasPrefix) subPos++;
            }

            items.Add(mainItem);
        }
        return items;
    }

    private void LoadProjectData()
    {
        TxtName.Text = Project.Name;
        TxtFullName.Text = Project.FullName;
        DpProjectStart.SelectedDate = Project.Timeline.ProjectStart;
        TxtNumberPreview.Text = Project.ProjectNumber;
        CmbStatus.ItemsSource = Enum.GetValues<ProjectStatus>();
        CmbStatus.SelectedItem = Project.Status;

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
        TxtCoordEast.Text = Project.Location.CoordinateEast != 0
            ? Project.Location.CoordinateEast.ToString(CultureInfo.InvariantCulture) : "";
        TxtCoordNorth.Text = Project.Location.CoordinateNorth != 0
            ? Project.Location.CoordinateNorth.ToString(CultureInfo.InvariantCulture) : "";

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
            UpdateFolderPreview();
        }
    }

    // === TreeView selection ===

    private void OnTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        _selectedTreeItem = e.NewValue as FolderTreeItem;
    }

    // === Folder preview ===

    private void UpdateFolderPreview()
    {
        if (TxtFolderPreview is null) return;

        var projectName = !string.IsNullOrEmpty(TxtName?.Text)
            ? TxtName.Text : (Project?.Name ?? "Projektname");
        var number = !string.IsNullOrEmpty(TxtNumberPreview?.Text)
            ? TxtNumberPreview.Text : (Project?.ProjectNumber ?? "YYYYMM");

        var sb = new StringBuilder();
        sb.AppendLine($"{number}_{projectName}/");

        for (int i = 0; i < _folderTreeItems.Count; i++)
        {
            var entry = _folderTreeItems[i];
            var mainPrefix = i == _folderTreeItems.Count - 1 ? "└── " : "├── ";
            var mainIndent = i == _folderTreeItems.Count - 1 ? "    " : "│   ";
            sb.AppendLine($"{mainPrefix}{entry.DisplayName}/");

            if (entry.HasInbox)
                sb.AppendLine($"{mainIndent}└── _Eingang/");

            foreach (var sub in entry.Children)
                sb.AppendLine($"{mainIndent}└── {sub.DisplayName}/");
        }

        TxtFolderPreview.Text = sb.ToString().TrimEnd();
    }

    // === Folder tree buttons ===

    private void RefreshTreePositions()
    {
        for (int i = 0; i < _folderTreeItems.Count; i++)
        {
            _folderTreeItems[i].Position = i;
            int subPos = 0;
            foreach (var sub in _folderTreeItems[i].Children)
            {
                sub.Position = sub.HasPrefix ? subPos : -1;
                if (sub.HasPrefix) subPos++;
            }
        }

        var items = _folderTreeItems.ToList();
        _folderTreeItems.Clear();
        foreach (var item in items)
            _folderTreeItems.Add(item);
        UpdateFolderPreview();
    }

    private static string ShowInputDialog(string title, string label, Window owner)
    {
        var inputWindow = new Window
        {
            Title = title,
            Width = 350,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner,
            ResizeMode = ResizeMode.NoResize,
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D30"))
        };

        var stack = new StackPanel { Margin = new Thickness(15) };
        var lbl = new TextBlock
        {
            Text = label,
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
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        btnOk.Click += (_, _) => { inputWindow.DialogResult = true; inputWindow.Close(); };

        stack.Children.Add(lbl);
        stack.Children.Add(textBox);
        stack.Children.Add(btnOk);
        inputWindow.Content = stack;
        inputWindow.ContentRendered += (_, _) => textBox.Focus();

        return inputWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(textBox.Text)
            ? textBox.Text.Trim() : "";
    }

    private void OnFolderMoveUp(object sender, RoutedEventArgs e)
    {
        if (_selectedTreeItem is null || !_selectedTreeItem.IsMainFolder) return;
        var index = _folderTreeItems.IndexOf(_selectedTreeItem);
        if (index <= 0) return;
        var item = _folderTreeItems[index];
        _folderTreeItems.RemoveAt(index);
        _folderTreeItems.Insert(index - 1, item);
        RefreshTreePositions();
    }

    private void OnFolderMoveDown(object sender, RoutedEventArgs e)
    {
        if (_selectedTreeItem is null || !_selectedTreeItem.IsMainFolder) return;
        var index = _folderTreeItems.IndexOf(_selectedTreeItem);
        if (index < 0 || index >= _folderTreeItems.Count - 1) return;
        var item = _folderTreeItems[index];
        _folderTreeItems.RemoveAt(index);
        _folderTreeItems.Insert(index + 1, item);
        RefreshTreePositions();
    }

    private void OnFolderAddMain(object sender, RoutedEventArgs e)
    {
        var name = ShowInputDialog("Hauptordner hinzufügen", "Ordnername (ohne Nummer):", this);
        if (string.IsNullOrEmpty(name)) return;

        _folderTreeItems.Add(new FolderTreeItem
        {
            Name = name,
            Position = _folderTreeItems.Count,
            IsMainFolder = true,
            HasInbox = false
        });
        RefreshTreePositions();
    }

    private void OnFolderAddSub(object sender, RoutedEventArgs e)
    {
        FolderTreeItem? parent = null;
        if (_selectedTreeItem is not null)
        {
            if (_selectedTreeItem.IsMainFolder)
                parent = _selectedTreeItem;
            else
            {
                foreach (var main in _folderTreeItems)
                    if (main.Children.Contains(_selectedTreeItem)) { parent = main; break; }
            }
        }

        if (parent is null)
        {
            MessageBox.Show("Bitte zuerst einen Hauptordner auswählen.",
                "Unterordner hinzufügen", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var name = ShowInputDialog("Unterordner hinzufügen",
            $"Unterordner-Name für \"{parent.Name}\":", this);
        if (string.IsNullOrEmpty(name)) return;

        parent.Children.Add(new FolderTreeItem
        {
            Name = name,
            IsMainFolder = false,
            HasPrefix = true,
            Position = parent.Children.Count(c => c.HasPrefix)
        });
        RefreshTreePositions();
    }

    private void OnFolderRemove(object sender, RoutedEventArgs e)
    {
        if (_selectedTreeItem is null) return;

        var displayName = _selectedTreeItem.IsMainFolder
            ? $"Hauptordner \"{_selectedTreeItem.Name}\""
            : $"Unterordner \"{_selectedTreeItem.Name}\"";

        if (MessageBox.Show($"{displayName} entfernen?", "Entfernen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        if (_selectedTreeItem.IsMainFolder)
            _folderTreeItems.Remove(_selectedTreeItem);
        else
            foreach (var main in _folderTreeItems)
                if (main.Children.Remove(_selectedTreeItem)) break;

        RefreshTreePositions();
    }

    private void OnFolderToggleInbox(object sender, RoutedEventArgs e)
    {
        if (_selectedTreeItem is null || !_selectedTreeItem.IsMainFolder) return;
        _selectedTreeItem.HasInbox = !_selectedTreeItem.HasInbox;
        RefreshTreePositions();
    }

    private void OnFolderTogglePrefix(object sender, RoutedEventArgs e)
    {
        if (_selectedTreeItem is null || _selectedTreeItem.IsMainFolder)
        {
            MessageBox.Show("Präfix kann nur für Unterordner umgeschaltet werden.",
                "Präfix", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _selectedTreeItem.HasPrefix = !_selectedTreeItem.HasPrefix;
        RefreshTreePositions();
    }

    // === Save / Cancel ===

    private void OnSave(object sender, RoutedEventArgs e)
    {
        Project.Name = TxtName.Text;
        Project.FullName = TxtFullName.Text;
        Project.Timeline.ProjectStart = DpProjectStart.SelectedDate;
        Project.Status = (ProjectStatus)CmbStatus.SelectedItem;
        Project.UpdateProjectNumberFromStart();

        Project.Client.Company = TxtClientCompany.Text;
        Project.Client.ContactPerson = TxtClientContact.Text;
        Project.Client.Phone = TxtClientPhone.Text;
        Project.Client.Email = TxtClientEmail.Text;

        Project.Location.Street = TxtStreet.Text;
        Project.Location.HouseNumber = TxtHouseNumber.Text;
        Project.Location.PostalCode = TxtPostalCode.Text;
        Project.Location.City = TxtCity.Text;

        Project.Location.Municipality = TxtMunicipality.Text;
        Project.Location.District = TxtDistrict.Text;
        Project.Location.State = TxtState.Text;

        Project.Location.CoordinateSystem = TxtCoordSystem.Text;
        if (double.TryParse(TxtCoordEast.Text, CultureInfo.InvariantCulture, out var east))
            Project.Location.CoordinateEast = east;
        if (double.TryParse(TxtCoordNorth.Text, CultureInfo.InvariantCulture, out var north))
            Project.Location.CoordinateNorth = north;

        Project.Location.CadastralKg = TxtCadastralKg.Text;
        Project.Location.CadastralKgName = TxtCadastralKgName.Text;
        Project.Location.CadastralGst = TxtCadastralGst.Text;

        Project.Timeline.ConstructionStart = DpConstructionStart.SelectedDate;
        Project.Timeline.PlannedEnd = DpPlannedEnd.SelectedDate;
        Project.Timeline.ActualEnd = DpActualEnd.SelectedDate;

        Project.Tags = TxtTags.Text;
        Project.Notes = TxtNotes.Text;

        // Build FolderTemplate from TreeView
        FolderTemplate = _folderTreeItems.Select(main => new FolderTemplateEntry
        {
            Name = main.Name,
            HasInbox = main.HasInbox,
            SubFolders = main.Children.Select(sub => new SubFolderEntry
            {
                Name = sub.Name,
                HasPrefix = sub.HasPrefix
            }).ToList()
        }).ToList();

        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
