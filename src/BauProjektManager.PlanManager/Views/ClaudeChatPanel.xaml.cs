using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// Chatfenster "Mit Claude sortieren" (Seitenliste 3.4). Logik im <see cref="ClaudeSortViewModel"/>;
/// hier nur Mitscrollen und Enter zum Senden.
/// </summary>
public partial class ClaudeChatPanel : UserControl
{
    private ClaudeSortViewModel? _vm;

    public ClaudeChatPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
            _vm.Entries.CollectionChanged -= OnEntriesChanged;
        _vm = DataContext as ClaudeSortViewModel;
        if (_vm is not null)
            _vm.Entries.CollectionChanged += OnEntriesChanged;
    }

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => Dispatcher.BeginInvoke(() => LogScroll.ScrollToEnd());

    /// <summary>Enter sendet, Umschalt+Enter ist eine neue Zeile.</summary>
    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || _vm is null)
            return;
        e.Handled = true;
        if (_vm.SendCommand.CanExecute(null))
            _vm.SendCommand.Execute(null);
    }
}
