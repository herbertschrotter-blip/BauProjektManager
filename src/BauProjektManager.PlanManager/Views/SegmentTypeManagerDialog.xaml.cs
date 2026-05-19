using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// Manager-Dialog fuer Segmenttypen (BPM-108 Phase C Teil 3).
/// </summary>
public partial class SegmentTypeManagerDialog : Window
{
    private readonly SegmentTypeManagerViewModel _vm;

    public SegmentTypeManagerDialog(
        ISegmentTypeRepository repository,
        ISegmentTypeCatalog catalog,
        IIdGenerator idGenerator)
    {
        // Converter aus dem Wizard-Dialog hier neu registrieren — die Dialog-Resources
        // sind nicht geteilt. Wir verwenden die selben Converter-Typen.
        Resources.Add("BoolToVis", new BoolToVisConverter());
        Resources.Add("InverseBoolToVis", new InverseBoolToVisConverter());
        Resources.Add("HexToColor", new HexToColorConverter());

        InitializeComponent();

        _vm = new SegmentTypeManagerViewModel(repository, catalog, idGenerator);
        DataContext = _vm;

        UpdateStats();
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SegmentTypeManagerViewModel.Groups))
                UpdateStats();
        };
    }

    private void UpdateStats()
    {
        var groupCount = _vm.Groups.Count;
        var typeCount = _vm.Groups.Sum(g => g.Items.Count);
        var inactiveCount = _vm.Groups.Sum(g => g.Items.Count(t => !t.IsActive));
        var customCount = _vm.Groups.Sum(g => g.Items.Count(t => !t.IsBuiltin));
        StatsText.Text = $"{groupCount} Gruppen · {typeCount} Segmenttypen · {inactiveCount} deaktiviert · {customCount} eigen";
    }

    private void OnTypeRowClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is SegmentTypeDefinition type)
        {
            _vm.SelectType(type);
            e.Handled = true;
        }
    }

    private void OnToggleTypeActiveClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is SegmentTypeDefinition type)
        {
            _vm.ToggleTypeActive(type);
            e.Handled = true;
        }
    }

    private void OnGroupToggleClick(object sender, MouseButtonEventArgs e)
    {
        // Doppelklick → Gruppe deaktivieren/aktivieren. Single-Klick koennte
        // spaeter zum Collapse/Expand verwendet werden — hier bewusst Doppelklick
        // damit nicht jeder Listen-Scroll-Klick versehentlich deaktiviert.
        if (e.ClickCount == 2
            && sender is FrameworkElement fe
            && fe.Tag is SegmentTypeGroupDefinition group)
        {
            _vm.ToggleGroupActive(group);
            e.Handled = true;
        }
    }

    private void OnColorPaletteClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string hex)
        {
            _vm.ColorDraft = hex;
            e.Handled = true;
        }
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
