using System.IO;
using System.Text.Json;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.Claude;
using BauProjektManager.Infrastructure.Services.Claude;
using BauProjektManager.Tests.Fakes;

namespace BauProjektManager.Tests;

/// <summary>
/// ClaudeCodeAssistant/ClaudeCodeSession mit Fake-Prozess (kein echtes claude):
/// Kommandozeile, Ereignisse, Fortsetzen, Abbruch, Leerlauf, Stille, Fehler.
/// </summary>
public sealed class ClaudeCodeAssistantTests
{
    private const string FakeExe = @"C:\Tools\claude.exe";
    private const string JobRoot = @"C:\Auftraege";
    private const string WorkDir = @"C:\Auftraege\A-0007";
    private readonly string _workDir = WorkDir;
    private readonly FakeFileStore _fileSystem = new();
    private readonly FakeClaudeProcessRunner _runner = new();
    private readonly CollectingProgress _events = new();

    public ClaudeCodeAssistantTests()
    {
        _fileSystem.AddFile(FakeExe);
        _fileSystem.CreateDirectory(WorkDir);
    }

    private ClaudeCodeAssistant CreateAssistant(ClaudeAssistantOptions? options = null) =>
        new(options ?? new ClaudeAssistantOptions { ExecutablePath = FakeExe, JobRootDirectory = JobRoot }, _runner, _fileSystem);

    private Task<IClaudeSession> StartAsync(ClaudeCodeAssistant assistant, string message = "Auftrag lesen") =>
        assistant.StartSessionAsync(new ClaudeSessionRequest(_workDir, message), _events);

    // --- Verfuegbarkeit ---

    [Fact]
    public async Task CheckAvailability_VersionAnswers_ReturnsAvailable()
    {
        _runner.OnStart = p => { p.EmitOutput("2.1.296 (Claude Code)"); p.Exit(0); };

        var result = await CreateAssistant().CheckAvailabilityAsync();

        Assert.True(result.IsAvailable);
        Assert.Equal("2.1.296 (Claude Code)", result.Version);
        Assert.Equal(["--version"], _runner.StartInfos[0].Arguments);
    }

    [Fact]
    public async Task CheckAvailability_ConfiguredPathMissing_ReturnsNotAvailableWithoutStart()
    {
        var assistant = new ClaudeCodeAssistant(
            new ClaudeAssistantOptions { ExecutablePath = @"C:\fehlt\claude.exe" }, _runner, _fileSystem);

        var result = await assistant.CheckAvailabilityAsync();

        Assert.False(result.IsAvailable);
        Assert.Contains("eingestellte Pfad", result.Message);
        Assert.Empty(_runner.StartInfos);
    }

    [Fact]
    public async Task CheckAvailability_StartThrows_ReturnsNotAvailable()
    {
        _runner.StartException = new System.ComponentModel.Win32Exception(2);

        var result = await CreateAssistant().CheckAvailabilityAsync();

        Assert.False(result.IsAvailable);
        Assert.Contains("nicht starten", result.Message);
    }

    [Fact]
    public async Task CheckAvailability_NoAnswer_TimesOutAndKills()
    {
        _runner.OnStart = p => p.IgnoreCloseInput = true;
        var options = new ClaudeAssistantOptions { ExecutablePath = FakeExe, JobRootDirectory = JobRoot, AvailabilityTimeout = TimeSpan.FromMilliseconds(100) };

        var result = await CreateAssistant(options).CheckAvailabilityAsync();

        Assert.False(result.IsAvailable);
        Assert.Contains("Zeitüberschreitung", result.Message);
        Assert.True(_runner.Last.Killed);
    }

    // --- Start / Kommandozeile ---

