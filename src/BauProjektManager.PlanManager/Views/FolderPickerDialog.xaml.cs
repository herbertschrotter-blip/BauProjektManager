using System.Windows;
using System.Windows.Controls;
using BauProjektManager.PlanManager.ViewModels;
using Serilog;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// Zielordner-Wahl fuer das journalisierte Verschieben im Explorer (112.06b).
/// R01-008.01 (ADR-064 P.11): zeigt NUR den eigenen Ablageordner des Plans samt
/// Unterordnern; Ordner mit fuehrendem _ (_Eingang, _Archiv) oder . sind
/// ausgeblendet (der ArchiveMoveService prueft dieselbe Regel). Ergebnis ist
/// der RELATIVE Zielpfad. Kein "+ Neu…" hier — neue Ordner entstehen ueber
/// die Radial-Strecke bzw. Stammdaten (ADR-061).
/// </summary>
public partial class FolderPickerDialog : Window
{
    // BPM-112.05 (ADR-060 Slice 5): FS-Port statt direktem System.IO in der View.
    private static readonly Infrastructure.Services.LocalFileSystem _fs = new();

    private readonly string _rootPath;
    private ExplorerFolderNode? _selected;

    public FolderPickerDialog(string rootPath, string allowedRelativeDirectory, string planLabel)
    {
        _rootPath = rootPath;
        InitializeComponent();
        TitleText.Text = $"Zielordner für {planLabel}";
        var areaPath = _fs.Combine(rootPath, allowedRelativeDirectory);
        var areaNode = new ExplorerFolderNode(allowedRelativeDirectory, areaPath);
        foreach (var child in BuildChildNodes(areaPath))
            areaNode.Children.Add(child);
        areaNode.ChildrenLoaded = true;
        FolderTree.Items.Add(areaNode);
    }

    /// <summary>Gewaehlter Zielordner relativ zum Projektroot (nur bei DialogResult=true).</summary>
    public string SelectedRelativeDirectory { get; private set; } = "";

    private void OnFolderExpanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TreeViewItem { DataContext: ExplorerFolderNode node }
            || node.IsPlaceholder || node.ChildrenLoaded)
            return;
        node.ChildrenLoaded = true;
        node.Children.Clear();
        foreach (var child in BuildChildNodes(node.FullPath))
            node.Children.Add(child);
    }

    private void OnFolderSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        _selected = e.NewValue is ExplorerFolderNode { IsPlaceholder: false } node ? node : null;
        MoveButton.IsEnabled = _selected is not null;
    }

    private void OnMoveClick(object sender, RoutedEventArgs e)
    {
        if (_selected is null)
            return;
        SelectedRelativeDirectory = _selected.FullPath.Length > _rootPath.Length
            ? _selected.FullPath[_rootPath.Length..].TrimStart('\\', '/')
            : "";
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private static IEnumerable<ExplorerFolderNode> BuildChildNodes(string parentPath)
    {
        List<ExplorerFolderNode> nodes = [];
        try
        {
            foreach (var dir in _fs.EnumerateDirectories(parentPath)
                         .Where(IsSelectable)
                         .OrderBy(d => _fs.GetFileName(d), StringComparer.OrdinalIgnoreCase))
            {
                var node = new ExplorerFolderNode(_fs.GetFileName(dir), dir);
                if (_fs.EnumerateDirectories(dir).Any(IsSelectable))
                    node.Children.Add(ExplorerFolderNode.Placeholder());
                nodes.Add(node);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "FolderPicker: Unterordner nicht lesbar");
        }
        return nodes;
    }

    /// <summary>Gesperrt: _Eingang, _Archiv und System-Ordner (fuehrendes _ oder .).</summary>
    private static bool IsSelectable(string dir)
    {
        var name = _fs.GetFileName(dir);
        return !name.StartsWith('.') && !name.StartsWith('_');
    }
}
