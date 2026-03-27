using System.Windows;
using System.Windows.Controls;
using BauProjektManager.PlanManager.Views;
using BauProjektManager.Settings.Views;

namespace BauProjektManager.App;

/// <summary>
/// Shell window with sidebar navigation and content area.
/// </summary>
public partial class MainWindow : Window
{
    private readonly PlanManagerView _planManagerView = new();
    private readonly SettingsView _settingsView = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnNavigate(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string target)
        {
            ContentArea.Content = target switch
            {
                "Plans" => _planManagerView,
                "Settings" => _settingsView,
                _ => null
            };

            StatusText.Text = target switch
            {
                "Plans" => "Pläne | Kein Projekt geladen",
                "Settings" => "Einstellungen",
                _ => "Bereit"
            };
        }
    }
}
