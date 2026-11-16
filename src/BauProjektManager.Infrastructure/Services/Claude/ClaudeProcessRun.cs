using System.Text.Json.Nodes;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models.Claude;
using Serilog;

namespace BauProjektManager.Infrastructure.Services.Claude;

/// <summary>
/// Rueckkanal eines <see cref="ClaudeProcessRun"/> zur Sitzung.
/// </summary>
internal interface IClaudeRunHost
{
    /// <summary>Ereignis an den Empfaenger weitergeben (wirft nicht).</summary>
    void Raise(ClaudeEvent claudeEvent);

    /// <summary>True, solange die Sitzung nicht endgueltig beendet ist.</summary>
    bool CanContinue { get; }

    /// <summary>Claude hat eine Runde abgeschlossen — die Sitzung ist bei Claude gespeichert.</summary>
    void OnTurnConfirmed(string? sessionId);

    /// <summary>system/init: Claude hat die Sitzungsdatei angelegt — ein Neustart braucht --resume.</summary>
    void OnSessionCreated(string? sessionId);

    /// <summary>
    /// Der Prozess endete mit Fehler, ohne eine Zeile auszugeben (z. B. "Session ID already in use" oder
    /// "No conversation found"): der naechste Start nimmt die andere Art (--resume bzw. --session-id).
    /// </summary>
    void OnStartRejected();

    /// <summary>
    /// Der Waechter meldet Leerlauf. Die Sitzung prueft unter ihrer Sperre erneut (eine gerade
    /// gesendete Nachricht gewinnt) und beendet den Lauf ggf. True = Lauf wurde beendet.
    /// </summary>
    Task<bool> OnIdleAsync(ClaudeProcessRun run);
}

/// <summary>
/// Ein Lauf des Claude-Prozesses innerhalb einer Sitzung: liest stdout (stream-json) und stderr,
/// zaehlt offene Runden und bewacht Leerlauf und Stille (Muster: Lauf in tools/chat_dienst.py).
/// Endet der Lauf, kann die Sitzung mit einem neuen Lauf (--resume) fortgesetzt werden.
/// </summary>
internal sealed class ClaudeProcessRun : IDisposable
{
    private readonly IClaudeProcess _process;
    private readonly ClaudeAssistantOptions _options;
    private readonly string _folderName;
    private readonly IClaudeRunHost _host;
    private readonly CancellationTokenSource _watchCts = new();
    private readonly TaskCompletionSource _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task _errorReader = Task.CompletedTask;
    private long _lastOutput;
    private long _lastActivity;
    private int _openTurns;
    private volatile bool _stoppedByUs;
    private volatile bool _outputEnded;
    private volatile bool _sawOutput;
    private volatile string? _lastErrorLine;

    /// <param name="process">Gestarteter Prozess.</param>
    /// <param name="options">Zeitgrenzen.</param>
    /// <param name="folderName">Nur der Ordnername des Auftrags — fuer Logs.</param>
    /// <param name="host">Rueckkanal zur Sitzung.</param>
    public ClaudeProcessRun(IClaudeProcess process, ClaudeAssistantOptions options, string folderName, IClaudeRunHost host)
    {
        _process = process;
        _options = options;
        _folderName = folderName;
        _host = host;
    }

    /// <summary>Wird erfuellt, wenn der Prozess beendet und alle Ereignisse gemeldet sind.</summary>
    public Task Finished => _finished.Task;

    /// <summary>Laeuft, wird nicht gerade beendet und liest noch (nach stdout-Ende nimmt er nichts mehr an).</summary>
    public bool IsAlive => !_stoppedByUs && !_outputEnded && !_finished.Task.IsCompleted;

    /// <summary>Gesendete Nachrichten ohne fertige Runde.</summary>
    public int OpenTurns => Volatile.Read(ref _openTurns);

    private static long Now => Environment.TickCount64;

    private long IdleMs => (long)_options.IdleTimeout.TotalMilliseconds;

