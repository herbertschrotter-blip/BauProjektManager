using System.IO;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using Serilog;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// Schreibt und liest den schlanken Projekt-Ausweis .bpm/manifest.json (ADR-046, BPM-046):
/// Identität + Modul-Flags, sonst nichts. Der Vollexport ist Sache des
/// <see cref="ProjectExportService"/>.
///
/// Vorwärtsmigration (einmalig, automatisch, <see cref="EnsureMigrated"/>):
/// - alte Einzeldatei <c>.bpm-manifest</c> → project.json + manifest.json, alte Datei wird gelöscht
/// - manifest.json mit SchemaVersion 1 (früherer Vollexport) → project.json + schlankes manifest.json
/// </summary>
public class ManifestService
{
    private readonly ProjectExportService _exportService;
    private readonly IPersistenceRegistry? _persistenceRegistry;

    public ManifestService(ProjectExportService exportService, IPersistenceRegistry? persistenceRegistry = null)
    {
        _exportService = exportService;
        _persistenceRegistry = persistenceRegistry;
    }

    // === Schreiben ===

    /// <summary>Schreibt den Ausweis aus dem Projekt. Legt .bpm/ bei Bedarf an.</summary>
    public void WriteManifest(Project project, string projectRootPath)
    {
        if (string.IsNullOrEmpty(projectRootPath) || !Directory.Exists(projectRootPath))
        {
            Log.Warning("Cannot write manifest: directory does not exist {Path}", projectRootPath);
            return;
        }

        WriteManifestCore(ProjectToManifest(project), projectRootPath);
    }

    private void WriteManifestCore(ProjectManifest manifest, string projectRootPath)
    {
        var manifestPath = BpmFolder.ManifestPath(projectRootPath);
        try
        {
            BpmFolder.EnsureFolder(projectRootPath);
            BpmFolder.WriteJsonAtomic(manifestPath, manifest);

            _persistenceRegistry?.Register(new PersistenceEntry(
                DisplayName: ".bpm/manifest.json",
                AbsolutePath: manifestPath,
                Type: PersistenceType.Config,
                Scope: PersistenceScope.ProjectLocal,
                Description: "Projekt-Ausweis: Identitaet + Modul-Flags (ADR-046)"));

            Log.Information("Manifest written: {Path}", manifestPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to write manifest to {Path}", manifestPath);
        }
    }

    // === Lesen ===

    /// <summary>
    /// Liest den Ausweis. Führt vorher die Migration alter Formate aus.
    /// null, wenn kein Manifest vorhanden oder nicht lesbar.
    /// </summary>
    public ProjectManifest? ReadManifest(string projectRootPath)
    {
        EnsureMigrated(projectRootPath);

        var path = BpmFolder.ManifestPath(projectRootPath);
        return File.Exists(path) ? BpmFolder.ReadJson<ProjectManifest>(path) : null;
    }

    /// <summary>
    /// Prüft, ob der Ordner ein BPM-Projekt ist — neues Format, alter Vollexport
    /// unter manifest.json oder alte Einzeldatei .bpm-manifest.
    /// </summary>
    public bool HasManifest(string projectRootPath)
    {
        return File.Exists(BpmFolder.ManifestPath(projectRootPath))
            || File.Exists(BpmFolder.ExportPath(projectRootPath))
            || File.Exists(BpmFolder.LegacyManifestPath(projectRootPath));
    }

    // === Migration ===

    /// <summary>
    /// Bringt einen Projektordner auf das Split-Format. Idempotent; true wenn etwas migriert wurde.
    /// Bestehendes project.json wird nie überschrieben — es könnte neuer sein als die alte Datei.
    /// </summary>
    public bool EnsureMigrated(string projectRootPath)
    {
        if (string.IsNullOrEmpty(projectRootPath) || !Directory.Exists(projectRootPath))
            return false;

        var manifestPath = BpmFolder.ManifestPath(projectRootPath);
        var legacyPath = BpmFolder.LegacyManifestPath(projectRootPath);

        try
        {
            if (File.Exists(manifestPath)
                && BpmFolder.ReadSchemaVersion(manifestPath) < ProjectManifest.CurrentSchemaVersion)
            {
                return MigrateFullExport(manifestPath, projectRootPath, deleteSourceAfter: false, "manifest.json v1");
            }

            if (!File.Exists(manifestPath) && File.Exists(legacyPath))
            {
                return MigrateFullExport(legacyPath, projectRootPath, deleteSourceAfter: true, ".bpm-manifest");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Manifest migration failed for {Path}", projectRootPath);
        }

        return false;
    }

    private bool MigrateFullExport(string sourcePath, string projectRootPath, bool deleteSourceAfter, string label)
    {
        var export = BpmFolder.ReadJson<ProjectExport>(sourcePath);
        if (export is null)
        {
            Log.Warning("Manifest migration skipped — {Label} not readable at {Path}", label, sourcePath);
            return false;
        }

        if (!File.Exists(BpmFolder.ExportPath(projectRootPath)))
            _exportService.WriteExport(export, projectRootPath);

        WriteManifestCore(ExportToManifest(export), projectRootPath);

        if (deleteSourceAfter)
        {
            BpmFolder.EnsureFolder(projectRootPath);
            File.SetAttributes(sourcePath, FileAttributes.Normal);
            File.Delete(sourcePath);
        }

        Log.Information("Manifest migrated from {Label} at {Path}", label, projectRootPath);
        return true;
    }

    // === Mapping ===

    private static ProjectManifest ProjectToManifest(Project project) => new()
    {
        ProjectId = project.Id,
        ProjectNumber = project.ProjectNumber,
        Name = project.Name,
        UpdatedAtUtc = DateTime.UtcNow,
        CreatedByMachine = Environment.MachineName
    };

    /// <summary>Ausweis aus einem alten Vollexport — die DB-ID ist dort nicht enthalten.</summary>
    private static ProjectManifest ExportToManifest(ProjectExport export) => new()
    {
        ProjectId = string.Empty,
        ProjectNumber = export.ProjectNumber,
        Name = export.Name,
        UpdatedAtUtc = DateTime.UtcNow,
        CreatedByMachine = Environment.MachineName
    };
}
