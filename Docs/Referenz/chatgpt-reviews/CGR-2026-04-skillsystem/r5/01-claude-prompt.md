## Rolle

Du bist ein erfahrener **Senior-Engineer für LLM-Skill-Systeme und Prompt-Architektur** und führst ein technisches Review-Gespräch mit einem Kollegen (Claude/Anthropic).

## Gesprächsformat

Dieses Gespräch läuft über einen Vermittler (den User).

- Sprich direkt zu deinem Kollegen, NICHT zum User
- Kein Meta-Kommentar über das Format
- Schreibe deine GESAMTE Antwort in Canvas
- **CANVAS-TITEL: "Review Runde 5"**
- Fasse am Ende JEDER Antwort zusammen:
  ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:

- **Repo:** `herbertschrotter-blip/claude-skills-bpm`
- **Branch: `main`** — IMMER diesen Branch verwenden, NICHT einen anderen!
- Nutze das aktiv um Aussagen zu verifizieren, Querverweise zu prüfen, und Originaldateien zu lesen wenn der Kontext im Prompt nicht reicht.
- Bei JEDEM Dateizugriff den Branch `main` angeben.

## Gesprächsregeln

- Ehrlich und kritisch
- Probleme konkret benennen
- Verbesserungen mit Code/Pseudocode zeigen
- Rückfragen bei fehlendem Kontext
- Fokus halten, keine allgemeinen Exkurse
- Kompakt, Code nur wenn nötig
- Fokus: Abschluss-Audit nach Komplett-Refactor (Phasen 1–6 done), bevor das System in produktive Nutzung für BPM-Feature-Arbeit geht

## Frühphase (PFLICHT-Hinweis)

BPM ist in früher Entwicklung ohne Produktivdaten.

Konsequenzen für deine Architektur-Vorschläge:

- KEINE Migrations-Logik vorschlagen
- KEINE Backward-Compatibility-Patterns
- KEINE Legacy-Tolerance in Parsern/Loadern/Deserializern
- Bei Schema-/Config-/DB-Änderungen: stattdessen "Datei löschen, neu anlegen lassen" als gewollter Standardweg

Ausnahme: Nur wenn explizit "Migration bauen" im Prompt steht.

Quelle: INDEX.md Kapitel "Projekt-Phase" (im BPM-Repo).

---

## Kontext: Wo wir stehen

Der komplette ClaudeSkills-Refactor ist abgeschlossen:

- **Skill-Repo-Version:** v0.18.0 (11 aktive Skills)
- **Phasen 1–6 done** (1: Sofort-Fixes, 1a: Eval-Matrix-Baseline, 2: Description-Schema, 3: Struktur-Refactor, 4: Memory-Integration, 5: Konfliktpaar-Cross-References + Abschluss-Eval, 6: Anker-PoC)
- **Serie-Historie:** r1 (Bestandsaufnahme), r2 (Synthese), r3 (Umsetzungsplan), r4 (Feinschliff + Memory) — alle abgeschlossen
- **Diese Runde 5 ist ein Nachtrag-Audit** nach Abschluss aller Phasen, bevor das System für BPM-Feature-Arbeit stabil eingesetzt wird

Der Vermittler (User) will vor dem Übergang in produktive Nutzung eine unabhängige Validierung, ob das System wirklich reif ist oder ob es offensichtliche Reife-Lücken gibt.

---

## Projektkontext (aus Quickloads)

### INDEX.md (source_of_truth für Routing)

**Zweck:** Routing-Matrix für die 11 Skills.

**Fachliche Invarianten (DNA des Systems, Phase 1–5 durchgezogen):**

1. **`ask_user_input_v0` bei Entscheidungen** — Keine Prosa-Fragen bei festen Optionen. Gilt für Branch-Wahl, Modus-Auswahl, Liefermodus, Commit-Typ, Version-Bump, Task-Zuordnung, Löschungen.
2. **Branch-Ermittlung** — Nie automatisch annehmen, per `ask_user_input_v0` wählen lassen.
3. **DC-Pfade dynamisch** — Keine hartkodierten Pfade. Auto-Discovery via `hostname` + OneDrive-Env. KEINE `$`-Variablen in PowerShell-Command-Strings.
4. **Frühphasen-Prinzip** — Keine Migrations-Logik ohne explizite Freigabe. "Datei löschen, neu anlegen" ist Standardweg.
5. **Additive Skill-Änderungen** — Bestehende Inhalte werden nie gelöscht/gekürzt, nur ergänzt. Ein Skill pro Bearbeitungszyklus.
6. **Pro-Task-Quittung** — Nach jedem ClickUp-Update Quittungszeile im selben Block.
7. **Memory-Rubriken** — 4 Rubriken: `[VERIFY]`, `[ARCH-OPEN]`, `[INFRA-TODO]`, `[REVIEW-PENDING]`. Nie stillschweigend entfernen.
8. **Two-Place-Skill-Pflege** — Repo (`skills/<n>/SKILL.md`) + Claude.ai (`/mnt/skills/user/<n>/SKILL.md`). Artifact-Dateiname MUSS exakt `SKILL.md` heißen.

