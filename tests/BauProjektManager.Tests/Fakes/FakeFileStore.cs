using System.IO;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;

namespace BauProjektManager.Tests.Fakes;

/// <summary>
/// In-Memory-Fake der drei Dateisystem-Ports (ADR-060, BPM-112) fuer Unit-Tests
/// ohne Festplattenzugriff. Pfad-Operationen delegieren an System.IO.Path
/// (deterministisch, plattform-korrekt = identisch zu LocalFileSystem).
///
/// Verhaltenstreue zum echten Adapter: MoveFile/CopyFile verlangen ein
/// existierendes Zielverzeichnis und werfen bei vorhandenem Ziel ohne overwrite;
/// DeleteFile auf fehlende Datei ist No-Op; CreateDirectory ist idempotent.
/// </summary>
public sealed class FakeFileStore : IFileSystemReader, IFileSystemWriter, IPathService
{
    /// <summary>Dateioperationen, auf die per <see cref="FailNext"/> Fehler injiziert werden koennen.</summary>
    public enum FileOp { Move, Copy, Delete, CreateDirectory, OpenRead, Write }

    private sealed class FaultRule
    {
        public required FileOp Op { get; init; }
        public required string PathContains { get; init; }
        public required Exception Exception { get; init; }
        public int Remaining { get; set; }
    }

    private readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _times = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _dirs = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _hidden = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FaultRule> _faults = [];

    // Fester Zeitstempel statt DateTime.UtcNow — deterministische Tests.
    private static readonly DateTime SeedTime = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Test-Helfer: legt eine Quelldatei mit Inhalt an (inkl. Elternverzeichnisse).</summary>
    public void AddFile(string path, byte[]? content = null)
    {
        var norm = Norm(path);
        EnsureDir(Norm(Path.GetDirectoryName(norm) ?? string.Empty));
        _files[norm] = content ?? Array.Empty<byte>();
        _times[norm] = SeedTime;
    }

    /// <summary>
    /// Fault-Injektion (BPM-120 T1, AK 3): laesst die naechsten <paramref name="times"/>
    /// Aufrufe von <paramref name="op"/>, deren Pfad (Quelle ODER Ziel)
    /// <paramref name="pathContains"/> enthaelt, mit <paramref name="exception"/>
    /// fehlschlagen — VOR jeder Mutation des Stores.
    /// </summary>
    public void FailNext(FileOp op, string pathContains, Exception? exception = null, int times = 1)
        => _faults.Add(new FaultRule
        {
            Op = op,
            PathContains = pathContains,
            Exception = exception ?? new IOException($"Injizierter Fehler: {op} auf '{pathContains}'"),
            Remaining = times
        });

    private void ThrowIfFault(FileOp op, params string[] paths)
    {
        var rule = _faults.FirstOrDefault(f => f.Op == op && f.Remaining > 0
            && paths.Any(p => p.Contains(f.PathContains, StringComparison.OrdinalIgnoreCase)));
        if (rule is null)
            return;
        rule.Remaining--;
        throw rule.Exception;
    }

    // --- IFileSystemReader ---

    public bool FileExists(string path) => _files.ContainsKey(Norm(path));

    public bool DirectoryExists(string path) => _dirs.Contains(Norm(path));

    public bool IsHidden(string path) => _hidden.Contains(Norm(path));

    /// <summary>Markiert einen (existierenden oder spaeter angelegten) Pfad als versteckt (BPM-066).</summary>
    public void SetHidden(string path) => _hidden.Add(Norm(path));

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", bool recursive = false)
    {
        var baseDir = Norm(path);
        foreach (var file in _files.Keys)
        {
            var dir = Norm(Path.GetDirectoryName(file) ?? string.Empty);
            var match = recursive ? (dir == baseDir || IsUnder(baseDir, file)) : dir == baseDir;
            if (match && MatchesPattern(Path.GetFileName(file), searchPattern))
                yield return file;
        }
    }

    public IEnumerable<string> EnumerateDirectories(string path, bool recursive = false)
    {
        var baseDir = Norm(path);
        foreach (var dir in _dirs)
        {
            if (dir == baseDir)
                continue;

            var parent = Norm(Path.GetDirectoryName(dir) ?? string.Empty);
            var match = recursive ? IsUnder(baseDir, dir) : parent == baseDir;
            if (match)
                yield return dir;
        }
    }

    public FileInfoSnapshot GetFileInfo(string path)
    {
        var norm = Norm(path);
        return _files.TryGetValue(norm, out var bytes)
            ? new FileInfoSnapshot(norm, true, bytes.Length, _times[norm])
            : new FileInfoSnapshot(norm, false, 0, default);
    }

