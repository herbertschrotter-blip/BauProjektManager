# BauProjektManager — Architektur & Spezifikation

**Version:** 1.2.0  
**Datum:** 26.03.2026  
**Sprache:** C# + WPF (.NET 9) | Registry: JSON (VBA-kompatibel)  
**Autor:** Herbert + Claude  

---

## 1. Vision & Übersicht

### 1.1 Was ist der BauProjektManager?

Ein Ökosystem aus lokalen Desktop-Tools für Baustellen-Management in der Steiermark (Österreich). Alle Apps — inklusive VBA-Makros in Outlook und Excel — greifen auf eine gemeinsame Projekt-Registry zu.

### 1.2 Apps im Ökosystem

| App | Technologie | Funktion | Status |
|-----|------------|----------|--------|
| **MasterApp** | C# + WPF | Projekte anlegen, Registry verwalten, Launcher | Geplant |
| **PlanManager** | C# + WPF | Pläne sortieren, versionieren, Planlisten | **Erste App** |
| **PhotoFolder** | PowerShell | Baustellenfotos per GPS/EXIF sortieren | Existiert |
| **Outlook-VBA** | VBA | Emails/Anhänge in Projektordner sortieren | Existiert teilweise |
| **Excel-Vorlagen** | VBA | Beton-/Ziegeltabellen mit Projektdaten | Existiert teilweise |
| **WetterApp** | PowerShell/C# | Wetterdaten pro Baustelle | Geplant |

### 1.3 Zentrale Registry als Nervensystem

```
  Outlook VBA ──┐
  Excel VBA ────┤
  PlanManager ──┼──→  registry.json  ←── Quelle der Wahrheit
  PhotoFolder ──┤      (OneDrive)
  WetterApp ────┘
```

Jede App liest die Registry, kennt alle Projekte, Pfade und Status. Kein Doppelt-Eintippen.

### 1.4 Multi-Device & OneDrive-Sync

- **Arbeitsplatz:** PC zuhause + Laptop in der Arbeit
- **Sync:** OneDrive synchronisiert alles automatisch
- **Kein AppData:** Nichts in C:\Users\AppData (synct nicht!)
- **Alles in OneDrive:** Configs, Caches, Logs — alles synct zwischen Geräten

### 1.5 Projektname-Format

```
Format: YYYYMM_Kurzname
Beispiele:
  202512_ÖWG-Dobl-Zwaring
  202302_Reihenhäuser-Kapfenberg
  202201_Sanierung-Leoben
```

---

## 2. Ordnerstruktur (OneDrive)

### 2.1 Gesamtstruktur

```
OneDrive/02Arbeit/
│
├── .AppData/                                  ← VERSTECKT, synct über OneDrive
│   └── BauProjektManager/
│       ├── registry.json                      ← DIE zentrale Registry
│       ├── settings.json                      ← Globale App-Einstellungen
│       ├── planTypes.json                     ← Erweiterte Plantyp-Liste
│       ├── templates.json                     ← Verzeichnis aller Vorlagen
│       └── Projects/                          ← Config pro Projekt
│           ├── 202512_OeWG-Dobl/
│           │   ├── planmanager-config.json     ← Plantyp-Profile + Import-History
│           │   ├── planmanager-cache.json      ← Bestandscache + MD5-Hashes
│           │   └── Logs/
│           │       ├── PlanManager_2026-03-24.log
│           │       └── PlanManager_2026-03-26.log
│           └── 202302_Kapfenberg/
│               ├── planmanager-config.json
│               └── planmanager-cache.json
│
├── Vorlagen/                                  ← Excel/Word Vorlagen (sichtbar!)
│   ├── Excel/
│   │   ├── Betontabelle_v3.xlsm
│   │   ├── Ziegeltabelle_v2.xlsm
│   │   ├── Bautagebuch_v1.xlsm
│   │   └── Stundenzettel_v1.xlsm
│   ├── Word/
│   │   ├── Bauprotokoll.dotx
│   │   └── Briefkopf.dotx
│   └── BPM_Helper.xlam                       ← Excel Add-In (Entscheidung offen)
│
├── 202512_ÖWG-Dobl-Zwaring/                  ← Projektordner (SAUBER!)
│   ├── .bpm-manifest                          ← Versteckt, winzig (<10 Zeilen)
│   ├── Pläne/
│   │   ├── _Eingang/                          ← Sammelordner (Outlook+User schreiben hier)
│   │   ├── Polierplan/
│   │   │   ├── TG/
│   │   │   │   ├── S-101-A_TG Bodenplatte.pdf
│   │   │   │   ├── S-101-A_TG Bodenplatte.dwg
│   │   │   │   └── _Archiv/
│   │   │   │       └── S-103-C_TG Wämde-Stützen.pdf
│   │   │   ├── EG/
│   │   │   └── 1OG/
│   │   ├── Schalungsplan/
│   │   └── Bewehrungsplan/
│   ├── Fotos/
│   ├── Dokumente/
│   ├── Protokolle/
│   └── Rechnungen/
│
├── 202302_Reihenhäuser-Kapfenberg/
│   ├── .bpm-manifest
│   └── ...
│
└── BauProjektManager/                         ← Die App selbst (.exe)
    ├── PlanManager.exe
    └── ...
```

### 2.2 Warum diese Struktur?

| Thema | Lösung | Warum |
|-------|--------|-------|
| Projektordner sauber | Nur `.bpm-manifest` (hidden) | Kollegen/Partner sehen keine App-Dateien |
| Configs synchen | In `.AppData/` auf OneDrive | PC ↔ Laptop automatisch synchron |
| Kein C:\AppData | Alles auf OneDrive | Sonst müsste man auf jedem Gerät neu einrichten |
| VBA-kompatibel | Registry ist reines JSON | VBA kann JSON nativ parsen |
| Versteckte Ordner | Punkt-Prefix (`.AppData`, `.bpm-manifest`) | Windows zeigt Punkt-Dateien nicht standardmäßig |

### 2.3 Hidden-Attribute

Der PlanManager setzt automatisch bei Erstellung:
- `.AppData/` → Hidden + System Attribute
- `.bpm-manifest` → Hidden Attribute
- `_Eingang/` → NICHT hidden (User muss hier Dateien reinwerfen)
- `_Archiv/` → NICHT hidden (User soll alte Pläne finden können)

---

## 3. Zentrale Projekt-Registry

### 3.1 Design-Prinzipien

- **Reines JSON** — VBA (Outlook, Excel) muss es lesen/schreiben können
- **Flache Struktur** — keine tief verschachtelten Objekte (VBA-freundlich)
- **UTF-8 ohne BOM** — kompatibel mit allen Tools
- **Keine Sonderzeichen in Keys** — nur a-z, A-Z, camelCase

