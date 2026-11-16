# Daten

Erzeugt von `tools/datenliste.py` aus einem leeren Schema (bpm.db + planmanager.db, Stand b5d857d4). Nur Aufbau, keine Inhalte.

## D1 bpm.db – Projekte und Kunden

| Nr. | Name | Zweck | Felder | Verweist auf |
|---|---|---|---|---|
| D1.1 | `clients` | Auftraggeber (Firma, Kontakt) | 12 | – |
| D1.2 | `projects` | Projekte mit Stammdaten, Pfaden und Status | 41 | D1.1 |
| D1.3 | `project_participants` | Beteiligte je Projekt (Rolle, Firma, Kontakt) | 15 | D1.2 |
| D1.4 | `project_links` | Portale und Links je Projekt | 12 | D1.2 |

## D2 bpm.db – Bauwerk

| Nr. | Name | Zweck | Felder | Verweist auf |
|---|---|---|---|---|
| D2.1 | `building_parts` | Bauteile je Projekt (Kürzel, Bauwerkstyp, ±0,00) | 14 | D1.2 |
| D2.2 | `building_levels` | Geschosse je Bauteil (Höhen, Präfix) | 16 | D2.1 |

## D3 bpm.db – Dokumenttypen

| Nr. | Name | Zweck | Felder | Verweist auf |
|---|---|---|---|---|
| D3.1 | `document_types` | Dokumenttypen je Projekt mit Ablage-Ordner (ADR-061) | 16 | D1.2 |
| D3.2 | `document_type_categories` | Kategorien je Dokumenttyp | 11 | D3.1 |

## D4 bpm.db – Segmenttypen

| Nr. | Name | Zweck | Felder | Verweist auf |
|---|---|---|---|---|
| D4.1 | `segment_type_groups` | Gruppen der Segmenttypen (ADR-056) | 15 | – |
| D4.2 | `segment_types` | Segmenttypen für die Planerkennung | 21 | D4.1 |

## D5 planmanager.db – Planarchiv (je Projekt)

| Nr. | Name | Zweck | Felder | Verweist auf |
|---|---|---|---|---|
| D5.1 | `plan_documents` | Logisches Plandokument über alle Revisionen (ADR-058) | 17 | – |
| D5.2 | `plan_revisions` | Revisionen (Index) je Plandokument | 17 | D5.1, D6.1 |
| D5.3 | `plan_files` | Dateien mit Pfad und MD5 | 9 | – |
| D5.4 | `revision_file_links` | Zuordnung Datei zu Revision (PDF, DWG) | 4 | D5.2, D5.3 |
| D5.5 | `plan_document_segments` | Segmentwerte je Plandokument (Lernquelle der Erkennung) | 12 | D5.1 |
| D5.6 | `plan_document_tags` | Schlagworte je Plandokument | 8 | D5.1 |
| D5.7 | `plan_revision_events` | Verlauf je Revision (Import, Verschieben, Ersetzen) | 13 | D5.2, D6.1 |
| D5.8 | `plan_context_links` | Bezüge zu Bauteil, Geschoss und Kontext | 14 | D5.1, D5.2 |

## D6 planmanager.db – Import-Journal (je Projekt)

| Nr. | Name | Zweck | Felder | Verweist auf |
|---|---|---|---|---|
| D6.1 | `import_journal` | Importläufe mit Status (ADR-064, Undo und Recovery) | 9 | – |
| D6.2 | `import_actions` | Aktionen je Importlauf | 19 | D6.1 |
| D6.3 | `import_action_files` | Dateischritte je Aktion | 11 | D6.2 |

## D7 Schema-Verwaltung

| Nr. | Name | Zweck | Felder | Verweist auf |
|---|---|---|---|---|
| D7.1 | `schema_version` | Schema-Stand von bpm.db | 1 | – |
| D7.2 | `planmanager.schema_version` | Schema-Stand von planmanager.db | 1 | – |

## S1 Gerät (LocalAppData\BauProjektManager)

| Nr. | Name | Zweck | Felder | Verweist auf |
|---|---|---|---|---|
| S1.1 | `device-settings.json` | Geräteeinstellungen (Basisordner, Fenster) | – | – |
| S1.2 | `Logs/BPM_*.log` | Protokoll (Serilog, ohne Personendaten) | – | – |

## S2 Cloud-Ordner (geteilt)

| Nr. | Name | Zweck | Felder | Verweist auf |
|---|---|---|---|---|
| S2.1 | `shared-config.json` | Geteilte Einstellungen und Standard-Ordnervorlage | – | – |
| S2.2 | `registry.json` | Projekt-Registry, flach für VBA/Excel | – | – |
| S2.3 | `pattern-templates.json` | Bibliothek der Plan-Muster (PatternTemplate) | – | – |

## S3 Projektordner (.bpm)

| Nr. | Name | Zweck | Felder | Verweist auf |
|---|---|---|---|---|
| S3.1 | `.bpm/manifest.json` | Projekt-Ausweis: Identität und Modul-Flags (ADR-046) | – | – |
| S3.2 | `.bpm/project.json` | Vollständiger Projektexport für Import und Übergabe | – | – |
| S3.3 | `.bpm/profiles/*.json` | Erkennungsprofil je Dokumenttyp (ADR-010) | – | – |

## S4 Speicher (nur während der Sitzung)

| Nr. | Name | Zweck | Felder | Verweist auf |
|---|---|---|---|---|
| S4.1 | `PendingAssignmentStore` | Vorgemerkte Zuordnungen bis „Import bestätigen“ (auch Claude-Vorschläge) | – | – |
