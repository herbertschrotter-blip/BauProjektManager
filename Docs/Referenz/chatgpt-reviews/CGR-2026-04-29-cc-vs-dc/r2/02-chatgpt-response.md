# ChatGPT-Antwort — Review Runde 2

**Modell:** GPT-5.4
**Datum:** 2026-04-29
**Canvas-Titel:** Review Runde 2

---

Claude,

ich arbeite die Runde-1-Entscheidungen als gesetzt weiter. Ich habe `INDEX.md` und `evals/smoke-all-skills.md` auf `main` gelesen. Die R1-Archivdatei im BPM-Repo war unter dem angegebenen Pfad über den GitHub-Zugriff nicht auffindbar; für diese Runde reicht der im Prompt gesetzte Entscheidungsstand.

## 1 — `.cc-task.md`: versioniert, gitignored oder überschreibender Hybrid?

**Empfehlung: C — `.cc-task.md` versioniert, aber immer überschreibend.**

Das ist für Herberts Setup der beste Kompromiss:

```text
.cc-task.md liegt im Repo
.cc-task.md wird versioniert
jede neue CC-Aufgabe überschreibt die Datei vollständig
kein Task-Archiv-Ordner
kein .gitignore für diese Datei
Audit-Trail nur über Git-History
```

Warum nicht A:

```text
.cc-tasks/2026-04-29-...md
```

Das erzeugt Ablage-Müll. Die meisten CC-Aufträge sind transiente Arbeitsanweisungen, keine fachliche Doku. Wenn jede Aufgabe als eigene Datei bleibt, wird das Repo mit operativen Wegwerf-Artefakten gefüllt. Später muss jemand aufräumen oder erklären, welche Aufträge noch relevant sind.

Warum nicht B:

```text
.cc-task.md in .gitignore
```

Für einen einzelnen Entwickler wäre das sauber. Für Herbert mit 3 PCs + OneDrive ist es schlechter, weil die Datei dann nur über lokalen Sync lebt. Wenn Herbert am falschen PC startet, ist unklar, ob der Auftrag da ist, alt ist oder nie synchronisiert wurde. Außerdem verliert ihr die Möglichkeit, im Commit-Verlauf nachzuvollziehen, welcher Auftrag zu einer größeren Änderung geführt hat.

C passt zum Frühphasenmodus: einfache Datei, keine Verwaltungsschicht, keine Migration, kein Archiv-Loader.

### Konkreter Dateistandard

Pfad im jeweiligen Arbeitsrepo:

```text
.cc-task.md
```

Inhalt bewusst maschinen- und menschenlesbar, aber kein YAML-Zwang:

````md
# Claude Code Auftrag

- Erstellt: 2026-04-29 14:35 Europe/Vienna
- Quelle: Claude.ai Chat via Desktop Commander
- Repo: BauProjektManager
- Branch: main
- Modus: CC darf Dateien ändern, aber nicht pushen

## Ziel

<Kurze Zielbeschreibung>

## Kontext

<Relevante Architektur-/Task-/Dateihinweise>

## Harte Grenzen

- Kein git push
- Keine neuen Packages ohne Rückfrage
- Keine Migrationen / Backward-Compatibility-Logik
- Keine Dateien außerhalb des Repos ändern

## Arbeitsauftrag

1. Lies relevante Dateien selbst.
2. Ändere die nötigen Dateien direkt.
3. Führe `dotnet build` aus, falls Projekt buildbar ist.
4. Iteriere bis Build grün ist oder ein blockierender Fehler klar dokumentiert ist.
5. Gib am Ende aus:
   - geänderte Dateien
   - Build-Ergebnis
   - offene Risiken

## Rückgabe an Chat

Nach Abschluss bitte Herbert diese Befehle ausführen lassen:

```powershell
git status --short
git diff --stat
dotnet build
````

````

### Git-Policy

`.cc-task.md` wird **normal committet**, aber nicht zwingend in jedem Feature-Commit. Regel:

```text
Wenn CC-Auftrag zu Codeänderung führte:
- .cc-task.md im selben Commit mitnehmen, wenn der Auftrag für Review/Audit nützlich ist.
- .cc-task.md nicht mitnehmen, wenn der Auftrag nur temporär war und der Commit selbst ausreichend beschreibt.
````

Ich würde hier nicht dogmatisch sein. Wichtig ist: Datei ist nicht gitignored. Ob sie im Commit landet, entscheidet der Commit-Review. Wenn ihr strenger sein wollt, dann: `.cc-task.md` immer mit dem CC-generierten Commit mitschreiben. Das macht die Kausalität maximal klar.

## 2 — Eskalations-Regel für `dc-steuerung/SKILL.md`

Die Eskalationsregel gehört **in den Body**, nicht ins Frontmatter. Im Frontmatter reicht ein kurzer negativer Trigger, damit große CC-Fälle nicht falsch bei DC landen.

### Frontmatter-Hinweis

```yaml
---
name: dc-steuerung
description: >
  Steuert Desktop Commander für direkte Datei-, Verzeichnis-, Terminal-, Git-
  und Build-Operationen auf Herberts Windows-PC. Use when users say "dc",
  "Desktop Commander", "direkt auf den PC", "schreib auf Platte", or approve
  small local file operations after a concrete plan. Do not trigger for Claude
  Code handoff, large multi-file refactors, build-fix loops, or unclear repo-wide
  code changes; use cc-launcher for those.
---
```

Frontmatter muss kurz bleiben. Die eigentliche Eskalationslogik ist operativ und gehört in den Body.

### Konkreter Body-Snippet

```md
## Eskalation von DC zu Claude Code

DC ist das Werkzeug für kleine, klar begrenzte lokale Operationen. Sobald
während Analyse oder Ausführung erkennbar wird, dass die Aufgabe die DC-
Schwelle überschreitet, MUSS Claude stoppen und per `ask_user_input_v0`
eskalieren. Nicht still weiterarbeiten.

### Eskalationspflicht

Eskalieren, sobald mindestens eine Bedingung zutrifft:

1. **Dateiumfang:** Es müssen voraussichtlich 3 oder mehr produktive Code-/XAML-/Projektdateien geändert werden.
2. **Build-Loop:** Ein Build-/Testfehler erfordert voraussichtlich mehrere Analyse→Fix→Build-Zyklen.
3. **Unklarer Refactor-Scope:** Claude kann die vollständige Dateiliste vor Beginn nicht sicher bestimmen.
4. **Repo-weite Suche:** Die Änderung verlangt Suchen/Anpassen über mehrere Ordner oder Module.
5. **Chunking-Risiko:** Eine einzelne Datei würde wegen Größe/Struktur mehr als 3 `write_file`/`edit_block`-Operationen benötigen.
6. **Kaskadenfund:** Beim Bearbeiten tauchen Folgeänderungen in weiteren Dateien auf, die im ursprünglichen Plan nicht genannt waren.

### Eskalationsfrage

Bei Eskalationspflicht `ask_user_input_v0` verwenden:

Frage:
"Die Aufgabe ist größer geworden als sinnvoll für DC. Wie weiter?"

Optionen:
- "Zu Claude Code eskalieren (.cc-task.md vorbereiten)"
- "Erst Re-Plan im Chat zeigen"
- "Trotzdem mit DC fortfahren"
- "Abbrechen"

### Verhalten je Option

- **Zu Claude Code eskalieren:** Keine weiteren Dateiänderungen per DC. `cc-launcher` übernimmt und erstellt/aktualisiert `.cc-task.md`.
- **Erst Re-Plan im Chat zeigen:** Keine Dateiänderungen. Claude zeigt aktualisierten Plan, betroffene Dateien, Risiken und empfiehlt DC oder CC.
- **Trotzdem mit DC fortfahren:** Claude darf genau einen weiteren DC-Arbeitsabschnitt ausführen. Danach muss er neu prüfen.
- **Abbrechen:** Keine weiteren Tool-Calls außer ggf. read-only Status.

### Wiederholungs-Schutz

Wenn der User "Trotzdem mit DC fortfahren" wählt, gilt das nur für einen
Arbeitsabschnitt:

- maximal 2 weitere Dateien oder
- maximal 3 weitere `edit_block`/`write_file`-Calls oder
- maximal 1 Build-Versuch

Je nachdem, was zuerst erreicht wird. Danach MUSS Claude erneut stoppen.

Eine zweite Eskalation in derselben Aufgabe darf die Option "Trotzdem mit DC
fortfahren" nur noch anbieten, wenn seit der ersten Eskalation keine neue
Eskalationsbedingung hinzugekommen ist. Wenn eine neue Bedingung hinzugekommen
ist, Optionen nur:

- "Zu Claude Code eskalieren (.cc-task.md vorbereiten)"
- "Erst Re-Plan im Chat zeigen"
- "Abbrechen"

### Unterschied Eskalation vs. regulärer Re-Plan

Regulärer Re-Plan:
- vor Schreiboperationen
- dient der fachlichen Klärung
- kein Tool-/Modalitätswechsel zwingend

Eskalation:
- tritt während Analyse/Ausführung auf
- Ursache ist Überschreiten der DC-Schwelle
- stoppt weitere DC-Schreiboperationen
- bietet explizit Übergabe an `cc-launcher` an
```

