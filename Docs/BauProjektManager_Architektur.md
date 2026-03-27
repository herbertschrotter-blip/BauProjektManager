# BauProjektManager — Architektur & Spezifikation

**Version:** 1.4.0  
**Datum:** 27.03.2026  
**Sprache:** C# (.NET 8 LTS), WPF (XAML), MVVM  
**Frameworks:** CommunityToolkit.Mvvm, Serilog, ClosedXML, PdfPig, QuestPDF  
**Basis:** v1.3 + 2 externe Reviews + 13 verbindliche Entscheidungen  
**Autor:** Herbert + Claude  

---

## 1. Vision & Übersicht

### 1.1 Was ist der BauProjektManager?

Ein modulares Desktop-Tool für Baustellen-Management in Österreich (Steiermark). Eine einzige Anwendung mit internen Feature-Modulen. Offline-fähig, lokal auf OneDrive, kein Cloud-Abo.

### 1.2 Architekturmodell: Modularer Monolith

Eine einzige Anwendung (`BauProjektManager.exe`) mit fest registrierten Feature-Modulen. Kein Plugin-System, kein dynamisches Laden, kein `IBpmModule` Interface. Module sind separate C#-Projekte (DLLs), werden aber direkt im DI-Container als konkrete Typen registriert.

```csharp
// DI-Registrierung: direkt, kein Interface
services.AddTransient<PlanManagerViewModel>();
services.AddTransient<SettingsViewModel>();
// Navigation fest in MainWindow.xaml definiert
```

```
BauProjektManager.exe
├── 📁 PlanManager        ← V1 KERN
├── ⚙️ Einstellungen      ← V1 KERN
├── 📊 Dashboard          ← nach V1 (eigenes Dokument)
├── 📓 Bautagebuch        ← nach V1 (eigenes Dokument)
├── 📧 Outlook-Modul      ← später (eigenes Dokument)
├── 🌤 Wetter-Modul       ← später (eigenes Dokument)
└── 📷 Foto-Modul         ← später (eigenes Dokument)
```

### 1.3 Module und Prioritäten

| Modul | Funktion | Phase | Dokumentation |
|-------|----------|-------|---------------|
| **Einstellungen** | Projekte, Registry, Pfade | V1 Pflicht | Dieses Dokument |
| **PlanManager** | Pläne sortieren, versionieren | V1 Pflicht | Dieses Dokument |
| **Dashboard** | Übersicht + Widgets | Nach V1 | Eigenes Dokument |
| **Bautagebuch** | Tägliches Protokoll, Auto-Befüllung, Export | Nach V1 | Eigenes Dokument |
| **Outlook** | COM Interop, Anhänge extrahieren | Später | Eigenes Dokument |
| **Wetter** | API-Anbindung pro Baustelle | Später | Eigenes Dokument |
| **Foto** | OneDrive-Fotos nach Projekt/Datum | Später | Eigenes Dokument |

### 1.4 Externe Anbindungen

Bestehende VBA-Makros lesen die automatisch generierte `registry.json`:

```
  BauProjektManager.exe ──→ SQLite (System of Record)
                               ↓ automatisch generiert
                            registry.json (read-only)
                               ↓
  Outlook VBA ←────────────────┘  (liest nur, schreibt nie)
  Excel VBA  ←─────────────────┘
  PowerShell (PhotoFolder) ←───┘
```

### 1.5 Multi-Device & OneDrive-Sync

- **Zwei Geräte:** PC zuhause + Laptop auf der Baustelle
- **Sync:** OneDrive synchronisiert Nutzdaten + Konfiguration
- **Operativer State:** Lokal (`%LocalAppData%`), synct NICHT
- **Sortiert auf beiden Geräten:** Ja — Profile synchen über OneDrive
- **Cache-Rebuild:** Wenn auf dem zweiten Gerät gearbeitet wird, baut sich der SQLite-Cache beim ersten Scan automatisch aus dem Dateisystem (OneDrive) neu auf

### 1.6 Projektname-Format

```
Format: YYYYMM_Kurzname
Beispiele:
  202512_ÖWG-Dobl-Zwaring
  202302_Reihenhäuser-Kapfenberg
  202201_Sanierung-Leoben
```

---

## 2. Speicherstrategie

### 2.1 System of Record: SQLite

SQLite ist die **einzige Wahrheitsquelle** für ALLE Daten. JSON-Dateien sind generierte Exporte oder selten geänderte Konfiguration. Wenn JSON korrupt wird → aus SQLite neu generiert.

### 2.2 Dreistufige Speicherung

