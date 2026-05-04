# Runde 2 — ChatGPT-Antwort

# Review Runde 2

Claude, ich bestätige den neuen Schnitt fast vollständig. Dass es **keine Produktivdaten** gibt, vereinfacht BPM-082 massiv und macht das Entfernen von `prefix/contains` jetzt plausibel statt riskant.

Im Branch **`feature/planmanager-v1`** existiert `FileNameParser` bereits als statische, reine Logik mit `TokenizationConfig`, inklusive `Path.GetFileNameWithoutExtension`, `CollapseRepeatedDelimiters` und `FirstTokenDelimiter` — also genau der richtige Kandidat für die Wiederverwendung im Recognizer (`src/BauProjektManager.PlanManager/Services/FileNameParser.cs`, Branch `feature/planmanager-v1`).

## 1. Bestätigung/Widerspruch zu deinen Entscheidungen 1–8

### 1. Datenmodell

**Bestätigt.**
`SegmentPosition: int?`, `IsValid(out string reason)`, `Method = "segment"` und `SchemaVersion = 3` sind für den aktuellen Reifegrad der sauberste Kompromiss.

Ein Zusatz: `IsValid` sollte nicht nur vom Loader, sondern auch **vor dem Speichern** genutzt werden. Sonst hast du Validierungslogik, die nur beim Lesen greift.

### 2. RecognitionContext

**Bestätigt.**
Das ist jetzt die richtige Schnittstelle. Der lokale Cache pro `Recognize(...)`-Aufruf ist exakt der richtige Scope.

### 3. Case-Sensitivity

**Bestätigt.**
`OrdinalIgnoreCase` reicht. Keine Persistenz-Normalisierung.

### 4. Migration obsolet / `prefix` + `contains` raus

**Im Grundsatz bestätigt, mit einem kleinen Vorbehalt.**

Architektonisch passt das jetzt, **weil keine Altprofile existieren**. Ich würde aber `regex` nicht nur als Spezialfall "behalten", sondern im Code explizit als **nicht-default expert path** behandeln. Also:

* Wizard speichert standardmäßig nur `segment`
* `regex` nur über explizite Spezialaktion
* Recognizer kennt nur `segment` + `regex`

Das ist konsistent.

### 5. AND-Semantik

**Bestätigt.**
Bleibt so.

### 6. Performance / Cache

**Bestätigt, mit Präzisierung beim Key** — dazu unten bei Rückfrage 3.

### 7. Reihenfolge

**Bestätigt.**
Die neue Reihenfolge ist besser als der ursprüngliche Fluss.

### 8. Weitere Punkte

**Fast alles bestätigt.** Zwei Anmerkungen:

* **a) Delimiter-Drift**: ja, das ist weiterhin der wichtigste Punkt.
* **b) Variable Segmente**: Hinweis ja, Blockade nein. Dazu unten.
* **d) Mischszenarien-Tests entfallen**: korrekt, wenn `prefix/contains` wirklich konsequent entfernt werden.
* **f) SchemaVersion 3**: richtig.
* **g) Logging**: richtig, aber `unknown method` sollte bei fehlender Altkompatibilität eher **Warning** sein, nicht Debug.

## 2. Antworten auf die 4 Rückfragen

### Rückfrage 1 — Zentraler Parser

Ich sehe **keinen guten Grund für ein neues `ITokenizerService`**.

Direkter Aufruf des bestehenden statischen `FileNameParser` ist hier die bessere Lösung, weil:

* derselbe Parser bereits existiert im Branch `feature/planmanager-v1`
* er ist rein funktional, ohne externe Abhängigkeiten
* kein DI-Container nötig
* geringste Drift-Gefahr
* geringster Umbau

Ein neues Service-Interface würde aktuell nur zwei Nachteile bringen:

1. mehr Struktur ohne fachlichen Mehrwert
2. Gefahr, dass später zwei Tokenisierungspfade entstehen: `FileNameParser` und `ITokenizerService`-Implementierung

Mein klares Votum:

* **`BuildRecognitionContext` ruft direkt `FileNameParser.Parse(fileName, profile.Tokenization)` auf**
* kein zusätzlicher Tokenizer-Service in BPM-082

Nur eine Kleinigkeit: `BuildRecognitionContext` sollte **nicht selbst erneut Extension-Stripping oder Split-Logik enthalten**, sondern wirklich nur auf `FileNameParser.Parse(...)` aufsetzen. Sonst baust du dir trotz gleicher Klasse wieder Drift.

Saubere Skizze:

```csharp
private static RecognitionContext BuildRecognitionContext(
    string fileName,
    RecognitionProfile profile)
{
    var parsed = FileNameParser.Parse(fileName, profile.Tokenization);

    return new RecognitionContext
    {
        FileName = fileName,
        FileStem = parsed.BaseName,
        Tokens = parsed.Segments
            .OrderBy(s => s.Position)
            .Select(s => s.RawValue)
            .ToList()
    };
}
```

### Rückfrage 2 — Warnung bei variablen Segmenten

Der vorgeschlagene **inline-Hinweis reicht**.
Ich würde den Weiter-Button **nicht** deaktivieren und auch kein "trotzdem verwenden"-Checkbox-Gate einbauen.

Warum ich gegen Blocking bin:

* in früher Entwicklung bremst das unnötig
* es gibt legitime Sonderfälle
* "niemals falsch zuordnen" wird primär durch `segment` + AND + Priorität geschützt, nicht durch harte UI-Verbote
* eine zusätzliche Bestätigungslogik macht den Wizard schwerer als nötig

Also:

* **Warnung anzeigen**
* Speichern weiter erlauben
* optional die Warnung im Test/Preview-Text klar formulieren

Ich würde die Warnlogik nicht nur an `FieldType`, sondern zusätzlich an **Tokenform** koppeln. Beispiel:

Warnen wenn:

* `FieldType` ist `PlanNumber`, `PlanIndex`, `Datum`
* oder Token ist überwiegend numerisch
* oder Token passt auf Datums-/Indexmuster

Denn "freie numerische Tokens" erkennt man nicht nur über `FieldType`.

Pragmatisch:

```csharp
private static bool IsLikelyVariableRecognitionSegment(FileNameSegment segment)
{
    if (segment.FieldType is FieldType.PlanNumber
        or FieldType.PlanIndex
        or FieldType.Datum)
        return true;

    var value = segment.RawValue?.Trim() ?? "";
    if (string.IsNullOrEmpty(value))
        return false;

    if (value.All(char.IsDigit))
        return true;

    if (DateTime.TryParse(value, out _))
        return true;

    return false;
}
```

Das ist nicht perfekt, aber für einen Wizard-Hinweis gut genug.

### Rückfrage 3 — Cache-Key

Hier widerspreche ich deinem vereinfachten Key **teilweise**.

`(fileName, profile.Id)` ist korrekt, aber unnötig fein.
`(fileStem, profile.Id)` ist meistens auch korrekt, aber nur wenn sicher ist, dass `profile.Id` eindeutig die Tokenisierung repräsentiert.

Das tut es im aktuellen Modell **indirekt**, weil `TokenizationConfig` am Profil hängt. Solange du pro Profil genau eine Tokenization hast, ist `profile.Id` als Cache-Komponente technisch ausreichend.

Der gefährliche Fall ist nicht "gleicher Stem mit unterschiedlichen Tokens im selben Profil", sondern:

* gleicher Dateiname
* zwei verschiedene Profile
* unterschiedliche `TokenizationConfig`

Mit `(fileName, profile.Id)` bist du dann sicher.
Mit nur `fileStem` ohne Profilbezug wärst du falsch.

Mein Urteil:

* **einfachster sichere Key: `(fileName, profile.Id)`**
* `(fileStem, profile.Id)` geht auch, weil `FileNameParser` ohnehin die Extension ignoriert
* ein Hash über `TokenizationConfig` ist nur dann nötig, wenn du profilübergreifend cachen willst

Da dein Cache **lokal pro `Recognize(...)`-Aufruf** lebt, würde ich maximal simpel bleiben:

```csharp
var cache = new Dictionary<(string FileName, string ProfileId), RecognitionContext>();
```

Das ist lesbarer und narrensicherer als ein zusammengesetzter String-Key.

### Rückfrage 4 — Testprojekt-Struktur

Dein Vorschlag ist gut. Ich würde **genau dieses eine Testprojekt** nehmen:

```text
tests/
  BauProjektManager.PlanManager.Tests/
```

Kein zentrales `BauProjektManager.Tests` als Sammelprojekt.

Warum:

* klare Modulgrenze
* schnell verständlich
* Testnamen bleiben fachlich fokussiert
* spätere weitere Testprojekte pro Modul sind problemlos möglich
* vermeidet einen "Test-Sammelcontainer"

Also ja zu:

* `DocumentTypeRecognizerTests.cs`
* `RecognitionRuleValidationTests.cs`
* `ProfileWizardViewModelTests.cs`
* `ProfileManagerLoadTests.cs`

Ich würde nur noch ergänzen:

* `FileNameParserTests.cs`

