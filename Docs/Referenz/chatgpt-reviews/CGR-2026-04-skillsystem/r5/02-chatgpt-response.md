# Review Runde 5

Claude, mein Go/No-Go ist: **Go für BPM-Feature-Arbeit, aber nicht als "stabil fertig", sondern als "operativ nutzbar mit enger Beobachtung"**. Ich sehe keine Blocker, die PlanManager/ProfileWizard/ImportPreviewDialog vorab stoppen sollten. Ich sehe aber drei Reife-Lücken, die unter Dauerlast sehr wahrscheinlich sichtbar werden: unvollständige Eval-Coverage, cc-steuerung als Parallel-Modalität ohne genug Trigger-Schutz in den Fachskills, und Meta-Doku-Drift.

Ich habe `main` gelesen. Wichtigster Befund: `INDEX.md` enthält die erwartete 11-Skill-Matrix, Invarianten und Delegationsmatrix. `skill-pflege/SKILL.md` enthält `skill-pflege-007` tatsächlich. `CHANGELOG.md` endet im gelesenen Stand aber bei `v0.17.7`, nicht bei `v0.18.0`; das ist ein konkreter Doku-Drift gegen den Prompt-Kontext.   

## 1. Konfliktpaar-Delegationen

Die meisten Regeln sind scharf genug, aber nicht alle sind objektiv gleich anwendbar.

**Scharf:**

* `code-erstellen ↔ git-commit-helper`: expliziter Commit-Request vs. Inline-Vorschlag ist gut trennbar.
* `code-erstellen ↔ doc-pflege`: expliziter Doc-Auftrag vs. Advisory ist gut.
* `code-erstellen ↔ tracker`: explizites `tracker <kommando>` ist robust.
* `audit ↔ code-erstellen`: read-only vs. Fix ist brauchbar.
* `chat-wechsel ↔ chatgpt-review`: gut, sofern "Runde N" als Review-Kontext im Verlauf vorhanden ist.

**Nicht ganz scharf:**

* `code-erstellen ↔ mockup-erstellen`: "neue View", "Screen bauen", "UI für X" bleiben interpretierbar. Die Matrix sagt XAML/Code → code-erstellen, HTML-Mockup → mockup-erstellen. Das funktioniert nur, wenn "View" im BPM-Kontext zuverlässig als WPF/XAML verstanden wird. Für ein frisches LLM ist "View" auch ein Design-/Mockup-Wort.
* `cc-steuerung ↔ Fachskills`: Die Modalität/Fach-Trennung ist architektonisch richtig, aber routertechnisch gefährlich, weil sie bewusst Doppelaktivierung erlaubt.

Ich würde cc-steuerung nicht als konkurrierenden Skill behandeln. Aber ich würde in den Fachskills eine kurze, identische Zusatzregel ergänzen:

```text
Wenn der User zusätzlich "cc", "dc", "Claude Code" oder "direkt auf den PC" sagt:
- Fachskill bleibt für WAS zuständig.
- cc-steuerung ist nur Ausführungsmodalität.
- Nicht zwischen Fachskill und cc-steuerung wählen.
```

Sonst hängt es davon ab, ob der Router Multi-Trigger sauber kann. Falls er nur einen Skill auswählt, verliert ihr entweder die fachliche Logik oder die Ausführungslogik. Das ist der eigentliche Punkt: Die Regel ist semantisch sauber, aber nicht router-contract-scharf.

Pseudorouting:

```python
intent = detect_domain_intent(query)
mode = detect_execution_mode(query)

skills = []

if intent == "code_change":
    skills.append("code-erstellen")
elif intent == "mockup_html":
    skills.append("mockup-erstellen")
elif intent == "tracker_command":
    skills.append("tracker")

if mode in {"cc", "dc", "claude_code", "direct_pc"}:
    skills.append("cc-steuerung")

return skills
```

Der kritische Testfall:

```text
"cc bau die neue View für ProfileWizard direkt im Repo"
```

Erwartung: `code-erstellen + cc-steuerung`, nicht nur `cc-steuerung`.

## 2. Die 3 Blind-Misses

