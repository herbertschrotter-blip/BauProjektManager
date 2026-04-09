# BauProjektManager — Modul: Wetter

**Version:** 0.2  
**Status:** Später (Phase 4+)  
**Datum:** 09.04.2026 (erweitert, basierend auf Konzeptsession 08./09.04.2026)  
**Abhängigkeiten:** Einstellungen (Koordinaten pro Projekt), IExternalCommunicationService (ADR-035)  
**Technologie:** Google Sheets (Cloud-Worker) + Google Sheets API + Open-Meteo API + LiveCharts2 (WPF)  
**Referenz:** Architektur v1.4, Kapitel 11.4 · DSVGO-Architektur.md  

---

## 1. Konzept

Wetterdaten pro Baustelle **rund um die Uhr** sammeln — unabhängig davon ob BPM läuft. Ein Google Sheet mit eingebettetem Apps Script fungiert als **kostenloser 24/7 Cloud-Worker**: Es pollt stündlich die Wetter-API und speichert die Rohdaten. BPM synchronisiert beim Start (und optional stündlich) die Daten in die lokale SQLite-Datenbank. Dort stehen sie offline zur Verfügung — für Dashboard, Bautagebuch, Statistiken und Betonierfreigabe.

**Kernprinzip:** Google Sheet sammelt, BPM visualisiert. Offline-first.

---

## 2. Architektur-Übersicht

```
┌──────────────────────────────────────────────────────────┐
│  Google Sheet (24/7 Cloud-Worker, im Google-Account       │
│  des Nutzers)                                             │
│  ┌──────────────┬──────────────┬────────────────────┐     │
│  │ Blatt:       │ Blatt:       │ Blatt:             │     │
│  │ "Projekte"   │ "Rohdaten"   │ "Dashboard"        │     │
│  │ (Baustellen, │ (stündliche  │ (Charts,           │     │
│  │  GPS-Koord.) │  Wetterdaten)│  Zusammenfassungen)│     │
│  └──────┬───────┴───────┬──────┴────────────────────┘     │
│         │               │                                  │
│  Apps Script:           │ Apps Script:                     │
│  "Einrichten"-Menü,     │ Stündlicher Trigger →            │
│  Trigger installieren   │ Open-Meteo API abrufen →         │
│                         │ Rohdaten ins Sheet schreiben      │
└─────────────────────────┼──────────────────────────────────┘
                          │ CSV-Veröffentlichung oder
                          │ Web-App URL (JSON)
                          ▼
┌──────────────────────────────────────────────────────────┐
│  BPM Desktop (WPF)                                        │
│  ┌─────────────────────┐  ┌────────────────────────┐      │
│  │ IWeatherSyncService │──│ IExternalCommunication │      │
│  │ - SyncOnStartup()   │  │ Service (ADR-035)      │      │
│  │ - SyncHourly()      │  │ - Audit-Log            │      │
│  │ - ParseCSV()        │  │ - Kill-Switch           │      │
│  └────────┬────────────┘  │ - ClassA (Wetterdaten)  │      │
│           │               └────────────────────────┘      │
│           ▼                                                │
│  ┌─────────────────────┐  ┌────────────────────────┐      │
│  │ SQLite: Weather-    │  │ WPF Dashboard          │      │
│  │ Tabelle (lokal)     │──│ - LiveCharts2          │      │
│  │ - HourlyData        │  │ - Tages/Wochen/Monat   │      │
│  │ - DailyAggregation  │  │ - Statistiken          │      │
│  │ - ForecastData      │  │ - Animierte Icons      │      │
│  └─────────────────────┘  └────────────────────────┘      │
└──────────────────────────────────────────────────────────┘
```

---

## 3. Google Sheet als Cloud-Worker

### 3.1 Warum Google Sheets?

- **24/7 Betrieb:** Apps Script läuft in Googles Cloud, unabhängig vom PC
- **Kostenlos:** Keine Server-Kosten, kein VPS, keine API-Keys auf BPM-Seite
- **Nutzer-Kontrolle:** Sheet liegt im eigenen Google-Account des Nutzers
- **Visuell:** Nutzer kann Rohdaten auch direkt im Sheet sehen
- **Kein Vendor Lock-in:** BPM funktioniert auch ohne Sheet (Fallback: direkter API-Call wenn BPM läuft)

### 3.2 Sheet-Struktur (Template)

BPM liefert eine .xlsx-Vorlage mit. Der Nutzer importiert sie in Google Sheets.

