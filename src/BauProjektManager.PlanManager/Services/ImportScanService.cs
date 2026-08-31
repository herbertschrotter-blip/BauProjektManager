using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Scans the _Eingang/ folder recursively for plan files.
/// Step 1 of the 7-stage analysis pipeline.
/// Returns ScannedFile records with filesystem metadata.
/// BPM-112.01 (ADR-060 Slice 1): laeuft komplett ueber die FS-Ports.
/// </summary>
public class ImportScanService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".dwg", ".dxf", ".jpg", ".jpeg", ".png", ".tif", ".tiff"
    };

    private readonly IFileSystemReader _reader;
    private readonly IPathService _path;

    public ImportScanService(IFileSystemReader reader, IPathService path)
    {
        _reader = reader;
        _path = path;
    }

    /// <summary>
    /// Scans the inbox folder recursively.
    /// Returns all supported files as ScannedFile records with paths relative to project root.
    /// </summary>
    public async Task<List<ScannedFile>> ScanAsync(
        string projectRootPath,
        string inboxRelativePath,
        CancellationToken ct = default)
    {
        var inboxPath = _path.Combine(projectRootPath, inboxRelativePath);

        if (!_reader.DirectoryExists(inboxPath))
        {
            Log.Warning("Eingang nicht gefunden: {Path}", inboxPath);
            return [];
        }

        var files = new List<ScannedFile>();

        await Task.Run(() =>
        {
            foreach (var filePath in _reader.EnumerateFiles(inboxPath, "*", recursive: true))
            {
                ct.ThrowIfCancellationRequested();

                var ext = _path.GetExtension(filePath);
                if (!SupportedExtensions.Contains(ext))
                    continue;

                var info = _reader.GetFileInfo(filePath);
                var relativePath = _path.GetRelativePath(projectRootPath, filePath);

                files.Add(new ScannedFile(
                    RelativePath: relativePath,
                    FileName: _path.GetFileName(filePath),
                    Extension: ext.ToLowerInvariant(),
                    FileSize: info.Length,
                    LastWriteTimeUtc: info.LastWriteTimeUtc));
            }
        }, ct);

        Log.Information("ImportScan: {Count} Dateien gefunden in {Path}",
            files.Count, inboxPath);
        return files;
    }
}
