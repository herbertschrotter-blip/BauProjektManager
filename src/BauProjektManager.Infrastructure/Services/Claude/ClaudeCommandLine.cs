namespace BauProjektManager.Infrastructure.Services.Claude;

/// <summary>
/// Baut die Argumente fuer Claude Code und findet die claude.exe (reine Funktionen).
/// Optionen geprueft gegen "claude --help" (CLI 2.1.296):
/// <list type="bullet">
/// <item><c>-p --input-format stream-json --output-format stream-json --verbose</c>: Gespraech ueber stdin/stdout</item>
/// <item><c>--restricted</c>: keine Werkzeuge, die Befehle/Code ausfuehren, kein WebFetch; Datei-Werkzeuge nur im
/// Arbeitsordner; Benutzer-/Projekt-Settings werden ignoriert</item>
/// <item><c>--strict-mcp-config</c>: keine MCP-Server</item>
/// <item><c>--safe-mode</c>: keine CLAUDE.md, Skills, Plugins, Hooks, Memory des Rechners (Anmeldung bleibt)</item>
/// <item><c>--append-system-prompt-file</c>: Rahmen aus einer Datei ausserhalb des Auftragsordners (keine
/// Laengengrenze der Kommandozeile, Claude kann ihn nicht umschreiben)</item>
/// <item><c>--tools</c> + <c>--allowedTools</c>: nur Read, Write, Edit, Glob — vorhanden und ohne Rueckfrage erlaubt</item>
/// <item><c>--permission-mode acceptEdits</c> + <c>--permission-prompts none</c>: Schreiben im Ordner ohne Rueckfrage,
/// alles, was eine Rueckfrage braeuchte, wird abgelehnt statt zu haengen</item>
/// <item><c>--session-id</c> (neu) bzw. <c>--resume</c> (fortsetzen)</item>
/// </list>
/// </summary>
public static class ClaudeCommandLine
{
    /// <summary>Dateiname der CLI unter Windows (nur .exe — .cmd wuerde Argumente ueber cmd.exe verfaelschen).</summary>
    public const string ExecutableName = "claude.exe";

    /// <summary>Argumente fuer eine Sitzung im Auftragsordner.</summary>
    /// <param name="tools">Erlaubte Werkzeuge.</param>
    /// <param name="sessionId">Session-ID (UUID).</param>
    /// <param name="resume">True = bestehende Sitzung fortsetzen (--resume), sonst neu (--session-id).</param>
    /// <param name="systemPromptFile">Datei mit dem Zusatz zum System-Prompt, NULL/leer = keiner.</param>
    /// <param name="model">Modell, NULL/leer = Standard.</param>
    public static IReadOnlyList<string> BuildSessionArguments(
        IReadOnlyList<string> tools,
        string sessionId,
        bool resume,
        string? systemPromptFile,
        string? model)
    {
        var toolList = string.Join(",", tools);
        var args = new List<string>
        {
            "-p",
            "--input-format", "stream-json",
            "--output-format", "stream-json",
            "--verbose",
            "--restricted",
            "--strict-mcp-config",
            "--safe-mode",
            "--tools", toolList,
            "--allowedTools", toolList,
            "--permission-mode", "acceptEdits",
            "--permission-prompts", "none"
        };
        if (!string.IsNullOrWhiteSpace(systemPromptFile))
            args.AddRange(["--append-system-prompt-file", systemPromptFile]);
        if (!string.IsNullOrWhiteSpace(model))
            args.AddRange(["--model", model]);
        args.AddRange([resume ? "--resume" : "--session-id", sessionId]);
        return args;
    }

    /// <summary>
    /// Findet die claude.exe: konfigurierter Pfad (muss existieren, voll qualifiziert sein und auf .exe enden —
    /// .cmd/.bat liefen ueber cmd.exe, das Argumente auswertet), sonst PATH (nur voll qualifizierte Eintraege), sonst
    /// %USERPROFILE%\.local\bin\claude.exe (Standardort des Installers). NULL = nicht gefunden.
    /// </summary>
    /// <param name="configuredPath">Pfad aus den Einstellungen, NULL/leer = automatisch.</param>
    /// <param name="pathVariable">Inhalt der PATH-Variable.</param>
    /// <param name="userProfile">Benutzerprofil-Ordner.</param>
    /// <param name="fileExists">Pruefung, ob eine Datei existiert.</param>
    public static string? ResolveExecutable(
        string? configuredPath,
        string? pathVariable,
        string? userProfile,
        Func<string, bool> fileExists)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return Path.IsPathFullyQualified(configuredPath)
                && configuredPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                && fileExists(configuredPath) ? configuredPath : null;

        var candidates = (pathVariable ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(dir => dir.Trim('"'))
            .Where(Path.IsPathFullyQualified)
            .Select(dir => Path.Combine(dir, ExecutableName));
        if (!string.IsNullOrWhiteSpace(userProfile))
            candidates = candidates.Append(Path.Combine(userProfile, ".local", "bin", ExecutableName));

        return candidates.FirstOrDefault(fileExists);
    }
}