| Blatt | Inhalt |
|-------|--------|
| **Projekte** | Baustellenname, GPS-Koordinaten (Lat/Lon), aktiv/inaktiv |
| **Rohdaten** | Stündliche Wetterdaten (Datum, Uhrzeit, Temp, Regen, Wind, Humidity, WeatherCode) |
| **API-Calls** | Berechnete API-URLs pro Baustelle (aus Koordinaten generiert) |
| **Dashboard** | Charts, Zusammenfassungen, Tagesübersichten (im Sheet sichtbar) |

> **Hinweis:** API-Keys und sensible Konfiguration werden NICHT in einem Sheet-Blatt gespeichert, sondern im `PropertiesService` von Apps Script (siehe Kapitel 3.5).

### 3.3 Apps Script (eingebettet im Sheet)

Das Script wird mit dem Sheet mitkopiert und enthält:

**Mitkopiert:**
- Script-Code (.gs Dateien)
- HTML-Dateien (Sidebars, Dialoge)
- Sheet-Struktur, Formatierungen, Formeln

**NICHT mitkopiert (einmalige Einrichtung durch Nutzer):**
- Zeitgesteuerte Trigger (1x Klick auf "Einrichten"-Menü → Script erstellt Trigger automatisch)
- Autorisierungen (1x Google-Berechtigungsdialog bestätigen)
- Web-App Deployment (falls JSON-Endpunkt gewünscht)

**Funktionen im Script:**

```
Menü "Wetter":
├── Einrichten          → Installiert stündlichen Trigger, prüft Berechtigungen
├── Jetzt abrufen       → Manueller Sofort-Abruf aller Baustellen
├── Baustelle hinzufügen → Sidebar mit Formular (Name, Lat, Lon)
├── Status prüfen       → Zeigt letzten Abruf, Fehler, Trigger-Status
└── Hilfe               → Anleitung, Troubleshooting
```

### 3.4 Nutzer-Setup-Workflow

1. Nutzer klickt in BPM "Wetter-Modul einrichten"
2. BPM öffnet Google-Template-URL → Nutzer klickt "Kopie erstellen"
3. Nutzer öffnet Menü "Wetter" → "Ersteinrichtung" → **Wizard-Dialog öffnet sich**
4. Wizard: API-Key eingeben (Button öffnet direkt die Key-Seite des Anbieters)
5. Wizard: Erste Baustelle anlegen (Name + Koordinaten)
6. Wizard: Klick "Fertigstellen" → Trigger wird automatisch installiert, Test-Abruf läuft
7. Nutzer kopiert die Sheet-URL / CSV-URL zurück in BPM-Einstellungen
8. BPM synct automatisch

**Geschätzter Aufwand für Nutzer: 5 Minuten einmalig.**

### 3.5 Ersteinrichtungs-Wizard (Apps Script Dialog)

Beim Klick auf Menü "Wetter → Ersteinrichtung" öffnet sich ein modaler HTML-Dialog direkt im Google Sheet. Der Wizard führt den Nutzer in 4 Schritten durch die Konfiguration:

**Schritt 1 — API-Key:**
- Eingabefeld für den Wetter-API-Key
- Button "API-Key holen" öffnet direkt die Registrierungsseite des gewählten Anbieters (z.B. `https://home.openweathermap.org/api_keys`)
- Validierung: Feld darf nicht leer sein

**Schritt 2 — Erste Baustelle:**
- Eingabefelder: Baustellenname, Breitengrad, Längengrad
- Optional: "Von Adresse suchen" (Google Maps Geocoding im Script)
- Eintrag wird automatisch ins "Projekte"-Blatt geschrieben

**Schritt 3 — Test-Abruf:**
- Script macht sofort einen API-Call mit dem Key und den Koordinaten
- Ergebnis wird angezeigt: "✅ 14.2°C, Sonnig — Verbindung funktioniert!"
- Bei Fehler: Klare Meldung ("❌ Ungültiger API-Key" oder "❌ Keine Internetverbindung")

**Schritt 4 — Trigger installieren:**
- Script erstellt automatisch den stündlichen Trigger via `ScriptApp.newTrigger()`
- Bestätigung: "✅ Stündlicher Abruf aktiv. Nächster Abruf in ~60 Minuten."
- Kein manueller Schritt nötig

**Speicherung sensibler Daten — PropertiesService (ENTSCHEIDUNG):**

