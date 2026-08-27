# Runde 3 — Claude-Analyse (Sign-off-Bewertung)

**Datum:** 2026-08-27
**Ergebnis:** ChatGPT gibt das volle Sign-off für H0+T0–T8 und die 11 Invarianten. Ich schließe mich an — **beidseitiges Sign-off aus meiner Sicht erreicht**, vorbehaltlich Herberts Bestätigung.

## Bewertung der beiden Präzisierungen (beide übernehmen)

**A — skipDuplicate-Undo-Semantik:** Richtig und wichtig. Ohne die explizite Regel („gemischter Import → undo-fähige Actions zurück, skipDuplicate bleibt gelöscht, Journal darf undone werden; reiner skipDuplicate-Import → kein Undo anbieten") würde T7 die Semantik ad hoc erfinden müssen, sobald `GetLastCompletedImportId()` auf einen reinen Dubletten-Import trifft. → Wird als Invariante 7-Ergänzung übernommen.

**B — `import_action_files.destination_path` ebenfalls NOT NULL:** Verifiziert — `PlanManagerDatabase.cs:278` bestätigt die zweite NOT-NULL-Spalte. Die Nullable-Änderung gilt für beide Tabellen (soweit T2 `import_action_files` nutzt), via planmanager.db-Reset. → Übernommen.

## Verifikation des Vollständigkeits-Funds

**`document_key` noch nicht ID-basiert:** ✅ Bestätigt — `CaptureConfirmService.BuildManualDocumentKey()` (Z. 132–147) baut den Key aus `DocumentTypeId` (ID ✓) + `PlanNumber` + **normalisierten Namen** von `p.BuildingPart`/`p.Level` (keine IDs ✗). ADR-059 Punkt 3 verlangt `document_type_id + building_part_id [+ building_level_id] + plan_number`. ChatGPTs Einordnung ist korrekt: Das gehört NICHT in die Import-Transaktions-Härtung, sondern ist ein bestehender **BPM-111-V1-Abnahmepunkt** (fällig, sobald die Ringe aus Stammdaten gespeist werden — ADR-059-Addendum / BPM-111.05 Slice 2). Muss als Abnahmepunkt am BPM-111-Ticket festgehalten werden, damit er nicht verloren geht.

## Bewertung der 15 Akzeptanzkriterien

Vollständig und testbar formuliert — sie decken H0 (AK 1), T0 (AK 2), T1 (AK 3), T2 (AK 4–6), T3 (AK 7), T4 (AK 10), T5 (AK 8/9/11/12), T6 (AK 13), T7 (AK 14), T8 (implizit über die Crash-AKs 8/9/11) und Bucket A (AK 6/15) ab. Die Crash-Fenster-AKs (8, 9, 11) sind exakt die richtigen Schnittstellen der Zustandsmaschine. Zur 1:1-Übernahme ins ClickUp-Ticket geeignet; AK 15 sollte um die Präzisierung A (gemischter/reiner Duplicate-Import) ergänzt werden — inhaltlich steckt sie schon im letzten Satz.

## Konsens-Stand der Serie (für den Abschluss)

- **Task:** „Import-Transaktions-Härtung", Slices H0+T0–T8, BPM-112 Slice 3 = T1, Start nach BPM-111.06.
- **11 Invarianten** + Präzisierungen A/B → Kandidat für ADR-Verankerung (Addendum zu ADR-061 oder eigenes ADR).
- **15 Akzeptanzkriterien** → ClickUp-Ticket.
- **Offener externer Punkt:** ID-basierter `document_key` = BPM-111-Abnahmepunkt (nicht Teil des neuen Tasks).
- **Gestrichen für V1:** Skip-only-Fix, IsConflict-Fix, Preview-UX-Ausbau, LearnIndex-Profil-Lernen, Papierkorb, Diagramm 13 (vertagt).

## Nächste Schritte (nach Herberts Bestätigung)

1. Serie abschließen: README finalisieren, INDEX-Status „Abgeschlossen".
2. ClickUp-Task anlegen (tracker) mit den 15 AKs + Präzisierungen; `document_key`-Abnahmepunkt an BPM-111 notieren.
3. ADR-Verankerung der Invarianten (doc-pflege) — Empfehlung: ADR-Ergänzung statt Doppelung des Ticket-Texts.
