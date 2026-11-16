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

    /// <summary>Umgebungsvariablen, die Anmeldung oder Ziel von Claude Code umlenken.</summary>
    internal static bool IsRedirectingVariable(string name) =>
        name.Equals("ANTHROPIC_API_KEY", StringComparison.OrdinalIgnoreCase)
        || name.Equals("ANTHROPIC_AUTH_TOKEN", StringComparison.OrdinalIgnoreCase)
        || name.Equals("ANTHROPIC_BASE_URL", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("CLAUDE_CODE_USE_", StringComparison.OrdinalIgnoreCase);

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