    public Stream OpenRead(string path)
    {
        var norm = Norm(path);
        ThrowIfFault(FileOp.OpenRead, norm);
        if (!_files.TryGetValue(norm, out var bytes))
            throw new FileNotFoundException("Datei nicht im Fake-Store.", norm);

        return new MemoryStream(bytes, writable: false);
    }

    public string ReadAllText(string path)
    {
        var norm = Norm(path);
        ThrowIfFault(FileOp.OpenRead, norm);
        if (!_files.TryGetValue(norm, out var bytes))
            throw new FileNotFoundException("Datei nicht im Fake-Store.", norm);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    // --- IFileSystemWriter ---

    public void WriteAllText(string path, string content)
    {
        var norm = Norm(path);
        ThrowIfFault(FileOp.Write, norm);
        // Verhaltenstreue: File.WriteAllText verlangt ein existierendes Verzeichnis.
        RequireDestinationDirectory(norm);
        _files[norm] = System.Text.Encoding.UTF8.GetBytes(content);
        _times[norm] = SeedTime;
    }

    public void CreateDirectory(string path)
    {
        var norm = Norm(path);
        ThrowIfFault(FileOp.CreateDirectory, norm);
        EnsureDir(norm);
    }

    public void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        var src = Norm(sourcePath);
        var dst = Norm(destinationPath);
        ThrowIfFault(FileOp.Move, src, dst);
        RequireSource(src);
        RequireDestinationDirectory(dst);
        RequireFreeOrOverwrite(dst, overwrite);

        _files[dst] = _files[src];
        _times[dst] = _times[src];
        _files.Remove(src);
        _times.Remove(src);
    }

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        var src = Norm(sourcePath);
        var dst = Norm(destinationPath);
        ThrowIfFault(FileOp.Copy, src, dst);
        RequireSource(src);
        RequireDestinationDirectory(dst);
        RequireFreeOrOverwrite(dst, overwrite);

        _files[dst] = (byte[])_files[src].Clone();
        _times[dst] = _times[src];
    }

    public void DeleteFile(string path)
    {
        var norm = Norm(path);
        ThrowIfFault(FileOp.Delete, norm);
        _files.Remove(norm);
        _times.Remove(norm);
    }

    // --- IPathService (delegiert) ---

    public string Combine(params string[] paths) => Path.Combine(paths);

    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);

    public string GetFileName(string path) => Path.GetFileName(path);

    public string GetExtension(string path) => Path.GetExtension(path);

    public string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);

    public string GetRelativePath(string relativeTo, string path) => Path.GetRelativePath(relativeTo, path);

    // --- intern ---

    private void RequireSource(string src)
    {
        if (!_files.ContainsKey(src))
            throw new FileNotFoundException("Quelldatei nicht im Fake-Store.", src);
    }

    private void RequireDestinationDirectory(string dst)
    {
        var parent = Norm(Path.GetDirectoryName(dst) ?? string.Empty);
        if (!_dirs.Contains(parent))
            throw new DirectoryNotFoundException($"Zielverzeichnis existiert nicht: {parent}");
    }

    private void RequireFreeOrOverwrite(string dst, bool overwrite)
    {
        if (_files.ContainsKey(dst) && !overwrite)
            throw new IOException($"Zieldatei existiert bereits: {dst}");
    }

    private void EnsureDir(string dir)
    {
        var current = dir;
        while (!string.IsNullOrEmpty(current) && _dirs.Add(current))
        {
            var parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent))
                break;

            var normParent = Norm(parent);
            if (normParent == current)
                break;

            current = normParent;
        }
    }

    private static string Norm(string path)
        => string.IsNullOrEmpty(path)
            ? string.Empty
            : Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private static bool IsUnder(string baseDir, string candidate)
    {
        if (candidate.Length <= baseDir.Length)
            return false;

        var prefix = baseDir.EndsWith(Path.DirectorySeparatorChar)
            ? baseDir
            : baseDir + Path.DirectorySeparatorChar;
        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesPattern(string name, string searchPattern)
    {
        if (string.IsNullOrEmpty(searchPattern) || searchPattern == "*")
            return true;

        if (searchPattern.StartsWith("*."))
            return name.EndsWith(searchPattern[1..], StringComparison.OrdinalIgnoreCase);

        return string.Equals(name, searchPattern, StringComparison.OrdinalIgnoreCase);
    }
}