### 3.2 Registry-Schema (registry.json)

```json
{
  "registryVersion": "1.0.0",
  "lastModified": "2026-03-26T10:00:00",

  "settings": {
    "basePath": "C:\\Users\\Herbert\\OneDrive\\02Arbeit",
    "appDataPath": "C:\\Users\\Herbert\\OneDrive\\02Arbeit\\.AppData\\BauProjektManager",
    "folderTemplate": "{projectNumber}_{projectName}"
  },

  "planTypes": [
    "Polierplan",
    "Schalungsplan",
    "Bewehrungsplan",
    "Elektroplan",
    "HKLS-Plan",
    "Detailplan",
    "Architekturplan",
    "Lageplan",
    "Grundrissplan",
    "Schnittplan"
  ],

  "customPlanTypes": [],

  "projects": [
    {
      "id": "proj_202512_dobl",
      "projectNumber": "202512",
      "name": "ÖWG-Dobl-Zwaring",
      "fullName": "Gartensiedlung Dobl-Zwaring",
      "status": "active",

      "address": "Hauptstraße 15, 8143 Dobl-Zwaring",
      "municipality": "Dobl-Zwaring",
      "district": "Graz-Umgebung",
      "state": "Steiermark",

      "coordinateSystem": "EPSG:31258",
      "coordinateEast": 450123.45,
      "coordinateNorth": 5210678.90,

      "cadastralKg": "63201",
      "cadastralKgName": "Dobl",
      "cadastralGst": "123/1, 123/2, 124",

      "projectStart": "2024-01-15",
      "constructionStart": "2024-06-01",
      "plannedEnd": "2026-12-31",
      "actualEnd": null,

      "rootPath": "C:\\Users\\Herbert\\OneDrive\\02Arbeit\\202512_ÖWG-Dobl-Zwaring",
      "plansPath": "Pläne",
      "inboxPath": "Pläne\\_Eingang",
      "photosPath": "Fotos",
      "documentsPath": "Dokumente",
      "protocolsPath": "Protokolle",
      "invoicesPath": "Rechnungen",

      "buildings": "H64:Haus Nr. 64:Reihenhaus:KG,EG,1.OG,2.OG,Dach|H66:Haus Nr. 66:Reihenhaus:EG,OG,Dach|H68:Haus Nr. 68:Reihenhaus:EG,1.OG,2.OG,Dach",

      "tags": "Wohnbau, Reihenhäuser, ÖWGES",
      "notes": "Bauteil B-13, 3 Häuser"
    }
  ]
}
```

### 3.3 VBA-Kompatibilitäts-Regeln

| Regel | Grund |
|-------|-------|
| Keine verschachtelten Objekte in Projekt-Daten | VBA JSON-Parser sind einfach |
| Koordinaten als separate Felder (nicht als Unter-Objekt) | `coordinateEast` statt `coordinates.east` |
| Pfade relativ zu `rootPath` (außer `rootPath` selbst) | VBA kann einfach zusammenbauen |
| Buildings als Pipe-getrennter String | VBA kann mit `Split()` parsen |
| Tags als Komma-getrennter String | VBA kann mit `Split()` parsen |
| Datum als ISO-String `YYYY-MM-DD` | VBA `CDate()` kompatibel |

### 3.4 Projekt-Manifest (.bpm-manifest)

Liegt im Root jedes Projektordners. Versteckt (Hidden-Attribut). Winzig.

```json
{
  "registryId": "proj_202512_dobl",
  "projectNumber": "202512",
  "name": "ÖWG-Dobl-Zwaring",
  "registryPath": "C:\\Users\\Herbert\\OneDrive\\02Arbeit\\.AppData\\BauProjektManager\\registry.json"
}
```

**Zweck:** Wenn eine App einen Ordner öffnet, erkennt sie sofort: "Das ist ein BPM-Projekt" und weiß wo die Registry liegt.

---

## 4. Outlook-VBA Integration (Registry-basiert)

### 4.1 Workflow

```
1. Outlook startet / User klickt "BPM Sync"
2. VBA liest registry.json
3. Für jedes Projekt mit status = "active":
   → Outlook-Ordner existiert? Nein → erstelle "202512_ÖWG-Dobl-Zwaring"
   → Eingangsordner auf Festplatte existiert? Nein → erstelle
4. Für jedes Projekt mit status = "completed":
   → "Projekt XY abgeschlossen — Outlook-Ordner archivieren?"
5. User verschiebt Emails mit Plänen in Projekt-Ordner
6. VBA exportiert Anhänge (PDF+DWG) → Eingangsordner des Projekts
7. PlanManager findet die Dateien beim nächsten Import
```

### 4.2 VBA liest Registry (Pseudo-Code)

```vba
' Registry lesen
Dim json As String
json = ReadTextFile(registryPath)

' Projekte durchlaufen
For Each project In ParseJSON(json)("projects")
    If project("status") = "active" Then
        ' Outlook-Ordner erstellen
        folderName = project("projectNumber") & "_" & project("name")
        CreateOutlookFolder folderName
        
        ' Eingangsordner sicherstellen
        inboxPath = project("rootPath") & "\" & project("inboxPath")
        If Not FolderExists(inboxPath) Then MkDir inboxPath
    End If
Next
```

---

## 5. Excel-Vorlagen & VBA-Anbindung

### 5.1 Vorlagen-Ordner

Zentral in `02Arbeit/Vorlagen/` (sichtbar, nicht versteckt):

```
02Arbeit/Vorlagen/
├── Excel/
│   ├── Betontabelle_v3.xlsm
│   ├── Ziegeltabelle_v2.xlsm
│   ├── Bautagebuch_v1.xlsm
│   └── Stundenzettel_v1.xlsm
├── Word/
│   ├── Bauprotokoll.dotx
│   └── Briefkopf.dotx
└── BPM_Helper.xlam                ← Excel Add-In (Entscheidung offen)
```

### 5.2 templates.json

Liegt in `.AppData/BauProjektManager/`. Verzeichnis aller verfügbaren Vorlagen:

```json
{
  "templatesPath": "C:\\Users\\Herbert\\OneDrive\\02Arbeit\\Vorlagen",
  "templates": [
    {
      "id": "betontabelle",
      "name": "Betontabelle",
      "file": "Excel\\Betontabelle_v3.xlsm",
      "version": "3.0",
      "type": "xlsm",
      "category": "Tabellen",
      "usesProjectData": true,
      "projectFields": ["name", "projectNumber", "address", "buildings"]
    },
    {
      "id": "ziegeltabelle",
      "name": "Ziegeltabelle",
      "file": "Excel\\Ziegeltabelle_v2.xlsm",
      "version": "2.0",
      "type": "xlsm",
      "category": "Tabellen",
      "usesProjectData": true,
      "projectFields": ["name", "projectNumber", "address"]
    }
  ]
}
```