Das ist streng genug, um den alten Fehler zu verhindern: DC darf nicht aus Bequemlichkeit weiterwursteln, wenn die Aufgabe eigentlich agentische Codearbeit ist.

### Kleine Präzisierung

„3+ Dateien" darf nicht blind heißen: drei kleine Markdown-Dateien sind nicht automatisch CC. Ich würde die Regel bewusst auf **produktive Code-/XAML-/Projektdateien** beziehen. Für reine Doku-Änderungen kann DC weiterhin sinnvoll sein, solange kein Such-/Build-/Refactor-Loop entsteht.

## 3 — Branch-Vorrang-Regel: wohin?

**Empfehlung: A — globale Regel in `INDEX.md`, aber nicht in Invariante 9, sondern in Invariante 2.**

Claude hat recht, wenn er sagt: Das gehört nicht primär in `dc-steuerung` oder `cc-launcher`. Branch-Vorrang ist keine lokale Ausführungsmodalität, sondern eine globale Repo-Zugriffsregel. Sie betrifft:

```text
chatgpt-review
skill-pflege
skill-neu
doc-pflege
audit
code-erstellen
git-commit-helper
dc-steuerung
cc-launcher
```

Ein neuer Skill `repo-zugriff` wäre jetzt Over-Engineering. Ihr würdet einen Querschnittsskill erzeugen, der bei fast allem mittriggern könnte. Das verschärft genau das Routingproblem, das ihr gerade stabilisiert habt.

Nicht D: `chatgpt-review` ist nur ein Hauptanwendungsfall, nicht die Regelquelle. Wenn die Regel dort steht, wird sie bei anderen GitHub-/Repo-Zugriffen vergessen.

Nicht C: Duplizieren in beide neuen Skills driftet auseinander und löst GitHub-Read-Fälle außerhalb DC/CC nicht.

### Konkreter INDEX-Snippet

Bestehende Invariante 2 ersetzen oder ergänzen:

```md
### 2. Branch-Ermittlung und Branch-Vorrang (Phase 1)

Grundregel: Nie automatisch einen Branch annehmen. Bei GitHub-/Repo-Zugriffen
muss Claude mit einem eindeutig bestimmten Branch arbeiten.

**Vorrang-Reihenfolge:**

1. **Expliziter User-Branch im aktuellen Prompt** gewinnt immer
   (`Branch main`, `auf feature/x`, `lies von release/y`). Keine Rückfrage.
2. **Bereits in dieser Session gewählter Branch** gilt weiter, solange der User
   keinen anderen Branch nennt.
3. **Kein Branch bekannt:** Branches per GitHub API oder `git branch -a` listen
   und per `ask_user_input_v0` wählen lassen.

**Wichtig:** Wenn ein Review-/Analyse-Prompt ausdrücklich einen Branch vorgibt,
ist das keine Annahme, sondern eine User-Vorgabe. In diesem Fall MUSS Claude
diesen Branch verwenden und darf keine zusätzliche Branch-Auswahl erzwingen.

**Bei jedem Dateizugriff dokumentieren:** Der Tool-Call verwendet den gewählten
Branch/Ref explizit, z.B. `ref: "main"`.
```

### Optionaler Verweis in neuen Skills

In `dc-steuerung` und `cc-launcher` nur ein kurzer Verweis, keine eigene Regelkopie:

```md
## Branch-Regel

Branch-Ermittlung folgt INDEX.md Invariante 2. Wenn der User im Prompt einen
Branch explizit nennt, diesen Branch verwenden. Nicht erneut fragen.
```

Das vermeidet Drift und hält die zentrale Regel im INDEX.

## Bonus A — 3-PC-OneDrive-Workflow für `.cc-task.md`

```text
1. Chat schreibt `.cc-task.md` per DC in das lokale Repo auf dem aktuell verwendeten PC.
2. Chat meldet: Datei geschrieben + Zielpfad + Zeitstempel im Auftrag.
3. Herbert wartet, bis OneDrive am Ziel-PC „aktuell" zeigt.
4. Herbert öffnet PowerShell im Repo-Ordner auf dem PC, auf dem CC laufen soll.
5. Herbert prüft: `Get-Content .\.cc-task.md -TotalCount 20`.
6. Wenn Zeitstempel/Ziel falsch ist: nicht starten; am richtigen PC starten oder Sync manuell anstoßen.
7. Start: `claude` im Repo, dann `Lies .cc-task.md und führe den Auftrag aus.`
8. Nach CC: `git status --short`, `git diff --stat`, `dotnet build` zurück in den Chat.
```

Wenn Sync zu lange dauert:

```text
- Auftrag im Chat anzeigen lassen und manuell auf PC-B in `.cc-task.md` speichern.
- Oder CC auf PC-A starten, wo die Datei sicher aktuell ist.
```

Wenn auf PC-A geschrieben wurde, Herbert aber auf PC-B startet:

```text
- Vor Start immer Zeitstempel in `.cc-task.md` prüfen.
- Bei altem Inhalt nicht CC starten.
- Entweder OneDrive-Sync abwarten/erzwingen oder Auftrag manuell kopieren.
```

Keine automatische Sync-Heilung bauen. Das wäre Infrastruktur-Engineering und in der Frühphase falsch.

## Bonus B — Smoke-Cases für `dc-steuerung` + `cc-launcher`

Ich würde den bestehenden `cc-steuerung`-Block in `evals/smoke-all-skills.md` löschen und durch zwei Blöcke ersetzen.

### `dc-steuerung`

```md
## dc-steuerung

| ID | Query | Erwartung | Kategorie | Notiz |
|---|---|---|---|---|
| DC-01 | `dc lies INDEX.md` | dc-steuerung | should_trigger (solo) | [synthetic] reine DC-Leseoperation |
| DC-02 | `direkt auf den PC: git status im BPM-Repo zeigen` | dc-steuerung | should_trigger (solo) | [synthetic] lokale Terminal-Operation |
| DC-03 | `schreib die kleine README-Korrektur direkt auf Platte` | dc-steuerung + doc-pflege | multi-trigger | [synthetic] Modalität + Doku, kleine Änderung |
| DC-04 | `dc bau die neue ViewModel-Struktur mit Build-Loop fertig` | cc-launcher + code-erstellen | conflict-near-miss | [synthetic] DC-Wort, aber CC-Schwelle überschritten |
| DC-05 | `wie funktioniert Desktop Commander?` | — (kein Skill) | should_not_trigger | [synthetic] Erklärfrage, keine Ausführung |
| DC-06 | `dc ändere diese 5 XAML- und C#-Dateien` | dc-steuerung + cc-launcher + code-erstellen (ask) | multi-trigger/escalation | [synthetic] DC explizit, aber Eskalationsfrage nötig |
```

### `cc-launcher`

```md
## cc-launcher

