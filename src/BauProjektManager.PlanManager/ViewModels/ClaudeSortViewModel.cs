using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.Claude;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services.Claude;
using BauProjektManager.PlanManager.Services.ClaudeSortierung;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>Ablauf von "Mit Claude sortieren" (Zustaende aus Mockup 3.4).</summary>
public enum ClaudeSortPhase
{
    /// <summary>Kein Auftrag (Knopf "Mit Claude sortieren").</summary>
    Idle,
    /// <summary>Bestaetigungs-Popover offen.</summary>
    Confirm,
    /// <summary>Claude arbeitet (Runde offen).</summary>
    Running,
    /// <summary>Runde fertig, wartet auf Herbert (Rueckfrage oder Freitext).</summary>
    Waiting,
    /// <summary>Gepruefter Entwurf liegt vor, noch nicht uebernommen.</summary>
    Draft,
    /// <summary>Vorschlaege als Pending uebernommen.</summary>
    Applied,
    /// <summary>Claude Code nicht erreichbar oder Start fehlgeschlagen.</summary>
    Unavailable
}

/// <summary>
/// Chatfenster "Mit Claude sortieren" (ADR-067, Seitenliste 3.4): Bestaetigung, Auftrag anlegen, Sitzung
/// mit Claude Code, Rueckfragen als Knoepfe, Entwurf pruefen (hoechstens <see cref="MaxRetries"/> Rueckgaben an
/// Claude je Nachricht), Uebernahme als Pending-Vorschlag. Importiert wird nur ueber "Import bestaetigen".
/// </summary>
public sealed partial class ClaudeSortViewModel : ObservableObject, IAsyncDisposable
{
    /// <summary>So oft geht ein ungueltiger Entwurf je Nachricht an Claude zurueck.</summary>
    public const int MaxRetries = 2;

    internal const string FirstMessage =
        "Lies auftrag.md, kontext.json und planzuordnung.schema.json in deinem Arbeitsordner und schreib entwurf.json. " +
        "Frag nach, wenn etwas unklar ist.";

    private readonly IClaudeAssistant? _assistant;
    private readonly ClaudeSortAuftragBuilder _builder;
    private readonly ClaudeSortEntwurfPruefer _pruefer;
    private readonly IClaudeSortHost _host;
    private readonly Func<bool> _isEnabled;
    private readonly string _jobRoot;

    private ClaudeSortAuftrag? _auftrag;
    private IClaudeSession? _session;
    private ClaudeSortPruefErgebnis? _pruefung;
    /// <summary>Gueltige Zeilen des Entwurfs, die noch weder uebernommen noch abgelehnt sind.</summary>
    private List<ClaudeSortZeile> _offen = [];
    private Dictionary<string, ClaudeRowMark> _marks = new(StringComparer.OrdinalIgnoreCase);
    private int _uebernommen;
    private int _retries;

    /// <param name="assistant">Port zu Claude Code; NULL = nicht verfuegbar (Knopf bleibt aus).</param>
    /// <param name="isEnabled">Schalter aus den Einstellungen (geraete-lokal, Standard aus).</param>
    /// <param name="jobRoot">Basisordner der Auftraege (ClaudeAssistantOptions.JobRootDirectory).</param>
    public ClaudeSortViewModel(
        IClaudeAssistant? assistant, ClaudeSortAuftragBuilder builder, ClaudeSortEntwurfPruefer pruefer,
        IClaudeSortHost host, Func<bool> isEnabled, string jobRoot)
    {
        _assistant = assistant;
        _builder = builder;
        _pruefer = pruefer;
        _host = host;
        _isEnabled = isEnabled;
        _jobRoot = jobRoot;
        // Nach einem Absturz liegen gebliebene Auftraege (mit Plankopf-Text) nach 30 Tagen entfernen, dazu die
        // Sitzungsdaten bei Claude zu allen erledigten Auftraegen (ADR-067 E8)
        if (!string.IsNullOrWhiteSpace(host.ProjectId))
            foreach (var ordner in _builder.AlteAufraeumen(Path.Combine(_jobRoot, host.ProjectId), TimeSpan.FromDays(30)))
                _assistant?.DeleteSessionData(ordner);
        RefreshButton();
    }

