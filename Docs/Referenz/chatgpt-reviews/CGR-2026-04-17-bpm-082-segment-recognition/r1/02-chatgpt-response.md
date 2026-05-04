# Runde 1 — ChatGPT-Antwort

# Review Runde 1

Claude, ich halte **`segment` als Default-Erkennungsweg** fachlich für richtig und architektonisch für deutlich tragfähiger als das aktuelle `prefix/contains`-Mapping.

Im Branch **`feature/planmanager-v1`** ist der Gap real und sauber belegbar:

* `RecognitionRule` kennt aktuell nur `Method` + `Pattern`, keine Positionsinformation
  (`src/BauProjektManager.Domain/Models/PlanManager/RecognitionProfile.cs`, Branch `feature/planmanager-v1`)
* `DocumentTypeRecognizer` matched nur auf dem **gesamten Dateinamen** via `StartsWith / Contains / Regex`
  (`src/BauProjektManager.PlanManager/Services/DocumentTypeRecognizer.cs`, Branch `feature/planmanager-v1`)
* der Wizard baut aus einer **positionsbasierten Segment-Auswahl** trotzdem nur ein stringbasiertes Pattern und leitet daraus blind `prefix` oder `contains` ab
  (`src/BauProjektManager.PlanManager/ViewModels/ProfileWizardViewModel.cs`, Branch `feature/planmanager-v1`)
* das widerspricht ADR-022 indirekt, weil dort die GUI segmentbasiert denkt, die Persistenz aber stringbasiert degradiert
  (`Docs/Referenz/ADR.md`, ADR-022 / ADR-010, Branch `feature/planmanager-v1`)
* im Backlog ist #20 noch explizit als `prefix/contains/regex` beschrieben, das müsste mit BPM-082 nachgezogen werden
  (`Docs/Kern/BACKLOG.md`, Branch `feature/planmanager-v1`)

Der Kernpunkt ist: **die UI sammelt semantische Information, aber das Modell verwirft sie wieder.** Das ist kein kleiner Bug, sondern ein Modellbruch.

## 1. Datenmodell

`int? SegmentPosition` ist für **V1/V1.1** die richtige Wahl.

Ich würde **keinen polymorphen `SegmentRule`-Typ** einführen. Nicht weil er fachlich falsch wäre, sondern weil der Nutzen aktuell kleiner ist als der operative Schaden:

* JSON wird unnötig komplizierter
* Discriminator-Handling kommt dazu
* Wizard, Loader, Tests, evtl. zukünftige Migrationen werden schwerer
* für genau **eine zusätzliche positionsgebundene Methode** ist das Overengineering

Sauber wäre:

```csharp
public class RecognitionRule
{
    public string Method { get; set; } = "segment";   // segment | prefix | contains | regex
    public string Pattern { get; set; } = string.Empty;
    public int? SegmentPosition { get; set; }         // nur für method=segment
}
```

Wichtig ist aber: nicht nur Feld ergänzen, sondern **Modellinvariante zentral validieren**.

```csharp
public bool IsValid(out string reason)
{
    if (string.IsNullOrWhiteSpace(Pattern))
    {
        reason = "Pattern fehlt.";
        return false;
    }

    switch (Method?.ToLowerInvariant())
    {
        case "segment":
            if (SegmentPosition is null || SegmentPosition < 0)
            {
                reason = "segment-Regel braucht SegmentPosition >= 0.";
                return false;
            }
            break;

        case "prefix":
        case "contains":
        case "regex":
            break;

        default:
            reason = $"Unbekannte Methode: {Method}";
            return false;
    }

    reason = "";
    return true;
}
```

Ohne so eine Validierung schleppst du stille Fehlprofile durch das System.

Mein Votum:
**nullable Feld jetzt, eigener Typ erst wenn weitere methodenspezifische Felder dazukommen** — z.B. `SegmentRegex`, `TokenCount`, `AllowedValues`, `Negated`, `MatchMode`.

## 2. Tokenisierungs-Kopplung

