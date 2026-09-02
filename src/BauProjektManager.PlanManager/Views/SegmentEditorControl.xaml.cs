using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// Wiederverwendbarer Segment-Editor (BPM-126c) im Muster des ProfilWizard-Schritts 2:
/// EINE Flaeche aus Token-Kacheln mit klickbaren Trennzeichen, Zuweisung per
/// Drag and Drop aus der Segmenttyp-Palette (BPM-108-Katalog).
/// Gedacht fuer Plandaten-Tab UND den Wizard (BPM-080.05) — die Persistenz
/// macht der Host ueber <see cref="AssignmentChanged"/>.
/// </summary>
public partial class SegmentEditorControl : UserControl
{
    /// <summary>Drag-Format fuer einen Segmenttyp aus der Palette.</summary>
    private const string DragFormat = "BpmSegmentTypeId";

    public SegmentEditorControl()
    {
        Resources.Add("BoolToVis", new BoolToVisConverter());
        InitializeComponent();
    }

    public SegmentEditorViewModel ViewModel { get; } = new();

    /// <summary>Weitergereicht: eine Zuweisung wurde gesetzt oder entfernt.</summary>
    public event EventHandler<SegmentAssignmentChangedEventArgs>? AssignmentChanged;

    /// <summary>Oeffnet den bestehenden Segmenttyp-Manager (BPM-108).</summary>
    public event EventHandler? ManageTypesRequested;

    /// <summary>Editor auf eine Datei setzen (Host ruft das bei Auswahl-Wechsel).</summary>
    public void Load(string fileName, ISegmentTypeCatalog? catalog,
        IReadOnlyList<PlanDocumentSegment>? existing)
    {
        DataContext = ViewModel;
        ViewModel.AssignmentChanged -= OnViewModelAssignmentChanged;
        ViewModel.AssignmentChanged += OnViewModelAssignmentChanged;
        ViewModel.Load(fileName, catalog, existing);
    }

    private void OnViewModelAssignmentChanged(object? sender, SegmentAssignmentChangedEventArgs e)
        => AssignmentChanged?.Invoke(this, e);

    // ── Trennzeichen ────────────────────────────────────────────────

    private void OnSeparatorClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SegmentSeparatorElement sep })
            ViewModel.ToggleSeparator(sep.SeparatorIndex);
    }

    /// <summary>Klick auf ein verschmolzenes Trennzeichen INNERHALB einer Kachel: wieder trennen.</summary>
    private void OnMergedSeparatorClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SegmentTokenPart { IsSeparator: true } part })
            ViewModel.ToggleSeparator(part.SeparatorIndex);
        e.Handled = true;
    }

    private void OnSeparatorChipClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SeparatorChoice choice })
            ViewModel.ToggleSeparatorChar(choice.Char);
    }

    // ── Drag and Drop ───────────────────────────────────────────────

    private void OnPaletteDragStart(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: SegmentTypeDefinition type } element)
            return;
        DragDrop.DoDragDrop(element, new DataObject(DragFormat, type.Id), DragDropEffects.Copy);
    }

    private void OnTokenDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DragFormat) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnTokenDrop(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: SegmentTokenElement token }
            || e.Data.GetData(DragFormat) is not string typeId)
            return;
        var type = ViewModel.Palette.FirstOrDefault(t => t.Id == typeId);
        if (type is not null)
            ViewModel.AssignType(token.StartAtomIndex, type);
        e.Handled = true;
    }

    /// <summary>Rechtsklick auf eine Kachel entfernt die Zuweisung.</summary>
    private void OnTokenClearClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SegmentTokenElement { IsAssigned: true } token })
            ViewModel.AssignType(token.StartAtomIndex, null);
    }

    private void OnManageTypesClick(object sender, MouseButtonEventArgs e)
        => ManageTypesRequested?.Invoke(this, EventArgs.Empty);
}

/// <summary>Faerbt eine zugewiesene Token-Kachel in der Feldtyp-Farbe (Colors.xaml).</summary>
public sealed class SegmentColorConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not string hex || hex.Length == 0)
            return DependencyProperty.UnsetValue;
        try
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
        catch
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
