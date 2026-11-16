using System.Text.RegularExpressions;
using BauProjektManager.Domain.Models.Claude;

namespace BauProjektManager.Infrastructure.Services.Claude;

/// <summary>
/// Liest Rueckfragen aus einer Nachricht von Claude (reine Funktion, keine Abhaengigkeiten).
/// Format wie im Auftragsrahmen (Vorbild ha-hausbau, _rahmen.md / logik/claude.py):
/// <code>
/// FRAGE: Welcher Dokumenttyp?
/// 1. Polierplan (Empfehlung)
/// 2. Schalplan – Plankopf nennt "Schalung"
/// </code>
/// "FRAGE (mehrere):" erlaubt mehrere Antworten; Erklaerung nach " – " (Gedanken-, nicht
/// Bindestrich, weil Planbezeichnungen wie "Haus 1 - OG" Bindestriche tragen); bis zu vier
/// Bloecke je Nachricht. Ohne FRAGE-Format greift ein Rueckfall: eine Frage mit Aufzaehlung
/// darunter oder eine Ja/Nein-Frage am Ende.
/// </summary>
public static partial class ClaudeQuestionParser
{
    /// <summary>Hoechstens so viele FRAGE-Bloecke je Nachricht.</summary>
    public const int MaxQuestions = 4;

    private const string RecommendedMarker = "(Empfehlung)";

    [GeneratedRegex(@"^FRAGE(\s*\(mehrere\))?:\s*(.+)$")]
    private static partial Regex QuestionLine();

    [GeneratedRegex(@"^\s*(\d{1,2})[.)]\s+(.+)$")]
    private static partial Regex AnswerLine();

    [GeneratedRegex(@"^\s*(?:(\d{1,2})[.)]|([a-hA-H])[.)]|[-•*])\s+(.+)$")]
    private static partial Regex EnumerationLine();

    [GeneratedRegex(@"\s+[–—]\s+")]
    private static partial Regex ExplanationSeparator();

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceBoundary();