| ID | Query | Erwartung | Kategorie | Notiz |
|---|---|---|---|---|
| CCL-01 | `Claude Code soll den Multi-File-Refactor für ProfileWizard machen` | cc-launcher + code-erstellen | should_trigger | [synthetic] expliziter CC-Codeauftrag |
| CCL-02 | `cc mit Build-Loop: behebe die Fehler bis dotnet build grün ist` | cc-launcher + code-erstellen | multi-trigger | [synthetic] CC + Build-Loop + Code |
| CCL-03 | `bereite eine .cc-task.md für die neue ImportPreview-Architektur vor` | cc-launcher | should_trigger (solo) | [synthetic] Auftragserstellung ohne direkte Code-Ausführung im Chat |
| CCL-04 | `cc lies nur die INDEX.md` | dc-steuerung | conflict-near-miss | [synthetic] historisches cc-Wort, aber reine PC-Leseoperation; ggf. ask wenn unklar |
| CCL-05 | `mach einen kleinen Tippfehler in README direkt auf dem PC` | dc-steuerung | should_not_trigger | [synthetic] kleine DC-Arbeit, kein CC |
| CCL-06 | `erstelle ein Mockup und lass Claude Code danach die WPF-Implementierung bauen` | mockup-erstellen + cc-launcher + code-erstellen (ask) | multi-trigger | [synthetic] Sequenz: Mockup zuerst, CC-Implementierung danach klären |
```

Wichtig: `CCL-04` ist bewusst provokant. Wenn ihr `cc` künftig ausschließlich als Claude Code behandelt, dann muss Erwartung `cc-launcher` sein. Ich würde aber für Herberts echte Sprache differenzieren:

```text
cc + reine Datei-/Terminal-Leseoperation -> ask oder dc-steuerung
Claude Code + Build/Refactor/Codearbeit -> cc-launcher
```

Da Herbert historisch „cc" unscharf verwendet hat, ist das ein realistischer Smoke-Test für Trigger-Fragilität.

## Zusatz: konkrete `.cc-task.md`-Erzeugung per DC

Der `cc-launcher` sollte keine komplizierten PowerShell-Heredocs verwenden. Wegen eurer bestehenden PowerShell-Quoting-Fragilität ist DC `write_file` sicherer:

```text
write_file(
  path: "<workFolder>\\.cc-task.md",
  mode: "rewrite",
  content: "<vollständiger Markdown-Auftrag>"
)
```

Bei großer Datei: lieber 2–3 Append-Chunks. Aber wenn `.cc-task.md` regelmäßig so groß wird, dass sie chunking-lastig ist, ist der Auftrag zu lang. Zielgröße: unter 150 Zeilen.

## Zusatz: `cc-launcher` Minimal-Body

```md
# Claude Code Launcher

## Zweck

Bereitet Claude-Code-Aufträge für code-intensive Arbeiten vor. Der Skill startet
keine unsichtbare persistente CC-Session und behauptet keinen Live-Feedback-Loop.
Er erzeugt `.cc-task.md` und liefert den Start-/Rückgabeablauf.

## CC-Schwelle

CC verwenden, wenn mindestens eins zutrifft:
- 3+ produktive Code-/XAML-/Projektdateien
- Build-/Test-Fix-Loop nötig
- Refactor-Scope vorab unklar
- repo-weite Suche/Änderung nötig
- DC müsste viele Chunked Writes ausführen

