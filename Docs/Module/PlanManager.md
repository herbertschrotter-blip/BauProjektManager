# BauProjektManager — PlanManager (Modul-Dokumentation)

**Version:** 2.0
**Datum:** 09.04.2026
**Status:** In Entwicklung (V1 Kernfeature)
**Autor:** Herbert + Claude
**Review:** ChatGPT Cross-Review (3 Runden, 09.04.2026)

**Verwandte Dokumente:**
- [BauProjektManager_Architektur.md](../Kern/BauProjektManager_Architektur.md) — Kap. 4–8
- [BACKLOG.md](../Kern/BACKLOG.md) — Features #18–#33
- [DB-SCHEMA.md](../Kern/DB-SCHEMA.md) — planmanager.db Tabellen
- [ADR.md](../Referenz/ADR.md) — ADR-007 bis ADR-010, ADR-022, ADR-045
- [UI_UX_Guidelines.md](../Referenz/UI_UX_Guidelines.md) — Design-Token, Screen States
- [WPF_UI_Architecture.md](../Referenz/WPF_UI_Architecture.md) — ViewState, MVVM
- [DSVGO-Architektur.md](../Kern/DSVGO-Architektur.md) — Klasse A, kein ext. Kontakt
- [Moduleplanheader.md](../Konzepte/Moduleplanheader.md) — Post-V1: PlanHeader

---

## 1. Zweck

Der PlanManager ist das **Kernfeature von BPM**. Er sortiert Dokumente (Pläne, Protokolle,
Berichte etc.) aus dem `_Eingang/`-Ordner automatisch in die richtige Ordnerstruktur — nach
Dokumenttyp, Geschoss, Bauteil etc. Mit Index-Versionierung, Undo-Journal, anlernbaren
Profilen und manuellem Sortier-Modus für Scans.

**Nicht nur Pläne:** Das Profil-System ist nicht auf Plantypen beschränkt. Jedes Dokument
mit erkennbarem Namensmuster kann angelernt werden: Polierpläne, Schalungspläne,
Bauprotokolle, Prüfberichte, Baubesprechungen etc. Der Zielordner ist frei wählbar
(01 Planunterlagen/, 04 Protokolle/, 03 Dokumente/ etc.).

**MVP-Frage:** *„Brauche ich das um Dokumente zu sortieren?"* — Wenn nein → nicht in V1.

---

## 2. Datenschutz-Einordnung (DSGVO)

| Prüfpunkt | Ergebnis |
|-----------|----------|
| Externe Kommunikation? | Nein (V1) |
| Datenklasse | **Klasse A** — Dateinamen, MD5-Hashes, Ordnerstrukturen, Indizes |
| Personenbezogene Daten? | Nein — nur technische Dokument-Metadaten |
| DSFA nötig? | Nein |
| IExternalCommunicationService nötig? | Nein (erst bei PlanHeader mit KI-API, Post-V1) |

**Post-V1:** Wenn `IndexSource.PlanHeader` mit KI-API kommt → Klasse C. Dann DSVGO-Architektur Kap. 7+12.

---

## 3. Konzeptübersicht

### 3.1 Grundprinzip

```
1. Projekt wählen
2. Dokumenttyp-Profil anlernen (einmalig pro Typ)
3. Dateien in _Eingang/ werfen
4. Import starten → Analyse → Vorschau → Bestätigen
5. Dateien werden automatisch einsortiert
```

Alternativ: Manueller Sortier-Modus für Scans und nicht erkannte Dateien.

### 3.2 Dokument-Dateien (ADR-007)

Ein Dokument (Revision) besteht aus **1 bis n Dateien**. Dateien werden über den gemeinsamen
Dateinamen-Stamm (ohne Extension) zusammengeführt. Fehlende PDF oder DWG ist kein Fehler.

**Wichtig:** PDF und DWG sind grundsätzlich eigenständige Dateien in der DB. Die Gruppierung
zu einer Revision ist flexibel: Default = gleicher Stamm → Auto-Link. Aber eine DWG kann
auch mehreren Revisionen zugeordnet sein (Sammel-DWG) oder eigenständig bleiben (standalone).

### 3.3 Zwei Sortier-Modi

| Modus | Wann | Was passiert |
|-------|------|-------------|
| **Automatisch** | Datei matcht ein Profil | Segmente geparst → Zielordner berechnet → Vorschau |
| **Manuell** | Kein Profil matcht (Scans, Fotos) | User wählt Typ, gibt Felder ein, optional umbenennen |

### 3.4 Speicherorte

