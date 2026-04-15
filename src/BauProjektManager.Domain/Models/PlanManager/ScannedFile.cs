using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Result of scanning a single file in the inbox.
/// Contains only filesystem metadata, no parsing or hashing yet.
/// </summary>
public sealed record ScannedFile(
    string RelativePath,
    string FileName,
    string Extension,
    long FileSize,
    DateTime LastWriteTimeUtc);

/// <summary>
/// A scanned file enriched with its MD5 hash.
/// MD5 + FileSize are ALWAYS mandatory (PlanManager invariant).
/// </summary>
public sealed record FingerprintedFile(
    ScannedFile Scan,
    string Md5);
