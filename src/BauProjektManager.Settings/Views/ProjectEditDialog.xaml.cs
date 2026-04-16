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

public partial class ProjectEditDialog : Window
{
    public Project Project { get; private set; }
    public List<FolderTemplateEntry>? FolderTemplate { get; private set; }

    private readonly bool _isNewProject;
    private readonly AppSettingsService _settingsService = new();
    private ObservableCollection<BuildingPart> _buildingParts = [];
    private ObservableCollection<ProjectParticipant> _participants = [];
    private ObservableCollection<ProjectLink> _portalLinks = [];
    private ObservableCollection<ProjectLink> _customLinks = [];
    private FileSystemWatcher? _folderWatcher;
    private bool _isGlobalZeroActive;

    public ProjectEditDialog(Project project) : this(project, null) { }

    public ProjectEditDialog(Project project, List<FolderTemplateEntry>? folderTemplate)
    {
        InitializeComponent();
        Project = project;
        _isNewProject = folderTemplate is not null;

        ProjectFolderTemplate.IsProjectMode = !_isNewProject;

        if (_isNewProject && folderTemplate is not null)
        {
            TxtDialogTitle.Text = "Neues Projekt anlegen";
            ProjectFolderTemplate.LoadFromTemplate(folderTemplate);
        }
        else if (!string.IsNullOrEmpty(project.Paths.Root))
        {
            TxtDialogTitle.Text = "Projekt bearbeiten";
            ProjectFolderTemplate.LoadFromDisk(project.Paths.Root);
            StartFolderWatcher(project.Paths.Root);
        }
        else
        {
            TxtDialogTitle.Text = "Projekt bearbeiten";
            ProjectFolderTemplate.LoadFromTemplate(_settingsService.Load().FolderTemplate);
        }

        ProjectFolderTemplate.PreviewRootName = $"{project.ProjectNumber}_{project.Name}";

        // Globales Nullniveau laden
        _isGlobalZeroActive = project.UseGlobalZeroLevel;
        if (_isGlobalZeroActive)
        {
            TxtGlobalZero.Text = project.GlobalZeroLevel.ToString("F2");
            TxtGlobalZero.Visibility = Visibility.Visible;
            TxtGlobalZeroHint.Visibility = Visibility.Visible;
            UpdateToggleVisual();
        }

        _buildingParts = new ObservableCollection<BuildingPart>(project.BuildingParts);
        DgParts.ItemsSource = _buildingParts;

        // Tab 3: Load participants
        _participants = new ObservableCollection<ProjectParticipant>(project.Participants);
        DgParticipants.ItemsSource = _participants;

        // Tab 4: Load links (split into portals and custom)
        _portalLinks = new ObservableCollection<ProjectLink>(project.Links.Where(l => l.LinkType == "Portal"));
        _customLinks = new ObservableCollection<ProjectLink>(project.Links.Where(l => l.LinkType != "Portal"));
        DgPortals.ItemsSource = _portalLinks;
        DgCustomLinks.ItemsSource = _customLinks;
        RefreshLinkPreview();

        var settings = _settingsService.Load();
        ColLevelName.ItemsSource = settings.LevelNames.Select(l => l.ShortName).ToList();

        LoadDropdowns();
        LoadProjectData();
    }

    private void LoadDropdowns()
    {
        var settings = _settingsService.Load();
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
        var settings = _settingsService.Load();
        var items = new ObservableCollection<string>(settings.ProjectTypes);
        if (ShowSimpleListEditDialog("Projektarten bearbeiten", items))
        {
            var selected = CmbProjectType.SelectedItem as string;
            settings.ProjectTypes = items.ToList();
            _settingsService.Save(settings);
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

    // ═══════════════════════════════════════════
    // TAB 2: BAUWERK
    // ═══════════════════════════════════════════

    private void OnPartSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgParts.SelectedItem is BuildingPart part)
        {
            RecalculateLevels(part);
            DgLevels.ItemsSource = part.Levels;
            TxtLevelsHeader.Text = $"Geschosse: {part.ShortName} ({part.Description})";
            TxtZeroInfo.Text = $"± 0,00 = {part.ZeroLevelAbsolute:F2} m ü.A.";
        }
        else
        {
            DgLevels.ItemsSource = null;
            TxtLevelsHeader.Text = "Geschosse";
            TxtZeroInfo.Text = "";
        }
    }

    private void RecalculateLevels(BuildingPart part)
    {
        var settings = _settingsService.Load();
        var levels = part.Levels;

        int egIndex = levels.FindIndex(l => l.Name.Equals("EG", StringComparison.OrdinalIgnoreCase));
        for (int i = 0; i < levels.Count; i++)
        {
            levels[i].Prefix = egIndex >= 0 ? i - egIndex : i;
            levels[i].Description = BuildingLevel.GetAutoDescription(levels[i].Name, settings.LevelNames);
        }

        for (int i = 0; i < levels.Count; i++)
        {
            if (i < levels.Count - 1)
            {
                levels[i].StoryHeight = Math.Round(levels[i + 1].Fbok - levels[i].Fbok, 3);
                levels[i].RawHeight = Math.Round(levels[i + 1].Rdok - levels[i].Rdok, 3);
                // Deckenstärke = RDOK(darüber) − RDUK(aktuell)
                levels[i].DeckThickness = levels[i].Rduk is { } rduk
                    ? Math.Round(levels[i + 1].Rdok - rduk, 3)
                    : null;
            }
            else
            {
                levels[i].StoryHeight = null;
                levels[i].RawHeight = null;
                levels[i].DeckThickness = null;
            }
        }
    }

    private void RefreshLevelsGrid()
    {
        if (DgParts.SelectedItem is BuildingPart part)
        {
            RecalculateLevels(part);
            DgLevels.ItemsSource = null;
            DgLevels.ItemsSource = part.Levels;
        }
    }

    private void OnLevelCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;
        if (e.Row.Item is not BuildingLevel level) return;

