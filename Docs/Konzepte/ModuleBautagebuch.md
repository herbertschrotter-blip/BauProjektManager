---
doc_id: konzept-bautagebuch
doc_type: concept
authority: secondary
status: active
owner: herbert
topics: [bautagebuch, tagesprotokoll, auto-befüllung, wetter, personal, export]
read_when: [bautagebuch-feature, tagesprotokoll, auto-befüllung, export-word-excel]
related_docs: [architektur, db-schema, konzept-mobile, konzept-zeiterfassung, konzept-wetter]
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: Konzept für tägliches Baustellenprotokoll mit Auto-Befüllung aus anderen Modulen
- Autorität: secondary
- Lesen wenn: Bautagebuch-Feature, Tagesprotokoll, Auto-Befüllung, Export Word/Excel
- Nicht zuständig für: Wetter-API (→ ModuleWetter.md), Zeiterfassung (→ ModuleZeiterfassung.md)
- Kapitel:
  - 1. Zweck und Zielzustand
  - 2. Datenmodell (geplant)
  - 3. Workflow
  - 4. Technische Umsetzung
  - 5. Abhängigkeiten
  - 6. No-Gos / Einschränkungen
  - 7. Offene Fragen
- Pflichtlesen: keine
- Fachliche Invarianten:
  - Tageseinträge in SQLite, nicht JSON-Dateien (Abfragen nötig)
  - Auto-Befüllung ist optional — jedes Quellmodul kann fehlen
  - Export in 3 Formate: Word (.docx), Excel (.xlsx), PDF

---

# BauProjektManager — Modul: Bautagebuch

**Status:** Nach V1 (Phase 3-4)  
**Version:** 1.1 (Refactoring auf DOC-STANDARD)  
**Abhängigkeiten:** Einstellungen, PlanManager, optional Wetter-Modul  

---

## 1. Zweck und Zielzustand

Tägliches Baustellenprotokoll mit automatischer Vorbefüllung. Das Modul zieht Daten aus allen anderen Modulen zusammen — der User ergänzt nur was fehlt.

### Auto-Befüllung aus:

```
Bautagebuch-Eintrag wird befüllt aus:
├── Registry           → Projektname, Adresse, Beteiligte
├── Wetter-Modul       → Temperatur, Niederschlag, Wind (API)
├── Stundenzettel      → Personal, Stunden (Excel Import)
├── Arbeitseinteilung  → Firma, Gewerk (Excel Import)
├── PlanManager        → Neue/aktualisierte Pläne des Tages
├── Foto-Modul         → OneDrive-Fotos des Tages
└── User-Eingaben      → Tätigkeiten, Vorkommnisse, Material, Maschinen
```

---

## 2. Datenmodell (geplant)

Tageseinträge in SQLite (lokal). Nicht in JSON-Dateien — weil über Monate viele Einträge entstehen und Abfragen nötig sind (z.B. "alle Tage mit Betonage", "Gesamtstunden Firma Müller").

### Tageseintrag-Struktur

```json
{
  "projectId": "proj_202512_dobl",
  "date": "2026-03-27",
  "createdAt": "2026-03-27T16:30:00",
  "lastModified": "2026-03-27T17:15:00",
  "createdBy": "HERBERT-PC",

  "weather": {
    "temperature": 14,
    "temperatureMin": 5,
    "temperatureMax": 16,
    "condition": "Sonnig",
    "wind": "12 km/h NW",
    "precipitation": 0,
    "source": "auto",
    "fetchedAt": "2026-03-27T08:00:00"
  },

  "personnel": [
    {
      "company": "Baufirma Müller GmbH",
      "trade": "Maurer",
      "count": 4,
      "hours": 8,
      "note": ""
    }
  ],

  "planUpdates": [
    {
      "planNumber": "S-103",
      "action": "indexUpdate",
      "oldIndex": "C",
      "newIndex": "D",
      "planType": "Polierplan"
    }
  ],

  "activities": "Betonage Decke über EG Haus 64...",
  "incidents": "Lieferverzögerung Bewehrungsstahl BSt 550...",
  "materials": "18m³ C25/30 XC2, Lieferwerk Beton Graz",
  "machinery": "Betonpumpe 36m (Fa. Kern), Kran LTM 1060",
  "photos": ["IMG_20260327_0914.jpg", "IMG_20260327_1042.jpg"],
  "notes": "Nächste Woche: Schalung OG Haus 66 beginnen."
}
```

---

## 3. Workflow

### GUI-Mockup

