# Review Runde 3 — Claude-Analyse (Stufe A)

## Gesamteinschätzung

Volle Zustimmung zu ChatGPT. Option A ist richtig. Die DDL-Korrektur (4 Cross-DB-FK-Klauseln entfernen, SoftRef-Kommentare + Cross-DB-Hinweis ergänzen) ist Pflicht vor BPM-109.01. Die 5 Härtungen (6.1–6.5 + ATTACH-Kapselung) sind sauber und tragbar. Kein sachlicher Widerspruch.

## Scope-Einordnung der Härtungen (Foundation Slice vs. post-V1)

| Härtung | Scope |
|---|---|
| 6.1 DDL-Fix (Cross-DB-FK raus, SoftRef-Kommentar) | **.01 (jetzt)** |
| 6.2 Harte Innen-FKs erhalten | **.01 (jetzt)** |
| 6.4 Import-Time-Validation (Resolve building_part/segment_type) | **.03** (deckt sich mit ADR-056-Health aus BPM-108) |
| 6.3 App-Level Delete Guard | **post-V1** (greift in Settings-UI, nur Invariante jetzt dokumentieren) |
| 6.5 Revalidate Command | **post-V1** (Wartungsservice) |
| 7. ATTACH-Kapselung | **post-V1** (IPlanLookupService-Impl .05; im Slice nur Stub .05a) |

## Antworten auf die 5 Rückfragen

1. **building_part_aliases:** Foundation Slice = PlanManager-spezifischer Import-Mapping-Cache in planmanager.db (Segmentwert → building_part_id beim Import). Allgemeines Stammdaten-Alias für alle Module wäre eigene bpm.db-Tabelle — erst bei zweitem Bedarf (YAGNI). → Richtungsentscheidung Herbert.

2. **plan_context_links:** ChatGPTs schärfster Punkt, berechtigt. Die Tabelle ist KEIN Cache — autorierte Cross-Modul-Daten, nicht aus Dateisystem rekonstruierbar. Spannung zum „disposable cache"-Modell. Aber: erst mit BPM-056 aktiv genutzt, im Slice nur angelegt. Empfehlung: jetzt wie ADR-058 in planmanager.db, als expliziten OFFENEN Punkt markieren (Heimat neu bewerten, wenn BPM-056-Sync kommt). Nicht jetzt lösen.

3. **ADR-053 Sync-Exclude:** Faktisch geprüft — ADR-053 nutzt per-DTO-Whitelist (DataClassification A/B/C), keine tabellenweise Exclude-Liste; planmanager.db ist außerhalb des Sync-Scopes. Option B wäre dort komplett neue Arbeit. ChatGPTs Sorge bestätigt.

4. **ATTACH-Ort:** Eigener Service (IPlanLookupService / PlanReferenceHealthService), nicht in PlanManagerDatabase. Im Slice nur Interface-Stub, kein ATTACH. Zustimmung.

5. **Delete-Policy:** Soft-Delete mit Warnbadge (konsistent ADR-050/ADR-056), kein harter Block. Guard-Implementierung post-V1. → Policy-Richtung Herbert.

## Konsens-Stand

Strong consensus, kein zwingender Grund für Runde 4. Nächster Schritt: Doc-Fix (DDL-Korrektur + Entscheidung dokumentieren als ADR-058-Addendum oder ADR-059).

## Offene Entscheidungspunkte für Herbert

1. Doc-Vehikel: ADR-058-Addendum vs. neues ADR-059 „Cross-DB Soft References".
2. building_part_aliases-Heimat: planmanager.db (Import-Mapping-Cache) vs. bpm.db (allgemeines Alias).
3. Delete-Guard-Policy: Soft-Delete + Warnbadge vs. harter Block vs. nur Invariante dokumentieren.
