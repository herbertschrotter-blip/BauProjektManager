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
        PreviewKeyDown += OnPanelKeyDown;
    }

    /// <summary>
    /// Tastatur fuer die letzte offene Rueckfrage (Mockup 3.4): Ziffer 1–9 waehlt die Antwort (bei "mehrere"
    /// kreuzt sie an), Enter sendet die angekreuzten. Gilt nicht, solange im Eingabefeld getippt wird.
    /// </summary>
    private void OnPanelKeyDown(object sender, KeyEventArgs e)
    {
        if (_vm is null || InputBox.IsKeyboardFocusWithin)
            return;
        var frage = _vm.Entries.LastOrDefault(x => x.Question is { IsAnswered: false })?.Question;
        if (frage is null)
            return;
        var ziffer = e.Key switch
        {
            >= Key.D1 and <= Key.D9 => e.Key - Key.D0,
            >= Key.NumPad1 and <= Key.NumPad9 => e.Key - Key.NumPad0,
            _ => 0
        };
        if (ziffer > 0 && frage.Answers.FirstOrDefault(a => a.Number == ziffer) is { } antwort)
        {
            _vm.AnswerCommand.Execute(antwort);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && frage.AllowMultiple)
        {
            _vm.SendMultipleCommand.Execute(frage);
            e.Handled = true;
        }
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
