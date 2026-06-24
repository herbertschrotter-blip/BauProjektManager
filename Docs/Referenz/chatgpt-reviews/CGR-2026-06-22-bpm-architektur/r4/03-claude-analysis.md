# Review Runde 4 — Claude-Analyse

## Gesamturteil
**Sign-off von beiden Seiten.** ChatGPTs Runde 4 ist der saubere Abschluss: finale Validierung + Slice-0-Tiefe, keine konzeptionelle Lücke mehr. Ich trage alles mit. Der Review ist **abschlussreif → ADR-060 + Implementierung**. Es bleibt genau **eine** offene ADR-Formulierungsfrage (Pflichtdialog vs. Default-Regel bei „+ Neu…").

## Volle Zustimmung (besonders wertvoll)
1. **Resolver-Fail-Fast + IDs vor Namen** — die wichtigste Invariante: kein halber Zielpfad, kein Fuzzy im Resolver. Genau das verhindert die Wiederkehr des Live-Bugs in neuer Form.
2. **Atomicity-Präzisierung:** atomar ist nur der finale Rename Temp→final im Zielordner, nicht der Gesamttransfer — Cross-Root-Move (Eingang → 06 Protokolle) ist unkritisch (gleiches Volume). Sauber begründet.
3. **Slice-Reihenfolge 1–7 (additiv zuerst, TargetFolder zuletzt entfernen)** — hält jeden Zwischenstand baubar und die 346 Tests kontrolliert. Sehr gute Migrationsmechanik ohne Migration.
4. **Frühphasen-Reset als exakte Liste** (bpm.db + planmanager.db + .bpm/profiles + ggf. settings.json) — kritisch, weil ein Teil-Reset sonst Geister-IDs hinterlässt.
5. **Normalisierung in Creation-/Seed-Services, nicht in der Low-Level-DB-Methode** — richtige Schichtung; `DocumentTypeCreationService` ist die saubere Heimat für die „+ Neu…"-Key/Folder-Erzeugung.
6. **R5-Warnung (`BuildingLevel.FolderName` braucht Model + Load/Save + Schnellanlage)** — genau die Stelle, die sonst still zu leerem Ring 3 führt.

## Konsequenz für unseren bestehenden Code
- **Slice 3a („+ Neu…" im Ring, 346 Tests grün, uncommitted)** bleibt in der *Geste* gültig, aber die **Typ-Erzeugung** muss aufs neue Modell gehoben werden: `root_relative_path` + `key` + der kleine Pflichtdialog + `DocumentTypeCreationService` statt direktem `InsertDocumentType(... Ring2Source.None ...)`. Ich würde Slice 3a daher **nicht separat committen**, sondern in den Slice-0-Umbau einfließen lassen (sonst committen wir Code, der sofort wieder umgebaut wird). Das ist meine klare Empfehlung — aber deine Entscheidung.
- Die `InsertBuildingPart`/`InsertBuildingLevel`-APIs aus Slice 3a passen ins Bild; `InsertBuildingLevel` muss nur zusätzlich `folder_name = "{PrefixString} {Name}"` setzen.

## Die eine offene ADR-Frage
Pflichtdialog vs. Default-Regel bei „+ Neu…" Dokumenttyp. **Meine Empfehlung = kleiner MVP-Pflichtdialog** (Name + Ablagebereich-Dropdown + Unterteilung + editierbarer Ordnername). Begründung: Genau die unsichtbare Default-Anlage hat den ursprünglichen Drift erzeugt. Ein minimaler Pflichtdialog kostet wenig und macht jeden neuen Typ erklärbar/korrekt verortet. Deckt sich mit ChatGPT.

## Empfehlung
Review **abschließen** (Sign-off beidseitig). Nächste Schritte: ADR-060 schreiben (Ports + DB-als-Ordner-Wahrheit + root_relative_path + Resolver + Slice 0.1–0.6 + Invarianten + Post-V1-Grenze), CGR-Serie auf „Abgeschlossen" setzen, ClickUp-Tasks je Slice anlegen. Slice 3a in Slice 0 aufgehen lassen.