    /// <summary>Startet Lesen und Waechter.</summary>
    public void Begin()
    {
        Interlocked.Exchange(ref _lastOutput, Now);
        Interlocked.Exchange(ref _lastActivity, Now);
        // Token hier holen: endet der Lauf sehr schnell, ist die Quelle beim Start des Waechters schon entsorgt
        var watchToken = _watchCts.Token;
        _errorReader = Task.Run(ReadErrorsAsync);
        _ = Task.Run(ReadOutputAsync);
        _ = Task.Run(() => WatchAsync(watchToken));
    }

    /// <summary>
    /// Schickt eine Nachricht als User an Claude und oeffnet eine Runde. Bewusst ohne
    /// CancellationToken: ein Abbruch mitten im Schreiben liesse eine halbe JSON-Zeile auf stdin.
    /// </summary>
    /// <exception cref="ClaudeAssistantException">Prozess nimmt nichts mehr an.</exception>
    public async Task WriteMessageAsync(string text)
    {
        var line = new JsonObject
        {
            ["type"] = "user",
            ["message"] = new JsonObject { ["role"] = "user", ["content"] = text }
        }.ToJsonString();

        // Runde vor dem Schreiben zaehlen: ein schnelles "result" darf nicht vor dem Zaehlen ankommen
        Interlocked.Increment(ref _openTurns);
        try
        {
            await _process.WriteInputLineAsync(line, CancellationToken.None);
        }
        catch (Exception ex)
        {
            DecrementOpenTurns();
            throw new ClaudeAssistantException("Claude nimmt keine Nachricht an (Prozess beendet).", ex);
        }

        Interlocked.Exchange(ref _lastOutput, Now);
        Interlocked.Exchange(ref _lastActivity, Now);
    }

    /// <summary>Beendet den Lauf, wenn er weiterhin ohne offene Runde und lange genug untaetig ist.</summary>
    public async Task<bool> StopIfIdleAsync()
    {
        if (!IsAlive || OpenTurns > 0 || Now - Interlocked.Read(ref _lastActivity) < IdleMs)
            return false;
        Log.Information("Claude: Leerlauf, Prozess fuer Auftrag {Folder} wird beendet", _folderName);
        await StopAsync();
        return true;
    }

    /// <summary>
    /// Beendet den Prozess: stdin schliessen (claude -p endet dann selbst), nach der Wartezeit
    /// den Prozessbaum hart beenden. Wartet, bis alle Ereignisse gemeldet sind.
    /// </summary>
    public async Task StopAsync()
    {
        _stoppedByUs = true;
        if (!_finished.Task.IsCompleted)
        {
            _process.CloseInput();
            var done = await Task.WhenAny(_finished.Task, Task.Delay(_options.StopGracePeriod));
            if (done != _finished.Task)
                _process.Kill();
        }
        await _finished.Task;
    }

    public void Dispose()
    {
        _watchCts.Dispose();
        _process.Dispose();
    }

