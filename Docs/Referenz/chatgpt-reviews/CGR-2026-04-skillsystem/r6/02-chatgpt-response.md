# Review Runde 6

Claude, Herberts Entscheidung **B+C** für `cc-steuerung` ist vertretbar, aber nur als kontrolliertes Experiment. Ich würde sie **nicht** jetzt revidieren. Die harte Bedingung ist: Die Smoke-Tests müssen explizit prüfen, ob "cc + Fachintent" im realen Claude.ai-Verhalten funktioniert. Wenn nicht, muss später doch Variante A kommen: kurzer Modalitätsblock in den Fachskills.

Ich habe `README.md` auf `main` erneut gelesen. Der aktuelle README ist nicht rein Onboarding; er enthält operative Regeln zu Two-Place-Pflege, Eval-Methodik, Projekt-Fallback, Memory-Lifecycle, Branch-/ask_user_input-/Commit-Konventionen und Frühphasen-Prinzip. Das ist für die gewählte Source-of-Truth-Strategie zu normativ. 

## 1. cc-steuerung Multi-Trigger objektiv testen

### Einschätzung

Objektiv beweisen lässt sich Multi-Skill-Laden nur, wenn Claude.ai die geladenen Skills sichtbar macht oder Logs/Debug-Infos ausgibt. Ohne solchen Indikator bleibt nur **Verhaltens-Evidenz**. Das ist schwächer, aber ausreichend für die Praxis, wenn die Tests so formuliert sind, dass beide Skill-Anteile eindeutig beobachtbar sind.

Ich habe keine belastbare, aktuelle Aussage aus Anthropic-Doku oder `mgechev/skills-best-practices`, weil ich hier nicht browsen kann und im Repo keine `mgechev`-Referenz gefunden habe. Also: Keine Behauptung über interne Tie-Breaker wie alphabetisch, Registrierungsreihenfolge oder Score. Ich würde davon ausgehen: **nicht spezifiziert, nicht darauf bauen**.

### Beobachtungs-Methode

Primär:

```text
Test bestanden, wenn die Antwort beide Signale enthält:
1. cc-steuerung-Signal: direkte Ausführung per DC/PC, Pfad-/Tool-/Terminal-Logik, keine reine Chat-Antwort.
2. Fachskill-Signal: fachliche Pflichtregeln des Zielskills werden eingehalten.
```

Beispiele:

* `cc + code-erstellen`: muss DC-Ausführung planen/nutzen **und** code-erstellen-Pflichten beachten: Branch-Ermittlung, Quickloads/INDEX, Frühphasen-Prinzip, Impact-Check.
* `dc + git-commit-helper`: muss DC/Git-Kontext nutzen **und** Commit-Format/SemVer/Version-Bump-Regel anwenden.
* `Claude Code + tracker`: muss PC/Tool-Modalität erkennen **und** Tracker-Protokoll samt Quittungs-/Anker-Regel einhalten.

Sekundär: Falls Claude.ai Skill-Ladechips oder "loaded skill" Hinweise zeigt, manuell notieren. Aber das darf nicht die einzige Messung sein, weil UI-Indikatoren nicht zwingend stabil sind.

### Test-Cases

Deine Q1–Q5 sind fast richtig. Ich würde Q5 präzisieren, weil "lies INDEX.md" ohne cc/dc je nach Umgebung trotzdem Datei-Lesen implizieren kann. Als Negation sollte sie nicht "nur Fachskill oder Antwort im Chat" heißen, sondern:

```text
Q5: "Was steht in INDEX.md zur Konfliktpaar-Delegation?"
Erwartung: KEIN cc-steuerung, sofern kein expliziter cc/dc/PC-Ausführungswunsch.
```

Damit testest du sauber: Datei-/Doc-Bezug allein darf `cc-steuerung` nicht triggern.

Ich würde ergänzen:

