using System.IO;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.Claude;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services.Claude;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.Services.ClaudeSortierung;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.Tests;

/// <summary>
/// Chatfenster "Mit Claude sortieren" (Seitenliste 3.4): Ablauf Start → Rueckfrage → Entwurf pruefen →
/// Uebernahme als Pending-Vorschlag, mit Fake-Claude und echtem Builder/Pruefer.
/// </summary>
public partial class ClaudeSortierungTests
{
    private const string JobRoot = @"C:\appdata\claude";

    private sealed class FakeHost(IReadOnlyList<CaptureItem> items) : IClaudeSortHost
    {
        public string ProjectId { get; init; } = "";
        public string ProjectRootPath => Root;
        public IReadOnlyList<CaptureItem> CurrentItems { get; set; } = items;
        public PendingAssignmentStore PendingStore { get; } = new();
        public IReadOnlyDictionary<string, ClaudeRowMark> Marks { get; private set; } = new Dictionary<string, ClaudeRowMark>();
        public int Reanalyzed { get; private set; }

        public Task ReanalyzeAsync()
        {
            Reanalyzed++;
            return Task.CompletedTask;
        }

        public void SetClaudeMarks(IReadOnlyDictionary<string, ClaudeRowMark> marks) => Marks = marks;

        public IReadOnlyList<BauProjektManager.Domain.Models.BuildingPart> BuildingParts { get; set; } = [];
        public int StammdatenGeladen { get; private set; }
        public void StammdatenNeuLaden() => StammdatenGeladen++;
    }

    private sealed class FakeSession : IClaudeSession
    {
        public List<string> Sent { get; } = [];
        public string SessionId => "00000000-0000-0000-0000-000000000001";
        public bool IsTurnOpen => false;
        public bool IsEnded { get; private set; }

        public Task SendMessageAsync(string text, CancellationToken cancellationToken = default)
        {
            Sent.Add(text);
            return Task.CompletedTask;
        }

        public Task EndAsync()
        {
            IsEnded = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => new(EndAsync());
    }

    private sealed class FakeAssistant(bool available = true) : IClaudeAssistant
    {
        public FakeSession Session { get; } = new();
        public ClaudeSessionRequest? Request { get; private set; }
        public List<string> DeletedSessionData { get; } = [];

        public void DeleteSessionData(string workingDirectory)
        {
            // Wie der echte Adapter: erst nach dem Ende der Sitzung
            Assert.True(Request is null || Session.IsEnded, "Sitzungsdaten vor dem Ende der Sitzung geloescht");
            DeletedSessionData.Add(workingDirectory);
        }

        public Task<ClaudeAvailability> CheckAvailabilityAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ClaudeAvailability(available, available ? "2.1" : null,
                available ? "bereit" : "Claude Code wurde nicht gefunden."));

        public Task<IClaudeSession> StartSessionAsync(ClaudeSessionRequest request, IProgress<ClaudeEvent> events,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult<IClaudeSession>(Session);
        }
    }

    private (ClaudeSortViewModel Vm, FakeAssistant Claude, FakeHost Host) ChatVm(bool enabled = true, bool available = true)
    {
        var claude = new FakeAssistant(available);
        var host = new FakeHost(_analyse) { ProjectId = _projectId };
        var vm = new ClaudeSortViewModel(claude, Builder(), Pruefer(), host, () => enabled, JobRoot);
        return (vm, claude, host);
    }

    private void SchreibeEntwurf(FakeAssistant claude, string json)
        => _fs.WriteAllText(Path.Combine(claude.Request!.WorkingDirectory, ClaudeSortArten.EntwurfDatei), json);

    [Fact]
    public async Task Chat_Disabled_ButtonOffAndStartDoesNothing()
    {
        var (vm, claude, _) = ChatVm(enabled: false);

        Assert.False(vm.IsButtonEnabled);
        Assert.Contains("ausgeschaltet", vm.OffHint);
        await vm.StartCommand.ExecuteAsync(null);
        Assert.Null(claude.Request);
    }

