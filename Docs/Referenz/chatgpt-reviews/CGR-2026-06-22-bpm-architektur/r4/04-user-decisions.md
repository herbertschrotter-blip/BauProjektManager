# Review Runde 4 — User-Entscheidungen (Serie-Abschluss)

## Entschieden
1. **„+ Neu…"-Schnellanlage** → **kleiner MVP-Pflichtdialog** (Name + Ablagebereich-Dropdown + Unterteilung [Bauteil/Geschoss | Kategorien | Keine] + editierbarer Ordnername). Verhindert unsichtbare Defaults = die Drift-Ursache.
2. **Slice 3a** (uncommitted, 346 Tests grün) → **in Slice 0 aufgehen lassen** (nicht separat committen; Typ-Erzeugung wird ohnehin aufs neue Modell gehoben).
3. **ADR-Aufteilung** → **ZWEI getrennte ADRs:**
   - **ADR-060 = Dateisystem-Ports** (Herberts Ausgangsfrage: vereinheitlichtes FS-Interface für alle Module — `IFileSystemReader`/`IFileSystemWriter`/`IPathService` + `LocalFileSystem`, kein direktes System.IO; + `IFileLauncher`/`IShareService` für Explorer).
   - **ADR-061 = Ordner-Wahrheit + Resolver** (Bug-Konsolidierung: DB führend, FolderTemplate nur Bootstrap, `document_types.key`+`root_relative_path`, `DocumentTargetPathResolver`, Multi-Root, `building_levels.folder_name`, `profile.TargetFolder` brechen).
4. **Runde 5** → **nein**, Review abschließen.

## Wichtige Klarstellung (Herbert-Einwand R4)
Der Review startete mit Herberts Kernfrage „brauchen wir ein vereinheitlichtes Dateisystem-Interface für alle Module?". Antwort: **Ja** (Thema A). Das Ring-/Ordner-Thema (B) kam Bug-getrieben über den Live-Test dazu und hat A in R2–R4 überlagert. Beide sind real und signiert, werden aber als **getrennte ADRs** geführt.

## Serie-Ergebnis
Beidseitiger Sign-off nach 4 Runden. Resultiert in ADR-060 + ADR-061 + ClickUp-Tasks je Slice. Slice 0.1–0.6 als Implementierungsplan steht. Post-V1-Grenze: eine Ring-2-Strategie pro Dokumenttyp.
