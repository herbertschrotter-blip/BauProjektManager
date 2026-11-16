using BauProjektManager.Domain.Models.Claude;

namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// Port "Claude-Assistent": fuehrt Claude Code als lokalen Prozess fuer einen Auftrag
/// (Feature "Mit Claude sortieren"). Claude arbeitet nur im Auftragsordner (Read, Write,
/// Edit, Glob) und schreibt dort einen Entwurf; uebernommen und importiert wird nie von
/// Claude, sondern von BPM nach Pruefung und Bestaetigung durch den User.
///
/// Laeuft ueber Herberts Claude-Abo (CLI-Anmeldung), kein API-Key, kein HttpClient in BPM.
/// Einzige Implementierung: ClaudeCodeAssistant (Infrastructure).
/// </summary>
public interface IClaudeAssistant
{
    /// <summary>
    /// Prueft, ob Claude Code gefunden wird und startet (claude --version).
    /// Wirft nicht (ausser bei Abbruch ueber das Token) — das Ergebnis traegt einen deutschen Hinweis.
    /// </summary>
    Task<ClaudeAvailability> CheckAvailabilityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Startet eine Sitzung im Auftragsordner (bzw. setzt sie mit
    /// <see cref="ClaudeSessionRequest.ResumeSessionId"/> fort) und sendet die erste Nachricht.
    /// </summary>
    /// <param name="request">Auftragsordner, erste Nachricht, optional System-Zusatz/Modell/Fortsetzung.</param>
    /// <param name="events">
    /// Empfaenger der Ereignisse. Mit <see cref="Progress{T}"/> auf dem UI-Thread erzeugt,
    /// kommen die Ereignisse auf dem UI-Thread an; sonst auf einem Hintergrund-Thread.
    /// </param>
    /// <param name="cancellationToken">Bricht nur den Start ab, nicht die laufende Sitzung.</param>
    /// <exception cref="ClaudeAssistantException">Claude nicht gefunden, Ordner fehlt, Start fehlgeschlagen.</exception>
    Task<IClaudeSession> StartSessionAsync(
        ClaudeSessionRequest request,
        IProgress<ClaudeEvent> events,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Eine laufende Claude-Sitzung zu einem Auftrag. Beendet sich der Prozess (Leerlauf,
/// abgebrochene Runde), setzt die naechste Nachricht dieselbe Sitzung fort.
/// </summary>
public interface IClaudeSession : IAsyncDisposable
{
    /// <summary>Session-ID von Claude (UUID) — fuer spaeteres Fortsetzen speichern.</summary>
    string SessionId { get; }

    /// <summary>True, solange eine gesendete Nachricht noch keine fertige Runde hat.</summary>
    bool IsTurnOpen { get; }

    /// <summary>True, nachdem <see cref="EndAsync"/> aufgerufen wurde.</summary>
    bool IsEnded { get; }

    /// <summary>
    /// Sendet eine Nachricht des Users (z. B. Antwort auf eine Rueckfrage). Laeuft der Prozess
    /// nicht mehr, wird die Sitzung mit derselben Session-ID fortgesetzt.
    /// </summary>
    /// <exception cref="ClaudeAssistantException">Sitzung beendet, Text leer/zu lang, Senden fehlgeschlagen.</exception>
    Task SendMessageAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>Beendet die Sitzung endgueltig (Prozess stoppen, keine Fortsetzung mehr).</summary>
    Task EndAsync();
}
