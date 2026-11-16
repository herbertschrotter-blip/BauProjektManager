namespace BauProjektManager.Domain.Enums;

/// <summary>
/// Art eines Ereignisses aus einer Claude-Sitzung ("Mit Claude sortieren").
/// </summary>
public enum ClaudeEventKind
{
    /// <summary>Claude hat Text geschrieben (ggf. mit geparsten Rueckfragen).</summary>
    Text,

    /// <summary>Claude benutzt ein Werkzeug (Read, Write, Edit, Glob).</summary>
    ToolUse,

    /// <summary>Eine Runde (Antwort auf eine gesendete Nachricht) ist fertig.</summary>
    TurnCompleted,

    /// <summary>Fehler: Prozess unerwartet beendet, Runde abgebrochen o. Ae.</summary>
    Error,

    /// <summary>Der Claude-Prozess ist beendet (die Sitzung kann ggf. fortgesetzt werden).</summary>
    Exited
}
