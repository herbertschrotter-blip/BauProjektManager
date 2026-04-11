---
doc_id: konzept-gis
doc_type: concept
authority: secondary
status: active
owner: herbert
topics: [gis, koordinaten, kataster, epsg-31258, projnet, geodaten]
read_when: [gis-feature, koordinaten, kataster-abfrage, karten-integration]
related_docs: [architektur, db-schema, konzept-foto]
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: Konzept für GIS-Integration — Katasterdaten, Koordinaten automatisch befüllen
- Autorität: secondary
- Lesen wenn: GIS-Feature, Koordinaten, Kataster-Abfrage, Karten-Integration
- Nicht zuständig für: Foto-Geodaten (→ ModuleFoto.md)
- Pflichtlesen: keine
- Fachliche Invarianten:
  - Offline-Koordinatentransformation über ProjNet (EPSG:31258↔WGS84)
  - Kataster-API nur bei Verbindung — Fallback auf manuelle Eingabe

---

﻿# BauProjektManager — Konzept: GIS-Integration

**Erstellt:** 29.03.2026  
**Version:** 0.1 (Konzeptentwurf)  
**Status:** Zukunftsidee — Umsetzung nach V1  
**Abhängigkeit:** HttpClient (.NET), ProjectLocation (Domain-Modell bereits vorbereitet)  
**Verwandte ADR:** —  
**Datenquelle:** GIS Steiermark (ArcGIS REST FeatureServer), optional Google Maps API

---

## 1. Zielsetzung

### Problem

Beim Anlegen eines neuen Projekts tippt Herbert Grundstücksdaten manuell ein: KG-Nummer, KG-Name, Grundstücksnummer, Koordinaten, Gemeinde, Bezirk. Diese Daten stehen alle in öffentlichen GIS-Datenbanken — sie müssten nicht abgetippt werden.

### Lösung

BPM fragt GIS-Dienste direkt ab und befüllt `ProjectLocation`-Felder automatisch:

- **Adresse eingeben** → Google Maps API → PLZ, Ort, Gemeinde, Bezirk, Koordinaten
- **Koordinaten eingeben** → GIS Steiermark API → KG-Nummer, KG-Name, Grundstücksnummern, Grundstücksgrenzen
- **Grundstück klicken** (Zukunft, Dashboard-Karte) → alle Daten auf einmal

### Warum kein REST Backend?

BPM ist eine Desktop-App. Die GIS-Abfrage geht direkt aus C# (HttpClient) an die externe API. Kein eigener Server nötig.

---

## 2. Bereits vorbereitete Felder im Domain-Modell

`ProjectLocation` hat diese Felder schon (aus ADR-003):

```csharp
public class ProjectLocation
{
    // Adresse (aufgeteilt für Google Maps API)
    public string Street { get; set; }           // "Hauptstraße"
    public string HouseNumber { get; set; }      // "15"
    public string PostalCode { get; set; }       // "8143"
    public string City { get; set; }             // "Dobl"
    
    // Verwaltung
    public string Municipality { get; set; }     // "Dobl-Zwaring"
    public string District { get; set; }         // "Graz-Umgebung"
    public string State { get; set; }            // "Steiermark"
    
    // Koordinaten
    public string CoordinateSystem { get; set; } // "EPSG:31258"
    public double CoordinateEast { get; set; }   // 450123.45
    public double CoordinateNorth { get; set; }  // 5210678.90
    
    // Grundstück (Kataster)
    public string CadastralKg { get; set; }      // "63201"
    public string CadastralKgName { get; set; }  // "Dobl"
    public string CadastralGst { get; set; }     // "123/1, 123/2, 124"
}
```

Alle diese Felder können automatisch befüllt werden — das ist der Kern dieses Moduls.

---

## 3. Datenquellen

### 3.1 GIS Steiermark (ArcGIS REST FeatureServer)

Das Land Steiermark bietet öffentliche GIS-Dienste:

| Dienst | URL-Muster | Liefert |
|--------|-----------|---------|
| Kataster (DKM) | ArcGIS REST FeatureServer | KG-Nummer, KG-Name, GST-Nummern, Grundstücksgrenzen (Polygon) |
| Flächenwidmung | WMS/WFS | Widmungskategorie (Bauland, Grünland etc.) |
| Orthofoto | WMS | Luftbild als Hintergrund für Karte |

**Abfrage-Prinzip:** Koordinate senden → Grundstück zurückbekommen (Point-in-Polygon Query).

```
GET https://gis.stmk.gv.at/arcgis/rest/services/DKM/FeatureServer/0/query
  ?geometry=450123,5210678
  &geometryType=esriGeometryPoint
  &inSR=31258
  &spatialRel=esriSpatialRelIntersects
  &returnGeometry=true
  &f=json
```

