using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
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
    private readonly AppSettingsService _settingsService;
    private readonly IPersistenceRegistry? _persistenceRegistry;
    private readonly IProfileArchiveService? _profileArchiveService;

    public MainWindow(
        ProjectDatabase db,
        IIdGenerator idGenerator,
        IDialogService dialogService,
        IProfileManager profileManager,
        AppSettingsService settingsService,
        IDeveloperToolsService? devTools = null,
        IPersistenceRegistry? persistenceRegistry = null,
        ISegmentTypeCatalog? segmentTypeCatalog = null,
        ISegmentTypeRepository? segmentTypeRepository = null,
        IProfileArchiveService? profileArchiveService = null,
        IPdfRenderService? pdfRenderService = null,
        IFileLauncher? fileLauncher = null,
        IPdfTextService? pdfTextService = null)
    {
        InitializeComponent();
        _devTools = devTools;
        _settingsService = settingsService;
        _persistenceRegistry = persistenceRegistry;
        _profileArchiveService = profileArchiveService;

        _planManagerView = new PlanManagerView(db, idGenerator, profileManager, persistenceRegistry,
            segmentTypeCatalog, segmentTypeRepository, pdfRenderService, fileLauncher, settingsService,
            pdfTextService);
        _settingsView = new SettingsView(db, dialogService, settingsService, persistenceRegistry);

        // BPM-067: Sidebar-Zustand (Text 220 / Icons 56) geraetelokal wiederherstellen
        SidebarVersion.Text = "v" + AppVersionText();
        ApplySidebarState(_settingsService.LoadDevice().UiLayout.SidebarCollapsed, persist: false);
        UpdateSidebarBadge();

        SourceInitialized += OnSourceInitialized;
        Closing += OnMainWindowClosing;

#if DEBUG
        BtnDevTools.Visibility = Visibility.Visible;
#endif
    }

    // ── Fensterlage merken/wiederherstellen (Win32 WINDOWPLACEMENT) ──
    // Persistiert geräte-lokal in device-settings.json. Windows klemmt beim
    // Wiederherstellen selbst auf einen sichtbaren Bildschirm und behandelt
    // unterschiedliche DPI je Monitor korrekt (4K-Haupt- + 1080p-Zweitmonitor).

    private const int SwShowNormal = 1;
    private const int SwShowMaximized = 3;

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        try
        {
            var saved = _settingsService.LoadDevice().MainWindowPlacement;
            if (saved is null)
            {
                // Erststart (noch nichts gespeichert): maximiert.
                WindowState = WindowState.Maximized;
                return;
            }

            var placement = new WINDOWPLACEMENT
            {
                length = Marshal.SizeOf<WINDOWPLACEMENT>(),
                flags = 0,
                // Minimiert nie wiederherstellen → auf Normal zurückfallen.
                showCmd = saved.ShowCmd == SwShowMaximized ? SwShowMaximized : SwShowNormal,
                ptMinPosition = new POINT { X = -1, Y = -1 },
                ptMaxPosition = new POINT { X = -1, Y = -1 },
                rcNormalPosition = new RECT
                {
                    Left = saved.Left,
                    Top = saved.Top,
                    Right = saved.Right,
                    Bottom = saved.Bottom
                }
            };
            SetWindowPlacement(new WindowInteropHelper(this).Handle, ref placement);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Fensterlage konnte nicht wiederhergestellt werden");
        }
    }

    private void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero) return;

            var placement = new WINDOWPLACEMENT { length = Marshal.SizeOf<WINDOWPLACEMENT>() };
            if (!GetWindowPlacement(handle, ref placement)) return;

            var device = _settingsService.LoadDevice();
            device.MainWindowPlacement = new WindowPlacementSettings
            {
                Left = placement.rcNormalPosition.Left,
                Top = placement.rcNormalPosition.Top,
                Right = placement.rcNormalPosition.Right,
                Bottom = placement.rcNormalPosition.Bottom,
                // Minimiert nicht persistieren — sonst startet die App minimiert.
                ShowCmd = placement.showCmd == SwShowMaximized ? SwShowMaximized : SwShowNormal
            };
            _settingsService.SaveDevice(device);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Fensterlage konnte nicht gespeichert werden");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public int showCmd;
        public POINT ptMinPosition;
        public POINT ptMaxPosition;
        public RECT rcNormalPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