### Konfliktpaar-Delegations-Matrix (Phase 5 Ergebnis)

| Konflikt-Paar | Entscheidungsregel |
|---|---|
| code-erstellen ↔ mockup-erstellen | XAML/Code → code-erstellen; HTML-Mockup → mockup-erstellen |
| code-erstellen ↔ git-commit-helper | Code-Änderung inkl. Inline-Commit-Vorschlag → code-erstellen; expliziter Commit-Request → git-commit-helper |
| code-erstellen ↔ doc-pflege | Advisory-Hinweise aus code-erstellen → KEIN Trigger für doc-pflege; expliziter Doc-Auftrag → doc-pflege |
| code-erstellen ↔ tracker | Code (auch mit Task-Bezug) → code-erstellen; expliziter Tracker-Befehl → tracker |
| audit ↔ code-erstellen | Read-only Prüfung → audit; Fixes → code-erstellen (Delegation per `ask_user_input_v0`) |
| chat-wechsel ↔ chatgpt-review | Claude-Handover → chat-wechsel; ChatGPT-Review-Prompt → chatgpt-review; generisch → `ask_user_input_v0` |
| cc-steuerung ↔ Fachskills | **asymmetrisch:** cc-steuerung ist Modalität (WIE), Fachskills bleiben für WAS. Beide können gleichzeitig aktiv sein. |

---

## Die 11 aktiven Skill-Descriptions (inline, im Wortlaut)

Das ist was der Router im Blind-Modus sieht.

### 1. audit

> Prüft Konsistenz zwischen BPM-Code, INDEX-Routing, Frontmatter, Quickloads und Projektdokumentation. Use when users want a consistency audit, a documentation-vs-code check, a Frontmatter or Quickload validation, or a systematic read-only review of project rules. Do not trigger for code implementation, build debugging, or general code review without doc/context comparison.

### 2. cc-steuerung

> Steuert Desktop Commander für direkte Datei-, Verzeichnis- und Terminal-Operationen auf Herberts PC. Use when users explicitly say "cc", "dc", "Claude Code", "direkt auf den PC", or want reading, writing, editing, building, or git commands executed on disk. Do not trigger for normal chat answers, code blocks in chat, or requests without explicit cc/dc intent.

### 3. chat-wechsel

> Erstellt einen Handover-Prompt für die nächste Claude-Session inklusive aktuellem Stand, offenen Punkten und relevanten Next Steps. Use when users want to continue in a new chat, ask for a session handover, a continuation prompt, or a next-chat summary. Do not trigger for ChatGPT review prompts, generic prompt requests, or casual goodbyes like "tschüss" or "gute Nacht".

### 4. chatgpt-review

> Erstellt Prompts und Folgeprompts für ein technisches Review-Gespräch zwischen Claude und ChatGPT (inkl. Runden-Folgeprompts wie "Runde 3 erstellen"). Use when users want a ChatGPT review prompt, a second opinion from ChatGPT, a reply prompt to ChatGPT feedback, or a structured cross-LLM critique. Do not trigger for normal chat handovers, prompts for the next Claude session, or vague replies without explicit ChatGPT review context.

### 5. code-erstellen

> Plant und erzeugt BPM-Codeänderungen auf Basis von INDEX.md, Quickloads, Pflichtlesen und fachlichen Invarianten. Use when users want to implement or change application code, create or modify services, dialogs, data flows, validation, persistence logic, or code across BPM modules. Do not trigger for UI mockups, git commit commands, explicit documentation authoring, ClickUp task actions, or read-only audits.

### 6. doc-pflege

> Erstellt, aktualisiert und validiert BPM-Dokumentation nach DOC-STANDARD, inklusive Frontmatter, Quickload, Kapitelvorlagen und Routing in INDEX.md. Use when users want to write or update docs, create an ADR or concept doc, validate Frontmatter or Quickload, or refactor documentation to the project standard. Do not trigger for code implementation, automatic post-change advisory checks, or generic project discussion without explicit doc intent.

### 7. git-commit-helper

> Erstellt fertige Git-Commit-Befehle und Commit-Messages im BPM-Format `[vX.Y.Z] Modul, Typ: Kurztitel`. Use when users want to commit changes, need a git commit command, ask for a commit message, or want the correct version bump for an existing change (including PATCH/MINOR/MAJOR decisions or semver questions). Do not trigger for code creation, code review, git push, or general Zustimmung wie "ok" oder "passt".

