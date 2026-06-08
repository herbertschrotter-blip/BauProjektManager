# Herberts Entscheidungen nach Phase 2 Stufe A (Runde 1)

**Kontext:** Phase 2 Stufe A-Vergleich Claude ↔ ChatGPT zeigte volle Einigkeit in 8 Punkten und 11 Verbesserungen, die alle übernommen wurden. Drei strategische Entscheidungen wurden Herbert vorgelegt.

## Entscheidung 1 — Snapshot-Strategie für Cross-Modul-Links

**Frage:** Wie sollen Plan-Verweise in Bautagebuch/Foto/Vorlagen persistiert werden?

**Entscheidung:** ✅ **Nur fixed_revision (ChatGPT-Empfehlung)**

**Konsequenz:**
- `plan_context_links.resolution_mode` wird auf einen Wert reduziert oder Spalte entfällt
- Beim Speichern eines Berichts/Fotos/Etc. wird IMMER konkrete `target_revision_id` festgezogen
- `context_time` bleibt für Audit/Debug
- Schema-Anpassung: `resolution_mode`-CHECK-Constraint nur noch `'fixed_revision'` ODER Spalte komplett raus
- Alte Berichte bleiben stabil auch bei späteren Korrekturen am Importdatum

## Entscheidung 2 — `rejected`-Status

**Frage:** `rejected` als dritter Wert in `revision_status` jetzt drin oder weglassen?

**Entscheidung:** ✅ **Drin lassen für bewusst verworfene Pläne (ChatGPT-Vorschlag)**

**Konsequenz:**
- `revision_status` CHECK: `('current', 'superseded', 'rejected')`
- Anwendungsfall: Vorabzüge die bewusst nicht als gültige Revision übernommen werden, oder Konfliktrevisionen aus `RevisionDecisionService`
- `ux_plan_revisions_current` UNIQUE-Index muss `WHERE revision_status = 'current'` haben (rejected zählt nicht als „current")

## Entscheidung 3 — Reihenfolge

**Frage:** Plan-Archiv-Ticket VOR oder NACH V1-Release?

**Entscheidung:** ⚡ **VOR V1-Release — Schema gleich richtig**

**Konsequenz (Roadmap-Änderung!):**

Beide Modelle hatten „nach V1-Release" empfohlen. Herbert entscheidet bewusst dagegen. Begründung implizit: V1 ist noch nicht released, Frühphase erlaubt sauberen Schnitt ohne Migration. Wizard (BPM-080.05) kann Schema-v2-Felder direkt berücksichtigen.

**Neue Reihenfolge:**

| # | Task | Status | Begründung |
|---:|---|---|---|
| 1 | BPM-NNN.01–.07 Plan-Archiv-Persistenz v2 | **wird neues V1-Sperr-Item** | Schema-Fundament zuerst |
| 2 | BPM-080.05 Schritte 3–5 weiterbauen | in progress | kann Schema-v2-Felder schon kennen |
| 3 | BPM-081 ImportPreviewDialog | in progress | nutzt neuen Schema-Stand |
| 4 | BPM-006 ProjectDetailView | open | nutzt neuen Schema-Stand |
| 5 | V1-Release | – | sauberer Schnitt |
| 6 | BPM-056 Bautagebuch | post-V1 | nutzt stabile Revision-Snapshots |
| 7 | BPM-057 Foto | post-V1 | |
| 8 | BPM-061 Vorlagen | post-V1 | |
| 9 | BPM-092 Profile in DB | post-V1 | unabhängig |

**Risiken:**
- V1-Release verzögert sich um geschätzt 1–2 Wochen
- Aktuelle V1-Tasks (080.05/081/006) könnten Schema-Wartesituation haben — Abhängigkeitsmanagement nötig
- Aber: Frühphasen-Reset ist explizit erlaubt, kein Produktivdaten-Risiko

**Vorteile:**
- Schema-Bruch nur einmal (statt zweimal: V1 + post-V1)
- Wizard / ImportPreview können von Anfang an die richtige Persistenz-Struktur nutzen
- Keine Refactoring-Schulden für die Module-Phase

## Empfehlung Phase 3

Konsens ist groß genug für Sign-off **ohne Runde 2** — keine ungelösten Streitpunkte. Was jetzt ansteht:

1. Konsens-Schema final dokumentieren (im README der Serie + neues ADR)
2. ClickUp-Issue mit 7 Subtasks anlegen (`tracker neu`)
3. Doc-Pflege: DB-SCHEMA.md vorbereiten + ADR-Stub

**ChatGPT-Rückfragen, die ich aus Kontext beantworten kann** (siehe 03-claude-analysis.md Abschnitt „ChatGPT-Rückfragen — meine Position dazu"):
1. Haus/Geschoss revisions-stabil → ja, FK in plan_documents
2. Snapshot fix → Entscheidung 1 oben
3. document_type_id aus Profil-JSON vorerst → ja
4. planmanager.db bleibt lokale Cache-DB → ja per ADR-053
5. Geschoss-Aliase erst bei Bedarf → ja, YAGNI

→ Keine Runde 2 nötig. **Status: Sign-off bereit.**
