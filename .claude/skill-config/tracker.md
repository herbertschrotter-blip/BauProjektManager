# Tracker-Konfiguration – BauProjektManager (BPM)

Für den Skill tracker (Skill-Profil → Tracker → Config). Übernommen am 07.10.2026 aus `projects/bpm/` des Skill-Repos
claude-workbench (dort entfernt). Quelle der Wahrheit ist ClickUp selbst; im Cowork-Chat gilt bei Abweichung der
Memory-Eintrag `[CLICKUP]`.

## Inhalt

- Provider
- Nummernschema und nächste freie Nummer
- Listen und Routing
- Statusmodell
- Felder und Option-IDs
- Titel- und Beschreibungsformat
- Quittungsformat
- Sonderfälle

## Provider

ClickUp · Workspace `90152410319` · Space „BPM“ `901510792907`

## Nummernschema und nächste freie Nummer

- Hauptaufgaben: `BPM-<NNN> | <KÜRZEL> | <Kurzbeschreibung>`, dreistellig, global über alle BPM-Listen, nie
  wiederverwendet. Beispiel: `BPM-016 | PM | Recovery nach Crash`
- Unteraufgaben: `<Parent-NNN>.<Sub-NN> — <Kurzbeschreibung>`, z. B. `016.01 — GetPendingImports() Detail-Infos`
- **Nächste freie Nummer: BPM-131** ← nach jedem `tracker neu` +1 und mit committen

## Listen und Routing

Eine Liste je Modul; `tracker neu: <KÜRZEL> — …` → Liste des Kürzels. Unbekanntes Kürzel → Auswahlfrage mit den Kürzeln
unten, nie raten.

| Modul | Kürzel | Listen-ID |
|-------|--------|----------|
| PlanManager | PM | 901522848097 |
| Docs | DOC | 901522848102 |
| Konzept | KON | 901522848103 |
| Infrastructure | INF | 901522848104 |
| Refactoring | REF | 901522848105 |
| Settings | SET | 901522849234 |
| Dashboard | DASH | 901522850042 |
| Bautagebuch | BTB | 901522850044 |
| Foto | FOTO | 901522850057 |
| Zeiterfassung | ZEIT | 901522850060 |
| KI-Assistent | KI | 901522850064 |
| Outlook | OL | 901522850068 |
| Vorlagen | VORL | 901522850070 |
| Plankopf | PK | 901522850071 |
| GIS | GIS | 901522850073 |
| Wetter | WET | 901522850074 |
| Mobile | MOB | 901522850075 |
| App | APP | (Auto-Create) |
| Domain | DOM | (Auto-Create) |
| Theme | THM | (Auto-Create) |

Skill-Issues und Aufgaben zum Skillsystem gehören nicht hierher, sondern in den Space „Claude Skills Entwicklung“
(Tracker-Config des Skill-Repos claude-workbench, `.claude/skill-config/tracker.md`).

## Statusmodell

| offen | in Arbeit | erledigt | gestrichen/abgelöst |
|---|---|---|---|
| `open` | `in progress` | `done` | `cancelled` |

Status immer kleingeschrieben (die ClickUp-API unterscheidet Groß- und Kleinschreibung).

Gestrichene oder abgelöste Aufgaben (ohne eigene Umsetzung, z. B. in einer anderen Aufgabe aufgegangen) bekommen
`cancelled` statt `done` mit Kommentar; die Begründung steht weiter als kurzer Kommentar, Commit ID und Commit Text
bleiben leer. `done` heißt nur noch: umgesetzt. Der Altbestand (gestrichene Aufgaben auf `done`) wird einmalig
umgestellt. (Audit Review 01, R01-001 E18, entschieden 2026-09-24)

## Felder und Option-IDs

11 Custom Fields auf allen BPM-Listen:

| Feld | ID | Typ | Phase | Inhalt |
|------|----|----|----|--------|
| **Typ** | `f7b8c62a-0783-4b6d-9a95-0ce861eae2ad` | drop_down | neu | Feature/Fix/Refactor/Perf/Docs/Konzept/Meta |
| **Aufwand** | `ecc194e5-2c67-46a7-b4d1-177ce3512c10` | drop_down | neu | S/M/L/XL |
| **Zielversion** | `1fcc0b5a-082c-4c2a-8238-f7ff31b560d4` | short_text | neu | Meilenstein-Tag: v1/v1-nice/post-v1/server/backlog |
| **Komponente** | `b0f37cca-c325-4ac6-9bb8-407c65e4611a` | short_text | neu | Hauptmodul/Datei |
| **Zugehörige Docs** | `421cf05a-d02a-40b9-aca7-1dc086536f36` | text | neu | Doc-Pfade, mehrzeilig |
| **Commit ID** | `a851a04d-f34a-429f-abce-bec0b0b6859f` | short_text | done | 7-char Hash |
| **Commit Text** | `cae9f6cb-e0eb-44aa-81f8-df2e1194111e` | text | done | Erste Zeile Commit-Message |
| **Erledigt** | `457bde4f-c9c1-40ab-b464-897b7f87e6bc` | date | done | YYYY-MM-DD |
| **Chat-Anker temp** | `fef35671-3752-4bf2-bcb8-f29482d3a2d7` | short_text | neu | TEMP-ID aus Ausarbeitungsphase |
| **Chat-Anker erstellt** | `512b8920-e958-4e43-826e-110e3bccdfc2` | short_text | neu | ClickUp-Task-ID beim Anlegen |
| **Chat-Anker erledigt** | `0c72a2ea-630e-4320-9cdb-80a19bc5ebb6` | short_text | done | ClickUp-Task-ID beim Abschluss |

Typ:

| Option | Option-ID | Farbe |
|--------|-----------|-------|
| Feature | `5f4be01a-1b85-42ef-a987-b8ffb16ecdc1` | Grün |
| Fix | `00bb1941-b0a4-4eaf-96c9-116b2a57a8d6` | Rot |
| Refactor | `aa48cd2f-a319-4abf-b6ba-9bbe6f44e2c7` | Blau |
| Perf | `976b3664-5973-4922-8287-4a63101e0c2d` | Orange |
| Docs | `12a547b2-4ebc-4caa-8527-023c66e9f111` | Violett |
| Konzept | `1291a46f-feab-4596-8762-3872e51ee327` | Gelb |
| Meta | `d60891c6-129f-4b99-9e6f-043940541a22` | Grau |

Die Commit-Typen sind Feature/Fix/Change/Refactor/Perf/Docs (wie `CLAUDE.md`). Die Option `Change` fehlt im Dropdown
noch: in ClickUp anlegen und ihre Option-ID hier nachtragen; bis dahin nicht raten. (Audit Review 01, R01-001 E18,
entschieden 2026-09-24)

Aufwand:

| Option | Option-ID | Farbe |
|--------|-----------|-------|
| S (<1h) | `805fd010-b820-46b1-acaa-8b2e047904e6` | Grün |
| M (1-4h) | `0298c9b6-9116-4c9e-bb74-d78a41bfe975` | Gelb |
| L (halber Tag) | `7646181a-b2ff-4eaa-a61b-0d378c0e91d3` | Orange |
| XL (>1 Tag) | `6b7587cc-2031-44c4-8b95-74a2d73c87a7` | Rot |

## Titel- und Beschreibungsformat

Titel siehe Nummernschema. Beschreibung nach dem Template des Skills tracker (`references/create-task.md`).

## Quittungsformat

`✅ BPM-NNN — [BPM-ANCHOR-<task-id>] — <typ>: <kurz>`; die Felder „Chat-Anker erstellt/erledigt“ bekommen denselben
Text mit ASCII-Bindestrich statt Gedankenstrich.

## Sonderfälle

Meilenstein-Tags (nur an Hauptaufgaben, kleingeschrieben):

| Tag | Bedeutung |
|-----|-----------|
| `v1` | Muss für PlanManager V1 fertig sein |
| `v1-nice` | Wäre gut für V1, blockiert aber nicht |
| `post-v1` | Nach V1 Release |
| `server` | Erst wenn Server-Modus gebaut wird |
| `backlog` | Irgendwann, kein Zeitdruck |

- Das Feld **Zielversion** enthält genau einen dieser Meilenstein-Tags (kleingeschrieben), keine konkrete Version
  (die steht im Commit-Präfix und im CHANGELOG). An Hauptaufgaben ist es identisch mit dem Tag; Unteraufgaben tragen
  nur das Feld. (Audit Review 01, R01-001 E18, entschieden 2026-09-24)

- Kein Gedankenstrich `—` in Feldwerten (ClickUp lehnt sonst ab).
- Option-IDs nie raten; bei unbekanntem Wert Auswahlfrage mit den Optionen oben.