    [Fact]
    public async Task StartSession_New_StartsRestrictedInWorkingDirAndSendsFirstMessage()
    {
        await using var session = await StartAsync(CreateAssistant(), "Lies kontext.json");

        var info = Assert.Single(_runner.StartInfos);
        Assert.Equal(FakeExe, info.FileName);
        Assert.Equal(_workDir, info.WorkingDirectory);
        Assert.True(Guid.TryParse(session.SessionId, out _));
        AssertArgument(info.Arguments, "--session-id", session.SessionId);
        AssertArgument(info.Arguments, "--tools", "Read,Write,Edit,Glob");
        AssertArgument(info.Arguments, "--allowedTools", "Read,Write,Edit,Glob");
        AssertArgument(info.Arguments, "--input-format", "stream-json");
        AssertArgument(info.Arguments, "--output-format", "stream-json");
        Assert.Contains("--restricted", info.Arguments);
        Assert.DoesNotContain("--resume", info.Arguments);

        var sent = JsonDocument.Parse(Assert.Single(_runner.Last.WrittenLines)).RootElement;
        Assert.Equal("user", sent.GetProperty("type").GetString());
        Assert.Equal("Lies kontext.json", sent.GetProperty("message").GetProperty("content").GetString());
        Assert.True(session.IsTurnOpen);
    }

    [Fact]
    public async Task StartSession_Resume_UsesResumeAndSendsNothingWithoutMessage()
    {
        var id = Guid.NewGuid().ToString();

        await using var session = await CreateAssistant().StartSessionAsync(
            new ClaudeSessionRequest(_workDir, "", ResumeSessionId: id), _events);

        Assert.Equal(id, session.SessionId);
        AssertArgument(_runner.StartInfos[0].Arguments, "--resume", id);
        Assert.Empty(_runner.Last.WrittenLines);
        Assert.False(session.IsTurnOpen);
    }

    [Fact]
    public async Task StartSession_InvalidInput_Throws()
    {
        var assistant = CreateAssistant();

        await Assert.ThrowsAsync<ClaudeAssistantException>(() =>
            assistant.StartSessionAsync(new ClaudeSessionRequest(Path.Combine(_workDir, "fehlt"), "x"), _events));
        await Assert.ThrowsAsync<ClaudeAssistantException>(() =>
            assistant.StartSessionAsync(new ClaudeSessionRequest("relativ", "x"), _events));
        await Assert.ThrowsAsync<ClaudeAssistantException>(() =>
            assistant.StartSessionAsync(new ClaudeSessionRequest(_workDir, "  "), _events));
        await Assert.ThrowsAsync<ClaudeAssistantException>(() =>
            assistant.StartSessionAsync(new ClaudeSessionRequest(_workDir, "", ResumeSessionId: "kein-uuid"), _events));
        Assert.Empty(_runner.StartInfos);
    }

