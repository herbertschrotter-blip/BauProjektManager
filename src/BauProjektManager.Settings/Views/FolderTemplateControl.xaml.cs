using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BauProjektManager.Domain.Models;
using Serilog;

namespace BauProjektManager.Settings.Views;

/// <summary>
/// Gemeinsame Ordnerstruktur-Komponente.
/// Wird sowohl in SettingsView (globales Template) als auch
/// im ProjectEditDialog (pro Projekt) verwendet.
/// </summary>
public partial class FolderTemplateControl : UserControl
{
    private ObservableCollection<FolderTreeItem> _folderTreeItems = [];
    private FolderTreeItem? _selectedItem;

    /// <summary>Wird ausgelöst wenn sich die Ordnerstruktur ändert.</summary>
    public event Action? TemplateChanged;

    /// <summary>Optionaler Projekt-Root-Name für die Vorschau (z.B. "202604_Projektname").</summary>
    public string PreviewRootName { get; set; } = string.Empty;

    /// <summary>Wenn true, ist Löschen deaktiviert (Projekt-Bearbeitungsmodus).</summary>
    public bool IsProjectMode { get; set; }

    public FolderTemplateControl()
    {
        InitializeComponent();
    }

    // ── Öffentliche API ──────────────────────────────────────

    /// <summary>Baut den Tree aus einer Liste von FolderTemplateEntry.</summary>
    public void LoadFromTemplate(List<FolderTemplateEntry> template)
    {
        _folderTreeItems.Clear();
        int mainPos = 0;

        foreach (var entry in template)
        {
            var mainItem = new FolderTreeItem
            {
                Name = entry.Name,
                Position = mainPos++,
                IsMainFolder = true,
                HasInbox = entry.HasInbox
            };

            LoadSubFolders(mainItem.Children, entry.SubFolders);
            _folderTreeItems.Add(mainItem);
        }

        TvFolders.ItemsSource = _folderTreeItems;
        UpdatePreview();
        Log.Debug("FolderTemplateControl loaded with {Count} main folders", _folderTreeItems.Count);
        UpdateButtonStates();
    }

    private void LoadSubFolders(ObservableCollection<FolderTreeItem> target, List<SubFolderEntry> subs)
    {
        int subPos = 0;
        foreach (var sub in subs)
        {
            var subItem = new FolderTreeItem
            {
                Name = sub.Name,
                Position = sub.HasPrefix ? subPos++ : -1,
                IsMainFolder = false,
                HasPrefix = sub.HasPrefix
            };
            LoadSubFolders(subItem.Children, sub.SubFolders);
            target.Add(subItem);
        }
    }

    /// <summary>Baut den Tree aus einem tatsächlichen Ordner auf der Platte.</summary>
    public void LoadFromDisk(string projectFolderPath)
    {
        _folderTreeItems.Clear();

        if (!Directory.Exists(projectFolderPath))
        {
            TvFolders.ItemsSource = _folderTreeItems;
            return;
        }

        int mainPos = 0;
        foreach (var dir in Directory.GetDirectories(projectFolderPath).OrderBy(d => d))
        {
            var dirName = Path.GetFileName(dir);
            var cleanName = StripNumericPrefix(dirName);

            var mainItem = new FolderTreeItem
            {
                Name = cleanName,
                Position = mainPos++,
                IsMainFolder = true,
                IsExisting = true,
                HasInbox = Directory.Exists(Path.Combine(dir, "_Eingang"))
            };

            LoadSubFoldersFromDisk(mainItem.Children, dir);
            _folderTreeItems.Add(mainItem);
        }

        TvFolders.ItemsSource = _folderTreeItems;
        UpdatePreview();
        Log.Debug("FolderTemplateControl loaded from disk: {Path}", projectFolderPath);
        UpdateButtonStates();
    }

    private void LoadSubFoldersFromDisk(ObservableCollection<FolderTreeItem> target, string parentPath)
    {
        int subPos = 0;
        foreach (var subDir in Directory.GetDirectories(parentPath).OrderBy(d => d))
        {
            var subName = Path.GetFileName(subDir);
            if (subName == "_Eingang") continue;

            var cleanSubName = StripNumericPrefix(subName);
            var subHasPrefix = subName != cleanSubName;

            var subItem = new FolderTreeItem
            {
                Name = cleanSubName,
                Position = subHasPrefix ? subPos++ : -1,
                IsMainFolder = false,
                IsExisting = true,
                HasPrefix = subHasPrefix
            };

            LoadSubFoldersFromDisk(subItem.Children, subDir);
            target.Add(subItem);
        }
    }

