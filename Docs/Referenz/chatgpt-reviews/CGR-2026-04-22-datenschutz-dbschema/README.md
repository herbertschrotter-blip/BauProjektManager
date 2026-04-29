# CGR-2026-04-datenschutz-dbschema — DSGVO + DB-Schema + ID-System

**Thema:** DSGVO-Architektur-Review, Datenbankschema-Konsistenz, einheitliches ID-Schema, registry.json als Whitelist-Exportvertrag.

**Zeitraum:** April 2026
**Ursprungs-Chat:** "Skills für Kern-Dokumentation"
**Status:** r2–r3 dokumentiert, abgeschlossen

---

## Ausgangslage

Review der DSGVO-Architektur-Doku + DB-Schema-Konsistenz vor erstem Online-Modul (Wetter). Gewichtung Sofort vs. Post-V1.

---

## Runden-Übersicht

### Runde 2 — Datenschutz-Architektur + ID-Inkonsistenz
- **Artefakte:** on-demand aus Chat-History
- **Fokus:** Auto-Sync, RBAC, Anonymisierung, ID-Schema
- **Kernergebnisse:**
  - **Auto-Sync ist kein Opt-in-Verstoß** (zweistufiges Modell: erst Modul aktivieren, dann Auto-Call)
  - **RBAC für V1 nicht nötig** (kommt mit Phase 3 Server-Modus)
  - **Anonymisierung Klasse C:** Stufenmodell vorhanden (Kap. 7.3)
  - **ID-Inkonsistenz muss gelöst werden** — TEXT-IDs mit Präfix für alle Tabellen

### Runde 3 — Konkretisierung der Änderungen
- **Artefakte:** on-demand aus Chat-History
- **Kernergebnisse (15 konkrete Änderungen):**
  - Architektur-Doku: Registry.json-Whitelist-Verweis, Betriebsmodi A/B/C, Privacy Control Layer
  - CODING_STANDARDS: Datenschutz-Entscheidungen nie im ViewModel
  - DB-SCHEMA: seq vs. id Rollen, Audit-Log Negativliste, TEXT-IDs für alle Tabellen
  - BACKLOG: Datenschutz-Infrastruktur als "Should (PFLICHT vor erstem Online-Modul)"
  - **Neue ADR-037: Einheitliches ID-Schema TEXT mit Präfix**

---

## Resultierende ADRs

- ADR-035 — IExternalCommunicationService (einziger Ausgangspunkt für externe Calls)
- ADR-036 — IPrivacyPolicy via Lizenz (RelaxedPrivacyPolicy vs StrictPrivacyPolicy)
- ADR-037 — Einheitliches ID-Schema TEXT mit Präfix

## Offene Punkte

- ULID-Migration (ADR-039 v2) — pending nach PlanManager-Komplettfertigstellung
- external_call_log-Tabelle wird implementiert mit IExternalCommunicationService (vor erstem Online-Modul)
