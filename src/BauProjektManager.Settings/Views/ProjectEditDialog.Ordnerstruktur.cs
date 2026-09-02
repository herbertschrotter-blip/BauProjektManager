﻿using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace BauProjektManager.Settings.Views;

/// <summary>Tab 5 Ordnerstruktur: Live-Refresh des FolderTemplateControl bei Disk-Aenderungen (BPM-070 Partial-Split).</summary>
public partial class ProjectEditDialog
{
    // ── FileSystemWatcher für Live-Ordnerstruktur (Tab 5) ────────────
    // BPM-112.05: FS-Ports fuer die Zugriffe (_fs in der Hauptdatei); der
    // FileSystemWatcher selbst bleibt System.IO (UI-Live-Refresh, kein Port-Aequivalent — bewusst).
    // Bekannt (BPM-066, post-V1): der Reload verwirft ungespeicherte Baum-Aenderungen.
    private void StartFolderWatcher(string rootPath)
    {
        if (!_fs.DirectoryExists(rootPath)) return;

        _folderWatcher = new FileSystemWatcher(rootPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.DirectoryName,
            EnableRaisingEvents = true
        };

        _folderWatcher.Created += OnFolderChanged;
        _folderWatcher.Deleted += OnFolderChanged;
        _folderWatcher.Renamed += OnFolderChanged;

        Closed += (_, _) =>
        {
            _folderWatcher.EnableRaisingEvents = false;
            _folderWatcher.Dispose();
            _folderWatcher = null;
        };
    }

    private void OnFolderChanged(object sender, FileSystemEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            ProjectFolderTemplate.LoadFromDisk(Project.Paths.Root);
        });
    }
}