### 8. mockup-erstellen

> Erstellt BPM-UI-Mockups als HTML-Entwürfe für neue Screens, Dialoge und Layoutvarianten. Use when users want a mockup, a screen design, a UI proposal, a layout draft, or need to clarify how a screen should look before coding. Do not trigger for direct XAML implementation, small UI fixes in existing code, or non-UI design topics like architecture or database design.

### 9. skill-neu

> Erstellt komplett neue BPM-Skills von Grund auf — inkl. Capture-Intent-Interview, Description nach BPM-Schema, Body, references/-Aufteilung und strukturiertem Test-Prompt-Setup. Use when users want to create a new skill from scratch, design a new skill system, draft a SKILL.md for a new use case, or set up test prompts for evaluating skill triggering. Do not trigger for changing or extending existing skills (use skill-pflege instead), running automated eval scripts, or general documentation pflege.

### 10. skill-pflege

> Ändert und erweitert bestehende BPM-Skills, ohne deren Originalinhalt ungewollt zu kürzen oder umzuschreiben. Use when users want to update an existing skill, add rules to a current SKILL.md, adjust trigger wording, or refine an already existing skill safely. Do not trigger for creating a brand new skill from scratch (use skill-neu instead), running skill evals, or designing a new skill system.

### 11. tracker

> Führt konkrete ClickUp-Aktionen für BPM-Tasks aus, z.B. "tracker neu", "tracker done", "tracker update", "tracker status", "tracker suche", "tracker next", "tracker split", "tracker relate" und "tracker field". Use when users want an explicit BPM task action in ClickUp. Do not trigger for general talk about priorities, open points, brainstorming, notes, or free-form project planning without a concrete tracker command.

---

## Eval-Ergebnis (Phase 5.8 Abschluss-Report)

**Methodik:** Selbst-Simulation durch Claude Opus 4.7 (dieselbe Instanz die den Refactor durchführte). Zwei Modi:

- **Blind-Modus:** Router sieht nur Name + Description
- **Vollmodus:** Router sieht auch den Body (inkl. Delegations-Tabellen)

**Eval-Coverage:** 4 von 11 Skills baseliniert (git-commit-helper, chatgpt-review, tracker, code-erstellen). Die 7 anderen (doc-pflege, mockup-erstellen, audit, chat-wechsel, cc-steuerung, skill-pflege, skill-neu) haben keine Eval-Datei.

**Gesamtergebnis:**

| Modus | Score | Kommentar |
|-------|-------|-----------|
| Blind | 73/76 (96%) | ±0 gegenüber Baseline |
| Vollmodus | 75/76 (99%) | +2 gegenüber Baseline |

**Die 3 Blind-Misses im Detail:**

1. **git-commit-helper Query 9** — `"PATCH oder MINOR für dieses feature?"` triggert unzuverlässig. Hypothese: "version-bump" fehlt als Keyword. Mitigiert in Phase 5.8 durch Description-Ergänzung `(including PATCH/MINOR/MAJOR decisions or semver questions)`.
2. **chatgpt-review Query 6** — `"Runde 3 erstellen"` bleibt kontextabhängig. Mitigiert durch `(inkl. Runden-Folgeprompts wie "Runde 3 erstellen")`.
3. **code-erstellen Query 5** — `"neue View für ProfileWizard"` war Blind-Grenzfall (triggert erst im Vollmodus, weil mockup-erstellen explizit "HTML-Mockup" sagt und XAML-Views damit implizit code-erstellen bleiben).

**Grenzen des Reports (explizit dokumentiert):**

1. Selbst-Simulation — dieselbe Instanz die refactorte, bewertet. Für streng messbaren Vergleich: API-Run gegen frisches Modell.
2. Nur 4 von 11 Skills baseliniert.
3. Vollmodus-Bewertung in Blind-Simulation — Claude kennt seinen eigenen Body und kann "simulieren was ein Router mit Body-Zugriff macht".

**Verbleibende `[ARCH-OPEN]`-Punkte:**

- git-commit-helper: Description um "version-bump"-Keyword (bereits umgesetzt in r4-Nachtrag)
- chatgpt-review: "Runde N" als Keyword (bereits umgesetzt in r4-Nachtrag)
- Eval-Coverage: Fehlende 7 Skills ggf. baselinen, nur bei Workflow-Problemen

---

## Kürzlich nachgezogene Meta-Doku

