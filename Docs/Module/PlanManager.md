---
doc_id: planmanager
doc_type: module
authority: source_of_truth
status: active
owner: herbert
topics: [planmanager, import, profil, parsing, undo, versionierung, index, sortierung]
read_when: [planmanager-feature, import-workflow, profil-anlernen, dateinamen-parsing, undo-journal]
related_docs: [architektur, db-schema, dsvgo-architektur, ui-ux-guidelines, wpf-ui-architecture]
related_code: [src/BauProjektManager.PlanManager/, src/BauProjektManager.Domain/Models/, src/BauProjektManager.Infrastructure/Persistence/]
supersedes: []
---

## AI-Quickload
- Zweck: Modul-Dokumentation für den PlanManager — Kern-Feature von BPM
- Autorität: source_of_truth
- Lesen wenn: PlanManager-Feature, Import-Workflow, Profil anlernen, Dateinamen-Parsing, Undo-Journal
- Nicht zuständig für: Allgemeine Architektur (→ Architektur.md), DB-Schema-Details (→ DB-SCHEMA.md), UI-Tokens (→ UI_UX_Guidelines.md)
- Kapitel:
  - 1. Zweck
  - 2. Datenschutz-Einordnung (DSGVO)
  - 3. Konzeptübersicht
  - 4. IndexSource — Dreistufiges Modell (ADR-045)
  - 5. Entscheidungsmatrix (Import-Versionierung)
  - 6. Workflow — 6 Phasen (0–5), 10-Schritte-Pipeline (ADR-008), 7-Stufen-Analyse
  - 7. Manueller Sortier-Modus
  - 8. DWG-Veraltet-Warnung
  - 9. Bestandsmanifest — .bpm/plan-index.json
  - 10. Datenbank-Schema (planmanager.db)
  - 11. Undo-System
  - 12. MD5 als universeller Fingerabdruck
  - 13. Dateinamen-Parsing (ADR-022)
  - 14. Profil-System (ADR-010)
  - 15. UI-Screens
  - 16. Solution-Struktur
  - 17. Implementierungsreihenfolge
  - 18. Verwandte ADRs
  - 19. Post-V1 Erweiterungen
  - 20. Implementierungs-Disziplinen
  - 21. Verwandte Konzepte
- Pflichtlesen:
  - Kapitel 5 (Entscheidungsmatrix) bei Import-Logik
  - Kapitel 10 (DB-Schema) bei Persistenz-Änderung
  - Kapitel 14 (Profil-System) bei Profil-Feature
- Fachliche Invarianten:
  - document_key über identityFields — nie nur plan_number allein
  - Import-Journal VORHER schreiben (pending) — erst dann Dateien verschieben
  - MD5 + file_size IMMER Pflicht — auch bei IndexSource=FileName
  - Alle Pfade im Journal relativ zum Projektordner
  - Undo nur letzter Import + Preflight-Prüfung (Trockenlauf)

---

# BauProjektManager — PlanManager (Modul-Dokumentation)

**Version:** 2.0
**Datum:** 09.04.2026
**Status:** In Entwicklung (V1 Kernfeature)
**Autor:** Herbert + Claude
**Review:** ChatGPT Cross-Review (3 Runden, 09.04.2026)

**Verwandte Dokumente:**
- [BauProjektManager_Architektur.md](../Kern/BauProjektManager_Architektur.md) — Kap. 4 (Überblick), Kap. 2–3 (Speicher/Registry)
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

### 3.2 Dokument-Dateien (ADR-007, ADR-050)

Ein Dokument (Revision) besteht aus **1 bis n Dateien**. Dateien werden über die
**fachliche Identity** (document_key + document_type + Revisionsstand) zusammengeführt,
NICHT über den Dateinamen-Stamm. Fehlende PDF oder DWG ist kein Fehler.

**Auto-Link Regeln (Cross-Review 15.04.2026):**
Dateien werden nur automatisch gruppiert wenn ALLE Bedingungen erfüllt sind:
1. Gleicher `document_key` (aus identityFields)
2. Gleiche `document_type` (DocumentTypeId)
3. Gleicher Revisionsstand
4. Extension-Kombi erlaubt (pdf+dwg, pdf+dxf)

Kein Auto-Link nur weil der Dateiname gleich aussieht — gleiche Dateinamen in
verschiedenen Ordnern (z.B. Schalung-DWG vs Bewehrung-PDF) sind VERSCHIEDENE Dokumente.

**Wichtig:** PDF und DWG sind grundsätzlich eigenständige Dateien in der DB. Eine DWG kann
auch mehreren Revisionen zugeordnet sein (Sammel-DWG) oder eigenständig bleiben (standalone).

### 3.3 Zwei Sortier-Modi

| Modus | Wann | Was passiert |
|-------|------|-------------|
| **Automatisch** | Datei matcht ein Profil | Segmente geparst → Zielordner berechnet → Vorschau |
| **Manuell** | Kein Profil matcht (Scans, Fotos) | User wählt Typ, gibt Felder ein, optional umbenennen |

### 3.4 Speicherorte

| Datei | Ort | Synct? |
|-------|-----|--------|
| `profiles.json` → `.bpm/profiles/<n>.json` | Cloud Projektordner `.bpm/profiles/` (ADR-046) | Ja |
| `pattern-templates.json` | Cloud `.AppData/` | Ja |
| `.bpm/plan-index.json` | Cloud Projektordner (.bpm/) | Ja |
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

## 6. Workflow — 6 Phasen (0–5)

### Phase 0 — Profil anlernen (einmalig pro Dokumenttyp)