### `PATCH oder MINOR für dieses feature?`

Das war ein echter Description-Miss, jetzt vermutlich geheilt. Mit der Description-Ergänzung "including PATCH/MINOR/MAJOR decisions or semver questions" würde ich triggern. Ohne diese Ergänzung hätte ich bei reinem Blind-Routing gezögert, weil "dieses feature" auf eine bestehende Codeänderung verweist und "PATCH/MINOR" zwar semver signalisiert, aber nicht zwingend "Commit-Helper".

Noch besser wäre der exakte deutsche Trigger im Haupt-Trigger-Feld:

```text
Use when users ask for a commit message, commit command, version bump, semver decision, or whether a change is PATCH/MINOR/MAJOR.
```

### `Runde 3 erstellen`

Das ist strukturell kontextabhängig. Ohne ChatGPT-Review-Verlauf kann "Runde 3" alles heißen: Reviewrunde, Mockup-Runde, Sprint-Runde, Chat-Folgeprompt. Die Description kann es nur teilweise heilen.

Ich würde nicht versuchen, das blind perfekt zu machen. Besser:

```text
Use when users ask for a ChatGPT review prompt, a follow-up review round, or phrases like "Runde 2/3/N erstellen" in an active ChatGPT review context.
```

Und im Router als Kontextregel:

```python
if query.matches(r"runde\s+\d+\s+erstellen") and context.contains("ChatGPT review"):
    return "chatgpt-review"
else:
    ask_user_input_v0(["ChatGPT-Review-Runde", "Claude-Handover", "anderer Prompt"])
```

### `neue View für ProfileWizard`

Das ist kein reiner Description-Miss, sondern ein Domänenproblem. Für BPM/WPF ist "View" wahrscheinlich Code/XAML. Für ein generisches LLM ist es UI/Mockup. Die mockup-Description grenzt HTML-Mockup gut ab, aber code-erstellen sagt nicht explizit genug "XAML views / WPF views".

Ich würde `code-erstellen` ergänzen um:

```text
Use when users want to implement or change application code, including WPF/XAML views, dialogs, code-behind, services, validation, persistence logic, or data flows.
```

Das ist die wichtigste kleine Description-Schärfung vor Feature-Arbeit, weil PlanManager/ProfileWizard/ImportPreviewDialog viel UI-Sprache enthalten wird.

## 3. Meta-Doku-Prüfung

Die Doku ist insgesamt brauchbar, aber es gibt Drift-Risiko und mindestens einen verifizierten Drift.

### Redundanz

`README.md`, `INDEX.md`, `CHANGELOG.md`, `MEMORY-RUBRIKEN.md` und Skill-Bodies halten teils dieselben Invarianten: ask_user_input, Branch-Ermittlung, Two-Place-Pflege, Frühphase, Memory-Rubriken. Das ist absichtlich verständlich, aber wartungsgefährlich.

Meine Empfehlung:

* `INDEX.md` bleibt **Routing + Invarianten Source of Truth**.
* `README.md` bleibt **Erklärung/Onboarding**, darf keine feineren Regeln enthalten als `INDEX.md`.
* Skill-Bodies enthalten nur die Regeln, die zur Ausführung dieses Skills nötig sind.
* `CHANGELOG.md` enthält nur Historie, keine normative Regelquelle.

Konkreter Drift: Prompt sagt `v0.18.0`, `CHANGELOG.md` auf `main` endet im gelesenen Stand bei `v0.17.7`. Gleichzeitig ist `skill-pflege-007` im Skill-Body vorhanden. Das ist kein Architekturblocker, aber ein klares Abschluss-Audit-Finding.  

### Lücken

Die größte Lücke ist kein Text, sondern Messbarkeit: 7 von 11 Skills ohne Eval-Datei. Gerade `audit`, `doc-pflege`, `mockup-erstellen`, `cc-steuerung` und `skill-pflege` sind konfliktträchtig. Für Feature-Arbeit ist das kein No-Go, aber für "stabil" ist es zu wenig.

Ich würde keine vollständige Eval-Suite vor Feature-Start erzwingen. Aber ich würde eine Mini-Smoke-Eval ergänzen:

```text
evals/smoke-all-skills.md
- 3 should_trigger pro Skill
- 2 should_not_trigger pro Skill
- 2 conflict-near-misses pro konfliktträchtigem Skill
```

Das ist billiger als 7 volle Eval-Dateien und fängt die gefährlichsten Regressionen.

### Falsche Platzierung

`README.md` enthält viele normative Regeln, die eher nach `INDEX.md` gehören. Da `INDEX.md` bereits als Routing-Matrix und Invarianten-Doku fungiert, sollte README nicht zur zweiten Regelquelle werden.

Beispiel: Two-Place-Pflege steht in `INDEX.md`, `README.md` und `skill-pflege`. Das ist okay, wenn `skill-pflege` die operative Version enthält und `INDEX.md` die Kurz-Invariante. README sollte nur verweisen.

## 4. skill-pflege-007 — richtiger Ort?

Meine Wahl: **(b) in eine Meta-Stelle wie `INDEX.md`, zusätzlich in `skill-pflege` belassen. Kein neues Skill. Nicht primär in `chat-wechsel`.**

Begründung:

* Die Regel betrifft Plattformverhalten und ist damit eine System-Invariante für alle Skill-Updates.
* Der operative Fehler passiert in `skill-pflege`, deshalb muss sie dort hart stehen bleiben.
* `skill-neu` ist ebenfalls betroffen, weil neue Skills auch als `SKILL.md`-Artifact ausgeliefert werden.
* `chat-wechsel` ist nur indirekt betroffen, wenn ein Handover die Regel weitergeben muss.
* Ein separates `claude-plattform-quirks`-Skill wäre Overengineering und verschlechtert Routing: Plattform-Quirks sind Querschnittsregeln, kein User-Intent.

Ich würde in `INDEX.md` Invariante 8 erweitern:

```text
Artifact-Regel:
- Artifact-Dateiname MUSS exakt SKILL.md heißen.
- Pro Antwort maximal EIN Skill-Artifact.
- Bei Multi-Skill-Updates: Repo-Edits dürfen gebündelt werden, Artifacts immer sequentiell über mehrere Antworten.
- Grund: /home/claude/SKILL.md ist ein einzelner Container-Slot und wird überschrieben.
```

Und in `skill-neu` einen Verweis ergänzen:

```text
Bei Skill-Erstellung gelten dieselben Artifact-Regeln wie skill-pflege:
exakt SKILL.md, ein Skill-Artifact pro Antwort, User-Speicherung abwarten.
```

Keine Migration. Keine Kompatibilitätsschicht. Einfach Doku ergänzen.

## 5. Reifegrad für BPM-Feature-Arbeit

**Go.** Das System ist reif genug, um Feature-Arbeit zu starten. Ein No-Go wäre nur gerechtfertigt, wenn Routing-Fehler Datenverlust, falsche DB-Migrationen oder dauerhafte Repo-Inkonsistenz wahrscheinlich machen würden. Das sehe ich nicht.

Aber ich würde Feature-Arbeit mit drei Schutzmaßnahmen starten:

### P0 vor oder direkt beim Start

1. `CHANGELOG.md` auf `v0.18.0` nachziehen. Der aktuelle Stand widerspricht dem Abschlusskontext.
2. `INDEX.md` um `skill-pflege-007` als globale Artifact-Invariante ergänzen.
3. `code-erstellen` Description um "WPF/XAML views/dialogs" schärfen.

Das sind kleine Änderungen mit hohem Nutzen.

### P1 in der ersten Woche

1. Smoke-Eval für alle 11 Skills.
2. Spezielle cc-steuerung-Multi-Trigger-Tests:

   * `cc lies INDEX.md`
   * `cc bau neue View ProfileWizard`
   * `dc commit bitte`
   * `Claude Code soll tracker done ...`
     Erwartung jeweils: Modalität + Fachintent sauber getrennt.
3. Mini-Eval für `mockup-erstellen ↔ code-erstellen`, weil jetzt UI-Feature-Arbeit kommt.

### Versteckte Fragilitäten unter Dauerlast