    private async Task ReadOutputAsync()
    {
        try
        {
            while (await _process.ReadOutputLineAsync(CancellationToken.None) is { } line)
            {
                Interlocked.Exchange(ref _lastOutput, Now);
                _sawOutput = true;
                var parsed = ClaudeStreamJsonParser.Parse(line);
                if (parsed.IsInit)
                    _host.OnSessionCreated(parsed.SessionId);
                foreach (var ev in parsed.Events)
                {
                    if (ev.Kind == ClaudeEventKind.TurnCompleted)
                    {
                        // Ein result schliesst alles Gesendete ab (die CLI kann Nachrichten zusammenfassen)
                        Volatile.Write(ref _openTurns, 0);
                        Interlocked.Exchange(ref _lastActivity, Now);
                        _host.OnTurnConfirmed(parsed.SessionId);
                    }
                    _host.Raise(ev);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning("Claude: Ausgabe fuer Auftrag {Folder} nicht lesbar ({Error})", _folderName, ex.GetType().Name);
        }
        finally
        {
            _outputEnded = true;
            await OnExitedAsync();
        }
    }

    private async Task ReadErrorsAsync()
    {
        try
        {
            while (await _process.ReadErrorLineAsync(CancellationToken.None) is { } line)
            {
                // stderr kann Pfade enthalten: nicht loggen, nur fuer die Fehlermeldung merken
                if (!string.IsNullOrWhiteSpace(line))
                    _lastErrorLine = line.Trim();
            }
        }
        catch (Exception)
        {
            // stderr ist nur Zusatzinfo — ein Lesefehler aendert nichts am Ablauf
        }
    }

    private async Task WatchAsync(CancellationToken token)
    {
        var silenceMs = (long)_options.TurnSilenceTimeout.TotalMilliseconds;
        var tick = TimeSpan.FromMilliseconds(Math.Clamp(Math.Min(IdleMs, silenceMs) / 5, 20, 30_000));
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(tick, token);
                if (!IsAlive)
                    continue;

                var now = Now;
                if (OpenTurns == 0 && now - Interlocked.Read(ref _lastActivity) >= IdleMs)
                {
                    if (await _host.OnIdleAsync(this))
                        return;
                }
                else if (OpenTurns > 0 && now - Interlocked.Read(ref _lastOutput) >= silenceMs)
                {
                    Log.Warning("Claude: keine Ausgabe fuer Auftrag {Folder}, Runde abgebrochen", _folderName);
                    _host.Raise(new ClaudeEvent(
                        ClaudeEventKind.Error,
                        Text: $"Claude hat {DurationText(_options.TurnSilenceTimeout)} lang nichts geliefert – "
                            + "Runde abgebrochen. Schreib noch einmal, dann geht es im selben Gespräch weiter."));
                    await StopAsync();
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Lauf beendet
        }
    }

    private async Task OnExitedAsync()
    {
        int exitCode;
        try
        {
            exitCode = await _process.WaitForExitAsync(CancellationToken.None);
        }
        catch (Exception)
        {
            exitCode = -1;
        }

        await Task.WhenAny(_errorReader, Task.Delay(TimeSpan.FromSeconds(2)));
        _watchCts.Cancel();
        var turnLost = Volatile.Read(ref _openTurns) > 0;
        Volatile.Write(ref _openTurns, 0);
        Log.Information("Claude: Prozess fuer Auftrag {Folder} beendet (Code {ExitCode})", _folderName, exitCode);

        if (exitCode != 0 && !_stoppedByUs)
        {
            var text = $"Claude ist beendet (Code {exitCode})";
            if (_lastErrorLine is { } detail)
                text += $": {detail}";
            _host.Raise(new ClaudeEvent(ClaudeEventKind.Error, Text: text));
            if (!_sawOutput)
                _host.OnStartRejected();
        }
        else if (turnLost && !_stoppedByUs)
        {
            _host.Raise(new ClaudeEvent(
                ClaudeEventKind.Error,
                Text: "Claude hat die Runde ohne Antwort beendet. Schreib noch einmal, dann geht es im selben Gespräch weiter."));
        }
        _host.Raise(new ClaudeEvent(ClaudeEventKind.Exited, ExitCode: exitCode, CanContinue: _host.CanContinue));
        _finished.TrySetResult();
    }

    private void DecrementOpenTurns()
    {
        int current;
        do
        {
            current = Volatile.Read(ref _openTurns);
            if (current == 0)
                return;
        }
        while (Interlocked.CompareExchange(ref _openTurns, current - 1, current) != current);
    }

    internal static string DurationText(TimeSpan duration) =>
        duration.TotalSeconds >= 90
            ? $"{Math.Round(duration.TotalMinutes)} Minuten"
            : $"{Math.Max(1, Math.Round(duration.TotalSeconds))} Sekunden";
}