API-Keys werden NICHT im Sheet gespeichert, sondern im `PropertiesService` von Apps Script. Das ist ein script-interner Key-Value-Store, unsichtbar für den Nutzer, nicht im Sheet einsehbar.

```javascript
// API-Key speichern (im Wizard nach Eingabe)
function saveApiKey(key) {
  PropertiesService.getScriptProperties().setProperty('WEATHER_API_KEY', key);
}

// API-Key abrufen (bei jedem stündlichen Abruf)
function getApiKey() {
  return PropertiesService.getScriptProperties().getProperty('WEATHER_API_KEY');
}

// Trigger automatisch installieren
function installHourlyTrigger() {
  // Bestehende Trigger entfernen (Duplikate vermeiden)
  var triggers = ScriptApp.getProjectTriggers();
  triggers.forEach(function(t) {
    if (t.getHandlerFunction() === 'fetchWeatherData') {
      ScriptApp.deleteTrigger(t);
    }
  });
  // Neuen stündlichen Trigger erstellen
  ScriptApp.newTrigger('fetchWeatherData')
    .timeBased()
    .everyHours(1)
    .create();
}
```

**Vorteile PropertiesService gegenüber verstecktem Sheet-Blatt:**
- Key taucht nirgends im Sheet auf (kein versehentliches Teilen)
- Nur das Script selbst kann den Key lesen
- Wird beim Kopieren des Sheets NICHT mitkopiert (Nutzer muss eigenen Key eingeben)
- Kein Blattschutz-Workaround nötig

**Dialog-Design:**
- Modaler Dialog via `SpreadsheetApp.getUi().showModalDialog()`
- HTML/CSS gestaltet: Schritt-für-Schritt mit Fortschrittsbalken
- Responsive, sauberes Design passend zum Google-Stil
- Validierung in Echtzeit (Key-Format prüfen, Koordinaten-Bereich prüfen)

---

## 4. Wetter-API: Open-Meteo

Kostenlos, kein API-Key nötig, kein Abo. Open Source Wetter-API.

```
Beispiel-Aufruf:
https://api.open-meteo.com/v1/forecast
  ?latitude=46.95
  &longitude=15.44
  &current=temperature_2m,weathercode,windspeed_10m,precipitation
  &hourly=temperature_2m,precipitation,windspeed_10m,relativehumidity_2m
  &daily=temperature_2m_max,temperature_2m_min,precipitation_sum,weathercode
  &timezone=Europe/Vienna
```

**Lizenz-Hinweis:** Open-Meteo ist kostenlos für nicht-kommerzielle Nutzung. Da das Script im Google-Account des *Nutzers* läuft (nicht auf BPM-Servern), ist die Nutzung eine Grauzone. **Alternativen mit explizit kommerziellem Free-Tier:** OpenWeatherMap (1.000 Calls/Tag), Visual Crossing (1.000 Calls/Tag), WeatherAPI.com.

**Attribution erforderlich:** "Wetterdaten: Open-Meteo.com" in BPM anzeigen.

### 4.1 Google Sheets API (BPM → Sheet)

- NuGet: `Google.Apis.Sheets.v4`
- **Kostenlos**, auch kommerziell
- Limits: 300 Read-Requests/Minute/Projekt, unbegrenzt pro Tag
- OAuth 2.0 Authentifizierung (Nutzer autorisiert BPM einmalig)
- BPM kann lesen UND schreiben (z.B. Projektdaten ins Sheet pushen)

**Optionaler Workflow:** BPM schreibt Projekt-Koordinaten direkt ins "Projekte"-Blatt via Sheets API → Sheet pollt automatisch für alle eingetragenen Standorte. Alternativ: Nutzer trägt Koordinaten manuell im Sheet ein (kein API-Zugriff nötig, einfacher).

**DSGVO:** GPS-Koordinaten sind ClassA (keine Personendaten). Baustellenadressen wären ClassB — deshalb nur Koordinaten übertragen, keine Adressen.

---

## 5. BPM-Sync-Architektur

### 5.1 IWeatherSyncService

```csharp
public interface IWeatherSyncService
{
    Task SyncAsync();                          // Hauptmethode: CSV/JSON abrufen, parsen, in DB schreiben
    Task<bool> TestConnectionAsync();          // Prüft ob Sheet erreichbar
    DateTime? LastSyncTime { get; }            // Letzter erfolgreicher Sync
}
```

