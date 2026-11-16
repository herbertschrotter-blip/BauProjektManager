using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

/// <summary>"Mit Claude sortieren" (Seitenliste 3.4): Knopf, Startdialog, Vorschlagsleiste und Chatfenster.</summary>
public partial class ManualCaptureView
{
    private ClaudeSortViewModel? _claudeSort;

    /// <summary>
    /// Chatfenster-ViewModel — vom Host (ProjectDetailView) gesetzt. NULL = Funktion auf diesem PC nicht
    /// eingerichtet: Knopf und Chat bleiben unsichtbar, von Hand sortieren geht unveraendert.
    /// </summary>
    public ClaudeSortViewModel? ClaudeSort
    {
        get => _claudeSort;
        set
        {
            if (_claudeSort is not null)
                _claudeSort.PropertyChanged -= OnClaudeSortChanged;
            _claudeSort = value;
            ClaudeBar.DataContext = value;
            ClaudeDraftBar.DataContext = value;
            ClaudeChat.DataContext = value;
            ClaudeBar.Visibility = value is null ? Visibility.Collapsed : Visibility.Visible;
            if (value is not null)
                value.PropertyChanged += OnClaudeSortChanged;
            // Einstellung kann sich in "Einstellungen" geaendert haben: Knopf beim Wiedereinblenden neu bewerten
            IsVisibleChanged -= OnClaudeVisibleChanged;
            IsVisibleChanged += OnClaudeVisibleChanged;
            UpdateClaudeVisibility();
        }
    }

    private void OnClaudeVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
            _claudeSort?.RefreshButton();
    }

    private void OnClaudeSortChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ClaudeSortViewModel.IsChatOpen) or nameof(ClaudeSortViewModel.IsDraftBarVisible)
            or nameof(ClaudeSortViewModel.Phase))
            UpdateClaudeVisibility();
    }

    private void UpdateClaudeVisibility()
    {
        ClaudeChat.Visibility = _claudeSort is { IsChatOpen: true } ? Visibility.Visible : Visibility.Collapsed;
        ClaudeDraftBar.Visibility = _claudeSort is { IsDraftBarVisible: true } ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Escape schliesst den Startdialog (Mockup Zustand 1).</summary>
    private void OnClaudeConfirmKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _claudeSort is not null)
        {
            _claudeSort.CancelStartCommand.Execute(null);
            e.Handled = true;
        }
    }
}
