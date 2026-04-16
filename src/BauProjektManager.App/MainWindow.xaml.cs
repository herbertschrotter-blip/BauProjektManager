using System.Windows;
using System.Windows.Controls;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.PlanManager.Views;
using BauProjektManager.Settings.Views;

namespace BauProjektManager.App;

/// <summary>
/// Shell window with sidebar navigation and content area.
/// Services werden via DI-Container injiziert.
/// </summary>
public partial class MainWindow : Window
{
    private readonly PlanManagerView _planManagerView;
    private readonly SettingsView _settingsView;
    private readonly IDeveloperToolsService? _devTools;

    public MainWindow(
        ProjectDatabase db,
        IIdGenerator idGenerator,
        IDialogService dialogService,
        IDeveloperToolsService? devTools = null)
    {
        InitializeComponent();
        _devTools = devTools;

        _planManagerView = new PlanManagerView(db, idGenerator);
        _settingsView = new SettingsView(db, dialogService);

        UpdateSidebarBadge();

#if DEBUG
        BtnDevTools.Visibility = Visibility.Visible;
#endif
    }

#if DEBUG
    private void OnOpenDevTools(object sender, RoutedEventArgs e)
    {
        if (_devTools is null) return;
        var dialog = new DevToolsDialog(_devTools);
        dialog.Owner = this;
        dialog.ShowDialog();
    }
#endif

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
                "Plans" => "PlanManager | Kein Projekt geladen",
                "Settings" => "Einstellungen",
                _ => "Bereit"
            };

            UpdateSidebarBadge();
            HighlightNavButton(button);
        }
    }

    private void UpdateSidebarBadge()
    {
        var count = _planManagerView.TotalInboxCount;
        if (count > 0)
        {
            SidebarBadgeText.Text = count.ToString();
            SidebarBadge.Visibility = Visibility.Visible;
        }
        else
        {
            SidebarBadge.Visibility = Visibility.Collapsed;
        }
    }

    private void HighlightNavButton(Button active)
    {
        var navButtons = new[] { BtnPlans, BtnSettings };
        var normalBrush = (System.Windows.Media.Brush)FindResource("BpmTextPrimary");
        var activeBrush = (System.Windows.Media.Brush)FindResource("BpmAccentPrimary");
        var activeBg = (System.Windows.Media.Brush)FindResource("BpmBgActive");

        foreach (var btn in navButtons)
        {
            btn.Foreground = normalBrush;
            btn.Background = System.Windows.Media.Brushes.Transparent;
        }

        active.Foreground = activeBrush;
        active.Background = activeBg;
    }
}