### 5.3 VBA-Anbindung (Entscheidung offen)

**Zwei Optionen für später:**

| Option | Beschreibung | Vorteil | Nachteil |
|--------|-------------|---------|----------|
| **Add-In (.xlam)** | Einmal installieren, überall verfügbar | Code nur 1x pflegen, Vorlagen brauchen kein Makro | Muss auf jedem Gerät aktiviert werden |
| **Gemeinsame .bas** | Manuell in jede Vorlage importieren | Kein Add-In nötig | Code in jeder Datei kopiert |

**Entscheidung wird getroffen wenn Excel-Vorlagen an die Registry angebunden werden.**

### 5.4 Excel-Workflow (Ziel)

1. User öffnet Vorlage (z.B. Betontabelle)
2. Klickt "Projekt laden" → Auswahldialog mit allen aktiven Projekten
3. Projektdaten (Name, Nr, Adresse, Gebäude) werden automatisch eingetragen
4. Gebäude-Dropdown wird aus Registry befüllt (H64, H66, H68...)

---

## 6. PlanManager — Kernfunktionen

### 5.1 Workflow-Übersicht

```
1. Projekt aus Registry laden (oder neues anlegen → Registry updaten)
2. Plantyp-Profil erstellen (Dateinamen-Muster anlernen)
3. Pläne in Sammelordner (_Eingang) werfen (manuell oder via Outlook)
4. Import starten → Vorschau → Bestätigen
5. Pläne werden automatisch einsortiert
6. Planliste importieren → Abgleich Soll vs. Ist
7. Planliste aus Bestand exportieren
```

### 5.2 Dateitypen

- **PDF** (.pdf) + **DWG** (.dwg) als Paar
- Gleicher Dateiname, unterschiedliche Extension → zusammen behandelt
- Beim Verschieben/Archivieren immer beide zusammen

### 5.3 Sammelordner-Konzept

- Fester "Briefkasten" pro Projekt: `Pläne/_Eingang/`
- Quellen: Email (Outlook-VBA), Portal-Download, USB, manuell
- Nach Import: Dateien werden **verschoben** (Eingang wird leer)
- Beim App-Start: Prüfe alle Eingänge → Benachrichtigung wenn nicht leer

### 5.4 Index-Versionierung

| Situation | Aktion |
|-----------|--------|
| Neuer Plan (Nummer existiert nicht) | Einsortieren in Zielordner |
| Neuer Index (z.B. C→D) | Alter Plan → `_Archiv/`, neuer an Stelle |
| Gleicher Index, geänderte Datei (MD5 anders) | Überschreiben (kein Archiv) |
| Gleicher Index, identische Datei (MD5 gleich) | Überspringen |
| Leerer Index | User definiert pro Projekt Bedeutung |

### 5.5 Bestandsverwaltung

- **Hybrid:** Filesystem scannen + JSON-Cache
- **Cache:** `.AppData/.../planmanager-cache.json` mit MD5, Pfaden, Indizes
- **Import-History:** Wann, was, wohin (für Rückgängig)
- **Unter-Nummern** (P-001.1, P-001.2): Als separate Pläne behandelt

---

## 7. PlanManager — Dateinamen-Parsing

### 10.1 Konzept

User "lehrt" das System pro Plantyp-Profil wie Dateinamen aufgebaut sind. Danach parsed das System automatisch.

### 10.2 Hybrid-Mechanismus

1. **Segmente:** Dateiname wird an Trennzeichen gesplittet → klickbare Blöcke
2. **Zeichen-Level:** Fallback per Toggle-Button für Feinauswahl

### 10.3 Beispiele aus der Praxis

```
Screen 1 (Polierplan):
  "S-103-C_TG Wämde-Stützen-Träger.pdf"
  Split by [-_]: [S] [103] [C] [TG Wämde-Stützen-Träger]
  Zuweisung:     Pref  Nr   Idx  Bezeichnung

Screen 2 (Schalungsplan):
  "5998-003_Wände_KG_Teil_1.pdf"
  Split by [-_]: [5998] [003] [Wände] [KG] [Teil] [1]
  Zuweisung:     ProjNr  Nr   Objekt Gesch  ?    Idx

Screen 3 (Architekturplan):
  "21005_104_AP_H1_GR_E2_05_Grundriss E+2.pdf"
  Split by [_]:  [21005] [104] [AP] [H1] [GR] [E2] [05] [Grundriss E+2]
  Zuweisung:     ProjNr   Nr   Typ  Haus Plan Gesch Idx  Bezeichnung

Screen 4 (Schalungsplan):
  "21-2094_404_A_Wände 20G_Haus 2_-_Schalung.pdf"
  Split by [_]:  [21-2094] [404] [A] [Wände 20G] [Haus 2] [-] [Schalung]
  Zuweisung:     ProjNr     Nr   Idx  Objekt      Haus     ign  Plantyp
```

### 10.4 Verfügbare Feld-Typen

**System-Felder (immer vorhanden):**

| Feld-ID | Anzeigename | Beispiel | Pflicht |
|---------|------------|---------|---------|
| `projectNumber` | Projekt-Nummer | 202401, 5998 | Nein |
| `planNumber` | Plan-Nummer | 101, P-013 | **Ja** |
| `planIndex` | Plan-Index | A, B, 03, 05 | **Ja** |
| `description` | Bezeichnung | TG Bodenplatte | Nein |
| `ignore` | Ignorieren | - | Nein |

**Bau-spezifische Felder (vordefiniert):**

| Feld-ID | Anzeigename | Beispiel |
|---------|------------|---------|
| `geschoss` | Geschoß/Ebene | EG, 1.OG, KG, E+2 |
| `haus` | Haus/Gebäude | H1, Haus 2, Nr. 64 |
| `planart` | Planart | GR (Grundriss), SC (Schnitt) |
| `objekt` | Objekt | Wände, Decke, Stützen |
| `bauteil` | Bauteil | Bauteil B-13 |
| `bauabschnitt` | Bauabschnitt | BA1, BA2 |
| `stiege` | Stiege/Trakt | Stiege 1, Trakt A |
| `achse` | Achse/Raster | Achse A-C |
| `zone` | Zone | Zone Nord |
| `block` | Block | Block A |

**Benutzerdefiniert:** User kann jederzeit neue Feld-Namen erstellen. Werden dauerhaft gespeichert.

### 10.5 Ordner-Hierarchie

- **Plantyp** ist IMMER die erste Ordner-Ebene (automatisch, nicht abwählbar)
- Darunter: User wählt per Checkbox welche Felder Ordner werden
- Reihenfolge frei sortierbar (↑↓), wird beim Profil-Erstellen festgelegt
- Ordnerstruktur nur bei Neuerstellung änderbar, nicht im laufenden Betrieb

