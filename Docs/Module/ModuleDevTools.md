---
doc_id: module-devtools
doc_type: module
authority: source_of_truth
status: active
owner: herbert
topics: [devtools, debug, diagnose, reset, log-ansicht, bug-report]
read_when: [devtools-feature, debug-dialog, app-reset, log-viewer]
related_docs: [architektur, coding-standards]
related_code: [src/BauProjektManager.App/DevToolsDialog.xaml, src/BauProjektManager.Infrastructure/Dev/DeveloperToolsService.cs, src/BauProjektManager.Domain/Interfaces/IDeveloperToolsService.cs]
supersedes: []
---

## AI-Quickload
- Zweck: Modul-Dokumentation für DevTools — Diagnose, Log-Ansicht, Reset, Bug-Report (nur DEBUG)
- Autorität: source_of_truth
- Lesen wenn: DevTools-Feature, Debug-Dialog, App-Reset, Log-Viewer
- Nicht zuständig für: Produktiv-Features, Release-Builds
- Pflichtlesen: keine
- Fachliche Invarianten:
  - Nur in DEBUG-Builds sichtbar — kein Zugriff in Release
  - Kein DI-Container — manuell instanziiert (Migration geplant)
  - Reset löscht unwiderruflich — kein Backup

---

﻿# DevTools-Modul — Dokumentation

**Letzte Änderung:** v0.19.0  
**Status:** Implementiert (DEBUG-only)  
**ADR:** ADR-043

---

## 1. Übersicht

Das DevTools-Modul stellt Entwicklerwerkzeuge für Diagnose, Debugging und Reset der BPM-App bereit. Es ist ausschließlich in Debug-Builds sichtbar und wird über einen Button in der Sidebar aufgerufen.

**Zuständigkeit:** System-Diagnose, Log-Ansicht, App-Reset, Bug-Report-Export

---

## 2. Architektur

### Schichtenverteilung

| Schicht | Datei | Verantwortung |
|---------|-------|---------------|
| Domain | `Interfaces/IDeveloperToolsService.cs` | Interface-Definition |
| Infrastructure | `Dev/DeveloperToolsService.cs` | Implementation: Log-Zugriff, System-Info, P/Invoke, Reset-Logik |
| App | `DevToolsDialog.xaml` + `.xaml.cs` | 3-Tab-Dialog (System-Info, Log, Reset) |
| App | `MainWindow.xaml.cs` | Einstiegspunkt, öffnet Dialog |
| App | `App.xaml.cs` | Instanziierung (manuell, kein DI) |

### Abhängigkeiten

```
App (DevToolsDialog) → Domain (IDeveloperToolsService) → Infrastructure (DeveloperToolsService)
```

Keine zusätzlichen NuGet-Pakete. Verwendet `System.Runtime.InteropServices` für Win32 P/Invoke (Monitor-Erkennung).

---

## 3. Zugriff und Sichtbarkeit

- **Nur in DEBUG-Builds** sichtbar (`#if DEBUG` in App.xaml.cs und MainWindow.xaml.cs)
- Kein DI-Container — manuell instanziiert in `App.xaml.cs` (Migration auf DI geplant nach PlanManager)
- Sidebar-Button "Dev Tools" öffnet modalen Dialog
- Dialog bekommt `IDeveloperToolsService` via Constructor Injection
- In Release-Builds existiert weder der Button noch die Instanziierung

---

## 4. Interface: IDeveloperToolsService

### Properties

| Property | Typ | Beschreibung |
|----------|-----|-------------|
| `DatabasePath` | `string` | Pfad zur bpm.db |
| `LogDirectory` | `string` | Pfad zum Log-Verzeichnis |
| `SettingsPath` | `string` | Pfad zur settings.json |

### Methoden

| Methode | Rückgabe | Beschreibung |
|---------|----------|-------------|
| `ReadLogTail(int lineCount)` | `string` | Letzte N Zeilen der aktuellen Log-Datei (Standard: 200) |
| `GetSystemInfo()` | `string` | App-Version, .NET Runtime, OS, Rechner, DB-Info, Freier Speicher |
| `GetDisplayInfo()` | `string` | Alle Monitore mit physischer Auflösung, Hz, DPI-Skalierung |
| `OpenLogDirectory()` | `void` | Öffnet Log-Ordner im Windows Explorer |
| `RequestFullReset(Action)` | `void` | Löscht DB + Settings, startet App neu |
| `RequestDatabaseReset(Action)` | `void` | Löscht nur DB, startet App neu |
| `RequestSettingsReset(Action)` | `void` | Löscht nur Settings, startet App neu |
| `RequestFirstRunReset(Action)` | `void` | Setzt isFirstRun=true in settings.json, startet App neu |

---

## 5. Dialog: 3 Tabs

### Tab 1 — System-Info

Zeigt in einem 2-Spalten-Grid (Label + Wert) mit Zebra-Striping (`BpmBgSurface`):

