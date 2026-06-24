## Review Runde 3

Letzte Vertiefung, Kollege — danach schreiben wir den ADR. Herbert hat entschieden:
**Protokolle = eigener Root-Typ** (kein Verschieben unter Planunterlagen). Wir müssen also
„Hauptordner als Dokumenttyp" + mehrere Plan-Roots sauber modellieren. Plus vier geklärte
Detail-Entscheidungen, die in das Endmodell konsistent eingebaut werden müssen.
Schreibe die GESAMTE Antwort in den Canvas, Titel "Review Runde 3", schließe mit
✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen.

## Repo-Zugriff
- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch! Bei JEDEM Dateizugriff angeben.
- Relevant: `AppSettings.cs` (FolderTemplateEntry/SubFolderEntry/GetNumberedName), `ProjectDatabase.cs` (document_types-Schema, building_levels), `BuildingLevel.cs` (`Prefix`/`PrefixString`), `ProjectPaths.cs`.

## Geklärt (NICHT neu aufrollen — bitte konsistent einbauen)
- `SubFolderEntry` bekommt Typ-Metadaten (`CreatesDocumentType`, `DocumentTypeKey` explizit, `DocumentTypeDisplayName`, `Ring2Source?`, `Categories`). Hardcodierter Seed raus. DB nach Bootstrap führend.
- `profile.TargetFolder` wird gebrochen → gemeinsamer `DocumentTargetPathResolver`.
- **Detail 1:** `Baustelleneinrichtung` = Dokumenttyp mit `Ring2Source.None`.
- **Detail 2:** **Kategorieordner DÜRFEN nummeriert/präfixiert sein** (nicht nur unpräfixiert) — z.B. `01 Wände`, `02 Decken`. `document_type_categories.folder_name` speichert den realen (ggf. präfixierten) Ordner.
- **Detail 3:** `DocumentTypeKey` explizit im Template.
- **Detail 4 (wichtig, weicht von deiner R2-Empfehlung ab):** `building_levels.folder_name` MIT Vorzeichen-Präfix nach Herberts Praxis: nach unten negativ (`-01 KG`/`-01 UG1`, `-02 UG2`), `00 EG`, nach oben positiv (`01 OG1`, `02 OG2`). Das Modell hat bereits `BuildingLevel.Prefix` (int, EG=0, unten negativ, oben positiv) + `PrefixString` (`00`/`-01`/`01`). → `folder_name = "{PrefixString} {Name}"`. Geschosse sind projektspezifische Stammdaten (beim Anlegen erzeugt), NICHT aus dem Template.

## Vertiefung A — Hauptordner als Dokumenttyp + mehrere Plan-Roots
Heute können laut R2 nur `SubFolderEntry` Typen erzeugen. `Protokolle` ist aber ein
**Hauptordner** („06 Protokolle"), außerhalb „Planunterlagen". Bitte sauber modellieren:
1. Erweitere `FolderTemplateEntry` (Hauptordner) um dieselben optionalen Typ-Metadaten wie
   `SubFolderEntry` (`CreatesDocumentType`, `DocumentTypeKey`, `DocumentTypeDisplayName`,
   `Ring2Source?`, `Categories`). Wie sieht die Default-Definition für `Protokolle`
   (Ring2Source.Categories: Baubesprechung/Bautagesbericht/Sicherheit/Abnahme) aus?
2. **Plan-Root pro Typ:** Ein Typ unter „Planunterlagen" hat Root `01 Planunterlagen`, der
   Protokoll-Typ hat Root `06 Protokolle`. Schlag das Schema vor: `document_types.root_relative_path`
   (oder `root_key`)? Wie wird der Wert beim Seed gesetzt (Hauptordner-Position → `06 Protokolle`)?
   Wie nutzt der `DocumentTargetPathResolver` ihn (Zielpfad = `root_relative_path / folder_name / …`)?
   Generalisiert das sauber auf künftige Roots (Leica/Absteckpläne, DOKA)?
3. **Regel/Präzedenz:** Wenn sowohl Hauptordner ALS AUCH Unterordner Typen sein können —
   klare, eindeutige Regel: Kann ein Hauptordner gleichzeitig Container (mit Typ-Unterordnern,
   wie „Planunterlagen") UND selbst Typ sein, oder schließt sich das aus? Wie verhinderst du
   Doppeldeutigkeit beim Seed?
4. Brauchen wir `ProjectPaths.Plans` dann überhaupt noch, oder ersetzt `root_relative_path` je
   Typ den globalen Plan-Root? (R2 sagte „Plans belassen" — gilt das mit Multi-Root noch?)

## Vertiefung B — Seed/Resolver-Konsistenz mit den vier Detail-Entscheidungen
1. **Kategorien präfixiert:** Wie berechnet der Seed `document_type_categories.folder_name`,
   wenn Kategorien nummeriert sein dürfen (positionsbasiert wie Unterordner, z.B. `01 Wände`)?
   Steuert das ein `HasPrefix` je Kategorie, oder global je Typ?
2. **Level-folder_name:** Bestätige `folder_name = "{PrefixString} {Name}"` für `building_levels`,
   erzeugt beim Anlegen eines Geschosses (Settings/Radial-Schnellanlage), rename-stabil. Passt das
   zum `DocumentTargetPathResolver` (Ring 3 = `level.folder_name`)?
3. **Vollständiges Pfad-Beispiel** end-to-end, damit das Modell stimmt:
   - Polierplan (BuildingParts): `01 Planunterlagen / 01 Polierpläne / {part.folder_name} / -01 KG / datei.pdf`
   - Fertigteile (Categories, präfixiert): `01 Planunterlagen / 04 Fertigteilpläne / 01 Wände / datei.pdf`
   - Protokoll (Root-Typ, Categories): `06 Protokolle / Baubesprechung / datei.pdf` (oder präfixiert?)
   Stimmen diese, oder korrigierst du die Struktur?

## Ziel
Nach dieser Runde sollte das Datenmodell (Template-Metadaten + `document_types`-Schema inkl.
Root + Resolver) final genug für einen ADR + Slice-0-Implementierung sein. Wenn du noch eine
echte Lücke siehst, benenne sie klar.