**Beispiele:**

```
Wenig Pläne:   /Polierplan/datei.pdf            (keine Unterordner)
Normal:        /Polierplan/EG/datei.pdf          (1 Ebene: Geschoß)
Mittel:        /Polierplan/H64/EG/datei.pdf      (2 Ebenen: Haus→Geschoß)
Komplex:       /Architekturplan/Grundriss/H1/E+2/datei.pdf  (3 Ebenen)
```

### 10.6 Plantyp-Erkennung

Automatisch anhand gespeicherter Muster. Mehrere Muster pro Profil (lernend):

```json
{
  "planType": "Polierplan",
  "recognition": [
    { "method": "prefix", "value": "S-" },
    { "method": "prefix", "value": "ST-" }
  ]
}
```

**Methoden:** `prefix`, `contains`, `regex`

**Konflikt:** Spezifischeres Muster gewinnt. Bei Gleichstand → User-Dialog.

**Plantyp-Liste:** Vordefiniert (10 Typen) + User kann dauerhaft erweitern.

---

## 8. PlanManager — Fehlerbehandlung

### 10.1 Drei Schutz-Stufen

| Stufe | Wann | Was |
|-------|------|-----|
| **Vorschau** | VOR Import | Jede Zuordnung sehen, Rechtsklick korrigieren |
| **Rückgängig** | NACH Import | Gesamten Import oder einzelne Dateien zurücknehmen |
| **Muster lernen** | Bei Korrektur | Erkennungsmuster verfeinern |

### 10.2 Unbekannte Dateien

Dialog mit Optionen:
- Bestehendes Profil erweitern (neues Erkennungsmuster)
- Neues Profil erstellen → Segment-Zuweiser öffnen
- Überspringen
- Manuell verschieben

---

## 9. PlanManager — Planlisten

### 10.1 Import

**Formate:** Excel (.xlsx), CSV, PDF (Best Effort)

**Spalten-Zuordnung:** Angelernt pro Plantyp. User weist Spalten zu (Plan-Nr, Index, Bezeichnung, Datum...). Wird gespeichert.

**Abgleich-Ergebnis:**

| Status | Bedeutung |
|--------|-----------|
| ✅ Aktuell | Index stimmt überein |
| ⚠️ Veraltet | User hat älteren Index |
| ❌ Fehlend | In Planliste aber nicht im Bestand |
| ℹ️ Extra | Im Bestand aber nicht in Planliste |

### 10.2 Export

Aus eigenem Bestand generieren:
- Plantypen wählen (Checkboxen)
- Spalten wählen (Checkboxen)
- Archiv-Pläne: Nein / Separates Blatt / Mit Markierung
- Sortierung: Mehrstufig, frei wählbar
- Format: Excel (.xlsx) oder PDF

---

## 10. GUI-Dialoge (WPF)

### 10.1 Hauptfenster (MainWindow)

```
╔══════════════════════════════════════════════════════════════════╗
║  BauProjektManager — PlanManager                        _ □ X  ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Projekte                                              [Menü ≡] ║
║  ╔════════════════════════════════════════════════════════════╗  ║
║  ║  🏗 202512_ÖWG-Dobl-Zwaring                              ║  ║
║  ║    Status: Aktiv | 6 Plantypen | 84 Pläne                ║  ║
║  ║    📥 3 neue Dateien im Eingang                          ║  ║
║  ╠════════════════════════════════════════════════════════════╣  ║
║  ║  🏗 202302_Reihenhäuser-Kapfenberg                       ║  ║
║  ║    Status: Aktiv | 3 Plantypen | 42 Pläne                ║  ║
║  ║    ✅ Eingang leer                                       ║  ║
║  ╠════════════════════════════════════════════════════════════╣  ║
║  ║  🏗 202201_Sanierung-Leoben                              ║  ║
║  ║    Status: Abgeschlossen | 2 Plantypen | 18 Pläne       ║  ║
║  ╚════════════════════════════════════════════════════════════╝  ║
║                                                                  ║
║  [ + Neues Projekt ]                                            ║
║                                                                  ║
║  ── Statusleiste ───────────────────────────────────────────── ║
║  Registry: .AppData\BauProjektManager\registry.json | 3 Proj.  ║
╚══════════════════════════════════════════════════════════════════╝
```

**Aktionen:**
- Doppelklick → Projekt-Detailansicht
- Badge "3 neue Dateien" → direkt zum Import
- Menü ≡ → Einstellungen, Registry-Pfad, Über

### 10.2 Projekt-Detailansicht

```
╔══════════════════════════════════════════════════════════════════╗
║  ← Zurück    202512_ÖWG-Dobl-Zwaring                   _ □ X  ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  📁 Pläne:   ...\202512_ÖWG-Dobl-Zwaring\Pläne                 ║
║  📥 Eingang: ...\202512_ÖWG-Dobl-Zwaring\Pläne\_Eingang        ║
║                                                                  ║
║  Plantypen:                                                      ║
║  ╔══════════════════╦═══════╦════════════════╦═════════════════╗ ║
║  ║ Plantyp          ║ Pläne ║ Letzter Import ║ Status          ║ ║
║  ╠══════════════════╬═══════╬════════════════╬═════════════════╣ ║
║  ║ Polierplan       ║  28   ║ 24.03.2026     ║ ✅ Aktuell     ║ ║
║  ║ Schalungsplan    ║  14   ║ 20.03.2026     ║ ⚠️ 2 veraltet ║ ║
║  ║ Bewehrungsplan   ║  22   ║ 22.03.2026     ║ ✅ Aktuell     ║ ║
║  ║ Elektroplan      ║   8   ║ 18.03.2026     ║ ✅ Aktuell     ║ ║
║  ║ HKLS-Plan        ║  12   ║ 15.03.2026     ║ ✅ Aktuell     ║ ║
║  ║ Detailplan       ║   0   ║ Noch nie       ║ —               ║ ║
║  ╚══════════════════╩═══════╩════════════════╩═════════════════╝ ║
║                                                                  ║
║  [ + Plantyp hinzufügen ]    [ 📥 Import starten ]             ║
║  [ 📋 Planliste abgleichen ] [ 📄 Planliste exportieren ]      ║
║  [ 🔍 Plan suchen ]                                            ║
║                                                                  ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.3 Plantyp hinzufügen — Schritt 1/3: Typ wählen

```
╔══════════════════════════════════════════════════════════════════╗
║  Plantyp hinzufügen — Schritt 1 von 3                   _ □ X ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Plantyp auswählen:                                             ║
║  ╔════════════════════════════════════╗                          ║
║  ║  ○ Polierplan                      ║                          ║
║  ║  ○ Schalungsplan                   ║                          ║
║  ║  ○ Bewehrungsplan                  ║                          ║
║  ║  ○ Elektroplan                     ║                          ║
║  ║  ○ HKLS-Plan                       ║                          ║
║  ║  ○ Detailplan                      ║                          ║
║  ║  ○ Architekturplan                 ║                          ║
║  ║  ○ Lageplan                        ║                          ║
║  ║  ○ Grundrissplan                   ║                          ║
║  ║  ○ Schnittplan                     ║                          ║
║  ║  ○ Benutzerdefiniert: [________]   ║                          ║
║  ╚════════════════════════════════════╝                          ║
║                                                                  ║
║  Beispieldateien laden aus:                                     ║
║  [ ...\202512_ÖWG-Dobl-Zwaring\Pläne\_Eingang ] [Durchsuchen]  ║
║                                                                  ║
║  Dateien gefunden: 47 (PDF: 24, DWG: 23)                       ║
║                                                                  ║
║                              [ Weiter → ]    [ Abbrechen ]     ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.4 Plantyp hinzufügen — Schritt 2/3: Muster definieren

