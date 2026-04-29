# Claude-Analyse — Runde 1

**Datum:** 2026-04-29
**Bezieht sich auf:** `02-chatgpt-response.md`

---

## Gesamtbewertung

ChatGPTs Antwort ist substantiell und trifft den Kern. Stimme in der Substanz mit fast allem zu, mit drei Detail-Differenzen.

## Wo ich vollständig zustimme

### Hybrid mit harter Schwelle (Frage 1)

Komplett-Migration zu CC würde Herbert die UX-Basis wegziehen, die er braucht (`ask_user_input_v0`-Buttons, Conversational Branching, Visualisierungen). Status quo verbessern adressiert das eigentliche Problem nicht. Hybrid ist richtig.

Die explizite Schwelle ist sehr stark:

- 1–2 Dateien + lokal beschreibbar → DC
- 3+ Dateien ODER Build-Loop ODER unklarer Refactor → CC

Viel besser als das aktuelle weiche „DC ist Default nach OK".

### Chat→CC-Live-Steuerung ist Fata Morgana auf Windows (Frage 2)

Deckt sich mit Web-Recherche. Anthropics Issue #27441 fordert genau das, weil es nicht existiert. Workarounds sind macOS-only, Custom-Engineering oder fragil im 3-PC-OneDrive-Setup. ChatGPTs Lösung ist die ehrliche: Chat bereitet Auftrag vor, User startet CC bewusst, Rückgabe via `git diff` / `git status` / Build-Output. Nicht magisch, aber stabil.

### Skill aufteilen (Frage 3 — Option A)

Der Name `cc-steuerung` für einen DC-Skill ist semantisch kaputt und hat genau cc-steuerung-004 produziert. Aufteilen in `dc-steuerung` + `cc-launcher` ist sauberer als umbenennen.

## Nuancierungen

### Branch-Vorrang (Bonus 1)

ChatGPT schlägt vor: „Wenn User im Prompt explizit Branch nennt, gilt der." Richtig, aber löst nur einen Teil. Das eigentliche Problem ist breiter: `cc-steuerung` mischt Branch-Logik (Modalität) mit GitHub-Read-Logik (Werkzeug). Bei sauberer Aufteilung gehört die Branch-Vorrang-Regel **nicht** in `dc-steuerung` oder `cc-launcher` — sie gehört in einen GitHub-/Repo-Zugriff-Block (INDEX.md Invariante 9 selbst, oder neuer Skill).

### Schwellen-Granularität

„Build-Loop erwartet" ist nicht binär. Manchmal weiß man es vorher nicht. Vorschlag: Wenn Claude die DC-Schwelle anwendet und während der Arbeit merkt „das wird zu groß", soll er via `ask_user_input_v0` eskalieren — nicht stumm weitermachen oder mittendrin wechseln.

## Widerspruch

### `.cc-task.md` als Default-Mechanismus

ChatGPT empfiehlt, CC-Auftrag als Datei im Repo zu schreiben. Hat ein OneDrive-Problem: Auf 3 PCs synchronisiert, mit Sync-Lag, kann veralteter `.cc-task.md` bei Herbert landen. Außerdem belastet es das Repo (`.gitignore`?).

**Alternative:** `.cc-task.md` außerhalb des Repos (Working-Dir) oder als reiner Copy-Paste-Block im Chat. Sollte ein Entscheidungspunkt für Herbert sein (siehe Rückfrage 2).

## Bewertung von ChatGPTs 4 Rückfragen

### Frage 1: CC darf Dateien selbst ändern oder nur Plan/Diff?

Kernfrage. Wenn CC nur Plan/Diff erzeugt und DC umsetzt → Build-Loop-Vorteil weg. Wenn CC direkt ändert → Herbert gibt Kontrolle ab.

**Mein Vorschlag:** CC darf direkt ändern, aber commits NICHT pushen. Herbert reviewt via `git diff` und pusht selbst. Passt zu „Herbert pusht IMMER selbst".

### Frage 2: `.cc-task.md` Datei oder Copy-Paste?

Siehe Widerspruch oben — empfehle Copy-Paste oder Datei außerhalb Repo.

### Frage 3: BPM-Repo-Pfad pro PC in `projects/bpm/`?

Unklar, müsste nachgeschaut werden. Falls nein → sollte rein, damit `cc-launcher` Pfade nicht raten muss.

### Frage 4: Alten `cc-steuerung` löschen oder Alias?

Klar: **löschen**. Kein Alias. Frühphase, keine Legacy-Toleranz. Passt zur Repo-Disziplin.

## Empfohlene Entscheidungen für Stufe A

Drei Entscheidungspunkte für den User per `ask_user_input_v0`:

1. **Migrations-Pfad:** Hybrid mit harter Schwelle (vorgeschlagen) vs Status quo
2. **CC-Auftragsformat:** Copy-Paste-Block / `.cc-task.md` im Repo / `.cc-task.md` außerhalb Repo
3. **CC-Schreibrechte:** CC darf Dateien direkt ändern (kein Push) vs CC erzeugt nur Plan/Diff

Plus immer Option „ChatGPT fragen" für Klärungs-Runde 2.
