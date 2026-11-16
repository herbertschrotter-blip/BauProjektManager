namespace BauProjektManager.Infrastructure.Services.Claude;

/// <summary>
/// Test-Naht (CODING_STANDARDS 8.1): startet den Claude-Prozess. Echte Implementierung:
/// <see cref="SystemClaudeProcessRunner"/>; Tests ersetzen sie durch einen Fake ohne echtes claude.
/// </summary>
public interface IClaudeProcessRunner
{
    /// <summary>Startet den Prozess mit umgeleitetem stdin/stdout/stderr.</summary>
    /// <exception cref="Exception">Start fehlgeschlagen (z. B. Datei fehlt).</exception>
    IClaudeProcess Start(ClaudeProcessStartInfo startInfo);
}

/// <summary>Was gestartet wird: Programm, Argumente (einzeln, ohne Quoting), Arbeitsordner.</summary>
public sealed record ClaudeProcessStartInfo(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory);

/// <summary>
/// Ein laufender Prozess mit zeilenweisem stdin/stdout/stderr (UTF-8).
/// </summary>
public interface IClaudeProcess : IDisposable
{
    /// <summary>Naechste Zeile von stdout, NULL am Ende.</summary>
    Task<string?> ReadOutputLineAsync(CancellationToken cancellationToken);

    /// <summary>Naechste Zeile von stderr, NULL am Ende.</summary>
    Task<string?> ReadErrorLineAsync(CancellationToken cancellationToken);

    /// <summary>Schreibt eine Zeile (mit "\n") auf stdin und leert den Puffer.</summary>
    Task WriteInputLineAsync(string line, CancellationToken cancellationToken);

    /// <summary>Schliesst stdin — claude -p beendet sich danach von selbst.</summary>
    void CloseInput();

    /// <summary>Wartet auf das Prozessende und liefert den Exit-Code.</summary>
    Task<int> WaitForExitAsync(CancellationToken cancellationToken);

    /// <summary>Beendet den Prozess samt Kindprozessen hart. Wirft nicht.</summary>
    void Kill();
}
