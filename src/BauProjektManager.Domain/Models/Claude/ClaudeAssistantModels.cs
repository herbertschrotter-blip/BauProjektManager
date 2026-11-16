using BauProjektManager.Domain.Enums;

namespace BauProjektManager.Domain.Models.Claude;

/// <summary>
/// Ergebnis der Verfuegbarkeitspruefung von Claude Code (lokale CLI).
/// Prueft nur, ob die CLI startet und eine Version meldet — nicht, ob Herbert
/// angemeldet ist (das zeigt erst die erste Runde).
/// </summary>
/// <param name="IsAvailable">True, wenn die CLI gefunden wurde und mit Code 0 antwortet.</param>
/// <param name="Version">Gemeldete Version (z. B. "2.1.296 (Claude Code)"), sonst NULL.</param>
/// <param name="Message">Deutscher Hinweis fuer die Oberflaeche.</param>
public sealed record ClaudeAvailability(bool IsAvailable, string? Version, string Message);

/// <summary>
/// Auftrag zum Starten (oder Fortsetzen) einer Claude-Sitzung.
/// </summary>
/// <param name="WorkingDirectory">
/// Absoluter Pfad des Auftragsordners. Claude liest und schreibt nur dort. Muss ein echter Unterordner des
/// Auftrags-Basisordners sein (ClaudeAssistantOptions.JobRootDirectory).
/// </param>
/// <param name="FirstMessage">Erste Nachricht (Auftragstext). Pflicht bei neuer Sitzung, beim Fortsetzen optional.</param>
/// <param name="SystemPromptFile">
/// Absoluter Pfad einer Datei mit dem Zusatz zum System-Prompt (Rahmenregeln), NULL = keiner. Muss ausserhalb
/// des Auftragsordners liegen, damit Claude seine eigenen Regeln nicht umschreiben kann.
/// </param>
/// <param name="Model">Modell-Alias oder voller Name, NULL = Standard der CLI.</param>
/// <param name="ResumeSessionId">Session-ID einer frueheren Sitzung zum Fortsetzen, NULL = neue Sitzung.</param>
public sealed record ClaudeSessionRequest(
    string WorkingDirectory,
    string FirstMessage,
    string? SystemPromptFile = null,
    string? Model = null,
    string? ResumeSessionId = null);

/// <summary>
/// Ereignis aus einer Claude-Sitzung. Welche Felder gesetzt sind, haengt von <see cref="Kind"/> ab.
/// </summary>
/// <param name="Kind">Art des Ereignisses.</param>
/// <param name="Text">Text von Claude (Text), Fehlertext (Error, TurnCompleted mit IsError).</param>
/// <param name="ToolName">Werkzeugname (ToolUse), z. B. "Write".</param>
/// <param name="ToolTarget">Ziel des Werkzeugs — nur Datei-/Ordnername bzw. Muster, nie ein voller Pfad.</param>
/// <param name="IsError">TurnCompleted: Runde endete mit Fehler (z. B. Anmeldung abgelaufen).</param>
/// <param name="ExitCode">Exited: Exit-Code des Prozesses.</param>
/// <param name="CanContinue">Exited: True, wenn die naechste Nachricht die Sitzung fortsetzt (nicht vom User beendet).</param>
/// <param name="Questions">Text: geparste Rueckfragen (FRAGE-Format), leer wenn keine.</param>
public sealed record ClaudeEvent(
    ClaudeEventKind Kind,
    string? Text = null,
    string? ToolName = null,
    string? ToolTarget = null,
    bool IsError = false,
    int? ExitCode = null,
    bool CanContinue = false,
    IReadOnlyList<ClaudeQuestion>? Questions = null);

/// <summary>
/// Rueckfrage von Claude im Format "FRAGE:" / "FRAGE (mehrere):" mit nummerierten Antworten.
/// </summary>
/// <param name="Title">Fragetext nach "FRAGE:".</param>
/// <param name="AllowMultiple">True bei "FRAGE (mehrere):" — mehrere Antworten zugleich erlaubt.</param>
/// <param name="Answers">Antwortmoeglichkeiten in Claudes Reihenfolge (Empfehlung zuerst).</param>
/// <param name="LeadText">Text vor der ersten Frage der Nachricht (nur bei der ersten Frage gesetzt).</param>
public sealed record ClaudeQuestion(
    string Title,
    bool AllowMultiple,
    IReadOnlyList<ClaudeAnswerOption> Answers,
    string? LeadText = null);

/// <summary>
/// Eine Antwortmoeglichkeit einer Rueckfrage.
/// </summary>
/// <param name="Number">Nummer, wie Claude sie geschrieben hat (1, 2, …).</param>
/// <param name="Text">Antworttext ohne "(Empfehlung)" und ohne Erklaerung.</param>
/// <param name="IsRecommended">True, wenn Claude "(Empfehlung)" angehaengt hat.</param>
/// <param name="Explanation">Erklaerung nach " – " (Gedankenstrich), NULL wenn keine.</param>
public sealed record ClaudeAnswerOption(int Number, string Text, bool IsRecommended, string? Explanation = null);

/// <summary>
/// Fehler beim Fuehren einer Claude-Sitzung (Start, Senden). Die Nachricht ist deutsch
/// und fuer die Oberflaeche gedacht; sie enthaelt keine Pfade.
/// </summary>
public sealed class ClaudeAssistantException : Exception
{
    public ClaudeAssistantException(string message) : base(message) { }

    public ClaudeAssistantException(string message, Exception innerException) : base(message, innerException) { }
}