| Datei | Ort | Synct? |
|-------|-----|--------|
| `profiles.json` | Cloud `.AppData/Projects/<P>/` | Ja |
| `pattern-templates.json` | Cloud `.AppData/` | Ja |
| `_plan_index.json` | Cloud Projektordner (hidden) | Ja |
| `planmanager.db` | Lokal `%LocalAppData%/Projects/<P>/` | Nein |
| Dokumente (PDF/DWG) | Cloud Projektordner | Ja |
| Backups (pre-import) | Lokal `%LocalAppData%/Backups/` | Nein |

---

## 4. IndexSource — Dreistufiges Modell (ADR-045)

Pro Projekt und Dokumenttyp wird im RecognitionProfile gespeichert wie der Index erkannt wird.

### 4.1 Drei Modi

| Modus | Verhalten | Archivierung |
|-------|-----------|-------------|
| `FileName` | Index aus Dateinamen-Segment | Alte Indizes → `_Archiv/` nach Buchstabe |
| `None` | Kein Index. MD5-Hash-Vergleich | Bei geändertem Hash → `_Archiv/` mit Timestamp |
| `PlanHeader` | Index aus PDF-Plankopf (Post-V1) | Wie FileName — Index bekannt |

**V1-Scope:** `FileName` und `None`. `PlanHeader` als Enum-Wert vorhanden, Implementierung Post-V1.

### 4.2 planIndex ist optional

Auch bei `FileName` kann der erste Plan ohne Index kommen (Erstausgabe). Profil-Feld:

```json
"indexSource": "FileName",
"indexMode": "optional",
"indexPattern": "^[A-Z0-9]{1,3}$"
```

- Erstausgabe ohne Index → normal einsortieren
- Wenn später Version MIT Index kommt → indexlose Erstausgabe ins `_Archiv/`
- Sortierregel: über `indexComparison` Policy im Profil (nicht hardcoded)

### 4.3 IndexComparison

Pro Profil konfigurierbar weil Index-Formate variieren (A/B/C, 00/01/02, A1/A2):

```json
"indexComparison": {
  "mode": "alphabetic",
  "caseInsensitive": true
}
```

V1: `alphabetic` (Default). Post-V1 erweiterbar auf `numeric`, `natural`, `custom`.

### 4.4 Nachlern-Mechanismus

Wenn ein Dokument bisher keinen Index hatte und plötzlich einer auftaucht:

1. Import 1: `S-103_TG Wände.pdf` → Plannr 103 unbekannt → Erstversion
2. Import 2: `S-103-B_TG Wände.pdf` → Plannr 103 bekannt, jetzt mit Index „B"

→ Nachlern-Dialog in Import-Vorschau: „Plan 103 hat jetzt einen Index — Profil anpassen?"
→ Bei Bestätigung: Profil erweitert, alte indexlose Datei ins `_Archiv/`

**UI-Warnung:** „Diese Entscheidung ändert das Profil dauerhaft." Default konservativ.

---

## 5. Entscheidungsmatrix (Import-Versionierung)

### 5.1 Fachliche Dokument-Identität

Nicht nur `plan_number` — die ist nur innerhalb eines Dokumenttyps eindeutig. Stabile
Identität über `document_key` aus `identityFields` im Profil:

```json
"identityFields": ["documentType", "planNumber", "haus"]
```

`document_key` wird deterministisch über einen zentralen Builder gebildet:
```csharp
string BuildDocumentKey(Profile profile, ParsedFile parsed)
```

### 5.2 Status-Typen (9 Stück)

| Status | Intern | UI-Text (deutsch) | Beschreibung |
|--------|--------|-------------------|-------------|
| `NEW` | new | Neu | Plannr nicht in DB → Erstversion |
| `SKIP_IDENTICAL` | skip | Schon vorhanden | Gleicher Name + gleicher MD5 → identisch |
| `UPDATE_NEWER_INDEX` | indexUpdate | Neue Revision | Neuer Index (C→D) → alte ins Archiv |
| `CHANGED_NO_INDEX` | changed | Geändert | IndexSource=None + anderer MD5 |
| `CHANGED_SAME_INDEX` | changedSameIdx | Geändert (gleicher Index) | ⚠ Warnung! Gleicher Index aber anderer MD5 |
| `OLDER_REVISION` | olderRevision | Ältere Revision | Eingang bringt B, aber D liegt schon → Warnung |
| `LEARN_INDEX` | learnIndex | Index erkannt | Bisher kein Index, jetzt einer → Nachanlernen |
| `UNKNOWN` | unknown | Unklar | Kein Profil erkannt → manuell zuweisen |
| `CONFLICT` | conflict | Mehrere Profile | Mehrere Profile matchen → User wählt |

### 5.3 Entscheidungsmatrix

