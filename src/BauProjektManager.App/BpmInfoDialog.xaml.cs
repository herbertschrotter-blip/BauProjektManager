using System.Windows;

namespace BauProjektManager.App;

/// <summary>
/// Eigene Info-/Warn-/Fehler-MessageBox im BPM Dark Theme.
/// Ersetzt MessageBox.Show() überall in der App.
/// </summary>
public partial class BpmInfoDialog : Window
{
    public BpmInfoDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Zeigt einen Info-Dialog im BPM-Design.
    /// </summary>
    public static void ShowInfo(string message, string title = "Hinweis")
    {
        var dialog = new BpmInfoDialog
        {
            Title = title,
            Owner = Application.Current.MainWindow
        };
        dialog.TxtIcon.Text = "ℹ";
        dialog.TxtMessage.Text = message;
        dialog.ShowDialog();
    }

    /// <summary>
    /// Zeigt einen Warn-Dialog im BPM-Design.
    /// </summary>
    public static void ShowWarning(string message, string title = "Warnung")
    {
        var dialog = new BpmInfoDialog
        {
            Title = title,
            Owner = Application.Current.MainWindow
        };
        dialog.TxtIcon.Text = "⚠";
        dialog.TxtMessage.Text = message;
        dialog.ShowDialog();
    }

    /// <summary>
    /// Zeigt einen Fehler-Dialog im BPM-Design.
    /// </summary>
    public static void ShowError(string message, string title = "Fehler")
    {
        var dialog = new BpmInfoDialog
        {
            Title = title,
            Owner = Application.Current.MainWindow
        };
        dialog.TxtIcon.Text = "❌";
        dialog.TxtMessage.Text = message;
        dialog.ShowDialog();
    }

    /// <summary>
    /// Zeigt einen Ja/Nein-Dialog im BPM-Design. Gibt true zurück wenn Ja geklickt.
    /// </summary>
    public static bool ShowConfirm(string message, string title = "Bestätigung")
    {
        var dialog = new BpmConfirmDialog
        {
            Title = title,
            Owner = Application.Current.MainWindow
        };
        dialog.TxtIcon.Text = "❓";
        dialog.TxtMessage.Text = message;
        return dialog.ShowDialog() == true;
    }
}