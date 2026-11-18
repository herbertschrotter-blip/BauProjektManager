namespace BauProjektManager.Infrastructure.Services.Claude;

/// <summary>
/// Einstellungen fuer <see cref="ClaudeCodeAssistant"/>. Standardwerte nach dem Vorbild
/// tools/chat_dienst.py (ha-hausbau): Leerlauf 15 min, Stille je Runde 10 min, 5 s bis Kill.
/// </summary>
public sealed class ClaudeAssistantOptions
{
    /// <summary>
    /// Standard-Basisordner der Auftraege: %LOCALAPPDATA%\BauProjektManager\claude — ausserhalb von Projekt-,
    /// Cloud- und Repo-Ordnern (ADR-067 P.3). Je Projekt ein Unterordner, darin A-0001, A-0002 …
    /// </summary>
    public static string DefaultJobRoot() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BauProjektManager", "claude");

    /// <summary>Werkzeuge, die Claude im Auftragsordner benutzen darf (sonst keine).</summary>
    public static readonly IReadOnlyList<string> DefaultTools = ["Read", "Write", "Edit", "Glob"];

    /// <summary>
    /// Pfad zur claude.exe. NULL/leer = automatisch suchen (PATH, dann
    /// %USERPROFILE%\.local\bin\claude.exe). Ein gesetzter, aber fehlender Pfad gilt als "nicht verfuegbar".
    /// </summary>
    public string? ExecutablePath { get; init; }

    /// <summary>
    /// Basisordner aller Auftragsordner. Claude startet nur in einem echten Unterordner davon — sonst
    /// bekaeme Claude mit acceptEdits Schreibrecht auf einen falschen Baum (z. B. den Projektordner).
    /// NULL/leer = es wird keine Sitzung gestartet.
    /// </summary>
    public string? JobRootDirectory { get; init; }

    /// <summary>
    /// Konfigurationsordner von Claude Code (dort liegen unter projects/ die Sitzungsverlaeufe).
    /// NULL/leer = wie Claude Code selbst: CLAUDE_CONFIG_DIR, sonst %USERPROFILE%\.claude.
    /// </summary>
    public string? ConfigDirectory { get; init; }

    /// <summary>Erlaubte Werkzeuge (--tools und --allowedTools).</summary>
    public IReadOnlyList<string> Tools { get; init; } = DefaultTools;

    /// <summary>Ohne offene Runde so lange untaetig → Prozess beenden (Sitzung bleibt fortsetzbar).</summary>
    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>In einer offenen Runde so lange keine Ausgabe → Runde abbrechen.</summary>
    public TimeSpan TurnSilenceTimeout { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>Nach dem Schliessen von stdin so lange warten, dann Prozessbaum beenden.</summary>
    public TimeSpan StopGracePeriod { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Hoechstdauer fuer "claude --version" bei der Verfuegbarkeitspruefung.</summary>
    public TimeSpan AvailabilityTimeout { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>Laengste Nachricht an Claude (Zeichen).</summary>
    public int MaxMessageLength { get; init; } = 20_000;
}