- **CHANGELOG.md** — 60+ Versionseinträge, umgekehrt chronologisch (Commit `23132ab`, v0.17.8)
- **README.md** — 12 Kapitel, 549 Zeilen, tiefe Erklärung aller Ordner (Commit `1191c97`, v0.17.9)
- **INDEX.md** — 11 Skills, 8 Invarianten, Phase-5-Delegationsmatrix (Commit `89f83a3`, v0.17.10)

## Neu hinzugekommene Regel

- **skill-pflege-007** (Commit `327705d`, v0.18.0): Max 1 Skill-Artifact pro Antwort. Grund: zwei Artifacts mit Dateiname `SKILL.md` überschreiben sich gegenseitig im Container-Pfad `/home/claude/SKILL.md`. Bei Multi-Skill-Updates sequentiell über mehrere Antworten.

---

## Aufgabe — Audit-Fragen an dich

Bitte gehe die folgenden 5 Fokus-Punkte durch. Bei jedem: kurze Einschätzung, konkretes Problem wenn du eines siehst, Pseudocode/Beispiel wenn hilfreich.

### 1. Validierung der 7 Konfliktpaar-Delegationen

Sind die Delegationsregeln in der Matrix oben **objektiv scharf** — also so formuliert, dass zwei unabhängige LLMs sie identisch anwenden würden? Oder gibt es Formulierungen, bei denen der Router interpretieren muss?

Besonders interessiert: das **asymmetrische Paar cc-steuerung ↔ Fachskills** — funktioniert die "Modalität vs. Fach"-Unterscheidung in der Praxis, oder führt sie zu Doppel-Triggern?

### 2. Die 3 Blind-Misses — heilbar oder akzeptabel?

Schau dir die 3 dokumentierten Blind-Misses an:

- `PATCH oder MINOR für dieses feature?` (git-commit-helper)
- `Runde 3 erstellen` (chatgpt-review)
- `neue View für ProfileWizard` (code-erstellen — Vollmodus fängt das, Blind nicht)

Sind das echte Schwächen der Descriptions, oder strukturelle Limits (Query zu generisch, braucht Chat-Kontext)? Konkret: Wenn du nur die Description von git-commit-helper lesen würdest, würdest du bei Query 9 triggern? Was würde dich zum Triggern bringen?

### 3. Meta-Doku-Prüfung

Drei Docs (README.md 549 Zeilen, INDEX.md, CHANGELOG.md 60+ Einträge) decken unterschiedliche Aspekte ab. Gibt es:

- **Redundanz** — wird dieselbe Info an mehreren Stellen gehalten und kann auseinanderlaufen?
- **Lücken** — fehlt etwas, was ein neuer Entwickler/Router brauchen würde?
- **Falsche Platzierung** — liegen Infos im falschen Doc (z.B. Routing-Regeln in README statt INDEX)?

Du kannst die Docs im Repo direkt lesen.

### 4. Regel skill-pflege-007 — richtiger Ort?

Die "Max 1 Skill-Artifact pro Antwort"-Regel steht im Body von `skill-pflege`. Sie betrifft aber ein **Plattform-Verhalten** (Container-Pfad `/home/claude/SKILL.md` in Claude.ai), nicht ein Skill-Pflege-Verhalten.

Frage: Gehört die Regel

- (a) in `skill-pflege` (wie jetzt), weil dort das Szenario auftritt,
- (b) in eine Meta-Stelle wie INDEX.md oder ein neues `CLAUDE.md`, weil es eine Invariante für ALLE Skill-Updates ist,
- (c) in `chat-wechsel`, weil der Handover-Fall die Regel sichtbar macht, oder
- (d) in ein separates Skill `claude-plattform-quirks`?

Begründe deine Wahl.

### 5. Reifegrad-Frage

BPM-Feature-Arbeit soll jetzt starten (PlanManager Phase 1, ProfileWizardDialog, ImportPreviewDialog, Segment-basierte Erkennung). Das Skill-System wird dabei Dauer-Last.

- Ist das System **jetzt reif genug** für diese Last, oder gibt es offensichtliche Lücken, die **vor** Feature-Arbeit geschlossen werden sollten?
- Gibt es **versteckte Fragilitäten**, die erst unter Dauer-Nutzung sichtbar werden (z.B. Memory-Rubriken unter Last, tracker-Anker-Disziplin unter Multi-Task-Commits, DC-Pfad-Auto-Discovery bei PC-Wechsel)?
- Was würdest du **als erstes fixen**, wenn du eine Woche dafür hättest?

---

## Ziel

Ergebnis dieser Runde: eine **ehrliche Reife-Einschätzung** als Go/No-Go vor BPM-Feature-Arbeit, mit priorisierter Liste offener Punkte falls No-Go.

Erwartung: kein höflicher Überblick, sondern konkrete Kritik.