Antwort enthält: KG-Nummer, GST-Nummer, Fläche, Polygon-Geometrie.

### 3.2 Google Maps Geocoding API

| Dienst | Liefert |
|--------|---------|
| Geocoding | Adresse → Koordinaten (WGS84) |
| Reverse Geocoding | Koordinaten → Adresse, PLZ, Ort |

**Einschränkung:** Braucht API-Key (kostenlos bis 28.000 Requests/Monat). Gemeinde und Bezirk kommen nicht immer zuverlässig aus Google — GIS Steiermark ist dafür besser.

### 3.3 Kombinierter Workflow

```
User gibt Adresse ein: "Hauptstraße 15, 8143 Dobl"
    │
    ▼
Google Maps API → Koordinaten (WGS84: 47.123, 15.456)
    │
    ▼
Koordinaten-Umrechnung WGS84 → EPSG:31258 (GK M31)
    │
    ▼
GIS Steiermark API → KG: 63201 "Dobl", GST: 123/1
                    → Gemeinde: Dobl-Zwaring
                    → Bezirk: Graz-Umgebung
                    → Grundstückspolygon (optional)
    │
    ▼
ProjectLocation automatisch befüllt
```

---

## 4. Architektur im BPM

### 4.1 Service-Klassen

```
BauProjektManager.Infrastructure/
└── GIS/
    ├── IGeocodingService.cs         ← Interface (Domain)
    ├── GoogleGeocodingService.cs    ← Adresse → Koordinaten
    ├── SteiermarkGisService.cs      ← Koordinaten → Katasterdaten
    ├── CoordinateConverter.cs       ← EPSG:31258 ↔ WGS84
    └── GisModels.cs                 ← GeocodingResult, ParcelData
```

### 4.2 Datenmodelle (Erweiterung)

```csharp
public class GeocodingResult
{
    public double Latitude { get; set; }       // WGS84
    public double Longitude { get; set; }      // WGS84
    public string FormattedAddress { get; set; }
    public string PostalCode { get; set; }
    public string City { get; set; }
    public string State { get; set; }
}

public class ParcelData
{
    public string KgNumber { get; set; }       // "63201"
    public string KgName { get; set; }         // "Dobl"
    public string GstNumber { get; set; }      // "123/1"
    public double Area { get; set; }           // m²
    public string Municipality { get; set; }   // "Dobl-Zwaring"
    public string District { get; set; }       // "Graz-Umgebung"
    public string? GeoJson { get; set; }       // Polygon als GeoJSON (optional)
}
```

### 4.3 Integration im ProjectEditDialog

Im Projekt-Dialog neben den Adressfeldern:

```
Adresse:  [ Hauptstraße    ] [ 15  ] [ 8143 ] [ Dobl       ]
          [🔍 Adresse suchen]  ← Klick → Google API → Koordinaten befüllen

Koordinaten:
System:   [ EPSG:31258 ▼ ]
Ost:      [ 450123.45    ]   Nord: [ 5210678.90  ]
          [🔍 Grundstück laden] ← Klick → GIS Stmk API → KG/GST befüllen

Grundstück:
KG:       [ 63201   ]   KG-Name: [ Dobl          ]
GST:      [ 123/1, 123/2, 124                     ]
          ↑ automatisch befüllt, aber editierbar
```

Beide Buttons sind optional — der User kann alles auch manuell eintippen. Wenn kein Internet → Fehlermeldung, manuelle Eingabe bleibt möglich (offline-first).

---

## 5. Koordinaten-Umrechnung

### EPSG:31258 (GK M31) ↔ WGS84 (EPSG:4326)

Die Steiermark verwendet GK M31 (Gauß-Krüger, Meridianstreifen 31). Google Maps verwendet WGS84. Umrechnung nötig in beide Richtungen.

**Optionen in C#:**

| Option | Paket | Aufwand |
|--------|-------|---------|
| ProjNet (NuGet) | `ProjNet` | Fertige Transformation, zuverlässig |
| Eigene Formeln | — | Fehleranfällig, nicht empfohlen |
| API-basiert | epsg.io | Braucht Internet für jede Umrechnung |

**Empfehlung:** ProjNet als NuGet-Paket. Einmal konfigurieren, dann offline nutzbar.

```csharp
// Beispiel mit ProjNet (konzeptionell)
var wgs84 = GeographicCoordinateSystem.WGS84;
var gkM31 = ProjectedCoordinateSystem.FromEpsg(31258);
var transform = new CoordinateTransformationFactory()
    .CreateFromCoordinateSystems(wgs84, gkM31);

double[] wgsPoint = { 15.456, 47.123 };  // Lng, Lat
double[] gkPoint = transform.MathTransform.Transform(wgsPoint);
// gkPoint = { 450123.45, 5210678.90 }
```

