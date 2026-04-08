using System.Windows;

namespace BauProjektManager.App;

/// <summary>
/// Ja/Nein-Bestätigungsdialog im BPM Dark Theme.
/// Wird über BpmInfoDialog.ShowConfirm() aufgerufen.
/// </summary>
public partial class BpmConfirmDialog : Window
{
    public BpmConfirmDialog()
    {
        InitializeComponent();
    }

    private void OnYesClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}