| Kategorie | Speicherort | Inhalt | Synct? |
|-----------|-------------|--------|--------|
| **Nutzdaten** | OneDrive (Projektordner) | Pläne, Fotos, Dokumente, `_Eingang`, `_Archiv` | Ja |
| **Konfiguration** | OneDrive (`.AppData/`) | `registry.json`, `settings.json`, `profiles.json`, `pattern-templates.json`, `templates.json` | Ja |
| **Operativer State** | Lokal (`%LocalAppData%\BauProjektManager\`) | SQLite-DBs, Logs, Cache, Undo-Journal, Temp | Nein |

### 2.3 Speicher-Matrix (komplett)

| Datei/Daten | Format | Ort | Synct? | Schreiber | Leser | Änderung |
|-------------|--------|-----|--------|-----------|-------|----------|
| Projekt-Stammdaten | SQLite | Lokal `bpm.db` | Nein | C# | C# | Selten |
| `registry.json` | JSON | OneDrive `.AppData/` | Ja | C# (auto) | VBA, C# | Bei Projekt-Änderung |
| `settings.json` | JSON | OneDrive `.AppData/` | Ja | C# | C# | Selten |
| `profiles.json` | JSON | OneDrive `.AppData/Projects/<P>/` | Ja | C# | C# | Beim Anlernen |
| `pattern-templates.json` | JSON | OneDrive `.AppData/` | Ja | C# | C# | Beim Anlernen |
| `templates.json` | JSON | OneDrive `.AppData/` | Ja | C# | C# | Selten |
| Plan-Cache | SQLite | Lokal `planmanager.db` | Nein | C# | C# | Bei Scan/Import |
| Import-Journal | SQLite | Lokal `planmanager.db` | Nein | C# | C# | Bei Import |
| Undo-Daten | SQLite | Lokal `planmanager.db` | Nein | C# | C# | Bei Import |
| Logs | Dateien | Lokal `Logs/` | Nein | Serilog | Dev | Ständig |
| `.bpm-manifest` | JSON | OneDrive Projektordner | Ja | C# | C#, Apps | Einmalig |
| Pläne (PDF/DWG) | Dateien | OneDrive Projektordner | Ja | Import | User | Bei Import |
| Vorlagen | Excel/Word | OneDrive `Vorlagen/` | Ja | User | User, C# | Selten |

### 2.4 Ordnerstruktur (OneDrive)

```
OneDrive/02Arbeit/
│
├── .AppData/                                  ← VERSTECKT, synct
│   └── BauProjektManager/
│       ├── registry.json                      ← Generierter VBA-Export
│       ├── settings.json                      ← App-Einstellungen
│       ├── pattern-templates.json             ← Musterbibliothek
│       ├── templates.json                     ← Vorlagen-Verzeichnis
│       └── Projects/
│           ├── 202512_OeWG-Dobl/
│           │   └── profiles.json              ← Plantyp-Profile
│           └── 202302_Kapfenberg/
│               └── profiles.json
│
├── Vorlagen/                                  ← Excel/Word Vorlagen
│   ├── Excel/
│   │   ├── Betontabelle_v3.xlsm
│   │   ├── Ziegeltabelle_v2.xlsm
│   │   ├── Bautagebuch_v1.xlsm
│   │   └── Stundenzettel_v1.xlsm
│   ├── Word/
│   │   ├── Bautagebuch_Vorlage.dotx
│   │   ├── Bauprotokoll.dotx
│   │   └── Briefkopf.dotx
│   └── BPM_Helper.xlam                       ← Excel Add-In (offen)
│
├── 202512_ÖWG-Dobl-Zwaring/                  ← Projektordner (SAUBER!)
│   ├── .bpm-manifest                          ← Versteckt
│   ├── Pläne/
│   │   ├── _Eingang/                          ← Sammelordner
│   │   ├── Polierplan/
│   │   │   ├── TG/
│   │   │   │   ├── S-101-A_TG Bodenplatte.pdf
│   │   │   │   ├── S-101-A_TG Bodenplatte.dwg
│   │   │   │   └── _Archiv/
│   │   │   │       ├── S-103-B_TG Wämde-Stützen.pdf
│   │   │   │       └── S-103-C_TG Wämde-Stützen.pdf
│   │   │   ├── EG/
│   │   │   │   ├── S-106-B_EG Wämde-Stützen.pdf
│   │   │   │   └── _Archiv/
│   │   │   └── 1OG/
│   │   ├── Schalungsplan/
│   │   │   ├── KG/
│   │   │   └── EG/
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
└── BauProjektManager/                         ← Die App
    └── BauProjektManager.exe
```

### 2.5 Ordnerstruktur (Lokal)

```
%LocalAppData%\BauProjektManager/
├── bpm.db                                     ← Haupt-SQLite
├── Projects/
│   ├── 202512_OeWG-Dobl/
│   │   └── planmanager.db                     ← Cache, Journal, Undo
│   └── 202302_Kapfenberg/
│       └── planmanager.db
├── Logs/
│   ├── BPM_2026-03-26.log
│   └── BPM_2026-03-27.log
├── Backups/
│   └── pre-import/
│       └── 2026-03-27_1432/
│           ├── bpm.db.bak
│           └── planmanager.db.bak
└── Temp/
```

### 2.6 Hidden-Attribute

| Ordner/Datei | Hidden? | Warum |
|---|---|---|
| `.AppData/` | Ja (Hidden + System) | Nicht für User sichtbar |
| `.bpm-manifest` | Ja (Hidden) | Nicht für Kollegen sichtbar |
| `_Eingang/` | Nein | User muss hier Dateien reinwerfen |
| `_Archiv/` | Nein | User soll alte Pläne finden |

---

## 3. Zentrale Projekt-Registry

### 3.1 SQLite als System of Record

Alle Projektdaten in SQLite (`bpm.db`). Bei jeder Änderung wird `registry.json` automatisch als flacher Export generiert. VBA liest NUR den Export.

### 3.2 Internes Domänenmodell (C#)

```csharp
public class Project
{
    public string Id { get; set; }                    // "proj_202512_dobl"
    public string ProjectNumber { get; set; }         // "202512"
    public string Name { get; set; }                  // "ÖWG-Dobl-Zwaring"
    public string FullName { get; set; }              // "Gartensiedlung Dobl-Zwaring"
    public ProjectStatus Status { get; set; }         // Active, Completed, Archived
    public ProjectLocation Location { get; set; }
    public ProjectTimeline Timeline { get; set; }
    public List<Building> Buildings { get; set; }
    public ProjectPaths Paths { get; set; }
    public string Tags { get; set; }
    public string Notes { get; set; }
}

public class ProjectLocation
{
    public string Address { get; set; }               // "Hauptstraße 15, 8143 Dobl"
    public string Municipality { get; set; }          // "Dobl-Zwaring"
    public string District { get; set; }              // "Graz-Umgebung"
    public string State { get; set; }                 // "Steiermark"
    public string CoordinateSystem { get; set; }      // "EPSG:31258"
    public double CoordinateEast { get; set; }
    public double CoordinateNorth { get; set; }
    public string CadastralKg { get; set; }           // "63201"
    public string CadastralKgName { get; set; }       // "Dobl"
    public string CadastralGst { get; set; }          // "123/1, 123/2, 124"
}

public class ProjectTimeline
{
    public DateTime? ProjectStart { get; set; }
    public DateTime? ConstructionStart { get; set; }
    public DateTime? PlannedEnd { get; set; }
    public DateTime? ActualEnd { get; set; }
}

public class Building
{
    public string Id { get; set; }                    // "bldg_64"
    public string Name { get; set; }                  // "Haus Nr. 64"
    public string ShortName { get; set; }             // "H64"
    public string Type { get; set; }                  // "Reihenhaus"
    public List<string> Levels { get; set; }          // ["KG","EG","1.OG","2.OG","Dach"]
}

public class ProjectPaths
{
    public string Root { get; set; }                  // Absoluter Pfad
    public string Plans { get; set; }                 // "Pläne" (relativ zu Root)
    public string Inbox { get; set; }                 // "Pläne\\_Eingang"
    public string Photos { get; set; }                // "Fotos"
    public string Documents { get; set; }             // "Dokumente"
    public string Protocols { get; set; }             // "Protokolle"
    public string Invoices { get; set; }              // "Rechnungen"
}

public enum ProjectStatus { Active, Completed, Archived }
```

### 3.3 registry.json (Generierter Export — flach für VBA)

```json
{
  "registryVersion": "1.0",
  "generatedAt": "2026-03-27T10:00:00",
  "settings": {
    "basePath": "C:\\Users\\Herbert\\OneDrive\\02Arbeit",
    "appDataPath": "...\\02Arbeit\\.AppData\\BauProjektManager",
    "templatesPath": "...\\02Arbeit\\Vorlagen",
    "folderTemplate": "{projectNumber}_{projectName}"
  },
  "planTypes": [
    "Polierplan", "Schalungsplan", "Bewehrungsplan", "Elektroplan",
    "HKLS-Plan", "Detailplan", "Architekturplan", "Lageplan",
    "Grundrissplan", "Schnittplan"
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

### 3.4 VBA liest Registry (Pseudo-Code)

```vba
Function GetRegistryPath() As String
    GetRegistryPath = Environ("OneDrive") & _
        "\02Arbeit\.AppData\BauProjektManager\registry.json"
End Function

Sub SyncOutlookFolders()
    Dim json As String
    json = ReadTextFile(GetRegistryPath())

    For Each project In ParseJSON(json)("projects")
        If project("status") = "active" Then
            ' Outlook-Ordner erstellen
            Dim folderName As String
            folderName = project("projectNumber") & "_" & project("name")
            CreateOutlookFolder folderName

            ' Eingangsordner sicherstellen
            Dim inboxPath As String
            inboxPath = project("rootPath") & "\" & project("inboxPath")
            If Not FolderExists(inboxPath) Then MkDir inboxPath
        End If
    Next
End Sub
```

### 3.5 VBA-Kompatibilitäts-Regeln

| Regel | Grund |
|-------|-------|
| Keine verschachtelten Objekte im Export | VBA JSON-Parser sind einfach |
| Koordinaten als separate Felder | `coordinateEast` statt `coordinates.east` |
| Pfade relativ zu `rootPath` | VBA baut zusammen mit `rootPath & "\" & plansPath` |
| Buildings als Pipe-String | VBA `Split(buildings, "|")` → Array |
| Tags als Komma-String | VBA `Split(tags, ", ")` → Array |
| Datum als `YYYY-MM-DD` | VBA `CDate("2024-01-15")` |
| VBA liest NUR, schreibt NIE | C#-App ist einziger Writer |

### 3.6 .bpm-manifest

```json
{
  "registryId": "proj_202512_dobl",
  "projectNumber": "202512",
  "name": "ÖWG-Dobl-Zwaring",
  "registryPath": "C:\\Users\\Herbert\\OneDrive\\02Arbeit\\.AppData\\BauProjektManager\\registry.json"
}
```

---

## 4. PlanManager — Kernfunktionen

### 4.1 Workflow-Übersicht

```
1. Projekt aus Registry laden (oder neues anlegen)
2. Plantyp-Profil erstellen (Muster anlernen — mit Vorschlägen)
3. Pläne in _Eingang werfen (manuell / Outlook / Portal)
4. Import starten → 10-Schritte-Workflow → Bestätigen
5. Pläne werden automatisch einsortiert
```

### 4.2 Plan-Dateien: Flexibles Modell

Ein Plan (Revision) besteht aus **1 bis n Dateien**. Kein festes PDF/DWG-Paar.

```
PlanRevision (z.B. "S-103, Index D")
├── PlanFile: S-103-D_TG Wämde.pdf         (Typ: PDF)
├── PlanFile: S-103-D_TG Wämde.dwg         (Typ: DWG)
└── (optional weitere)

Oder nur PDF:
PlanRevision (z.B. "S-103, Index D")
└── PlanFile: S-103-D_TG Wämde.pdf

Oder mehrere PDFs:
PlanRevision (z.B. "B-221, Index B")
├── PlanFile: B-221-B_1OG Decke Teil 1.pdf
└── PlanFile: B-221-B_1OG Decke Teil 2.pdf

Oder nur DWG:
PlanRevision (z.B. "5998-003")
└── PlanFile: 5998-003_Wände_KG.dwg
```

Dateien werden über den gemeinsamen Dateinamen-Stamm (ohne Extension) zusammengeführt. Fehlende PDF oder DWG ist KEIN Fehler.

### 4.3 Sammelordner (Briefkasten)

- Fester `_Eingang/` pro Projekt unter `Pläne/`
- Quellen: Email (manuell oder Outlook-VBA), Portal-Download, USB, manuell
- Nach Import: Dateien werden **verschoben** (Eingang wird leer)
- Beim App-Start: Prüfe alle Eingänge → Benachrichtigung

### 4.4 Index-Versionierung

| Situation | Aktion |
|-----------|--------|
| Neuer Plan (Nummer existiert nicht) | Einsortieren in Zielordner |
| Neuer Index (z.B. C→D) | Alle Dateien des alten Index → `_Archiv/`, neue an Stelle |
| Gleicher Index, geänderte Datei (MD5 anders) | Überschreiben (kein Archiv) |
| Gleicher Index, identische Datei (MD5 gleich) | Überspringen |
| Leerer Index | User definiert pro Projekt Bedeutung |

### 4.5 Bestandsverwaltung

- **Hybrid:** Filesystem-Scan + SQLite-Cache
- **MD5-Hash** für exakten Dateivergleich
- **Cache-Rebuild:** Auf neuem Gerät baut sich Cache beim ersten Scan automatisch aus Dateisystem (OneDrive) auf
- **Import-History:** Wann, was, wohin (für Rückgängig) in SQLite

---

## 5. PlanManager — Dateinamen-Parsing

### 5.1 Hybrid-Mechanismus

1. **Segmente:** Dateiname an Trennzeichen splitten → klickbare Blöcke in der GUI
2. **Zeichen-Level:** Fallback per Toggle-Button für Feinauswahl innerhalb eines Segments

### 5.2 Praxis-Beispiele

```
Screen 1 (Polierplan):
  "S-103-C_TG Wämde-Stützen-Träg + Decke ü.TG Grundriss.pdf"
  Split [-_]: [S] [103] [C] [TG Wämde-Stützen-Träg + Decke ü.TG Grundriss]
  Zuweisung:  Pref  Nr   Idx  Bezeichnung

Screen 2 (Schalungsplan):
  "5998-003_Wände_KG_Teil_1.pdf"
  Split [-_]: [5998] [003] [Wände] [KG] [Teil] [1]
  Zuweisung:  ProjNr  Nr   Objekt Gesch  ign  Idx

Screen 3 (Architekturplan):
  "21005_104_AP_H1_GR_E2_05_Grundriss E+2.pdf"
  Split [_]:  [21005] [104] [AP] [H1] [GR] [E2] [05] [Grundriss E+2]
  Zuweisung:  ProjNr   Nr   Typ  Haus Plan Gesch Idx  Bezeichnung

Screen 4 (Schalungsplan):
  "21-2094_404_A_Wände 20G_Haus 2_-_Schalung.pdf"
  Split [_]:  [21-2094] [404] [A] [Wände 20G] [Haus 2] [-] [Schalung]
  Zuweisung:  ProjNr     Nr   Idx  Objekt      Haus     ign  Plantyp
```

### 5.3 Verfügbare Feld-Typen

**Pflicht:** `planNumber`, `planIndex`

**System-Felder:** `projectNumber`, `description`, `ignore`

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

### 5.4 Ordner-Hierarchie

- **Plantyp** IMMER Ebene 1 (fix, nicht abwählbar)
- Darunter: User wählt per Checkbox welche Felder Ordner-Ebenen werden
- Reihenfolge frei sortierbar (↑↓ Pfeile)
- Wird beim Profil-Erstellen festgelegt, nur bei Neuerstellung änderbar

**Beispiele:**

```
Wenig Pläne:   /Polierplan/datei.pdf
Normal:        /Polierplan/EG/datei.pdf
Mittel:        /Polierplan/H64/EG/datei.pdf
Komplex:       /Architekturplan/Grundriss/H1/E+2/datei.pdf
```

### 5.5 Plantyp-Erkennung

- Automatisch per gespeichertem Muster
- **Methoden:** `prefix` (beginnt mit), `contains` (enthält), `regex` (komplex)
- **Mehrere Muster pro Profil** → Profil "lernt" mit der Zeit
- **Konflikt:** Spezifischeres Muster gewinnt. Bei Gleichstand → User-Dialog
- **Plantyp-Liste:** 10 vordefiniert + User kann dauerhaft erweitern

### 5.6 Vorschlags- und Profil-System

| Konzept | Zweck | Gespeichert | Wann erstellt |
|---------|-------|-------------|---------------|
| **RecognitionProfile** | Verbindlich pro Projekt/Plantyp | `profiles.json` (OneDrive, pro Projekt) | Beim Anlernen |
| **PatternTemplate** | Vorschlag aus Musterbibliothek | `pattern-templates.json` (OneDrive, global) | Automatisch nach jedem Profil |

**Workflow beim neuen Profil:**
1. User wählt Beispieldateien aus dem `_Eingang`
2. System vergleicht mit bestehenden PatternTemplates
3. Match? → "Sieht aus wie Muster 'Statik_S-Prefix' — übernehmen?"
4. User bestätigt → RecognitionProfile wird erstellt
5. User lehnt ab → manuell definieren
6. Neues Profil wird automatisch als PatternTemplate gespeichert

**Regeln:**
- Kein Machine Learning, keine Blackbox
- Immer User-Bestätigung
- PatternTemplates sind nur Vorschläge, nie automatische Wahrheit
- RecognitionProfiles sind verbindlich pro Projekt

### 5.7 profiles.json (pro Projekt)

```json
{
  "schemaVersion": "1.0",
  "projectId": "proj_202512_dobl",
  "emptyIndexMeaning": "firstEdition",
  "customFields": [
    { "id": "stiege", "label": "Stiege" }
  ],
  "typeProfiles": [
    {
      "id": "prof_polier_001",
      "planType": "Polierplan",
      "recognition": [
        { "method": "prefix", "value": "S-" },
        { "method": "prefix", "value": "ST-" }
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
      "exampleFile": "S-101-A_TG Bodenplatte Grundriss.pdf",
      "createdAt": "2026-03-26T14:00:00"
    },
    {
      "id": "prof_schal_001",
      "planType": "Schalungsplan",
      "recognition": [
        { "method": "prefix", "value": "5998-" }
      ],
      "separators": ["_", "-"],
      "segments": [
        { "index": 0, "field": "projectNumber", "example": "5998" },
        { "index": 1, "field": "planNumber",     "example": "003" },
        { "index": 2, "field": "objekt",         "example": "Wände" },
        { "index": 3, "field": "geschoss",       "example": "KG" },
        { "index": 4, "field": "ignore",         "example": "Teil" },
        { "index": 5, "field": "planIndex",      "example": "1" }
      ],
      "folderHierarchy": [
        { "field": "geschoss", "order": 1 }
      ],
      "exampleFile": "5998-003_Wände_KG_Teil_1.pdf",
      "createdAt": "2026-03-26T14:30:00"
    }
  ]
}
```

### 5.8 pattern-templates.json (global)

```json
{
  "schemaVersion": "1.0",
  "templates": [
    {
      "id": "tpl_statik_s_prefix",
      "name": "Statik S-Prefix",
      "description": "Polierplan mit S-NNN-X Format",
      "sourceProject": "202512_ÖWG-Dobl-Zwaring",
      "separators": ["-", "_"],
      "segments": [
        { "index": 0, "field": "prefix",      "example": "S" },
        { "index": 1, "field": "planNumber",   "example": "101" },
        { "index": 2, "field": "planIndex",    "example": "A" },
        { "index": 3, "field": "description",  "example": "..." }
      ],
      "recognitionHint": { "method": "prefix", "value": "S-" },
      "createdAt": "2026-03-26T14:00:00",
      "usedCount": 3
    }
  ]
}
```


---

## 6. PlanManager — Import-Workflow (10 Schritte)

### 6.1 Übersicht

| # | Schritt | Was passiert | User sieht |
|---|---------|-------------|-----------|
| 1 | **Scan** | `_Eingang` durchsuchen, alle Dateien auflisten | Nein |
| 2 | **Parse** | Dateinamen in Segmente splitten, Felder extrahieren | Nein |
| 3 | **Validate** | Profil vorhanden? Pflichtfelder erkannt? Dateityp ok? | Nein |
| 4 | **Classify** | Plantyp erkennen, Status bestimmen | Nein |
| 5 | **Plan** | Zielpfad berechnen, Dateien zu Revisionen gruppieren (1..n) | Nein |
| 6 | **Preview** | Ergebnis anzeigen, User kann korrigieren | **JA** |
| 7 | **Execute** | Journal schreiben (pending) → Dateien verschieben → Cache updaten | Fortschritt |
| 8 | **Finalize** | Journal-Status "completed", Eingang aufräumen | Nein |
| 9 | **Recover** | Beim App-Start: pending-Einträge → Reparatur anbieten | Nur bei Bedarf |
| 10 | **Undo** | Journal rückwärts, Dateien zurück, Status "undone" | Auf Knopfdruck |

### 6.2 Detail pro Schritt

**Schritt 1 — Scan:**
- Input: Pfad zu `_Eingang/`
- Output: Liste aller Dateien (Name, Extension, Größe, Datum)
- Fehler: Ordner existiert nicht → Fehlermeldung

**Schritt 2 — Parse:**
- Input: Dateiliste + aktive Profile des Projekts
- Output: Geparste Dateien mit extrahierten Segmenten
- Fehler: Unbekanntes Muster → markiert als "Unknown"

**Schritt 3 — Validate:**
- Input: Geparste Dateien
- Output: Validierte Dateien (Pflichtfelder vorhanden?)
- Prüft: planNumber erkannt? planIndex erkannt? Dateityp (pdf/dwg/other)?
- Fehler: Pflichtfeld fehlt → markiert als "Invalid"

**Schritt 4 — Classify:**
- Input: Validierte Dateien + Plan-Cache (SQLite)
- Output: Klassifizierte Dateien mit Status
- Status: `New` (nicht im Bestand), `IndexUpdate` (neuer Index), `Overwrite` (gleicher Index, andere MD5), `Skip` (gleicher Index, gleiche MD5), `Unknown` (nicht erkannt), `Conflict` (Mehrdeutigkeit)

**Schritt 5 — Plan:**
- Input: Klassifizierte Dateien + Ordner-Hierarchie aus Profil
- Output: Geplante Aktionen mit Zielpfaden
- Gruppiert Dateien mit gleichem Stamm zu einer Revision (1..n)
- Berechnet: Zielordner (z.B. `Polierplan/TG/`), Archiv-Pfad (bei IndexUpdate)

**Schritt 6 — Preview:**
- Input: Geplante Aktionen
- Output: User-Bestätigung oder -Korrektur
- **GUI-Dialog** (siehe Kapitel 8)
- Rechtsklick: Plantyp ändern, Ordner ändern, Überspringen

**Schritt 7 — Execute:**
- Input: Bestätigte Aktionen
- Ablauf:
  1. Backup der SQLite-DB erstellen
  2. Journal-Eintrag schreiben (Status "pending")
  3. Pro Aktion (in Reihenfolge):
     a. Journal-Action schreiben (Status "pending")
     b. Bei IndexUpdate: Alte Dateien → `_Archiv/` verschieben
     c. Neue Dateien → Zielordner verschieben
     d. Journal-Action Status → "completed"
  4. Cache aktualisieren (MD5, Pfade)
- Fehler: Bei Fehler in einer Aktion → Status "failed", restliche Aktionen abbrechen

**Schritt 8 — Finalize:**
- Journal-Status → "completed"
- Prüfe ob `_Eingang` leer ist (sollte er nach erfolgreichem Import)
- Log-Eintrag: "Import completed: X actions"

**Schritt 9 — Recover (beim App-Start):**
- Prüfe: Gibt es Journal-Einträge mit Status "pending"?
- Ja → Dialog: "Letzter Import wurde unterbrochen. Reparieren?"
  - Reparieren: Ausstehende Aktionen rückgängig machen
  - Ignorieren: Journal bleibt (User kann manuell aufräumen)

**Schritt 10 — Undo:**
- Input: Import-ID
- Ablauf:
  1. Journal-Actions rückwärts lesen (höchster `action_order` zuerst)
  2. Pro Aktion:
     a. Dateien zurück an Quellpfad verschieben
     b. Bei IndexUpdate: Archivierte Dateien zurück an Originalplatz
  3. Cache aktualisieren
  4. Journal-Status → "undone"

---

## 7. PlanManager — Fehlerbehandlung (3 Stufen)

| Stufe | Wann | Was |
|-------|------|-----|
| **Vorschau** | VOR Import | Rechtsklick → Plantyp ändern, Ordner ändern, Überspringen |
| **Rückgängig** | NACH Import | Gesamter Import oder einzelne Aktionen zurücknehmen |
| **Muster lernen** | Bei Korrektur | Erkennungsmuster verfeinern, neues Template speichern |

### Unbekannte Dateien

Dialog: Profil erweitern / Neues Profil / Überspringen / Manuell verschieben

### Plan korrigieren

Rechtsklick auf eine Zeile in der Vorschau:
- Plantyp ändern (Dropdown)
- Zielordner ändern (Durchsuchen)
- Index manuell setzen
- Überspringen

### Erkennungs-Konflikt

Wenn eine Datei zu mehreren Profilen passt:
- Dialog zeigt alle Treffer
- User wählt das richtige Profil
- Optional: Muster des verlierer-Profils anpassen

---

## 8. PlanManager — Undo-Journal (SQLite)

### 3 Tabellen

```sql
CREATE TABLE import_journal (
    id TEXT PRIMARY KEY,                -- "imp_20260326_143200"
    timestamp TEXT NOT NULL,
    completed_at TEXT,
    status TEXT NOT NULL,               -- "pending", "completed", "failed", "undone"
    source_path TEXT NOT NULL,
    file_count INTEGER NOT NULL,
    profile_id TEXT,
    machine_name TEXT,
    error_message TEXT
);

CREATE TABLE import_actions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    import_id TEXT NOT NULL,
    action_order INTEGER NOT NULL,      -- Reihenfolge für Undo
    action_type TEXT NOT NULL,          -- "new", "indexUpdate", "overwrite", "skip"
    action_status TEXT NOT NULL,        -- "pending", "completed", "failed"
    started_at TEXT,
    completed_at TEXT,
    plan_number TEXT NOT NULL,
    plan_index TEXT,
    old_index TEXT,
    destination_path TEXT NOT NULL,
    archive_path TEXT,
    error_message TEXT,
    FOREIGN KEY (import_id) REFERENCES import_journal(id)
);

CREATE TABLE import_action_files (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    action_id INTEGER NOT NULL,
    file_name TEXT NOT NULL,
    file_type TEXT NOT NULL,            -- "pdf", "dwg", "other"
    source_path TEXT NOT NULL,
    destination_path TEXT NOT NULL,
    md5_hash TEXT,
    file_size INTEGER,
    FOREIGN KEY (action_id) REFERENCES import_actions(id)
);
```

---

## 9. Planlisten (V1.1)

### 9.1 Import

**Formate:** Excel (.xlsx), CSV. PDF (Best Effort mit PdfPig) → nach V1.

**Spalten-Zuordnung:** Angelernt pro Plantyp. User weist Spalten den Feldern zu (Plan-Nr, Index, Bezeichnung, Datum). Zuordnung wird im Profil gespeichert.

**Abgleich-Ergebnis:**

| Status | Symbol | Bedeutung |
|--------|--------|-----------|
| Aktuell | ✅ | Index stimmt überein |
| Veraltet | ⚠️ | User hat älteren Index |
| Fehlend | ❌ | In Planliste aber nicht im Bestand |
| Extra | ℹ️ | Im Bestand aber nicht in Planliste |

### 9.2 Export

- Plantypen wählen (Checkboxen)
- Spalten wählen (Checkboxen)
- Archiv-Pläne: Nein / Separates Blatt / Mit Markierung
- Sortierung: Mehrstufig, frei wählbar
- Format: Excel (.xlsx via ClosedXML) oder PDF (via QuestPDF)

---

## 10. GUI-Mockups (Kernprogramm)

### 10.1 Shell (Hauptfenster)

```
╔══════════════════════════════════════════════════════════════════╗
║  BauProjektManager                                      _ □ X  ║
╠══════════════════════════════════════════════════════════════════╣
║  ┌──────────┐                                                    ║
║  │📁 Pläne   │  Inhalt des gewählten Moduls wird hier           ║
║  │⚙ Settings│  angezeigt (ContentFrame)                         ║
║  │          │                                                    ║
║  │          │                                                    ║
║  │          │                                                    ║
║  └──────────┘                                                    ║
║  ── Statusleiste ───────────────────────────────────────────── ║
║  Projekt: 202512_ÖWG-Dobl-Zwaring | Registry OK | 3 Projekte  ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.2 Einstellungen — Projektliste

```
╔══════════════════════════════════════════════════════════════════╗
║  Einstellungen — Projekte                                      ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  ╔══════════════════╦════════╦══════════════╦═════════════════╗  ║
║  ║ Projekt           ║ Nr.    ║ Status       ║ Pläne-Pfad     ║  ║
║  ╠══════════════════╬════════╬══════════════╬═════════════════╣  ║
║  ║ ÖWG-Dobl-Zwaring ║ 202512 ║ 🟢 Aktiv    ║ ...\Pläne      ║  ║
║  ║ Kapfenberg        ║ 202302 ║ 🟢 Aktiv    ║ ...\Pläne      ║  ║
║  ║ Sanierung Leoben  ║ 202201 ║ 🔴 Fertig   ║ ...\Pläne      ║  ║
║  ╚══════════════════╩════════╩══════════════╩═════════════════╝  ║
║                                                                  ║
║  [ + Neues Projekt ]  [ Bearbeiten ]  [ Archivieren ]           ║
║                                                                  ║
║  ── Pfade ─────────────────────────────────────────────────     ║
║  Basis:     [ C:\Users\Herbert\OneDrive\02Arbeit     ] [📁]    ║
║  AppData:   [ ...\.AppData\BauProjektManager         ] [📁]    ║
║  Vorlagen:  [ ...\Vorlagen                           ] [📁]    ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.3 Einstellungen — Projekt-Detail

```
╔══════════════════════════════════════════════════════════════════╗
║  Projekt bearbeiten — 202512_ÖWG-Dobl-Zwaring                 ║
╠══════════════════════════════════════════════════════════════════╣
║  ── Stammdaten ─────────────────────────────────────────────   ║
║  Projektname:   [ ÖWG-Dobl-Zwaring                         ]  ║
║  Voller Name:   [ Gartensiedlung Dobl-Zwaring               ]  ║
║  Nummer:        [ 202512     ]   Status: [ 🟢 Aktiv ▼ ]       ║
║                                                                  ║
║  ── Adresse ────────────────────────────────────────────────   ║
║  Adresse:       [ Hauptstraße 15, 8143 Dobl-Zwaring         ]  ║
║  Gemeinde:      [ Dobl-Zwaring    ]  Bezirk: [ Graz-Umgeb. ]  ║
║  Bundesland:    [ Steiermark      ]                             ║
║                                                                  ║
║  ── Koordinaten ────────────────────────────────────────────   ║
║  System: [ EPSG:31258 ▼ ]                                       ║
║  Ost:    [ 450123.45     ]   Nord:  [ 5210678.90    ]          ║
║                                                                  ║
║  ── Grundstück ─────────────────────────────────────────────   ║
║  KG:     [ 63201   ]  KG-Name: [ Dobl         ]               ║
║  GST:    [ 123/1, 123/2, 124                   ]               ║
║                                                                  ║
║  ── Gebäude ────────────────────────────────────────────────   ║
║  ╔══════╦════════════╦═══════════╦═══════════════════════════╗  ║
║  ║ Kurz ║ Name       ║ Typ       ║ Geschoße                 ║  ║
║  ║ H64  ║ Haus Nr.64 ║ Reihenhaus║ KG, EG, 1.OG, 2.OG, Dach║  ║
║  ║ H66  ║ Haus Nr.66 ║ Reihenhaus║ EG, OG, Dach             ║  ║
║  ║ H68  ║ Haus Nr.68 ║ Reihenhaus║ EG, 1.OG, 2.OG, Dach    ║  ║
║  ╚══════╩════════════╩═══════════╩═══════════════════════════╝  ║
║  [ + Gebäude ]  [ Bearbeiten ]  [ Entfernen ]                  ║
║                                                                  ║
║  ── Laufzeit ───────────────────────────────────────────────   ║
║  Projektstart:  [ 15.01.2024 ]   Baustart:  [ 01.06.2024 ]    ║
║  Geplantes Ende:[ 31.12.2026 ]   Tats. Ende:[ __________ ]    ║
║                                                                  ║
║  ── Pfade ──────────────────────────────────────────────────   ║
║  Root:   [ C:\Users\Herbert\OneDrive\02Arbeit\202512_ÖWG-.. ]  ║
║  Pläne:  [ Pläne            ]  Eingang:  [ Pläne\_Eingang  ]   ║
║  Fotos:  [ Fotos            ]  Dokumente:[ Dokumente       ]   ║
║                                                                  ║
║  Tags:   [ Wohnbau, Reihenhäuser, ÖWGES                    ]   ║
║  Notizen:[ Bauteil B-13, 3 Häuser                          ]   ║
║                                                                  ║
║             [ Speichern ]                [ Abbrechen ]          ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.4 PlanManager — Projekt-Detailansicht

```
╔══════════════════════════════════════════════════════════════════╗
║  ← Zurück    202512_ÖWG-Dobl-Zwaring                          ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  📁 Pläne:   ...\Pläne                                          ║
║  📥 Eingang: ...\Pläne\_Eingang (5 Dateien)                     ║
║                                                                  ║
║  ╔══════════════════╦═══════╦════════════════╦═════════════════╗ ║
║  ║ Plantyp          ║ Pläne ║ Letzter Import ║ Status          ║ ║
║  ╠══════════════════╬═══════╬════════════════╬═════════════════╣ ║
║  ║ Polierplan       ║  28   ║ 24.03.2026     ║ ✅ Aktuell     ║ ║
║  ║ Schalungsplan    ║  14   ║ 20.03.2026     ║ ⚠️ 2 veraltet ║ ║
║  ║ Bewehrungsplan   ║  22   ║ 22.03.2026     ║ ✅ Aktuell     ║ ║
║  ╚══════════════════╩═══════╩════════════════╩═════════════════╝ ║
║                                                                  ║
║  [ + Plantyp ]  [ 📥 Import ]  [ 📋 Planliste ]  [ 🔍 Suche ] ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.5 Plantyp hinzufügen — Schritt 1/3: Typ wählen

```
╔══════════════════════════════════════════════════════════════════╗
║  Plantyp hinzufügen — Schritt 1 von 3                         ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Plantyp: [ Polierplan                    ▼ ]                   ║
║           [ Benutzerdefiniert: __________ ]                      ║
║                                                                  ║
║  💡 Vorschlag aus Musterbibliothek:                              ║
║  ┌──────────────────────────────────────────────────────────┐   ║
║  │ "Statik S-Prefix" (aus Projekt Dobl-Zwaring)            │   ║
║  │ Muster: S-NNN-X_Bezeichnung                              │   ║
║  │                              [ Übernehmen ] [ Nein ]     │   ║
║  └──────────────────────────────────────────────────────────┘   ║
║                                                                  ║
║  Beispieldateien laden aus:                                     ║
║  [ ...\Pläne\_Eingang                    ] [ Durchsuchen ]      ║
║  47 Dateien gefunden (24 PDF, 23 DWG)                           ║
║                                                                  ║
║                              [ Weiter → ]    [ Abbrechen ]     ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.6 Plantyp hinzufügen — Schritt 2/3: Muster definieren

```
╔══════════════════════════════════════════════════════════════════╗
║  Muster definieren — Schritt 2 von 3                           ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Trennzeichen: [ - ☑ ] [ _ ☑ ] [ . ☐ ] [ + Eigenes ]          ║
║                                                                  ║
║  Segmente — klicke zum Zuweisen:                                ║
║  ╭───────╮  ╭───────╮  ╭───────╮  ╭──────────────────────╮     ║
║  │   S   │  │  103  │  │   C   │  │ TG Wämde-Stützen... │     ║
║  │ Prefix│  │ Nr.   │  │ Index │  │ Bezeichnung          │     ║
║  ╰───────╯  ╰───────╯  ╰───────╯  ╰──────────────────────╯     ║
║                                                                  ║
║  Zuweisen als: [ Plan-Nummer ▼ ] [ + Neues Feld ]              ║
║  ☐ Zeichen-Level anzeigen (Feinauswahl innerhalb Segment)       ║
║                                                                  ║
║  Erkennungsmuster: Segment 0 = [ S ] → "ist Polierplan"        ║
║  Methode: [ Prefix ▼ ]  Wert: [ S- ]                           ║
║  [ + Weiteres Muster ]                                           ║
║                                                                  ║
║  Live-Vorschau (alle Dateien im Eingang):                       ║
║  ╔════════════════════════════════╦═══════╦═══════╦══════╗      ║
║  ║ Dateiname                      ║  Nr.  ║ Index ║ OK?  ║      ║
║  ╠════════════════════════════════╬═══════╬═══════╬══════╣      ║
║  ║ S-101-A_TG Bodenplatte.pdf    ║  101  ║   A   ║  ✅  ║      ║
║  ║ S-103-C_TG Wämde-Stützen...   ║  103  ║   C   ║  ✅  ║      ║
║  ║ S-106-B_EG Wämde-Stützen...   ║  106  ║   B   ║  ✅  ║      ║
║  ║ 5998-003_Wände_KG.pdf         ║  ---  ║  ---  ║  ❌  ║      ║
║  ╚════════════════════════════════╩═══════╩═══════╩══════╝      ║
║  ✅ 28/47 geparst (19 anderen Typs)                             ║
║                                                                  ║
║           [ ← Zurück ]    [ Weiter → ]       [ Abbrechen ]     ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.7 Plantyp hinzufügen — Schritt 3/3: Ordnerstruktur

```
╔══════════════════════════════════════════════════════════════════╗
║  Ordnerstruktur — Schritt 3 von 3                              ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Nicht als Ordner:       Ordner-Hierarchie:                     ║
║  ╔══════════════════╗    ╔═══════════════════════════════╗      ║
║  ║ Projekt-Nr       ║    ║ Ebene 1: Geschoß       [↑][↓]║      ║
║  ║ Plan-Nr          ║    ║ Ebene 2: Haus          [↑][↓]║      ║
║  ║ Index            ║ →→ ║ [ + Ebene hinzufügen ]       ║      ║
║  ║ Bezeichnung      ║    ╚═══════════════════════════════╝      ║
║  ╚══════════════════╝                                            ║
║                                                                  ║
║  Leerer Index bedeutet:                                         ║
║  ○ Erstausgabe (häufigster Fall)                                ║
║  ○ Nachfragen bei jedem Import                                  ║
║  ○ Eigene Bedeutung: [ ________________ ]                       ║
║                                                                  ║
║  Vorschau:                                                       ║
║  📁 Polierplan/                                                  ║
║  ├── 📁 TG/                                                     ║
║  │   ├── S-101-A_TG Bodenplatte.pdf                            ║
║  │   ├── S-101-A_TG Bodenplatte.dwg                            ║
║  │   ├── S-103-C_TG Wämde-Stützen.pdf                         ║
║  │   └── 📁 _Archiv/                                            ║
║  ├── 📁 EG/                                                     ║
║  │   ├── S-106-B_EG Wämde-Stützen.pdf                         ║
║  │   └── ...                                                    ║
║  └── 📁 1OG/                                                    ║
║                                                                  ║
║           [ ← Zurück ]    [ Übernehmen ]     [ Abbrechen ]     ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.8 Import — Vorschau (Hauptdialog)

```
╔══════════════════════════════════════════════════════════════════╗
║  Import — Vorschau                                             ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Projekt: 202512_ÖWG-Dobl-Zwaring | Quelle: _Eingang (12)     ║
║                                                                  ║
║  ╔════════╦════════════════════════════╦══════════╦═════╦══════╗║
║  ║ Status ║ Dateiname                  ║ Plantyp  ║ Idx ║ Ziel ║║
║  ╠════════╬════════════════════════════╬══════════╬═════╬══════╣║
║  ║  🆕    ║ S-113-A_2OG Decke.pdf     ║ Polier   ║  A  ║ /2OG ║║
║  ║  🆕    ║ S-113-A_2OG Decke.dwg     ║ Polier   ║  A  ║ /2OG ║║
║  ║  📈    ║ S-103-D_TG Wämde...pdf    ║ Polier   ║ C→D ║ /TG  ║║
║  ║  📈    ║ S-103-D_TG Wämde...dwg    ║ Polier   ║ C→D ║ /TG  ║║
║  ║  ✅    ║ S-101-A_TG Boden...pdf    ║ Polier   ║  =  ║ skip ║║
║  ║  ✅    ║ S-101-A_TG Boden...dwg    ║ Polier   ║  =  ║ skip ║║
║  ║  ❓    ║ Zeichnung1.dwl            ║ ???      ║  ?  ║  ?   ║║
║  ╚════════╩════════════════════════════╩══════════╩═════╩══════╝║
║                                                                  ║
║  Zusammenfassung:                                                ║
║  🆕 Neu: 2 Revisionen (4 Dateien)                               ║
║  📈 Index-Update: 1 Revision (2 Dateien) → alte in _Archiv     ║
║  ✅ Unverändert: 1 Revision (2 Dateien) → übersprungen          ║
║  ❓ Unbekannt: 1 Datei                                          ║
║                                                                  ║
║  Rechtsklick → Plantyp ändern / Ordner ändern / Überspringen   ║
║                                                                  ║
║     [ Details ]    [ 📥 Importieren ]    [ Abbrechen ]          ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.9 Import — Abgeschlossen

```
╔══════════════════════════════════════════════════════════════════╗
║  Import abgeschlossen — 27.03.2026, 14:32                     ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Import-ID: imp_20260327_143200                                  ║
║                                                                  ║
║  • 2 Revisionen neu einsortiert (4 Dateien)                     ║
║  • 1 Revision aktualisiert — C→D (2 Dateien)                   ║
║    Alter Index C → _Archiv/ verschoben                          ║
║  • 1 Revision übersprungen — identisch (2 Dateien)             ║
║  • 1 Datei unbekannt — im _Eingang belassen                    ║
║                                                                  ║
║  [ Rückgängig ]  [ Einzelne korrigieren ]  [ Schließen ]       ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.10 Unbekannte Dateien

```
╔══════════════════════════════════════════════════════════════════╗
║  Unbekannte Datei: Zeichnung1.dwl                              ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Diese Datei konnte keinem Plantyp zugeordnet werden.           ║
║                                                                  ║
║  ○ Bestehendes Profil erweitern: [ Polierplan ▼ ]              ║
║  ○ Neues Profil erstellen (Wizard)                              ║
║  ○ Manuell verschieben nach: [ _________ ] [📁]                ║
║  ○ Überspringen (im Eingang belassen)                           ║
║                                                                  ║
║              [ Übernehmen ]        [ Abbrechen ]                ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.11 Plan korrigieren (Rechtsklick in Vorschau)

```
╔══════════════════════════════════════════════════════════════════╗
║  Plan korrigieren: S-103-D_TG Wämde.pdf                       ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Erkannter Plantyp: Polierplan      [ Ändern ▼ ]               ║
║  Erkannte Nummer:   103              [ Ändern   ]               ║
║  Erkannter Index:   D                [ Ändern   ]               ║
║  Erkanntes Geschoß: TG              [ Ändern   ]               ║
║  Zielordner:        Polierplan/TG/   [ 📁 Ändern]              ║
║                                                                  ║
║  ☐ Erkennungsmuster für diesen Typ verbessern                  ║
║                                                                  ║
║              [ Übernehmen ]        [ Abbrechen ]                ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.12 Erkennungs-Konflikt

```
╔══════════════════════════════════════════════════════════════════╗
║  Erkennungs-Konflikt: 21-2094_404_A_Wände.pdf                 ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Diese Datei passt zu mehreren Plantypen:                       ║
║                                                                  ║
║  ○ Schalungsplan  (Muster: "21-" prefix)                       ║
║  ○ Polierplan     (Muster: enthält "Wände")                    ║
║                                                                  ║
║  Bitte wähle den richtigen Typ.                                 ║
║  ☐ Muster des anderen Typs einschränken                        ║
║                                                                  ║
║              [ Übernehmen ]        [ Abbrechen ]                ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.13 Planliste importieren + Abgleich (V1.1)

```
╔══════════════════════════════════════════════════════════════════╗
║  Planliste importieren                                         ║
╠══════════════════════════════════════════════════════════════════╣
║  Datei: [ Planliste_Statik.xlsx         ] [Durchsuchen]        ║
║  Plantyp: [ Polierplan ▼ ]                                      ║
║                                                                  ║
║  Spalten zuweisen:                                              ║
║  Spalte A → [ Plan-Nummer ▼ ]  Spalte C → [ Plan-Index ▼ ]    ║
║  Spalte B → [ Bezeichnung ▼ ]  Spalte D → [ Datum      ▼ ]    ║
║  ☑ Zuordnung merken (im Profil speichern)                      ║
║                                                                  ║
║  ── Abgleich-Ergebnis ─────────────────────────────────────     ║
║  ╔════════╦═══════════════╦══════╦══════╦══════════════════╗    ║
║  ║ Status ║ Plannummer    ║ Soll ║ Ist  ║ Planinhalt       ║    ║
║  ╠════════╬═══════════════╬══════╬══════╬══════════════════╣    ║
║  ║  ✅    ║ P-010         ║  B   ║  B   ║ Grundriß Keller ║    ║
║  ║  ⚠️   ║ P-011         ║  C   ║  B   ║ Grundriß EG 64  ║    ║
║  ║  ❌    ║ P-029         ║  A   ║  —   ║ Ansichten H68   ║    ║
║  ║  ℹ️   ║ P-030         ║  —   ║  A   ║ Sonderteil       ║    ║
║  ╚════════╩═══════════════╩══════╩══════╩══════════════════╝    ║
║                                                                  ║
║  ✅ 20 Aktuell | ⚠️ 3 Veraltet | ❌ 4 Fehlend | ℹ️ 1 Extra    ║
║  [ Export Excel ]  [ Drucken ]  [ Schließen ]                   ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.14 Planliste exportieren (V1.1)

```
╔══════════════════════════════════════════════════════════════════╗
║  Planliste erstellen                                           ║
╠══════════════════════════════════════════════════════════════════╣
║  Plantypen:                  Spalten:                           ║
║  ☑ Polierplan       (28)    ☑ Plan-Nummer  ☑ Geschoß          ║
║  ☑ Bewehrungsplan   (22)    ☑ Bezeichnung  ☑ Haus             ║
║  ☐ Schalungsplan    (14)    ☑ Index        ☑ Plantyp          ║
║                              ☑ Datum        ☐ Dateipfad        ║
║                                                                  ║
║  Archiv: ○ Nein  ○ Separates Blatt  ○ Mit Markierung          ║
║  Sortierung: 1. Plantyp ↑↓  2. Geschoß ↑↓  3. Nr ↑↓          ║
║  Format: ○ Excel  ○ PDF                                        ║
║                                                                  ║
║      [ Vorschau ]    [ Exportieren ]    [ Abbrechen ]           ║
╚══════════════════════════════════════════════════════════════════╝
```

### 10.15 Schnellsuche (V1.1)

```
╔══════════════════════════════════════════════════════════════════╗
║  Plan suchen                                                   ║
╠══════════════════════════════════════════════════════════════════╣
║  🔍 [ P-013                                         ]           ║
║                                                                  ║
║  📄 202512 — P-013 — Grundriß 2.OG, Haus Nr. 64              ║
║     Index: B | Polierplan/2OG/ | [ Im Explorer ] [ Details ]   ║
║  📄 202512 — P-013 — Index A (archiviert)                      ║
║     Polierplan/2OG/_Archiv/ | [ Im Explorer ]                  ║
║  📄 202302 — P-013 — Grundriß EG                              ║
║     Index: C | Schalungsplan/EG/ | [ Im Explorer ]             ║
╚══════════════════════════════════════════════════════════════════╝
```

---

## 11. Zukünftige Module (Kurzübersicht — eigene Dokumente)

### 11.1 Dashboard

Startseite mit Widgets: Projekt-Übersicht, Wetter, neue Pläne im Eingang, Outlook-Status, Bautagebuch-Status. Wird nach V1 als eigenes Modul entwickelt. **Details: siehe `Docs/Module_Dashboard.md`**

### 11.2 Bautagebuch

Tägliches Bauprotokoll mit Auto-Befüllung aus: Registry (Projektdaten), Wetter-API, Stundenzettel (Excel), PlanManager (neue Pläne), OneDrive-Fotos. Export als Word (COM), Excel (ClosedXML), PDF (QuestPDF). **Details: siehe `Docs/Module_Bautagebuch.md`**

### 11.3 Outlook-Integration

COM Interop (`Microsoft.Office.Interop.Outlook`). Projekt-Ordner in Outlook erstellen, Anhänge extrahieren → `_Eingang`. VBA-Makros laufen parallel weiter. **Details: siehe `Docs/Module_Outlook.md`**

### 11.4 Wetter-Modul

API-Anbindung (OpenMeteo o.ä.) pro Baustelle. Aktuelle Daten + Vorhersage. Betonierfreigabe (Temperatur-Check). **Details: siehe `Docs/Module_Wetter.md`**

### 11.5 Foto-Modul

OneDrive-Baustellenfotos nach Projekt/Datum anzeigen. Thumbnail-Vorschau. **Details: siehe `Docs/Module_Foto.md`**

### 11.6 Vorlagen-System

Excel/Word Vorlagen mit Projektdaten befüllen (COM Interop). templates.json verwaltet das Verzeichnis. **Details: siehe `Docs/Module_Vorlagen.md`**

---

## 12. Solution-Struktur

```
BauProjektManager.sln
│
├── src/
│   ├── BauProjektManager.App/                ← EXE (Shell, DI, MainWindow)
│   │   ├── App.xaml + App.xaml.cs             ← DI Setup, Startup
│   │   ├── MainWindow.xaml                    ← Shell + Navigation
│   │   └── BauProjektManager.App.csproj
│   │
│   ├── BauProjektManager.Domain/             ← Modelle, Interfaces, Enums
│   │   ├── Models/                            ← Project, Plan, Building...
│   │   ├── Enums/                             ← PlanStatus, ProjectStatus...
│   │   ├── Interfaces/                        ← IRegistryService, IImportService...
│   │   └── BauProjektManager.Domain.csproj    ← KEINE Abhängigkeiten
│   │
│   ├── BauProjektManager.Infrastructure/     ← Technische Umsetzung
│   │   ├── Persistence/
│   │   │   ├── SqliteConnectionFactory.cs
│   │   │   ├── ProjectRepository.cs
│   │   │   ├── PlanCacheRepository.cs
│   │   │   ├── ImportJournalRepository.cs
│   │   │   ├── RegistryJsonExporter.cs        ← SQLite → JSON Export
│   │   │   └── RegistryJsonMapper.cs          ← Verschachtelt → Flach
│   │   ├── FileSystem/
│   │   │   ├── FileOperationService.cs
│   │   │   ├── Md5HashService.cs
│   │   │   ├── DirectoryScanner.cs
│   │   │   └── BackupService.cs
│   │   ├── Logging/
│   │   │   └── SerilogSetup.cs
│   │   └── BauProjektManager.Infrastructure.csproj
│   │
│   ├── BauProjektManager.PlanManager/        ← PlanManager Feature
│   │   ├── ViewModels/
│   │   │   ├── PlanManagerViewModel.cs
│   │   │   ├── ProjectDetailViewModel.cs
│   │   │   ├── SegmentAssignerViewModel.cs
│   │   │   ├── ImportPreviewViewModel.cs
│   │   │   └── PlanListViewModel.cs
│   │   ├── Views/
│   │   │   ├── PlanManagerView.xaml
│   │   │   ├── ProjectDetailView.xaml
│   │   │   ├── SegmentAssignerDialog.xaml
│   │   │   ├── ImportPreviewDialog.xaml
│   │   │   └── PlanListDialog.xaml
│   │   ├── Services/
│   │   │   ├── FileParserService.cs
│   │   │   ├── PlanCompareService.cs
│   │   │   ├── ImportService.cs               ← 10-Schritte Workflow
│   │   │   ├── PlanTypeRecognitionService.cs
│   │   │   └── PatternTemplateService.cs      ← Vorschlagslogik
│   │   ├── Converters/
│   │   │   ├── StatusToColorConverter.cs
│   │   │   └── BoolToVisibilityConverter.cs
│   │   ├── PlanManagerModule.cs               ← Modul-Registrierung
│   │   └── BauProjektManager.PlanManager.csproj
│   │
│   └── BauProjektManager.Settings/           ← Einstellungen Feature
│       ├── ViewModels/
│       │   ├── SettingsViewModel.cs
│       │   └── ProjectEditViewModel.cs
│       ├── Views/
│       │   ├── SettingsView.xaml
│       │   └── ProjectEditDialog.xaml
│       ├── SettingsModule.cs
│       └── BauProjektManager.Settings.csproj
│
├── tests/
│   ├── BauProjektManager.Domain.Tests/
│   ├── BauProjektManager.Infrastructure.Tests/
│   └── BauProjektManager.PlanManager.Tests/
│       ├── FileParserServiceTests.cs
│       ├── PlanCompareServiceTests.cs
│       └── ImportServiceTests.cs
│
└── docs/
    ├── BauProjektManager_Architektur.md       ← DIESES DOKUMENT
    ├── Architekturentscheidungen_v1.4.md
    ├── CODING_STANDARDS.md
    └── Module_*.md                            ← Eigene Docs pro Modul
```

**Dependency-Regel (eisern):**
```
Domain          → NICHTS
Infrastructure  → nur Domain
PlanManager     → Domain + Infrastructure
Settings        → Domain + Infrastructure
App             → alles (DI verdrahtet hier)
```

---

## 13. Technische Entscheidungen (komplett)

| Thema | Entscheidung |
|-------|-------------|
| **Sprache** | C# (.NET 8 LTS) |
| **GUI** | WPF (XAML) |
| **Architektur** | Modularer Monolith (feste Registrierung, kein Plugin) |
| **MVVM** | CommunityToolkit.Mvvm |
| **DI** | Microsoft.Extensions.DependencyInjection |
| **Logging** | Serilog (File + Console, Structured Logging) |
| **System of Record** | SQLite |
| **VBA-Export** | registry.json (automatisch generiert, read-only) |
| **Konfiguration** | JSON-Dateien auf OneDrive (settings, profiles, templates) |
| **Operativer State** | Lokal (%LocalAppData%) — SQLite, Logs, Cache |
| **Excel (neu)** | ClosedXML (kein Excel nötig) |
| **Excel (bestehend)** | COM Interop (Excel nötig) — nach V1 |
| **Outlook** | COM Interop (Outlook nötig) — nach V1 |
| **Word** | COM Interop (Word nötig) — nach V1 |
| **PDF-Export** | QuestPDF |
| **PDF-Parsing** | PdfPig |
| **Testing** | xUnit |
| **Git** | GitHub (herbertschrotter-blip/BauProjektManager) |
| **Dateivergleich** | MD5-Hash |
| **Plan-Dateien** | 1..n Dateien pro Revision (kein festes PDF/DWG-Paar) |
| **Import** | 10-Schritte-Workflow mit SQLite-Journal |
| **Undo** | 3 SQLite-Tabellen (Journal, Actions, ActionFiles) |
| **Deployment** | Single-file .exe (self-contained) |
| **Multi-Device** | OneDrive für Nutzdaten+Config, lokal für State |
| **Nullable** | Aktiviert in allen Projekten |
| **Single-Writer** | Mutex beim App-Start |
| **Backup** | Vor jedem Import (SQLite + JSON als .bak) |
| **Atomische Writes** | Write-to-temp-then-rename für JSON |
| **Schema-Version** | In jeder DB und JSON |
| **Profil-System** | RecognitionProfiles (verbindlich) + PatternTemplates (Vorschläge) |

---

## 14. V1-Scope & Roadmap

### V1 PFLICHT
- [ ] App-Shell + Navigation
- [ ] Einstellungen (Projekte anlegen/laden/bearbeiten)
- [ ] SQLite Setup + Haupt-DB
- [ ] Automatischer registry.json Export
- [ ] Serilog Logging (File + Console)
- [ ] Dateinamen-Parser + Segment-Zuweiser GUI (3-Schritt-Wizard)
- [ ] Plantyp-Erkennung (Muster)
- [ ] PatternTemplates (Vorschlagslogik beim Profil-Anlegen)
- [ ] Import-Workflow (alle 10 Schritte)
- [ ] Index-Archivierung
- [ ] Undo (Journal-basiert in SQLite)
- [ ] Recovery bei Abbruch (beim App-Start)
- [ ] Backup vor Import
- [ ] Single-Writer Mutex

### V1.1
- [ ] Planlisten-Import (Excel) + Soll/Ist-Abgleich
- [ ] Planlisten-Export (Excel + PDF)
- [ ] Schnellsuche (Plan finden über alle Projekte)
- [ ] CSV-Import für Planlisten

### Nach V1
- [ ] Dashboard mit Widgets (Phase 3)
- [ ] Bautagebuch Eingabe + Export (Phase 3-4)
- [ ] Profil-Lernen (Muster automatisch erweitern bei Korrektur)
- [ ] PDF-Planlisten-Import (Phase 3)
- [ ] Outlook COM (Phase 4+)
- [ ] Wetter-API (Phase 4+)
- [ ] Foto-Modul (Phase 4+)
- [ ] Excel/Word COM Vorlagen (Phase 4+)
- [ ] PDF-Vorschau (Phase 4+)
- [ ] VERALTET-Stempel (Phase 4+)
- [ ] Auto-Update (weit nach V1)

---

## 15. Coding Standards + Definition of Done

### V1-Pflicht Standards

| Standard | Umsetzung |
|----------|-----------|
| Nullable Reference Types | `<Nullable>enable</Nullable>` in allen .csproj |
| `.editorconfig` | Einmal erstellen, erzwingt einheitliche Formatierung |
| Logging | Serilog, Structured Logging, `{PropertyName}` Platzhalter |
| CancellationToken | Alle async Methoden bekommen CancellationToken |
| Verbot `async void` | Nur erlaubt für UI Event-Handler |
| IDisposable | `using`-Statement Pflicht |
| Single-Writer | Mutex beim App-Start — nur eine Instanz läuft |
| Schema-Version | `"schemaVersion": "1.0"` in jeder DB und JSON |
| Backup vor Import | SQLite-DB + JSON als .bak kopieren |
| Atomische JSON-Writes | Write-to-temp-then-rename |

### Definition of Done (V1)

```
Bevor ein Feature als "fertig" gilt:
☐ Code kompiliert ohne Fehler und Warnungen
☐ Manuelle Tests durchgeführt (Happy Path + ein Fehlerfall)
☐ Logging vorhanden (Info für Hauptaktionen, Error für Fehler)
☐ Nullable Warnings aufgelöst
☐ Kein offenes TODO oder HACK (oder bewusst dokumentiert warum)
☐ Git Commit mit korrektem Format ([vX.Y.Z] Modul, Typ: Titel)
```

Detaillierte Coding Standards: siehe `CODING_STANDARDS.md`

---

## 16. Alle Config-Dateien — Übersicht

| Datei | Ort | Format | Zweck | Erstellt von |
|-------|-----|--------|-------|-------------|
| `registry.json` | OneDrive `.AppData/` | JSON | VBA-Export (generiert) | C#-App (auto) |
| `settings.json` | OneDrive `.AppData/` | JSON | App-Einstellungen | C#-App |
| `pattern-templates.json` | OneDrive `.AppData/` | JSON | Musterbibliothek | C#-App |
| `templates.json` | OneDrive `.AppData/` | JSON | Vorlagen-Verzeichnis | C#-App |
| `profiles.json` | OneDrive `.AppData/Projects/<P>/` | JSON | Plantyp-Profile | C#-App |
| `bpm.db` | Lokal | SQLite | Haupt-DB (Projekte) | C#-App |
| `planmanager.db` | Lokal `Projects/<P>/` | SQLite | Cache, Journal, Undo | C#-App |
| `.bpm-manifest` | OneDrive Projektordner | JSON | Zeiger auf Registry | C#-App |

---

## 17. Offene Punkte

- [ ] Domänenmodell im Detail (alle Klassen, Felder, Beziehungen)
- [ ] Import-Workflow Detail (Input/Output pro Schritt mit Datentypen)
- [ ] SQLite-Tabellen komplett (Plan-Cache, Projekt-Tabellen)
- [ ] OneDrive-Strategie Detail (Offline, Rebuild, Konflikte)
- [ ] Coding Standards Dokument aktualisieren (Nullable, .editorconfig)
- [ ] `.editorconfig` erstellen
- [ ] Modul-Dokumente (Dashboard, Bautagebuch, Outlook, Wetter, Foto)
- [ ] Phase 0 starten: C#-Solution aufsetzen

---

*Dokument Version 1.4.0 — 27.03.2026*

*Basis: v1.3 + 2 Review-Runden (ChatGPT) + Gegen-Review (Claude) + 13 verbindliche Entscheidungen*

*Kernänderungen gegenüber v1.3:*
- *Modularer Monolith statt Plugin-System (kein IBpmModule)*
- *SQLite als System of Record, registry.json nur als generierter VBA-Export*
- *Internes Modell sauber verschachtelt, VBA bekommt flachen Export*
- *OneDrive nur für Nutzdaten + Config, operativer State lokal*
- *.NET 8 LTS statt .NET 9*
- *Plan-Dateien: 1..n pro Revision statt festes PDF/DWG-Paar*
- *10-Schritte Import-Workflow (inkl. Validate, Plan, Finalize, Recover)*
- *Undo-Journal mit 3 SQLite-Tabellen (1..n Dateien pro Aktion)*
- *PatternTemplates getrennt von RecognitionProfiles*
- *V1-Scope radikal reduziert (nur PlanManager + Shell + Einstellungen)*
- *Coding Standards ergänzt + Mini Definition of Done*
- *Module (Dashboard, Bautagebuch etc.) nur angerissen → eigene Dokumente*