    public ObservableCollection<ClaudeChatEntry> Entries { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfirmOpen), nameof(IsDraftBarVisible), nameof(IsUnavailable),
        nameof(IsRunning), nameof(CanType), nameof(StatusText))]
    private ClaudeSortPhase _phase;

    [ObservableProperty]
    private bool _isChatOpen;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string _inputText = "";

    /// <summary>Haken im Startdialog: Plankopf-Text mitsenden (ADR-067 E2, vorbelegt an).</summary>
    [ObservableProperty]
    private bool _plankopfMitsenden = true;

    [ObservableProperty]
    private string _statusDetail = "";

    [ObservableProperty]
    private string _auftragName = "";

    [ObservableProperty]
    private string _buttonText = "Mit Claude sortieren";

    [ObservableProperty]
    private string _buttonToolTip = "";

    [ObservableProperty]
    private bool _isButtonEnabled;

    /// <summary>Hinweis neben dem Knopf, wenn die Funktion aus oder Claude nicht erreichbar ist.</summary>
    [ObservableProperty]
    private string _offHint = "";

    [ObservableProperty]
    private string _confirmCountText = "";

    [ObservableProperty]
    private string _excludedText = "";

    [ObservableProperty]
    private string _draftSummary = "";

    [ObservableProperty]
    private string _draftButtonText = "";

    [ObservableProperty]
    private string _unavailableText = "";

    public bool IsConfirmOpen => Phase == ClaudeSortPhase.Confirm;
    public bool IsDraftBarVisible => Phase == ClaudeSortPhase.Draft;
    public bool IsUnavailable => Phase == ClaudeSortPhase.Unavailable;
    public bool IsRunning => Phase == ClaudeSortPhase.Running;

    /// <summary>Freitext moeglich: Sitzung laeuft und Claude wartet.</summary>
    public bool CanType => _session is not null && Phase is ClaudeSortPhase.Waiting or ClaudeSortPhase.Draft
        or ClaudeSortPhase.Applied;

    public string StatusText => Phase switch
    {
        ClaudeSortPhase.Running => "arbeitet …",
        ClaudeSortPhase.Waiting => "wartet auf deine Antwort",
        ClaudeSortPhase.Draft => "Entwurf bereit",
        ClaudeSortPhase.Applied => "Vorschläge übernommen",
        ClaudeSortPhase.Unavailable => "nicht erreichbar",
        _ => "bereit"
    };

    private int NewCaptureCount => _host.CurrentItems.Count(i => i.Bucket == CaptureBucket.NewCapture);

    /// <summary>Knopf neu bewerten (Einstellung, Zahl der Erstaufnahmen) — nach jeder Analyse.</summary>
    public void RefreshButton()
    {
        var enabled = _isEnabled();
        var count = NewCaptureCount;
        var aktiv = _auftrag is not null && Phase is not ClaudeSortPhase.Idle;
        OffHint = !enabled ? "Claude ist ausgeschaltet · Einstellungen › Allgemein"
            : _assistant is null ? "Claude ist auf diesem PC nicht eingerichtet"
            : "";
        IsButtonEnabled = enabled && _assistant is not null && (aktiv || count > 0);
        ButtonText = aktiv ? (IsChatOpen ? $"Claude-Sitzung {AuftragName}" : "Sitzung fortsetzen") : "Mit Claude sortieren";
        ButtonToolTip = !enabled ? "In den Einstellungen ausgeschaltet"
            : count == 0 && !aktiv ? "Keine neuen Pläne im Eingang"
            : $"{count} Dateien (Erstaufnahme) von Claude vorsortieren lassen";
    }

    partial void OnIsChatOpenChanged(bool value) => RefreshButton();

    partial void OnPhaseChanged(ClaudeSortPhase value)
    {
        SendCommand.NotifyCanExecuteChanged();
        RefreshButton();
    }

    [RelayCommand]
    private void ToggleStart()
    {
        if (Phase == ClaudeSortPhase.Confirm)
        {
            Phase = ClaudeSortPhase.Idle;
            return;
        }
        if (Phase is ClaudeSortPhase.Idle or ClaudeSortPhase.Unavailable && _session is null)
        {
            var items = _host.CurrentItems;
            ConfirmCountText = $"{NewCaptureCount} Dateien";
            ExcludedText = Ausgenommen(items);
            Phase = ClaudeSortPhase.Confirm;
            return;
        }
        IsChatOpen = true;
    }

    [RelayCommand]
    private void CancelStart()
    {
        if (Phase == ClaudeSortPhase.Confirm)
            Phase = ClaudeSortPhase.Idle;
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        if (_assistant is null || !_isEnabled())
            return;
        Entries.Clear();
        IsChatOpen = true;
        Phase = ClaudeSortPhase.Running;
        StatusDetail = "prüft Claude Code …";

        var verfuegbar = await _assistant.CheckAvailabilityAsync();
        if (!verfuegbar.IsAvailable)
        {
            ZeigeNichtErreichbar(verfuegbar.Message);
            return;
        }

        try
        {
            var ordner = _builder.NaechsterAuftragsOrdner(Path.Combine(_jobRoot, _host.ProjectId));
            _auftrag = await _builder.ErstellenAsync(new ClaudeSortAuftragAnfrage(
                _host.ProjectId, _host.ProjectRootPath, ordner, _host.CurrentItems, PlankopfMitsenden));
            AuftragName = _auftrag.Name;
            Add(ClaudeChatEntryKind.System,
                $"Auftrag {_auftrag.Name} angelegt · {_auftrag.Dateien.Count} Dateien (Erstaufnahme)" +
                (PlankopfMitsenden ? " mit Plankopf-Text" : " ohne Plankopf-Text") + " · Claude Code gestartet");
            MarkiereBeiClaude();
            _uebernommen = 0;
            _offen = [];
            _retries = 0;
            StatusDetail = $"{_auftrag.Dateien.Count} Dateien";
            _session = await _assistant.StartSessionAsync(
                new ClaudeSessionRequest(_auftrag.Ordner, FirstMessage, SystemPromptFile: _auftrag.RegelnDatei),
                new Progress<ClaudeEvent>(OnEvent));
            OnPropertyChanged(nameof(CanType));
        }
        catch (Exception ex) when (ex is ClaudeAssistantException or InvalidOperationException)
        {
            Log.Warning("Claude-Sortierung: Start fehlgeschlagen ({Error})", ex.GetType().Name);
            _host.SetClaudeMarks(new Dictionary<string, ClaudeRowMark>());
            ZeigeNichtErreichbar(ex.Message);
        }
    }

    private bool CanSendText() => !string.IsNullOrWhiteSpace(InputText) && CanType;

    [RelayCommand(CanExecute = nameof(CanSendText))]
    private async Task SendAsync()
    {
        var text = InputText.Trim();
        InputText = "";
        Add(ClaudeChatEntryKind.User, text);
        await SendeAnClaudeAsync(text);
    }

    /// <summary>Knopf einer Antwort: einfache Frage sendet sofort, Mehrfachfrage kreuzt an.</summary>
    [RelayCommand]
    private async Task AnswerAsync(ClaudeAnswerViewModel? answer)
    {
        if (answer is null || answer.Question.IsAnswered || !CanType)
            return;
        if (answer.Question.AllowMultiple)
        {
            answer.IsChecked = !answer.IsChecked;
            return;
        }
        answer.IsChecked = true;
        await BeantworteAsync(answer.Question, [answer.Number]);
    }

    [RelayCommand]
    private async Task SendMultipleAsync(ClaudeQuestionViewModel? question)
    {
        if (question is null || question.IsAnswered || question.CheckedNumbers.Count == 0 || !CanType)
            return;
        await BeantworteAsync(question, question.CheckedNumbers);
    }

    [RelayCommand]
    private async Task TakeOverAsync()
    {
        if (_pruefung is null || Phase != ClaudeSortPhase.Draft || _offen.Count == 0)
            return;
        var ergebnis = ClaudeSortUebernahme.Uebernehmen(_offen, _host.PendingStore);
        _uebernommen += ergebnis.Uebernommen.Count;
        _offen = [];
        _marks.Clear();
        _host.SetClaudeMarks(new Dictionary<string, ClaudeRowMark>());
        await _host.ReanalyzeAsync();
        Phase = ClaudeSortPhase.Applied;
        StatusDetail = $"{_uebernommen} pending";
        var text = $"{ergebnis.Uebernommen.Count} Vorschläge übernommen → pending. Importiert wird erst mit " +
                   "„Import bestätigen“.";
        if (ergebnis.Uebersprungen.Count > 0)
            text += $" {ergebnis.Uebersprungen.Count} Datei(en) hattest du schon von Hand zugeordnet – die bleiben.";
        Add(ClaudeChatEntryKind.SystemOk, text);
    }

    /// <summary>"Nur diesen übernehmen" im Detail-Panel (Mockup Zustand 5). Parameter = relativer Pfad der Zeile.</summary>
    [RelayCommand]
    private async Task TakeOverOneAsync(string? relativePath)
    {
        if (Phase != ClaudeSortPhase.Draft || Finde(relativePath) is not { } zeile)
            return;
        var ergebnis = ClaudeSortUebernahme.Uebernehmen([zeile], _host.PendingStore);
        _uebernommen += ergebnis.Uebernommen.Count;
        EntferneAusEntwurf(zeile);
        await _host.ReanalyzeAsync();
        Add(ergebnis.Uebernommen.Count > 0 ? ClaudeChatEntryKind.SystemOk : ClaudeChatEntryKind.System,
            ergebnis.Uebernommen.Count > 0
                ? $"{zeile.Item.File.Scan.FileName} übernommen → pending."
                : $"{zeile.Item.File.Scan.FileName} hattest du schon von Hand zugeordnet – bleibt.");
    }

    /// <summary>"Ablehnen" im Detail-Panel: Vorschlag verworfen, die Zeile ist wieder offen.</summary>
    [RelayCommand]
    private void RejectOne(string? relativePath)
    {
        if (Phase != ClaudeSortPhase.Draft || Finde(relativePath) is not { } zeile)
            return;
        EntferneAusEntwurf(zeile);
        Add(ClaudeChatEntryKind.System, $"Vorschlag für {zeile.Item.File.Scan.FileName} abgelehnt – Zeile wieder offen.");
    }

    private ClaudeSortZeile? Finde(string? relativePath) =>
        relativePath is null ? null
            : _offen.FirstOrDefault(z => string.Equals(z.Item.File.Scan.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase));

    /// <summary>Zeile aus dem offenen Entwurf nehmen, Kennzeichen und Leiste nachziehen.</summary>
    private void EntferneAusEntwurf(ClaudeSortZeile zeile)
    {
        _offen.Remove(zeile);
        _marks.Remove(zeile.Item.File.Scan.RelativePath);
        _host.SetClaudeMarks(new Dictionary<string, ClaudeRowMark>(_marks, StringComparer.OrdinalIgnoreCase));
        AktualisiereEntwurfText();
        if (_offen.Count == 0)
        {
            Phase = _uebernommen > 0 ? ClaudeSortPhase.Applied : ClaudeSortPhase.Waiting;
            StatusDetail = _uebernommen > 0 ? $"{_uebernommen} pending" : "";
        }
    }

    [RelayCommand]
    private void Discard()
    {
        if (Phase != ClaudeSortPhase.Draft)
            return;
        _pruefung = null;
        _offen = [];
        _marks.Clear();
        _host.SetClaudeMarks(new Dictionary<string, ClaudeRowMark>());
        Phase = ClaudeSortPhase.Waiting;
        StatusDetail = "Entwurf verworfen";
        Add(ClaudeChatEntryKind.System,
            "Entwurf verworfen. Die Sitzung bleibt offen – du kannst Claude um einen neuen Entwurf bitten.");
    }

    [RelayCommand]
    private void CloseChat() => IsChatOpen = false;

    /// <summary>Sitzung beenden (Abbrechen in der Statuszeile): Prozess stoppen, Kennzeichen weg.</summary>
    [RelayCommand]
    private async Task AbortAsync()
    {
        await BeendeSitzungAsync();
        RaeumeAuftragAuf();
        _host.SetClaudeMarks(new Dictionary<string, ClaudeRowMark>());
        _auftrag = null;
        _pruefung = null;
        Phase = ClaudeSortPhase.Idle;
        StatusDetail = "";
        Add(ClaudeChatEntryKind.System, "Sitzung beendet. Deine Dateien sind unverändert.");
    }

    /// <summary>"Erneut prüfen" in der Fehlerkarte.</summary>
    [RelayCommand]
    private async Task RetryAsync()
    {
        await BeendeSitzungAsync();
        RaeumeAuftragAuf();
        _auftrag = null;
        Phase = ClaudeSortPhase.Idle;
        await StartAsync();
    }

    /// <summary>Sitzung beenden und die Auftragsdateien (mit Plankopf-Text) loeschen.</summary>
    public async ValueTask DisposeAsync()
    {
        await BeendeSitzungAsync();
        RaeumeAuftragAuf();
    }

    private void RaeumeAuftragAuf()
    {
        if (_auftrag is null)
            return;
        _builder.Aufraeumen(_auftrag.Ordner);
        // Erst nach BeendeSitzungAsync: der Prozess haelt die Verlaufsdatei offen
        _assistant?.DeleteSessionData(_auftrag.Ordner);
    }

    private async Task BeendeSitzungAsync()
    {
        if (_session is null)
            return;
        var session = _session;
        _session = null;
        await session.DisposeAsync();
        OnPropertyChanged(nameof(CanType));
    }

    private async Task BeantworteAsync(ClaudeQuestionViewModel question, IReadOnlyList<int> nummern)
    {
        question.IsAnswered = true;
        var text = ClaudeQuestionParser.FormatAnswer(question.Model, nummern);
        Add(ClaudeChatEntryKind.User, text);
        if (!await SendeAnClaudeAsync(text))
        {
            // Nicht angekommen: Knoepfe wieder freigeben, damit die Antwort erneut gewaehlt werden kann
            question.IsAnswered = false;
            foreach (var a in question.Answers)
                a.IsChecked = false;
        }
    }

    /// <summary>Sendet an Claude; false, wenn die Nachricht nicht angekommen ist (Meldung steht im Chat).</summary>
    private async Task<bool> SendeAnClaudeAsync(string text)
    {
        if (_session is null)
            return false;
        _retries = 0;
        Phase = ClaudeSortPhase.Running;
        StatusDetail = "";
        try
        {
            await _session.SendMessageAsync(text);
            return true;
        }
        catch (ClaudeAssistantException ex)
        {
            Add(ClaudeChatEntryKind.SystemError, ex.Message);
            Phase = ClaudeSortPhase.Waiting;
            return false;
        }
    }

    private void ZeigeNichtErreichbar(string grund)
    {
        UnavailableText = grund;
        StatusDetail = "";
        Phase = ClaudeSortPhase.Unavailable;
    }

    private void Add(ClaudeChatEntryKind kind, string text, ClaudeQuestionViewModel? question = null)
        => Entries.Add(new ClaudeChatEntry(kind, text, question));

    private static string Ausgenommen(IReadOnlyList<CaptureItem> items)
    {
        var teile = new List<string>();
        void Zaehle(CaptureBucket bucket, string einzahl, string mehrzahl)
        {
            var n = items.Count(i => i.Bucket == bucket);
            if (n > 0)
                teile.Add($"{n} {(n == 1 ? einzahl : mehrzahl)}");
        }
        Zaehle(CaptureBucket.UpdateProposal, "Update (bekannter Plan)", "Updates (bekannte Pläne)");
        Zaehle(CaptureBucket.Duplicate, "Dublette", "Dubletten");
        Zaehle(CaptureBucket.Conflict, "Konflikt", "Konflikte");
        return teile.Count == 0 ? "" : "ausgenommen: " + string.Join(", ", teile);
    }
}