---

## 6. Offline-Strategie

GIS-Abfragen brauchen Internet. BPM ist offline-first. Lösung:

| Situation | Verhalten |
|-----------|----------|
| Internet verfügbar | API-Abfrage, Daten in ProjectLocation speichern |
| Kein Internet | Fehlermeldung "Keine Verbindung", manuelle Eingabe möglich |
| Daten einmal geladen | In SQLite/ProjectLocation gespeichert, kein erneuter API-Call nötig |
| Koordinaten-Umrechnung | Offline möglich (ProjNet rechnet lokal) |

GIS-Daten werden **einmalig beim Projekt-Anlegen** abgefragt und dann lokal gespeichert. Kein ständiger API-Zugriff nötig.

---

## 7. Zukunft: Karte im Dashboard

Wenn das Dashboard gebaut wird, könnte ein Karten-Widget die Baustellen zeigen:

- Alle aktiven Projekte auf einer Karte (Pins aus Koordinaten)
- Klick auf Pin → Projekt-Dashboard öffnet
- Grundstücksgrenzen anzeigen (aus gespeichertem GeoJSON)
- Orthofoto-Hintergrund (WMS von GIS Steiermark)

**Technologie für Karte in WPF:**

| Option | Beschreibung |
|--------|-------------|
| GMap.NET | WPF-Kartenkontrolle, OpenStreetMap/Google, offline-fähig |
| Mapsui | Modernes WPF/MAUI-Kartenkontrolle, NuGet |
| WebView2 + Leaflet | HTML-basierte Karte in WPF eingebettet |

Entscheidung wird getroffen wenn das Dashboard drankommt.

---

## 8. API-Key Verwaltung

Google Maps API braucht einen API-Key. Dieser darf **nicht im Code** stehen (ADR-016, Coding Standards).

| Option | Beschreibung |
|--------|-------------|
| settings.json | API-Key in App-Einstellungen (OneDrive, versteckt in .AppData) |
| Umgebungsvariable | `%BPM_GOOGLE_API_KEY%` |
| Erster Start | Dialog "Google Maps API-Key eingeben" (optional, GIS funktioniert auch ohne) |

GIS Steiermark braucht **keinen API-Key** — die Dienste sind öffentlich.

---

## 9. Umsetzungsreihenfolge

| Phase | Was | Abhängigkeit |
|-------|-----|-------------|
| **1** | `SteiermarkGisService` — Koordinaten → KG/GST abfragen | Nur HttpClient |
| **2** | `CoordinateConverter` — EPSG:31258 ↔ WGS84 | ProjNet NuGet |
| **3** | "🔍 Grundstück laden" Button im ProjectEditDialog | Phase 1 + 2 |
| **4** | `GoogleGeocodingService` — Adresse → Koordinaten | API-Key nötig |
| **5** | "🔍 Adresse suchen" Button im ProjectEditDialog | Phase 4 |
| **6** | GeoJSON der Grundstücksgrenzen speichern (optional) | Phase 1 |
| **7** | Karten-Widget im Dashboard (weit nach V1) | Dashboard-Modul |

Phase 1–3 können ohne Google API-Key umgesetzt werden (GIS Steiermark ist kostenlos und öffentlich).

---

## 10. Offene Entscheidungen

| Frage | Optionen | Entscheidung |
|-------|---------|-------------|
| GIS Steiermark API stabil genug? | Testen ob die REST-Endpunkte zuverlässig antworten | ⬜ Testen |
| Grundstücksgrenzen speichern? | GeoJSON in DB vs. nur KG/GST-Nummern | ⬜ Offen |
| Google Maps oder alternatives Geocoding? | Google (zuverlässig, API-Key) vs. Nominatim/OSM (kostenlos, weniger genau) | ⬜ Offen |
| Karten-Bibliothek für WPF? | GMap.NET vs. Mapsui vs. WebView2+Leaflet | ⬜ Offen (erst bei Dashboard) |
| Flächenwidmung auch abfragen? | Bauland/Grünland aus GIS Steiermark | ⬜ Nice to have |
| Baugrundanalyse / AI? | Aus Prompt — für BPM derzeit nicht relevant | ❌ Nicht geplant |

---

*Dieses Konzept beschreibt die BPM-spezifische GIS-Integration als Desktop-App. Kein REST Backend, kein eigener GIS-Server — direkte API-Calls aus C# an öffentliche Dienste. Die bestehenden ProjectLocation-Felder sind bereits vorbereitet.*
