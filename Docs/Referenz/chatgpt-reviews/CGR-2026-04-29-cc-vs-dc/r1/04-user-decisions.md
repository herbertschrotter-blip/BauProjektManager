# User-Entscheidungen — Runde 1

**Datum:** 2026-04-29
**Bezieht sich auf:** `03-claude-analysis.md` (Stufe A)

---

## Beantwortete Stufe-A-Fragen

### Frage 1: Migrations-Pfad?

**Entscheidung:** Hybrid mit harter Schwelle (3+ Dateien / Build-Loop / unklarer Refactor → CC) **plus Eskalations-Regel**: Wenn Claude während DC-Arbeit merkt, dass es zu groß wird, fragt er via `ask_user_input_v0` ob auf CC gewechselt werden soll.

**Begründung:** Anfangs Status quo gewählt aus Unsicherheit, ob CC wirklich tauglich ist. Nach Diskussion über Pro/Kontra harte vs weiche Schwelle:

- Harte Schwelle ist **vorhersagbar** (User weiß vorher: das wird CC)
- Harte Schwelle verhindert **Drift** (genau das Problem, das `cc-steuerung` hatte: weiche Regel → bequemer Default → DC)
- Harte Schwelle stabilisiert **über mehrere Claude-Sessions** (steht im Skill/Memory, muss nicht jedes Mal neu erklärt werden)
- Eskalations-Regel als Sicherheitsnetz: Wenn DC mittendrin unerwartet groß wird → fragen, nicht stumm weitermachen

ChatGPTs konkrete Schwelle übernehmen:
- 3+ produktive Code-Dateien
- XAML + Code-behind + ViewModel zusammen
- Build muss mehrfach laufen, Fehler sollen direkt zurück in Implementierung fließen
- Refactor-Auswirkung unklar
- „Mach es fertig bis dotnet build grün ist" Teil der Aufgabe

### Frage 2: CC-Schreibrechte?

**Entscheidung:** CC darf Dateien direkt ändern. Herbert reviewt via `git diff`, pusht selbst. Keine Plan-/Diff-only-Beschränkung.

**Begründung:**
- Plan-/Diff-only würde den Build-Loop-Vorteil neutralisieren (CC kann nicht iterativ bauen+testen+fixen wenn er nicht direkt ändern darf)
- Push-Beschränkung passt zu bestehendem Memory-Eintrag „Herbert pusht IMMER selbst"
- `git diff` als Review-Mechanismus ist erprobt und funktioniert auf allen 3 PCs

### Frage 3: CC-Auftragsformat?

**Entscheidung:** `.cc-task.md` im Repo. OneDrive-Lag-Risiko bewusst akzeptiert.

**Begründung:** Sauberster Workflow gewinnt gegen alternative Optionen:
- Copy-Paste-Block im Chat: Bricht den Workflow auf, kein Audit-Trail im Repo
- `.cc-task.md` außerhalb Repo (z.B. Working-Dir): Lokale Insellösung, kein Git-Verlauf

OneDrive-Lag-Risiko wird durch Disziplin gemindert: Auftrag generiert → kurz warten bis Sync durch → CC starten. Nicht ideal, aber lebbar.

**Offen für Runde 2:** Soll `.cc-task.md` in `.gitignore` (nur lokal) oder versioniert (Audit-Trail)?

---

## Vorab geklärte Punkte (aus Claude-Analyse)

### Skill-Aufteilung (Frage 3 von Runde 1)

**Entscheidung:** Option A — Aufteilen in `dc-steuerung` + `cc-launcher`. Alten `cc-steuerung` löschen, kein Alias.

**Begründung:** Konsens zwischen ChatGPT und Claude. Frühphase = keine Legacy-Toleranz. Saubere Trennung verhindert weiteres Drift wie cc-steuerung-004.

### Branch-Vorrang (Bonus 1)

**Position:** Die Regel „User-Branch im Prompt hat Vorrang" gehört NICHT in `dc-steuerung` oder `cc-launcher`, sondern in einen GitHub-/Repo-Zugriffs-Block (eventuell INDEX.md Invariante 9 oder neuer dedizierter Skill). In Runde 2 nochmal aufgreifen.

---

## Offene Fragen für Runde 2

Aus Claude-Analyse + neue Punkte:

1. **`.cc-task.md` versioniert oder gitignored?** Audit-Trail vs Repo-Sauberkeit
2. **Eskalations-Regel:** Wann darf Claude während DC-Arbeit eskalieren (genaue Trigger)? Wie formuliere ich das im Skill?
3. **Branch-Vorrang-Regel:** Wo gehört sie hin? INDEX.md Invariante 9? Neuer Skill? In `dc-steuerung`?
4. **3-PC-OneDrive-Setup mit `.cc-task.md`:** Konkreter Workflow für Sync-Sicherheit (warten bis Sync? Datei im `.git/` außerhalb OneDrive?)
5. **Smoke-Cases für die 2 neuen Skills:** Welche konkreten Cases müssen rein, damit Trigger-Stabilität getestet wird?

---

## Nicht zur Diskussion in Runde 2 (entschieden)

- Hybrid statt Komplett-Migration oder Status quo ✅
- Harte Schwelle statt weicher ✅
- CC darf direkt schreiben ✅
- `.cc-task.md` im Repo (Format) ✅
- Skill-Aufteilung Option A ✅
- Alten `cc-steuerung` löschen, kein Alias ✅