    [Fact]
    public async Task Chat_Start_CreatesJobInProjectFolder_MarksOnlyBucketC()
    {
        var (vm, claude, host) = ChatVm();
        vm.ToggleStartCommand.Execute(null);
        Assert.True(vm.IsConfirmOpen);
        Assert.Equal("2 Dateien", vm.ConfirmCountText);
        Assert.Contains("1 Update", vm.ExcludedText);

        await vm.StartCommand.ExecuteAsync(null);

        Assert.Equal(Path.Combine(JobRoot, _projectId, "A-0001"), claude.Request!.WorkingDirectory);
        Assert.Equal(claude.Request.WorkingDirectory + ".regeln.md", claude.Request.SystemPromptFile);
        Assert.Equal(ClaudeSortViewModel.FirstMessage, claude.Request.FirstMessage);
        Assert.Equal(2, host.Marks.Count);
        Assert.All(host.Marks.Values, m => Assert.Equal(ClaudeRowMarkKind.AtClaude, m.Kind));
        Assert.Equal(ClaudeSortPhase.Running, vm.Phase);
        Assert.True(vm.IsChatOpen);
    }

    [Fact]
    public async Task Chat_ClaudeUnavailable_ShowsErrorCard()
    {
        var (vm, claude, _) = ChatVm(available: false);

        await vm.StartCommand.ExecuteAsync(null);

        Assert.Equal(ClaudeSortPhase.Unavailable, vm.Phase);
        Assert.Contains("nicht gefunden", vm.UnavailableText);
        Assert.Null(claude.Request);
    }

    [Fact]
    public async Task Chat_Question_BecomesButtons_AnswerGoesToClaude()
    {
        var (vm, claude, _) = ChatVm();
        await vm.StartCommand.ExecuteAsync(null);
        const string text = "Eine Sache noch:\n\nFRAGE: Zu welchem Bauteil gehört scan_001?\n1. Haus A (Empfehlung) – Plankopf\n2. Tiefgarage";

        vm.OnEvent(new ClaudeEvent(ClaudeEventKind.Text, Text: text, Questions: ClaudeQuestionParser.Parse(text)));
        vm.OnEvent(new ClaudeEvent(ClaudeEventKind.TurnCompleted));
        await vm.NachRundeAsync();

        var frage = Assert.Single(vm.Entries, e => e.Kind == ClaudeChatEntryKind.Question).Question!;
        Assert.True(frage.Answers[0].IsRecommended);
        Assert.Equal(ClaudeSortPhase.Waiting, vm.Phase);

        await vm.AnswerCommand.ExecuteAsync(frage.Answers[0]);

        Assert.True(frage.IsAnswered);
        Assert.Equal("1. Haus A", Assert.Single(claude.Session.Sent));
        Assert.Equal(ClaudeSortPhase.Running, vm.Phase);
    }

    [Fact]
    public async Task Chat_InvalidDraft_GoesBackTwice_ThenShowsWhatIsValid()
    {
        var (vm, claude, host) = ChatVm();
        await vm.StartCommand.ExecuteAsync(null);
        var falsch = PolierplanZeile(typId: "gibt-es-nicht");
        SchreibeEntwurf(claude, Entwurf(falsch));

        await vm.NachRundeAsync();
        await vm.NachRundeAsync();
        Assert.Equal(2, claude.Session.Sent.Count);
        Assert.All(claude.Session.Sent, s => Assert.StartsWith("Entwurf ungültig", s));

        await vm.NachRundeAsync();
        Assert.Equal(2, claude.Session.Sent.Count);
        Assert.Equal(ClaudeSortPhase.Waiting, vm.Phase); // nichts Gueltiges → kein Entwurf zum Uebernehmen
        Assert.All(host.Marks.Values, m => Assert.Equal(ClaudeRowMarkKind.Invalid, m.Kind));
    }

