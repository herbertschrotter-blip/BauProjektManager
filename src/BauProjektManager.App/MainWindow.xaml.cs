using System.Windows;
using System.Windows.Controls;

namespace BauProjektManager.App;

/// <summary>
/// Shell window with sidebar navigation and content area.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnNavigate(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string target)
        {
            StatusText.Text = $"Aktiv: {target} | Kein Projekt geladen";
        }
    }
}