### 5.2 Sync-Ablauf

1. BPM startet → `IWeatherSyncService.SyncAsync()` wird aufgerufen
2. Service ruft CSV-URL über `IExternalCommunicationService` ab (ClassA, Audit-Log)
3. CSV wird geparst → neue Einträge identifiziert (seit letztem Sync)
4. Neue Einträge werden in SQLite `Weather`-Tabelle geschrieben
5. Optional: Stündlicher Timer wiederholt den Sync

### 5.3 Fallback (ohne Google Sheet)

Wenn kein Sheet konfiguriert oder nicht erreichbar:
- BPM ruft Open-Meteo direkt ab (nur wenn BPM läuft)
- Daten nur während Laufzeit verfügbar, keine 24/7-Sammlung
- Funktioniert als einfacher Offline-Cache

---

## 6. Datenmodell

### 6.1 SQLite-Tabellen

```sql
CREATE TABLE WeatherHourly (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProjectId TEXT NOT NULL,
    Timestamp DATETIME NOT NULL,
    Temperature REAL,           -- °C
    Precipitation REAL,         -- mm
    WindSpeed REAL,             -- km/h
    WindDirection TEXT,          -- "NW"
    Humidity INTEGER,           -- %
    WeatherCode INTEGER,        -- WMO Code
    CloudCover INTEGER,         -- %
    UNIQUE(ProjectId, Timestamp)
);

CREATE TABLE WeatherDaily (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProjectId TEXT NOT NULL,
    Date DATE NOT NULL,
    TemperatureMax REAL,
    TemperatureMin REAL,
    TemperatureAvg REAL,
    PrecipitationSum REAL,
    WindSpeedMax REAL,
    SunshineHours REAL,
    WeatherCode INTEGER,
    IsBadWeather INTEGER DEFAULT 0,   -- Schlechtwettertag
    IsFrostDay INTEGER DEFAULT 0,     -- Min < 0°C
    ConcreteWorkPossible INTEGER DEFAULT 1,
    UNIQUE(ProjectId, Date)
);

CREATE TABLE WeatherForecast (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProjectId TEXT NOT NULL,
    FetchedAt DATETIME NOT NULL,
    ForecastDate DATE NOT NULL,
    TemperatureMax REAL,
    TemperatureMin REAL,
    PrecipitationSum REAL,
    WeatherCode INTEGER,
    Confidence INTEGER           -- Prognose-Sicherheit in %
);
```

### 6.2 C# Domain-Modelle

```csharp
public class WeatherHourlyEntry
{
    public string ProjectId { get; set; }
    public DateTime Timestamp { get; set; }
    public double Temperature { get; set; }
    public double Precipitation { get; set; }
    public double WindSpeed { get; set; }
    public string? WindDirection { get; set; }
    public int Humidity { get; set; }
    public int WeatherCode { get; set; }
}

public class WeatherDailySummary
{
    public string ProjectId { get; set; }
    public DateTime Date { get; set; }
    public double TemperatureMax { get; set; }
    public double TemperatureMin { get; set; }
    public double TemperatureAvg { get; set; }
    public double PrecipitationSum { get; set; }
    public double WindSpeedMax { get; set; }
    public bool IsBadWeather { get; set; }
    public bool IsFrostDay { get; set; }
    public bool ConcreteWorkPossible { get; set; }
}
```

---

## 7. Dashboard-UI (WPF)

### 7.1 Tabs

| Tab | Inhalt |
|-----|--------|
| **Heute** | Stündlicher Temperaturverlauf (animierte Kurve), Stunden-Icons scrollbar, Tageskennzahlen (Höchst/Tiefst/Regen/Wind), externe Links (Regenradar, ZAMG, WetterOnline) |
| **Woche** | Temperaturband (Höchst/Tiefst/Mittel mit Füllfläche), Niederschlag-Balkendiagramm, 7-Tage-Karten mit animiertem Regen-Overlay |
| **Vorschau** | 7-Tage-Prognose mit Konfidenz-Badge (grün/orange/grau), Temperatur-Chart, Baustellenempfehlung ("Mo–Mi gut, Do Regen vermeiden") |
| **Monat** | Kalender-Heatmap (grün=gut, blau=Regen, dunkelblau=Frost), aktueller Tag hervorgehoben |
| **Statistik** | Kennzahlen-Karten (Ø Temp, Max, Min, Gesamtregen, Schlechtwettertage, Frosttage, Sonnenstunden, Arbeitstage), Arbeitstage-Bilanz mit Kreisdiagramm |