    [Fact]
    public async Task StartSession_ProcessStartFails_ThrowsAssistantException()
    {
        _runner.StartException = new System.ComponentModel.Win32Exception(2);

        var ex = await Assert.ThrowsAsync<ClaudeAssistantException>(() => StartAsync(CreateAssistant()));

        Assert.DoesNotContain(@"C:\", ex.Message);
    }

    // --- Ereignisse ---

    [Fact]
    public async Task Session_StreamLines_RaiseEventsInOrderAndCloseTurn()
    {
        await using var session = await StartAsync(CreateAssistant());
        var process = _runner.Last;

        process.EmitOutput(ClaudeStreamJsonParserTests.InitLine);
        process.EmitOutput(ClaudeStreamJsonParserTests.ToolUseLine);
        process.EmitOutput(ClaudeStreamJsonParserTests.QuestionTextLine);
        process.EmitOutput(ClaudeStreamJsonParserTests.SuccessResultLine);
        await _events.WaitForAsync(e => e.Kind == ClaudeEventKind.TurnCompleted);

        Assert.Equal(
            [ClaudeEventKind.ToolUse, ClaudeEventKind.Text, ClaudeEventKind.TurnCompleted],
            _events.Events.Select(e => e.Kind));
        Assert.Single(_events.Events[1].Questions!);
        Assert.False(session.IsTurnOpen);
    }

    [Fact]
    public async Task SendMessage_AfterProcessExited_ResumesSameSession()
    {
        await using var session = await StartAsync(CreateAssistant());
        _runner.Last.EmitOutput(ClaudeStreamJsonParserTests.SuccessResultLine);
        _runner.Last.Exit(0);
        await _events.WaitForAsync(e => e.Kind == ClaudeEventKind.Exited);

        await session.SendMessageAsync("1. Polierplan");

        Assert.Equal(2, _runner.StartInfos.Count);
        AssertArgument(_runner.StartInfos[1].Arguments, "--resume", session.SessionId);
        Assert.Contains("1. Polierplan", Assert.Single(_runner.Last.WrittenLines));
        Assert.True(_events.Events.Single(e => e.Kind == ClaudeEventKind.Exited).CanContinue);
        Assert.DoesNotContain(_events.Events, e => e.Kind == ClaudeEventKind.Error);
    }

    [Fact]
    public async Task Session_UnexpectedExitCode_RaisesErrorWithStderrThenExited()
    {
        await using var session = await StartAsync(CreateAssistant());
        _runner.Last.EmitError("Fehler: kaputt");
        await Task.Delay(50);
        _runner.Last.Exit(1);

        await _events.WaitForAsync(e => e.Kind == ClaudeEventKind.Exited);

        var error = _events.Events.Single(e => e.Kind == ClaudeEventKind.Error);
        Assert.Equal("Claude ist beendet (Code 1): Fehler: kaputt", error.Text);
        Assert.Equal(ClaudeEventKind.Exited, _events.Events[^1].Kind);
        Assert.Equal(1, _events.Events[^1].ExitCode);
        Assert.False(session.IsTurnOpen);
    }

    // --- Beenden ---

    [Fact]
    public async Task EndAsync_ClosesInputAndBlocksFurtherMessages()
    {
        var session = await StartAsync(CreateAssistant());

        await session.EndAsync();

        Assert.True(_runner.Last.InputClosed);
        Assert.False(_runner.Last.Killed);
        Assert.True(_runner.Last.Disposed);
        Assert.True(session.IsEnded);
        var exited = _events.Events.Single(e => e.Kind == ClaudeEventKind.Exited);
        Assert.False(exited.CanContinue);
        Assert.DoesNotContain(_events.Events, e => e.Kind == ClaudeEventKind.Error);
        await Assert.ThrowsAsync<ClaudeAssistantException>(() => session.SendMessageAsync("noch was"));
        await session.EndAsync(); // zweites Mal ohne Wirkung
    }

    [Fact]
    public async Task EndAsync_ProcessIgnoresClose_KillsAfterGracePeriod()
    {
        _runner.OnStart = p => p.IgnoreCloseInput = true;
        var options = new ClaudeAssistantOptions { ExecutablePath = FakeExe, JobRootDirectory = JobRoot, StopGracePeriod = TimeSpan.FromMilliseconds(50) };
        var session = await StartAsync(CreateAssistant(options));

        await session.EndAsync();

        Assert.True(_runner.Last.Killed);
        Assert.DoesNotContain(_events.Events, e => e.Kind == ClaudeEventKind.Error);
    }

    // --- Waechter ---

    [Fact]
    public async Task Session_SilentTurn_RaisesErrorAndStopsButStaysResumable()
    {
        var options = new ClaudeAssistantOptions { ExecutablePath = FakeExe, JobRootDirectory = JobRoot, TurnSilenceTimeout = TimeSpan.FromMilliseconds(150) };
        await using var session = await StartAsync(CreateAssistant(options));

        await _events.WaitForAsync(e => e.Kind == ClaudeEventKind.Exited);

        var error = _events.Events.Single(e => e.Kind == ClaudeEventKind.Error);
        Assert.Contains("Runde abgebrochen", error.Text);
        Assert.True(_runner.Processes[0].InputClosed);
        Assert.True(_events.Events.Single(e => e.Kind == ClaudeEventKind.Exited).CanContinue);

        // Noch keine fertige Runde → Sitzung ist bei Claude evtl. nicht gespeichert → wieder --session-id
        await session.SendMessageAsync("weiter");
        AssertArgument(_runner.StartInfos[1].Arguments, "--session-id", session.SessionId);
        Assert.DoesNotContain("--resume", _runner.StartInfos[1].Arguments);
    }

    [Fact]
    public async Task Session_ResultWithOtherSessionId_AdoptsReportedId()
    {
        await using var session = await StartAsync(CreateAssistant());
        var reported = Guid.NewGuid().ToString();

        _runner.Last.EmitOutput($$"""{"type":"result","is_error":false,"session_id":"{{reported}}"}""");
        await _events.WaitForAsync(e => e.Kind == ClaudeEventKind.TurnCompleted);

        Assert.Equal(reported, session.SessionId);
    }

    [Fact]
    public async Task Session_MessageSentAroundIdleLimit_IsNeverLost()
    {
        // Je nach Takt stoppt der Leerlauf-Waechter vor der Nachricht (dann Neustart) oder gar nicht —
        // in keinem Fall darf er den Prozess stoppen, der die Nachricht schon bekommen hat.
        var options = new ClaudeAssistantOptions { ExecutablePath = FakeExe, JobRootDirectory = JobRoot, IdleTimeout = TimeSpan.FromMilliseconds(150) };
        await using var session = await StartAsync(CreateAssistant(options));
        _runner.Last.EmitOutput(ClaudeStreamJsonParserTests.SuccessResultLine);
        await _events.WaitForAsync(e => e.Kind == ClaudeEventKind.TurnCompleted);
        await Task.Delay(140);

        await session.SendMessageAsync("noch eine Antwort");
        await Task.Delay(400);

        var last = _runner.Last;
        Assert.Contains(last.WrittenLines, l => l.Contains("noch eine Antwort"));
        Assert.False(last.InputClosed);
        Assert.True(session.IsTurnOpen);
    }

    [Fact]
    public async Task CheckAvailability_CallerCancels_KillsProcess()
    {
        _runner.OnStart = p => p.IgnoreCloseInput = true;
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateAssistant().CheckAvailabilityAsync(cts.Token));

        Assert.True(_runner.Last.Killed);
    }

