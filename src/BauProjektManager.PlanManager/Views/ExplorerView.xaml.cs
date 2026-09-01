using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;
using Serilog;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// In-App-Explorer (BPM-112.06a, ADR-061 P.6 Modell A): Live-Ansicht des
/// Projektordners — Baum + Dateiliste + Launcher-Aktionen. Code-behind macht
/// nur UI-Orchestrierung (Lazy-Expand, Selektion, Clipboard); Fachlogik im VM.
/// </summary>
public partial class ExplorerView : UserControl
{
    public ExplorerView()
    {
        Resources.Add("BoolToVis", new BoolToVisConverter());
        Resources.Add("InverseBoolToVis", new InverseBoolToVisConverter());
        InitializeComponent();
    }

    public ExplorerViewModel? ViewModel => DataContext as ExplorerViewModel;

    /// <summary>"Im PlanManager anzeigen" — der Host wechselt zum ManuellSortieren-Tab (Archiv).</summary>
    public event EventHandler? PlanManagerRequested;

    /// <summary>Vom Host (ProjectDetailView) aufgerufen: VM aufbauen + Root laden.</summary>
    public void Initialize(
        string projectRootPath, IFileLauncher? fileLauncher,
        string inboxRelativePath = "", PlanManagerDatabase? planDb = null)
    {
        var vm = new ExplorerViewModel(fileLauncher, planDb);
        DataContext = vm;
        vm.Initialize(projectRootPath, inboxRelativePath);
    }

    private void OnFolderExpanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem { DataContext: ExplorerFolderNode node }
            && !node.IsPlaceholder)
            ViewModel?.LoadChildren(node);
    }

    private void OnFolderSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (ViewModel is { } vm && e.NewValue is ExplorerFolderNode { IsPlaceholder: false } node)
            vm.SelectedFolder = node;
    }

    private void OnFileDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel is { SelectedFile: not null } vm)
            vm.OpenFileCommand.Execute(vm.SelectedFile);
    }

    // ── Kontextmenü (112.06b) ───────────────────────────────────────

    /// <summary>Rechtsklick selektiert die Zeile unter dem Cursor, bevor das Menü aufgeht.</summary>
    private void OnListContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source)
        {
            var element = source;
            while (element is not null and not DataGridRow)
                element = VisualTreeHelper.GetParent(element);
            if (element is DataGridRow { Item: ExplorerFileRow row })
                FileList.SelectedItem = row;
        }

        var tracked = ViewModel?.SelectedFile?.IsTracked == true;
        CtxShowInPlanManager.IsEnabled = tracked;
        CtxMove.IsEnabled = tracked && ViewModel?.SelectedFile?.Entry is not null;
    }

    private void OnCtxOpenClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { SelectedFile: not null } vm)
            vm.OpenFileCommand.Execute(vm.SelectedFile);
    }

    private void OnCtxRevealClick(object sender, RoutedEventArgs e)
        => ViewModel?.RevealInExplorerCommand.Execute(null);

    private void OnCtxShowInPlanManagerClick(object sender, RoutedEventArgs e)
        => PlanManagerRequested?.Invoke(this, EventArgs.Empty);

    private void OnCtxMoveClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { SelectedFile: { Entry: not null } row } vm)
            return;

        var dialog = new FolderPickerDialog(vm.ProjectRootPath,
            $"{row.Entry.PlanNumber} ({row.Entry.Title})")
        {
            Owner = Window.GetWindow(this)
        };
        if (dialog.ShowDialog() == true && dialog.SelectedRelativeDirectory.Length > 0)
            vm.MoveTracked(row, dialog.SelectedRelativeDirectory);
    }

    private void OnCopyPathClick(object sender, RoutedEventArgs e)
    {
        var path = ViewModel?.PathForClipboard;
        if (path is null)
            return;
        try
        {
            Clipboard.SetText(path);
            if (ViewModel is { } vm)
                vm.StatusText = "Pfad kopiert.";
        }
        catch (Exception ex)
        {
            // Zwischenablage kann durch andere Prozesse gesperrt sein (COM) —
            // kein Crash, nur Status.
            Log.Warning(ex, "Explorer: Zwischenablage nicht verfuegbar");
            if (ViewModel is { } vm)
                vm.StatusText = "Zwischenablage nicht verfügbar.";
        }
    }
}