```text
Q6: "cc mach ein HTML-Mockup für den ImportPreviewDialog"
Erwartung: cc-steuerung + mockup-erstellen

Q7: "dc prüfe read-only, ob INDEX und Skill-Descriptions konsistent sind"
Erwartung: cc-steuerung + audit

Q8: "Claude Code soll die README entnormativieren"
Erwartung: cc-steuerung + doc-pflege

Q9: "cc"
Erwartung: cc-steuerung, aber ask_user_input_v0/kurze Klärung, weil WAS fehlt

Q10: "ProfileWizard View bauen"
Erwartung: code-erstellen, KEIN cc-steuerung
```

Q9 ist wichtig, weil `cc` allein nur Modalität ist. Der Skill darf dann nicht fachlich raten.

### Globale INDEX-Regel

Der Vorschlag ist inhaltlich richtig, aber zu absolut bei "der Router darf nicht". Der Router selbst ist Plattformverhalten; INDEX kann nur Zielverhalten definieren. Ich würde es so formulieren:

```markdown
### 9. Modalitäts-Skill `cc-steuerung` läuft parallel zu Fachskills

`cc-steuerung` ist kein konkurrierender Fachskill, sondern eine Ausführungs-Modalität für Desktop Commander / Claude Code / direkte PC-Operationen.

Trigger für die Modalität:
- `cc`
- `dc`
- `Claude Code`
- `direkt auf den PC`
- explizite Datei-/Terminal-/Build-/Git-Ausführung auf Herberts PC

Entscheidungsregel:
- Das **WAS** bleibt beim Fachskill: `code-erstellen`, `tracker`, `git-commit-helper`, `doc-pflege`, `audit`, `mockup-erstellen`, etc.
- Das **WIE** kommt zusätzlich von `cc-steuerung`.
- Bei `cc`/`dc` ohne fachliches WAS fragt Claude per `ask_user_input_v0` nach der Zielaktion.
- Datei-/Repo-Nennung allein triggert `cc-steuerung` nicht. Es braucht explizite PC-/DC-/Ausführungsabsicht.

Smoke-Test-Pflicht:
- Multi-Intent-Fälle `cc + code`, `cc + commit`, `cc + tracker`, `cc + audit`, `cc + mockup` werden in `evals/smoke-all-skills.md` geprüft.
- Wenn die reale Claude.ai-Ausführung nur einen Skill sichtbar berücksichtigt, wird die Modalitätsregel zusätzlich in den betroffenen Fachskills ergänzt.
```

Das ist die richtige Granularität: genug operativ, aber noch INDEX-tauglich.

## 2. README-Refactor konkretisieren

### Einschätzung

README soll nach Herberts Entscheidung nur Onboarding sein. Also: Beschreibungen bleiben, normative Muss-/Nie-/Workflow-Regeln raus oder auf Verweis reduzieren.

### Kapitelweise Vorschlag

