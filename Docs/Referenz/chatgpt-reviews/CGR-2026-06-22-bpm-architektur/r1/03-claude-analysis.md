# Review Runde 1 — Claude-Analyse

## Gesamturteil
ChatGPTs Antwort ist solide, projektkonform und deckt sich zu ~90 % mit meiner Sicht. Kein Gegenwind bei den großen Linien. Es ist umsetzbar, ohne etwas „Eigenes" zu erfinden — alles etablierte Muster (Ports & Adapters, Master-Data-in-DB, staging-temp + atomic rename, idempotente Recovery).

## Volle Zustimmung
1. **Drei schmale Ports** (`IFileSystemReader` / `IFileSystemWriter` / `IPathService`) statt eines `IFileStorage`-God-Interface — exakt richtig, vermeidet Coupling und hält den Port fachlogik-frei.
2. **Eigenes Interface statt System.IO.Abstractions** — passt zur Projektregel (keine neuen Libs) und wir brauchen BPM-Verhalten (Logging, DSGVO-Pfadmaskierung, Same-Volume, Cloud-Vorsicht). In-Memory-Fake + Temp-Integrationstests ist die richtige Test-Pyramide.
3. **DB ist die einzige Wahrheit, FolderTemplate nur Bootstrap.** Das ist die eigentliche Lösung des Live-Bugs: `document_types.folder_name` MUSS den realen, präfixierten Ordner speichern (`01 Polierpläne`), `name` bleibt Anzeige. Der Drift-Auslöser ist exakt der `folderName: null` im Seed → Normalizer erfindet „Polierplan".
4. **`profile.TargetFolder` als dritte Wahrheit ist gefährlich** — Auflösung über `DocumentTypeId → DB.folder_name` ist sauberer. Wichtiger Punkt, den ich unterstütze.
5. **Journal + temp-im-Zielordner + atomic rename + idempotente Recovery** — angemessen für Single-User-Desktop auf Cloud-Sync, ohne Overengineering. Die „NICHT bauen"-Liste (verteilte Locks, Watcher-Engine, 2PC) ist genau richtig.
6. **High-Level-Services bleiben**, werden nur entkoppelt. `IPlanTargetPathResolver` (FS-frei) als Pfad-Builder ist eine schöne Trennung.

## Eigene Ergänzungen / leichte Nuancen
- **Seed-Mapping:** ChatGPTs `MapTemplateFolderToDocumentType`-`switch` koppelt Template-Ordnernamen wieder hart an Typen im Code. Sauberer wäre, `ring2_source` (+ optional `name`) direkt in die `SubFolderEntry` der `FolderTemplate` aufzunehmen — dann beschreibt die Vorlage den Typ vollständig und der Seed braucht keinen hardcodierten Switch. (Mehr Aufwand, aber zukunftssicher bei custom Vorlagen.)
- **Bestehende Fehl-Ordner:** Frühphase → `bpm.db` löschen **und** die im Test fälschlich erzeugten Ordner (`Polierplan`, `Statikpläne`, nested `01 Planunterlagen`) im Testprojekt aufräumen, sonst bleibt Drift sichtbar.
- **building_levels.folder_name (Rückfrage 2):** konsistent mit der folder_name-Einmal-Regel für Typen/Bauteile — ich würde es mitnehmen (billig, verhindert späteren Sonderfall), aber es ist defensiv und könnte auch verschoben werden.
- **Scope (Rückfrage 5):** Ich empfehle, den Port + den Umbau zuerst auf den **Plan-Pfad** (PlanManager + ProjectFolderService + Seed) zu fokussieren — dort sitzt der Bug. Settings/Views von System.IO zu befreien ist richtig, aber als eigener, späterer Aufräum-Schritt (sonst wird der erste PR zu groß).

## Was das konkret für BPM-111.05 bedeutet
- **Slice 3a** („+ Neu…", 346 Tests grün, uncommitted) bleibt gültig — die Insert-APIs (`InsertBuildingPart/Level`) passen ins Bild. Nur die `folder_name`-Erzeugung (Seed + „+ Neu…"-Typen) muss auf die neue Regel (echter präfixierter Ordnername) gehoben werden.
- Resultat dieser Runde sollte ein **ADR** sein (Datei-Port + DB-als-Wahrheit + Umbauweg), plus konkrete ClickUp-Tasks.

## Offene Entscheidungen → an Herbert (Rückfragen 1–5)
Siehe `04-user-decisions.md`.
