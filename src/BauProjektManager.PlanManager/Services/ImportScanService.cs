using System.IO;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Scans the _Eingang/ folder recursively for plan files.
/// Step 1 of the 7-stage analysis pipeline.
/// Returns ScannedFile records with filesystem metadata.
/// </summary>
public class ImportScanService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".dwg", ".dxf", ".jpg", ".jpeg", ".png", ".tif", ".tiff"
    };

    /// <summary>
    /// Scans the inbox folder recursively.
    /// Returns all supported files as ScannedFile records with paths relative to project root.
    /// </summary>
    public async Task<List<ScannedFile>> ScanAsync(
        string projectRootPath,
        string inboxRelativePath,
        CancellationToken ct = default)
    {
        var inboxPath = Path.Combine(projectRootPath, inboxRelativePath);

        if (!Directory.Exists(inboxPath))
        {
            Log.Warning("Eingang nicht gefunden: {Path}", inboxPath);
            return [];
        }

        var files = new List<ScannedFile>();

        await Task.Run(() =>
        {
            foreach (var filePath in Directory.EnumerateFiles(
                inboxPath, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();

                var ext = Path.GetExtension(filePath);
                if (!SupportedExtensions.Contains(ext))
                    continue;

                var fileInfo = new FileInfo(filePath);
                var relativePath = Path.GetRelativePath(projectRootPath, filePath);

                files.Add(new ScannedFile(
                    RelativePath: relativePath,
                    FileName: fileInfo.Name,
                    Extension: ext.ToLowerInvariant(),
                    FileSize: fileInfo.Length,
                    LastWriteTimeUtc: fileInfo.LastWriteTimeUtc));
            }
        }, ct);

        Log.Information("ImportScan: {Count} Dateien gefunden in {Path}",
            files.Count, inboxPath);
        return files;
    }
}
