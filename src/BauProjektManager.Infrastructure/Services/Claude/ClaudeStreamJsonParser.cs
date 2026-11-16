using System.Text.Json;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models.Claude;

namespace BauProjektManager.Infrastructure.Services.Claude;

/// <summary>
/// Ergebnis einer stream-json-Zeile: ggf. die Session-ID und die daraus entstehenden Ereignisse.
/// <see cref="IsInit"/> = system/init: Claude hat die Sitzung angelegt (ab hier gilt --resume).
/// </summary>
public sealed record ClaudeStreamLine(string? SessionId, IReadOnlyList<ClaudeEvent> Events, bool IsInit = false)
{
    public static readonly ClaudeStreamLine Empty = new(null, []);
}

/// <summary>
/// Liest eine Zeile der Ausgabe von "claude -p --output-format stream-json --verbose"
/// (reine Funktion). Ausgewertet werden:
/// <list type="bullet">
/// <item><c>system/init</c> → Session-ID</item>
/// <item><c>assistant</c> → Text (mit Rueckfragen) und Werkzeugnutzung</item>
/// <item><c>result</c> → Runde fertig (mit Fehler-Flag)</item>
/// </list>
/// Alles andere (user/tool_result, stream_event, kaputte Zeilen) wird ignoriert.
/// </summary>
public static class ClaudeStreamJsonParser
{
    public static ClaudeStreamLine Parse(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return ClaudeStreamLine.Empty;

        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return ClaudeStreamLine.Empty;

            return GetString(root, "type") switch
            {
                "system" => new ClaudeStreamLine(GetString(root, "session_id"), [],
                    IsInit: GetString(root, "subtype") == "init"),
                "assistant" => new ClaudeStreamLine(GetString(root, "session_id"), ParseAssistant(root)),
                "result" => new ClaudeStreamLine(GetString(root, "session_id"), [ParseResult(root)]),
                _ => ClaudeStreamLine.Empty
            };
        }
        catch (JsonException)
        {
            return ClaudeStreamLine.Empty;
        }
    }

    private static List<ClaudeEvent> ParseAssistant(JsonElement root)
    {
        var events = new List<ClaudeEvent>();
        if (!root.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object
            || !message.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            return events;

        var text = new System.Text.StringBuilder();
        foreach (var block in content.EnumerateArray())
        {
            if (block.ValueKind != JsonValueKind.Object)
                continue;
            switch (GetString(block, "type"))
            {
                case "text":
                    text.Append(GetString(block, "text"));
                    break;
                case "tool_use":
                    events.Add(new ClaudeEvent(
                        ClaudeEventKind.ToolUse,
                        ToolName: GetString(block, "name") ?? string.Empty,
                        ToolTarget: ToolTarget(block)));
                    break;
            }
        }

        var full = text.ToString();
        if (!string.IsNullOrWhiteSpace(full))
            events.Add(new ClaudeEvent(ClaudeEventKind.Text, Text: full, Questions: ClaudeQuestionParser.Parse(full)));
        return events;
    }

    private static ClaudeEvent ParseResult(JsonElement root)
    {
        var isError = root.TryGetProperty("is_error", out var flag) && flag.ValueKind == JsonValueKind.True;
        return new ClaudeEvent(
            ClaudeEventKind.TurnCompleted,
            Text: isError ? GetString(root, "result") : null,
            IsError: isError);
    }

    /// <summary>
    /// Ziel eines Werkzeugs fuer die Anzeige: bei Pfaden nur der Datei-/Ordnername (DSGVO-Pfadregel),
    /// bei Glob das Muster.
    /// </summary>
    private static string? ToolTarget(JsonElement block)
    {
        if (!block.TryGetProperty("input", out var input) || input.ValueKind != JsonValueKind.Object)
            return null;

        var path = GetString(input, "file_path") ?? GetString(input, "path");
        if (!string.IsNullOrWhiteSpace(path))
        {
            // Pure Pfad-String-Operation ohne Disk-Zugriff (ADR-060 Praezisierung)
            var name = Path.GetFileName(path.TrimEnd('\\', '/'));
            return string.IsNullOrEmpty(name) ? null : name;
        }

        // Glob-Muster koennen einen absoluten Pfad tragen (Benutzername) — nur das letzte Segment zeigen
        var pattern = GetString(input, "pattern");
        return pattern is not null && Path.IsPathRooted(pattern)
            ? Path.GetFileName(pattern.TrimEnd('\\', '/'))
            : pattern;
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