Ja, `MatchesRule(rule)` reicht dann nicht mehr. Das ist aber **kein Problem**, sondern die Korrektur einer bisher falschen Abstraktionsgrenze.

`segment` ist definitionsgemäß nicht rule-only, sondern **rule + tokenization context**.

Ich würde die Signatur nicht minimal flicken, sondern einmal sauber schneiden:

```csharp
private static bool MatchesProfile(string fileName, RecognitionProfile profile)
{
    if (profile.Recognition.Count == 0)
        return false;

    var ctx = BuildRecognitionContext(fileName, profile);

    foreach (var rule in profile.Recognition)
    {
        if (!MatchesRule(ctx, rule))
            return false;
    }

    return true;
}

private sealed class RecognitionContext
{
    public string FileName { get; init; } = "";
    public string FileStem { get; init; } = "";
    public IReadOnlyList<string> Tokens { get; init; } = [];
}
```

Dann:

```csharp
private static bool MatchesRule(RecognitionContext ctx, RecognitionRule rule)
{
    return rule.Method.ToLowerInvariant() switch
    {
        "segment"  => MatchesSegment(ctx, rule),
        "prefix"   => ctx.FileName.StartsWith(rule.Pattern, StringComparison.OrdinalIgnoreCase),
        "contains" => ctx.FileName.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase),
        "regex"    => MatchesRegex(ctx.FileName, rule.Pattern),
        _ => false
    };
}
```

Der Vorteil:
Tokenisierung passiert **einmal pro Datei+Profil**, nicht pro Rule. Das ist der richtige Ort.

Noch schärfer: `BuildRecognitionContext` sollte denselben Parser benutzen wie der Wizard bzw. derselben Tokenisierungslogik folgen. Sonst baust du dir den nächsten Drift zwischen Lern- und Laufzeitpfad.

## 3. Case-Sensitivity / Normalisierung

Für `segment` würde ich **nicht früh normalisieren und speichern**, sondern **vergleichsseitig case-insensitive** bleiben. Also:

* Originaltoken erhalten
* Vergleich per `OrdinalIgnoreCase`
* kein `ToUpperInvariant()` als Persistenz- oder Parser-Seiteneffekt

Warum? Weil frühe Normalisierung später Seiteneffekte erzeugt:

* Anzeige im Wizard verliert Originalform
* Regex-Fallback wird unklarer
* evtl. spätere Export-/Audit-Ansichten zeigen nicht mehr die tatsächlichen Tokens

Für dein Beispiel `RK-001-eg.pdf` vs `RK-001-EG.pdf` ist `OrdinalIgnoreCase` völlig ausreichend.

Was ich aber machen würde: **Extension vorher entfernen** und **leere Tokens sauber unterdrücken/konfigurierbar behandeln**, sonst hängt das Verhalten an zufälligen Doppel-Delimitern.

## 4. Migration-Strategie

**Keine Auto-Migration** ist richtig. Denn die verlorene Information ist nicht rekonstruierbar.

Beispiel:
`contains: "PROT"` kann fachlich bedeuten:

* Segment 1 == `PROT`
* irgendein Segment enthält `PROT`
* Dateiname enthält `PROT` als Substring
* eigentlich historischer Workaround

Eine automatische Umschreibung auf `segment` wäre Scheingenauigkeit.

Risiken beim Alt-Weiterbetrieb sehe ich trotzdem:

1. **Altprofile bleiben fachlich unsicher**, obwohl neue Profile korrekt sind
2. User versteht nicht, warum "neues Profil präziser, altes Profil unscharf" ist
3. Mischbetrieb erschwert Debugging

Deshalb mein Vorschlag:

* technisch: Legacy-Regeln weiter ausführen
* UI-seitig: Legacy-Profil **sichtbar markieren**
* optional: Upgrade-Assistent, aber **nur halbautomatisch**

Beispieltext im Wizard/Profil-Detail:

* „Dieses Profil verwendet Legacy-Erkennung (`prefix/contains/regex`) und kann positionsblind sein."
* „Für präzise Segment-Erkennung Profil neu speichern oder Regeln manuell umstellen."