Gerade weil Delimiter-Drift dein kritischster Punkt ist, sollte der Parser selbst separat abgesichert sein. Sonst testest du Drift nur indirekt.

## 3. Hinweise zur finalen Code-Skizze von `MatchesSegment` + `BuildRecognitionContext`

Ja, da fallen mir noch drei Dinge auf.

### a) `MatchesSegment` sollte Regelvalidierung nicht stillschweigend doppeln

Wenn `IsValid()` eingeführt wird, sollte `MatchesSegment` nicht zu einer zweiten, konkurrierenden Validierungsquelle werden. Es darf defensiv bleiben, aber nicht die einzige Sicherheitslinie sein.

Gut wäre:

```csharp
private static bool MatchesRule(RecognitionContext ctx, RecognitionRule rule)
{
    if (!rule.IsValid(out var reason))
    {
        Log.Warning("Ungueltige RecognitionRule ({Method}, {Pattern}): {Reason}",
            rule.Method, rule.Pattern, reason);
        return false;
    }

    return rule.Method.ToLowerInvariant() switch
    {
        "segment" => MatchesSegment(ctx, rule),
        "regex"   => MatchesRegex(ctx.FileName, rule.Pattern),
        _ => false
    };
}
```

Dann ist `MatchesSegment` schlank:

```csharp
private static bool MatchesSegment(RecognitionContext ctx, RecognitionRule rule)
{
    var pos = rule.SegmentPosition!.Value;
    return pos >= 0
        && pos < ctx.Tokens.Count
        && string.Equals(ctx.Tokens[pos], rule.Pattern,
            StringComparison.OrdinalIgnoreCase);
}
```

### b) `BuildRecognitionContext` sollte Tokens als `IReadOnlyList<string>` materialisieren

Nicht lazy lassen. Sonst riskierst du mehrfaches Enumerieren und unübersichtliches Verhalten.

Also konkret `.ToList()` oder Array.

### c) `MatchesProfile` sollte leere/ungültige Profile hart verwerfen

Nicht nur "keine Rules => false", sondern auch: wenn **eine** Regel ungültig ist, matcht das Profil nicht. Das passt zu eurem Grundsatz "lieber unknown als falsch zuordnen".

Skizze:

```csharp
private static bool MatchesProfile(RecognitionContext ctx, RecognitionProfile profile)
{
    if (profile.Recognition.Count == 0)
        return false;

    foreach (var rule in profile.Recognition)
    {
        if (!MatchesRule(ctx, rule))
            return false;
    }

    return true;
}
```

Das ist einfach und fachlich korrekt.

### d) kleiner Zusatz zu Regex

Wenn `regex` bleibt, sollte klar sein, **worauf** es läuft:

* auf `ctx.FileName`
* oder auf `ctx.FileStem`

Aktuell läuft Regex im Branch `feature/planmanager-v1` auf dem vollständigen Dateinamen in `DocumentTypeRecognizer`. Ich würde das bewusst so lassen oder bewusst ändern — aber nicht implizit.
Mein Bauchgefühl: **Regex auf `FileName` beibehalten**, weil es die bisherige Semantik fortsetzt und Spezialfälle am wenigsten überrascht.

## Gesamturteil

Das Konzept ist in Runde 2 klarer geworden. Der entscheidende Fortschritt ist nicht nur `segment`, sondern die **Vereinheitlichung von Lern- und Laufzeitpfad über denselben Parser**. Genau das macht die Methode tragfähig.

✅ **Einigkeit**

* `segment` + `SegmentPosition` + `SchemaVersion = 3`
* `RecognitionContext`
* `prefix/contains` jetzt komplett entfernen
* lokaler Cache pro `Recognize(...)`
* ein eigenes Testprojekt `BauProjektManager.PlanManager.Tests`
* direkter Aufruf von `FileNameParser`, kein zusätzlicher Tokenizer-Service

⚠️ **Widerspruch**

* Cache-Key nicht nur aus `fileStem`, sondern sicherheitshalber mit Profilbezug
* Warnung bei variablen Segmenten nur Hinweis, kein Blocking
* `MatchesSegment` sollte auf vorgelagerte `IsValid()`-Prüfung aufbauen statt Validierung doppelt zu verteilen

❓ **Rückfragen**

* Soll `regex` weiterhin auf dem kompletten `FileName` laufen oder bewusst auf `FileStem` umgestellt werden?
* Wird `ProfileManager.Load(...)` ungültige Regeln nur ignorieren oder das gesamte Profil verwerfen, wenn eine Regel invalide ist? Ich würde aktuell zum Verwerfen des ganzen Profils tendieren, weil ein halbgültiges Profil schwerer zu debuggen ist.
