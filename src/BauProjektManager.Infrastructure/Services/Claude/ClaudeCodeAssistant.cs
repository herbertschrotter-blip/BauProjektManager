using System.Text.RegularExpressions;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.Claude;
using Serilog;

namespace BauProjektManager.Infrastructure.Services.Claude;

/// <summary>
/// Adapter fuer den Port <see cref="IClaudeAssistant"/>: fuehrt Claude Code ("claude -p",
/// stream-json) als lokalen Prozess im Auftragsordner. Kein HttpClient, kein API-Key —
/// die CLI nutzt Herberts Anmeldung (Abo). Vorbild: tools/chat_dienst.py (ha-hausbau).
/// </summary>
public sealed partial class ClaudeCodeAssistant : IClaudeAssistant
{
    /// <summary>Laengster Ordnername, den Claude Code unveraendert bildet (darueber kuerzt es mit Hash).</summary>
    private const int MaxProjectFolderName = 200;

    private readonly ClaudeAssistantOptions _options;
    private readonly IClaudeProcessRunner _runner;
    private readonly IFileSystemReader _fileSystem;
    private readonly IFileSystemWriter? _writer;

    /// <param name="writer">Zum Loeschen der Sitzungsdaten; NULL = <see cref="DeleteSessionData"/> tut nichts.</param>
    public ClaudeCodeAssistant(ClaudeAssistantOptions options, IClaudeProcessRunner runner, IFileSystemReader fileSystem,
        IFileSystemWriter? writer = null)
    {
        _options = options;
        _runner = runner;
        _fileSystem = fileSystem;
        _writer = writer;
    }

