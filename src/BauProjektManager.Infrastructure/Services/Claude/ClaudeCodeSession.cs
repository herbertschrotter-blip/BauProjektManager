using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.Claude;
using Serilog;

namespace BauProjektManager.Infrastructure.Services.Claude;

/// <summary>
/// Sitzung mit Claude Code zu einem Auftragsordner. Haelt hoechstens einen laufenden Prozess
/// (<see cref="ClaudeProcessRun"/>); ist er beendet (Leerlauf, abgebrochene Runde, Absturz),
/// startet die naechste Nachricht ihn neu — mit "--resume", sobald Claude die Sitzung
/// gespeichert hat (erste fertige Runde), sonst erneut mit "--session-id".
/// Erzeugt nur von <see cref="ClaudeCodeAssistant"/>.
/// </summary>
public sealed class ClaudeCodeSession : IClaudeSession, IClaudeRunHost
{
    private readonly IClaudeProcessRunner _runner;
    private readonly ClaudeAssistantOptions _options;
    private readonly string _executable;
    private readonly string _workingDirectory;
    private readonly string _folderName;
    private readonly string? _systemPromptFile;
    private readonly string? _model;
    private readonly IProgress<ClaudeEvent> _events;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ClaudeProcessRun? _run;
    private volatile string _sessionId;
    private volatile bool _savedAtClaude;
    private volatile bool _ended;

    internal ClaudeCodeSession(
        IClaudeProcessRunner runner,
        ClaudeAssistantOptions options,
        string executable,
        ClaudeSessionRequest request,
        string sessionId,
        bool resume,
        IProgress<ClaudeEvent> events)
    {
        _runner = runner;
        _options = options;
        _executable = executable;
        _workingDirectory = request.WorkingDirectory;
        _folderName = FolderName(request.WorkingDirectory);
        _systemPromptFile = request.SystemPromptFile;
        _model = request.Model;
        _events = events;
        _sessionId = sessionId;
        _savedAtClaude = resume;
    }

    public string SessionId => _sessionId;

    public bool IsTurnOpen => _run is { } run && run.OpenTurns > 0 && !run.Finished.IsCompleted;

    public bool IsEnded => _ended;

    bool IClaudeRunHost.CanContinue => !_ended;

    /// <summary>Startet den ersten Prozess und sendet ggf. die erste Nachricht.</summary>
    internal async Task StartAsync(string? firstMessage, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var run = await EnsureRunningAsync();
            if (!string.IsNullOrWhiteSpace(firstMessage))
                await run.WriteMessageAsync(firstMessage);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SendMessageAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ClaudeAssistantException("Die Nachricht ist leer.");
        if (text.Length > _options.MaxMessageLength)
            throw new ClaudeAssistantException(
                $"Die Nachricht ist länger als {_options.MaxMessageLength} Zeichen.");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_ended)
                throw new ClaudeAssistantException("Die Sitzung mit Claude ist beendet.");
            if (_run is { IsAlive: true, OpenTurns: > 0 })
                throw new ClaudeAssistantException("Claude arbeitet noch. Bitte die Antwort abwarten.");
            var run = await EnsureRunningAsync();
            await run.WriteMessageAsync(text);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task EndAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_ended)
                return;
            _ended = true;
            if (_run is { } run)
            {
                await run.StopAsync();
                run.Dispose();
                _run = null;
            }
            Log.Information("Claude: Sitzung {SessionId} fuer Auftrag {Folder} beendet", SessionId, _folderName);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync() => await EndAsync();

    void IClaudeRunHost.Raise(ClaudeEvent claudeEvent)
    {
        try
        {
            _events.Report(claudeEvent);
        }
        catch (Exception ex)
        {
            // Ein Fehler beim Empfaenger darf das Lesen der Ausgabe nicht abbrechen
            Log.Warning("Claude: Ereignis {Kind} nicht zugestellt ({Error})", claudeEvent.Kind, ex.GetType().Name);
        }
    }

    void IClaudeRunHost.OnTurnConfirmed(string? sessionId)
    {
        _savedAtClaude = true;
        if (sessionId is not null && sessionId != _sessionId && Guid.TryParse(sessionId, out _))
        {
            Log.Information("Claude: Sitzung {OldId} meldet neue Session-ID {NewId}", _sessionId, sessionId);
            _sessionId = sessionId;
        }
    }

    void IClaudeRunHost.OnSessionCreated(string? sessionId)
    {
        // Ab system/init liegt die Sitzungsdatei bei Claude: ein Neustart mit --session-id scheiterte jetzt
        _savedAtClaude = true;
        if (sessionId is not null && sessionId != _sessionId && Guid.TryParse(sessionId, out _))
            _sessionId = sessionId;
    }

    void IClaudeRunHost.OnStartRejected()
    {
        _savedAtClaude = !_savedAtClaude;
        Log.Information("Claude: Start fuer Auftrag {Folder} abgelehnt, naechster Versuch mit {Mode}",
            _folderName, _savedAtClaude ? "--resume" : "--session-id");
    }

    async Task<bool> IClaudeRunHost.OnIdleAsync(ClaudeProcessRun run)
    {
        // Unter der Sperre: eine gleichzeitig gesendete Nachricht oeffnet erst ihre Runde, dann prueft der Lauf
        await _gate.WaitAsync();
        try
        {
            return ReferenceEquals(_run, run) && await run.StopIfIdleAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Laufender Prozess oder ein neuer.</summary>
    private async Task<ClaudeProcessRun> EnsureRunningAsync()
    {
        if (_run is { IsAlive: true } alive)
            return alive;

        if (_run is { } old)
        {
            await old.Finished;
            old.Dispose();
            _run = null;
        }

        var resume = _savedAtClaude;
        var arguments = ClaudeCommandLine.BuildSessionArguments(
            _options.Tools, SessionId, resume, _systemPromptFile, _model);

        IClaudeProcess process;
        try
        {
            process = _runner.Start(new ClaudeProcessStartInfo(_executable, arguments, _workingDirectory));
        }
        catch (Exception ex)
        {
            // Exception-Text von Process.Start enthaelt den vollen Pfad (Benutzername) — nur den Typ loggen
            Log.Warning("Claude: Start fuer Auftrag {Folder} fehlgeschlagen ({Error})", _folderName, ex.GetType().Name);
            throw new ClaudeAssistantException("Claude Code lässt sich nicht starten.", ex);
        }

        Log.Information("Claude: Sitzung {SessionId} fuer Auftrag {Folder} gestartet ({Mode})",
            SessionId, _folderName, resume ? "fortgesetzt" : "neu");

        var run = new ClaudeProcessRun(process, _options, _folderName, this);
        _run = run;
        run.Begin();
        return run;
    }

    /// <summary>Nur der Ordnername (DSGVO-Pfadregel) — pure String-Operation ohne Disk-Zugriff.</summary>
    internal static string FolderName(string path)
    {
        var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrEmpty(name) ? "…" : name;
    }
}
