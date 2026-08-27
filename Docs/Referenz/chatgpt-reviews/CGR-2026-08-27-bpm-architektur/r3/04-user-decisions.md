# Runde 3 — Herberts Entscheidungen

**Datum:** 2026-08-27

## Sign-off

✅ **Herbert gibt das Sign-off** („passt soweit, sign-off ja"). Damit ist das beidseitige Sign-off komplett:

- **Slice-Folge H0 + T0–T8** für den neuen Task „Import-Transaktions-Härtung" (BPM-112 Slice 3 = T1, Start nach Abschluss BPM-111.06)
- **11 Architektur-Invarianten** inkl. der beiden Runde-3-Präzisierungen:
  - (A) skipDuplicate-Undo-Semantik explizit: gemischter Import → undo-fähige Actions zurück, skipDuplicate bleibt gelöscht, Journal darf `undone` werden; reiner skipDuplicate-Import → kein Undo anbieten
  - (B) `import_actions.destination_path` UND (bei Nutzung) `import_action_files.destination_path` werden nullable — via planmanager.db-Reset, keine Migration
- **15 Akzeptanzkriterien** (ChatGPT r3) zur 1:1-Übernahme ins ClickUp-Ticket
- **Offener externer Punkt:** ID-basierter `document_key` (`BuildManualDocumentKey` nutzt noch Namen statt `building_part_id`/`building_level_id`) = Abnahmepunkt an BPM-111, NICHT Teil des neuen Tasks

## Folgeaktionen (Stand bei Serienabschluss)

- Serie abgeschlossen, README/INDEX finalisiert (diese Session)
- ClickUp-Task-Anlage + ADR-Verankerung der Invarianten: noch nicht beauftragt, als nächste Schritte vorgeschlagen