    [Fact]
    public async Task Chat_ValidDraft_TakeOver_CreatesClaudeSuggestionsOnly()
    {
        var (vm, claude, host) = ChatVm();
        await vm.StartCommand.ExecuteAsync(null);
        SchreibeEntwurf(claude, Entwurf(PolierplanZeile()));

        await vm.NachRundeAsync();

        Assert.Equal(ClaudeSortPhase.Draft, vm.Phase);
        Assert.True(vm.IsDraftBarVisible);
        Assert.Equal("1 Vorschläge übernehmen", vm.DraftButtonText);
        var mark = host.Marks[Path.Combine("_Eingang", "5998-100_EG.pdf")];
        Assert.Equal(ClaudeRowMarkKind.DraftUncertain, mark.Kind);
        Assert.Equal("✦ Polierplan › Haus A › EG", mark.Text);
        Assert.Equal(ClaudeRowMarkKind.Invalid, host.Marks[Path.Combine("_Eingang", "scan_001.pdf")].Kind);

        await vm.TakeOverCommand.ExecuteAsync(null);

        var p = Assert.Single(host.PendingStore.Snapshot());
        Assert.True(p.IsClaudeSuggestion);
        Assert.Equal(ClaudeSortPhase.Applied, vm.Phase);
        Assert.Empty(host.Marks);
        Assert.True(host.Reanalyzed >= 2);
        Assert.Contains(vm.Entries, e => e.Kind == ClaudeChatEntryKind.SystemOk && e.Text.Contains("Import bestätigen"));
    }

    [Fact]
    public async Task Chat_DiscardDraft_KeepsSessionAndPending()
    {
        var (vm, claude, host) = ChatVm();
        await vm.StartCommand.ExecuteAsync(null);
        SchreibeEntwurf(claude, Entwurf(PolierplanZeile()));
        await vm.NachRundeAsync();

        vm.DiscardCommand.Execute(null);

        Assert.Equal(ClaudeSortPhase.Waiting, vm.Phase);
        Assert.Empty(host.PendingStore.Snapshot());
        Assert.True(vm.CanType);
        Assert.False(claude.Session.IsEnded);
    }

    [Fact]
    public async Task Chat_AuthFailure_ShowsErrorCard_NoDuplicate_CleansUp()
    {
        var (vm, claude, host) = ChatVm();
        await vm.StartCommand.ExecuteAsync(null);
        var ordner = claude.Request!.WorkingDirectory;
        const string meldung = "Failed to authenticate: OAuth session expired and could not be refreshed";

        vm.OnEvent(new ClaudeEvent(ClaudeEventKind.Text, Text: meldung, Questions: []));
        vm.OnEvent(new ClaudeEvent(ClaudeEventKind.TurnCompleted, Text: meldung, IsError: true));

        Assert.Equal(ClaudeSortPhase.Unavailable, vm.Phase);
        Assert.Contains("nicht angemeldet", vm.UnavailableText);
        Assert.DoesNotContain(vm.Entries, e => e.Text == meldung);
        Assert.True(claude.Session.IsEnded);
        Assert.Empty(host.Marks);
        Assert.Empty(_fs.EnumerateFiles(ordner));
    }

    [Theory]
    [InlineData("Failed to authenticate: OAuth session expired", true)]
    [InlineData("Invalid API key · Please run /login", true)]
    [InlineData("Entwurf ungültig: D-001 fehlt", false)]
    [InlineData(null, false)]
    public void IstAnmeldefehler_ErkenntLoginProbleme(string? text, bool erwartet)
        => Assert.Equal(erwartet, ClaudeSortViewModel.IstAnmeldefehler(text));

    [Fact]
    public async Task Chat_Abort_EndsSession()
    {
        var (vm, claude, host) = ChatVm();
        await vm.StartCommand.ExecuteAsync(null);

        await vm.AbortCommand.ExecuteAsync(null);

        Assert.True(claude.Session.IsEnded);
        Assert.Equal(ClaudeSortPhase.Idle, vm.Phase);
        Assert.Empty(host.Marks);
    }

    private static readonly string PdfPfad = Path.Combine("_Eingang", "5998-100_EG.pdf");