| document_key in DB? | Dateiname identisch? | MD5 identisch? | Index-Situation | Status |
|---|---|---|---|---|
| Nein | — | — | egal | **NEW** |
| Ja | Ja | Ja | egal | **SKIP_IDENTICAL** |
| Ja | — | — | neuer Index höher | **UPDATE_NEWER_INDEX** |
| Ja | — | — | neuer Index niedriger | **OLDER_REVISION** ⚠ |
| Ja | Ja | Nein | IndexSource=None | **CHANGED_NO_INDEX** |
| Ja | — | Nein | gleicher Index (FileName) | **CHANGED_SAME_INDEX** ⚠ |
| Ja | Nein | — | vorher kein Index, jetzt schon | **LEARN_INDEX** |
| — | — | — | kein Profil erkannt | **UNKNOWN** |
| — | — | — | mehrere Profile matchen | **CONFLICT** |

**Hinweise:**
- Entscheidung läuft auf **Revisionsebene** für erkannte Dokumente, auf **Dateiebene** für standalone.
- `CHANGED_SAME_INDEX` und `OLDER_REVISION` sind Warnfälle — nicht automatisch archivieren, User entscheidet.
- Bei SKIP: Datei aus Eingang entfernen nach Bestätigung in Vorschau.
- `_Archiv/` wird automatisch erstellt falls nicht vorhanden.

---

## 6. Workflow — 5 Phasen

### Phase 0 — Profil anlernen (einmalig pro Dokumenttyp)

