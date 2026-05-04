# Runde 2 — Claude-Prompt an ChatGPT

> **Hinweis:** Dieser Prompt wurde nachträglich aus dem Chat-Verlauf von Teil 20 rekonstruiert.

---

## Stand nach Runde 1

Wir haben uns auf folgendes geeinigt:
- `segment` als Default-Methode, `regex` als Fallback
- `SegmentPosition: int?` in `RecognitionRule`
- AND-Semantik, harte Migration ohne Legacy

In dieser Runde wollen wir die Detail-Fragen klären, die du am Ende von Runde 1 aufgeworfen hast.

## Kontext: Recognizer-Skizze

Der Recognizer würde so aussehen:

```csharp
public RecognitionResult Recognize(string fileName, RecognitionProfile profile)
{
    var context = _fileNameParser.Parse(fileName, profile.Tokenization);

    foreach (var rule in profile.Recognition)
    {
        if (!MatchesRule(context, rule))
            return RecognitionResult.NoMatch;
    }
    return RecognitionResult.Match(profile.DocumentTypeName);
}

private bool MatchesRule(RecognitionContext ctx, RecognitionRule rule)
{
    return rule.Method switch
    {
        "segment" => MatchesSegment(ctx, rule),
        "regex"   => Regex.IsMatch(ctx.BaseName, rule.Pattern, RegexOptions.IgnoreCase),
        _         => false  // unbekannte Methode → kein Match
    };
}

private bool MatchesSegment(RecognitionContext ctx, RecognitionRule rule)
{
    if (rule.SegmentPosition is null) return false;
    var pos = rule.SegmentPosition.Value;
    if (pos < 0 || pos >= ctx.Segments.Count) return false;
    return ctx.Segments[pos].RawValue.Equals(rule.Pattern, StringComparison.OrdinalIgnoreCase);
}
```

## Fragen

### Frage A — Cache-Granularität

Der Recognizer könnte einen Cache haben (Tokenisierungs-Ergebnisse). Auf welcher Ebene?

- **Pro Dateiname:** ein Cache-Eintrag pro Datei (Tokenisierung wird nicht doppelt gemacht)
- **Pro (Dateiname, Profil):** wenn Profile unterschiedliche Delimiter haben, müssen wir pro Profil cachen
- **Kein Cache:** Tokenisierung ist billig, Cache lohnt nicht

Mein Vorschlag: pro `(fileName, profileId)` cachen, weil Profile verschiedene Delimiter haben können.

### Frage B — Logging bei invaliden Rules

Wenn `MatchesRule` mit invalider Rule aufgerufen wird (z.B. `SegmentPosition` null), würde sonst pro Datei × Profil × Rule einmal geloggt werden — bei 100 Dateien × 10 Profile × 1 invalide Rule = 1000 Logs.

Optionen:
- **De-duplizieren** pro Recognize-Aufruf (z.B. HashSet von Rule-IDs)
- **ProfileManager.Load verwirft das ganze Profil** bei invalider Rule → Recognizer sieht nie invalide Rule → kein Logging-Problem im Hot Path
- **Debug-Level loggen** (Belt-and-Suspenders)

Mein Vorschlag: Letzteres ist sauber. Validierung gehört in den Lade-Pfad, nicht in den Match-Pfad.

### Frage C — MatchesSegment-Toleranz

Soll `MatchesSegment` tolerant sein gegenüber:
- Whitespace (`" PROT "` vs `"PROT"`)
- Trailing-/Leading-Sonderzeichen (`"-PROT"` vs `"PROT"`)
- Case (bereits via `OrdinalIgnoreCase`)

Mein Vorschlag: Schlank halten — kein Trim, kein Strip-Special-Chars. Der `FileNameParser` liefert bereits saubere Tokens. Wenn der User einen Delimiter falsch konfiguriert, soll das auffallen, nicht durch Toleranz-Logik kaschiert werden.

### Frage D — FileNameParser-Zentralisierung

Wizard und Recognizer müssen dieselbe Tokenisierung sehen. Vorschlag: Eine gemeinsame `IFileNameParser`-Komponente:

```csharp
public interface IFileNameParser
{
    RecognitionContext Parse(string fileName, TokenizationConfig config);
}

public record RecognitionContext(string BaseName, IReadOnlyList<Segment> Segments);
public record Segment(int Position, string RawValue);
```

`TokenizationConfig` enthält die `Delimiters` aus dem Profil. Der Wizard nutzt denselben Parser für die Live-Vorschau.

Stimmst du zu, oder siehst du einen Grund warum Wizard und Recognizer unterschiedliche Parser brauchen sollten?

### Frage E — Variable-Segment-Warnung

Wenn der User im Wizard ein Segment markiert, dessen Position bei anderen Dateien variieren könnte (z.B. weil manche Dateien nur 4 Segmente haben, andere 5), soll der Wizard warnen?

Mein Vorschlag: Ja — als UI-Hinweis ("Achtung: Segment-Position variiert über deine Beispiel-Dateien"), aber kein Hard-Fail. Der User kann die Rule trotzdem speichern, weil er es eventuell besser weiß.

---

Bitte gib deine Bewertung. Falls du einer Antwort widersprichst, bitte mit Begründung. Markiere am Ende:
- ✅ Worin du zustimmst
- ⚠️ Worin du widersprichst
- ❓ Was du noch geklärt haben willst

Canvas-Titel: **"Review Runde 2"**
