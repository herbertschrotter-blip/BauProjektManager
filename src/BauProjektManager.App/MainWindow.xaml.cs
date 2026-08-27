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
