# CGR-2026-06-22-bpm-architektur — Datei-/Ordner-Verwaltungs-Abstraktion

**Thema:** BPM-Code-Architektur — dedizierte Schnittstelle für Datei- und Ordnerverwaltung (lesen/schreiben/verschieben/anlegen), Schichtgrenzen, Konsolidierung der zwei Ordner-Wahrheiten (`document_types` vs `FolderTemplate`).
**Zeitraum:** 2026-06-22 (Teil 44)
**Ursprungs-Chat:** Teil 44 (v0.28.81), Live-Test BPM-111.05
**Status:** ✅ Abgeschlossen (4 Runden, beidseitiger Sign-off)

---

## Auslöser

Live-Test der Radial-Plan-Erfassung (BPM-111.05): Der Import legt einen **neuen** Ordner
`Polierplan` an, statt den vorhandenen Vorlagenordner `01 Polierpläne` zu treffen. Ursache
sind **zwei getrennte Ordner-Wahrheiten**, die sich nicht kennen:

1. `AppSettings.FolderTemplate` (Domain) → `ProjectFolderService` erzeugt die physischen
   Plan-Ordner mit positionsbasierter Nummerierung (`Polierpläne` → `01 Polierpläne`).
2. `document_types` (bpm.db, ADR-059-Addendum) → Ring 1 der Radial-Erfassung;
   `folder_name` = normalisierter Typname (`Polierplan`).

Herberts Leitfrage: Sollte es **eine** dedizierte Datei-/Ordner-Schnittstelle geben, auf die
alle Module zugreifen? Wie wird das professionell gehandhabt (keine Eigenerfindung)?

---

## Runden-Übersicht

### Runde 1 — Bestandsaufnahme + Zielbild
- **Artefakte:** [r1/](./r1/)
- **Fokus:** Filesystem-Port/Adapter, Testbarkeit, Transaktionalität über Cloud-Sync,
  Single-Source-of-Truth für Plan-Ordnerstruktur, Schichtgrenzen.
- **Kernergebnis (Konsens):** 3 schmale FS-Ports + Adapter (kein God-Interface), eigenes Interface statt System.IO.Abstractions, **DB = einzige Ordner-Wahrheit** (FolderTemplate nur Bootstrap, `folder_name` = realer präfixierter Ordner), Journal+temp+atomic-rename+Recovery.

### Runde 2 — Schema + Import-Break + Scope + Explorer + DB-Scope
- **Artefakte:** [r2/](./r2/)
- **Kernergebnis:** `SubFolderEntry` trägt Typ-Metadaten (Typ ⇔ `CreatesDocumentType`); `profile.TargetFolder` brechen → gemeinsamer `DocumentTargetPathResolver`; Slices 0–6; Explorer mit `IFileLauncher`, getrackte Dateien gesperrt, Cloud-Share out-of-scope; **Modell A** (kuratierter Index) + Reconcile nur getrackte Teilmenge.

### Runde 3 — Protokolle als Root-Typ + Multi-Root + Detail-Konsistenz
- **Artefakte:** [r3/](./r3/)
- **Kernergebnis:** `document_types.key` + `root_relative_path` (Multi-Root, löst Protokolle); `FolderTemplateEntry`+`SubFolderEntry` gleiche Typ-Metadaten; Kategorie-`HasPrefix` je Kategorie; `building_levels.folder_name="{PrefixString} {Name}"`; Resolver nur aus DB; Post-V1-Grenze: eine Ring-2-Strategie pro Typ.

### Runde 4 — Abschluss-Validierung + Slice-0-Tiefe (Sign-off)
- **Artefakte:** [r4/](./r4/)
- **Kernergebnis:** Beidseitiger **Sign-off**. „+ Neu…" = kleiner MVP-Pflichtdialog; Resolver Fail-Fast/IDs-vor-Namen; Atomicity = finaler Rename im Zielordner (Cross-Root-Move unkritisch); Slice-Reihenfolge 1–7 (additiv zuerst, TargetFolder zuletzt); exakte Reset-Liste; Normalisierung in Creation-Services.

---

## Serie-Ergebnis

Zwei getrennte, voneinander unabhängig umsetzbare Ergebnisse:

- **ADR-060 — Dateisystem-Ports** (Herberts Ausgangsfrage): vereinheitlichtes FS-Interface für alle Module — `IFileSystemReader`/`IFileSystemWriter`/`IPathService` + Adapter `LocalFileSystem`, alle Module via DI, kein direktes `System.IO` (heute ~29 Dateien); + `IFileLauncher`/`IShareService` (Explorer); In-Memory-Fake + Temp-Integrationstests.
- **ADR-061 — Ordner-Wahrheit + Resolver** (Bug-Konsolidierung): DB führend, FolderTemplate nur Bootstrap; `document_types.key`+`root_relative_path`+`folder_name`; `DocumentTargetPathResolver` (Fail-Fast, IDs vor Namen); Multi-Root; `building_levels.folder_name`; `profile.TargetFolder` gebrochen; Journal+temp+atomic-rename+Recovery; Modell A (kuratierter Index, Reconcile getrackte Teilmenge).

**Implementierung:** Slice 0.1–0.6. Slice 3a („+ Neu…" im Ring) geht in Slice 0 auf. **Status:** Code-Umsetzung offen.
