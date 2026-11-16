using System.Diagnostics;
using System.Text;

namespace BauProjektManager.Infrastructure.Services.Claude;

/// <summary>
/// Startet Claude Code als echten Prozess (System.Diagnostics.Process) ohne Fenster,
/// mit umgeleitetem stdin/stdout/stderr in UTF-8.
/// </summary>
public sealed class SystemClaudeProcessRunner : IClaudeProcessRunner
{
    public IClaudeProcess Start(ClaudeProcessStartInfo startInfo)
    {
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var psi = new ProcessStartInfo(startInfo.FileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = utf8,
            StandardOutputEncoding = utf8,
            StandardErrorEncoding = utf8,
            WorkingDirectory = startInfo.WorkingDirectory
        };
        foreach (var argument in startInfo.Arguments)
            psi.ArgumentList.Add(argument);

        // Herberts Abo (CLI-Anmeldung) statt API-Key: ein gesetzter Key, Token oder anderer Endpunkt
        // (Bedrock/Vertex/Proxy) haette sonst Vorrang bzw. schickte die Daten woandershin
        foreach (var name in psi.Environment.Keys.Where(IsRedirectingVariable).ToList())
            psi.Environment.Remove(name);

        var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Prozess wurde nicht gestartet.");
        process.StandardInput.NewLine = "\n";
        return new SystemClaudeProcess(process);
    }

    /// <summary>
    /// Umgebungsvariablen, die Anmeldung, Ziel oder Betriebsart von Claude Code umlenken: alle ANTHROPIC_*
    /// (Key, Token, Base-URL, Modell) und alle CLAUDE_CODE_* bis auf den Pfad zur Git-Bash. Wird BPM aus einer
    /// Claude-Sitzung heraus gestartet (Desktop-App, Terminal), erbt es sonst deren Host-Anmeldung und z. B.
    /// CLAUDE_CODE_SIMPLE (Bare-Modus nur mit API-Key) — der Kindprozess meldet dann "Failed to authenticate".
    /// </summary>
    internal static bool IsRedirectingVariable(string name) =>
        name.StartsWith("ANTHROPIC_", StringComparison.OrdinalIgnoreCase)
        || name.Equals("CLAUDECODE", StringComparison.OrdinalIgnoreCase)
        || (name.StartsWith("CLAUDE_CODE_", StringComparison.OrdinalIgnoreCase)
            && !name.Equals("CLAUDE_CODE_GIT_BASH_PATH", StringComparison.OrdinalIgnoreCase));

    private sealed class SystemClaudeProcess(Process process) : IClaudeProcess
    {
        public async Task<string?> ReadOutputLineAsync(CancellationToken cancellationToken) =>
            await process.StandardOutput.ReadLineAsync(cancellationToken);

        public async Task<string?> ReadErrorLineAsync(CancellationToken cancellationToken) =>
            await process.StandardError.ReadLineAsync(cancellationToken);

        public async Task WriteInputLineAsync(string line, CancellationToken cancellationToken)
        {
            await process.StandardInput.WriteLineAsync(line.AsMemory(), cancellationToken);
            await process.StandardInput.FlushAsync(cancellationToken);
        }

        public void CloseInput()
        {
            try
            {
                process.StandardInput.Close();
            }
            catch (IOException)
            {
                // Pipe schon zu (Prozess beendet) — nichts zu tun
            }
        }

        public async Task<int> WaitForExitAsync(CancellationToken cancellationToken)
        {
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode;
        }

        public void Kill()
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (Exception)
            {
                // Prozess bereits beendet oder Zugriff verweigert — Kill ist best effort
            }
        }

        public void Dispose() => process.Dispose();
    }
}