5-Schritt-Wizard (auch erreichbar über „✎ Profil" im Projektdetail):

1. **Datei auswaehlen** → Datei aus Eingang klicken oder Name eingeben, Trennzeichen, Parsen → Segmente als Vorschau
2. **Segmente zuweisen** → Feldtypen per Dropdown zuweisen (PlanNumber Pflicht)
3. **IndexSource** → FileName / None / PlanHeader(Post-V1), indexMode, indexComparison
4. **Zielordner + Ordner-Hierarchie** → Hauptordner + Unterebenen (Geschoss, Haus etc.)
5. **Erkennung** → Klickbare Segment-Bloecke (Default `Method=segment`, BPM-082) oder Regex-Pattern (Fallback `Method=regex`, BPM-007.02), Live-Test gegen Beispieldatei, Prioritaet. Alte Methoden `prefix`/`contains` sind entfernt (siehe Kap. 14)

Ergebnis: RecognitionProfile in `.bpm/profiles/<n>.json` (ADR-046) + PatternTemplate in `pattern-templates.json`.

### Phase 1 — Dateien landen im Eingang

- Quellen: E-Mail, Portal-Download, USB, Scanner, Explorer
- Beim App-Start: Alle `_Eingang/`-Ordner prüfen → Badge in Sidebar

### Phase 2 — Import-Analyse (automatisch, 7-Stufen-Pipeline)

Intern orchestriert durch `ImportWorkflowService.AnalyzeAsync()` (Cross-Review 15.04.2026):

| Stufe | Service | Was passiert |
|-------|---------|-------------|
| 1. Scan | `ImportScanService` | `_Eingang/` rekursiv durchsuchen, Dateiliste + Basis-Metadaten |
| 2. Fingerprint | `FileFingerprintService` | MD5-Hash + file_size berechnen (bounded parallel) |
| 3. Parse | `FileParseService` | Dateinamen in Segmente splitten laut Profil-Tokenization |
| 4. Resolve Context | `ImportContextResolver` | Ordner-Kontext + Profil-Match + Extension → ResolutionEvidence |
| 5. Build Identity | `DocumentKeyBuilder` | document_key deterministisch aus resolved fields bilden |
| 6. Version Decision | `RevisionDecisionService` | Entscheidungsmatrix (Kap. 5.3) → 9 Status-Typen |
| 7. Execution Plan | `ImportPlanBuilder` | Zielpfade berechnen, Dateien gruppieren (nach fachlicher Identity) |

**Fehler-/Abbruchstrategie:** Analyse-Fehler pro Datei → Warning/Unknown in Vorschau, kein Gesamtabbruch.
**CancellationToken:** Durch alle 7 Stufen durchgereicht (async).

### Phase 3 — Import-Vorschau (User entscheidet)

Tabelle mit Status, Dateiname, Dokumenttyp, Nr, Index, Zielordner.
User kann: Rechtsklick korrigieren, UNKNOWN → manuell zuweisen, LEARN_INDEX bestätigen.
Import erst nach Bestätigung: Button „Import ausführen".

### Phase 4 — Ausführung

| Schritt | Was passiert |
|---------|-------------|
| 4a. Backup | planmanager.db + .bpm/profiles/ als .bak |
| 4b. Journal | Alle Aktionen VORHER ins Undo-Journal (Status: pending) |
| 4c. Execute | Pro Aktion: _Archiv/ erstellen → verschieben → ggf. umbenennen → completed |
| 4d. Finalize | Journal completed. .bpm/plan-index.json aktualisieren. Zusammenfassung. |

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

## 9. Bestandsmanifest — `.bpm/plan-index.json`

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

1. `.bpm/plan-index.json` laden (aus Cloud)
2. Lokalen Cache daraus aufbauen
3. Delta-Scan: Dateisystem prüfen ob Manifest stimmt
4. Abweichungen markieren: `ManifestOnly`, `VerifiedOnDisk`, `MissingOnDisk`, `DiscoveredNotInManifest`
5. Import-Historie wird NICHT rekonstruiert (bleibt auf Gerät A)

---

## 10. Datenbank-Schema (planmanager.db) — 6 Tabellen (v1.0) + Schema v2.0 geplant

**Status:** Schema v1.0 war implementiert seit v0.25.15. **Schema v2.0 DDL implementiert in BPM-109.01 (v0.28.55)** — die Tabellen werden von `PlanManagerDatabase.EnsureTables()` erzeugt. **Models + Repository in BPM-109.02, Pipeline-Verdrahtung in .03 (Import reaktiviert, live verifiziert), Identity-Key-Fix .03b, Zeitlogik + Audit-Events .04, `released_at` .04b** (Stand v0.28.62). Import schreibt jetzt `plan_documents`/`plan_revisions`/`plan_files` über das Drei-Ebenen-Modell; Drei-Zeiten-Modell (`current_from`/`received_at`/`released_at`) aktiv. Offen im Foundation Slice nur noch **BPM-109.05a** (IPlanLookupService-Stub). Drei-Ebenen-Modell + Plan-Archiv-Persistenz — siehe **ADR-058** + **ADR-058-Addendum** und **DB-SCHEMA.md Kap. 6.7** für die vollständige v2.0-Definition mit `plan_documents` (NEU), `plan_revisions` (umgebaut mit FK + Zeitstempeln), `plan_document_segments`, `plan_revision_events`, `plan_context_links` und `building_part_aliases`.

**Pipeline-Erweiterung mit Schema v2.0:**
Nach Stage 5 (`DocumentKeyBuilder` produziert Natural Key) kommt eine neue **Document-Resolve-Stage**, die das `document_key` in `plan_documents.id` auflöst (Upsert). `ImportPlanBuilder` unterscheidet danach 4 Operationen: `NewDocument`, `NewRevision`, `SupersedeCurrent`, `FileLink`. Extrahierte Segmentwerte landen in `plan_document_segments` mit FK auf `segment_types` (ADR-056). Stammdaten-FKs `building_part_id`/`building_level_id` werden über exakte Normalisierung gegen `building_parts`/`building_levels` (+ `building_part_aliases` post-V1) aufgelöst.

**Cross-Modul-API:**
`IPlanLookupService` (Interface-Stub in V1, Implementation post-V1 parallel zu BPM-056) ist die öffentliche API für konsumierende Module (Bautagebuch, Foto, Vorlagen). Module schreiben Cross-Modul-Verweise in `plan_context_links` mit `resolution_mode = 'fixed_revision'` — alte Berichte zeigen immer dieselbe Revision (ADR-058 fachliche Invariante).

**Cross-DB-Referenzen (ADR-058-Addendum, CGR r3):**
`planmanager.db` (per-Projekt-Cache) und `bpm.db` (zentrale Stammdaten) sind getrennte SQLite-Dateien. Bezüge von Plan-Tabellen auf `bpm.db` (`building_part_id`, `building_level_id`, `segment_type_id`) sind **logische Referenzen** (`TEXT`, kein FK — SQLite erzwingt keine FK über DB-Datei-Grenzen). Gültigkeit wird **service-seitig** validiert: Import-Resolve (`.03`), `IPlanLookupService` (post-V1) und Stammdaten-Soft-Delete-Guard (post-V1). `ATTACH bpm.db` für Lookup/Reporting bleibt gekapselt im Service, **kein** Cross-DB-SQL in UI oder Low-Level-Repo. Harte FKs gelten nur innerhalb `planmanager.db`. Das Alias-Mapping `building_part_aliases` liegt zentral in `bpm.db` (DB-SCHEMA Kap. 4.11).

**Reset-Anweisung bei Schema-Wechsel (Frühphasen-Regel):** User löscht `planmanager.db` → BPM erstellt sie beim nächsten App-Start neu mit v2.0. Keine Migration.

Die folgenden Tabellen-Definitionen zeigen den **aktuellen Stand (v1.0)** — sie werden durch die v2.0-Variante in DB-SCHEMA.md Kap. 6.7 abgelöst.

### 10.1 Plan-Revisions-Cache (3 Tabellen, v1.0)

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

### 13.4 Tokenization (v2 — Cross-Review 15.04.2026)

Seit Schema v2 wird die Tokenization pro Profil konfiguriert statt global:

```json
"tokenization": {
  "delimiters": ["-", "_"],
  "collapseRepeatedDelimiters": false,
  "firstTokenDelimiter": null
}
```

| Feld | Zweck | Beispiel |
|------|-------|---------|
| `delimiters` | Trennzeichen-Liste | `["-", "_"]` für Standard, `["-", "_", " "]` für Space-Splitting |
| `collapseRepeatedDelimiters` | `__` und `______` als ein Trenner | `24101__505b` → `[24101][505b]` statt `[24101][][505b]` |
| `firstTokenDelimiter` | Erstes Token vor separatem Trennzeichen abspalten | `PP01-1 Wohnanlage...` → `[PP01-1][Wohnanlage]` |

### 13.5 IndexExtraction (v2)

Für zusammengeschriebene Indizes wie `002a` (Nummer+Buchstabe ohne Trenner):

```json
"indexExtraction": {
  "source": "segment",
  "segmentSelector": "planNumber",
  "pattern": "^(?<number>\\d{3})(?<index>[A-Za-z])$",
  "numberGroup": "number",
  "indexGroup": "index"
}
```

Wird nach dem normalen Segment-Parsing angewendet. Wenn das Regex matcht, werden `planNumber` und `planIndex` aus den Capture Groups extrahiert.

### 13.6 Stage-Konzept (Cross-Review 15.04.2026)

Jedes Dokument hat eine Stage im Review-/Freigabe-Lifecycle:

| Stage | Bedeutung | Erkennung |
|-------|-----------|-----------|
| `Unknown` | Keine Stage-Info (Default, NICHT "Final") | Standard |
| `Draft` | VORABZUG, Vorab, VA | Ordnername oder Dateiname enthält Marker |
| `Final` | Endgültig freigegeben | Explizit erkannt, nicht Default |

Stage ist NICHT Teil des `document_key` und beeinflusst die Versionierung nicht direkt.

---

## 14. Profil-System (ADR-010)

### 14.1 RecognitionProfile JSON-Schema (v3 — BPM-082, 2026-05)

**Aenderungen zu v2:** `recognition[].method` ist jetzt `"segment"` (Default) oder
`"regex"` (Fallback). `segment`-Rules tragen zusaetzlich `segmentPosition: int?`
(0-basierte Token-Position). Alte Methoden `prefix`/`contains` sind entfernt —
Profile mit diesen Methoden werden von `ProfileManager.Load` mit `Log.Error`
verworfen. Details: [ADR-010](../Referenz/ADR.md#adr-010-recognitionprofiles-und-patterntemplates-getrennt).

```json
{
  "schemaVersion": 3,
  "id": "01HV...",
  "documentTypeId": "polierplan",
  "documentTypeName": "Polierplan",
  "targetFolder": "01 Planunterlagen",
  "indexSource": "FileName",
  "indexMode": "optional",
  "indexPattern": "^[A-Z0-9]{1,3}$",
  "indexComparison": { "mode": "alphabetic", "caseInsensitive": true },
  "indexExtraction": {
    "source": "segment",
    "segmentSelector": "planNumber",
    "pattern": "^(?<number>\\d{3})(?<index>[A-Za-z])$",
    "numberGroup": "number",
    "indexGroup": "index"
  },
  "tokenization": {
    "delimiters": ["-", "_"],
    "collapseRepeatedDelimiters": false,
    "firstTokenDelimiter": null
  },
  "identityFields": ["documentType", "planNumber"],
  "segments": [
    { "position": 0, "fieldType": "projectNumber", "label": "Prefix", "required": false, "includeInIdentity": false },
    { "position": 1, "fieldType": "planNumber", "label": "Plannummer", "required": true, "includeInIdentity": true },
    { "position": 2, "fieldType": "planIndex", "label": "Index", "required": false, "includeInIdentity": false },
    { "position": 3, "fieldType": "geschoss", "label": "Geschoss", "required": false, "includeInIdentity": false },
    { "position": 4, "fieldType": "description", "label": "Bezeichnung", "required": false, "includeInIdentity": false }
  ],
  "recognition": [
    { "method": "segment", "pattern": "S", "segmentPosition": 0 }
  ],
  "recognitionPriority": 100,
  "conflictPolicy": "askUser",
  "grouping": { "mode": "identity" },
  "folderHierarchy": ["geschoss"],
  "renameSchema": "{prefix}-{planNumber}-{planIndex}_{geschoss}",
  "createdAt": "2026-04-09T10:00:00Z",
  "updatedAt": "2026-04-09T10:00:00Z"
}
```

**Recognition-Methoden (BPM-082):**

| Methode | Pflichtfelder | Zweck |
|---------|---------------|-------|
| `segment` (Default) | `pattern`, `segmentPosition` | Positionsgenauer Token-Vergleich. Datei wird via `FileNameParser` tokenisiert, Token an `segmentPosition` muss `pattern` matchen (OrdinalIgnoreCase). |
| `regex` (Fallback) | `pattern` | Voll-Filename-Regex fuer Sonderfaelle (Statiknummernkreise wie `^5998-2\d{2}_`, Dateien ohne saubere Delimiter). ReDoS-Schutz via Timeout (100 ms). |

**AND-Semantik bei Multi-Rules:** alle Rules eines Profils muessen matchen.

**Reset bei Schema-Wechsel (Fruehphase):** Profile aus dem alten Schema
(`method=prefix`/`contains`) werden beim Laden verworfen. Aktion fuer Tester:
betroffene `.bpm/profiles/*.json`-Dateien loeschen, im Wizard neu anlegen.

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

## 15. UI-Screens

### 15.1 Überblick

| Screen | Beschreibung |
|--------|-------------|
| **Hauptseite** | Projektliste mit Eingang-Badge (amber/grün) |
| **Projektdetail** | 3 Tabs: Profile (gruppiert nach Zielordner, ✎ Profil-Button), Manuell sortieren, Sync |
| **Import-Vorschau** | DataGrid mit 9 Status-Typen, Zusammenfassungszeile, Rechtsklick-Korrekturen |
| **Profil-Wizard** | 5 Schritte: Datei auswaehlen → Segmente zuweisen → IndexSource → Zielordner → Erkennung (klickbare Segment-Bloecke) |
| **Manueller Sortier-Dialog** | Liste mit kontextuellem Radial-Menü beim Klick & Halten |

**Mockup-Verzeichnis:** `Docs/Mockups/PlanManager/` — durchklickbarer HTML-Prototyp.
Quelle der Wahrheit für Navigationskanten: `Docs/Mockups/PlanManager/_SITEMAP.md`.

---

### 15.2 Mockup-Konventionen (ab 2026-05)

Verbindlich für alle PlanManager-UI-Mockups. Skill `mockup-erstellen` v0.23.0+ erzwingt sie.

- **Ordner-pro-Fenster** — flache Struktur direkt unter `Docs/Mockups/PlanManager/`:
  `01_Projektuebersicht/`, `02_Projektdetail/`, `03_ProfilWizard/`, `_Archiv/`.
  Varianten/Tabs/Wizard-Schritte als `NN_Variante.html` im Fenster-Ordner.
  Hierarchie/User-Journey wird **nicht** in Ordnernamen kodiert, sondern in der
  `Aufrufer`-Spalte von `_SITEMAP.md`.
- **Klick-Navigation Pflicht** — jedes interaktive Element bekommt
  `onclick="location.href='…'"` mit relativem Pfad. Mockups sind als
  Durchklick-Prototyp ausgelegt, nicht als Standbild.
- **Tote Pfade** — Ziel-Mockup existiert geplant, aber noch nicht gebaut:
  `onclick="alert('Mockup folgt: <Pfad>')"` + gestrichelter Rand + opacity:0.7.
  In Sitemap als `🟡 tot` markiert.
- **`_SITEMAP.md` pro Modul** — zentrale Wahrheit für Navigationskanten.
  Spalten: Quelle, Ziel, Trigger, Status (`✅ aktiv` / `🟡 tot` / `❌ kaputt`).
  Bei jedem neuen Mockup gelesen und gepflegt.

**Sprachregelung "Profil" statt "Dokumenttyp":**
Im UI heißt es konsequent **Profil** — der Wizard erstellt ein Profil, der Tab
zeigt Profile, der Button heißt "+ Neues Profil anlernen", das Wizard-Feld
heißt "Profil-Name". Die interne Code-Property `DocumentTypeName` bleibt
Implementation-Detail; im XAML-Label wird "Profil-Name" angezeigt
(WPF-Umsetzung BPM-080.05).

---

### 15.3 Hauptseite — Projektübersicht

**Mockup:** `Docs/Mockups/PlanManager/01_Projektuebersicht/01_Projektuebersicht.html`

App-Shell mit Sidebar links (BauProjektManager-Header, 📁 PlanManager aktiv,
⚙ Einstellungen), Statusbar unten (BPM-Akzent #0078D4 — "PlanManager · 3 aktive Projekte").

**Aufbau:**
- Header: Modul-Titel + Aktive-Projekte-Zähler + 🔍 Suche-Input + ⟳ Aktualisieren-Button
- Projekt-Tabelle (Karten-Style):
  - Farbstreifen links pro Karte (amber #F0AD4E = unsortierte Files / grün #4EC94E = aktuell)
  - Spalten: Projekt, Nr., Eingang-Status-Badge ("50 unsortiert" / "✓ Aktuell"), Aktiv-Indikator
- Footer-Hinweis: "3 Projekte geladen"

**Klick-Verhalten:**
- Projekt-Karte → Projektdetail (`02_Projektdetail/01_Profile.html`)
- Sidebar "⚙ Einstellungen" → Cross-Modul `../../Settings/01_Einstellungen/01_Allgemein.html`

---

### 15.4 Projektdetail — Profile-Tab

**Mockup:** `Docs/Mockups/PlanManager/02_Projektdetail/01_Profile.html`

Selbe App-Shell. Project-Header oben mit `← Zurück`-Pfeil, Projektname,
[↻ Rückgängig] und [Import starten]-Buttons. Eingang-Bar darunter:
"⚡ 15 im Eingang · 3 vom Server-Sync".

**Tabs:** `Profile` (aktiv) — `Manuell sortieren` — `Sync` (tot, BPM-005 vertagt).

**Profile-Tab-Inhalt:**
- Profil-Karten gruppiert nach Zielordner (Pläne (01 Planunterlagen), Protokolle (04 Protokolle), …)
- Pro Profil-Karte: farbiger Status-Streifen + Name (Polierplan, Schalungsplan, …) +
  Pattern-Chips (`S-`, `FileName`, `opt.Idx`) + Plan-Anzahl + ✎ Profil-Button
- Statusbar unten: Projektkürzel + Profil-Anzahl + Dokument-Anzahl + Eingang-Counter

**Klick-Verhalten:**
- `← Zurück` und Sidebar "📁 PlanManager" → Projektübersicht
- Sidebar "⚙ Einstellungen" → Cross-Modul Settings
- Tab "Manuell sortieren" → `02_ManuellSortieren.html` (aktiv)
- Tab "Sync" → `alert('Mockup folgt nach Spike 0 / ADR-053')` (tot)
- ✎ Profil-Button (4×) → ProfilWizard Schritt 1
- `+ Neues Profil anlernen` (gestrichelter Plus-Button am Ende) → ProfilWizard Schritt 1

---

### 15.5 Projektdetail — Manuell sortieren

**Mockup:** `Docs/Mockups/PlanManager/02_Projektdetail/02_ManuellSortieren.html`

Selbe App-Shell + Tab-Bar. Tab `Manuell sortieren` aktiv.

**Kern-UX-Konzept: Radial-Menü beim Klicken & Halten.**

Der manuelle Sortier-Modus löst das Problem, dass nicht jede Datei profilgerecht
benannt ist (Scans, Fremdformate, Foto-Uploads). Statt eines klassischen
Drag-and-Drop-Trees nutzt das Mockup ein **kontextuelles Radial-Menü**, das beim
Mausklick-und-Halten auf einem Dokument **um das Dokument herum** erscheint.

**Aufbau:**
- Dokumenten-Liste vertikal (Consolas-Font, ein Item pro Zeile mit Filesize + Datum)
- Hint oben rechts: "☝ Klicken & halten zum Sortieren"
- **Aktives Dokument** beim Halten:
  - Akzent-Border #0078D4 + Glow (box-shadow rgba(0,120,212,0.6))
  - `cursor:grabbing`
  - z-index 10 (über dimmed Items)
- **Andere Listen-Items**: `opacity:0.25` — Fokus auf das gehaltene Dokument
- **Radial-Menü** (420×420px, z-index:100, pointer-events:none auf Container,
  pointer-events:auto auf Segmenten):
  - 6 Segmente in 60°-Anordnung um das aktive Dokument:
    - **12 Uhr**: Polierpläne (blau #185FA5)
    - **2 Uhr**: Statikpläne (lila #534AB7) — Beispiel für Hover-State mit Sub-Menü
    - **4 Uhr**: Bewehrungspläne (rot #993C1D)
    - **6 Uhr**: Protokolle (grün #0F6E56)
    - **8 Uhr**: Sonstiges (grau #555)
    - **10 Uhr**: + Neuer Ordner (transparent, dashed border #F0AD4E)
  - Jedes Segment zeigt: Icon, Ordner-Name, Item-Anzahl bzw. "▸ halten" bei Hover-Segmenten
- **Sub-Menü** (z-index:110, rechts vom hover-Segment):
  - Erscheint, wenn der User über einem Segment mit Sub-Ordnern festhält
  - Beispiel "Statikpläne": Schalung (23), Bewehrung (31), + Neuer Sub-Ordner
- Footer-Hint im Radial-Container: "Loslassen über einem Ordner → Dokument wird
  verschoben. Auf Pfeil-Segment halten → Unter-Ordner."

**UX-Entscheidungen / Begründung:**
- **Radial statt Dropdown:** Pen/Maus-Distanz konstant in alle Richtungen — der
  User muss nicht erst zum Dropdown navigieren, sondern findet die Ordner-Optionen
  direkt um den Cursor.
- **Drum-herum-Anordnung:** Center bleibt sichtbar (transparent durch das Radial),
  damit der User die Identität des gehaltenen Dokuments während der Auswahl
  präsent hat.
- **Sub-Menü nach rechts ausklappend:** zeigt Hierarchie ohne das Hauptmenü zu
  verlassen — wenn nur 1 Ebene tief, kann der User direkt zum Sub-Ordner ziehen.
- **`opacity:0.25` für andere Items:** verhindert visuelle Überforderung, simuliert
  Backdrop-Dimming ohne separates Overlay.
- **6 Segmente fix:** mehr als 6 wird optisch unübersichtlich. Wenn ein User mehr
  Top-Level-Kategorien hat, wird die Liste vom System nach Häufigkeit/Letzte-Nutzung
  begrenzt; "+ Neuer Ordner" als 6. Segment ermöglicht jederzeit Erweiterung.

**Klick-Verhalten (im Mockup):**
- Statisches HTML — Radial-Menü ist als sichtbarer Hover-/Halt-State gerendert,
  nicht als echtes JS-Drag-and-Drop.
- WPF-Umsetzung später: `MouseDown` startet Hold-Timer (~150ms), Radial-Menü
  per `Popup` oder `AdornerLayer` rendern, `MouseMove` über Segment = Hover,
  `MouseUp` = Auswahl.

---

### 15.6 Profil-Wizard — 5 Schritte (modaler Dialog)

**Mockup-Verzeichnis:** `Docs/Mockups/PlanManager/03_ProfilWizard/`

Modaler Dialog, **750×580px**, ohne Sidebar (Dialog-Frame mit Window-Title + rotem
X-Schließen-Button). Header pro Schritt: Schritt-Titel + 5 Progress-Dots
(Dot.done = #04395E / Dot.act = #0078D4 / inactive = #3C3C3C) + "Schritt N von 5"-Counter.
Footer durchgängig: `Abbrechen` (links) — `← Zurück` — `Weiter →` (Primary).
Letzter Schritt: `Profil speichern` (Primary, breit) statt Weiter.

Schließen-Mechanismen (alle 5 Schritte → zurück zu Profile-Tab):
- Rotes X im Window-Title
- `Abbrechen`-Button im Footer

#### 15.6.1 Schritt 1 — Datei auswählen

**Mockup:** `03_ProfilWizard/01_Datei.html`

- **Eingang-Liste** (ListBox-Style, max-height 140px, Consolas-Font): Dateien aus
  dem Projekt-Eingang. Eine als Vorlage selected (Akzent-Background).
- **Beispiel-Dateiname-Input**: TextBox mit aktueller Datei + `Parsen`-Button
  rechts daneben (Secondary-Style).
- **Trennzeichen-Input**: TextBox 80px breit (Default `- _ .`) + Hinweis
  "(Leerzeichen-getrennt, z.B.: - _ .)".
- **Parse-Info**: "✓ N Segmente erkannt" (grün, nach Parse).
- **Segment-Vorschau** (read-only Tokens, Consolas): WrapPanel mit Border-Cards
  je Segment-RawValue.

`Zurück`-Button disabled (erster Schritt).

#### 15.6.2 Schritt 2 — Segmente zuweisen

**Mockup:** `03_ProfilWizard/02_Segmente.html`

**UX-Konzept: Drag-and-Drop von Feldtypen auf farbige Segment-Tokens.**

Ersetzt die ursprüngliche Per-Segment-Dropdown-Lösung. Inspiriert vom
historischen Archiv-Mockup (`_Archiv/00_Gesamtuebersicht.html`).

- **Beispiel-Block** (Background #252526): Dateiname (Consolas, grau, klein) +
  Token-Reihe horizontal mit Trennzeichen sichtbar dazwischen.
- **Tokens** (Drop-Ziele):
  - Pro Segment ein farbiger Block mit 2 Zeilen — RawValue oben (Consolas weiß),
    Field-Type-Label unten (klein, opacity 0.85).
  - Farben pro Field-Type (konsistent mit Archiv-Mockup):
    Projektnummer #534AB7 (lila), Plannummer #0F6E56 (grün), Index #993C1D (rot),
    Geschoss #185FA5 (blau), Planart #1F7280 (cyan), Bezeichnung #555 (grau),
    Ignorieren #3C3C3C + opacity 0.55 (gedimmt).
  - **Unset-Token** (noch nicht zugewiesen): `background:#2D2D30`, `border:1px dashed #858585`,
    Label "? Typ wählen".
- **Feldtyp-Chips** (Drag-Quellen, unter dem Beispiel-Block):
  - WrapPanel mit allen 16 verfügbaren Field-Types als Chips
    (Plannummer, Index, Projektnummer, Bezeichnung, Datum, Geschoss, Haus, Planart,
    Objekt, Bauteil, Bauabschnitt, Stiege, Achse, Zone, Block, Ignorieren)
  - `cursor:grab` / `:grabbing`
  - Schon zugewiesene Chips: `background:#04395E` mit Akzent-Text — visualisiert
    "in Verwendung"
  - **Plannummer** mit Pflicht-Marker `★` (orange)
  - `+ Eigenes` als Custom-Chip (dashed orange border, transparenter Background)
- **Drop-Hint** zwischen Tokens und Chips: "💡 Drop-Ziel: ein Segment-Token oben
  — Feldtyp wird sofort übernommen"
- **Warn-Hinweis** unten: "⚠ Pflicht: Mindestens Plannummer (★) muss zugewiesen werden."

**WPF-Umsetzung später:** WPF-DragDrop-Framework, `DragSource` = Type-Chip,
`DropTarget` = Segment-Token, OnDrop setzt `SegmentInfo.FieldType`.

#### 15.6.3 Schritt 3 — Index-Erkennung

**Mockup:** `03_ProfilWizard/03_IndexSource.html`

- **Body-Title**: "Wie wird der Index (Revision) erkannt?"
- **3 Radio-Optionen** mit Beschreibungstext darunter:
  - **Aus Dateiname** (Default selected) — "Index wird aus einem Segment im
    Dateinamen gelesen (z.B. A, B, C)"
  - **Kein Index** — "Dokument hat keinen Index. Versionen werden per MD5-Hash erkannt."
  - **Aus Plankopf** mit Badge `POST-V1`, disabled (opacity 0.45) — "Index wird
    aus dem PDF-Plankopf gelesen. Noch nicht verfügbar."
- **Sub-Box** (Background #252526, sichtbar wenn "Aus Dateiname" gewählt):
  - **Index-Modus**:
    - ● Optional (Erstausgabe kann ohne Index sein) — default
    - ○ Pflicht (Jedes Dokument muss einen Index haben)
  - Trennlinie
  - ☑ Gross-/Kleinschreibung ignorieren (A = a)
- **Warn-Box** (orange Border #F0AD4E, wenn in Schritt 2 kein Index-Segment zugewiesen):
  "⚠ In Schritt 2 wurde kein Index-Segment zugewiesen. Gehe zurück und weise einem
  Segment den Typ **Index** zu, oder wähle **Kein Index**."

#### 15.6.4 Schritt 4 — Zielordner

**Mockup:** `03_ProfilWizard/04_Zielordner.html`

Kombination aus klassischem Dropdown (Hauptordner) und Drag-and-Drop-Hierarchie
(Unterordner-Ebenen).

- **Hauptordner**:
  - Pseudo-Dropdown (TextBox-Look mit ▾): zeigt aktuell gewählten Ordner
    (z.B. "01 Planunterlagen"). Klick = klassisches Auswahl-Dropdown.
  - Daneben `+ Neuer Ordner`-Button (Secondary): falls keiner der vorhandenen
    Top-Level-Ordner passt, kann ad-hoc einer erstellt werden.
- **Unterordner-Ebenen** (optional):
  - **Aktive Hierarchie-Liste** (dashed Border-Container als Drop-Zone):
    Reihen mit `⋮⋮` Grip-Icon + "Ebene N:" + Feldname + Sample-Wert + ✕-Button.
    Reihenfolge per Drag-and-Drop änderbar (Grip-Cursor).
    Beispiel: `Ebene 1: Planart → Polierplan/`, `Ebene 2: Geschoss → OG1/`.
  - **Verfügbare Felder** (WrapPanel mit Chips, Drag-Quelle):
    Nur die in Schritt 2 zugewiesenen FieldTypes sind verfügbar (Plannummer ist
    technisch verfügbar, ergibt aber keinen Sinn als Ordner-Ebene — kein
    Hard-Block, User-Verantwortung). Schon verwendete Chips: `opacity:0.4`,
    `cursor:not-allowed`.
  - Sample-Wert pro Chip in Akzent-Farbe Consolas (z.B. `Geschoss [OG1]`).
- **Vorschau Zielpfad** (Box mit Monospace-Pfad):
  ```
  01 Planunterlagen/
    Polierplan/
      OG1/
        5998-201_OG1_Polierplan.dwg
  ```

**Konzept-Entscheidung:** Hauptordner als klassisches Dropdown (vertraut, schnell),
weil es eine 1-aus-N-Auswahl ist. Unterordner-Ebenen als Drag-and-Drop, weil hier
**Reihenfolge** (Ebene 1 → Ebene 2 → …) entscheidend ist und mehrere Felder
auswählbar sind — Reihenfolge per Drag visuell intuitiv.

#### 15.6.5 Schritt 5 — Erkennung

**Mockup:** `03_ProfilWizard/05_Erkennung.html`

**Kern-Entscheidung BPM-007.02: Modus-Toggle "Segmente klicken" vs. "Regex (für
Sonderfälle)".** Der Recognizer unterstützt seit BPM-082 bereits beide Methoden
(`Method=segment` Default + `Method=regex` Fallback mit 100ms Timeout). Im Wizard
fehlte bisher die UI-Auswahl — `SelectedRecognitionMethod` war hardcoded auf
`"segment"`.

- **Profil-Name** (TextBox, Pflicht): freier Name, default = Planart-Wert aus Schritt 2.
- **Modus-Toggle** (Pills-Style, segmentiert):
  - `Segmente klicken` (Default, aktiv = Akzent-Background)
  - `Regex (für Sonderfälle)` (Hover-/Klick → wechselt Mode)
- **Segmente-Modus** (Default, BPM-082 Standard):
  - Hint: "Klicke auf die Teile, die bei diesem Profil **immer gleich** sind:"
  - Beispiel-Datei-Anzeige (Consolas, grau).
  - Klickbare Token-Buttons (WrapPanel): unselected = transparent + grauer Border,
    selected = Akzent-Background + weiß. Multi-Select möglich.
  - Erkennungsmuster-Preview-Box:
    - Erkennungsmuster: z.B. `Polierplan` (Akzent, Consolas, bold)
    - Erkennungs-Methode: "Position-genauer Token-Vergleich (Segment N)"
    - Priorität: 100 (Standard)
- **Regex-Modus** (BPM-007.02 Sonderfall, noch nicht gemockt — `🟡 tot` Pfad zu
  `05_Erkennung_Regex.html`):
  - Pattern-TextBox (Consolas, monospace)
  - Live-Syntax-Validierung (BPM-007.03) — bei `ArgumentException` rote Border + Fehler-Text,
    Speichern blockiert
  - Live-Match-Test gegen die Schritt-1-Beispieldatei (✓/✗-Indikator)
  - Anwendungsfälle: Statiknummernkreise (`^5998-2\d{2}_`), Dateien ohne saubere Delimiters
- **BPM-082.04 Erkennungs-Warnung** (orange, wenn variables Segment als
  Erkennungsmuster gewählt — Plannummer, Index, Datum, rein numerisch):
  Soft-Warn (kein Hard-Fail), User darf trotzdem speichern.

Footer: `Profil speichern` (Primary, breit) statt `Weiter →`.

---

### 15.7 Sync-Tab (vertagt)

**Status:** Mockup BPM-005 nach Spike 0 / ADR-053 — **nicht in der heutigen
Mockup-Foundation.**

**Begründung:** Mit der Entscheidung ADR-053 (Server-Sync-Architektur, 2026-04-30,
PostgreSQL + ASP.NET Core, 5–10 User parallel, Pull/Push-Protokoll) ändert sich
der **Inhalt** des Sync-Tabs grundlegend. Ein Mockup auf Basis der alten
Cloud-Ordner-Sync-Mechanik (ADR-004) wäre Wegwerf-Arbeit.

| Vorher (Cloud-Ordner-Sync, ADR-004) | Nachher (Server-Sync, ADR-053) |
|---|---|
| Files-im-Cloud-Ordner-Scanner | Aktive Server-Verbindung (online/offline) |
| Polling-Status | Letzter Push/Pull, Pending Operations |
| keine Multi-User-Sicht | Wer hat zuletzt geändert (5–10 User parallel) |
| keine Konflikt-UI | Konflikt-Anzeige (auch wenn "server gewinnt", User will wissen was überschrieben wurde) |
| — | Re-Sync, Force-Sync, Re-Auth |

Im Mockup ist der Tab `Sync` aktuell ein `🟡 tot`-Pfad mit Hinweis-Text in der
Sitemap: *"BPM-005 — Sync-Mockup nach ADR-053, frühestens nach Spike 0"*.

---

### 15.8 UI-Regeln (allgemein)

**Screen States (5 Pflicht):** Empty, Loading, Error, Partial, Filled.
Max. 1 Primary-Button pro Kontext. BPM Dark Theme Tokens aus `Colors.xaml`,
Icons aus `Icons.xaml`. Sprache Deutsch, de-AT Formate. Interne Status reicher
als UI-Begriffe (z.B. 9 Status-Typen im Code → 4–5 Anzeige-Kategorien in der UI).

**Konsistente Farb-Token in den Mockups:**
- Akzent: `#0078D4` (Primary), `#04395E` (aktiv-dunkel)
- Background: `#1E1E1E` (Surface), `#252526` (elevated), `#2D2D30` (card)
- Border: `#3C3C3C` (default), `#3E3E42` (input)
- Text: `#FFFFFF` (bright), `#CCCCCC` (primary), `#858585` (secondary)
- Status: `#4EC94E` (ok), `#F0AD4E` (warn), `#F44747` (error)
- Field-Type-Token-Farben in Wizard-Schritt 2 / Manuell-Sortieren-Radial:
  `#534AB7` `#0F6E56` `#993C1D` `#185FA5` `#1F7280` `#555` (siehe 15.6.2 / 15.5)

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
│   ├── FileNameParser.cs              ← Segment-Splitting + TokenizationConfig (v2)
│   ├── DocumentTypeRecognizer.cs      ← Dokumenttyp-Erkennung
│   ├── DocumentKeyBuilder.cs          ← document_key deterministisch bilden
│   ├── ImportScanService.cs           ← Eingang rekursiv scannen
│   ├── FileFingerprintService.cs      ← MD5-Hashing (bounded parallel)
│   ├── FileParseService.cs            ← Parser + Recognizer + Feld-Extraktion
│   ├── ImportContextResolver.cs       ← Ordner-Kontext + Stage + Evidence
│   ├── RevisionDecisionService.cs     ← 9-Status Entscheidungsmatrix
│   ├── ImportPlanBuilder.cs           ← Zielpfade berechnen
│   ├── ImportWorkflowService.cs       ← 7-Stufen-Pipeline Orchestrator
│   ├── ImportExecutionService.cs      ← Dateien verschieben + Journal + DB
│   ├── RecoveryDecisionService.cs     ← Recovery-Entscheidung beim App-Start (pending → Reparatur)
│   ├── RecoveryExecutorService.cs     ← Recovery-Aktionen ausführen
│   ├── ProfileManager.cs              ← .bpm/profiles/ CRUD + v1→v2/v2→v3 Migration
│   ├── PatternTemplate.cs             ← (Model) PatternTemplate-Type
│   ├── PatternTemplateService.cs      ← Globale Musterbibliothek
│   ├── PlanIndexManifestService.cs    ← .bpm/plan-index.json (⬜ GEPLANT — optional V1)
│   ├── FileRenamer.cs                 ← RenameSchemaEngine + FileNameSanitizer (⬜ GEPLANT)
│   └── PlanManagerDatabase.cs         ← planmanager.db 6 Tabellen + CRUD
└── BauProjektManager.PlanManager.csproj
```

---

## 17. Implementierungsreihenfolge

| Prio | # | Feature | Status |
|------|---|---------|--------|
| 1 | 18 | Dateinamen-Parser (Segment-Splitting, Domain-Logik) | ✅ v0.24.3 |
| 2 | 19 | Profil-Wizard GUI (5-Schritt: Datei, Segmente, Index, Zielordner, Erkennung) | ✅ v0.24.10 |
| 3 | 20 | Dokumenttyp-Erkennung (prefix/contains) | ✅ v0.25.5 |
| 4 | 21 | PatternTemplates (Vorschlagslogik) | ✅ v0.25.5 |
| 5 | 22 | .bpm/profiles/ (Pro Projekt) — ProfileManager + v1→v2 Migration | ✅ v0.25.8 |
| 6 | 23 | pattern-templates.json (Globale Bibliothek) | ✅ v0.25.5 |
| 7 | 24 | Import-Pipeline 7 Services (Scan→Fingerprint→Parse→Resolve→Key→Decision→Plan) | ✅ v0.25.11 + v0.27.5 (BPM-001 DB-Anbindung) |
| 8 | 25 | Import-Vorschau (DataGrid, 9 Status-Typen) | ✅ v0.25.14 |
| 9 | 26 | Import-Execute (Verschieben, Journal, DB-Update) | ✅ v0.25.15 |
| 10 | 27 | Index-Archivierung (_Archiv/) | ✅ v0.25.15 |
| 11 | 28 | DB-Schema (6 SQLite-Tabellen, planmanager.db) | ✅ v0.25.13 |
| 12 | 29 | Recovery (pending → Reparatur) | ⬜ |
| 13 | 30 | Undo (letzter Import + Preflight) | ⬜ |
| 14 | 31 | Backup vor Import | ⬜ |
| 15 | 32 | Manueller Sortier-Modus + Umbenennung | ⬜ |
| 16 | 33 | Erkennungs-Konflikt (CONFLICT) | ⬜ |

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
| ADR-056 | Segmenttyp-Architektur (BPM-108) — fieldTypeId + SemanticRole |
| ADR-058 | Plan-Archiv-Persistenz (BPM-109) — Drei-Ebenen-Modell + Foundation Slice |

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

### 19.1 Planlisten Import/Export (V1.1 — Details)

**Import-Formate:** Excel (.xlsx), CSV. PDF (Best Effort mit PdfPig) → Post-V1.

**Spalten-Zuordnung:** Angelernt pro Plantyp. User weist Spalten den Feldern zu.

**Abgleich-Ergebnis:**

| Status | Symbol | Bedeutung |
|--------|--------|-----------|
| Aktuell | ✅ | Index stimmt überein |
| Veraltet | ⚠️ | User hat älteren Index |
| Fehlend | ❌ | In Planliste aber nicht im Bestand |
| Extra | ℹ️ | Im Bestand aber nicht in Planliste |

**Export:**
- Plantypen wählen (Checkboxen)
- Spalten wählen (Checkboxen)
- Format: Excel (.xlsx via ClosedXML) oder PDF (via QuestPDF)

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

`.bpm/plan-index.json` = synchronisierter Soll-Bestand.
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
*- .bpm/plan-index.json als Bestandsmanifest (Cloud-synced)*
*- DWG-Veraltet-Warnung (Revisions-Inkonsistenz)*
*- rename_log gestrichen, Felder in import_action_files integriert*
*- MD5 + file_size immer Pflicht*
*- Undo auf letzten Import begrenzt + Preflight*
*- Journal = Execution-Log, relative Pfade*
*- Profil: identityFields, required, indexComparison, recognitionPriority, grouping, schemaVersion*
*- Solution: DocumentKeyBuilder, PlanIndexManifestService*
*- 3 Implementierungs-Disziplinen dokumentiert*