```
╔══════════════════════════════════════════════════════════════════╗
║  Muster definieren — Schritt 2 von 3                     _ □ X ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Trennzeichen: [ - ☑ ] [ _ ☑ ] [ . ☐ ] [ Leerz. ☐ ]          ║
║                                           [ + Eigenes ]         ║
║                                                                  ║
║  Beispiel: S-103-C_TG Wämde-Stützen-Träger.pdf                 ║
║                                                                  ║
║  Klicke auf ein Segment → Feld zuweisen:                        ║
║                                                                  ║
║  ╭───────╮  ╭───────╮  ╭───────╮  ╭──────────────────────╮     ║
║  │   S   │  │  103  │  │   C   │  │ TG Wämde-Stützen... │     ║
║  │ Prefix│  │ Nr.   │  │ Index │  │ Bezeichnung          │     ║
║  │  grau │  │ blau  │  │ orange│  │ grün                 │     ║
║  ╰───────╯  ╰───────╯  ╰───────╯  ╰──────────────────────╯     ║
║                                                                  ║
║  Gewähltes Segment: [103]                                       ║
║  Zuweisen als: [ Plan-Nummer        ▼ ]                         ║
║                [ + Neues Feld erstellen... ]                     ║
║                                                                  ║
║  ☐ Zeichen-Level anzeigen (für Feinauswahl)                     ║
║                                                                  ║
║  ── Erkennungsmuster ──────────────────────────────────────     ║
║  Woran erkenne ich diesen Plantyp?                              ║
║  Segment: [ Seg.0: Prefix ▼ ]  Wert: [ S    ]                  ║
║  → "Alle Dateien wo Segment 0 = 'S' sind Polierplan"           ║
║                                                                  ║
║  ── Live-Vorschau ─────────────────────────────────────────     ║
║  ╔══════════════════════════════════╦═══════╦═══════╦══════╗    ║
║  ║ Dateiname                        ║  Nr.  ║ Index ║ OK?  ║    ║
║  ╠══════════════════════════════════╬═══════╬═══════╬══════╣    ║
║  ║ S-101-A_TG Bodenplatte.pdf      ║  101  ║   A   ║  ✅  ║    ║
║  ║ S-103-C_TG Wämde-Stützen...     ║  103  ║   C   ║  ✅  ║    ║
║  ║ S-106-B_EG Wämde-Stützen...     ║  106  ║   B   ║  ✅  ║    ║
║  ╚══════════════════════════════════╩═══════╩═══════╩══════╝    ║
║  ✅ 28/28 Dateien erfolgreich geparst                           ║
║                                                                  ║
║           [ ← Zurück ]    [ Weiter → ]       [ Abbrechen ]     ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.5 Plantyp hinzufügen — Schritt 3/3: Ordnerstruktur

```
╔══════════════════════════════════════════════════════════════════╗
║  Ordnerstruktur — Schritt 3 von 3                        _ □ X ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Welche Felder sollen Ordner-Ebenen werden?                     ║
║                                                                  ║
║  Nicht als Ordner:      ║  Ordner-Hierarchie:                   ║
║  ╔════════════════════╗ ║  ╔═══════════════════════════════╗    ║
║  ║ Projekt-Nr         ║ ║  ║ Ebene 1: Geschoß       [↑][↓]║    ║
║  ║ Plan-Nr            ║ ║  ║ Ebene 2: Haus          [↑][↓]║    ║
║  ║ Index              ║ ║  ║                               ║    ║
║  ║ Bezeichnung        ║ ║  ║                               ║    ║
║  ║ Prefix             ║→║→ ║ [ + Ebene hinzufügen ]       ║    ║
║  ╚════════════════════╝ ║  ╚═══════════════════════════════╝    ║
║                          ║                                       ║
║  Leerer Index bedeutet:                                         ║
║  ○ Erstausgabe (gilt als aktuell)                               ║
║  ○ Unbekannt (nachfragen)                                       ║
║  ○ Eigene Definition: [________________]                        ║
║                                                                  ║
║  ── Vorschau ──────────────────────────────────────────────     ║
║  📁 Polierplan\                    ← Plantyp (fix)             ║
║  ├── 📁 TG\                        ← Ebene 1                  ║
║  │   └── S-101-A_TG Bodenplatte.pdf                            ║
║  ├── 📁 EG\                                                    ║
║  └── 📁 1OG\                                                   ║
║                                                                  ║
║           [ ← Zurück ]    [ Übernehmen ]     [ Abbrechen ]     ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.6 Import — Vorschau

