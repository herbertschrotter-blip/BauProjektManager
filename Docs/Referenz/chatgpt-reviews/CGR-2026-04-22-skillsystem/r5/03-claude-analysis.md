# Claude-Analyse Runde 5

**Datum:** 2026-04-24
**Zu:** ChatGPTs Review-Antwort in `02-chatgpt-response.md`

---

## Gesamteindruck

ChatGPTs Antwort ist substantiell, kritisch und kalibriert. Kein Blindlob, kein Fake-Widerspruch. Das Go-with-conditions ist die richtige Antwort auf die Reife-Frage. Drei Punkte sind besonders wertvoll, einer ist diskutabel.

## Verifizierte Befunde

### CHANGELOG-Drift bestätigt — ChatGPT hat recht

`CHANGELOG.md` auf `main` endet bei `v0.17.7`. Es fehlen:

- v0.17.8 (CHANGELOG-Pflege selbst)
- v0.17.9 (README-Erweiterung)
- v0.17.10 (INDEX-Erweiterung)
- v0.17.11 (Description-Schärfungen git-commit-helper + chatgpt-review)
- v0.18.0 (skill-pflege-007)

Henne-Ei-Problem: Der Eintrag, der den CHANGELOG-Update dokumentieren würde, fehlt selbst.
Das ist der einzige harte Doku-Drift. Sehr nützlicher Befund.

### `code-erstellen` Description-Schärfung für WPF/XAML

Treffer. Im Eval-Vollmodus fängt es derzeit, aber im Blind-Modus bleibt "View" tatsächlich
domänenmehrdeutig. Vor PlanManager-Feature-Arbeit, wo "View", "Dialog", "Wizard" sehr oft kommen,
ist die Schärfung billig und wirkungsvoll.

### Smoke-Eval statt voller Eval-Suite

Das ist die richtige Größenordnung. Eine vollständige Eval-Suite für 7 Skills wäre Overengineering
vor Feature-Arbeit. 3+2+2 pro Skill (Smoke) ist machbar in einem Tag und schließt die größte Lücke.

## Diskutabel

### cc-steuerung-Zusatzregel in Fachskills

ChatGPTs Vorschlag, einen identischen Modalitäts-Block in jedem Fachskill zu spiegeln,
ist verteidigungstechnisch richtig — aber widerspricht der bewussten Asymmetrie aus Phase 5.7.
Wir haben absichtlich darauf verzichtet, Delegations-Zeilen in den 8 Fachskills zu setzen,
weil cc-steuerung kein konkurrierender Skill ist.

Mögliche Auflösung:
- Variante A: ChatGPT folgen, in jedem Fachskill identischen Modalitäts-Block ergänzen
  (8 Skills × ~3 Zeilen, ~24 Zeilen Redundanz)
- Variante B: Statt 8 Mal: einmal in INDEX.md als globale Routing-Regel
  ("cc-steuerung läuft parallel zu Fachskills, nie statt")
- Variante C: Den real beobachteten Fall messen: läuft das System in Praxis stabil mit der
  asymmetrischen Regel? Nur fixen wenn ein Test-Case fehlschlägt.

Ich tendiere zu B + C: globale Regel in INDEX, plus 1–2 echte cc-steuerung-Multi-Trigger-Test-Cases
als Smoke-Eval. Wenn die fehlschlagen, dann Variante A nachziehen.

### README entnormativieren

Korrekt im Prinzip. Aber mit Vorsicht: README dient auch als Onboarding für ein frisches Modell,
das ohne INDEX startet. Wenn README auf reine Verweise reduziert wird, verliert es Onboarding-Wert.
Mein Vorschlag: README darf normative Inhalte enthalten, MUSS aber als "siehe INDEX.md
für die verbindliche Fassung" gekennzeichnet sein. Das löst die Drift-Gefahr ohne Onboarding-Verlust.

## Klare Übernahmen

P0 wie ChatGPT vorgeschlagen, eine Reihenfolge:

1. CHANGELOG nachziehen — zwingend, ist verifizierter Drift
2. INDEX.md Invariante 8 erweitern (Artifact-Regel mit "max 1 Artifact pro Antwort")
3. code-erstellen Description um WPF/XAML/Dialog/code-behind erweitern

P1 (erste Woche, nicht zwingend vor Feature-Start):

- Smoke-Eval-Datei `evals/smoke-all-skills.md`
- cc-steuerung Multi-Trigger-Cases als Teil der Smoke-Eval
- README-Drift-Schutz (Verweis-Banner, nicht entfernen)

## Antworten auf ChatGPTs Rückfragen

**1. Echte API-/Fresh-Model-Eval außerhalb der Selbstsimulation?**
Nein. Aktuell nur Selbst-Simulation. Ein Fresh-Model-API-Run wäre methodisch sauberer,
ist aber ein eigenes Projekt. Für Dauer-Last-Beobachtung würde ich zunächst den realen
Workflow als Test verwenden: jeder Trigger-Fail in echter BPM-Arbeit bekommt eine Zeile
in der Smoke-Eval und wird als Regression gepflegt.

**2. Multi-Trigger im realen Claude-Skill-Router?**
Aus Beobachtung: Claude.ai-Router lädt Skills parallel, wenn Descriptions matchen. Das heißt,
cc-steuerung + code-erstellen sind beide aktiv, wenn beide passen. Das war die Annahme
hinter Phase 5.7. Aber der reale Test fehlt — daher Smoke-Eval.

**3. README normative Regeln erlaubt?**
Mein Vorschlag: README enthält normative Inhalte mit explizitem Verweis-Banner
"Verbindlich: INDEX.md". Das verhindert Drift ohne Onboarding-Verlust.

## Nicht relevant für Runde 5

- ChatGPT hat keine fundamentalen Architektur-Probleme gefunden
- Keine Phase-5-Rückrollung nötig
- Konfliktpaar-Matrix bleibt gültig (mit Caveat zu Paar 1 + Paar 7)

## Empfehlung an Herbert

**Go für BPM-Feature-Arbeit, mit P0 als Vorbedingung in dieser Sitzung erledigt.**

P0 ist klein, eindeutig und reduziert das größte Risiko (Doku-Drift sichtbar nach außen).
P1 läuft parallel zu Feature-Arbeit als Beobachtungs-Backlog.

Wichtigste offene User-Entscheidung: cc-steuerung-Zusatzregel-Variante (A/B/C oben).
