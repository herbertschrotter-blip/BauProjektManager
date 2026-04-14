using System.Windows;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;
using Microsoft.Win32;
using Serilog;

namespace BauProjektManager.App;

public partial class SetupDialog : Window
{
    public AppSettings Settings { get; private set; }
    public bool SetupCompleted { get; private set; }

    private readonly AppSettingsService _settingsService;

    public SetupDialog(AppSettingsService settingsService, AppSettings settings)
    {
        InitializeComponent();
        _settingsService = settingsService;
        Settings = settings;
        LoadSystemInfo();
    }

    private void LoadSystemInfo()
    {
        // Show system info
        // Show system info with all detected cloud storages
        var detectedClouds = AppSettingsService.DetectAllCloudStoragePaths();
        var cloudInfo = detectedClouds.Count > 0
            ? "\n   " + string.Join("\n   ", detectedClouds)
            : "\n   (nicht gefunden)";
        TxtSystemInfo.Text = $"Rechner: {Environment.MachineName}\n" +
                             $"Benutzer: {Environment.UserName}\n" +
                             $"Cloud-Speicher: {cloudInfo}";

        // Pre-fill cloud storage with first detected
        var detectedCloud = detectedClouds.Count > 0 ? detectedClouds[0] : null;
        if (detectedCloud is not null)
        {
            TxtCloudStorage.Text = detectedCloud;
            Settings.OneDrivePath = detectedCloud;

            // Try to find common work folder
            var commonWorkFolders = new[]
            {
                System.IO.Path.Combine(detectedCloud, "Dokumente", "02 Arbeit"),
                System.IO.Path.Combine(detectedCloud, "02 Arbeit"),
                System.IO.Path.Combine(detectedCloud, "Dokumente", "Arbeit"),
                System.IO.Path.Combine(detectedCloud, "Arbeit")
            };

            foreach (var folder in commonWorkFolders)
            {
                if (System.IO.Directory.Exists(folder))
                {
                    TxtBasePath.Text = folder;
                    Settings.BasePath = folder;
                    break;
                }
            }
        }

        // Pre-fill existing settings
        if (!string.IsNullOrEmpty(Settings.BasePath))
            TxtBasePath.Text = Settings.BasePath;
        if (!string.IsNullOrEmpty(Settings.ArchivePath))
            TxtArchivePath.Text = Settings.ArchivePath;
    }

    private void OnBrowseCloudStorage(object sender, RoutedEventArgs e)
    {
        var path = BrowseFolder("Cloud-Speicher-Ordner auswählen", TxtCloudStorage.Text);
        if (path is not null)
        {
            TxtCloudStorage.Text = path;
            Settings.OneDrivePath = path;
        }
    }

    private void OnBrowseBasePath(object sender, RoutedEventArgs e)
    {
        var startPath = !string.IsNullOrEmpty(TxtCloudStorage.Text) ? TxtCloudStorage.Text : "";
        var path = BrowseFolder("Arbeitsordner auswählen", startPath);
        if (path is not null)
        {
            TxtBasePath.Text = path;
            Settings.BasePath = path;
        }
    }

    private void OnBrowseArchivePath(object sender, RoutedEventArgs e)
    {
        var startPath = !string.IsNullOrEmpty(TxtBasePath.Text) ? TxtBasePath.Text : "";
        var path = BrowseFolder("Archiv-Ordner auswählen (oder neuen erstellen)", startPath);
        if (path is not null)
        {
            TxtArchivePath.Text = path;
            Settings.ArchivePath = path;
        }
    }

    private static string? BrowseFolder(string title, string initialDirectory)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            InitialDirectory = initialDirectory
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        // Validate
        if (string.IsNullOrEmpty(TxtCloudStorage.Text))
        {
            TxtStatus.Text = "Bitte Cloud-Speicher-Pfad angeben!";
            return;
        }

        if (string.IsNullOrEmpty(TxtBasePath.Text))
        {
            TxtStatus.Text = "Bitte Arbeitsordner angeben!";
            return;
        }

        if (!System.IO.Directory.Exists(TxtBasePath.Text))
        {
            TxtStatus.Text = $"Arbeitsordner existiert nicht: {TxtBasePath.Text}";
            return;
        }

        // Save settings
        Settings.OneDrivePath = TxtCloudStorage.Text;
        Settings.BasePath = TxtBasePath.Text;
        Settings.ArchivePath = TxtArchivePath.Text;
        Settings.MachineName = Environment.MachineName;
        Settings.IsFirstRun = false;
        Settings.SetupCompletedAt = DateTime.Now;

        // Determine export path
        Settings.ExportPath = System.IO.Path.Combine(Settings.BasePath, ".AppData", "BauProjektManager");

        try
        {
            _settingsService.Save(Settings);
            SetupCompleted = true;
            Log.Information("Setup completed: Base={Base}, Archive={Archive}, Export={Export}",
                Settings.BasePath, Settings.ArchivePath, Settings.ExportPath);
            Close();
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Fehler beim Speichern: {ex.Message}";
            Log.Error(ex, "Setup save failed");
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        SetupCompleted = false;
        Close();
    }
}