    /// <summary>Gibt die aktuelle Struktur als FolderTemplateEntry-Liste zurück.</summary>
    public List<FolderTemplateEntry> ToTemplate()
    {
        var result = new List<FolderTemplateEntry>();
        foreach (var main in _folderTreeItems)
        {
            var entry = new FolderTemplateEntry
            {
                Name = main.Name,
                HasInbox = main.HasInbox,
                SubFolders = ConvertChildrenToSubFolders(main.Children)
            };
            result.Add(entry);
        }
        return result;
    }

    private List<SubFolderEntry> ConvertChildrenToSubFolders(ObservableCollection<FolderTreeItem> children)
    {
        return children.Select(sub => new SubFolderEntry
        {
            Name = sub.Name,
            HasPrefix = sub.HasPrefix,
            SubFolders = ConvertChildrenToSubFolders(sub.Children)
        }).ToList();
    }

    public void UpdateButtonStates()
    {
        if (BtnRemove != null)
        {
            BtnRemove.IsEnabled = !IsProjectMode;
            BtnRemove.ToolTip = IsProjectMode
                ? "Bestehende Ordner können hier nicht gelöscht werden"
                : "Löschen";
            BtnRemove.Opacity = IsProjectMode ? 0.4 : 1.0;
        }
    }

    // ── Interne Logik ────────────────────────────────────────

    private void RefreshTreePositions()
    {
        int mainPos = 0;
        foreach (var main in _folderTreeItems)
        {
            main.Position = mainPos++;
            RefreshChildPositions(main.Children);
        }

        var items = _folderTreeItems.ToList();
        _folderTreeItems.Clear();
        foreach (var item in items)
            _folderTreeItems.Add(item);

        UpdatePreview();
        TemplateChanged?.Invoke();
    }

    private void RefreshChildPositions(ObservableCollection<FolderTreeItem> children)
    {
        int subPos = 0;
        foreach (var sub in children)
        {
            sub.Position = sub.HasPrefix ? subPos++ : -1;
            if (sub.Children.Count > 0)
                RefreshChildPositions(sub.Children);
        }
    }

    private void UpdatePreview()
    {
        var sb = new StringBuilder();
        var rootName = string.IsNullOrEmpty(PreviewRootName) ? "Projekt" : PreviewRootName;
        sb.AppendLine($"{rootName}/");
        AppendPreviewChildren(sb, _folderTreeItems, "");
        TxtPreview.Text = sb.ToString().TrimEnd();
    }

