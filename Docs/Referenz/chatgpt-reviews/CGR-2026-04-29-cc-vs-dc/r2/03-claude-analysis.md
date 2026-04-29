# Claude-Analyse — Runde 2

**Datum:** 2026-04-29
**Bezieht sich auf:** `02-chatgpt-response.md`

---

## Gesamtbewertung

ChatGPT hat diese Runde mit **operativen Konkretionen** geliefert, nicht mehr Grundsatzfragen. Sehr substantiell. Stimme bei fast allem zu, mit zwei Detail-Differenzen.

## Wo ich vollständig zustimme

### Frage 1 — `.cc-task.md` versioniert + überschreibend (Option C)

Klare Argumentation:
- Audit-Trail über Git-History (kein Müll-Ordner)
- OneDrive-Robustheit (Datei wird gesynct, nicht insellokal)
- Frühphase-konform (keine Verwaltungsschicht)

Sein vorgeschlagenes Format mit Frontmatter-Block (Erstellt/Quelle/Repo/Branch/Modus) ist wirklich gut — der Zeitstempel ist zentral fürs OneDrive-Sync-Problem.

### Frage 2 — Eskalations-Regel: Body statt Frontmatter, mit Wiederholungs-Schutz

Die 6 Eskalations-Bedingungen sind sehr durchdacht. Besonders **Punkt 5 (Chunking-Risiko: 3+ write_file/edit_block-Calls für eine Datei)** und **Punkt 6 (Kaskadenfund während der Arbeit)** adressieren Drift-Szenarien, die wir heute haben.

Der **Wiederholungs-Schutz** (max 2 Dateien / 3 Calls / 1 Build nach "trotzdem mit DC") ist ein wichtiger Anti-Drift-Mechanismus.

**Präzisierung "produktive Code-/XAML-/Projektdateien" statt "3+ Dateien" generisch:** Sonst wäre eine Doku-Aktualisierung in 4 Markdown-Files plötzlich CC-Pflicht.

### Frage 3 — Branch-Regel in INDEX Invariante 2

ChatGPT korrigiert mich produktiv. Ich hatte "neuer Skill `repo-zugriff`" als Option vorgeschlagen, ChatGPT lehnt mit "Querschnittsskill mit Trigger-Risiko" zurecht ab. Globale Regel in zentraler INDEX-Invariante ist sauberer.

### Bonus B — Smoke-Cases

12 Cases sind solide. Besonders gut:
- **DC-04** (`dc bau die neue ViewModel-Struktur mit Build-Loop fertig`) → erwartet `cc-launcher`. Testet ob Schwelle wichtiger ist als Trigger-Wort.
- **CCL-04** (`cc lies nur die INDEX.md`) → erwartet `dc-steuerung`. Provokant aber realistisch für historische Sprache.
- **CCL-06** (Mockup → CC) → testet Multi-Intent-Sequenzen.

### `cc-launcher` Minimal-Body und `dc-steuerung` Differenz zum alten Skill

Direkt umsetzbar. Schlanker DC-only-Body, klarer Auslass-Block.

## Detail-Differenzen

### Differenz 1 — Frontmatter

ChatGPTs Vorschlag ist gut, aber ich würde ergänzen: das Frontmatter sollte auch positiv `cc-launcher` als Alternative nennen, nicht nur als „use cc-launcher for those". Hilft Claude beim parallelen Lade-Pattern. Kleine Sache.

### Differenz 2 — `.cc-task.md`-Größe < 150 Zeilen

Sinnvolle Faustregel, aber **nicht hart in den Skill schreiben**. Manche legitimen Multi-File-Refactors brauchen mehr Kontext. Lieber als Empfehlung formulieren.

## Position zu ChatGPTs 3 Rückfragen

### Rückfrage 1: `.cc-task.md` immer im selben Commit?

**Empfehlung: Immer im selben Commit.** ChatGPTs zweite Variante („strenger") ist besser. Begründung: Wenn die Disziplin „Commit-Review entscheidet" lautet, wird sie unter Druck eingespart und der Audit-Trail fragmentiert. Harte Regel hier besser als weiche.

### Rückfrage 2: `cc` strikt = `cc-launcher` oder Sonderfall bei `dc-steuerung`?

Schwierig. Beide Risiken:
- Strikt = `cc-launcher`: bricht bestehende Sprachgewohnheit. „cc lies INDEX.md" würde fälschlich `cc-launcher` triggern.
- Sonderfall via ask: Friktion bei jeder kurzen `cc`-Eingabe.

**Vorschlag:** `cc` allein ist ambivalent → ask. `cc + Build/Refactor/Multi-File` → eindeutig `cc-launcher`. Das ist ChatGPTs Differenzierung. User entscheidet.

### Rückfrage 3: `cc-launcher` führt automatisch `git status --short` aus?

**Klare Empfehlung: Ja.** Wenn `.cc-task.md` geschrieben wird und Working Tree hat uncommittete Änderungen, will Herbert das vor CC-Start wissen. Sonst riskiert CC, ungesicherte Änderungen zu überschreiben. Billiger, hochwertiger Sicherheits-Check.

## Was ChatGPT übersehen hat

In der Umsetzungs-Reihenfolge am Schluss schreibt ChatGPT 7 Schritte. Ein wichtiger fehlt:

**Bevor `cc-steuerung` gelöscht wird**, muss der Memory-Eintrag aktualisiert werden:

> „Default-Ausführungsmodus ist DC (`edit_block` / `write_file`) sobald User das Konzept freigegeben hat. SUCHE/ERSETZE-Blöcke im Chat NUR wenn..."

Dieser Memory-Eintrag passt nicht mehr zur neuen Hybrid-Schwelle. Er muss aktualisiert werden, sonst widerspricht das Memory dem Skill-Body.

## Empfohlene Stufe-A-Entscheidungen für Herbert

Drei Entscheidungspunkte:

1. **Rückfrage 1:** `.cc-task.md` immer mit-committen vs nur bei Reviewrelevanz
2. **Rückfrage 2:** `cc` strikt = `cc-launcher` vs ambivalent (ask)
3. **Rückfrage 3:** `cc-launcher` führt automatisch `git status --short` aus vs nicht

Plus immer Option „ChatGPT in Runde 3 fragen".

## Status der Serie

Mit Runde 2 sind alle architektonischen und operativen Hauptfragen geklärt. Wenn Rückfrage 1-3 entschieden sind, kann die Implementierung starten. Eine **Runde 3 wäre nur nötig**, wenn bei einer der 3 Rückfragen Unklarheit bleibt — sonst ist die Serie damit abschließbar.

## Implementierungs-Reihenfolge (basierend auf ChatGPT + meiner Ergänzung)

```
0. Memory-Eintrag DC-Default aktualisieren auf Hybrid-Schwelle [Claude-Ergänzung]
1. INDEX Invariante 2 ergänzen: Branch-Vorrang
2. cc-steuerung löschen
3. dc-steuerung neu anlegen, bewusst DC-only
4. cc-launcher neu anlegen, bewusst CC-Auftragsdatei-only
5. INDEX Skill-Tabelle + Invariante 9 ersetzen
6. Smoke-Cases ersetzen
7. Two-Place-Sync über Artifact, dann frischer Claude.ai Smoke-Test
```

Keine Aliasphase, keine Kompatibilität, keine Migration.
