using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.Claude;
using Serilog;

namespace BauProjektManager.Infrastructure.Services.Claude;

/// <summary>
/// Adapter fuer den Port <see cref="IClaudeAssistant"/>: fuehrt Claude Code ("claude -p",
/// stream-json) als lokalen Prozess im Auftragsordner. Kein HttpClient, kein API-Key —
/// die CLI nutzt Herberts Anmeldung (Abo). Vorbild: tools/chat_dienst.py (ha-hausbau).
/// </summary>
public sealed class ClaudeCodeAssistant : IClaudeAssistant
{
    private readonly ClaudeAssistantOptions _options;
    private readonly IClaudeProcessRunner _runner;
    private readonly IFileSystemReader _fileSystem;

    public ClaudeCodeAssistant(ClaudeAssistantOptions options, IClaudeProcessRunner runner, IFileSystemReader fileSystem)
    {
        _options = options;
        _runner = runner;
        _fileSystem = fileSystem;
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