```text
Top-Intro "Single Source of Truth..." 
→ ÄNDERN.
Problem: README nennt sich selbst/Repo als SSoT für Regeln. 
Neu: "Dieses Repo enthält die Skill-Definitionen. Verbindliche Routing- und Invarianten-Regeln stehen in INDEX.md."

"Zweck dieses Repos"
→ bleibt überwiegend (Onboarding), aber Commit-Format aus der Versionshistorie-Zeile entfernen oder als Verweis auf INDEX/CHANGELOG formulieren.

"Verzeichnisstruktur"
→ bleibt (Onboarding).

Kapitel 1 "Die 11 Skills im Überblick"
→ bleibt als Kurzüberblick, aber operative Details kürzen.
Entfernen/reduzieren:
- "Führt Impact-Check und Blocking-Conditions durch"
- genaue CGR-Archivierungsdateien
- genaue Tracker-Kommandos/Quittungsregeln
- DC Default-Ausführungsmodus
Bleibt:
- Skill-Gruppen, Zweck je Skill, grobe Trigger-Beispiele.
Verweis: "Verbindliche Trigger: INDEX.md; operative Regeln: jeweilige SKILL.md."

"Trigger-Disziplin — das Kernprinzip"
→ ENTFERNEN oder stark reduzieren.
Normativ. Gehört in INDEX.md und Skill-Frontmatter-Schema-Doku.
README nur: "Descriptions folgen einem einheitlichen Schema; siehe INDEX.md und Skill-Dateien."

Kapitel 2 "`skills/` — Die Skill-Dateien"
→ Struktur bleibt.
"Aufbau einer SKILL.md" bleibt deskriptiv.
"Body enthält typischerweise..." bleibt, solange nicht als Pflichtliste formuliert.
"Progressive Disclosure am Beispiel tracker" bleibt als Architekturbeispiel, aber konkrete Tracker-Regeln/Reference-Inhalte nur auflisten, keine Muss-Regeln.
"Sync-Mechanismus: Two-Place-Pflege"
→ VERWEIS-ONLY. Operativ nach skill-pflege/SKILL.md + INDEX.md.

Kapitel 3 "`evals/`"
→ bleibt überwiegend.
Normative Scoring-Regeln dürfen bleiben, wenn README nur erklärt, wie vorhandene Eval-Dateien aufgebaut sind. Aber "objektives Qualitätsmaß" abschwächen.
Run-Log-Format kann bleiben als Onboarding, eigentliche Eval-Regeln besser in evals/README.md.

Kapitel 4 "`projects/<projekt>/`"
→ bleibt als Architektur-Onboarding.
Normative Entscheidungsregel "was gehört wohin" auf docs/project-architecture.md verweisen.
Fallback-Verhalten auf INDEX.md verweisen, wenn es für Skills verbindlich ist.

Kapitel 5 "`docs/`"
→ bleibt.

Kapitel 6 "`reference/`"
→ bleibt.

Kapitel 7 "Top-Level-Dokumente"
→ bleibt, aber muss Source-of-Truth-Hierarchie explizit machen.
Neu:
- INDEX.md = verbindliche Routing/Invarianten
- skill bodies = operative Skill-spezifische Regeln
- MEMORY-RUBRIKEN.md = verbindliche Memory-Rubrik-Konvention
- README.md = Onboarding

Kapitel 8 "Refactor-Historie"
→ kürzen oder auf CHANGELOG/docs/skill-refactor-phases.md verweisen.
Aktuell zu detailreich und driftgefährdet. README sollte nicht Versionstabellen duplizieren.

Kapitel 9 "Wichtige Workflow-Konventionen"
→ ENTFERNEN, ersetzen durch Verweisblock auf INDEX.md.
Das ist der größte Normativitätsverstoß.
Betrifft ask_user_input_v0, Branch-Ermittlung, Two-Place, Commit-Format, Frühphase.

Weitere Kapitel nach dem truncation-Bereich, falls vorhanden:
→ Alles mit "MUSS", "NIE", "verbindlich", "Default", "Workflow-Reihenfolge" entweder entfernen oder auf INDEX.md / Skill-Body / MEMORY-RUBRIKEN.md verweisen.
```

### Verweis-Block-Pattern

Dein Pattern ist gut, aber ich würde es ohne Warn-Icon formulieren. Warnboxen überall machen README wieder "regelhaft". Einheitlich:

```markdown
> **Verbindliche Regelquelle:** [INDEX.md](./INDEX.md) definiert Routing und globale Invarianten.
> Dieses Kapitel ist nur Onboarding-Kontext und wiederholt keine operativen Regeln.
```

Für Skill-spezifische Regeln:

```markdown
> **Operative Regelquelle:** [`skills/<skill>/SKILL.md`](./skills/<skill>/SKILL.md).
> Dieser Abschnitt beschreibt nur Zweck und Einordnung.
```

Für Memory:

```markdown
> **Operative Regelquelle:** [`MEMORY-RUBRIKEN.md`](./MEMORY-RUBRIKEN.md).
> README enthält nur eine Kurzbeschreibung.
```

Für Evals:

```markdown
> **Operative Regelquelle:** [`evals/README.md`](./evals/README.md).
> README zeigt nur die Ordnerstruktur.
```

## 3. Smoke-Eval konkretisieren

### Einschätzung

3+2+2 ist als Baseline gut, aber nicht gleichmäßig für alle Skills. Die 4 bereits baselinierten Skills brauchen weniger Breite; die 7 unbaselinierten brauchen mehr. Konfliktträchtige Skills brauchen mehr Near-Misses als reine Workflow-Skills.

### Empfohlene Verteilung

```text
Bereits baseliniert:
- git-commit-helper: 2 should_trigger, 1 should_not_trigger, 3 conflict-near-misses
- chatgpt-review: 2 should_trigger, 1 should_not_trigger, 3 conflict-near-misses
- tracker: 2 should_trigger, 1 should_not_trigger, 3 conflict-near-misses
- code-erstellen: 3 should_trigger, 1 should_not_trigger, 4 conflict-near-misses

Unbaseliniert, konfliktarm/mittel:
- chat-wechsel: 3 should_trigger, 2 should_not_trigger, 2 conflict-near-misses
- skill-neu: 3 should_trigger, 2 should_not_trigger, 2 conflict-near-misses
- skill-pflege: 3 should_trigger, 2 should_not_trigger, 3 conflict-near-misses

Unbaseliniert, konfliktträchtig:
- audit: 3 should_trigger, 2 should_not_trigger, 4 conflict-near-misses
- mockup-erstellen: 3 should_trigger, 2 should_not_trigger, 4 conflict-near-misses
- doc-pflege: 3 should_trigger, 2 should_not_trigger, 4 conflict-near-misses
- cc-steuerung: 3 solo should_trigger, 2 should_not_trigger, 6 multi-trigger cases
```

Das ergibt grob 75–85 Queries. Das ist noch handhabbar.

### Systematische Near-Miss-Auswahl

Nicht willkürlich. Reihenfolge:

1. **Konfliktpaar-Matrix**: Für jedes Paar mindestens je eine Query pro Richtung.
2. **Description-Negativgrenzen**: Jede "Do not trigger for …"-Phrase bekommt mindestens einen Test.
3. **Real beobachtete Fehlroutings**: Golden Cases bevorzugen.
4. **BPM-Feature-Wortschatz**: ProfileWizard, ImportPreviewDialog, PlanManager, Segment-Erkennung, View, Dialog, Commit, Tracker.

Beispiel für `mockup-erstellen`:

```text
should_trigger:
- "Mockup für ImportPreviewDialog"
- "HTML-Entwurf für ProfileWizard"
- "Screen-Design für Segment-Erkennung"

conflict-near-miss:
- "neue XAML View für ProfileWizard" → code-erstellen
- "kleiner UI-Fix im bestehenden Dialog" → code-erstellen
- "HTML-Mockup danach bitte implementieren" → code-erstellen oder ask_user_input_v0, je nach Hauptabsicht
- "Architektur für ImportPreviewDialog" → nicht mockup, eher code/doc je nach Auftrag
```

### Ergebnis-Speicherung

Ich empfehle **beides**, aber mit klarer Trennung:

```text
evals/smoke-all-skills.md
→ stabiler Testkatalog + erwartete Ergebnisse + Scoring-Regeln.

evals/runs/smoke-YYYY-MM-DD.md
→ einzelne Run-Snapshots mit Modell, Modus, Ergebnis, Abweichungen.
```

Nicht jeden Run direkt in `smoke-all-skills.md` schreiben, sonst wird die Datei schnell unlesbar. Im Katalog nur eine kleine "Latest Run Summary"-Tabelle.

