# Review Runde 2 — Plan-Archiv-Architektur (Sanity-Check „vor V1")

## Gesprächsformat

Wie Runde 1: Direkt zu deinem Kollegen, CANVAS-TITEL **„Review Runde 2 — Plan-Archiv-Architektur"**. Am Ende:
- ✅ Einigkeit
- ⚠️ Widerspruch
- ❓ Rückfragen

## Repo-Zugriff

- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch verwenden
- 10 lokale Commits noch nicht gepusht (Stand auf GitHub kann hinter `v0.28.52` liegen)

## Stand nach Runde 1

**Konsens erreicht** — Herbert hat deine Verbesserungen alle übernommen:

| Punkt | Übernommen |
|---|---|
| `plan_document_segments` statt `_attributes` | ✓ |
| `superseded` statt `archived` + optional `rejected` | ✓ |
| `plan_revision_events` als Minimal-Audit | ✓ |
| `plan_context_links` Name + Snapshot auf `revision_id` | ✓ |
| `building_part_aliases` als Tabelle (nicht JSON) | ✓ |
| `+ segment_key` Denormalisierung | ✓ |
| `+ project_id` in `plan_documents` | ✓ |
| Plan-Archiv VOR BPM-092 | ✓ |
| Aufwand-Schätzung 6–8 PT | ✓ |
| Auto-Learn Stufe 1: exakt + Preview-Warnung, kein Fuzzy | ✓ |

**Drei strategische Entscheidungen** durch Herbert:

| # | Frage | Herberts Entscheidung |
|---|---|---|
| 1 | Snapshot-Strategie | ✅ **Nur `fixed_revision`** (deine Empfehlung — `target_revision_id` zwingend, kein `current_at_time`-Mode) |
| 2 | `rejected`-Status | ✅ **Drin lassen** (dein Vorschlag) |
| 3 | Reihenfolge | ⚡ **VOR V1-Release** statt nach (entgegen deiner und meiner Empfehlung) |

## Worum es in dieser Runde geht

**Nur ein Punkt — Entscheidung 3.** Beide Modelle (du und ich) hatten „nach V1-Release" empfohlen. Herbert wählt bewusst „vor V1" mit folgender Logik:

- Frühphase erlaubt Schema-Reset ohne Migration (per `INDEX.md` „Projekt-Phase")
- V1 ist noch nicht released — sauberer Schnitt jetzt möglich
- Wizard (BPM-080.05) kann gleich Schema-v2-Felder berücksichtigen statt später refactoren
- Schema-Bruch einmal statt zweimal

**Konkrete neue Roadmap:**

| # | Task | Status |
|---:|---|---|
| 1 | BPM-NNN Plan-Archiv-Persistenz v2 (.01 Schema, .02 Domain, .03 Pipeline) | **wird neues V1-Sperr-Item** |
| 2 | BPM-080.05 Schritte 3–5 weiterbauen | kann parallel mit Schema-v2-Bewusstsein |
| 3 | BPM-081 ImportPreviewDialog | UI, parallel möglich |
| 4 | BPM-006 ProjectDetailView | UI, parallel möglich |
| 5 | V1-Release | nach 1–4 |
| 6 | BPM-056 / 057 / 061 | nutzen stabile Persistenz |

V1 verzögert sich geschätzt um ~1–2 Wochen.

## Meine Bewertung (zur Diskussion)

Ich tendiere dazu Herberts Argument zu folgen — Schema-Bruch nur einmal ist sauberer. Aber zwei konkrete Bedenken:

1. **Scope-Creep-Risiko:** Aktuell sind 080.05 + 081 + 006 die V1-Endsprint-Items. Plan-Archiv-Ticket einzuschieben bedeutet einen neuen großen Brocken VOR den finalen Polish. Frage: Wird V1 dadurch eher in 2 Wochen oder eher in 6 fertig?

2. **BPM-080.05 Block-Abhängigkeit:** Wenn BPM-080.05 von Schema v2 wissen muss, dann ist es de facto blockiert bis .01–.03 fertig sind. Damit verschiebt sich auch BPM-081 (das auf 080.05 wartet).

## Konkrete Fragen an dich

1. **Siehst du echte Risiken bei „vor V1"**, oder ist das eine vertretbare Entscheidung — wenn ja, welche Bedingung muss erfüllt sein damit es funktioniert?

2. **Reihenfolge zwischen den V1-Sperrposten:** Würdest du eher empfehlen
   - (a) Plan-Archiv komplett erst (alle 7 Subtasks), dann 080.05/081/006, oder
   - (b) Plan-Archiv .01–.03 (Schema + Pipeline-Grundgerüst) zuerst, parallel 080.05 fortsetzen, dann Plan-Archiv .04–.07 nach 080.05 abschließen, oder
   - (c) etwas anderes?

3. **Gibt es einen Punkt im aktuellen V1-Plan**, an dem du noch zurückrudern würdest und Plan-Archiv doch nach V1 schieben würdest — z.B. wenn ein neues Risiko auftaucht?

4. **Tests-Risiko bei Schema-Bruch in Frühphase:** Aktuell 238 grüne Tests. Bei Schema-v2-Refactor wahrscheinlich 10–40 betroffen (PlanManager-Pipeline-Tests). Wie würdest du das in den 6–8 PT Aufwand einrechnen? Reichen die 6–8 PT inkl. Test-Refactor, oder sollte Herbert eher 8–10 PT planen?

5. **Risiko Wizard-Doppel-Refactor:** BPM-080.05 ist in progress mit dem alten Schema-Bewusstsein. Wenn .05 schon halb fertig ist und dann Schema v2 kommt — wieviel davon ist Wegwerfware? Soll BPM-080.05 pausiert werden bis Schema v2 steht?

6. **Wenn du frei entscheiden müsstest** — würdest du Herberts Entscheidung mittragen oder ihn überzeugen wollen doch nach V1 zu schieben? Sei ehrlich.

## Fokus

Keine erneute Schema-Diskussion. Keine Tabellen-Skizzen. Nur die **Reihenfolge-Frage** und ihre konkreten Konsequenzen für den Polish-Endsprint.

Kompakt — der Konsens steht, hier geht es um Roadmap-Risiko.
