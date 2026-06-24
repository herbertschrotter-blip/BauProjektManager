# Review Runde 2 — Claude-Analyse

## Gesamturteil
ChatGPTs Runde 2 ist **implementierungsreif**. Ich trage alles mit — es gibt keinen substanziellen Dissens mehr. Die offenen Punkte sind nur noch fünf kleine Konfigurations-Detailfragen, jeweils mit klarer Empfehlung. Aus meiner Sicht ist der Review damit **abschlussreif → ADR + Implementierung**.

## Volle Zustimmung (tragende Entscheidungen)
1. **`SubFolderEntry`-Erweiterung** (`CreatesDocumentType` + `DocumentTypeKey` + `DocumentTypeDisplayName` + `Ring2Source?` + `Categories`) mit Regel „Typ ⇔ `CreatesDocumentType==true`" — sauber, explizit, kein hardcodierter Seed mehr. Genau die Konsolidierung, die den Live-Bug behebt.
2. **`profile.TargetFolder` jetzt brechen** + gemeinsamer `DocumentTargetPathResolver` für Profil- UND Radial-Import. Das ist der wichtigste strukturelle Gewinn: nur noch EINE Zielpfad-Engine. `ResolvedDocumentTarget`-Record in `ImportDecision` ist die richtige Entkopplung.
3. **Slice-Schnitt 0–6** mit ImportExecutionService als isoliertem Hochrisiko-Slice und „kein Slice fasst zwei Risikozonen an" — pragmatisch, reviewbar, 346 Tests als Regressionsnetz.
4. **Explorer:** eigener `IFileLauncher`, getrackte Dateien im MVP gesperrt (Option b → Weg zu a), Cloud-Share out-of-scope. Konsistent mit der Move-/Journal-Strecke.
5. **Modell A** (kuratierter Index) + Reconcile nur der getrackten Teilmenge, Exists+Size-first, MD5-Relink nur als Vorschlag. Richtig und schlank.
6. **Detailfragen:** name=Singular / folder_name=präfixiert getrennt (6.1); `building_levels.folder_name` einführen, ohne Nummer (6.2); `ProjectPaths.Plans` belassen (6.3). Alles meine Sicht.

## Eigene Bewertung der 5 offenen Rückfragen (mit Empfehlung)
1. **Baustelleneinrichtung:** → erfassbarer Dokumenttyp mit `Ring2Source.None` (kostet nichts, kann später Kategorien bekommen). Empfehlung: ja, als Typ.
2. **Protokolle:** Es ist heute ein **eigener Hauptordner** („06 Protokolle"), NICHT unter Planunterlagen. Sauber wäre, es auch als Typ in seinem eigenen Root-Bereich zu führen — das verlangt aber, dass auch `FolderTemplateEntry` (Hauptordner) Typ-Metadaten tragen darf (ChatGPTs „nicht mehr minimal"-Variante). Meine Empfehlung: **Protokolle = eigener Root-Typ**, dafür die Typ-Metadaten auch auf `FolderTemplateEntry` heben (geringe Mehrarbeit, vermeidet den unschönen gleichnamigen Subfolder-Trick). Das berührt auch 6.3 (mehrere Planbereiche → `root_relative_path`).
3. **Kategorieordner-Präfix:** unpräfixiert (Kategorien sind klein, Sortierung über sort_order). Empfehlung: unpräfixiert, aber `folder_name` pro Kategorie speicherbar (Flexibilität bleibt).
4. **DocumentTypeKey explizit:** ja, explizit ins Template (stabiler Key ≠ Anzeigename ≠ Ordnername). Empfehlung: explizit (wie ChatGPT).
5. **Level-folder_name-Format:** ohne Nummer (`EG`/`OG1`). Empfehlung: ohne Nummer.

→ 1, 3, 4, 5 sind risikoarm; ich würde sie als ChatGPT-Empfehlung in den ADR übernehmen. **2 (Protokolle/Root-Typen)** ist die einzige mit echter Struktur-Konsequenz (Hauptordner als Typ-Quelle).

## Empfehlung
Review **abschließen** → ADR-060 (o.ä.) „Dateisystem-Ports + DB-als-Ordner-Wahrheit + Zielpfad-Resolver" schreiben, ClickUp-Tasks je Slice anlegen, dann Slice 0 implementieren. Eine Runde 3 ist nur nötig, wenn Herbert die Protokolle-/Root-Typen-Frage (2) noch mit ChatGPT vertiefen will.