    private void AppendPreviewChildren(StringBuilder sb, ObservableCollection<FolderTreeItem> items, string indent)
    {
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var isLast = i == items.Count - 1;
            var connector = isLast ? "└── " : "├── ";
            var childIndent = indent + (isLast ? "    " : "│   ");

            sb.AppendLine($"{indent}{connector}{item.DisplayName}/");

            if (item.HasInbox)
                sb.AppendLine($"{childIndent}├── _Eingang/");

            if (item.Children.Count > 0)
                AppendPreviewChildren(sb, item.Children, childIndent);
        }
    }

    private static string StripNumericPrefix(string name)
    {
        if (name.Length > 3 && char.IsDigit(name[0]) && char.IsDigit(name[1]) && name[2] == ' ')
            return name[3..];
        return name;
    }

    private string? ShowInputDialog(string title, string prompt)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 350,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            ResizeMode = ResizeMode.NoResize,
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E))
        };

        var sp = new StackPanel { Margin = new Thickness(16) };
        var lbl = new TextBlock
        {
            Text = prompt,
            Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
            Margin = new Thickness(0, 0, 0, 8)
        };
        var txt = new TextBox
        {
            Background = new SolidColorBrush(Color.FromRgb(0x3C, 0x3C, 0x3C)),
            Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x3C, 0x3C, 0x3C)),
            Padding = new Thickness(4),
            FontSize = 13
        };
        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
        var btnOk = new Button { Content = "OK", Width = 75, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var btnCancel = new Button { Content = "Abbrechen", Width = 75, IsCancel = true };

        string? result = null;
        btnOk.Click += (_, _) => { result = txt.Text; dialog.DialogResult = true; };

        btnPanel.Children.Add(btnOk);
        btnPanel.Children.Add(btnCancel);
        sp.Children.Add(lbl);
        sp.Children.Add(txt);
        sp.Children.Add(btnPanel);
        dialog.Content = sp;

        txt.Loaded += (_, _) => txt.Focus();

        return dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(result) ? result.Trim() : null;
    }

    // ── Event Handlers ───────────────────────────────────────

    private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        _selectedItem = e.NewValue as FolderTreeItem;
    }

    private void OnMoveUp(object sender, RoutedEventArgs e)
    {
        if (_selectedItem is not { IsMainFolder: true }) return;
        var idx = _folderTreeItems.IndexOf(_selectedItem);
        if (idx <= 0) return;
        _folderTreeItems.Move(idx, idx - 1);
        RefreshTreePositions();
    }

    private void OnMoveDown(object sender, RoutedEventArgs e)
    {
        if (_selectedItem is not { IsMainFolder: true }) return;
        var idx = _folderTreeItems.IndexOf(_selectedItem);
        if (idx < 0 || idx >= _folderTreeItems.Count - 1) return;
        _folderTreeItems.Move(idx, idx + 1);
        RefreshTreePositions();
    }

    private void OnAddMain(object sender, RoutedEventArgs e)
    {
        var name = ShowInputDialog("Hauptordner", "Name des neuen Hauptordners:");
        if (name == null) return;

        _folderTreeItems.Add(new FolderTreeItem
        {
            Name = name,
            Position = _folderTreeItems.Count,
            IsMainFolder = true
        });
        RefreshTreePositions();
    }

    private void OnAddSub(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null)
        {
            MessageBox.Show("Bitte einen Ordner auswählen.", "Hinweis",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var name = ShowInputDialog("Unterordner", $"Name des neuen Unterordners in '{_selectedItem.Name}':");
        if (name == null) return;

        _selectedItem.Children.Add(new FolderTreeItem
        {
            Name = name,
            Position = _selectedItem.Children.Count,
            IsMainFolder = false,
            HasPrefix = true
        });
        RefreshTreePositions();
    }

    private void OnRemove(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null) return;

        if (_selectedItem.IsExisting)
        {
            MessageBox.Show(
                $"Der Ordner '{_selectedItem.Name}' existiert bereits auf dem Laufwerk und kann hier nicht gelöscht werden.\n\nBestehende Ordner können nur im Explorer gelöscht werden.",
                "Löschen nicht möglich",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var msg = _selectedItem.IsMainFolder
            ? $"Hauptordner '{_selectedItem.Name}' und alle Unterordner löschen?"
            : $"Unterordner '{_selectedItem.Name}' löschen?";

        if (MessageBox.Show(msg, "Löschen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        if (_selectedItem.IsMainFolder)
        {
            _folderTreeItems.Remove(_selectedItem);
        }
        else
        {
            RemoveFromParent(_folderTreeItems, _selectedItem);
        }

        _selectedItem = null;
        RefreshTreePositions();
    }

    private bool RemoveFromParent(ObservableCollection<FolderTreeItem> items, FolderTreeItem target)
    {
        foreach (var item in items)
        {
            if (item.Children.Contains(target))
            {
                item.Children.Remove(target);
                return true;
            }
            if (RemoveFromParent(item.Children, target))
                return true;
        }
        return false;
    }

    private void OnToggleInbox(object sender, RoutedEventArgs e)
    {
        if (_selectedItem is not { IsMainFolder: true }) return;
        _selectedItem.HasInbox = !_selectedItem.HasInbox;
        RefreshTreePositions();
    }

    private void OnTogglePrefix(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null || _selectedItem.IsMainFolder)
        {
            MessageBox.Show("Präfix kann nur bei Unterordnern umgeschaltet werden.",
                "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _selectedItem.HasPrefix = !_selectedItem.HasPrefix;
        RefreshTreePositions();
    }
}

/// <summary>UI-Model für einen Ordner im TreeView.</summary>
public class FolderTreeItem
{
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public bool IsMainFolder { get; set; }
    public bool HasInbox { get; set; }
    public bool HasPrefix { get; set; } = true;
    public bool IsExisting { get; set; }
    public ObservableCollection<FolderTreeItem> Children { get; set; } = [];

    public string DisplayName => IsMainFolder
        ? $"{Position:D2} {Name}"
        : (HasPrefix ? $"{Position:D2} {Name}" : Name);

    // Bindings für XAML
    public string Foreground => IsMainFolder ? "#CCCCCC" : "#858585";
    public string FontWeight => IsMainFolder ? "SemiBold" : "Normal";
    public Visibility HasInboxVisibility => HasInbox ? Visibility.Visible : Visibility.Collapsed;
    public string PrefixLabel => HasPrefix ? "Präfix: an" : "Präfix: aus";
    public SolidColorBrush PrefixColor => HasPrefix
        ? new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0x4E))
        : new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
    public Visibility PrefixVisibility => IsMainFolder ? Visibility.Collapsed : Visibility.Visible;
}