### 7.2 Tagesansicht — Klickbare Tageskarten

Oben im Dashboard: 7-Tage-Kartenstrip. Klick auf eine Karte wechselt in die Tagesansicht dieses Tages. Aktueller Tag hervorgehoben, Schlechtwetter-Karten mit animiertem Regen-Overlay.

### 7.3 Externe Links

Direkt aus der Tagesansicht öffenbar (im Standardbrowser via `Process.Start()`):

| Link | URL-Template | Quelle |
|------|-------------|--------|
| Regenradar | `https://www.msn.com/de-at/wetter/radar/{Ort}/lat={Lat}&lon={Lon}` | MSN Wetter |
| Wetterkarte | `https://www.zamg.ac.at/cms/de/wetter/wetterkarte` | GeoSphere Austria |
| Detailprognose | `https://www.wetteronline.de/wetter/{Ort}` | WetterOnline |

URLs werden dynamisch aus den Projektkoordinaten gebaut.

### 7.4 Visualisierungs-Library

**LiveCharts2** (Empfohlen)
- GitHub: https://github.com/beto-rodriguez/LiveCharts2
- NuGet: `LiveChartsCore.SkiaSharpView.WPF` (v2.0.0)
- Lizenz: MIT — kommerziell frei
- Eingebaute Animationen, Tooltips, interaktive Charts, Cross-Platform

**ScottPlot** (Fallback)
- GitHub: https://github.com/ScottPlot/ScottPlot
- NuGet: `ScottPlot.WPF` (v5.1.57)
- Lizenz: MIT
- Sehr performant bei großen Datenmengen, eher wissenschaftlicher Stil

### 7.5 Icon-Packs

**Wetter-Icons:**
- **Bas Milius Weather Icons** — https://github.com/basmilius/weather-icons — Animierte SVGs, Filled/Outlined, kostenlos
- **amCharts Animated SVG** — https://www.amcharts.com/free-animated-svg-weather-icons/ — CC BY 4.0

**Allgemeine App-Icons:**
- **MahApps.Metro.IconPacks** — NuGet, direkt als WPF-Controls, MIT-Lizenz
- **Fluent UI Icons (Microsoft)** — MIT, passt zum Windows-Look
- **Tabler Icons** — 5.800+ MIT-lizenzierte SVGs

---

## 8. Betonierfreigabe

Einfache Regel (konfigurierbar in Einstellungen):

| Bedingung | Standard | Änderbar |
|-----------|----------|----------|
| Temperatur | > 5°C | Ja |
| Starkregen | < 10 mm/h | Ja |
| Wind | < 60 km/h | Ja |

Wenn Bedingungen nicht erfüllt → Warnung in Tagesansicht und Dashboard:
```
🌧 Dobl-Zwaring: Morgen 8°C, Regen erwartet
⚠️ Betonieren nicht empfohlen!
```

---

## 9. Bestehendes Google Sheet

Herbert hat bereits ein funktionierendes Wetter-Sheet: **"Historische Wetterdaten Baustellen"**

**Bestehende Struktur:**
- Steuerung: Datum, Baustelle (ÖWG Dobl-Zwaring), Anzahl Tage
- Links: "Live Wetterdaten öffnen", "Live Regen Radar öffnen"
- Daten: Datum, Max °C, Min °C, Mittel °C, Regen mm/h
- Custom-Menü: Diagramm | Orte | Wetter | Daten | Widget | Hilfe
- Apps Script bereits eingebettet mit Menü, Sidebars, Triggern

**Nächster Schritt:** Bestehendes Sheet als Grundlage für das baulotse-Template nehmen und generalisieren (mehrere Baustellen, konfigurierbar, CSV-Veröffentlichung).

---

## 10. Statistiken & Kennzahlen

Berechnungen aus `WeatherDaily`-Tabelle:

| Kennzahl | Berechnung |
|----------|-----------|
| Ø Temperatur | AVG(TemperatureAvg) pro Monat |
| Max-Temperatur | MAX(TemperatureMax) + Datum |
| Min-Temperatur | MIN(TemperatureMin) + Datum |
| Gesamtregen | SUM(PrecipitationSum) pro Monat |
| Schlechtwettertage | COUNT(IsBadWeather = 1): Regen > 5mm ODER Wind > 40 km/h |
| Frosttage | COUNT(IsFrostDay = 1): Min < 0°C |
| Sonnenstunden | SUM(SunshineHours) geschätzt aus WeatherCode |
| Arbeitstage | Werktage − Schlechtwettertage − Feiertage |