```
╔══════════════════════════════════════════════════════════════════╗
║  Import — Vorschau                                       _ □ X ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Projekt: 202512_ÖWG-Dobl-Zwaring                              ║
║  Quelle:  ...\Pläne\_Eingang (12 Dateien: 6 PDF, 6 DWG)       ║
║                                                                  ║
║  ╔════════╦════════════════════════════╦══════════╦═════╦══════╗║
║  ║ Status ║ Dateiname                  ║ Plantyp  ║ Idx ║ Ziel ║║
║  ╠════════╬════════════════════════════╬══════════╬═════╬══════╣║
║  ║  🆕    ║ S-113-A_2OG Decke.pdf     ║ Polier   ║  A  ║ /2OG ║║
║  ║  🆕    ║ S-113-A_2OG Decke.dwg     ║ Polier   ║  A  ║ /2OG ║║
║  ║  📈    ║ S-103-D_TG Wämde...pdf    ║ Polier   ║ C→D ║ /TG  ║║
║  ║  📈    ║ S-103-D_TG Wämde...dwg    ║ Polier   ║ C→D ║ /TG  ║║
║  ║  ✅    ║ S-101-A_TG Boden...pdf    ║ Polier   ║  =  ║ skip ║║
║  ║  ❓    ║ Zeichnung1.dwl            ║ ???      ║  ?  ║  ?   ║║
║  ╚════════╩════════════════════════════╩══════════╩═════╩══════╝║
║                                                                  ║
║  🆕 Neu: 2    📈 Update: 2    ✅ Gleich: 2    ❓ Unbekannt: 1  ║
║                                                                  ║
║  Rechtsklick → Plantyp ändern / Ordner ändern / Überspringen   ║
║                                                                  ║
║         [ Details... ]  [ 📥 Importieren ]  [ Abbrechen ]      ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.7 Import — Abgeschlossen

```
╔══════════════════════════════════════════════════════════════════╗
║  Import abgeschlossen                                    _ □ X ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  ✅ Import erfolgreich — 26.03.2026, 14:32                      ║
║                                                                  ║
║  • 2 Pläne neu einsortiert                                      ║
║  • 2 Pläne aktualisiert (alter Index → _Archiv/)               ║
║  • 2 Pläne übersprungen (unverändert)                           ║
║  • 1 Datei übersprungen (unbekannt)                             ║
║                                                                  ║
║  ╔════════════════════════════╦══════════╦══════════════════╗    ║
║  ║ Datei                      ║ Aktion   ║ Zielordner       ║    ║
║  ╠════════════════════════════╬══════════╬══════════════════╣    ║
║  ║ S-113-A_2OG Decke.pdf     ║ 🆕 Neu   ║ Polierplan/2OG/  ║    ║
║  ║ S-103-D_TG Wämde...pdf    ║ 📈 C→D   ║ Polierplan/TG/   ║    ║
║  ╚════════════════════════════╩══════════╩══════════════════╝    ║
║                                                                  ║
║  [ Rückgängig: Gesamten Import ]  [ Einzelne korrigieren ]      ║
║  [ Protokoll speichern ]          [ Schließen ]                 ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.8 Planliste importieren

```
╔══════════════════════════════════════════════════════════════════╗
║  Planliste importieren                                   _ □ X ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Datei: [ ...\Planliste_Statik.xlsx          ] [ Durchsuchen ] ║
║  Plantyp: [ Polierplan ▼ ]                                      ║
║                                                                  ║
║  Vorschau (erste 5 Zeilen):                                     ║
║  ╔═══════╦══════════════════════════════╦═══════╦════════════╗  ║
║  ║ Sp.A  ║ Sp.B                         ║ Sp.C  ║ Sp.D       ║  ║
║  ╠═══════╬══════════════════════════════╬═══════╬════════════╣  ║
║  ║ S-101 ║ TG Bodenplatte Grundriss    ║   A   ║ 07.05.2025 ║  ║
║  ║ S-103 ║ TG Wämde-Stützen-Träger    ║   D   ║ 21.07.2025 ║  ║
║  ╚═══════╩══════════════════════════════╩═══════╩════════════╝  ║
║                                                                  ║
║  Spalten zuweisen:                                              ║
║  ╔════════════╦═══════════════════════════╗                     ║
║  ║ Spalte A   ║ [ Plan-Nummer        ▼ ] ║                     ║
║  ║ Spalte B   ║ [ Bezeichnung        ▼ ] ║                     ║
║  ║ Spalte C   ║ [ Plan-Index         ▼ ] ║                     ║
║  ║ Spalte D   ║ [ Datum              ▼ ] ║                     ║
║  ╚════════════╩═══════════════════════════╝                     ║
║                                                                  ║
║  Kopfzeile: Zeile [ 1 ] überspringen                            ║
║  ☑ Zuordnung für diesen Plantyp merken                          ║
║                                                                  ║
║              [ Abgleich starten ]         [ Abbrechen ]         ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.9 Planlisten-Abgleich Ergebnis

```
╔══════════════════════════════════════════════════════════════════╗
║  Abgleich: Polierplanung                                 _ □ X ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Planliste: 31 Pläne (Stand 16.02.2026)                        ║
║  Bestand:   27 Pläne                                            ║
║                                                                  ║
║  ╔════════╦═══════════════════╦══════╦══════╦══════════════════╗║
║  ║ Status ║ Plannummer        ║ Soll ║ Ist  ║ Planinhalt       ║║
║  ╠════════╬═══════════════════╬══════╬══════╬══════════════════╣║
║  ║  ✅    ║ 202401_P-010      ║  B   ║  B   ║ Grundriß Keller ║║
║  ║  ⚠️   ║ 202401_P-011      ║  C   ║  B   ║ Grundriß EG 64  ║║
║  ║  ❌    ║ 202401_P-029      ║  —   ║  —   ║ Ansichten H68   ║║
║  ╚════════╩═══════════════════╩══════╩══════╩══════════════════╝║
║                                                                  ║
║  ✅ Aktuell: 20 | ⚠️ Veraltet: 3 | ❌ Fehlend: 4 | ℹ️ Extra: 1║
║                                                                  ║
║  Filter: [ Alle ▼ ]    Sortierung: [ Status ▼ ]                ║
║                                                                  ║
║       [ Export als Excel ]  [ Drucken ]  [ Schließen ]          ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.10 Planliste exportieren

```
╔══════════════════════════════════════════════════════════════════╗
║  Planliste erstellen                                     _ □ X ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Plantypen:                  Spalten:                           ║
║  ☑ Polierplan       (28)    ☑ Plan-Nummer                      ║
║  ☑ Bewehrungsplan   (22)    ☑ Bezeichnung                      ║
║  ☐ Schalungsplan    (14)    ☑ Aktueller Index                  ║
║  ☐ Elektroplan      ( 8)    ☑ Datum                            ║
║                              ☑ Geschoß                          ║
║                              ☑ Haus                             ║
║                              ☑ Plantyp                          ║
║                              ☐ Dateipfad                        ║
║                              ☐ Dateigröße                       ║
║                              ☐ MD5-Hash                         ║
║                                                                  ║
║  Archiv:  ○ Nein  ○ Separates Blatt  ○ Mit Markierung          ║
║                                                                  ║
║  Sortierung:                                                    ║
║  1. [ Plantyp      ▼ ] ↑↓                                      ║
║  2. [ Geschoß      ▼ ] ↑↓                                      ║
║  3. [ Plan-Nummer  ▼ ] ↑↓                                      ║
║  [ + Sortierung ]                                               ║
║                                                                  ║
║  Format: ○ Excel (.xlsx)  ○ PDF                                 ║
║                                                                  ║
║      [ Vorschau ]    [ Exportieren ]    [ Abbrechen ]           ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.11 Plan suchen

```
╔══════════════════════════════════════════════════════════════════╗
║  Plan suchen                                             _ □ X ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  🔍 [ P-013                                         ]           ║
║                                                                  ║
║  2 Treffer:                                                     ║
║  ╔═══════════════════════════════════════════════════════════╗   ║
║  ║ 📄 202401_P-013 — Grundriß 2.OG, Haus Nr. 64           ║   ║
║  ║    Index: B | Polierplan | Polierplan/2OG/               ║   ║
║  ║    [ Im Explorer öffnen ]  [ Details ]                   ║   ║
║  ╠═══════════════════════════════════════════════════════════╣   ║
║  ║ 📄 202401_P-013 — Index A (archiviert)                  ║   ║
║  ║    Archiv | Polierplan/2OG/_Archiv/                      ║   ║
║  ║    [ Im Explorer öffnen ]                                ║   ║
║  ╚═══════════════════════════════════════════════════════════╝   ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.12 Unbekannte Dateien zuordnen

