# Review Runde 2 — User-Entscheidungen

## Meta
- **Nächster Schritt:** → **Runde 3** zur Protokolle-/Root-Typen-Frage, danach ADR.
- **Protokolle:** → **eigener Root-Typ** (Hauptordner „06 Protokolle" bleibt eigener Bereich und wird als Dokumenttyp geführt). Typ-Metadaten auch auf `FolderTemplateEntry` (Hauptordner) heben; öffnet Weg für mehrere Planbereiche (`root_relative_path`). In Runde 3 sauber ausarbeiten.

## Detailfragen (einzeln entschieden)
1. **Baustelleneinrichtung** → **Dokumenttyp** mit `Ring2Source.None` (kann später Kategorien bekommen).
2. **Kategorieordner-Präfix** → **Nummerierung erlaubt** (Kategorien dürfen Präfix tragen, nicht nur unpräfixiert). `folder_name` pro Kategorie speicherbar.
3. **DocumentTypeKey** → **explizit im Template** (stabiler fachlicher Key, ≠ Anzeigename/Ordnername).
4. **building_levels.folder_name** → **Vorzeichen-Präfix nach Herberts Praxis-Konvention**:
   - nach unten negativ: `-01 KG` / `-01 UG1`, `-02 UG2`, …
   - `00 EG` (EG immer 00)
   - nach oben positiv: `01 OG1`, `02 OG2`, …
   - Praktisch: `folder_name = "{BuildingLevel.PrefixString} {Name}"` (PrefixString liefert `00`/`-01`/`01`). Weicht bewusst von ChatGPTs „ohne Nummer"-Empfehlung ab.

## Konsens Runde 2 (bestätigt, implementierungsreif)
- `SubFolderEntry` + (jetzt auch) `FolderTemplateEntry` tragen Typ-Metadaten (`CreatesDocumentType`, `DocumentTypeKey`, `DocumentTypeDisplayName`, `Ring2Source?`, `Categories`). Hardcodierter `_builtins`-Seed raus.
- `profile.TargetFolder` jetzt brechen → gemeinsamer `DocumentTargetPathResolver` (Profil + Radial).
- Slices 0–6 (Ports → Scanner → Pfad → Import → DB-Pfade → Settings/Views → Explorer).
- Explorer: eigener `IFileLauncher`; getrackte Dateien MVP gesperrt; Cloud-Share out-of-scope.
- Modell A (kuratierter Index) + Reconcile nur getrackte Teilmenge, MD5-Relink nur Vorschlag.
- name=Singular / folder_name=präfixiert getrennt; `building_levels.folder_name` einführen; `ProjectPaths.Plans` belassen.

→ Runde 3 in [../r3/01-claude-prompt.md](../r3/01-claude-prompt.md).