    [Fact]
    public async Task Session_IdleWithoutOpenTurn_StopsWithoutError()
    {
        var options = new ClaudeAssistantOptions { ExecutablePath = FakeExe, JobRootDirectory = JobRoot, IdleTimeout = TimeSpan.FromMilliseconds(150) };
        await using var session = await StartAsync(CreateAssistant(options));
        _runner.Last.EmitOutput(ClaudeStreamJsonParserTests.SuccessResultLine);

        await _events.WaitForAsync(e => e.Kind == ClaudeEventKind.Exited);

        Assert.DoesNotContain(_events.Events, e => e.Kind == ClaudeEventKind.Error);
        Assert.True(_runner.Last.InputClosed);
        Assert.False(session.IsEnded);
    }

    [Fact]
    public async Task SendMessage_WriteFails_ThrowsAndLeavesNoOpenTurn()
    {
        await using var session = await StartAsync(CreateAssistant());
        _runner.Last.EmitOutput(ClaudeStreamJsonParserTests.SuccessResultLine);
        await _events.WaitForAsync(e => e.Kind == ClaudeEventKind.TurnCompleted);
        _runner.Last.FailWrites = true;

        await Assert.ThrowsAsync<ClaudeAssistantException>(() => session.SendMessageAsync("antwort"));
        Assert.False(session.IsTurnOpen);
    }

    [Fact]
    public async Task SendMessage_TooLong_Throws()
    {
        var options = new ClaudeAssistantOptions { ExecutablePath = FakeExe, JobRootDirectory = JobRoot, MaxMessageLength = 5 };
        await using var session = await StartAsync(CreateAssistant(options), "kurz");

        await Assert.ThrowsAsync<ClaudeAssistantException>(() => session.SendMessageAsync("zu lang"));
    }