| Feld | Quelle | Anmerkung |
|------|--------|-----------|
| App-Version | `Assembly.GetEntryAssembly().Version` | Aus Directory.Build.props |
| .NET Runtime | `RuntimeInformation.FrameworkDescription` | z.B. ".NET 10.0.5" |
| Windows | `Environment.OSVersion` | z.B. "NT 10.0.26200.0" |
| Rechner | `Environment.MachineName` | Computername |
| Benutzer | `Environment.UserName` | DSGVO: OK für Dev-Tools, nie in Logs |
| Monitore | `EnumDisplayMonitors` (P/Invoke) | Anzahl erkannter Monitore |
| Auflösung | `EnumDisplaySettings` + `GetDpiForMonitor` | Pro Monitor: Auflösung, Hz, DPI% |
| Datenbank | `DatabasePath` Property | Pfad + Dateigröße in KB |
| Freier Speicher | `DriveInfo.AvailableFreeSpace` | Laufwerk der DB |
| Einstellungen | `SettingsPath` Property | Pfad zur settings.json |
| Log-Verzeichnis | `LogDirectory` Property | Pfad zum Log-Ordner |

Pfad-Felder verwenden `BpmAccentPrimary` Foreground und `TextWrapping="Wrap"`.

### Tab 2 — Log

- Zeigt die letzten 200 Zeilen der aktuellen Log-Datei (`BPM_*.log`)
- Monospace-Font (Consolas, 12px)
- Auto-Scroll zum Ende beim Laden
- Liest mit `FileShare.ReadWrite` (sicher bei laufendem Serilog)
- **Buttons:** "Aktualisieren" (neu laden), "Log kopieren" (nur Log in Clipboard), "Log-Ordner öffnen" (Explorer)

### Tab 3 — Reset

4 Reset-Optionen als Radio-Button-Auswahl mit Bestätigungsdialog:

| Option | Was passiert | Risiko |
|--------|-------------|--------|
| Ersteinrichtung wiederholen | Setzt `isFirstRun: true` in settings.json | Niedrig — Setup-Dialog erscheint beim nächsten Start |
| Nur Datenbank zurücksetzen | Löscht bpm.db | Mittel — alle Projekte weg |
| Nur Einstellungen zurücksetzen | Löscht settings.json | Niedrig — Pfade und Ordner-Template weg |
| Komplett-Reset | Löscht bpm.db + settings.json | Hoch — alles weg |

Jede Option zeigt eine Bestätigungs-MessageBox vor Ausführung.

---

## 6. Reset-Mechanismus (ADR-043)

Der Reset funktioniert über ein **selbstlöschendes Batch-Script**:

1. App generiert ein `.bat`-Script mit **GUID-basiertem Dateinamen** im `%TEMP%`-Verzeichnis
2. Script verwendet **CP850-Encoding** (Windows-Konsole Kompatibilität)
3. Script-Ablauf:
   - Wartet bis der BPM-Prozess beendet ist (`taskkill` / Prozess-Check)
   - Löscht die konfigurierten Dateien (DB und/oder Settings)
   - Startet die App neu
   - Löscht sich selbst
4. Bei Fehler wird eine **Failure-File** in `%TEMP%` angelegt zur Diagnose
5. Die App ruft `Process.Start()` auf das Script und beendet sich dann selbst via `shutdownAction`

### Warum Batch-Script statt direktem Löschen?

- Die App kann ihre eigene DB nicht löschen während sie läuft (File Lock)
- Das Script wartet bis der Prozess komplett beendet ist
- Dann löscht es die Dateien und startet die App neu — nahtloser Neustart

---

## 7. Bug-Report (Clipboard-Export)

Button "System-Info + Log kopieren" exportiert vollständige Diagnosedaten in die Zwischenablage:

```
BauProjektManager Bug-Report
============================
App-Version:       0.19.2
.NET Runtime:      .NET 10.0.5
Windows:           Microsoft Windows NT 10.0.26200.0
Rechner:           FIRMENLAPTOP
Benutzer:          herbe
DB-Pfad:           C:\Users\herbe\AppData\Local\BauProjektManager\bpm.db
DB-Größe:          4.0 KB
Freier Speicher:   234.0 GB

Monitore:          2
Primär:            1920 × 1080 px, 60 Hz, 150% (144 DPI)
Monitor 2:         1920 × 1080 px, 60 Hz, 100% (96 DPI)

Einstellungen:     C:\Users\herbe\AppData\Local\BauProjektManager\settings.json
Log-Verzeichnis:   C:\Users\herbe\AppData\Local\BauProjektManager\Logs

--- LOG ---
[Letzte 200 Log-Zeilen]
```

---

## 8. P/Invoke (Win32 API)

Für die Multi-Monitor-Erkennung werden folgende Win32-Funktionen verwendet:

| Funktion | DLL | Zweck |
|----------|-----|-------|
| `EnumDisplayMonitors` | user32.dll | Alle Monitore auflisten |
| `GetMonitorInfo` | user32.dll | Name, Bounds, Primary-Flag pro Monitor |
| `EnumDisplaySettings` | user32.dll | Physische Auflösung + Refresh-Rate |
| `GetDpiForMonitor` | shcore.dll | DPI-Skalierung pro Monitor |

### Structs

| Struct | Layout | Anmerkung |
|--------|--------|-----------|
| `RECT` | Sequential | Left, Top, Right, Bottom |
| `MONITORINFOEX` | Sequential, CharSet.Auto | 32-Char DeviceName, Flags (Bit 0 = Primary) |
| `DEVMODE` | **Explicit** | Feste FieldOffsets wegen Union-Feldern, `dmSize = 220` |

`DEVMODE` verwendet `LayoutKind.Explicit` statt `Sequential` weil die Win32-Struktur Union-Felder enthält die das sequentielle Layout durcheinanderbringen. Die Offsets für `dmPelsWidth` (108), `dmPelsHeight` (112) und `dmDisplayFrequency` (120) sind fest definiert.

---

## 9. Logging im Modul

Das Modul verwendet Serilog mit zwei Log-Levels:

- `Log.Warning()` — bei Fehlern in ReadLogTail und Reset-Operationen
- `Log.Debug()` — in App.xaml.cs bei Service-Registrierung

Keine `Log.Information()` im Modul selbst (das DevTools-Modul ist ein Consumer des Logs, kein Producer).

---

## 10. Bekannte Einschränkungen

- **Kein DI-Container** — manuell instanziiert, Migration auf DI geplant nach PlanManager
- **Nur in DEBUG sichtbar** — kein Zugriff in Release-Builds (bewusste Entscheidung)
- **Reset ist unwiderruflich** — kein Backup vor dem Löschen
- **Log-Ansicht** zeigt nur die aktuelle Tagesdatei (Serilog rollt täglich)
- **`Environment.UserName`** wird in System-Info angezeigt — DSGVO-konform weil nur lokal im Dev-Dialog, nie geloggt oder exportiert
- **Monitor-Erkennung** abhängig von Windows P/Invoke — keine Fallback-Logik bei Fehlern (fällt auf 96 DPI zurück)

---

## 11. Geplante Verbesserungen (genehmigtes v2-Konzept)

Das v2-Konzept wurde als Mockup genehmigt und ist im Backlog:

| Verbesserung | Beschreibung | Status |
|-------------|-------------|--------|
| Farbcodierte Sicherheits-Badges | Grün/Gelb/Rot bei den 4 Reset-Optionen je nach Risiko | Genehmigt, nicht implementiert |
| Bug-Report als Datei | Export als .txt Datei zusätzlich zum Clipboard | Genehmigt, nicht implementiert |
| Crash-Dialog | Unhandled Exception → automatischer Dialog mit System-Info + Stacktrace | Genehmigt, nicht implementiert |
| Migration auf DI-Container | `IDeveloperToolsService` über DI statt manuellem `new` | Geplant nach PlanManager |
| Release-Build Zugriff | Verstecktes Tastenkürzel oder Menü in Release | Idee, nicht entschieden |

---

## 12. Dateien

```
src/
├── BauProjektManager.Domain/
│   └── Interfaces/
│       └── IDeveloperToolsService.cs      (19 Zeilen)
├── BauProjektManager.Infrastructure/
│   └── Dev/
│       └── DeveloperToolsService.cs       (216 Zeilen)
└── BauProjektManager.App/
    ├── DevToolsDialog.xaml                 (318 Zeilen)
    ├── DevToolsDialog.xaml.cs              (182 Zeilen)
    ├── MainWindow.xaml                     (Button, #if DEBUG)
    ├── MainWindow.xaml.cs                  (OnOpenDevTools, #if DEBUG)
    └── App.xaml.cs                         (Instanziierung, #if DEBUG)
```

---

## 13. Verwandte Entscheidungen

| ADR | Titel | Relevanz |
|-----|-------|----------|
| ADR-043 | Dev-Tools Architektur | Definiert Reset-Mechanismus, GUID-Batch-Script, 3-Tab-Struktur |
| ADR-023 | GitHub Read-Only für Claude | Claude darf nie pushen — auch nicht über DevTools |
| ADR-035 | IExternalCommunicationService | DevTools hat keine externen Calls, aber Bug-Report Export könnte zukünftig darüber laufen |

---

## 14. Änderungshistorie

| App-Version | Änderung |
|-------------|----------|
| v0.17.0 | Erste Implementation: IDeveloperToolsService, DeveloperToolsService, DevToolsDialog mit 3 Tabs, 4 Reset-Optionen, GUID-Batch-Script Reset |
| v0.18.0 | PerMonitorV2 DPI-Awareness, Multi-Monitor-Erkennung mit P/Invoke, Debug/Verbose Logging, Bug-Report erweitert um Display-Info und Pfade |
| v0.19.0 | Labels korrigiert (Monitore/Auflösung statt Auflösung/DPI-Skalierung) |