4-Schritt-Wizard (auch erreichbar über „✎ Profil" im Projektdetail):

1. **Datei auswaehlen** → Datei aus Eingang klicken oder Name eingeben, Trennzeichen, Parsen → Segmente als Vorschau
2. **Segmente zuweisen** → Feldtypen per Dropdown zuweisen (PlanNumber Pflicht)
3. **IndexSource** → FileName / None / PlanHeader(Post-V1), indexMode, indexComparison
4. **Zielordner + Ordner-Hierarchie** → Hauptordner + Unterebenen (Geschoss, Haus etc.)
5. **Erkennung** → Klickbare Segment-Bloecke (Toggle), auto-Muster + auto-Methode (prefix/contains), Live-Test, Prioritaet

Ergebnis: RecognitionProfile in `profiles.json` + PatternTemplate in `pattern-templates.json`.

### Phase 1 — Dateien landen im Eingang

- Quellen: E-Mail, Portal-Download, USB, Scanner, Explorer
- Beim App-Start: Alle `_Eingang/`-Ordner prüfen → Badge in Sidebar

### Phase 2 — Import-Analyse (automatisch)

| Schritt | Was passiert |
|---------|-------------|
| 2a. Scan | `_Eingang/` durchsuchen, MD5-Hash + file_size berechnen |
| 2b. Parse | Dateinamen in Segmente splitten laut Profil |
| 2c. Classify | Dokumenttyp erkennen, document_key bilden |
| 2d. Versionierung | Entscheidungsmatrix (Kap. 5.3) pro Datei/Revision |
| 2e. Plan | Zielpfad berechnen, Dateien gruppieren (Auto-Link) |

### Phase 3 — Import-Vorschau (User entscheidet)

Tabelle mit Status, Dateiname, Dokumenttyp, Nr, Index, Zielordner.
User kann: Rechtsklick korrigieren, UNKNOWN → manuell zuweisen, LEARN_INDEX bestätigen.
Import erst nach Bestätigung: Button „Import ausführen".

### Phase 4 — Ausführung

| Schritt | Was passiert |
|---------|-------------|
| 4a. Backup | planmanager.db + profiles.json als .bak |
| 4b. Journal | Alle Aktionen VORHER ins Undo-Journal (Status: pending) |
| 4c. Execute | Pro Aktion: _Archiv/ erstellen → verschieben → ggf. umbenennen → completed |
| 4d. Finalize | Journal completed. _plan_index.json aktualisieren. Zusammenfassung. |

**Journal = Execution-Log:** Speichert die tatsächlich ausgeführten Aktionen, nicht nur geplante.
**Alle Pfade relativ** zum Projektordner (keine absoluten Pfade im Journal).

### Phase 5 — Sicherheitsnetz

| Schritt | Was passiert |
|---------|-------------|
| 5a. Recovery | App-Start: pending Einträge → Reparatur anbieten |
| 5b. Undo | Nur letzter Import + Preflight-Prüfung (Trockenlauf) |

---

## 7. Manueller Sortier-Modus

Für Scans (`20260409_143522.pdf`), Fotos, nicht erkannte Dateien.

### 7.1 Zugang

- Tab „Manuell sortieren" im Projektdetail
- Rechtsklick → „Manuell zuweisen" bei UNKNOWN in Import-Vorschau

### 7.2 Dialog

Links: Nicht erkannte Dateien. Rechts: Zuweisungs-Formular:

1. PDF-Vorschau (Post-V1 via PdfPig)
2. Dokumenttyp-Dropdown (aus angelernten Profilen)
3. Eingabefelder je nach Profil (Dropdown/Vorschläge aus Bestand, nicht nur Freitext)
4. Umbenennen-Toggle mit Live-Vorschau über `RenameSchemaEngine`
5. Buttons: „Überspringen" + „Einsortieren"

### 7.3 Umbenennung

`renameSchema` im Profil + Sanitizing:
```csharp
var fileName = RenameSchemaEngine.Render(profile.RenameSchema, values);
fileName = FileNameSanitizer.Normalize(fileName); // leere Felder, Sonderzeichen, Pfadlänge
```
Original-Name wird in `import_action_files.original_file_name` gespeichert → Undo möglich.

### 7.4 Validierung im manuellen Modus

- `geschoss`: Dropdown/Vorschläge aus Bestand + Freitext mit Normalisierung
- `planIndex`: Validierung nach indexPattern aus Profil
- `datum`: DatePicker, kein Freitext
- Nur Pflichtfelder (aus Profil `required`) abfragen — minimale Eingabe

---

## 8. DWG-Veraltet-Warnung (Revisions-Inkonsistenz)

Wenn eine DWG über `revision_file_links` mit Revisionen verknüpft ist deren Index-Stände
auseinanderlaufen, zeigt BPM eine Warnung.

**Beispiel:** DWG `BT1 gesamt_A.dwg` verlinkt mit 5 PDFs (alle Index A). Neue PDF kommt mit
Index B → Warnung: „Verknüpfte Dateien haben unterschiedliche Revisionsstände. DWG prüfen."

**Technisch:** Query über `revision_file_links` + `plan_revisions.plan_index`. Kein neues Schema.

**UI-Text:** Nicht „DWG ist veraltet" (zu absolut), sondern Inkonsistenz-Hinweis.

---

## 9. Bestandsmanifest — `_plan_index.json`

Versteckte JSON-Datei im Projektordner (Cloud-synced), ähnlich `.bpm-manifest`. Enthält den
aktuellen Dokumentenbestand als leichtgewichtigen Export.

### 9.1 Zweck

- **Gerät B** kann den Bestand laden ohne Vollscan (kein planmanager.db nötig)
- **Cache-Rebuild** nutzt Manifest als Startpunkt + Delta-Scan
- **Trennung:** Manifest = synchronisierter Soll-Bestand, Disk-Scan = Ist-Bestand

### 9.2 Schema

```json
{
  "schemaVersion": 1,
  "generatedAt": "2026-04-09T14:32:00Z",
  "generatedBy": "DESKTOP-HERBERT",
  "projectId": "01HV...",
  "manifestVersion": 42,
  "sourceImportId": "01JW...",
  "revisions": [
    {
      "documentKey": "Polierplan_103",
      "planNumber": "103",
      "planIndex": "D",
      "documentType": "Polierplan",
      "revisionStatus": "current",
      "files": [
        { "fileName": "S-103-D_TG.pdf", "relativePath": "01 Planunterlagen/TG/S-103-D_TG.pdf", "md5": "a3f2b8...", "fileSize": 2450000, "fileType": "pdf" }
      ]
    }
  ],
  "standaloneFiles": [
    { "fileName": "TG_Gesamt.dwg", "relativePath": "01 Planunterlagen/TG/TG_Gesamt.dwg", "md5": "c4d5e6...", "fileSize": 15000000, "fileType": "dwg" }
  ]
}
```

### 9.3 Schreibstrategie

Atomisch: Write-to-temp → fsync → Replace (nie in-place überschreiben).
Wird nach jedem Import automatisch aktualisiert.

### 9.4 Cache-Rebuild auf Gerät B

1. `_plan_index.json` laden (aus Cloud)
2. Lokalen Cache daraus aufbauen
3. Delta-Scan: Dateisystem prüfen ob Manifest stimmt
4. Abweichungen markieren: `ManifestOnly`, `VerifiedOnDisk`, `MissingOnDisk`, `DiscoveredNotInManifest`
5. Import-Historie wird NICHT rekonstruiert (bleibt auf Gerät A)

---

## 10. Datenbank-Schema (planmanager.db) — 6 Tabellen

### 10.1 Plan-Revisions-Cache (3 Tabellen)

```sql
CREATE TABLE plan_revisions (
    id TEXT PRIMARY KEY,                -- ULID
    document_key TEXT NOT NULL,         -- aus identityFields: "Polierplan_103_H5"
    plan_number TEXT NOT NULL,
    plan_index TEXT,                    -- NULL bei Erstausgabe / IndexSource=None
    document_type TEXT NOT NULL,
    target_folder TEXT NOT NULL,
    relative_directory TEXT NOT NULL,
    index_source TEXT NOT NULL,         -- "FileName", "None", "PlanHeader"
    revision_status TEXT NOT NULL,      -- "current", "archived"
    last_import_id TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE UNIQUE INDEX ux_plan_revision_current
ON plan_revisions(document_key, revision_status)
WHERE revision_status = 'current';

CREATE TABLE plan_files (
    id TEXT PRIMARY KEY,                -- ULID
    file_name TEXT NOT NULL,
    relative_path TEXT NOT NULL,
    file_type TEXT NOT NULL,            -- "pdf", "dwg", "jpg", "other"
    md5_hash TEXT NOT NULL,             -- IMMER Pflicht (universeller Fingerabdruck)
    file_size INTEGER NOT NULL,
    origin_mode TEXT NOT NULL,          -- "autoGrouped", "manualLinked", "standalone"
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE revision_file_links (
    revision_id TEXT NOT NULL,
    file_id TEXT NOT NULL,
    link_mode TEXT NOT NULL,            -- "auto", "manual"
    is_primary INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (revision_id, file_id),
    FOREIGN KEY (revision_id) REFERENCES plan_revisions(id),
    FOREIGN KEY (file_id) REFERENCES plan_files(id)
);
```

**n:m Verknüpfung:** Eine Datei kann mehreren Revisionen zugeordnet sein (Sammel-DWG).
Eine Datei ohne Links in `revision_file_links` ist standalone.
Link = nicht mehr standalone (verschwindet aus „nicht zugeordnet"-Liste).

### 10.2 Import-Journal (3 Tabellen)

```sql
CREATE TABLE import_journal (
    id TEXT PRIMARY KEY,                -- ULID
    timestamp TEXT NOT NULL,
    completed_at TEXT,
    status TEXT NOT NULL,               -- "pending", "completed", "failed", "undone"
    source_path TEXT NOT NULL,          -- relativ zum Projektordner
    file_count INTEGER NOT NULL,
    profile_id TEXT,
    machine_name TEXT,
    error_message TEXT
);

CREATE TABLE import_actions (
    id TEXT PRIMARY KEY,                -- ULID
    import_id TEXT NOT NULL,
    action_order INTEGER NOT NULL,
    action_type TEXT NOT NULL,          -- "new", "indexUpdate", "changed", "changedSameIdx",
                                       -- "olderRevision", "skip", "manual", "learnIndex"
    action_status TEXT NOT NULL,        -- "pending", "completed", "failed"
    document_key TEXT,
    plan_number TEXT NOT NULL,
    plan_index TEXT,
    old_index TEXT,
    source_path TEXT NOT NULL,          -- relativ
    destination_path TEXT NOT NULL,     -- relativ
    archive_path TEXT,                  -- relativ
    error_message TEXT,
    FOREIGN KEY (import_id) REFERENCES import_journal(id)
);

CREATE TABLE import_action_files (
    id TEXT PRIMARY KEY,                -- ULID
    action_id TEXT NOT NULL,
    file_id TEXT,                       -- FK → plan_files.id (optional, für Cache-Verknüpfung)
    file_name TEXT NOT NULL,
    original_file_name TEXT,            -- vor Umbenennung (NULL wenn nicht umbenannt)
    final_file_name TEXT,               -- nach Umbenennung (NULL wenn nicht umbenannt)
    file_type TEXT NOT NULL,            -- "pdf", "dwg", "jpg", "other"
    source_path TEXT NOT NULL,          -- relativ
    destination_path TEXT NOT NULL,     -- relativ
    md5_hash TEXT NOT NULL,
    file_size INTEGER,
    FOREIGN KEY (action_id) REFERENCES import_actions(id)
);
```

---

## 11. Undo-System

### 11.1 Reichweite

**Nur letzter vollständiger Import** ist undo-bar. Kein Multi-Rollback in V1.

| Aktion | Undo? | Wie |
|--------|-------|-----|
| Datei verschoben (Eingang → Ziel) | Ja | Zurück in Eingang |
| Datei ins _Archiv/ | Ja | Aus Archiv zurück |
| Datei umbenannt | Ja | original_file_name wiederherstellen |
| Manuell zugewiesen | Ja | Zurück in Eingang |
| plan_revisions/plan_files | Ja | Wird zurückgesetzt |
| SKIP (aus Eingang entfernt) | Nein | Liegt noch am Ziel |
| Profil-Änderung (Nachanlernen) | Nein | Nur Dateibewegungen |

### 11.2 Preflight-Prüfung (Trockenlauf)

Vor Undo-Ausführung: Alle Aktionen prüfen ob Dateien noch da sind wo erwartet.

```csharp
foreach (var action in actions.Reverse())
{
    if (!FileExistsWhereExpected(action))
        report.AddConflict(action, "Datei wurde extern verändert");
}
if (report.HasBlockingConflicts)
    ShowUndoConflictDialog(report);
else
    ExecuteUndo(actions);
```

### 11.3 Recovery (App-Start)

`import_journal` auf „pending" Einträge prüfen → Reparatur-Dialog.

---

## 12. MD5 als universeller Fingerabdruck

MD5-Hash + file_size werden **immer** gespeichert (nicht nur bei IndexSource=None).

### 12.1 Einsatzbereiche

- **Duplikat-Erkennung:** Gleicher Hash + Größe → identische Datei
- **Wiedererkennung nach Umbenennung:** Name ändert sich, Hash bleibt gleich
- **Cache-Rebuild:** Dateien auf Gerät B über Hash wiederfinden
- **SKIP-Erkennung:** Schnellvergleich ohne PDF-Inhalt lesen

### 12.2 Grenzen

- **Nicht für Sicherheit/Manipulation** — dafür SHA-256 (Post-V1 wenn nötig)
- **Metadaten-Sensibel:** Minimale Änderung (Export-Timestamp) → anderer Hash
- **Schutzgeländer:** Hash-Match immer mit file_size doppelt prüfen

```csharp
if (existing.Md5 == scanned.Md5 && existing.FileSize == scanned.FileSize)
    RelinkPath(existing, scanned.RelativePath); // Wiedererkennung
```

---

## 13. Dateinamen-Parsing (ADR-022)

### 13.1 Hybrid-Mechanismus

1. **Segment-Level:** Dateiname an Trennzeichen splitten → klickbare Blöcke
2. **Zeichen-Level:** Fallback per Toggle für Feinauswahl innerhalb eines Segments

### 13.2 Praxis-Beispiele

```
Polierplan: "S-103-C_TG Wände.pdf" → [S][103][C][TG Wände]
Schalungsplan: "5998-003_Wände_KG.pdf" → [5998][003][Wände][KG]
Architekturplan: "21005_104_AP_H1_GR_E2_05_Grundriss E+2.pdf" → [21005][104][AP][H1][GR][E2][05][Grundriss E+2]
Bauprotokoll: "BB_2026-04-09_003_Baubesprechung.pdf" → [BB][2026-04-09][003][Baubesprechung]
```

### 13.3 Verfügbare Feldtypen

**System:** `planNumber`, `planIndex`, `projectNumber`, `description`, `ignore`, `datum`
**Bau-spezifisch:** `geschoss`, `haus`, `planart`, `objekt`, `bauteil`, `bauabschnitt`, `stiege`, `achse`, `zone`, `block`
**Benutzerdefiniert:** User kann neue Feld-Namen erstellen.

---

## 14. Profil-System (ADR-010)

### 14.1 RecognitionProfile JSON-Schema

```json
{
  "schemaVersion": 1,
  "id": "01HV...",
  "documentTypeName": "Polierplan",
  "targetFolder": "01 Planunterlagen",
  "indexSource": "FileName",
  "indexMode": "optional",
  "indexPattern": "^[A-Z0-9]{1,3}$",
  "indexComparison": { "mode": "alphabetic", "caseInsensitive": true },
  "identityFields": ["documentType", "planNumber"],
  "delimiters": ["-", "_"],
  "segments": [
    { "position": 0, "fieldType": "projectNumber", "label": "Prefix", "required": false },
    { "position": 1, "fieldType": "planNumber", "label": "Plannummer", "required": true },
    { "position": 2, "fieldType": "planIndex", "label": "Index", "required": false },
    { "position": 3, "fieldType": "geschoss", "label": "Geschoss", "required": false },
    { "position": 4, "fieldType": "description", "label": "Bezeichnung", "required": false }
  ],
  "recognition": [
    { "method": "prefix", "pattern": "S-" }
  ],
  "recognitionPriority": 100,
  "conflictPolicy": "askUser",
  "grouping": { "mode": "baseFileName" },
  "folderHierarchy": ["geschoss"],
  "renameSchema": "{prefix}-{planNumber}-{planIndex}_{geschoss}",
  "createdAt": "2026-04-09T10:00:00Z",
  "updatedAt": "2026-04-09T10:00:00Z"
}
```

### 14.2 Wichtige Profil-Felder (Review-Ergebnis)

| Feld | Zweck | V1? |
|------|-------|-----|
| `identityFields` | Bildet `document_key` für fachliche Eindeutigkeit | Ja |
| `required` pro Segment | Pflicht vs. optional | Ja |
| `indexMode` | required / optional | Ja |
| `indexComparison` | Vergleichslogik (alphabetic/numeric) | Ja |
| `recognitionPriority` | Auflösung bei CONFLICT | Ja |
| `grouping` | Wie Dateien zu Revisionen gruppiert werden | Ja |
| `schemaVersion` | Für spätere Migration | Ja |
| `renameSchema` | Template für manuelle Umbenennung | Ja |
| `fieldRules` (Regex/allowedValues) | Validierung pro Feld | Post-V1 |

---

## 15. UI-Screens (Zusammenfassung)

| Screen | Beschreibung |
|--------|-------------|
| **Hauptseite** | Projektliste mit Eingang-Badge (amber/grün) |
| **Projektdetail** | 2 Tabs: Automatisch (Profile gruppiert nach Zielordner, ✎ Profil-Button) + Manuell sortieren |
| **Import-Vorschau** | DataGrid mit 9 Status-Typen, Zusammenfassungszeile, Rechtsklick-Korrekturen |
| **Profil-Wizard** | 5 Schritte: Datei auswaehlen → Segmente zuweisen → IndexSource → Zielordner → Erkennung (klickbare Segment-Bloecke) |
| **Manueller Sortier-Dialog** | Links: Dateien, Rechts: Formular + Umbenennung + Vorschau |

**UI-Regeln:** Screen States (5 Pflicht), max. 1 Primary/Kontext, BPM Dark Theme Tokens,
Icons.xaml, Deutsch, de-AT Formate. Interne Status reicher als UI-Begriffe.

---

## 16. Solution-Struktur

```
BauProjektManager.PlanManager/
├── ViewModels/
│   ├── PlanManagerViewModel.cs
│   ├── ProjectDetailViewModel.cs
│   ├── ImportPreviewViewModel.cs
│   ├── ManualSortViewModel.cs
│   └── ProfileWizardViewModel.cs
├── Views/
│   ├── PlanManagerView.xaml
│   ├── ProjectDetailView.xaml
│   ├── ImportPreviewDialog.xaml
│   ├── ManualSortDialog.xaml
│   └── ProfileWizardDialog.xaml
├── Services/
│   ├── FileNameParser.cs              ← Segment-Splitting
│   ├── DocumentTypeRecognizer.cs      ← Dokumenttyp-Erkennung
│   ├── DocumentKeyBuilder.cs          ← document_key deterministisch bilden
│   ├── ImportWorkflowService.cs       ← Workflow-Orchestrierung
│   ├── ProfileManager.cs             ← profiles.json + pattern-templates.json
│   ├── PlanIndexManifestService.cs   ← _plan_index.json lesen/schreiben
│   ├── FileRenamer.cs                ← RenameSchemaEngine + FileNameSanitizer
│   └── PlanManagerDatabase.cs        ← planmanager.db CRUD
└── BauProjektManager.PlanManager.csproj
```

---

## 17. Implementierungsreihenfolge

| Prio | # | Feature | Status |
|------|---|---------|--------|
| 1 | 18 | Dateinamen-Parser (Segment-Splitting, Domain-Logik) | ✅ v0.24.3 |
| 2 | 19 | Profil-Wizard GUI (5-Schritt: Datei, Segmente, Index, Zielordner, Erkennung) | ✅ v0.24.10 (UI, Speichern offen) |
| 3 | 20 | Dokumenttyp-Erkennung (prefix/contains) | |
| 4 | 21 | PatternTemplates (Vorschlagslogik) | |
| 5 | 22 | profiles.json (Pro Projekt) — ProfileManager Service | |
| 6 | 23 | pattern-templates.json (Globale Bibliothek) |
| 7 | 24 | Import-Workflow Scan→Parse→Classify→Plan |
| 8 | 25 | Import-Vorschau (9 Status, Rechtsklick) |
| 9 | 26 | Import-Execute (Verschieben, Journal, _plan_index.json) |
| 10 | 27 | Index-Archivierung (_Archiv/) |
| 11 | 28 | DB-Schema (6 SQLite-Tabellen) |
| 12 | 29 | Recovery (pending → Reparatur) |
| 13 | 30 | Undo (letzter Import + Preflight) |
| 14 | 31 | Backup vor Import |
| 15 | 32 | Manueller Sortier-Modus + Umbenennung |
| 16 | 33 | Erkennungs-Konflikt (CONFLICT) |

---

## 18. Verwandte ADRs

| ADR | Bezug |
|-----|-------|
| ADR-007 | Dokument-Dateien: 1..n pro Revision |
| ADR-008 | Import-Workflow |
| ADR-009 | Undo-Journal in SQLite |
| ADR-010 | RecognitionProfiles + PatternTemplates |
| ADR-022 | Segment-basiertes Dateinamen-Parsing |
| ADR-039 | ULID als Primärschlüssel |
| ADR-045 | IndexSource — Dreistufiges Modell |

---

## 19. Post-V1 Erweiterungen

| Feature | Abhängigkeit | Priorität |
|---------|-------------|-----------|
| PlanHeader-Extraktion (IndexSource) | PdfPig / KI-API (ADR-027) | Hoch |
| PDF-Vorschau im Import + manueller Sortierung | PdfPig (Seite als Bild) | Hoch |
| fieldRules (Regex/allowedValues pro Feld) | Profil-System V1 | Mittel |
| Planlisten Import/Export | ClosedXML + QuestPDF | V1.1 |
| Plan-Sammler (#34) | PlanManager Basis | Mittel |
| Schnellsuche Dokumente | plan_revisions + plan_files | Niedrig |
| Batch-Umbenennung | FileRenamer + eigene rename_history Tabelle | Niedrig |
| DB-Sync (planmanager.db über Cloud) | Event-Sync ADR-037 | Post-V1 |
| IndexComparison numeric/natural | indexComparison Policy | Bei Bedarf |

---

## 20. Implementierungs-Disziplinen (aus Cross-Review)

Drei Punkte die bei der Implementierung sauber gehalten werden müssen:

### 20.1 document_key deterministisch bilden

Nicht implizit im Code verstreut, sondern zentral in `DocumentKeyBuilder`:
```csharp
string BuildDocumentKey(Profile profile, ParsedFile parsed)
// Ergebnis z.B.: "Polierplan_103" oder "Polierplan_103_H5"
```

### 20.2 Link-Management explizit

Auto-Linking (gleicher Stamm) und manuelles Linking dürfen sich nicht gegenseitig
überschreiben. `link_mode` in `revision_file_links` unterscheidet die Quelle.
Manuelle Links haben Vorrang vor Auto-Links.

### 20.3 Manifest ≠ Wahrheit

`_plan_index.json` = synchronisierter Soll-Bestand.
Lokaler Disk-Scan = Ist-Bestand.
BPM muss mit Abweichungen leben können (Dateien fehlen, neue da, Hash anders).
Delta-Scan erkennt Differenzen und markiert sie intern.

---

## 21. Verwandte Konzepte (noch nicht gebaut)

| Konzept | Dokument | Wann relevant |
|---------|----------|---------------|
| Plankopf-Extraktion | [Moduleplanheader.md](../Konzepte/Moduleplanheader.md) | Bei IndexSource.PlanHeader |
| KI-API Import | ADR-027, [ModuleKiAssistent.md](../Konzepte/ModuleKiAssistent.md) | Bei automatischer PDF-Analyse |
| Multi-User Sync | ADR-037, [MultiUserKonzept.md](../Konzepte/MultiUserKonzept.md) | Bei DB-Sync über Event-System |

---

*Kernfrage: „Brauche ich das um Dokumente zu sortieren?" — Wenn nein → nicht jetzt bauen.*

*Änderungen v1.0 → v1.1 (09.04.2026):*
*- Dokumenttypen statt nur Plantypen*
*- Manueller Sortier-Modus + Umbenennung*
*- rename_log + plan_cache Tabellen*
*- Profil-Bearbeitung über Direktzugriff*

*Änderungen v1.1 → v2.0 (09.04.2026, nach ChatGPT Cross-Review 3 Runden):*
*- 3-Tabellen Cache: plan_revisions + plan_files + revision_file_links (n:m)*
*- document_key über identityFields statt nur plan_number*
*- 9 Status-Typen statt 6 (+CHANGED_SAME_INDEX, OLDER_REVISION, CONFLICT)*
*- IndexComparison Policy im Profil statt hardcoded*
*- indexMode: optional im Profil*
*- _plan_index.json als Bestandsmanifest (Cloud-synced)*
*- DWG-Veraltet-Warnung (Revisions-Inkonsistenz)*
*- rename_log gestrichen, Felder in import_action_files integriert*
*- MD5 + file_size immer Pflicht*
*- Undo auf letzten Import begrenzt + Preflight*
*- Journal = Execution-Log, relative Pfade*
*- Profil: identityFields, required, indexComparison, recognitionPriority, grouping, schemaVersion*
*- Solution: DocumentKeyBuilder, PlanIndexManifestService*
*- 3 Implementierungs-Disziplinen dokumentiert*