```
╔══════════════════════════════════════════════════════════════════╗
║  Unbekannte Dateien                                      _ □ X ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  3 Dateien passen in kein Profil:                               ║
║                                                                  ║
║  ╔════════════════════════════════════╦═══════════════════════╗  ║
║  ║ Dateiname                          ║ Aktion               ║  ║
║  ╠════════════════════════════════════╬═══════════════════════╣  ║
║  ║ B-221-B_1.OG Decke Bewehrung.pdf  ║ [ Zuweisen...    ▼ ] ║  ║
║  ║ Detail_Attika_Schnitt.pdf          ║ [ Zuweisen...    ▼ ] ║  ║
║  ║ Zeichnung1.dwl                     ║ [ Zuweisen...    ▼ ] ║  ║
║  ╚════════════════════════════════════╩═══════════════════════╝  ║
║                                                                  ║
║  Dropdown:                                                      ║
║  ╔═══════════════════════════════════╗                           ║
║  ║ Profil erweitern:                ║                           ║
║  ║   Polierplan (S-...)             ║                           ║
║  ║   Schalungsplan (5998-...)       ║                           ║
║  ║ Neues Profil erstellen           ║                           ║
║  ║ Überspringen                     ║                           ║
║  ║ Manuell verschieben              ║                           ║
║  ╚═══════════════════════════════════╝                           ║
║                                                                  ║
║              [ Übernehmen ]         [ Alle überspringen ]       ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.13 Plan korrigieren

```
╔══════════════════════════════════════════════════════════════════╗
║  Plan korrigieren                                        _ □ X ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Datei: S-205-A_Sanitär Anschluss.pdf                          ║
║  Aktuell in: Polierplan/TG/                                     ║
║                                                                  ║
║  ○ Anderen Plantyp zuweisen    → [ Sanitärplan         ▼ ]     ║
║  ○ Anderen Ordner zuweisen     → [ Durchsuchen...        ]     ║
║  ○ Zurück in Eingangsordner                                     ║
║                                                                  ║
║  ☑ Erkennungsmuster anpassen (Fehler zukünftig vermeiden)       ║
║                                                                  ║
║                    [ Anwenden ]         [ Abbrechen ]           ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.14 Erkennungs-Konflikt

```
╔══════════════════════════════════════════════════════════════════╗
║  Zuordnungs-Konflikt                                     _ □ X ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  S-205-A_Sanitär Anschluss.pdf                                  ║
║  passt auf 2 Profile:                                           ║
║                                                                  ║
║  ○ Polierplan    (Präfix "S-")                                  ║
║  ○ Sanitärplan   ("Sanitär" im Namen)                           ║
║                                                                  ║
║  ☑ Für alle ähnlichen Dateien merken                            ║
║                                                                  ║
║              [ Übernehmen ]              [ Überspringen ]       ║
╚══════════════════════════════════════════════════════════════════╝
```

---

## 11. Config-Struktur (planmanager-config.json)

Liegt in `.AppData/BauProjektManager/Projects/<ProjektName>/`

```json
{
  "configVersion": "1.0.0",
  "projectId": "proj_202512_dobl",

  "emptyIndexMeaning": "firstEdition",

  "customFields": [
    { "id": "stiege", "label": "Stiege" },
    { "id": "trakt", "label": "Trakt" }
  ],

  "typeProfiles": [
    {
      "planType": "Polierplan",
      "recognition": [
        { "method": "prefix", "value": "S-" }
      ],
      "separators": ["-", "_"],
      "segments": [
        { "index": 0, "field": "prefix",      "example": "S" },
        { "index": 1, "field": "planNumber",   "example": "101" },
        { "index": 2, "field": "planIndex",    "example": "A" },
        { "index": 3, "field": "description",  "example": "TG Bodenplatte Grundriss" }
      ],
      "folderHierarchy": [
        { "field": "geschoss", "order": 1 }
      ],
      "planListMapping": {
        "format": "xlsx",
        "headerRow": 1,
        "columns": {
          "planNumber": "A",
          "planIndex": "B",
          "description": "F",
          "date": "D"
        }
      },
      "exampleFile": "S-101-A_TG Bodenplatte Grundriss.pdf"
    }
  ],

  "importHistory": [
    {
      "id": "imp_20260326_1432",
      "timestamp": "2026-03-26T14:32:00",
      "source": "_Eingang",
      "actions": [
        {
          "file": "S-113-A_2OG Decke.pdf",
          "partner": "S-113-A_2OG Decke.dwg",
          "action": "new",
          "destination": "Polierplan\\2OG\\",
          "md5Pdf": "abc123...",
          "md5Dwg": "def789..."
        },
        {
          "file": "S-103-D_TG Wämde.pdf",
          "partner": "S-103-D_TG Wämde.dwg",
          "action": "indexUpdate",
          "oldIndex": "C",
          "newIndex": "D",
          "archivedFiles": [
            "Polierplan\\TG\\_Archiv\\S-103-C_TG Wämde.pdf",
            "Polierplan\\TG\\_Archiv\\S-103-C_TG Wämde.dwg"
          ],
          "destination": "Polierplan\\TG\\",
          "md5Pdf": "ghi012...",
          "md5Dwg": "jkl345..."
        }
      ]
    }
  ]
}
```

---

## 12. Cache-Struktur (planmanager-cache.json)

```json
{
  "lastScan": "2026-03-26T14:32:00",
  "plans": [
    {
      "planType": "Polierplan",
      "planNumber": "103",
      "currentIndex": "D",
      "files": [
        {
          "fileName": "S-103-D_TG Wämde-Stützen-Träger.pdf",
          "relativePath": "Polierplan\\TG",
          "md5": "a1b2c3d4e5f6...",
          "fileSize": 2145678,
          "lastModified": "2025-07-21T16:21:00"
        },
        {
          "fileName": "S-103-D_TG Wämde-Stützen-Träger.dwg",
          "relativePath": "Polierplan\\TG",
          "md5": "f6e5d4c3b2a1...",
          "fileSize": 4523100,
          "lastModified": "2025-07-21T16:21:00"
        }
      ],
      "archivedIndexes": ["B", "C"]
    }
  ]
}
```