## Ablauf

1. Zielrepo und Branch bestimmen. Branch-Regel: INDEX.md Invariante 2.
2. Falls `.cc-task.md` bereits existiert: vollständig überschreiben.
3. Auftrag mit Ziel, Kontext, Grenzen, Arbeitsschritten und Rückgabeformat schreiben.
4. User anweisen, auf dem Ziel-PC im Repo-Terminal `claude` zu starten.
5. Startprompt liefern: `Lies .cc-task.md und führe den Auftrag aus.`
6. Nach CC-Rückkehr: `git status --short`, `git diff --stat`, `dotnet build` prüfen.

## Verbote

- Kein git push
- Keine neuen Dependencies ohne Freigabe
- Keine Migration/Backward-Compatibility ohne expliziten Auftrag
- Keine externe CC-Orchestrierungsbrücke erfinden
- Keine File-Watch-/Relay-Infrastruktur bauen
```

## Zusatz: `dc-steuerung` Minimal-Body-Differenz zu altem Skill

Bei der Neuanlage nicht den alten `cc-steuerung`-Body kopieren und nur umbenennen. Das würde den Fehler konservieren. Die DC-Version sollte schlanker werden:

```text
behalten:
- DC-Verfügbarkeit über Tool-Liste
- Pfad-Auto-Discovery
- PowerShell-ohne-$-Regel
- write_file/edit_block-Regeln
- Berechtigungen
- Rückgabeformat

auslagern/entfernen:
- Claude-Code-Begriffe
- CC-Trigger
- große Refactor-Logik
- „cc" als Synonym für DC
```

## Schluss

Für die Umsetzung würde ich diese Reihenfolge nehmen:

```text
1. INDEX Invariante 2 ergänzen: Branch-Vorrang.
2. `cc-steuerung` löschen.
3. `dc-steuerung` neu anlegen, bewusst DC-only.
4. `cc-launcher` neu anlegen, bewusst CC-Auftragsdatei-only.
5. INDEX Skill-Tabelle + Invariante 9 ersetzen.
6. Smoke-Cases ersetzen.
7. Two-Place-Sync über Artifact, dann frischer Claude.ai Smoke-Test.
```

Keine Aliasphase, keine Kompatibilität, keine Migration.

✅ Einigkeit

* Hybrid ist gesetzt.
* `.cc-task.md` ist gesetzt.
* CC darf Dateien ändern, aber nie pushen.
* `cc-steuerung` wird gelöscht, nicht umbenannt.
* `dc-steuerung` und `cc-launcher` brauchen getrennte Trigger und getrennte Bodies.
* Branch-Regel braucht expliziten User-Branch-Vorrang.

⚠️ Widerspruch

* Ich widerspreche einer gitignored `.cc-task.md`; bei 3-PC-OneDrive ist das zu lokal und zu wenig nachvollziehbar.
* Ich widerspreche einem neuen `repo-zugriff`-Skill zum jetzigen Zeitpunkt; das ist ein Querschnittsskill mit hohem Trigger-Risiko.
* Ich widerspreche der Idee, die Eskalationsregel nur implizit über „3+ Dateien" zu lösen. Sie muss als Stop-Regel mit Wiederholungs-Schutz in den Body.

❓ Rückfragen

1. Soll `.cc-task.md` standardmäßig im gleichen Commit wie die CC-Codeänderung landen, oder nur wenn der Chat/Herbert sie für reviewrelevant hält?
2. Soll `cc` als Kurzform künftig strikt `cc-launcher` bedeuten, oder bleibt `cc lies Datei X` als historischer Sonderfall bei `dc-steuerung`/ask?
3. Soll `cc-launcher` nach dem Schreiben von `.cc-task.md` automatisch `git status --short` ausführen, um Herbert vor uncommitted Voränderungen zu warnen?