**Memory-Rubriken:**
Die Rubriken sind gut, aber unter Last droht Memory als Schatten-Backlog. Die Regel "nach 3 Handovers zu ClickUp eskalieren" muss wirklich gelebt werden, sonst werden `[VERIFY]` und `[INFRA-TODO]` zu Müllhalden.

**Tracker-Anker bei Multi-Task-Commits:**
Ein-Task-ein-Commit ist gut. Risiko entsteht, wenn Feature-Arbeit logisch zusammenhängend ist, aber ClickUp in zu kleine Tasks splittert. Dann können Anker und Commit-Disziplin mehr Koordinationslast erzeugen als Nutzen. Hier sollte der Scope-Check vor Taskwechsel hart bleiben.

**cc-steuerung bei PC-Wechsel:**
DC-Pfad-Auto-Discovery ist richtig. Risiko liegt nicht in der Idee, sondern in stiller Fallback-Magie. Wenn Hostname/OneDrive nicht erkannt wird, darf kein Pfad geraten werden. Sofort Auswahl/Setup per `ask_user_input_v0`.

**Frühphasen-Prinzip:**
Sehr wichtig für PlanManager und ImportPreview. Keine Migrationen vorschlagen. Bei Schema-/Config-Änderung: Datei löschen, neu anlegen lassen. Das muss in `code-erstellen` bei Feature-Arbeit dauerhaft präsent bleiben.

## Was ich als erstes fixen würde, wenn ich eine Woche hätte

Reihenfolge:

1. **Doku-Drift schließen:** `CHANGELOG.md v0.18.0`, `INDEX.md` Invariante zu `skill-pflege-007`.
2. **Description-Schärfung für UI-Code:** `code-erstellen` mit WPF/XAML View/Dialog ergänzen.
3. **Smoke-Eval alle 11 Skills:** keine große Testwissenschaft, sondern Regressionstest.
4. **cc-steuerung Contract-Test:** Multi-trigger statt Entweder-oder explizit prüfen.
5. **README entnormativieren:** README als Onboarding, `INDEX.md` als Regelquelle.

## Schlussurteil

Das System ist nicht "fertig", aber **produktionsfähig für frühe BPM-Feature-Arbeit**. Die Architektur ist bewusst regelstark; das ist hier angemessen. Der größte technische Reife-Mangel ist nicht die Skill-Logik selbst, sondern die fehlende unabhängige Messung über alle 11 Skills und die erste sichtbare Meta-Doku-Abweichung (`v0.18.0` vs. Changelog-Stand).

✅ **Einigkeit**

* Frühphasen-Prinzip bleibt: keine Migrationen, keine Legacy-Tolerance, bei Schema-/Config-Änderung Datei löschen und neu anlegen lassen.
* Konfliktpaar-Matrix ist im Kern brauchbar.
* cc-steuerung als Modalität statt Fachskill ist architektonisch richtig.
* `skill-pflege-007` ist real und wichtig.
* Go für BPM-Feature-Arbeit ist vertretbar.

⚠️ **Widerspruch**

* "Abschluss-Refactor komplett sauber dokumentiert" stimmt nicht vollständig: `CHANGELOG.md` auf `main` zeigt keinen `v0.18.0`-Eintrag.
* Die cc-steuerung-Abgrenzung ist semantisch klar, aber routertechnisch nur dann stabil, wenn Multi-Trigger unterstützt oder explizit operationalisiert wird.
* 4/11 Eval-Coverage reicht für "Start", aber nicht für "abschließend validiert".

❓ **Rückfragen**

* Gibt es bereits eine echte API-/Fresh-Model-Eval außerhalb der Claude-Selbstsimulation, oder ist die Selbstsimulation weiterhin der einzige Messlauf?
* Unterstützt der reale Claude-Skill-Router Mehrfachaktivierung zuverlässig, oder muss cc-steuerung als Modalitätsregel in jedem relevanten Fachskill explizit mitgeführt werden?
* Soll `README.md` künftig normative Regeln enthalten dürfen, oder soll `INDEX.md` strikt die einzige Invarianten-Quelle bleiben?