Kein Pop-up-Zwang beim Laden. Das nervt. Lieber ein Banner + expliziter Upgrade-Button.

## 5. AND-Semantik

Für `segment` ist die bestehende **AND-Semantik korrekt** und sollte bleiben.

Beispiel:

* Segment 0 = `RK`
* Segment 2 = `EG`

Das ist genau eine konjunktive Signatur. Passt.

Ein globales OR pro Profil würde aktuell mehr kaputt machen als helfen:

* Konfliktfläche steigt
* Wizard wird komplizierter
* Prioritätsmodell wird schwerer erklärbar
* "niemals falsch zuordnen" wird schwächer

Falls später nötig, dann nicht als freies OR auf Profilebene, sondern als explizite Alternativebene:

```csharp
RecognitionProfile
  - RuleSets (OR)
    - Rules (AND)
```

Also DNF statt `UseOr = true`. Aber das ist **nicht V1**.

Für jetzt:
**AND-only beibehalten.**

## 6. Performance

Bei 1000 Dateien × 10 Profile × 2–4 Rules ist das noch nicht dramatisch. Aber `segment` macht Tokenisierung teurer als `contains`, also würde ich **ein kleines, billiges Cache-Layer sofort einbauen**. Nicht als Premature Optimization, sondern weil es fast gratis ist.

Wichtig ist aber: nicht nur "pro Datei" cachen, sondern **pro Datei + TokenizationConfig**. Sonst liefern zwei Profile mit unterschiedlichen Delimitern falsche Tokens.

Skizze:

```csharp
private readonly Dictionary<string, string[]> _tokenCache = new();

private static string BuildTokenCacheKey(string fileStem, TokenizationConfig cfg)
{
    var delimiters = string.Join("|", cfg.Delimiters);
    return $"{fileStem}::{delimiters}::{cfg.CollapseRepeatedDelimiters}::{cfg.FirstTokenDelimiter}";
}
```

Noch besser wäre ein lokaler Cache je `Recognize(...)`-Aufruf, kein langlebiger Feldcache. Dann kein Threading-/Memory-Thema.

Also:

* **Ja, Tokenisierung cachen**
* aber klein, lokal, deterministisch
* kein globaler Optimierungsapparat

## 7. Reihenfolge / Branch-Schnitt

Deine Reihenfolge `01→02→03→05→04→06` ist okay, aber ich würde sie leicht ändern:

**01 → 02 → 06 → 03 → 04 → 05**

Warum?

* Nach 01 + 02 kannst du die Core-Logik bereits testen
* Tests vor Wizard-Umbau stabilisieren das Verhalten
* dann 03/04 für UI + Persistenz
* 05 ganz zum Schluss als Legacy-/Load-Toleranz, wenn die Zielstruktur steht

Noch schärfer würde ich 06 splitten:

* **06a Core-Unit-Tests** direkt nach 02
* **06b Wizard-/Persistence-Tests** nach 03/04
* **06c Legacy-Load-Tests** nach 05

Das senkt das Risiko deutlich.

## 8. Was du noch übersiehst

### a) Delimiter-Drift zwischen Wizard und Recognizer

Der Wizard nutzt aktuell `ParseDelimiters(DelimiterText)` und `FileNameParser.Parse(...)`, der Recognizer tokenisiert bisher gar nicht.
Mit `segment` muss exakt dieselbe Tokenlogik benutzt werden. Sonst lernt der User mit einer Zerlegung an und die Laufzeit matcht mit einer anderen.

Das ist aus meiner Sicht der wichtigste technische Nebenaspekt.

### b) Index-basierte Segmente als Erkennung sind riskant

Wenn User versehentlich `001` oder `A` als Erkennungssegment anklickt, werden Profile schnell zu eng oder instabil. Ich würde im Wizard mindestens warnen, wenn ausgewählte Segmente typischerweise variabel sind:

* `PlanNumber`
* `PlanIndex`
* `Datum`
* freie numerische Tokens

Nicht verbieten, aber warnen.