Struktur:

```markdown
# Smoke Eval — All Skills

## Scope
- 11 Skills
- Blind-Modus: Name + Description
- Optional Vollmodus: Name + Description + Body

## Scoring
- PASS: erwartete Skillmenge exakt oder akzeptierte Teilmenge
- FAIL: falscher Primärskill oder unerlaubter Skill
- WARN: fachlich korrekt, aber Modalität unklar sichtbar

## Cases

### cc-steuerung
| ID | Query | Erwartung | Kategorie | Notiz |
|---|---|---|---|---|
| CC-01 | cc lies INDEX.md | cc-steuerung | should_trigger | solo |
| CC-02 | cc bau die neue View für ProfileWizard | cc-steuerung + code-erstellen | multi-trigger | WPF/XAML |
```

Run-Snapshot:

```markdown
# Smoke Run — 2026-04-25

- model:
- tester:
- mode: real Claude.ai / self-sim / API
- repo ref: main @ <sha>

## Summary
| Skill | PASS | WARN | FAIL |
|---|---:|---:|---:|

## Failures
| Case | Expected | Actual | Diagnosis | Action |
```

## 4. Versteckte Fragilitäten — Risiko-Priorisierung

### Kritischste zwei in Woche 1

#### 1. cc-steuerung-Pfad-/Modalitätsfragilität

Warum kritisch: Feature-Arbeit wird konkret Dateien lesen/schreiben/builden. Wenn `cc-steuerung` falsch triggert oder Pfade falsch resolved, entstehen falsche Edits, falsche Buildannahmen oder Chat-Antworten statt echter Repo-Operationen. Das blockiert direkt.

Frühwarn-Indikatoren:

* Claude antwortet bei "cc/dc" mit Codeblöcken statt echter Dateioperation.
* Claude nennt hartkodierte Pfade.
* Claude nutzt `$`-PowerShell-Variablen in Command-Strings.
* Claude fragt nicht nach Branch/PC-Kontext, obwohl nötig.
* Claude behandelt "cc bau X" nur als `cc-steuerung` und vergisst `code-erstellen`-Pflichten.
* Build-/Git-Ausgaben werden paraphrasiert statt aus Tool-/Terminal-Ergebnis abgeleitet.

Sofortmaßnahme bei Auftreten:

```text
Stop Feature-Task.
Case in smoke-all-skills.md aufnehmen.
INDEX-Regel prüfen.
Wenn zweimal reproduzierbar: Variante A nachziehen, aber nur für betroffene Fachskills.
```

#### 2. Tracker-Anker / Task-Scope-Disziplin

Warum kritisch: PlanManager/ProfileWizard/ImportPreview sind mehrteilige Feature-Stränge. Wenn Anker, Taskstatus, Commit-Hash und Scope-Übergänge auseinanderlaufen, verliert Herbert die Rückverfolgbarkeit. Das ist kein kleines Doku-Problem, sondern Projektsteuerungs-Schaden.

Frühwarn-Indikatoren:

* Antworten mit Taskbezug enthalten keinen `[BPM-ANCHOR-...]`.
* `tracker done` ohne Hash-/Nachlauf-/Folgeoptionen.
* Mehrere Task-Änderungen ohne Pro-Task-Quittung im selben Block.
* Ein Commit deckt mehrere ClickUp-Tasks ab.
* Claude startet neuen Task, obwohl Parent/Siblings/offene Verweise nicht geprüft wurden.
* "Das machen wir später" landet nur im Chat, aber weder in Memory noch ClickUp.

Sofortmaßnahme:

```text
Task-Arbeit pausieren.
Aktuellen Chat-Anker und ClickUp-Status reconciliieren.
Fehlende Anker nachtragen.
Erst danach weiterentwickeln.
```

### Warum nicht Memory/Frühphase als Top 2?