    // Ja/Nein-Frage: beginnt mit einem Verb (Soll ich …?, Ist das …?, Passt …?)
    [GeneratedRegex(
        @"^(soll|sollen|ist|sind|passt|stimmt|darf|dürfen|kann|können|gehört|gehören|zählt|zählen|war|waren|hast|" +
        @"habe|hat|haben|willst|möchtest|wollen|gilt|gelten|bleibt|bleiben|nehme|nehmen|übernehme|trage|lasse|" +
        @"mache|machen|gibt|liegt|meinst|brauchst|brauche|wird|werden)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex YesNoStart();

    /// <summary>
    /// Alle Rueckfragen einer Nachricht (hoechstens <see cref="MaxQuestions"/>); leer, wenn keine.
    /// Die erste Frage traegt den Text davor als <see cref="ClaudeQuestion.LeadText"/>.
    /// </summary>
    public static IReadOnlyList<ClaudeQuestion> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var lines = text.Split('\n').Select(l => l.Trim().Trim('`').Trim()).ToList();
        var questions = ParseFormatted(lines);
        if (questions.Count > 0)
            return questions;

        var fallback = ParseWithoutFormat(lines);
        return fallback is null ? [] : [fallback];
    }

    /// <summary>
    /// Antworttext fuer gewaehlte Nummern, wie er an Claude zurueckgeht:
    /// "2. Schalplan", bei mehreren "1. Polierplan, 3. Bewehrungsplan".
    /// Unbekannte Nummern werden ignoriert.
    /// </summary>
    public static string FormatAnswer(ClaudeQuestion question, IEnumerable<int> numbers)
    {
        var parts = numbers
            .Select(nr => question.Answers.FirstOrDefault(a => a.Number == nr))
            .Where(a => a is not null)
            .Select(a => $"{a!.Number}. {a.Text}");
        return string.Join(", ", parts);
    }

    private static List<ClaudeQuestion> ParseFormatted(List<string> lines)
    {
        var result = new List<ClaudeQuestion>();
        int? firstQuestionLine = null;
        var i = 0;
        while (i < lines.Count)
        {
            var match = QuestionLine().Match(lines[i]);
            if (!match.Success)
            {
                i++;
                continue;
            }

            var answers = new List<ClaudeAnswerOption>();
            var j = i + 1;
            while (j < lines.Count)
            {
                var line = lines[j];
                var answer = AnswerLine().Match(line);
                if (answer.Success)
                    answers.Add(BuildAnswer(int.Parse(answer.Groups[1].Value), answer.Groups[2].Value.Trim()));
                else if ((answers.Count > 0 && line.Length > 0) || QuestionLine().IsMatch(line))
                    break;
                j++;
            }

            if (answers.Count > 0)
            {
                firstQuestionLine ??= i;
                result.Add(new ClaudeQuestion(match.Groups[2].Value.Trim(), match.Groups[1].Success, answers));
            }
            i = j;
        }

        if (result.Count == 0)
            return result;

        var lead = string.Join("\n", lines.Take(firstQuestionLine!.Value)).Trim();
        result[0] = result[0] with { LeadText = lead.Length > 0 ? lead : null };
        return result.Take(MaxQuestions).ToList();
    }

    private static ClaudeAnswerOption BuildAnswer(int number, string raw)
    {
        var recommended = raw.Contains(RecommendedMarker, StringComparison.Ordinal);
        var text = raw.Replace(RecommendedMarker, string.Empty, StringComparison.Ordinal).Trim();
        var parts = ExplanationSeparator().Split(text, 2);
        if (parts.Length == 2 && parts[0].Length > 0 && parts[1].Length > 0)
            return new ClaudeAnswerOption(number, parts[0].Trim(), recommended, parts[1].Trim());
        return new ClaudeAnswerOption(number, text, recommended);
    }

    /// <summary>
    /// Rueckfall: Claude fragt ohne "FRAGE:". Eine Frage mit Aufzaehlung darunter gibt je Punkt
    /// eine Antwort; eine Ja/Nein-Frage am Ende gibt "Ja" und "Nein". Sonst keine Frage.
    /// </summary>
    private static ClaudeQuestion? ParseWithoutFormat(List<string> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (!line.EndsWith('?') && !line.EndsWith("?:", StringComparison.Ordinal))
                continue;

            var answers = new List<ClaudeAnswerOption>();
            foreach (var next in lines.Skip(i + 1))
            {
                var item = EnumerationLine().Match(next);
                if (!item.Success)
                {
                    // Strenger als der FRAGE-Block: Fliesstext zwischen Frage und Aufzaehlung beendet die Suche,
                    // sonst wuerde eine weiter unten stehende Liste ("Erledigt: - …") zu Antworten
                    if (next.Length > 0)
                        break;
                    continue;
                }
                answers.Add(BuildAnswer(answers.Count + 1, item.Groups[3].Value.Trim()));
            }

            if (answers.Count >= 2)
                return new ClaudeQuestion(line.TrimEnd(':'), false, answers, LeadOrNull(lines.Take(i), "\n"));
        }

        return ParseYesNo(lines);
    }

    private static ClaudeQuestion? ParseYesNo(List<string> lines)
    {
        var text = string.Join(" ", lines.Where(l => l.Length > 0)).Trim();
        if (!text.EndsWith('?'))
            return null;

        var sentences = SentenceBoundary().Split(text);
        var last = sentences[^1];
        if (!YesNoStart().IsMatch(last))
            return null;

        return new ClaudeQuestion(
            last,
            false,
            [new ClaudeAnswerOption(1, "Ja", false), new ClaudeAnswerOption(2, "Nein", false)],
            LeadOrNull(sentences.Take(sentences.Length - 1), " "));
    }

    private static string? LeadOrNull(IEnumerable<string> parts, string separator)
    {
        var lead = string.Join(separator, parts).Trim();
        return lead.Length > 0 ? lead : null;
    }
}