    // --- reine Funktionen ---

    [Fact]
    public void BuildSessionArguments_WithPromptAndModel_AppendsThemBeforeSession()
    {
        var args = ClaudeCommandLine.BuildSessionArguments(
            ClaudeAssistantOptions.DefaultTools, "id-1", resume: false, @"C:\Auftraege\A-0007.rahmen.md", "sonnet");

        AssertArgument(args, "--append-system-prompt-file", @"C:\Auftraege\A-0007.rahmen.md");
        Assert.Contains("--safe-mode", args);
        Assert.Contains("--restricted", args);
        AssertArgument(args, "--model", "sonnet");
        AssertArgument(args, "--permission-mode", "acceptEdits");
        AssertArgument(args, "--permission-prompts", "none");
        Assert.Equal(["--session-id", "id-1"], args.TakeLast(2));
        Assert.Equal("-p", args[0]);
    }

    [Fact]
    public void ResolveExecutable_PrefersConfiguredThenPathThenUserProfile()
    {
        var path = string.Join(Path.PathSeparator, @"C:\a", @"C:\b");
        var inB = Path.Combine(@"C:\b", "claude.exe");
        var inProfile = Path.Combine(@"C:\Users\x", ".local", "bin", "claude.exe");

        Assert.Equal(@"C:\eigen\claude.exe",
            ClaudeCommandLine.ResolveExecutable(@"C:\eigen\claude.exe", path, @"C:\Users\x", _ => true));
        Assert.Null(ClaudeCommandLine.ResolveExecutable(@"C:\eigen\claude.exe", path, @"C:\Users\x", p => p == inB));
        Assert.Equal(inB, ClaudeCommandLine.ResolveExecutable(null, path, @"C:\Users\x", p => p == inB || p == inProfile));
        Assert.Equal(inProfile, ClaudeCommandLine.ResolveExecutable("", path, @"C:\Users\x", p => p == inProfile));
        Assert.Null(ClaudeCommandLine.ResolveExecutable(null, null, null, _ => false));
    }

    // --- Review-Befunde (Abschottung, Fortsetzen, Runden) ---