---

## 13. Zusatz-Features (spätere Phasen)

| Feature | Priorität | Beschreibung |
|---------|-----------|-------------|
| Schnellsuche | Hoch | "Wo liegt Plan X?" |
| Dashboard | Hoch | Übersicht aller Projekte + Status |
| Neue-Pläne-Erkennung | Hoch | Beim Start alle Eingänge prüfen |
| Druck-Liste | Mittel | Aktualisierte Pläne seit Datum X |
| PDF-Vorschau | Mittel | Thumbnail der ersten Seite |
| VERALTET-Stempel | Niedrig | Wasserzeichen auf archivierte PDFs |

---

## 14. Phasenplan

### Phase 0 — Fundament (Woche 1-2)
- [ ] .NET 9 Projekt-Struktur anlegen (Solution + Projects)
- [ ] Registry-Schema implementieren (Lesen/Schreiben)
- [ ] Projekt-Manifest Handling (.bpm-manifest)
- [ ] Logging-System
- [ ] Settings-Handling (.AppData Pfad-Erkennung)
- [ ] Basis-WPF-Fenster (Hauptfenster mit Projektliste)

### Phase 1 — Kern-Import (Woche 3-5)
- [ ] Dateinamen-Parser (Segment-Splitting)
- [ ] Segment-Zuweiser GUI (3-Schritt-Wizard)
- [ ] Plantyp-Erkennung
- [ ] Datei-Vergleich (MD5)
- [ ] Import-Workflow (Vorschau → Bestätigen → Verschieben)
- [ ] Archivierung bei Index-Update
- [ ] Import-History

### Phase 2 — Planlisten (Woche 6-7)
- [ ] Excel-Import (ClosedXML)
- [ ] CSV-Import
- [ ] PDF-Import (Best Effort, PdfPig)
- [ ] Spalten-Zuordnungs-GUI
- [ ] Soll/Ist-Abgleich
- [ ] Export als Excel + PDF

### Phase 3 — Erweitert (Woche 8-10)
- [ ] Fehlerkorrektur + Rückgängig
- [ ] Profil-Lernen (Muster erweitern)
- [ ] Schnellsuche
- [ ] Dashboard
- [ ] Neue-Pläne-Erkennung beim Start
- [ ] Outlook-VBA Helper (Registry lesen)

### Phase 4 — Polish (Woche 11+)
- [ ] PDF-Vorschau
- [ ] VERALTET-Stempel
- [ ] Performance-Optimierung
- [ ] Umfangreiche Tests
- [ ] Dokumentation

---

## 15. Technische Entscheidungen

| Thema | Entscheidung |
|-------|-------------|
| Sprache | C# (.NET 9) |
| GUI | WPF (XAML) |
| Pattern | MVVM (Model-View-ViewModel) |
| IDE | VS Code jetzt → Visual Studio Community später |
| Registry | JSON (VBA-kompatibel, flach) |
| Config-Speicherort | OneDrive `.AppData/BauProjektManager/` |
| Projekt-Manifest | `.bpm-manifest` (hidden) im Projektordner |
| Excel-Library | ClosedXML |
| PDF-Export | QuestPDF |
| PDF-Parsing | PdfPig |
| Testing | xUnit + Moq |
| Versionierung | Git |
| Dateivergleich | MD5-Hash |
| Deployment | Single-file .exe (self-contained) |
| Multi-Device | OneDrive-Sync (kein AppData) |
| Versteckte Dateien | Punkt-Prefix + Hidden-Attribut |

---

## 16. Projektordner-Ergebnis (komplett)

```
OneDrive/02Arbeit/
│
├── .AppData/                                ← Hidden, synct
│   └── BauProjektManager/
│       ├── registry.json
│       ├── settings.json
│       ├── planTypes.json
│       ├── templates.json
│       └── Projects/
│           └── 202512_OeWG-Dobl/
│               ├── planmanager-config.json
│               ├── planmanager-cache.json
│               └── Logs/
│
├── Vorlagen/                               ← Excel/Word Vorlagen
│   ├── Excel/
│   │   ├── Betontabelle_v3.xlsm
│   │   ├── Ziegeltabelle_v2.xlsm
│   │   └── ...
│   ├── Word/
│   │   └── ...
│   └── BPM_Helper.xlam                    ← Add-In (Entscheidung offen)
│
├── 202512_ÖWG-Dobl-Zwaring/               ← Projektordner
│   ├── .bpm-manifest                       ← Hidden
│   ├── Pläne/
│   │   ├── _Eingang/                       ← Sammelordner
│   │   ├── Polierplan/
│   │   │   ├── TG/
│   │   │   │   ├── S-101-A_TG Bodenpl.pdf
│   │   │   │   ├── S-101-A_TG Bodenpl.dwg
│   │   │   │   └── _Archiv/
│   │   │   │       └── S-103-C_TG Wäm.pdf
│   │   │   ├── EG/
│   │   │   └── 1OG/
│   │   ├── Schalungsplan/
│   │   └── Bewehrungsplan/
│   ├── Fotos/
│   ├── Dokumente/
│   ├── Protokolle/
│   └── Rechnungen/
│
└── BauProjektManager/                      ← Die App
    └── PlanManager.exe
```

---

## Alle JSON/Config-Dateien — Komplettübersicht

| Datei | Ort | Zweck | Erstellt von |
|-------|-----|-------|-------------|
| `registry.json` | `.AppData/BauProjektManager/` | Alle Projekte, zentrale Datenquelle | MasterApp / PlanManager |
| `settings.json` | `.AppData/BauProjektManager/` | Globale App-Einstellungen | PlanManager |
| `planTypes.json` | `.AppData/BauProjektManager/` | Erweiterte Plantyp-Liste | PlanManager |
| `templates.json` | `.AppData/BauProjektManager/` | Vorlagen-Verzeichnis | MasterApp |
| `planmanager-config.json` | `.AppData/.../Projects/<Projekt>/` | Plantyp-Profile + Import-History | PlanManager |
| `planmanager-cache.json` | `.AppData/.../Projects/<Projekt>/` | Bestandscache + MD5-Hashes | PlanManager |
| `.bpm-manifest` | Projektordner Root (hidden) | Zeiger auf Registry | MasterApp / PlanManager |

---

*Dokument Version 1.2.0 — 26.03.2026*  
*Änderungen v1.2: Vorlagen-Ordner (02Arbeit/Vorlagen/), templates.json, Excel VBA-Anbindung (Kapitel 5), Config-Übersichtstabelle*  
*Änderungen v1.1: OneDrive-Sync, .AppData, .bpm-manifest, VBA-Kompatibilität, Outlook-Integration, Projektname-Format YYYYMM_Name*