```
╔══════════════════════════════════════════════════════════════════╗
║  Bautagebuch — 27.03.2026                        [◀] [▶] [📅] ║
╠══════════════════════════════════════════════════════════════════╣
║  Projekt: [ 202512_ÖWG-Dobl-Zwaring ▼ ]                        ║
║                                                                  ║
║  ── Wetter (automatisch) ──────────────────────────────────     ║
║  ☀️ 14°C Sonnig | Wind: 12 km/h NW | Niederschlag: 0mm         ║
║  Quelle: OpenMeteo API, 08:00              [✏️ Manuell ändern] ║
║                                                                  ║
║  ── Personal ──────────────────────────────────────────────     ║
║  ╔═══════════════════════╦══════════╦════════╦════════╗         ║
║  ║ Firma                 ║ Gewerk   ║ Anzahl ║ Stunden║         ║
║  ╠═══════════════════════╬══════════╬════════╬════════╣         ║
║  ║ Baufirma Müller GmbH  ║ Maurer   ║   4    ║   8    ║         ║
║  ║ Elektro Schmidt        ║ Elektro  ║   2    ║   6    ║         ║
║  ║ Installationen Gruber  ║ HKLS     ║   3    ║   8    ║         ║
║  ╚═══════════════════════╩══════════╩════════╩════════╝         ║
║  [ + Firma hinzufügen ]  [ Aus Stundenzettel laden ]            ║
║                                                                  ║
║  ── Tätigkeiten / Vorkommnisse / Material / Maschinen ────     ║
║  (Freitext-Felder)                                               ║
║                                                                  ║
║  ── Pläne heute (automatisch aus PlanManager) ─────────────     ║
║  📈 S-103: Index C → D (Polierplan TG)                          ║
║  🆕 S-113-A: Neu (Polierplan 2OG)                               ║
║                                                                  ║
║  ── Fotos des Tages ───────────────────────────────────────     ║
║  (Thumbnails aus OneDrive/Fotos nach Datum)                      ║
║                                                                  ║
║  [ Speichern ] [ Export: Word ▼ ] [ Vorheriger Tag kopieren ]   ║
╚══════════════════════════════════════════════════════════════════╝
```

### Features

| Feature | Beschreibung |
|---------|-------------|
| Tagesnavigation | [◀] [▶] Pfeile + Kalender-Picker [📅] |
| Vortag kopieren | Kopiert Personal + Maschinen vom Vortag (häufig gleich) |
| Auto-Wetter | Holt Wetterdaten automatisch beim Öffnen (wenn Wetter-Modul aktiv) |
| Auto-Pläne | Zeigt automatisch Planänderungen des Tages aus PlanManager |
| Auto-Fotos | Sucht Cloud-Speicher-Fotos nach Datum (wenn Foto-Modul aktiv) |
| Stundenzettel-Import | Lädt Personal aus Excel-Stundenzettel |
| Firmenliste merken | Häufig verwendete Firmen/Gewerke als Vorauswahl |
| Freitext-Felder | Tätigkeiten, Vorkommnisse, Material, Maschinen, Notizen |
| Multi-Export | Word, Excel, PDF in einem Klick |

---

## 4. Technische Umsetzung

### Export-Formate

| Format | Technologie | Zweck |
|--------|-------------|-------|
| Word (.docx) | COM Interop → Vorlage (.dotx) befüllen | Professionelles Layout, Druck, Bauherr |
| Excel (.xlsx) | ClosedXML | Tabellarisch, Auswertung über Zeitraum |
| PDF | QuestPDF | Direkter Druck, digitales Archiv |

### Word-Export Workflow

1. User hat Word-Vorlage in `Vorlagen/Word/Bautagebuch_Vorlage.dotx`
2. Vorlage enthält Platzhalter (Bookmarks oder Content Controls)
3. C#-App öffnet Vorlage über COM Interop
4. Füllt Platzhalter mit Tageseintrag-Daten
5. Speichert als .docx im Projektordner `Protokolle/`

### Excel-Export

Ein Blatt pro Tag oder ein Blatt mit allen Tagen als Zeilen. Spalten: Datum, Wetter, Personal (Summe), Tätigkeiten, Vorkommnisse. Für Monats-/Quartalsberichte an Bauherr.

---

## 5. Abhängigkeiten

| Abhängigkeit | Pflicht? | Wenn nicht vorhanden |
|---|---|---|
| Einstellungen (Registry) | Ja | Projektname, Adresse fehlen |
| PlanManager | Nein | "Pläne heute" Abschnitt bleibt leer |
| Wetter-Modul | Nein | Wetter manuell eintragen |
| Foto-Modul | Nein | Fotos manuell auswählen |
| Excel-Stundenzettel | Nein | Personal manuell eingeben |

---

## 6. No-Gos / Einschränkungen

- Kein Ersatz für rechtlich verbindliches Bautagebuch (ÖNORM B 2110) — BPM ist Hilfsmittel, nicht Rechtsprodukt
- Keine automatische Unterschrift / digitale Signatur in V1
- Kein Multi-User-Editing am gleichen Tageseintrag (→ DatenarchitekturSync: diary_days + diary_notes Aggregate-Split)

---

## 7. Offene Fragen

- Soll der Export auch als ÖNORM-konformes Formular möglich sein?
- Wie wird mit nachträglichen Korrekturen umgegangen? (Versionierung der Einträge?)
- Soll es eine "Wochenansicht" geben die mehrere Tage zusammenfasst?

---

*Erstellt: 27.03.2026 | Phase 3-4 (nach V1)*

*Änderungen v1.0 → v1.1 (11.04.2026):*
*- Frontmatter + AI-Quickload ergänzt (DOC-STANDARD)*
*- Kapitelstruktur auf concept-Vorlage refactort*
*- Kap. 6 (No-Gos) und Kap. 7 (Offene Fragen) als Skelett ergänzt*
*- Kein Inhalt gelöscht — nur umgruppiert*
