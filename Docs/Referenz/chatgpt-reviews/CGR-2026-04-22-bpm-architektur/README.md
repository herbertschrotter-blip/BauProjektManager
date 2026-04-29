# CGR-2026-04-bpm-architektur — PlanManager/Import-Pipeline-Architektur

**Thema:** BPM-Code-Architektur-Diskussion rund um PlanImport, SQLite als Source-of-Truth, Path-Resolution und Domain/Infrastructure-Aufteilung.

**Zeitraum:** April 2026
**Ursprungs-Chat:** "Architektur-Dokumentation analysieren"
**Status:** mindestens r2 dokumentiert, weitere Runden möglich

---

## Ausgangslage

Vor PlanManager-Implementierung: Architektur-Unklarheiten rund um drei konkurrierende autoritative Stores (SQLite, JSON, Filesystem), absolute vs. relative Pfade, Import-Pipeline-Struktur.

---

## Runden-Übersicht

### Runde 2 — Sofort-vs-Post-V1-Priorisierung
- **Artefakte:** on-demand aus Chat-History
- **Kernergebnisse (sofort):**
  - **SQLite-Wahrheits-Widerspruch auflösen** (drei autoritäre Stores zusammenführen)
  - **`ProjectPaths.Root` → relative Pfade** im Domain, Auflösung in Infrastructure
  - **`PlanImportFacade` als orchestrierender Kern** (statt UseCase-Interface)
  - **Import-Pipeline mit immutable Records** (ScannedFile → ParsedFile → …)
  - **Heartbeat als UX-Warnung** formulieren (kein Konsistenzgarant)
  - **ProjectStatus `Archived` ist Doku-Bug** (entfernen)

- **Kernergebnisse (Post-V1):**
  - Synchronisiertes Import-Ledger
  - `RevisionOrderingMode` vollständig (V1: einfacher lexikalischer Vergleich)
  - `IDomainEvent` (erst bei 3+ Modulen)
  - JSON-Revisionshülle (V1.1)
  - Ordner-Hierarchie-Migration

---

## Resultierende ADRs

- ADR-046 — Storage-Zonen (drei Zonen)
- ADR-047 — settings.json Single-File in .AppData/
- ADR-049 — Pfad-Resolution Option C (relative `folder_name` + `.bpm/manifest.json` Fallback-Scan)

## Offene Punkte

- Weitere Runden bei Bedarf während PlanManager-Entwicklung
- Post-V1-Punkte als BACKLOG-Einträge zu prüfen
