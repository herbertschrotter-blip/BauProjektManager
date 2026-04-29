# CGR-2026-04-docs-refactor — Dokumentations-System-Refactor

**Thema:** Strukturierung der BPM-Projektdokumentation — Frontmatter, INDEX-Router, AI-Quickload-Schema, DOC-STANDARD.

**Zeitraum:** April 2026
**Ursprungs-Chat:** "Docs und Skill refactoring (Teil 1)"
**Status:** 3 Runden abgeschlossen, Umsetzung teilweise erfolgt (DOC-STANDARD + Frontmatter in Kern-Docs)

---

## Ausgangslage

BPM-Docs hatten heterogene Struktur ohne standardisierte Metadaten. Ziel: KI-effizientes Doc-System das ohne komplettes Re-Read navigierbar ist.

---

## Runden-Übersicht

### Runde 1 — Governance/Briefs/Frontmatter-Vorschlag
- **Artefakte:** on-demand aus Chat-History
- **Fokus:** ChatGPT schlägt 4-Ebenen-Modell vor (Governance → Briefs → Authority → Reference)
- **Kernpunkte:** Claude widerspricht bei separatem DOCS-CATALOG, massivem Ordnerumbau und Brief-Dateien — zu viel Pflegeaufwand für Ein-Mann-Projekt

### Runde 2 — Skills als Enforcement-Layer
- **Artefakte:** on-demand aus Chat-History
- **Fokus:** Claude erklärt dass Skills feste Formate **erzwingen** können (kein "weiches Auto-Sync")
- **Kernergebnis:** ChatGPT übernimmt, da ihm unser Skill-System vorher unklar war

### Runde 3 — Finale 8-Schritt-Reihenfolge + Pilotgruppe
- **Artefakte:** on-demand aus Chat-History
- **Kernergebnisse:**
  - **Evolutionärer Weg** (kein Ordner-Umbau jetzt)
  - **Frontmatter zuerst** in 5–8 Pilot-Docs
  - **AI-Quickload am Anfang jeder Doc** statt separate Brief-Dateien
  - **INDEX früher im Ablauf** als Quickload-Schema (Routing bestimmt Priorität)
  - **DOC-STANDARD.md als Norm-Dokument** im Repo
  - **Cross-Checks in audit-Skill** (related_docs existieren, INDEX nur gültige Docs)
  - **Generierter Katalog** statt manueller DOCS-CATALOG

---

## Finale Pilotgruppe (7 Docs)

1. `Docs/Kern/BauProjektManager_Architektur.md`
2. `Docs/Kern/DB-SCHEMA.md`
3. `Docs/Kern/DSVGO-Architektur.md`
4. `Docs/Module/PlanManager.md`
5. `Docs/Referenz/UI_UX_Guidelines.md`
6. `Docs/Kern/BACKLOG.md`
7. `Docs/Konzepte/MultiUserKonzept.md` (in Runde 3 hinzugenommen statt ADR.md)

## Resultierende Artefakte

- `Docs/Referenz/DOC-STANDARD.md` — Norm-Dokument im BPM-Repo
- Frontmatter + AI-Quickload in Pilot-Docs (schrittweise)
- `doc-pflege`-Skill enforcement (im Skill-Repo)

## Offene Punkte

- Vollständige Frontmatter-Migration über alle 17+ Konzept-Docs — on-demand, modulweise bei Modul-Aktivierung
