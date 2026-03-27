# BauProjektManager — Modul: Bautagebuch

**Status:** Nach V1 (Phase 3-4)  
**Abhängigkeiten:** Einstellungen, PlanManager, optional Wetter-Modul  
**Referenz:** Architektur v1.4, Kapitel 11.2  

---

## 1. Konzept

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

## 2. GUI-Mockup

```
╔══════════════════════════════════════════════════════════════════╗
║  Bautagebuch — 27.03.2026                        [◀] [▶] [📅] ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
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
║  ── Tätigkeiten ───────────────────────────────────────────     ║
║  ┌──────────────────────────────────────────────────────────┐   ║
║  │ Betonage Decke über EG Haus 64, Bewehrungsabnahme       │   ║
║  │ durch Statiker Ing. Tschom. Nachbestellung Abstandhalter│   ║
║  └──────────────────────────────────────────────────────────┘   ║
║                                                                  ║
║  ── Besondere Vorkommnisse ────────────────────────────────     ║
║  ┌──────────────────────────────────────────────────────────┐   ║
║  │ Lieferverzögerung Bewehrungsstahl BSt 550 — 2 Stunden   │   ║
║  │ Wartezeit. Lieferwerk: Marienhütte Graz.                │   ║
║  └──────────────────────────────────────────────────────────┘   ║
║                                                                  ║
║  ── Material / Maschinen ──────────────────────────────────     ║
║  Material:  [ 18m³ C25/30 XC2, Lieferwerk Beton Graz      ]    ║
║  Maschinen: [ Betonpumpe 36m (Fa. Kern), Kran LTM 1060    ]    ║
║                                                                  ║
║  ── Pläne heute (automatisch aus PlanManager) ─────────────     ║
║  📈 S-103: Index C → D (Polierplan TG)                          ║
║  🆕 S-113-A: Neu (Polierplan 2OG)                               ║
║  ℹ️ Keine Planlisten-Änderungen                                  ║
║                                                                  ║
║  ── Fotos des Tages ───────────────────────────────────────     ║
║  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐                           ║
║  │ 📷   │ │ 📷   │ │ 📷   │ │ 📷   │  12 Fotos gefunden       ║
║  │09:14 │ │10:42 │ │13:07 │ │15:31 │  (OneDrive/Fotos/27.03.) ║
║  └──────┘ └──────┘ └──────┘ └──────┘                           ║
║  [ Alle anzeigen ]  [ + Foto manuell wählen ]                   ║
║                                                                  ║
║  ── Notizen ───────────────────────────────────────────────     ║
║  ┌──────────────────────────────────────────────────────────┐   ║
║  │ Nächste Woche: Schalung OG Haus 66 beginnen.            │   ║
║  │ Bewehrungsabnahme OG H64 am Mittwoch geplant.           │   ║
║  └──────────────────────────────────────────────────────────┘   ║
║                                                                  ║
║  [ Speichern ] [ Export: Word ▼ ] [ Vorheriger Tag kopieren ]   ║
╚══════════════════════════════════════════════════════════════════╝
```

---

## 3. Tageseintrag-Struktur (JSON in SQLite oder als Datei)

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
    },
    {
      "company": "Elektro Schmidt",
      "trade": "Elektriker",
      "count": 2,
      "hours": 6,
      "note": "Nur vormittags"
    },
    {
      "company": "Installationen Gruber",
      "trade": "HKLS",
      "count": 3,
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
    },
    {
      "planNumber": "S-113",
      "action": "new",
      "newIndex": "A",
      "planType": "Polierplan"
    }
  ],

  "activities": "Betonage Decke über EG Haus 64, Bewehrungsabnahme durch Statiker Ing. Tschom. Nachbestellung Abstandhalter.",
  "incidents": "Lieferverzögerung Bewehrungsstahl BSt 550 — 2 Stunden Wartezeit. Lieferwerk: Marienhütte Graz.",
  "materials": "18m³ C25/30 XC2, Lieferwerk Beton Graz",
  "machinery": "Betonpumpe 36m (Fa. Kern), Kran LTM 1060",

  "photos": [
    "IMG_20260327_0914.jpg",
    "IMG_20260327_1042.jpg",
    "IMG_20260327_1307.jpg",
    "IMG_20260327_1531.jpg"
  ],

  "notes": "Nächste Woche: Schalung OG Haus 66 beginnen. Bewehrungsabnahme OG H64 am Mittwoch."
}
```

---

## 4. Export-Formate

| Format | Technologie | Zweck |
|--------|-------------|-------|
| Word (.docx) | COM Interop → Vorlage (.dotx) befüllen | Professionelles Layout, Druck, Bauherr |
| Excel (.xlsx) | ClosedXML | Tabellarisch, Auswertung über Zeitraum |
| PDF | QuestPDF | Direkter Druck, digitales Archiv |

### Word-Export Workflow:
1. User hat Word-Vorlage in `Vorlagen/Word/Bautagebuch_Vorlage.dotx`
2. Vorlage enthält Platzhalter (Bookmarks oder Content Controls)
3. C#-App öffnet Vorlage über COM Interop
4. Füllt Platzhalter mit Tageseintrag-Daten
5. Speichert als .docx im Projektordner `Protokolle/`

### Excel-Export:
- Ein Blatt pro Tag oder ein Blatt mit allen Tagen als Zeilen
- Spalten: Datum, Wetter, Personal (Summe), Tätigkeiten, Vorkommnisse
- Für Monats-/Quartalsberichte an Bauherr

---

## 5. Features

| Feature | Beschreibung |
|---------|-------------|
| Tagesnavigation | [◀] [▶] Pfeile + Kalender-Picker [📅] |
| Vortag kopieren | Kopiert Personal + Maschinen vom Vortag (häufig gleich) |
| Auto-Wetter | Holt Wetterdaten automatisch beim Öffnen (wenn Wetter-Modul aktiv) |
| Auto-Pläne | Zeigt automatisch Planänderungen des Tages aus PlanManager |
| Auto-Fotos | Sucht OneDrive-Fotos nach Datum (wenn Foto-Modul aktiv) |
| Stundenzettel-Import | Lädt Personal aus Excel-Stundenzettel |
| Firmenliste merken | Häufig verwendete Firmen/Gewerke als Vorauswahl |
| Freitext-Felder | Tätigkeiten, Vorkommnisse, Material, Maschinen, Notizen |
| Multi-Export | Word, Excel, PDF in einem Klick |

---

## 6. Persistenz

Tageseinträge werden in SQLite gespeichert (lokal, `%LocalAppData%`). Nicht in JSON-Dateien — weil über Monate viele Einträge entstehen und Abfragen nötig sind (z.B. "alle Tage mit Betonage", "Gesamtstunden Firma Müller").

---

## 7. Abhängigkeiten

| Abhängigkeit | Pflicht? | Wenn nicht vorhanden |
|---|---|---|
| Einstellungen (Registry) | Ja | Projektname, Adresse fehlen |
| PlanManager | Nein | "Pläne heute" Abschnitt bleibt leer |
| Wetter-Modul | Nein | Wetter manuell eintragen |
| Foto-Modul | Nein | Fotos manuell auswählen |
| Excel-Stundenzettel | Nein | Personal manuell eingeben |

---

*Erstellt: 27.03.2026 | Phase 3-4 (nach V1)*