    public async Task<ClaudeAvailability> CheckAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        var executable = ResolveExecutable();
        if (executable is null)
            return new ClaudeAvailability(false, null, NotFoundMessage());

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.AvailabilityTimeout);
        IClaudeProcess? process = null;
        var exited = false;
        try
        {
            process = _runner.Start(new ClaudeProcessStartInfo(executable, ["--version"], AppContext.BaseDirectory));
            process.CloseInput();
            _ = DrainErrorsAsync(process);
            var firstLine = await process.ReadOutputLineAsync(timeout.Token);
            var exitCode = await process.WaitForExitAsync(timeout.Token);
            exited = true;
            if (exitCode == 0 && !string.IsNullOrWhiteSpace(firstLine))
            {
                var version = firstLine.Trim();
                Log.Information("Claude: verfuegbar, Version {Version}", version);
                return new ClaudeAvailability(true, version, $"Claude Code {version} ist bereit.");
            }
            Log.Warning("Claude: Versionsabfrage mit Code {ExitCode}", exitCode);
            return new ClaudeAvailability(false, null, $"Claude Code antwortet nicht wie erwartet (Code {exitCode}).");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Warning("Claude: Versionsabfrage Zeitueberschreitung");
            return new ClaudeAvailability(false, null, "Claude Code antwortet nicht (Zeitüberschreitung).");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Exception-Text von Process.Start enthaelt den vollen Pfad — nur den Typ loggen
            Log.Warning("Claude: Versionsabfrage fehlgeschlagen ({Error})", ex.GetType().Name);
            return new ClaudeAvailability(false, null, "Claude Code lässt sich nicht starten.");
        }
        finally
        {
            // Zeitueberschreitung oder Abbruch durch den Aufrufer: keinen haengenden Prozess zuruecklassen
            if (!exited)
                process?.Kill();
            process?.Dispose();
        }
    }

    public async Task<IClaudeSession> StartSessionAsync(
        ClaudeSessionRequest request,
        IProgress<ClaudeEvent> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(events);

        if (string.IsNullOrWhiteSpace(request.WorkingDirectory)
            || !Path.IsPathFullyQualified(request.WorkingDirectory)
            || !_fileSystem.DirectoryExists(request.WorkingDirectory))
            throw new ClaudeAssistantException("Der Auftragsordner existiert nicht.");
        if (!IsInsideJobRoot(request.WorkingDirectory, _options.JobRootDirectory))
            throw new ClaudeAssistantException("Der Auftragsordner liegt nicht im Ordner für Claude-Aufträge.");
        if (!string.IsNullOrWhiteSpace(request.SystemPromptFile)
            && (!Path.IsPathFullyQualified(request.SystemPromptFile)
                || IsInsideJobRoot(request.SystemPromptFile, request.WorkingDirectory)
                || !_fileSystem.FileExists(request.SystemPromptFile)))
            throw new ClaudeAssistantException("Die Datei mit den Regeln für Claude fehlt oder liegt im Auftragsordner.");

        var resume = !string.IsNullOrWhiteSpace(request.ResumeSessionId);
        if (resume && !Guid.TryParse(request.ResumeSessionId, out _))
            throw new ClaudeAssistantException("Die Session-ID zum Fortsetzen ist ungültig.");
        if (!resume && string.IsNullOrWhiteSpace(request.FirstMessage))
            throw new ClaudeAssistantException("Der Auftrag hat keinen Text.");
        if ((request.FirstMessage?.Length ?? 0) > _options.MaxMessageLength)
            throw new ClaudeAssistantException(
                $"Der Auftragstext ist länger als {_options.MaxMessageLength} Zeichen.");

        var executable = ResolveExecutable() ?? throw new ClaudeAssistantException(NotFoundMessage());
        var sessionId = resume ? request.ResumeSessionId! : Guid.NewGuid().ToString();
        var session = new ClaudeCodeSession(_runner, _options, executable, request, sessionId, resume, events);
        try
        {
            await session.StartAsync(request.FirstMessage, cancellationToken);
        }
        catch
        {
            await session.EndAsync();
            throw;
        }
        return session;
    }

    public void DeleteSessionData(string workingDirectory)
    {
        if (_writer is null || !IsInsideJobRoot(workingDirectory, _options.JobRootDirectory))
            return;
        var ordner = SessionDataDirectory(workingDirectory);
        var name = ClaudeCodeSession.FolderName(workingDirectory);
        if (ordner is null)
        {
            Log.Warning("Claude: Sitzungsdaten zu Auftrag {Folder} nicht geloescht (Pfad zu lang)", name);
            return;
        }
        try
        {
            if (!_fileSystem.DirectoryExists(ordner))
                return;
            var dateien = _fileSystem.EnumerateFiles(ordner, "*", recursive: true).ToList();
            foreach (var datei in dateien)
                _writer.DeleteFile(datei);
            if (dateien.Count > 0)
                Log.Information("Claude: Sitzungsdaten zu Auftrag {Folder} geloescht ({Count} Datei(en))", name, dateien.Count);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning("Claude: Sitzungsdaten zu Auftrag {Folder} nicht geloescht ({Error})", name, ex.GetType().Name);
        }
    }

    /// <summary>
    /// Ordner, in dem Claude Code die Sitzungen zu <paramref name="workingDirectory"/> ablegt:
    /// &lt;Konfig&gt;/projects/&lt;Pfad mit '-' statt jedem Zeichen ausser A-Z, a-z, 0-9&gt;.
    /// NULL, wenn der Name so lang ist, dass Claude Code ihn kuerzt (dann nicht sicher bestimmbar).
    /// </summary>
    internal string? SessionDataDirectory(string workingDirectory)
    {
        var name = ProjectFolderName(workingDirectory);
        if (name.Length > MaxProjectFolderName)
            return null;
        var config = string.IsNullOrWhiteSpace(_options.ConfigDirectory) ? DefaultConfigDirectory() : _options.ConfigDirectory;
        return Path.Combine(config, "projects", name);
    }

    /// <summary>Ordnername wie bei Claude Code: voller Pfad, jedes Zeichen ausser A-Z, a-z, 0-9 wird '-'.</summary>
    internal static string ProjectFolderName(string workingDirectory)
        => NichtAlphanumerisch().Replace(Path.TrimEndingDirectorySeparator(Path.GetFullPath(workingDirectory)), "-");

    private static string DefaultConfigDirectory()
    {
        var env = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
        return string.IsNullOrWhiteSpace(env)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude")
            : env;
    }

    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex NichtAlphanumerisch();

    /// <summary>
    /// True, wenn <paramref name="path"/> ein echter Unterpfad von <paramref name="root"/> ist (normalisiert, ohne
    /// ".."-Flucht, nie der Basisordner selbst; ein Laufwerks-Root als Basis gilt nicht). Pure Pfad-String-Operation.
    /// </summary>
    internal static bool IsInsideJobRoot(string path, string? root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Path.IsPathFullyQualified(root) || !Path.IsPathFullyQualified(path))
            return false;
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var driveRoot = Path.TrimEndingDirectorySeparator(Path.GetPathRoot(fullRoot) ?? string.Empty);
        if (string.Equals(fullRoot, driveRoot, StringComparison.OrdinalIgnoreCase))
            return false;
        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && fullPath.Length > fullRoot.Length + 1;
    }

    private string? ResolveExecutable() =>
        ClaudeCommandLine.ResolveExecutable(
            _options.ExecutablePath,
            Environment.GetEnvironmentVariable("PATH"),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            _fileSystem.FileExists);

    private string NotFoundMessage() =>
        string.IsNullOrWhiteSpace(_options.ExecutablePath)
            ? "Claude Code wurde nicht gefunden. Bitte installieren oder den Pfad zur claude.exe in den Einstellungen angeben."
            : "Der eingestellte Pfad zur claude.exe existiert nicht.";

    /// <summary>stderr leer lesen, damit eine volle Pipe "claude --version" nicht blockiert.</summary>
    private static async Task DrainErrorsAsync(IClaudeProcess process)
    {
        try
        {
            while (await process.ReadErrorLineAsync(CancellationToken.None) is not null)
            {
            }
        }
        catch (Exception)
        {
            // Prozess beendet/entsorgt — nichts mehr zu lesen
        }
    }
}