        var colHeader = (e.Column.Header as string) ?? "";

        if (e.EditingElement is TextBox tb && colHeader is "RDOK" or "FBOK" or "RDUK")
        {
            var text = tb.Text.Replace(',', '.');
            if (double.TryParse(text, CultureInfo.InvariantCulture, out var val))
            {
                switch (colHeader)
                {
                    case "RDOK": level.Rdok = val; break;
                    case "FBOK": level.Fbok = val; break;
                    case "RDUK": level.Rduk = val; break;
                }
                tb.Text = val.ToString("F2");
            }
            else if (colHeader == "RDUK" && string.IsNullOrWhiteSpace(tb.Text))
            {
                level.Rduk = null;
            }
        }

        Dispatcher.BeginInvoke(new Action(() => RefreshLevelsGrid()));
    }

    // --- Bauteil CRUD ---

    private void OnAddPart(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Load();

        bool addMore = true;
        while (addMore)
        {
            var part = new BuildingPart();
            if (!ShowPartEditDialog(part, settings, "Bauteil hinzufügen"))
                break;

            part.SortOrder = _buildingParts.Count;
            _buildingParts.Add(part);
            DgParts.SelectedItem = part;

            // Geschoss-Schleife für dieses Bauteil
            AddLevelsLoop(part, settings);

            // Weiteres Bauteil?
            addMore = ShowDarkConfirm("Weiteres Bauteil anlegen?", "Bauteil");
        }
    }

    /// <summary>
    /// Geschoss-Eingabeschleife: Öffnet den Geschoss-Dialog wiederholt
    /// bis der User "Fertig" wählt.
    /// </summary>
    private void AddLevelsLoop(BuildingPart part, AppSettings settings)
    {
        bool addMoreLevels = true;
        while (addMoreLevels)
        {
            string suggestedName;
            if (part.Levels.Count == 0)
                suggestedName = settings.LevelNames.Count > 0 ? settings.LevelNames[0].ShortName : "EG";
            else
                suggestedName = BuildingLevel.GetNextLevelName(part.Levels[^1].Name, settings.LevelNames);

            var suggestedDesc = BuildingLevel.GetAutoDescription(suggestedName, settings.LevelNames);
            var level = new BuildingLevel { Name = suggestedName, Description = suggestedDesc };

            var result = ShowLevelEditDialogWithContinue(level, settings);
            if (result == LevelDialogResult.Cancel)
                break;

            level.SortOrder = part.Levels.Count;
            part.Levels.Add(level);
            RefreshLevelsGrid();

            if (result == LevelDialogResult.Done)
                break;

            // result == LevelDialogResult.AddMore → weiter im Loop
        }
    }

    private enum LevelDialogResult { Cancel, Done, AddMore }

    private void OnEditPart(object sender, RoutedEventArgs e)
    {
        if (DgParts.SelectedItem is not BuildingPart part) return;
        var settings = _settingsService.Load();
        ShowPartEditDialog(part, settings, "Bauteil bearbeiten");
        DgParts.Items.Refresh();
        OnPartSelectionChanged(sender, null!);
    }

    private void OnRemovePart(object sender, RoutedEventArgs e)
    {
        if (DgParts.SelectedItem is not BuildingPart part) return;
        if (MessageBox.Show($"Bauteil \"{part.ShortName}\" entfernen?", "Entfernen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _buildingParts.Remove(part);
            DgLevels.ItemsSource = null;
            TxtLevelsHeader.Text = "Geschosse";
            TxtZeroInfo.Text = "";
        }
    }

    private bool ShowPartEditDialog(BuildingPart part, AppSettings settings, string title)
    {
        var w = new Window
        {
            Title = title,
            Width = 420,
            Height = 240,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = BrushFromHex("#2D2D30")
        };
        // Dark Theme Styles vom Dialog vererben (ComboBox, TextBox etc.)
        foreach (var key in Resources.Keys)
            w.Resources[key] = Resources[key];
        var grid = new Grid { Margin = new Thickness(15) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < 5; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var txtShort = MakeTextBox(part.ShortName, 0, 1);
        var txtDesc = MakeTextBox(part.Description, 1, 1);
        var cmbType = new ComboBox { ItemsSource = settings.BuildingTypes, Margin = new Thickness(0, 3, 0, 3) };
        Grid.SetRow(cmbType, 2); Grid.SetColumn(cmbType, 1);
        if (!string.IsNullOrEmpty(part.BuildingType)) cmbType.SelectedItem = part.BuildingType;
        else if (settings.BuildingTypes.Count > 0) cmbType.SelectedIndex = 0;
        var txtZero = MakeTextBox(part.ZeroLevelAbsolute != 0 ? part.ZeroLevelAbsolute.ToString("F2", CultureInfo.InvariantCulture) : "", 3, 1);

        var btnOk = new Button
        {
            Content = "OK",
            Width = 80,
            Padding = new Thickness(0, 5, 0, 5),
            Margin = new Thickness(0, 10, 0, 0),
            Background = BrushFromHex("#007ACC"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(btnOk, 4); Grid.SetColumn(btnOk, 1);
        btnOk.Click += (_, _) => { w.DialogResult = true; w.Close(); };

        grid.Children.Add(MakeLabel("Kürzel:", 0, 0)); grid.Children.Add(txtShort);
        grid.Children.Add(MakeLabel("Beschreibung:", 1, 0)); grid.Children.Add(txtDesc);
        grid.Children.Add(MakeLabel("Bauwerkstyp:", 2, 0)); grid.Children.Add(cmbType);
        grid.Children.Add(MakeLabel("± 0,00 abs.:", 3, 0)); grid.Children.Add(txtZero);
        grid.Children.Add(btnOk);
        w.Content = grid;

        if (w.ShowDialog() == true)
        {
            part.ShortName = txtShort.Text.Trim();
            part.Description = txtDesc.Text.Trim();
            part.BuildingType = cmbType.SelectedItem as string ?? "";
            var zeroText = txtZero.Text.Replace(',', '.');
            if (double.TryParse(zeroText, CultureInfo.InvariantCulture, out var z)) part.ZeroLevelAbsolute = z;
            return true;
        }
        return false;
    }

    // --- Geschoss CRUD ---

    /// <summary>
    /// + Geschoss: Öffnet Dialog mit intelligentem Vorschlag für das nächste Geschoss.
    /// </summary>
    private void OnAddLevel(object sender, RoutedEventArgs e)
    {
        if (DgParts.SelectedItem is not BuildingPart part)
        {
            MessageBox.Show("Bitte zuerst ein Bauteil auswählen.", "Geschoss", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var settings = _settingsService.Load();

        // Nächstes logisches Geschoss vorschlagen
        string suggestedName;
        if (part.Levels.Count == 0)
            suggestedName = settings.LevelNames.Count > 0 ? settings.LevelNames[0].ShortName : "EG";
        else
            suggestedName = BuildingLevel.GetNextLevelName(part.Levels[^1].Name, settings.LevelNames);

        var suggestedDesc = BuildingLevel.GetAutoDescription(suggestedName, settings.LevelNames);
        var level = new BuildingLevel { Name = suggestedName, Description = suggestedDesc };

        if (ShowLevelEditDialog(level, settings, "Geschoss hinzufügen"))
        {
            level.SortOrder = part.Levels.Count;
            part.Levels.Add(level);
            RefreshLevelsGrid();
        }
    }

    private void OnEditLevel(object sender, RoutedEventArgs e)
    {
        if (DgParts.SelectedItem is not BuildingPart) return;
        if (DgLevels.SelectedItem is not BuildingLevel level) return;
        var settings = _settingsService.Load();
        ShowLevelEditDialog(level, settings, "Geschoss bearbeiten");
        RefreshLevelsGrid();
    }

    private void OnRemoveLevel(object sender, RoutedEventArgs e)
    {
        if (DgParts.SelectedItem is not BuildingPart part) return;
        if (DgLevels.SelectedItem is not BuildingLevel level) return;
        if (MessageBox.Show($"Geschoss \"{level.Name}\" entfernen?", "Entfernen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            part.Levels.Remove(level);
            RefreshLevelsGrid();
        }
    }

    private void OnMoveLevelUp(object sender, RoutedEventArgs e)
    {
        if (DgParts.SelectedItem is not BuildingPart part) return;
        if (DgLevels.SelectedItem is not BuildingLevel level) return;
        var idx = part.Levels.IndexOf(level);
        if (idx <= 0) return;
        part.Levels.RemoveAt(idx);
        part.Levels.Insert(idx - 1, level);
        RefreshLevelsGrid();
        DgLevels.SelectedItem = level;
    }

    private void OnMoveLevelDown(object sender, RoutedEventArgs e)
    {
        if (DgParts.SelectedItem is not BuildingPart part) return;
        if (DgLevels.SelectedItem is not BuildingLevel level) return;
        var idx = part.Levels.IndexOf(level);
        if (idx < 0 || idx >= part.Levels.Count - 1) return;
        part.Levels.RemoveAt(idx);
        part.Levels.Insert(idx + 1, level);
        RefreshLevelsGrid();
        DgLevels.SelectedItem = level;
    }

    /// <summary>
    /// Dialog für Geschoss anlegen/bearbeiten — editierbares Dropdown + Höhenwerte.
    /// </summary>
    private bool ShowLevelEditDialog(BuildingLevel level, AppSettings settings, string title)
    {
        var w = new Window
        {
            Title = title,
            Width = 400,
            Height = 280,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = BrushFromHex("#2D2D30")
        };
        foreach (var key in Resources.Keys)
            w.Resources[key] = Resources[key];

        var grid = new Grid { Margin = new Thickness(15) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < 6; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Geschoss-Dropdown (editierbar, ShortNames)
        var shortNames = settings.LevelNames.Select(l => l.ShortName).ToList();
        var cmbName = new ComboBox
        {
            IsEditable = true,
            ItemsSource = shortNames,
            Text = level.Name,
            Margin = new Thickness(0, 3, 0, 3)
        };
        StyleComboBoxDark(cmbName);
        Grid.SetRow(cmbName, 0); Grid.SetColumn(cmbName, 1);

        // Beschreibung (auto-filled, aber editierbar)
        var txtDesc = MakeTextBox(level.Description, 1, 1);

        // Beschreibung automatisch aktualisieren bei Geschoss-Änderung
        cmbName.SelectionChanged += (_, _) =>
        {
            var selectedName = cmbName.SelectedItem as string ?? cmbName.Text;
            var autoDesc = BuildingLevel.GetAutoDescription(selectedName, settings.LevelNames);
            if (!string.IsNullOrEmpty(autoDesc)) txtDesc.Text = autoDesc;
        };

        var txtRdok = MakeTextBox(level.Rdok != 0 ? level.Rdok.ToString("F2") : "", 2, 1);
        var txtFbok = MakeTextBox(level.Fbok != 0 ? level.Fbok.ToString("F2") : "", 3, 1);
        var txtRduk = MakeTextBox(level.Rduk.HasValue ? level.Rduk.Value.ToString("F2") : "", 4, 1);

        var btnOk = new Button
        {
            Content = "OK",
            Width = 80,
            Padding = new Thickness(0, 5, 0, 5),
            Margin = new Thickness(0, 10, 0, 0),
            Background = BrushFromHex("#007ACC"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(btnOk, 5); Grid.SetColumn(btnOk, 1);
        btnOk.Click += (_, _) => { w.DialogResult = true; w.Close(); };

        grid.Children.Add(MakeLabel("Geschoss:", 0, 0)); grid.Children.Add(cmbName);
        grid.Children.Add(MakeLabel("Beschreibung:", 1, 0)); grid.Children.Add(txtDesc);
        grid.Children.Add(MakeLabel("RDOK:", 2, 0)); grid.Children.Add(txtRdok);
        grid.Children.Add(MakeLabel("FBOK:", 3, 0)); grid.Children.Add(txtFbok);
        grid.Children.Add(MakeLabel("RDUK:", 4, 0)); grid.Children.Add(txtRduk);
        grid.Children.Add(btnOk);
        w.Content = grid;

        if (w.ShowDialog() == true)
        {
            level.Name = (cmbName.SelectedItem as string ?? cmbName.Text).Trim();
            level.Description = string.IsNullOrWhiteSpace(txtDesc.Text)
                ? BuildingLevel.GetAutoDescription(level.Name, settings.LevelNames)
                : txtDesc.Text.Trim();
            if (double.TryParse(txtRdok.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var rdok)) level.Rdok = rdok;
            if (double.TryParse(txtFbok.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var fbok)) level.Fbok = fbok;
            level.Rduk = double.TryParse(txtRduk.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var rduk) ? rduk : null;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Geschoss-Dialog mit 2 Buttons: "+ Geschoss" (speichern + nächstes) und "Fertig" (speichern + schließen).
    /// Wird von AddLevelsLoop aufgerufen.
    /// </summary>
    private LevelDialogResult ShowLevelEditDialogWithContinue(BuildingLevel level, AppSettings settings)
    {
        var result = LevelDialogResult.Cancel;

        var w = new Window
        {
            Title = "Geschoss hinzufügen",
            Width = 400,
            Height = 290,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = BrushFromHex("#2D2D30")
        };
        foreach (var key in Resources.Keys)
            w.Resources[key] = Resources[key];

        var grid = new Grid { Margin = new Thickness(15) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < 7; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var shortNames = settings.LevelNames.Select(l => l.ShortName).ToList();
        var cmbName = new ComboBox
        {
            IsEditable = true,
            ItemsSource = shortNames,
            Text = level.Name,
            Margin = new Thickness(0, 3, 0, 3)
        };
        Grid.SetRow(cmbName, 0); Grid.SetColumn(cmbName, 1);

        var txtDesc = MakeTextBox(level.Description, 1, 1);
        cmbName.SelectionChanged += (_, _) =>
        {
            var selectedName = cmbName.SelectedItem as string ?? cmbName.Text;
            var autoDesc = BuildingLevel.GetAutoDescription(selectedName, settings.LevelNames);
            if (!string.IsNullOrEmpty(autoDesc)) txtDesc.Text = autoDesc;
        };

        var txtRdok = MakeTextBox("", 2, 1);
        var txtFbok = MakeTextBox("", 3, 1);
        var txtRduk = MakeTextBox("", 4, 1);

        // 2 Buttons: + Geschoss und Fertig
        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
        Grid.SetRow(btnPanel, 6); Grid.SetColumn(btnPanel, 0); Grid.SetColumnSpan(btnPanel, 2);

        var btnAddMore = new Button
        {
            Content = "+ Geschoss",
            Padding = new Thickness(12, 5, 12, 5),
            Margin = new Thickness(0, 0, 8, 0),
            Background = BrushFromHex("#007ACC"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        var btnDone = new Button
        {
            Content = "Fertig",
            Padding = new Thickness(12, 5, 12, 5),
            Background = BrushFromHex("#3C3C3C"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        void ApplyValues()
        {
            level.Name = (cmbName.SelectedItem as string ?? cmbName.Text).Trim();
            level.Description = string.IsNullOrWhiteSpace(txtDesc.Text)
                ? BuildingLevel.GetAutoDescription(level.Name, settings.LevelNames)
                : txtDesc.Text.Trim();
            if (double.TryParse(txtRdok.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var rdok)) level.Rdok = rdok;
            if (double.TryParse(txtFbok.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var fbok)) level.Fbok = fbok;
            level.Rduk = double.TryParse(txtRduk.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var rduk) ? rduk : null;
        }

        btnAddMore.Click += (_, _) => { ApplyValues(); result = LevelDialogResult.AddMore; w.DialogResult = true; w.Close(); };
        btnDone.Click += (_, _) => { ApplyValues(); result = LevelDialogResult.Done; w.DialogResult = true; w.Close(); };

        btnPanel.Children.Add(btnAddMore);
        btnPanel.Children.Add(btnDone);

        grid.Children.Add(MakeLabel("Geschoss:", 0, 0)); grid.Children.Add(cmbName);
        grid.Children.Add(MakeLabel("Beschreibung:", 1, 0)); grid.Children.Add(txtDesc);
        grid.Children.Add(MakeLabel("RDOK:", 2, 0)); grid.Children.Add(txtRdok);
        grid.Children.Add(MakeLabel("FBOK:", 3, 0)); grid.Children.Add(txtFbok);
        grid.Children.Add(MakeLabel("RDUK:", 4, 0)); grid.Children.Add(txtRduk);
        grid.Children.Add(btnPanel);
        w.Content = grid;

        return w.ShowDialog() == true ? result : LevelDialogResult.Cancel;
    }

    /// <summary>
    /// ✎ Button: Geschoss-Namensliste bearbeiten (2-spaltig: Kurz + Lang).
    /// </summary>
    private void OnEditLevelNames(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Load();
        var items = new ObservableCollection<LevelNameEntry>(
            settings.LevelNames.Select(l => new LevelNameEntry(l.ShortName, l.LongName)));

        var w = new Window
        {
            Title = "Geschoss-Bezeichnungen bearbeiten",
            Width = 450,
            Height = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = BrushFromHex("#2D2D30")
        };

        var stack = new StackPanel { Margin = new Thickness(15) };

        var dg = new DataGrid
        {
            ItemsSource = items,
            AutoGenerateColumns = false,
            CanUserAddRows = false,
            CanUserResizeRows = false,
            Height = 260,
            Background = BrushFromHex("#1E1E1E"),
            Foreground = BrushFromHex("#CCCCCC"),
            BorderBrush = BrushFromHex("#3E3E42"),
            RowBackground = BrushFromHex("#1E1E1E"),
            AlternatingRowBackground = BrushFromHex("#1E1E1E"),
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            HorizontalGridLinesBrush = BrushFromHex("#3E3E42"),
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        dg.ColumnHeaderStyle = new Style(typeof(DataGridColumnHeader));
        dg.ColumnHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, BrushFromHex("#007ACC")));
        dg.ColumnHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, System.Windows.Media.Brushes.White));
        dg.ColumnHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.PaddingProperty, new Thickness(6, 4, 6, 4)));
        dg.Columns.Add(new DataGridTextColumn { Header = "Kurzbezeichnung", Binding = new System.Windows.Data.Binding("ShortName"), Width = new DataGridLength(120) });
        dg.Columns.Add(new DataGridTextColumn { Header = "Langbezeichnung", Binding = new System.Windows.Data.Binding("LongName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });

        var bp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 8) };
        var btnAdd = new Button
        {
            Content = "Hinzufügen",
            Padding = new Thickness(10, 4, 10, 4),
            Margin = new Thickness(0, 0, 5, 0),
            Background = BrushFromHex("#007ACC"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btnAdd.Click += (_, _) =>
        {
            items.Add(new LevelNameEntry("NEU", "Neues Geschoss"));
            dg.ScrollIntoView(items[^1]);
        };
        var btnRem = new Button
        {
            Content = "Entfernen",
            Padding = new Thickness(10, 4, 10, 4),
            Background = BrushFromHex("#C62828"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btnRem.Click += (_, _) => { if (dg.SelectedIndex >= 0) items.RemoveAt(dg.SelectedIndex); };
        bp.Children.Add(btnAdd); bp.Children.Add(btnRem);

        var btnOk = new Button
        {
            Content = "Übernehmen",
            Padding = new Thickness(15, 5, 15, 5),
            Background = BrushFromHex("#007ACC"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        btnOk.Click += (_, _) => { w.DialogResult = true; w.Close(); };

        stack.Children.Add(dg);
        stack.Children.Add(bp);
        stack.Children.Add(btnOk);
        w.Content = stack;

        if (w.ShowDialog() == true)
        {
            settings.LevelNames = items.ToList();
            _settingsService.Save(settings);
            ColLevelName.ItemsSource = settings.LevelNames.Select(l => l.ShortName).ToList();
        }
    }

    // ═══════════════════════════════════════════
    // TAB 3: BETEILIGTE
    // ═══════════════════════════════════════════

    private void OnAddParticipant(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Load();
        var p = new ProjectParticipant();
        if (ShowParticipantEditDialog(p, settings, "Beteiligten hinzufügen"))
        {
            p.SortOrder = _participants.Count;
            _participants.Add(p);
        }
    }

    private void OnEditParticipant(object sender, RoutedEventArgs e)
    {
        if (DgParticipants.SelectedItem is not ProjectParticipant p) return;
        var settings = _settingsService.Load();
        ShowParticipantEditDialog(p, settings, "Beteiligten bearbeiten");
        DgParticipants.Items.Refresh();
    }

    private void OnRemoveParticipant(object sender, RoutedEventArgs e)
    {
        if (DgParticipants.SelectedItem is not ProjectParticipant p) return;
        if (MessageBox.Show($"Beteiligten \"{p.Company}\" entfernen?", "Entfernen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            _participants.Remove(p);
    }

    private void OnMoveParticipantUp(object sender, RoutedEventArgs e)
    {
        if (DgParticipants.SelectedItem is not ProjectParticipant p) return;
        var idx = _participants.IndexOf(p);
        if (idx <= 0) return;
        _participants.RemoveAt(idx);
        _participants.Insert(idx - 1, p);
        DgParticipants.SelectedItem = p;
    }

    private void OnMoveParticipantDown(object sender, RoutedEventArgs e)
    {
        if (DgParticipants.SelectedItem is not ProjectParticipant p) return;
        var idx = _participants.IndexOf(p);
        if (idx < 0 || idx >= _participants.Count - 1) return;
        _participants.RemoveAt(idx);
        _participants.Insert(idx + 1, p);
        DgParticipants.SelectedItem = p;
    }

    private void OnEditParticipantRoles(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Load();
        var items = new ObservableCollection<string>(settings.ParticipantRoles);
        if (ShowSimpleListEditDialog("Rollen bearbeiten", items))
        {
            settings.ParticipantRoles = items.ToList();
            _settingsService.Save(settings);
        }
    }

    private void OnImportParticipantsJson(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON Dateien|*.json",
            Title = "Beteiligte aus JSON importieren"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var json = File.ReadAllText(dlg.FileName);
            var imported = System.Text.Json.JsonSerializer.Deserialize<List<ProjectParticipant>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (imported is null || imported.Count == 0)
            {
                MessageBox.Show("Keine Beteiligten in der JSON-Datei gefunden.", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            foreach (var p in imported)
            {
                p.Id = string.Empty;
                p.SortOrder = _participants.Count;
                _participants.Add(p);
            }
            MessageBox.Show($"{imported.Count} Beteiligte importiert.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Import: {ex.Message}", "Import", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool ShowParticipantEditDialog(ProjectParticipant p, AppSettings settings, string title)
    {
        var w = new Window
        {
            Title = title, Width = 450, Height = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this, ResizeMode = ResizeMode.NoResize,
            Background = BrushFromHex("#2D2D30")
        };
        var grid = new Grid { Margin = new Thickness(15) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < 6; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var cmbRole = new ComboBox { IsEditable = true, ItemsSource = settings.ParticipantRoles, Text = p.Role, Margin = new Thickness(0, 3, 0, 3) };
        Grid.SetRow(cmbRole, 0); Grid.SetColumn(cmbRole, 1);
        var txtCompany = MakeTextBox(p.Company, 1, 1);
        var txtContact = MakeTextBox(p.ContactPerson, 2, 1);
        var txtPhone = MakeTextBox(p.Phone, 3, 1);
        var txtEmail = MakeTextBox(p.Email, 4, 1);

        var btnOk = new Button
        {
            Content = "OK", Width = 80, Padding = new Thickness(0, 5, 0, 5), Margin = new Thickness(0, 10, 0, 0),
            Background = BrushFromHex("#007ACC"), Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(btnOk, 5); Grid.SetColumn(btnOk, 1);
        btnOk.Click += (_, _) => { w.DialogResult = true; w.Close(); };

        grid.Children.Add(MakeLabel("Rolle:", 0, 0)); grid.Children.Add(cmbRole);
        grid.Children.Add(MakeLabel("Firma:", 1, 0)); grid.Children.Add(txtCompany);
        grid.Children.Add(MakeLabel("Kontaktperson:", 2, 0)); grid.Children.Add(txtContact);
        grid.Children.Add(MakeLabel("Telefon:", 3, 0)); grid.Children.Add(txtPhone);
        grid.Children.Add(MakeLabel("Email:", 4, 0)); grid.Children.Add(txtEmail);
        grid.Children.Add(btnOk);
        w.Content = grid;

        if (w.ShowDialog() == true)
        {
            p.Role = (cmbRole.SelectedItem as string ?? cmbRole.Text).Trim();
            p.Company = txtCompany.Text.Trim();
            p.ContactPerson = txtContact.Text.Trim();
            p.Phone = txtPhone.Text.Trim();
            p.Email = txtEmail.Text.Trim();
            return true;
        }
        return false;
    }

    // ═══════════════════════════════════════════
    // TAB 4: PORTALE + LINKS
    // ═══════════════════════════════════════════

    private void OnAddPortal(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Load();
        var link = new ProjectLink { LinkType = "Portal" };
        if (ShowLinkEditDialog(link, settings.PortalTypes, "Portal hinzufügen", true))
        {
            link.SortOrder = _portalLinks.Count;
            _portalLinks.Add(link);
            RefreshLinkPreview();
        }
    }

    private void OnEditPortal(object sender, RoutedEventArgs e)
    {
        if (DgPortals.SelectedItem is not ProjectLink link) return;
        var settings = _settingsService.Load();
        ShowLinkEditDialog(link, settings.PortalTypes, "Portal bearbeiten", true);
        DgPortals.Items.Refresh();
        RefreshLinkPreview();
    }

    private void OnRemovePortal(object sender, RoutedEventArgs e)
    {
        if (DgPortals.SelectedItem is not ProjectLink link) return;
        if (MessageBox.Show($"Portal \"{link.Name}\" entfernen?", "Entfernen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _portalLinks.Remove(link);
            RefreshLinkPreview();
        }
    }

    private void OnOpenPortal(object sender, RoutedEventArgs e)
    {
        if (DgPortals.SelectedItem is not ProjectLink link || !link.IsConfigured) return;
        OpenUrlInBrowser(link.Url);
    }

    private void OnAddCustomLink(object sender, RoutedEventArgs e)
    {
        var link = new ProjectLink { LinkType = "Custom" };
        if (ShowLinkEditDialog(link, null, "Link hinzufügen", false))
        {
            link.SortOrder = _customLinks.Count;
            _customLinks.Add(link);
            RefreshLinkPreview();
        }
    }

    private void OnEditCustomLink(object sender, RoutedEventArgs e)
    {
        if (DgCustomLinks.SelectedItem is not ProjectLink link) return;
        ShowLinkEditDialog(link, null, "Link bearbeiten", false);
        DgCustomLinks.Items.Refresh();
        RefreshLinkPreview();
    }

    private void OnRemoveCustomLink(object sender, RoutedEventArgs e)
    {
        if (DgCustomLinks.SelectedItem is not ProjectLink link) return;
        if (MessageBox.Show($"Link \"{link.Name}\" entfernen?", "Entfernen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _customLinks.Remove(link);
            RefreshLinkPreview();
        }
    }

    private void OnOpenCustomLink(object sender, RoutedEventArgs e)
    {
        if (DgCustomLinks.SelectedItem is not ProjectLink link || !link.IsConfigured) return;
        OpenUrlInBrowser(link.Url);
    }

    private void OnEditPortalTypes(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Load();
        var items = new ObservableCollection<string>(settings.PortalTypes);
        if (ShowSimpleListEditDialog("Portal-Typen bearbeiten", items))
        {
            settings.PortalTypes = items.ToList();
            _settingsService.Save(settings);
        }
    }

    private bool ShowLinkEditDialog(ProjectLink link, List<string>? nameOptions, string title, bool isPortal)
    {
        var w = new Window
        {
            Title = title, Width = 450, Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this, ResizeMode = ResizeMode.NoResize,
            Background = BrushFromHex("#2D2D30")
        };
        var grid = new Grid { Margin = new Thickness(15) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < 3; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Name: ComboBox für Portale, TextBox für eigene Links
        System.Windows.Controls.Control nameControl;
        if (isPortal && nameOptions is not null)
        {
            var cmb = new ComboBox { IsEditable = true, ItemsSource = nameOptions, Text = link.Name, Margin = new Thickness(0, 3, 0, 3) };
            Grid.SetRow(cmb, 0); Grid.SetColumn(cmb, 1);
            nameControl = cmb;
        }
        else
        {
            var txt = MakeTextBox(link.Name, 0, 1);
            nameControl = txt;
        }

        var txtUrl = MakeTextBox(link.Url, 1, 1);

        var btnOk = new Button
        {
            Content = "OK", Width = 80, Padding = new Thickness(0, 5, 0, 5), Margin = new Thickness(0, 10, 0, 0),
            Background = BrushFromHex("#007ACC"), Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(btnOk, 2); Grid.SetColumn(btnOk, 1);
        btnOk.Click += (_, _) => { w.DialogResult = true; w.Close(); };

        grid.Children.Add(MakeLabel(isPortal ? "Portal:" : "Bezeichnung:", 0, 0));
        grid.Children.Add(nameControl);
        grid.Children.Add(MakeLabel("URL:", 1, 0));
        grid.Children.Add(txtUrl);
        grid.Children.Add(btnOk);
        w.Content = grid;

        if (w.ShowDialog() == true)
        {
            if (nameControl is ComboBox cmb2)
                link.Name = (cmb2.SelectedItem as string ?? cmb2.Text).Trim();
            else if (nameControl is TextBox txt2)
                link.Name = txt2.Text.Trim();
            link.Url = txtUrl.Text.Trim();
            return true;
        }
        return false;
    }

    private void RefreshLinkPreview()
    {
        if (WpLinkPreview is null) return;
        WpLinkPreview.Children.Clear();

        foreach (var link in _portalLinks.Where(l => l.IsConfigured))
        {
            var btn = new Button
            {
                Content = link.Name, Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 6, 6),
                Background = BrushFromHex("#005A9E"), Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, FontSize = 12
            };
            btn.Click += (_, _) => OpenUrlInBrowser(link.Url);
            WpLinkPreview.Children.Add(btn);
        }

        foreach (var link in _customLinks.Where(l => l.IsConfigured))
        {
            var btn = new Button
            {
                Content = link.Name, Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 6, 6),
                Background = BrushFromHex("#3E3E42"), Foreground = BrushFromHex("#CCCCCC"),
                BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, FontSize = 12
            };
            btn.Click += (_, _) => OpenUrlInBrowser(link.Url);
            WpLinkPreview.Children.Add(btn);
        }

        if (WpLinkPreview.Children.Count == 0)
        {
            WpLinkPreview.Children.Add(new TextBlock
            {
                Text = "Noch keine Links konfiguriert", Foreground = BrushFromHex("#555555"),
                FontSize = 11, FontStyle = System.Windows.FontStyles.Italic
            });
        }
    }

    private static void OpenUrlInBrowser(string url)
    {
        try
        {
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url, UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Link konnte nicht geöffnet werden: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ═══════════════════════════════════════════
    // GLOBALES NULLNIVEAU
    // ═══════════════════════════════════════════

    private void OnToggleGlobalZero(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _isGlobalZeroActive = !_isGlobalZeroActive;
        UpdateToggleVisual();

        TxtGlobalZero.Visibility = _isGlobalZeroActive ? Visibility.Visible : Visibility.Collapsed;
        TxtGlobalZeroHint.Visibility = _isGlobalZeroActive ? Visibility.Visible : Visibility.Collapsed;

        if (_isGlobalZeroActive && double.TryParse(TxtGlobalZero.Text.Replace(',', '.'),
            CultureInfo.InvariantCulture, out var val))
        {
            foreach (var part in _buildingParts)
                part.ZeroLevelAbsolute = val;
            DgParts.Items.Refresh();
        }
    }

    private void UpdateToggleVisual()
    {
        if (_isGlobalZeroActive)
        {
            ToggleTrack.Background = BrushFromHex("#007ACC");
            ToggleKnob.HorizontalAlignment = HorizontalAlignment.Right;
            ToggleKnob.Margin = new Thickness(0, 0, 2, 0);
        }
        else
        {
            ToggleTrack.Background = BrushFromHex("#555555");
            ToggleKnob.HorizontalAlignment = HorizontalAlignment.Left;
            ToggleKnob.Margin = new Thickness(2, 0, 0, 0);
        }
    }

    private void OnGlobalZeroValueChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isGlobalZeroActive) return;
        if (double.TryParse(TxtGlobalZero.Text.Replace(',', '.'),
            CultureInfo.InvariantCulture, out var val))
        {
            foreach (var part in _buildingParts)
                part.ZeroLevelAbsolute = val;
            DgParts.Items.Refresh();
        }
    }

    // ═══════════════════════════════════════════
    // SAVE / CANCEL
    // ═══════════════════════════════════════════

    private void OnSave(object sender, RoutedEventArgs e)
    {
        Project.Name = TxtName.Text;
        Project.FullName = TxtFullName.Text;
        Project.Timeline.ProjectStart = DpProjectStart.SelectedDate;
        Project.Status = (ProjectStatus)CmbStatus.SelectedItem;
        Project.ProjectType = CmbProjectType.SelectedItem as string ?? "";
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
        if (double.TryParse(TxtCoordEast.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var east)) Project.Location.CoordinateEast = east;
        if (double.TryParse(TxtCoordNorth.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var north)) Project.Location.CoordinateNorth = north;
        Project.Location.CadastralKg = TxtCadastralKg.Text;
        Project.Location.CadastralKgName = TxtCadastralKgName.Text;
        Project.Location.CadastralGst = TxtCadastralGst.Text;
        Project.Timeline.ConstructionStart = DpConstructionStart.SelectedDate;
        Project.Timeline.PlannedEnd = DpPlannedEnd.SelectedDate;
        Project.Timeline.ActualEnd = DpActualEnd.SelectedDate;
        Project.Tags = TxtTags.Text;
        Project.Notes = TxtNotes.Text;
        Project.BuildingParts = _buildingParts.ToList();
        Project.Participants = _participants.ToList();
        Project.Links = _portalLinks.Concat(_customLinks).ToList();

        Project.UseGlobalZeroLevel = _isGlobalZeroActive;
        if (Project.UseGlobalZeroLevel && double.TryParse(TxtGlobalZero.Text.Replace(',', '.'),
            CultureInfo.InvariantCulture, out var gz))
            Project.GlobalZeroLevel = gz;

        FolderTemplate = ProjectFolderTemplate.ToTemplate();

        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    // ── FileSystemWatcher für Live-Ordnerstruktur ────────────
    private void StartFolderWatcher(string rootPath)
    {
        if (!Directory.Exists(rootPath)) return;

        _folderWatcher = new FileSystemWatcher(rootPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.DirectoryName,
            EnableRaisingEvents = true
        };

        _folderWatcher.Created += OnFolderChanged;
        _folderWatcher.Deleted += OnFolderChanged;
        _folderWatcher.Renamed += OnFolderChanged;

        Closed += (_, _) =>
        {
            _folderWatcher.EnableRaisingEvents = false;
            _folderWatcher.Dispose();
            _folderWatcher = null;
        };
    }

    private void OnFolderChanged(object sender, FileSystemEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            ProjectFolderTemplate.LoadFromDisk(Project.Paths.Root);
        });
    }

    // ═══════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════

    private string ShowSmallInputDialog(string title, string label)
    {
        var w = new Window { Title = title, Width = 350, Height = 150, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, ResizeMode = ResizeMode.NoResize, Background = BrushFromHex("#2D2D30") };
        var stack = new StackPanel { Margin = new Thickness(15) };
        var lbl = new TextBlock { Text = label, Foreground = BrushFromHex("#CCCCCC"), Margin = new Thickness(0, 0, 0, 5) };
        var tb = new TextBox { Background = BrushFromHex("#1E1E1E"), Foreground = BrushFromHex("#CCCCCC"), BorderBrush = BrushFromHex("#3E3E42"), Padding = new Thickness(5, 3, 5, 3), Margin = new Thickness(0, 0, 0, 10) };
        var btn = new Button { Content = "OK", Width = 80, Padding = new Thickness(0, 5, 0, 5), Background = BrushFromHex("#007ACC"), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Right };
        btn.Click += (_, _) => { w.DialogResult = true; w.Close(); };
        stack.Children.Add(lbl); stack.Children.Add(tb); stack.Children.Add(btn);
        w.Content = stack; w.ContentRendered += (_, _) => tb.Focus();
        return w.ShowDialog() == true && !string.IsNullOrWhiteSpace(tb.Text) ? tb.Text.Trim() : "";
    }

    /// <summary>
    /// Einfache String-Liste bearbeiten (für Projektarten etc.).
    /// </summary>
    private bool ShowSimpleListEditDialog(string title, ObservableCollection<string> items)
    {
        var w = new Window { Title = title, Width = 350, Height = 380, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, ResizeMode = ResizeMode.NoResize, Background = BrushFromHex("#2D2D30"), SizeToContent = SizeToContent.Height };
        var stack = new StackPanel { Margin = new Thickness(15) };
        var lb = new ListBox { ItemsSource = items, Background = BrushFromHex("#1E1E1E"), Foreground = BrushFromHex("#CCCCCC"), BorderBrush = BrushFromHex("#3E3E42"), Height = 200, Margin = new Thickness(0, 0, 0, 8) };
        var bp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        var btnAdd = new Button { Content = "Hinzufügen", Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 0, 5, 0), Background = BrushFromHex("#007ACC"), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
        btnAdd.Click += (_, _) => { var n = ShowSmallInputDialog("Hinzufügen", "Name:"); if (!string.IsNullOrEmpty(n)) items.Add(n); };
        var btnRem = new Button { Content = "Entfernen", Padding = new Thickness(10, 4, 10, 4), Background = BrushFromHex("#C62828"), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
        btnRem.Click += (_, _) => { if (lb.SelectedIndex >= 0) items.RemoveAt(lb.SelectedIndex); };
        bp.Children.Add(btnAdd); bp.Children.Add(btnRem);
        var btnOk = new Button { Content = "Übernehmen", Padding = new Thickness(15, 5, 15, 5), Background = BrushFromHex("#007ACC"), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Right };
        btnOk.Click += (_, _) => { w.DialogResult = true; w.Close(); };
        stack.Children.Add(lb); stack.Children.Add(bp); stack.Children.Add(btnOk);
        w.Content = stack;
        return w.ShowDialog() == true;
    }

    private static System.Windows.Media.SolidColorBrush BrushFromHex(string hex)
        => new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));

    private static TextBlock MakeLabel(string text, int row, int col)
    {
        var tb = new TextBlock { Text = text, Foreground = BrushFromHex("#CCCCCC"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 3, 0, 3) };
        Grid.SetRow(tb, row); Grid.SetColumn(tb, col);
        return tb;
    }

    private static TextBox MakeTextBox(string text, int row, int col)
    {
        var tb = new TextBox { Text = text, Background = BrushFromHex("#1E1E1E"), Foreground = BrushFromHex("#CCCCCC"), BorderBrush = BrushFromHex("#3E3E42"), Padding = new Thickness(5, 3, 5, 3), Margin = new Thickness(0, 3, 0, 3) };
        Grid.SetRow(tb, row); Grid.SetColumn(tb, col);
        return tb;
    }

    private static void StyleComboBoxDark(ComboBox cmb)
    {
        // Nicht mehr nötig — Styles werden über Window.Resources vererbt
    }

    private bool ShowDarkConfirm(string message, string title)
    {
        var w = new Window
        {
            Title = title, Width = 360, Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this, ResizeMode = ResizeMode.NoResize,
            Background = BrushFromHex("#2D2D30")
        };
        var sp = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };
        sp.Children.Add(new TextBlock { Text = message, Foreground = BrushFromHex("#CCCCCC"), FontSize = 14, Margin = new Thickness(0, 0, 0, 16) });
        var bp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        bool result = false;
        var btnYes = new Button { Content = "Ja", Width = 80, Padding = new Thickness(0, 5, 0, 5), Margin = new Thickness(0, 0, 8, 0), Background = BrushFromHex("#007ACC"), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
        var btnNo = new Button { Content = "Nein", Width = 80, Padding = new Thickness(0, 5, 0, 5), Background = BrushFromHex("#3C3C3C"), Foreground = BrushFromHex("#CCCCCC"), BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
        btnYes.Click += (_, _) => { result = true; w.DialogResult = true; w.Close(); };
        btnNo.Click += (_, _) => { w.DialogResult = false; w.Close(); };
        bp.Children.Add(btnYes); bp.Children.Add(btnNo);
        sp.Children.Add(bp);
        w.Content = sp;
        w.ShowDialog();
        return result;
    }
}