    [Theory]
    [InlineData(@"C:\Auftraege\A-0007", @"C:\Auftraege", true)]
    [InlineData(@"C:\Auftraege\A-0007\", @"C:\Auftraege\", true)]
    [InlineData(@"C:\Auftraege", @"C:\Auftraege", false)]
    [InlineData(@"C:\Auftraege\..\Projekte\P1", @"C:\Auftraege", false)]
    [InlineData(@"C:\AuftraegeX\A-1", @"C:\Auftraege", false)]
    [InlineData(@"C:\Projekte\P1", @"C:\Auftraege", false)]
    [InlineData(@"C:\Projekte\P1", @"C:\", false)]
    [InlineData(@"C:\Auftraege\A-1", null, false)]
    public void IsInsideJobRoot_OnlyRealSubfolders(string path, string? root, bool expected)
    {
        Assert.Equal(expected, ClaudeCodeAssistant.IsInsideJobRoot(path, root));
    }

    [Fact]
    public async Task StartSession_WorkingDirOutsideJobRoot_ThrowsWithoutStart()
    {
        _fileSystem.CreateDirectory(@"C:\Projekte\P1");
        var assistant = CreateAssistant();

        await Assert.ThrowsAsync<ClaudeAssistantException>(() =>
            assistant.StartSessionAsync(new ClaudeSessionRequest(@"C:\Projekte\P1", "x"), _events));
        Assert.Empty(_runner.StartInfos);
    }

    [Fact]
    public async Task StartSession_SystemPromptFileInsideWorkDir_Throws()
    {
        var inside = Path.Combine(WorkDir, "rahmen.md");
        _fileSystem.AddFile(inside);
        var assistant = CreateAssistant();

        await Assert.ThrowsAsync<ClaudeAssistantException>(() =>
            assistant.StartSessionAsync(new ClaudeSessionRequest(WorkDir, "x", SystemPromptFile: inside), _events));
        Assert.Empty(_runner.StartInfos);
    }

    [Fact]
    public async Task Session_CrashAfterInit_RestartsWithResume()
    {
        await using var session = await StartAsync(CreateAssistant());
        _runner.Last.EmitOutput(ClaudeStreamJsonParserTests.InitLine);
        _runner.Last.Exit(1);
        await _events.WaitForAsync(e => e.Kind == ClaudeEventKind.Exited);

        await session.SendMessageAsync("weiter");

        Assert.Contains("--resume", _runner.StartInfos[1].Arguments);
    }

    [Fact]
    public async Task Session_StartRejectedWithoutOutput_NextStartSwitchesMode()
    {
        await using var session = await StartAsync(CreateAssistant());
        _runner.Last.Exit(1);
        await _events.WaitForAsync(e => e.Kind == ClaudeEventKind.Exited);

        await session.SendMessageAsync("weiter");

        Assert.Contains("--resume", _runner.StartInfos[1].Arguments);
    }

    [Fact]
    public async Task SendMessage_WhileTurnOpen_IsRejected()
    {
        await using var session = await StartAsync(CreateAssistant());

        var ex = await Assert.ThrowsAsync<ClaudeAssistantException>(() => session.SendMessageAsync("noch was"));
        Assert.Contains("arbeitet noch", ex.Message);
        Assert.Single(_runner.Last.WrittenLines);
    }

    [Fact]
    public async Task Session_ExitCodeZeroWithOpenTurn_RaisesError()
    {
        await using var session = await StartAsync(CreateAssistant());
        _runner.Last.Exit(0);
        await _events.WaitForAsync(e => e.Kind == ClaudeEventKind.Exited);

        Assert.Contains(_events.Events, e => e.Kind == ClaudeEventKind.Error && e.Text!.Contains("ohne Antwort"));
    }

    [Fact]
    public void RedirectingVariables_AreRemoved()
    {
        Assert.True(SystemClaudeProcessRunner.IsRedirectingVariable("ANTHROPIC_API_KEY"));
        Assert.True(SystemClaudeProcessRunner.IsRedirectingVariable("ANTHROPIC_AUTH_TOKEN"));
        Assert.True(SystemClaudeProcessRunner.IsRedirectingVariable("anthropic_base_url"));
        Assert.True(SystemClaudeProcessRunner.IsRedirectingVariable("CLAUDE_CODE_USE_BEDROCK"));
        Assert.False(SystemClaudeProcessRunner.IsRedirectingVariable("PATH"));
        Assert.False(SystemClaudeProcessRunner.IsRedirectingVariable("USERPROFILE"));
    }

    [Fact]
    public void ResolveExecutable_RejectsCmdAndRelativePaths()
    {
        Assert.Null(ClaudeCommandLine.ResolveExecutable(@"C:\npm\claude.cmd", null, null, _ => true));
        Assert.Null(ClaudeCommandLine.ResolveExecutable(@"claude.exe", null, null, _ => true));
        var relative = Path.Combine(".", "claude.exe");
        Assert.Null(ClaudeCommandLine.ResolveExecutable(null, ".", null, p => p == relative));
    }

    [Theory]
    [InlineData(@"C:\Kunde Meier\Auftraege\A-0007", "A-0007")]
    [InlineData(@"C:\Kunde Meier\Auftraege\A-0007\", "A-0007")]
    public void FolderName_ReturnsOnlyLastSegment(string path, string expected)
    {
        Assert.Equal(expected, ClaudeCodeSession.FolderName(path));
    }

    private static void AssertArgument(IReadOnlyList<string> args, string option, string value)
    {
        var index = args.ToList().IndexOf(option);
        Assert.True(index >= 0, $"Option {option} fehlt");
        Assert.Equal(value, args[index + 1]);
    }
}