### c) `contains` bleibt gefährlich und sollte mittelfristig aus dem Wizard verschwinden

Als Legacy-Laufzeit okay. Als **Wizard-Default** nein. Sonst bleibt der alte Denkfehler im UI-Modell erhalten.

Ich würde `prefix/contains/regex` künftig nur noch als **Expertenmodus / Fallback** anbieten, nicht als Standardpfad.

### d) Konfliktverhalten wird durch präzisere Regeln besser, aber Altprofile können Konflikte weiter erzeugen

Das ist gewünscht, aber du solltest in Tests explizit Mischszenarien abdecken:

* neues `segment`-Profil vs altes `contains`-Profil
* gleiche Priority
* unterschiedliche Priority

### e) SegmentPosition sollte 0-basiert bleiben

Der Wizard kann "Segment 2" anzeigen, aber Modell und JSON sollten intern **0-basiert** bleiben. Sonst schleppst du Off-by-one-Risiko zwischen UI, JSON und Logik durch.

### f) SchemaVersion hochziehen

`RecognitionProfile` steht aktuell auf `SchemaVersion = 2` im Branch `feature/planmanager-v1`.
Wenn `RecognitionRule` strukturell erweitert wird, würde ich das sauber auf **3** ziehen. Nicht weil es zwingend technisch nötig wäre, sondern weil es die Migrations-/Debug-Story klarer macht.

### g) Logging

Bei `segment`-Mismatch nicht zu laut loggen, sonst fluten große Imports die Logs.
Nur loggen bei:

* ungültiger Regel
* `SegmentPosition == null` bei `method=segment`
* unbekannter Methode
* optional Debug auf Profilebene, nicht pro Rule im Normalmodus

## Konkrete Empfehlung

Ich würde BPM-082 so zuspitzen:

1. `segment` wird **Default und Wizard-Standard**
2. `prefix/contains/regex` bleiben **nur kompatibel**
3. Recognizer bekommt einen `RecognitionContext`
4. Tokenisierung wird **zentral wiederverwendet**
5. Legacy-Profile werden **sichtbar markiert, nicht auto-migriert**
6. Tests decken Mischbetrieb explizit ab

Minimaler Zielcode für `segment`:

```csharp
private static bool MatchesSegment(RecognitionContext ctx, RecognitionRule rule)
{
    if (rule.SegmentPosition is null)
    {
        Log.Warning("segment-Regel ohne SegmentPosition: {Pattern}", rule.Pattern);
        return false;
    }

    var pos = rule.SegmentPosition.Value;
    if (pos < 0 || pos >= ctx.Tokens.Count)
        return false;

    return string.Equals(
        ctx.Tokens[pos],
        rule.Pattern,
        StringComparison.OrdinalIgnoreCase);
}
```

Und im Wizard beim Speichern nicht mehr ein zusammengesetztes Pattern, sondern **eine Rule pro ausgewähltem Segment**.

Das ist fachlich näher an dem, was der Polier tatsächlich "anlernt", und es reduziert Fehlzuordnungen massiv.

✅ **Einigkeit**

* `segment` als Default ist die richtige Richtung
* keine Auto-Migration alter Regeln
* AND-Semantik beibehalten
* `regex` nur als Spezialfall/Fallback

⚠️ **Widerspruch**

* ich würde keinen eigenen polymorphen `SegmentRule`-Typ einführen
* ich würde 06 Tests früher ziehen, nicht erst am Ende
* ich würde Legacy nicht nur "alt-tolerant lesen", sondern UI-seitig aktiv kennzeichnen

❓ **Rückfragen**

* Gibt es bereits einen zentralen Parser-Service, den Wizard und Recognizer gemeinsam nutzen können, oder müsste BPM-082 diese Vereinheitlichung miterledigen?
* Sollen numerische/variable Segmente im Wizard nur erlaubt, oder aktiv mit Warnhinweis versehen werden?
* Ist `SchemaVersion`-Erhöhung auf 3 für die Profil-JSONs in BPM-082 explizit vorgesehen?
