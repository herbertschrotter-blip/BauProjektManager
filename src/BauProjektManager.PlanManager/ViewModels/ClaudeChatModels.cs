using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using BauProjektManager.Domain.Models.Claude;
using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>Art eines Eintrags im Chat von "Mit Claude sortieren" (Mockup 3.4).</summary>
public enum ClaudeChatEntryKind
{
    /// <summary>Nachricht von Claude.</summary>
    Claude,
    /// <summary>Nachricht von Herbert.</summary>
    User,
    /// <summary>Meldung der App (grau).</summary>
    System,
    /// <summary>Meldung der App, Erfolg (gruen).</summary>
    SystemOk,
    /// <summary>Meldung der App, Fehler (rot).</summary>
    SystemError,
    /// <summary>Werkzeugnutzung von Claude ("↳ liest kontext.json").</summary>
    Activity,
    /// <summary>Rueckfrage mit Antwort-Knoepfen.</summary>
    Question
}

/// <summary>Ein Eintrag im Chatverlauf.</summary>
public sealed class ClaudeChatEntry(ClaudeChatEntryKind kind, string text, ClaudeQuestionViewModel? question = null)
{
    public ClaudeChatEntryKind Kind { get; } = kind;
    public string Text { get; } = text;
    public ClaudeQuestionViewModel? Question { get; } = question;
}

/// <summary>Rueckfrage im FRAGE-Format: Knoepfe je Antwort, bei "mehrere" Kontrollkaestchen.</summary>
public sealed partial class ClaudeQuestionViewModel : ObservableObject
{
    public ClaudeQuestionViewModel(ClaudeQuestion model)
    {
        Model = model;
        Answers = new(model.Answers.Select(a => new ClaudeAnswerViewModel(this, a)));
        foreach (var a in Answers)
            a.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ClaudeAnswerViewModel.IsChecked))
                    OnPropertyChanged(nameof(SendMultipleText));
            };
    }

    public ClaudeQuestion Model { get; }
    public string Title => Model.Title;
    public bool AllowMultiple => Model.AllowMultiple;
    public string CaptionText => AllowMultiple ? "RÜCKFRAGE · MEHRERE MÖGLICH" : "RÜCKFRAGE";
    public ObservableCollection<ClaudeAnswerViewModel> Answers { get; }

    /// <summary>True, sobald beantwortet — Knoepfe werden gesperrt, die gewaehlte bleibt markiert.</summary>
    [ObservableProperty]
    private bool _isAnswered;

    public string SendMultipleText => $"Antworten senden ({Answers.Count(a => a.IsChecked)})";

    /// <summary>Gewaehlte Nummern (Mehrfach: angekreuzte).</summary>
    public IReadOnlyList<int> CheckedNumbers => [.. Answers.Where(a => a.IsChecked).Select(a => a.Number)];
}

/// <summary>Eine Antwortmoeglichkeit (Empfehlung mit Akzent-Rahmen, Erklaerung klein darunter).</summary>
public sealed partial class ClaudeAnswerViewModel(ClaudeQuestionViewModel question, ClaudeAnswerOption model)
    : ObservableObject
{
    public ClaudeQuestionViewModel Question { get; } = question;
    public int Number => model.Number;
    public string Text => model.Text;
    public string? Explanation => model.Explanation;
    public bool HasExplanation => !string.IsNullOrWhiteSpace(model.Explanation);
    public bool IsRecommended => model.IsRecommended;

    [ObservableProperty]
    private bool _isChecked;
}

/// <summary>Kennzeichnung einer Tabellenzeile waehrend "Mit Claude sortieren".</summary>
public enum ClaudeRowMarkKind
{
    /// <summary>Datei liegt bei Claude ("◌ bei Claude").</summary>
    AtClaude,
    /// <summary>Gueltiger Vorschlag, noch nicht uebernommen.</summary>
    Draft,
    /// <summary>Gueltiger Vorschlag mit unsicheren Feldern.</summary>
    DraftUncertain,
    /// <summary>Kein gueltiger Vorschlag (nicht zuordenbar oder von der Pruefung abgelehnt).</summary>
    Invalid
}

/// <summary>Kennzeichnung samt Anzeigetext und (bei Vorschlaegen) Begruendung.</summary>
public sealed record ClaudeRowMark(ClaudeRowMarkKind Kind, string Text, ClaudeSuggestionInfo? Info = null);
