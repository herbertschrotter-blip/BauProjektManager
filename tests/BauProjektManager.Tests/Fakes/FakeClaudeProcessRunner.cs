using System.Collections.Concurrent;
using System.IO;
using System.Threading.Channels;
using BauProjektManager.Domain.Models.Claude;
using BauProjektManager.Infrastructure.Services.Claude;

namespace BauProjektManager.Tests.Fakes;

/// <summary>
/// Fake fuer die Prozess-Naht: kein echtes claude. Jeder Start liefert einen
/// <see cref="FakeClaudeProcess"/>, den der Test steuert (Zeilen ausgeben, beenden).
/// </summary>
internal sealed class FakeClaudeProcessRunner : IClaudeProcessRunner
{
    private readonly ConcurrentQueue<FakeClaudeProcess> _started = new();

    public List<ClaudeProcessStartInfo> StartInfos { get; } = [];

    /// <summary>Wirft beim Start (simuliert fehlende Datei).</summary>
    public Exception? StartException { get; set; }

    /// <summary>Wird fuer jeden neuen Prozess aufgerufen (z. B. um Ausgaben vorzubelegen).</summary>
    public Action<FakeClaudeProcess>? OnStart { get; set; }

    public IReadOnlyList<FakeClaudeProcess> Processes => _started.ToList();

    public FakeClaudeProcess Last => _started.Last();

    public IClaudeProcess Start(ClaudeProcessStartInfo startInfo)
    {
        if (StartException is not null)
            throw StartException;
        lock (StartInfos)
            StartInfos.Add(startInfo);
        var process = new FakeClaudeProcess();
        OnStart?.Invoke(process);
        _started.Enqueue(process);
        return process;
    }
}

/// <summary>
/// Steuerbarer Fake-Prozess. CloseInput beendet ihn (wie claude -p bei stdin-EOF) mit Code 0,
/// ausser <see cref="IgnoreCloseInput"/> ist gesetzt; Kill beendet mit Code -1.
/// </summary>
internal sealed class FakeClaudeProcess : IClaudeProcess
{
    private readonly Channel<string> _stdout = Channel.CreateUnbounded<string>();
    private readonly Channel<string> _stderr = Channel.CreateUnbounded<string>();
    private readonly TaskCompletionSource<int> _exit = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ConcurrentQueue<string> WrittenLines { get; } = new();

    public bool InputClosed { get; private set; }

    public bool Killed { get; private set; }

    public bool IgnoreCloseInput { get; set; }

    public bool FailWrites { get; set; }

    public bool Disposed { get; private set; }

    public void EmitOutput(string line) => _stdout.Writer.TryWrite(line);

    public void EmitError(string line) => _stderr.Writer.TryWrite(line);

    public void Exit(int code)
    {
        _stdout.Writer.TryComplete();
        _stderr.Writer.TryComplete();
        _exit.TrySetResult(code);
    }

    public async Task<string?> ReadOutputLineAsync(CancellationToken cancellationToken) =>
        await ReadAsync(_stdout, cancellationToken);

    public async Task<string?> ReadErrorLineAsync(CancellationToken cancellationToken) =>
        await ReadAsync(_stderr, cancellationToken);

    public Task WriteInputLineAsync(string line, CancellationToken cancellationToken)
    {
        if (FailWrites || InputClosed || _exit.Task.IsCompleted)
            throw new IOException("Pipe geschlossen");
        WrittenLines.Enqueue(line);
        return Task.CompletedTask;
    }

    public void CloseInput()
    {
        InputClosed = true;
        if (!IgnoreCloseInput)
            Exit(0);
    }

    public Task<int> WaitForExitAsync(CancellationToken cancellationToken) =>
        _exit.Task.WaitAsync(cancellationToken);

    public void Kill()
    {
        Killed = true;
        Exit(-1);
    }

    public void Dispose() => Disposed = true;

    private static async Task<string?> ReadAsync(Channel<string> channel, CancellationToken cancellationToken)
    {
        while (await channel.Reader.WaitToReadAsync(cancellationToken))
        {
            if (channel.Reader.TryRead(out var line))
                return line;
        }
        return null;
    }
}

/// <summary>Sammelt Ereignisse synchron (Progress&lt;T&gt; ohne UI-Kontext waere ungeordnet).</summary>
internal sealed class CollectingProgress : IProgress<ClaudeEvent>
{
    private readonly List<ClaudeEvent> _events = [];

    public IReadOnlyList<ClaudeEvent> Events
    {
        get
        {
            lock (_events)
                return _events.ToList();
        }
    }

    public void Report(ClaudeEvent value)
    {
        lock (_events)
            _events.Add(value);
    }

    /// <summary>Wartet, bis ein passendes Ereignis da ist (Fehler nach Zeitlimit).</summary>
    public async Task<ClaudeEvent> WaitForAsync(Func<ClaudeEvent, bool> predicate, int timeoutMs = 5000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            var hit = Events.FirstOrDefault(predicate);
            if (hit is not null)
                return hit;
            await Task.Delay(10);
        }
        throw new TimeoutException("Erwartetes Claude-Ereignis kam nicht.");
    }
}