    [Fact]
    public async Task Chat_TakeOverOne_OnlyThisRow_DraftEnds()
    {
        var (vm, claude, host) = ChatVm();
        await vm.StartCommand.ExecuteAsync(null);
        SchreibeEntwurf(claude, Entwurf(PolierplanZeile()));
        await vm.NachRundeAsync();

        await vm.TakeOverOneCommand.ExecuteAsync(PdfPfad);

        Assert.True(Assert.Single(host.PendingStore.Snapshot()).IsClaudeSuggestion);
        Assert.False(host.Marks.ContainsKey(PdfPfad));
        Assert.Equal(ClaudeSortPhase.Applied, vm.Phase); // kein offener Vorschlag mehr
    }

    [Fact]
    public async Task Chat_RejectOne_RowOpenAgain_NothingPending()
    {
        var (vm, claude, host) = ChatVm();
        await vm.StartCommand.ExecuteAsync(null);
        SchreibeEntwurf(claude, Entwurf(PolierplanZeile()));
        await vm.NachRundeAsync();

        vm.RejectOneCommand.Execute(PdfPfad);

        Assert.Empty(host.PendingStore.Snapshot());
        Assert.False(host.Marks.ContainsKey(PdfPfad));
        Assert.Equal(ClaudeSortPhase.Waiting, vm.Phase);
        Assert.Equal("0 Vorschläge übernehmen", vm.DraftButtonText);
    }

    [Fact]
    public async Task Chat_Dispose_DeletesJobFilesAndRules()
    {
        var (vm, claude, _) = ChatVm();
        await vm.StartCommand.ExecuteAsync(null);
        var ordner = claude.Request!.WorkingDirectory;
        Assert.True(_fs.FileExists(Path.Combine(ordner, ClaudeSortArten.KontextDatei)));

        await vm.DisposeAsync();

        Assert.Empty(_fs.EnumerateFiles(ordner));
        Assert.False(_fs.FileExists(claude.Request.SystemPromptFile!));
        Assert.True(claude.Session.IsEnded);
        Assert.Equal([ordner], claude.DeletedSessionData);
    }

    [Fact]
    public void Chat_Open_DeletesSessionDataOfFinishedJobsOnly()
    {
        var basis = Path.Combine(JobRoot, _projectId);
        _fs.AddFile(Path.Combine(basis, "A-0001", ClaudeSortArten.KontextDatei), [1]); // frisch, laeuft evtl. noch
        var leer = Path.Combine(basis, "A-0002");
        _fs.CreateDirectory(leer);                                                     // frueher aufgeraeumt

        var claude = new FakeAssistant();
        var builder = new ClaudeSortAuftragBuilder(_bpmDb, _planDb, _fs, _fs, _fs, time: new FesteZeit(1));
        _ = new ClaudeSortViewModel(claude, builder, Pruefer(), new FakeHost(_analyse) { ProjectId = _projectId },
            () => true, JobRoot);

        Assert.Equal([leer], claude.DeletedSessionData);
    }

    /// <summary>Feste "Jetzt"-Zeit relativ zum Zeitstempel des FakeFileStore (01.01.2026 UTC).</summary>
    private sealed class FesteZeit(int tageNachStempel) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(tageNachStempel);
    }

    [Fact]
    public async Task Builder_AlteAufraeumen_OnlyOldJobs()
    {
        var auftrag = await ErstelleAuftragAsync();
        var root = Path.GetDirectoryName(auftrag.Ordner)!;

        new ClaudeSortAuftragBuilder(_bpmDb, _planDb, _fs, _fs, _fs, time: new FesteZeit(1))
            .AlteAufraeumen(root, TimeSpan.FromDays(30));
        Assert.NotEmpty(_fs.EnumerateFiles(auftrag.Ordner)); // 1 Tag alt → bleibt

        new ClaudeSortAuftragBuilder(_bpmDb, _planDb, _fs, _fs, _fs, time: new FesteZeit(31))
            .AlteAufraeumen(root, TimeSpan.FromDays(30));
        Assert.Empty(_fs.EnumerateFiles(auftrag.Ordner));
    }
}
