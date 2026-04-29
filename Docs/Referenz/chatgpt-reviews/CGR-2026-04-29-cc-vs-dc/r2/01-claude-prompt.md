# Review-Prompt für ChatGPT — Runde 2

## Anschluss an Runde 1

Du hast in Runde 1 eine sehr klare Empfehlung gegeben: Hybrid mit harter
Schwelle, Skill-Aufteilung in `dc-steuerung` + `cc-launcher`, alten
`cc-steuerung` löschen. Herbert hat fast alle Empfehlungen übernommen.

In dieser Runde geht es um die **Umsetzungs-Details**. Drei Hauptfragen
und zwei Bonus-Punkte. Halte deine Antworten konkret — Pseudocode, kurze
Snippets, klare Regeln. Kein erneutes Aufrollen der Grundsatzentscheidung.

---

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:

- **Repo:** `herbertschrotter-blip/claude-skills-bpm`
- **Branch: `main`** — IMMER diesen Branch verwenden
- Bei JEDEM Dateizugriff den Branch `main` angeben

Außerdem hat Herbert das BPM-Repo gepusht, dort liegt jetzt die
vollständige Runden-1-Archivierung:

- **BPM-Repo:** `herbertschrotter-blip/BauProjektManager`
- Pfad: `Docs/Referenz/chatgpt-reviews/CGR-2026-04-29-cc-vs-dc/r1/`
- Enthält: `01-claude-prompt.md`, `02-chatgpt-response.md`, `03-claude-analysis.md`, `04-user-decisions.md`

Kannst du als Kontext lesen, falls Bezug auf Runde 1 nötig ist.

---

## Gesprächsformat

- Sprich direkt zu Claude, nicht zu Herbert
- Kein Meta-Kommentar
- Schreibe deine GESAMTE Antwort in Canvas
- CANVAS-TITEL: "Review Runde 2"
- Schließe mit: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen

---

## Festgelegte Entscheidungen aus Runde 1

Diese Punkte sind **entschieden** und nicht erneut zu diskutieren:

- ✅ Hybrid statt Komplett-Migration oder Status quo
- ✅ Harte Schwelle (3+ Dateien / Build-Loop / unklarer Refactor → CC)
- ✅ CC darf Dateien direkt ändern (kein Push, Herbert reviewt per `git diff`)
- ✅ `.cc-task.md` als Auftragsformat (im Repo, OneDrive-Lag-Risiko akzeptiert)
- ✅ Skill-Aufteilung Option A: `dc-steuerung` + `cc-launcher` (neue Skills)
- ✅ Alten `cc-steuerung` löschen, kein Alias

Wenn du eine dieser Entscheidungen für falsch hältst, sag es kurz —
aber arbeite nicht dagegen, Herbert hat entschieden.

---

## Hauptfragen Runde 2

### Frage 1 — `.cc-task.md`: versioniert oder gitignored?

Herbert hat sich für `.cc-task.md` im Repo entschieden, aber zwei
Sub-Optionen sind offen:

**A) `.cc-task.md` wird versioniert (in Git)**
- Audit-Trail: Welche CC-Aufträge wurden gestellt, wann, mit welchem Inhalt
- Reproduzierbar: Andere Sessions können nachvollziehen warum ein Refactor lief
- Nachteil: Repo füllt sich mit Auftrags-Snapshots, die meist obsolet sind

**B) `.cc-task.md` in `.gitignore` (nur lokal)**
- Repo bleibt sauber
- Pro PC eigene Auftrags-Datei
- Nachteil: Kein Audit-Trail, OneDrive-Sync ist die einzige
  Replikations-Strategie zwischen Herberts 3 PCs

**C) Hybrid: `.cc-task.md` im Repo, aber überschreibt sich**
- Nur die jeweils letzte Aufgabe steht drin
- Wird mit jedem neuen Auftrag überschrieben
- Audit-Trail nur über Git-History
- Vorteil: Automatischer Cleanup, trotzdem im Git

Welche Option ist sauber? Berücksichtige Herberts 3-PC-OneDrive-Setup.

### Frage 2 — Eskalations-Regel: konkrete Formulierung für `dc-steuerung`

In Runde 1 wurde besprochen: Wenn Claude während DC-Arbeit merkt, dass
es zu groß wird, soll er via `ask_user_input_v0` eskalieren statt stumm
weiterzumachen. Die genaue Formulierung im Skill ist offen.

Bitte gib eine **konkrete Regel-Formulierung** für `dc-steuerung/SKILL.md`
inklusive:

- Wann genau muss eskaliert werden? (Trigger, mind. 3 konkrete Bedingungen)
- Was sind die `ask_user_input_v0`-Optionen?
- Was passiert, wenn der User „weiter mit DC" wählt — gibt es Wiederholungs-Schutz?
- Wie unterscheidet sich Eskalation von einem regulären Re-Plan?

Zusätzlich: Soll die Eskalations-Regel im Frontmatter erwähnt werden
oder nur im Body?

### Frage 3 — Branch-Vorrang-Regel: wo gehört sie hin?

In Runde 1 (Bonus 1) hast du angesprochen, dass `cc-steuerung` Branch-
Logik mit GitHub-Read-Logik vermischt. Claude hat in Runde 1 widersprochen,
dass die Regel in `dc-steuerung` oder `cc-launcher` gehört.

Die offene Frage:

**A) INDEX.md Invariante 9 selbst** als globale Regel
**B) Neuer Skill `repo-zugriff`** (oder ähnlich) für GitHub-/Repo-Lesen
**C) Beide neuen Skills (`dc-steuerung` + `cc-launcher`) bekommen die Regel im Body**
**D) `chatgpt-review`-Skill bekommt sie (weil Reviews der Hauptanwendungsfall sind)**

Wo gehört sie hin? Begründe — und gib einen kurzen Skill-Body-Snippet
oder Invarianten-Text für deine empfohlene Position.

---

## Bonus-Punkte (knapp halten)

### Bonus A — 3-PC-OneDrive-Workflow für `.cc-task.md`

Konkreter Schritt-für-Schritt-Workflow gewünscht (max 8 Zeilen) für:
1. Chat schreibt `.cc-task.md` via DC
2. OneDrive-Sync abwarten
3. Herbert startet CC im Repo-Terminal
4. CC liest die Datei und arbeitet

Was tut Herbert wenn der Sync zu lange dauert? Was wenn auf PC-A der
Auftrag steht aber er an PC-B startet?

### Bonus B — Smoke-Cases für `dc-steuerung` + `cc-launcher`

Mindestens 6 Test-Cases (3 pro Skill) die Trigger-Stabilität prüfen.
Format wie in `evals/smoke-all-skills.md`. Inkl. Multi-Intent-Cases
(„CC mit Build-Loop UND code-erstellen").

---

## Frühphase (PFLICHT-Hinweis bleibt)

Frühphasen-Block aus Runde 1 gilt weiterhin: keine Migrations-Logik,
keine Backward-Compatibility, alten `cc-steuerung` löschen statt
umbenennen oder beibehalten.

---

Antworte in Canvas mit Titel "Review Runde 2". Schließe mit:

✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen
