# CGR-2026-05-12-segmenttyp-architektur — Segmenttyp-Verwaltung (BPM-108)

**Thema:** DB-basierte Segmenttyp-Verwaltung mit Gruppen, ID-Referenz, Soft-Delete, Rename-Stabilität
**Zeitraum:** 2026-05-12
**Ursprungs-Chat:** Teil 39 (BPM v0.28.42)
**Tracker:** [BPM-108](https://app.clickup.com/t/86c9rtvzm)
**Status:** Abgeschlossen

---

## Runden-Übersicht

### Runde 1 — Architektur-Review erste Skizze
- **Artefakte:** [r1/](./r1/)
- **Fokus:** Wechselwirkungen mit RecognitionProfile v3, DocumentTypeRecognizer, ProfileWizard, ProfileManager, Sync (ADR-053). Risiken bei Migration vom hardcoded `FieldType`-Enum zur DB-basierten Lookup-Tabelle.
- **Kernergebnis:** Zwei-Schichten-Modell (`fieldTypeId` + `SemanticRole`) statt Enum-Ersatz. Schema v4 + Frühphasen-Reset. `RecognitionRule` bleibt unverändert. `token_key` für Templates. ProfileHealth-Marker statt Hardfail. Schichten A/B/C (Domain → Profilformat v4 → Wizard/UI). User-Entscheidungen: Built-ins voll editierbar mit `user_modified_*`-Flags; Custom rein dekorativ ohne SemanticRole.

### Runde 2 — User-Entscheidungen validieren + offene Details
- **Artefakte:** [r2/](./r2/)
- **Fokus:** Validierung Built-in-Editierbarkeit + Custom dekorativ-only. Edge-Cases bei identityFields (2 Spatial-Segmente?). recognition_profiles-Tabellen-Vorbereitung. Profile-Lösch-Skript. `lastKnownLabel` ja/nein. Custom-Chip UI-Flow (Inline vs Modal). Schritt-5-Reopen mit Missing-IDs. Migration der 5 Domain-Konstanten-Stellen. Chip-Rendering bei deaktivierten Typen.
- **Kernergebnis:** `user_modified_group`-Flag ergänzt; `token_key` bei Built-ins unveränderlich; `semantic_role` read-only mit Warntext bei Built-ins; `PlanNumber == 1`-Validierung; `identityFields` nur tatsächlich verwendete Profilsegmente; `is_required` nur aus Rolle abgeleitet; 13 Refactor-Stellen als Akzeptanzkriterien; 3-Phasen-Plan A/B/C mit 3 ersten Commits; DevTool archiviert (nicht löscht); `lastKnownLabel` nicht in v4; Massenreparatur nicht in Schritt 5; Custom-Chip Inline-Popover.

### Runde 3 — Abschluss-Bestätigung
- **Artefakte:** [r3/](./r3/)
- **Fokus:** Finale Antworten zu Spatial-Rollen objekt/achse (Spatial), token_key unveränderlich, DevTool archivieren, PatternTemplateService deaktivieren. Mockup-Ergänzungen (Manager read-only Rollenanzeige + Inline-Popover „+ Eigenes"). Sign-off von ChatGPT.
- **Kernergebnis:** **Sign-off erreicht.** 4 zusätzliche Akzeptanzkriterien (#14 Immutable Keys, #15 Built-in-Rollen unveränderlich, #16 Strict Reset PatternTemplates, #17 Health-Gating vor Auto-Import). Token-Vorschau im Inline-Popover bestätigt. Implementierungsreihenfolge: Commit 1 Catalog Persistence → Commit 2 Profile v4 → Commit 3 Wizard-Refactor.

---

## Finale Architektur-Zusammenfassung

**Zwei-Schichten-Modell:**
- `fieldTypeId` (persistente Referenz: ULID für Custom, snake_case für Built-in)
- `SemanticRole` (kleine Enum: None, PlanNumber, PlanIndex, ProjectNumber, Date, Description, Spatial, Ignore)

**Spatial-Built-ins:** geschoss, haus, bauteil, bauabschnitt, stiege, zone, block, achse, objekt

**Editierbarkeit:**
- Built-ins: Name, Farbe, Gruppe, Sortierung, Aktiv-Status — alles editierbar mit `user_modified_*`-Flags
- Built-ins: `id`, `token_key`, `semantic_role`, `is_builtin` — immutable
- Custom: voll editierbar, `semantic_role` immer NULL, `id` und `token_key` nach Anlage immutable

**Frühphase = Reset:**
- Schema v4 strict, keine Migration
- DevTool archiviert Profile/Templates nach `_archiv/schema-reset-YYYYMMDD-HHMMSS/`
- `RecognitionRule` bleibt unverändert (BPM-082-kompatibel)
- `PatternTemplateService` lädt nur v4-Templates

**3-Phasen-Plan:** A Domain/Persistence → B Profilformat v4 → C Wizard/UI
**17 Akzeptanzkriterien** (siehe [r3/04-user-decisions.md](./r3/04-user-decisions.md))
