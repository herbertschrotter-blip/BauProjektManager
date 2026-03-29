# BauProjektManager — Modul: Wetter

**Status:** Später (Phase 4+)  
**Abhängigkeiten:** Einstellungen (Koordinaten pro Projekt)  
**Technologie:** HTTP API (OpenMeteo — kostenlos, kein API-Key)  
**Referenz:** Architektur v1.4, Kapitel 11.4  

---

## 1. Konzept

Wetterdaten pro Baustelle automatisch abrufen. Nutzt die GPS-Koordinaten aus der Projekt-Registry. Zeigt aktuelle Daten + Vorhersage. Wichtig für Bautagebuch (Auto-Befüllung) und Betonierfreigabe.

---

## 2. API: OpenMeteo

Kostenlos, kein API-Key nötig, kein Abo. Open Source Wetter-API.

```
Beispiel-Aufruf:
https://api.open-meteo.com/v1/forecast
  ?latitude=46.95
  &longitude=15.44
  &current=temperature_2m,weathercode,windspeed_10m,precipitation
  &daily=temperature_2m_max,temperature_2m_min,precipitation_sum,weathercode
  &timezone=Europe/Vienna
```

---

## 3. Datenmodell

```csharp
public class WeatherData
{
    public string ProjectId { get; set; }
    public DateTime FetchedAt { get; set; }
    public CurrentWeather Current { get; set; }
    public List<DailyForecast> Forecast { get; set; }  // 3-7 Tage
}

public class CurrentWeather
{
    public double Temperature { get; set; }       // °C
    public string Condition { get; set; }          // "Sonnig", "Regen" etc.
    public int WeatherCode { get; set; }           // WMO Code
    public double WindSpeed { get; set; }          // km/h
    public string WindDirection { get; set; }      // "NW"
    public double Precipitation { get; set; }      // mm
}

public class DailyForecast
{
    public DateTime Date { get; set; }
    public double TemperatureMin { get; set; }
    public double TemperatureMax { get; set; }
    public double PrecipitationSum { get; set; }
    public string Condition { get; set; }
    public bool ConcreteWorkPossible { get; set; }  // Temp > 5°C, kein Starkregen
}
```

---

## 4. Features

| Feature | Beschreibung |
|---------|-------------|
| Aktuelle Daten | Temperatur, Wind, Niederschlag pro Baustelle |
| Vorhersage | 3-7 Tage voraus |
| Betonierfreigabe | Automatische Prüfung: Temp > 5°C, kein Starkregen |
| Dashboard-Widget | Wetter aller aktiven Baustellen |
| Bautagebuch-Auto | Wetterdaten automatisch ins Bautagebuch eintragen |
| Offline-Cache | Letzte Daten lokal cachen für Offline-Betrieb |

---

## 5. Betonierfreigabe

Einfache Regel (konfigurierbar):
- Temperatur > 5°C (Standard, änderbar)
- Kein Starkregen (> 10mm/h)
- Wind < 60 km/h

Wenn Bedingungen nicht erfüllt → Warnung im Dashboard:
```
🌧 Dobl-Zwaring: Morgen 8°C, Regen erwartet
⚠️ Betonieren nicht empfohlen!
```

---

## 6. GUI-Mockup (Dashboard-Widget)

```
┌─ Wetter ────────────────────────────────────────┐
│ Dobl-Zwaring (46.95°N, 15.44°E):                │
│ ☀️ 14°C Sonnig | Wind: 12 km/h NW | 0mm Regen  │
│ Morgen: 🌧 8°C, 15mm Regen                      │
│ ⚠️ Morgen kein Betonieren!                      │
│                                                   │
│ Kapfenberg (47.44°N, 15.29°E):                   │
│ ⛅ 11°C Bewölkt | Wind: 8 km/h W | 0mm          │
│ Morgen: ☀️ 15°C, trocken                        │
│ ✅ Betonieren möglich                            │
└───────────────────────────────────────────────────┘
```

---

## 7. Abhängigkeiten

- **Koordinaten aus Registry:** Projekt muss coordinateEast/North haben
- **Internetverbindung:** Für API-Aufruf nötig (Offline → Cache)
- **Kein API-Key nötig:** OpenMeteo ist kostenlos

---

*Erstellt: 27.03.2026 | Phase 4+ (nach V1)*