Memory-Schatten-Backlog ist real, aber Schaden entsteht langsamer. Frühphasen-Prinzip ist fachlich wichtig, aber gut dokumentiert und bei frühen BPM-Features leicht manuell zu erkennen: Sobald Claude "Migration", "Backward Compatibility", "Legacy Parser" sagt, ist es sichtbar falsch.

Frühwarn-Indikatoren für Memory:

* Mehr als 5 offene `[VERIFY]`/`[INFRA-TODO]` nach einem Handover.
* Derselbe Memory-Punkt taucht in 3 Handovers wieder auf.
* Claude referenziert Memory-Punkte, die eigentlich erledigt sind.
* Offene Punkte sind unklar formuliert und nicht entscheidbar.

Frühwarn-Indikatoren für Frühphase:

* Vorschläge enthalten "Migration", "Legacy", "Backward Compatibility", "alte Config tolerieren".
* Parser akzeptieren mehrere alte Schemaformen.
* Code enthält Upgrade-/Migrationspfade ohne expliziten Auftrag.
* Doku beschreibt Datenübernahme statt "Datei löschen, neu anlegen lassen".

## 5. Fresh-Model-API-Eval — lohnt der Aufwand?

### Einschätzung

Vor BPM-Feature-Arbeit: **Nein, nicht als Blocker.**
Nach 1–2 Wochen realer Nutzung: **Ja, wenn Fehlrouting auftritt oder wenn das Skill-System weiter skaliert.**

Der Nutzen eines Fresh-Model-Runs ist methodisch sauber, aber er beantwortet nicht exakt dieselbe Frage wie Claude.ai-Routing. Ein API-Modell, dem du Skill-Descriptions in einen Prompt gibst, simuliert Routing. Es ist besser als Selbstsimulation, aber nicht identisch mit der echten Claude.ai Skill-Auswahl.

### Aufwand

Minimal:

* 2–4 Stunden für statischen Smoke-Katalog + manuelle Auswertung.
* 4–8 Stunden für kleines Script mit API-Aufruf + JSON-Auswertung.
* 1–2 Stunden für GitHub Actions, Secrets, Markdown-Report.
* Mehr Aufwand, wenn Vollmodus mit Body-Auszügen und Multi-Skill-Erwartungen sauber modelliert werden soll.

### Minimales aussagekräftiges Setup

Nur Blind-Modus, nur Descriptions, deterministischer Klassifikationsprompt:

```json
{
  "query": "cc bau die neue View für ProfileWizard",
  "available_skills": [
    {"name": "cc-steuerung", "description": "..."},
    {"name": "code-erstellen", "description": "..."}
  ],
  "instruction": "Return all skills that should trigger. Return JSON only."
}
```

Erwartete Ausgabe:

```json
{
  "skills": ["cc-steuerung", "code-erstellen"],
  "confidence": "high",
  "reason": "cc is execution modality; new View is code/WPF intent"
}
```

Scoring:

```python
expected = set(case["expected_skills"])
actual = set(model_output["skills"])

if actual == expected:
    result = "PASS"
elif expected.issubset(actual):
    result = "WARN_EXTRA"
elif actual.issubset(expected) and "cc-steuerung" in expected:
    result = "WARN_MISSING_MODALITY"
else:
    result = "FAIL"
```

### CI/Cron-Vorschlag

GitHub Actions:

```yaml
name: skill-smoke-eval

on:
  workflow_dispatch:
  schedule:
    - cron: "0 7 * * 1"

jobs:
  eval:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          ref: main

      - uses: actions/setup-python@v5
        with:
          python-version: "3.11"

      - run: pip install anthropic pyyaml

      - run: python scripts/run_smoke_eval.py
        env:
          ANTHROPIC_API_KEY: ${{ secrets.ANTHROPIC_API_KEY }}

      - uses: actions/upload-artifact@v4
        with:
          name: smoke-eval-report
          path: evals/runs/
```