---

## 11. DSGVO / Datenschutz

| Aspekt | Bewertung |
|--------|-----------|
| Wetterdaten | ClassA — keine Personendaten, unkritisch |
| GPS-Koordinaten | ClassA — keine Personenzuordnung |
| Baustellenadressen | ClassB — NICHT an Google übertragen, nur Koordinaten |
| API-Calls | Über IExternalCommunicationService mit Audit-Log und Kill-Switch |
| Google Sheet | Liegt im Account des Nutzers, BPM hat keinen eigenen Google-Account |
| Attribution | "Wetterdaten: Open-Meteo.com" in BPM anzeigen |

---

## 12. Rechtliche Aspekte

| Thema | Status |
|-------|--------|
| Open-Meteo Nutzung | Kostenlos für nicht-kommerzielle Nutzung. Nutzer ruft API über eigenen Account ab. Attribution erforderlich. |
| Google Sheets API | Kostenlos, kommerziell erlaubt. 300 Reads/Min. Nutzer autorisiert per OAuth. |
| Google Apps Script | Erlaubt. Nutzer führt Script in eigenem Account aus. BPM liefert nur Vorlage. |
| LiveCharts2 | MIT-Lizenz — kommerziell frei, Lizenzhinweis beilegen |
| ScottPlot | MIT-Lizenz — kommerziell frei, Lizenzhinweis beilegen |
| Bas Milius Icons | MIT-Lizenz — kostenlos, Attribution im Über-Dialog |
| amCharts Icons | CC BY 4.0 — kommerziell frei mit Attribution |

---

## 13. Implementierungs-Phasen

### Phase 1: Sheet-Sync + Tabelle (Minimum Viable)
- Google Sheet Template erstellen (generalisiert aus bestehendem Sheet)
- `IWeatherSyncService` implementieren (CSV abrufen, parsen)
- SQLite `WeatherHourly` + `WeatherDaily` Tabellen
- Einfache Tabellenansicht in BPM (keine Charts)

### Phase 2: Dashboard + Charts
- LiveCharts2 einbinden
- Tagesansicht mit stündlicher Kurve
- Wochenansicht mit Temperaturband + Regenbalken
- 7-Tage-Kartenstrip

### Phase 3: Erweiterte Features
- Vorschau-Tab mit Konfidenz-Badges
- Monats-Kalender-Heatmap
- Statistik-Tab mit Kennzahlen
- Betonierfreigabe-Warnung
- Animierte Wetter-Icons (Bas Milius)
- Baustellenempfehlung

### Phase 4: Google Sheets API Integration
- BPM schreibt Projektdaten direkt ins Sheet via Sheets API
- Automatische Baustellen-Synchronisation (BPM ↔ Sheet)
- Setup-Wizard in BPM mit Schritt-für-Schritt-Anleitung

---

## 14. Abhängigkeiten

| Komponente | Benötigt |
|-----------|----------|
| Koordinaten aus Projekt-DB | Projekt muss Lat/Lon haben |
| Internetverbindung | Für Sync (Offline → lokaler Cache) |
| Google-Account (Nutzer) | Für Sheet als Cloud-Worker |
| IExternalCommunicationService | ADR-035, für alle externen HTTP-Calls |
| LiveCharts2 NuGet | Für Visualisierung |
| Google.Apis.Sheets.v4 NuGet | Für Sheets API (Phase 4) |

---

## 15. Offene Entscheidungen

- [ ] Open-Meteo vs. OpenWeatherMap vs. Visual Crossing als API-Quelle
- [ ] CSV-Veröffentlichung vs. Web-App URL (JSON) als Sync-Methode
- [ ] LiveCharts2 vs. ScottPlot vs. eigene WPF-Visualisierung
- [ ] Sheets API Integration (Phase 4) vs. nur manueller Koordinaten-Eintrag im Sheet
- [ ] Lottie-Animationen in WPF evaluieren (SkiaSharp.Lottie / LottieSharp)

---

*Erstellt: 27.03.2026 | Erweitert: 09.04.2026 (Konzeptsession Server/Sheets/Dashboard)*  
*Mockup: baulotse-wetter-dashboard-v2.jsx (React-Prototyp, 09.04.2026)*