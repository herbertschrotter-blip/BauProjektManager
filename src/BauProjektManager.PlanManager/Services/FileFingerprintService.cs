using System.IO;
using System.Security.Cryptography;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Computes MD5 hashes for scanned files (bounded parallel).
/// Step 2 of the 7-stage analysis pipeline.
/// MD5 + file_size are ALWAYS mandatory (PlanManager invariant).
/// </summary>
public class FileFingerprintService
{
    private const int MaxParallelism = 4;

    /// <summary>
    /// Enriches scanned files with MD5 hashes.
    /// Runs bounded parallel to avoid disk thrash.
    /// Files that fail to hash get a warning instead of crashing the import.
    /// </summary>
    public async Task<List<FingerprintResult>> FingerprintAsync(
        List<ScannedFile> files,
        string projectRootPath,
        CancellationToken ct = default)
    {
        var results = new List<FingerprintResult>(files.Count);
        var semaphore = new SemaphoreSlim(MaxParallelism);

        var tasks = files.Select(async file =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                var fullPath = Path.Combine(projectRootPath, file.RelativePath);
                var md5 = await ComputeMd5Async(fullPath, ct);

                lock (results)
                {
                    results.Add(new FingerprintResult(
                        new FingerprintedFile(file, md5), Error: null));
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log.Warning(ex, "MD5-Berechnung fehlgeschlagen: {File}", file.FileName);
                lock (results)
                {
                    results.Add(new FingerprintResult(
                        new FingerprintedFile(file, Md5: ""), Error: ex.Message));
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        Log.Information("Fingerprint: {Count} Dateien gehasht", results.Count);
        return results;
    }

    private static async Task<string> ComputeMd5Async(string filePath, CancellationToken ct)
    {
        using var md5 = MD5.Create();
        await using var stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 8192, useAsync: true);
        var hash = await md5.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>
/// Result of fingerprinting a single file.
/// Error is non-null if MD5 computation failed.
/// </summary>
public sealed record FingerprintResult(
    FingerprintedFile File,
    string? Error);