#if DEBUG
    private void OnOpenDevTools(object sender, RoutedEventArgs e)
    {
        if (_devTools is null) return;
        var dialog = new DevToolsDialog(_devTools, _settingsService, _persistenceRegistry, _profileArchiveService);
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

        // Zugeklappt traegt der Tooltip die Zahl, weil neben dem Icon kein Platz fuer Text ist.
        if (_sidebarCollapsed)
            BtnPlans.ToolTip = MakeToolTip(count > 0 ? $"PlanManager · {count} im Eingang" : "PlanManager");
    }

    // ── Klappbare Sidebar (BPM-067, ADR-siehe UI_Navigation.md Kap. 2) ──
    // Zustand A: 220px, Emoji + Text, Badge neben dem Text.
    // Zustand B: 56px, nur Emoji mit Tooltip, Badge als Ecke am Icon.
    // Default beim ersten Start: aufgeklappt; Zustand in device-settings.json.

    private const double SidebarExpandedWidth = 220;
    private const double SidebarCollapsedWidth = 56;
    private bool _sidebarCollapsed;

    private void OnToggleSidebar(object sender, RoutedEventArgs e)
        => ApplySidebarState(!_sidebarCollapsed, persist: true);

    private void ApplySidebarState(bool collapsed, bool persist)
    {
        _sidebarCollapsed = collapsed;
        SidebarColumn.Width = new GridLength(collapsed ? SidebarCollapsedWidth : SidebarExpandedWidth);

        var textVisibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        SidebarTitle.Visibility = textVisibility;
        SidebarVersion.Visibility = textVisibility;

        BtnSidebarToggle.Content = FindResource(collapsed ? "IconNavExpand" : "IconNavCollapse");
        BtnSidebarToggle.ToolTip = MakeToolTip(collapsed ? "Sidebar aufklappen" : "Sidebar zuklappen");
        BtnSidebarToggle.HorizontalAlignment = collapsed ? HorizontalAlignment.Center : HorizontalAlignment.Right;

        ApplyNavButtonState(BtnPlans, PlansText, "PlanManager", collapsed);
        ApplyNavButtonState(BtnSettings, SettingsText, "Einstellungen", collapsed);
        ApplyNavButtonState(BtnDevTools, DevToolsText, "Dev Tools", collapsed);

        // Badge: neben dem Text (A) bzw. als Ecke oben rechts am Icon (B)
        SidebarBadge.VerticalAlignment = collapsed ? VerticalAlignment.Top : VerticalAlignment.Center;
        SidebarBadge.Margin = collapsed ? new Thickness(0, -6, -8, 0) : new Thickness(6, 0, 0, 0);
        SidebarBadge.Padding = collapsed ? new Thickness(4, 0, 4, 0) : new Thickness(5, 1, 5, 1);

        if (!persist) return;
        try
        {
            var device = _settingsService.LoadDevice();
            device.UiLayout.SidebarCollapsed = collapsed;
            _settingsService.SaveDevice(device);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Sidebar-Zustand konnte nicht gespeichert werden");
        }
    }

    private void ApplyNavButtonState(Button button, TextBlock label, string name, bool collapsed)
    {
        label.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        // Aufgeklappt volle Breite, damit das Badge rechts am Rand sitzt statt ueber dem Text.
        button.HorizontalContentAlignment = collapsed ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;
        button.Padding = collapsed ? new Thickness(0, 10, 0, 10) : new Thickness(15, 10, 15, 10);
        button.ToolTip = collapsed ? MakeToolTip(name) : null;
    }

    private ToolTip MakeToolTip(string text)
        => new() { Content = text, Style = (Style)FindResource("BpmToolTip") };

    private static string AppVersionText()
    {
        var v = typeof(MainWindow).Assembly.GetName().Version;
        return v is null ? "?" : $"{v.Major}.{v.Minor}.{v.Build}";
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