Script-Skelett:

```python
import json
from pathlib import Path

def load_descriptions():
    skills = []
    for p in Path("skills").glob("*/SKILL.md"):
        text = p.read_text(encoding="utf-8")
        fm = text.split("---", 2)[1]
        name = extract_yaml_field(fm, "name")
        desc = extract_yaml_field(fm, "description")
        skills.append({"name": name, "description": desc})
    return skills

def classify(client, case, skills):
    prompt = {
        "query": case["query"],
        "available_skills": skills,
        "task": (
            "Return JSON only: {skills: string[], confidence: low|medium|high, reason: string}. "
            "Select all skills that should trigger. Multi-skill selection is allowed."
        ),
    }
    # call API here
    return parsed_json

def score(expected, actual):
    e, a = set(expected), set(actual)
    if a == e:
        return "PASS"
    if e.issubset(a):
        return "WARN_EXTRA"
    if a.issubset(e):
        return "WARN_MISSING"
    return "FAIL"
```

### Billigere 80%-Variante

Ich würde zuerst diese nehmen:

1. `evals/smoke-all-skills.md` schreiben.
2. In Claude.ai in einem frischen Chat manuell 30 kritischste Cases laufen lassen.
3. Herbert notiert:

   * Welche Skills sichtbar geladen wurden, falls UI sichtbar.
   * Ob Verhalten Fachpflichten erfüllt.
   * PASS/WARN/FAIL.
4. Nur FAILs als `[ARCH-OPEN]` oder ClickUp-Task erfassen.

Das kostet weniger als ein API-Harness und misst näher am echten System. API-Eval lohnt erst, wenn ihr Regressionen regelmäßig verhindern wollt.

## Schlussurteil

B+C bleibt richtig, aber nur mit Exit-Kriterium. Das Exit-Kriterium muss explizit sein:

```text
Wenn zwei oder mehr cc+Fachskill-Smoke-Cases in realer Claude.ai-Nutzung nur einen Skill faktisch berücksichtigen, wird Variante A umgesetzt: kurzer Modalitätsblock in den betroffenen Fachskills.
```

README-Refactor sollte vor Feature-Freigabe passieren, aber nicht als großer Rewrite. Entfernt zuerst Kapitel 9 und entschärft Two-Place/Memory/Eval-Regeln zu Verweisen. Die Smoke-Eval ist wichtiger als README-Kosmetik.

✅ **Einigkeit**

* B+C für `cc-steuerung` ist als Teststrategie tragfähig.
* README soll Onboarding sein; `INDEX.md` ist Routing/Invarianten-Source-of-Truth.
* Smoke-Eval gehört vor die Freigabe, aber als pragmatische Matrix, nicht als Forschungsprojekt.
* Fresh-Model-API-Eval ist methodisch besser als Selbstsimulation, aber kein Vorab-Blocker.

⚠️ **Widerspruch**

* Q5 "lies INDEX.md" ist als Negation zu unscharf. Besser eine inhaltliche Frage zu INDEX ohne Ausführungsmodalität.
* README kann nicht "wortgleich weitgehend bleiben"; mindestens Kapitel 9 und Two-Place-/Memory-/Fallback-Regelteile müssen reduziert werden.
* "Router darf nicht zwischen ihnen wählen" ist als INDEX-Regel zu stark formuliert. Besser Zielverhalten + Fallback/Exit-Kriterium definieren.

❓ **Rückfragen**

* Sieht Herbert in Claude.ai tatsächlich Skill-Ladeindikatoren pro Antwort, oder muss die Smoke-Eval rein verhaltensbasiert bewertet werden?
* Soll `evals/smoke-all-skills.md` auch Vollmodus-Fälle enthalten, oder zunächst strikt Blind/Description-only?
* Gibt es echte Fehlrouting-Golden-Cases aus den letzten Tagen, die in den Smoke-Test aufgenommen werden müssen?
