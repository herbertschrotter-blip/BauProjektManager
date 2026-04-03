# BauProjektManager — Architecture Decision Records (ADR)

**Erstellt:** 29.03.2026  
**Aktualisiert:** 30.03.2026  
**Version:** 1.1  
**Kontext:** Alle Entscheidungen aus Architektur-Sessions, Review-Runden (ChatGPT + Claude), und Implementierungs-Chats.

---

## Inhaltsverzeichnis

| ADR | Titel | Status | Datum |
|-----|-------|--------|-------|
| 001 | Modularer Monolith statt Plugin-System | ✅ Entschieden | 2026-03 |
| 002 | SQLite als System of Record | ✅ Entschieden | 2026-03 |
| 003 | Internes Domänenmodell vs. flacher VBA-Export | ✅ Entschieden | 2026-03 |
| 004 | Dreistufige Cloud-Sync-Strategie | ✅ Entschieden | 2026-03 |
| 005 | .NET Version: 8 → 10 LTS | ✅ Entschieden (geändert) | 2026-03 |
| 006 | Solution-Struktur: 5 Projekte | ✅ Entschieden | 2026-03 |
| 007 | Plan-Dateien: 1..n pro Revision | ✅ Entschieden | 2026-03 |
| 008 | 10-Schritte Import-Workflow | ✅ Entschieden | 2026-03 |
| 009 | Undo-Journal in SQLite | ✅ Entschieden | 2026-03 |
| 010 | Profil- und Template-System getrennt | ✅ Entschieden | 2026-03 |
| 011 | Ordnernamen: Nummern mit Leerzeichen | ✅ Entschieden | 2026-03 |
| 012 | Nummern-Präfix aus Listenposition | ✅ Entschieden | 2026-03 |
| 013 | .bpm-manifest als Projektordner-Ausweis | ✅ Entschieden | 2026-03 |
| 014 | C# + WPF statt PowerShell | ✅ Entschieden | 2026-03 |
| 015 | CommunityToolkit.Mvvm + Serilog | ✅ Entschieden | 2026-03 |
| 016 | Coding Standards + Definition of Done | ✅ Entschieden | 2026-03 |
| 017 | VBA liest nur, schreibt nie | ✅ Entschieden | 2026-03 |
| 018 | Arbeitszeiterfassung: WPF + ClosedXML → Excel | ✅ Entschieden | 2026-03 |
| 019 | Mobile PWA statt Native App | 🟡 Konzept, aufgeschoben | 2026-03 |
| 020 | Write-Lock mit Heartbeat für Multi-Device-Sync | 🟡 Konzept, aufgeschoben | 2026-03 |
| 021 | Client/Firma als eigene Entität (Vorbereitung) | 🟡 Konzept | 2026-03 |
| 022 | Segment-basiertes Dateinamen-Parsing | ✅ Entschieden | 2026-03 |
| 023 | Claude schreibt Code, Herbert committet | ✅ Entschieden | 2026-03 |
| 024 | Adressbuch getrennt von Projekt-Beteiligten | ✅ Entschieden | 2026-03 |
| 025 | Status vereinfacht: Active + Completed | ✅ Entschieden | 2026-03 |
| 026 | Portal-Typen als editierbare Liste | ✅ Entschieden | 2026-03 |
| 027 | KI-API-Import für Datenextraktion | 🟡 Konzept | 2026-03 |
| 028 | Theme-System mit Resource Dictionaries | ✅ Entschieden | 2026-03 |
| 029 | Arbeitspaket als zentrales Verbindungskonzept | ✅ Entschieden | 2026-03 |
| 030 | Abschluss-Erfassung statt Tages-Aufmaß | ✅ Entschieden | 2026-03 |
| 031 | DB-SCHEMA.md als zentrales Leitdokument | ✅ Entschieden | 2026-03 |
| 032 | ITaskManagementService — nicht an ClickUp gebunden | 🟡 Konzept | 2026-03 |
| 033 | Multi-User: 3 Modi (eigene DB, geteilte DB, Server) | 🟡 Konzept | 2026-03 |
| 034 | Modul-Aktivierung + Offline-Lizenzierung | 🟡 Konzept | 2026-03 |
| 035 | IExternalCommunicationService — zentrales Privacy Gate | ✅ Entschieden | 2026-04 |

---

## ADR-001: Modularer Monolith statt Plugin-System

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Architektur-Session, ChatGPT-Review Punkt 1, Claude Gegen-Review

**Kontext:**

Beim Architekturentwurf stand die Frage: Soll BPM ein Plugin-System mit dynamischem Laden (IBpmModule Interface, MEF/Reflection) verwenden, oder Module als fest registrierte C#-Projekte (DLLs) direkt im DI-Container verdrahten? Die erste Architektur-Version (v1.3) enthielt ein `IBpmModule`-Interface. Die externe ChatGPT-Review kritisierte, dass ein echtes Plugin-System für V1 zu früh sei.

**Entscheidung:**

Modularer Monolith mit fester Registrierung. Module sind separate C#-Projekte (eigene DLLs), werden aber direkt als konkrete Typen im DI-Container registriert. Kein `IBpmModule`, kein dynamisches Laden, keine Reflection.

**Alternativen:**

- *Plugin-System (MEF/Reflection):* Flexibler, Module könnten nachgeladen werden. Aber: Deutlich komplexer, Debugging schwieriger, für einen Solo-Entwickler Overkill.
- *Alles in einem Projekt:* Einfacher, aber keine Trennung. Wird bei wachsender Codebasis unübersichtlich.
- *Prism-Framework:* Wurde explizit als Overkill abgelehnt.

**Konsequenzen:**

- Neue Module erfordern Änderung in App.xaml.cs (DI-Registrierung) und MainWindow.xaml (Navigation) — ca. 3 Zeilen XAML pro Modul
- Klare Projekt-Grenzen erzwingen saubere Abhängigkeiten
- Einfaches Debugging, kein Reflection-Magic
- Kann später auf Interface-basiert umgestellt werden (kleine Änderung)
- Gut genug für Solo-Projekt mit ≤10 Modulen

---

## ADR-002: SQLite als System of Record

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Architektur-Session, ChatGPT-Review Punkt 2, Claude Gegen-Review

**Kontext:**

Die ursprüngliche Architektur (v1.3) verwendete JSON als primäre Datenquelle für alles. Die externe Review kritisierte, dass JSON für operative Daten (Import-History, Undo, Cache) zu schwach sei — keine Transaktionen, kein Locking, kein Schema.

**Entscheidung:**

SQLite ist die einzige Wahrheitsquelle für alle operativen Daten. JSON dient nur als generierter Export für VBA-Interop (`registry.json`) und als selten geänderte Konfiguration (`settings.json`, `profiles.json`). Wenn JSON korrupt wird, kann es aus SQLite neu generiert werden.

**Alternativen:**

- *Alles in JSON:* Einfacher, aber nicht transaktionssicher. Undo-Journal und Import-History in JSON sind fragil.
- *Alles in SQLite, kein JSON:* Technisch sauberer, aber VBA kann kein SQLite lesen. Herbert braucht VBA-Kompatibilität für bestehende Outlook/Excel-Makros.

**Konsequenzen:**

- `bpm.db` (lokal) = Haupt-Datenbank für Projekte, Stammdaten
- `planmanager.db` (lokal, pro Projekt) = Cache, Journal, Undo
- `registry.json` wird bei jeder Projektänderung automatisch aus SQLite generiert
- VBA liest nur den generierten Export, schreibt nie
- Zwei SQLite-DBs statt einer, um Projekt-spezifische Daten zu trennen

---

## ADR-003: Internes Domänenmodell vs. flacher VBA-Export

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Architektur-Session, ChatGPT-Review Punkt 3

**Kontext:**

VBA JSON-Parser sind einfach — sie können keine verschachtelten Objekte gut verarbeiten. Die Frage war: Soll das interne C#-Modell flach sein (wie VBA es braucht), oder sauber verschachtelt (wie es fachlich korrekt ist)?

**Entscheidung:**

Das interne C#-Modell ist sauber verschachtelt und stark typisiert (`Project.Location.Street`, `Project.Buildings[].Levels[]`). Für VBA wird über einen Mapping-Layer (`RegistryJsonMapper`) automatisch ein flacher JSON-Export generiert. VBA diktiert nicht die interne Struktur.

**Alternativen:**

- *Flaches internes Modell:* VBA-kompatibel, aber fachlich verkrüppelt. Keine verschachtelten Objekte möglich.
- *VBA-Parser verbessern:* Aufwändig, fragil, nicht wartbar.

**Konsequenzen:**

- `RegistryJsonMapper.cs` muss gepflegt werden wenn sich das Modell ändert
- Buildings werden als Pipe-String serialisiert: `"H64:Haus Nr. 64:Reihenhaus:KG,EG,1.OG|H66:..."`
- Koordinaten als separate Felder: `coordinateEast`, `coordinateNorth` statt verschachteltem Objekt
- Pfade relativ zu `rootPath` — VBA baut zusammen mit `rootPath & "\" & plansPath`

---

## ADR-004: Dreistufige Cloud-Sync-Strategie

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Architektur-Session, ChatGPT-Review Punkt 4

**Kontext:**

Herbert arbeitet auf zwei Geräten (PC zuhause, Laptop auf Baustelle), synchronisiert über Cloud-Speicher. Die externe Review warnte, dass Cloud-Sync als State-Store riskant sei (Sync-Konflikte, File-Locking, halbfertige Schreibvorgänge). Herbert bestätigte, dass er auf beiden Geräten Pläne sortiert — Profile müssen also synchronisieren.

**Entscheidung:**

Dreistufige Trennung:

| Kategorie | Speicherort | Inhalt | Synct? |
|-----------|-------------|--------|--------|
| Nutzdaten | Cloud-Speicher (Projektordner) | Pläne, Fotos, Dokumente | Ja |
| Konfiguration | Cloud-Speicher (`.AppData/`) | registry.json, settings.json, profiles.json | Ja |
| Operativer State | Lokal (`%LocalAppData%`) | SQLite-DBs, Logs, Cache, Undo | Nein |

BPM funktioniert mit jedem Cloud-Speicher der sich als Ordner im Explorer einblendet: OneDrive, Google Drive, Dropbox, Synology Drive, Nextcloud etc. BPM ist **nicht** an OneDrive gebunden.

**Alternativen:**

- *Alles auf Cloud-Speicher:* SQLite + Cloud-Sync = Sync-Konflikte. File-Locking-Probleme.
- *Alles lokal:* Dann keine Synchronisation zwischen Geräten.
- *Cloud-DB (z.B. Azure):* Herbert will keine Cloud-Services oder Abos.

**Konsequenzen:**

- Import-History und Undo-Journal sind geräte-spezifisch (akzeptabel — ein Import läuft auf einem Gerät)
- Auf dem zweiten Gerät wird der SQLite-Cache beim ersten Scan aus dem Dateisystem neu aufgebaut
- Atomische JSON-Writes (write-to-temp-then-rename) verhindern halbfertige Dateien
- `.AppData/` Ordner ist Hidden+System, für User unsichtbar

---

## ADR-005: .NET Version — von .NET 9 über .NET 8 zu .NET 10

**Datum:** 2026-03 (mehrfach geändert)  
**Status:** ✅ Entschieden (.NET 10 LTS)  
**Herkunft:** Architektur-Session, ChatGPT-Review Punkt 5, Implementierung

**Kontext:**

Die ursprüngliche Architektur sah .NET 9 vor. Die externe Review empfahl LTS. Claude empfahl .NET 8 LTS (stabiler, beste KI-Trainingsdaten). Herbert wählte zunächst .NET 8. Während der Implementierung wurde auf .NET 10 LTS gewechselt (Released März 2026, Support bis November 2028).

**Entscheidung:**

.NET 10 LTS. Der Wechsel von .NET 8 war eine Zeile in der .csproj (`<TargetFramework>net10.0-windows</TargetFramework>`). WPF funktioniert identisch.

**Alternativen:**

- *.NET 8 LTS:* Support endet November 2026 — zu kurz für ein Projekt das über Jahre laufen soll.
- *.NET 9 STS:* Support endet Mai 2026 — nicht tragbar.
- *.NET 10 LTS:* Frisch aber LTS. Support bis November 2028. Gewählt.

**Konsequenzen:**

- Längerer Support-Zeitraum (bis Nov 2028)
- Neuere C#-Features verfügbar
- Libraries/NuGet-Pakete müssen .NET 10 unterstützen (inzwischen der Fall)
- Claude hat etwas weniger Trainingsdaten für .NET 10, aber der Unterschied zu .NET 8 für WPF ist minimal

---

## ADR-006: Solution-Struktur mit 5 Projekten

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Architektur-Session, ChatGPT-Review Punkt 6

**Kontext:**

Erst war alles in `Shell + Core + Module`. Die Review kritisierte, dass Core zum Sammelcontainer werden kann. Clean Architecture mit 5+ Schichten (Domain/Application/Infrastructure/Presentation) wurde als "zu akademisch" für ein Solo-Projekt abgelehnt.

**Entscheidung:**

5 Projekte: App, Domain, Infrastructure, PlanManager, Settings. Domain + Infrastructure ist die minimale sinnvolle Trennung. Domain enthält fachliche Definitionen, Infrastructure die technische Umsetzung.

**Dependency-Regel (eisern):**
```
Domain          → referenziert NICHTS
Infrastructure  → referenziert nur Domain
PlanManager     → referenziert Domain + Infrastructure
Settings        → referenziert Domain + Infrastructure
App             → referenziert alles (DI verdrahtet hier)
```

**Alternativen:**

- *Shell + Core:* Zu wenig Trennung, Core wird Sammelcontainer.
- *Clean Architecture (5+ Schichten):* Zu akademisch, zu viele Dateien für Solo-Entwickler.

**Konsequenzen:**

- Infrastructure könnte mit der Zeit groß werden — dann in Unter-Namespaces gliedern (Persistence, FileSystem, Logging), ohne neues Projekt
- Jedes Feature-Modul (PlanManager, Settings) ist ein WPF Class Library Projekt
- App-Projekt verdrahtet alles über DI

---

## ADR-007: Plan-Dateien — 1..n pro Revision

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Architektur-Session

**Kontext:**

Ursprünglich war angedacht, dass ein Plan immer aus genau einem PDF/DWG-Paar besteht. In der Praxis gibt es aber Pläne die nur als PDF kommen, nur als DWG, oder sogar aus mehreren PDFs bestehen (z.B. "Teil 1" und "Teil 2").

**Entscheidung:**

Ein Plan (Revision) besteht aus 1 bis n Dateien. Dateien werden über den gemeinsamen Dateinamen-Stamm (ohne Extension) zusammengeführt. Fehlende PDF oder DWG ist kein Fehler.

**Konsequenzen:**

- Flexibleres Datenmodell (PlanRevision → List<PlanFile>)
- Import-Workflow muss Gruppierung können (gleicher Stamm = gleiche Revision)
- Undo-Journal muss pro Aktion mehrere Dateien tracken (→ 3 SQLite-Tabellen)

---

## ADR-008: 10-Schritte Import-Workflow

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Architektur-Session, ChatGPT-Review Punkt 7

**Kontext:**

Der Import von Plänen (Dateien von `_Eingang` in die Zielordner verschieben) ist die kritischste Operation. Wenn der Prozess abstürzt, könnten Dateien in einem inkonsistenten Zustand sein. Die Review forderte einen transaktionalen Ansatz.

**Entscheidung:**

10-Schritte-Workflow: Scan → Parse → Validate → Classify → Plan → Preview (User) → Execute (mit Journal) → Finalize → Recover (beim App-Start) → Undo.

**Konsequenzen:**

- Journal wird VOR Dateiverschiebung geschrieben (Status "pending")
- Bei Abbruch: Beim nächsten App-Start Recovery anbieten
- Undo ist möglich (Journal rückwärts lesen, Dateien zurückverschieben)
- Backup von SQLite + JSON vor jedem Import

---

## ADR-009: Undo-Journal in SQLite (3 Tabellen)

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Architektur-Session, ChatGPT-Review Punkt 8

**Kontext:**

Undo war ursprünglich als JSON geplant. Die Review forderte SQLite für Transaktionssicherheit.

**Entscheidung:**

3 SQLite-Tabellen in `planmanager.db`: `import_journal` (pro Import), `import_actions` (pro Aktion mit Reihenfolge), `import_action_files` (pro Datei pro Aktion). Status-Tracking: pending → completed → failed → undone.

**Konsequenzen:**

- Robustes Undo auch nach App-Absturz
- Journal unterstützt 1..n Dateien pro Aktion (wegen ADR-007)
- Recovery beim App-Start prüft auf "pending"-Einträge

---

## ADR-010: RecognitionProfiles und PatternTemplates getrennt

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Architektur-Session

**Kontext:**

Beim Anlernen von Plantyp-Mustern gibt es zwei Konzepte: Das verbindliche Profil für ein Projekt, und der Vorschlag aus einer Musterbibliothek.

**Entscheidung:**

- **RecognitionProfile** = verbindlich pro Projekt/Plantyp, gespeichert in `profiles.json` (Cloud-Speicher, pro Projekt)
- **PatternTemplate** = Vorschlag aus Musterbibliothek, gespeichert in `pattern-templates.json` (Cloud-Speicher, global)

Beim Anlegen eines neuen Profils vergleicht das System mit bestehenden Templates und schlägt Übernahme vor. Neues Profil wird automatisch als Template gespeichert.

**Konsequenzen:**

- Kein Machine Learning, keine Blackbox — immer User-Bestätigung
- Templates synchen über Cloud-Speicher (auf beiden Geräten gleiche Vorschläge)
- Sync-Konfliktrisiko gering (Templates werden selten bearbeitet)

---

## ADR-011: Ordnernamen mit Nummern und Leerzeichen

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Analyse realer Projektordner (via PowerShell-Tool `Get-ProjektOrdner.ps1`)

**Kontext:**

Herberts reale Projektordner verwenden nummerierte Präfixe. Die Frage war: Leerzeichen oder Unterstriche zwischen Nummer und Name?

**Entscheidung:**

Leerzeichen: `01 Planunterlagen`, `02 Fotos`, nicht `01_Planunterlagen`. Entspricht Herberts bestehendem Schema, das sich über Jahre entwickelt hat.

**Alternativen:**

- *Unterstriche:* Technisch einfacher (keine Leerzeichen in Pfaden), aber weicht von bestehendem Schema ab.

**Konsequenzen:**

- `FolderTemplateEntry.GetNumberedName(position)` generiert: `$"{position:D2} {Name}"`
- Bestehende Ordner passen zum neuen Schema
- Pfade mit Leerzeichen erfordern Anführungszeichen in Skripten (kein Problem in C#)

---

## ADR-012: Nummern-Präfix aus Listenposition (nicht gespeichert)

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Implementierung Feature #10

**Kontext:**

Sollen die Ordnernummern (00, 01, 02...) im Template gespeichert werden, oder automatisch aus der Position in der Liste generiert werden?

**Entscheidung:**

Die Nummer wird NICHT im Template gespeichert. Die Position in der Liste bestimmt die Nummer. Position 0 → "00 Sonstiges", Position 1 → "01 Planunterlagen" etc. Beim Umsortieren ändern sich die Nummern automatisch.

**Konsequenzen:**

- Template speichert nur `Name` + `HasInbox` + optionale Unterordner
- Beim Drag&Drop/Umsortieren aktualisiert sich die Vorschau sofort
- Einfacheres Datenmodell

---

## ADR-013: .bpm-manifest als Projektordner-Ausweis

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Architektur-Session

**Kontext:**

Wenn ein Projektordner umbenannt wird (im Explorer), verliert die App den Bezug zum Projekt. Die Frage war: Wie erkennt die App den Ordner wieder?

**Entscheidung:**

Jeder Projektordner enthält eine versteckte `.bpm-manifest`-Datei (JSON) mit der Projekt-ID und dem Pfad zur Registry. Bei Ordner-Umbenennung sucht die App nach `.bpm-manifest`-Dateien im BasePath und aktualisiert den Pfad automatisch.

**Konsequenzen:**

- Robuste Pfad-Erkennung auch nach Umbenennung
- `.bpm-manifest` hat Hidden-Attribut (nicht für Kollegen sichtbar)
- Syncht über Cloud-Speicher (liegt im Projektordner)
- Feature #11 im Backlog (noch nicht implementiert)

---

## ADR-014: C# + WPF statt PowerShell

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Architektur-Session (Anfang)

**Kontext:**

Herbert hatte bereits PowerShell-Tools (PhotoFolder) und überlegte, ob der PlanManager auch in PowerShell + WPF gebaut werden sollte. Claude zeigte die Vor/Nachteile beider Ansätze.

**Entscheidung:**

C# + WPF. PowerShell bleibt für bestehende Tools (PhotoFolder) und kleine Automatisierungs-Skripte.

**Alternativen:**

- *PowerShell + WPF:* Herbert kennt PowerShell, aber WPF in PowerShell ist ungewöhnlich, wenige Tutorials, kein XAML-Designer. Deployment schwieriger (braucht PS 7 + Module).
- *C# + WPF:* Standard-Tooling, XAML-Designer in Visual Studio, NuGet-Ökosystem, eine .exe die einfach funktioniert.

**Konsequenzen:**

- Herbert lernt eine neue Sprache (C#), aber Claude schreibt den Code
- Professionelleres Deployment (Single-file .exe)
- Besseres NuGet-Ökosystem für Excel/PDF/SQLite
- Visual Studio Community als IDE (kostenlos, deutsch)

---

## ADR-015: CommunityToolkit.Mvvm + Serilog von Anfang an

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Architektur-Session

**Kontext:**

Sollen NuGet-Pakete für MVVM und Logging von Anfang an eingesetzt werden, oder erst eigene Implementierungen und später umstellen?

**Entscheidung:**

CommunityToolkit.Mvvm (MVVM-Boilerplate-Reduktion) und Serilog (Structured Logging) werden von Anfang an verwendet. Spart ~50% Boilerplate-Code. Herbert merkt beim Testen keinen Unterschied.

**Konsequenzen:**

- `[ObservableProperty]` und `[RelayCommand]` Attribute statt manuellem INotifyPropertyChanged
- Serilog mit File + Console Sink, tägliche Rotation, 30 Tage Aufbewahrung
- Structured Logging mit `{PropertyName}` Platzhaltern
- Logging in `%LocalAppData%\BauProjektManager\Logs\`

---

## ADR-016: Coding Standards + Definition of Done

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Architektur-Session, ChatGPT-Review Punkt 11

**Kontext:**

Die Review kritisierte fehlende Coding Standards für Nullable, Async, Logging-Konventionen.

**Entscheidung:**

V1-Pflicht Standards: Nullable Reference Types (`<Nullable>enable</Nullable>`), `.editorconfig`, CancellationToken für alle async-Methoden, Verbot von `async void` (außer UI Event-Handler), `using`-Statement Pflicht für IDisposable, Schema-Version in jeder DB und JSON, atomische JSON-Writes (write-to-temp-then-rename).

Definition of Done pro Feature:
- Code kompiliert ohne Fehler und Warnungen
- Manuelle Tests (Happy Path + ein Fehlerfall)
- Logging vorhanden (Info für Hauptaktionen, Error für Fehler)
- Nullable Warnings aufgelöst
- Git Commit mit korrektem Format

Nicht für V1: COM-Objektfreigabe (erst bei Outlook/Excel-Modul), Migrations-Framework.

---

## ADR-017: VBA liest nur, schreibt nie

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Architektur-Session

**Kontext:**

Herbert nutzt Outlook-VBA und Excel-VBA Makros. Die Frage war: Sollen VBA-Makros auch Projektdaten ändern können?

**Entscheidung:**

VBA liest nur `registry.json`, schreibt nie. Die C#-App ist der einzige Writer. Das vereinfacht die Architektur erheblich — kein bidirektionaler Sync nötig.

**Konsequenzen:**

- `registry.json` ist ein generierter, read-only Export
- Neue Projekte/Änderungen nur über die C#-App
- Kein Risiko, dass VBA die Daten inkonsistent macht
- Falls Herbert VBA langfristig ablöst, ist der Export einfach zu entfernen

---

## ADR-018: Arbeitszeiterfassung — WPF als Eingabemaske, Excel als Wahrheitsquelle

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Phase 1 Teil 1 Chat

**Kontext:**

Herbert hat ein bestehendes Excel-basiertes Zeiterfassungssystem mit Power Query, Pivot-Tabellen und Formeln. Soll die C#-App das ablösen?

**Entscheidung:**

Nein. Excel bleibt die Single Source of Truth für Zeitdaten. WPF liefert nur eine schöne Eingabemaske (Dark Theme, Dropdowns, Kalender). Daten werden per ClosedXML direkt in die Excel-Tabelle `tbl_Zeiten` geschrieben (append-only). Excel behält alle Formeln, Power Query, Pivot und Auswertungen. Baustellen-Dropdown kommt aus `bpm.db` / `registry.json`.

**Alternativen:**

- *Alles in SQLite:* Würde Excel-Formeln/Pivot/Power Query verlieren. Lohnbüro liest aktuell Excel direkt über OneDrive.
- *COM Interop:* Erfordert Excel auf dem Rechner. ClosedXML braucht kein Excel.

**Konsequenzen:**

- ClosedXML als NuGet-Paket, kein Excel nötig zum Schreiben
- Excel-Architektur muss vorher fertig sein (tbl_Zeiten Schema etc.)
- Kein eigenes Überstunden-Modul in C# — das macht Excel

---

## ADR-019: Mobile PWA statt Native App (aufgeschoben)

**Datum:** 2026-03  
**Status:** 🟡 Konzept, Umsetzung aufgeschoben  
**Herkunft:** Smartphone-App Chat

**Kontext:**

Herbert wollte eine Smartphone-App für Bautagebuch-Einträge und Plan-Viewer auf der Baustelle. Drei Optionen: .NET MAUI (native), PWA (Browser-App), Hybrid.

**Entscheidung:**

PWA (Progressive Web App) im Browser. Kein App Store nötig, funktioniert auf jedem Handy. Offline-fähig über Service Worker + IndexedDB. Umsetzung erst nach Stabilisierung der Desktop-Features.

**Alternativen:**

- *.NET MAUI:* C#-Code-Sharing möglich, aber neues Framework, App Store nötig.
- *React Native / Flutter:* Neue Sprache, kein Code-Sharing.

**Konsequenzen:**

- ASP.NET Minimal API als Backend (oder Microsoft Graph API für OneDrive-Variante)
- Zwei Sync-Optionen offen gehalten: Option A (Cloud/Graph API), Option B (lokaler Server im LAN)
- Desktop-Core muss erst stabil sein
- Konzeptdokument: `BPM-Mobile-Konzept.md` v0.3

---

## ADR-020: Write-Lock mit Heartbeat für Multi-Device-Sync (aufgeschoben)

**Datum:** 2026-03  
**Status:** 🟡 Konzept, Umsetzung aufgeschoben  
**Herkunft:** Smartphone-App Chat, Vergleich mit Notion/ClickUp/Excel

**Kontext:**

Wie verhindert man Konflikte wenn Desktop und Mobile gleichzeitig schreiben? Herbert schlug einen Lock-Mechanismus vor. Verglichen wurde mit Notion (CRDTs), ClickUp (Cloud-Sync), und Excel Co-Authoring (Zell-Level-Locking).

**Entscheidung:**

Exklusiver Schreibzugriff mit Warteschlange. Wer den Lock hält, darf schreiben. Alle anderen lesen nur. Heartbeat alle 60 Sekunden. Auto-Release nach ~3 Minuten ohne Heartbeat (konfigurierbar). Bei Offline: großzügigerer Timeout (30 Min).

**Alternativen:**

- *CRDTs (wie Notion):* Text-Merges automatisch, aber komplex. Overkill für BPM.
- *Zell-Locking (wie Excel):* Granulares gleichzeitiges Arbeiten, aber enormer Entwicklungsaufwand.
- *Last-Write-Wins:* Einfach, aber Datenverlust möglich.

**Konsequenzen:**

- Konflikte sind komplett ausgeschlossen (nur ein Schreiber)
- Passt zum Baustellen-Szenario (Polier schreibt, andere lesen)
- Einfache Implementierung
- Konzept erst relevant bei Mobile-Umsetzung

---

## ADR-021: Client/Firma als eigene Entität (Vorbereitung)

**Datum:** 2026-03  
**Status:** 🟡 Konzept  
**Herkunft:** Backlog Vision-Sektion

**Kontext:**

Aktuell ist der Auftraggeber (Client) als eingebettetes Objekt im Projekt gespeichert. Für die Zukunftsvision (Firmendaten-Verwaltung, Portal-Links, Adressbuch) sollte Client/Firma eine eigene Entität in der DB sein.

**Entscheidung:**

Für V1: Client bleibt als eingebettetes Objekt im Projekt (einfacher). Aber das Domänenmodell ist so aufgebaut, dass der Umbau zu einer eigenen Entität mit Fremdschlüssel später möglich ist. Die Client-Klasse existiert bereits als separates Modell (`Client.cs`).

**Konsequenzen:**

- In V1: Pro Projekt eigene Client-Daten (Duplikate möglich)
- Später: Eigene Clients-Tabelle in SQLite, Projekte referenzieren per client_id
- Dashboard-Vision nutzt Firmendaten für Portal-Links und Kontaktdaten

---

## ADR-022: Segment-basiertes Dateinamen-Parsing (Hybrid)

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Architektur-Session (PlanManager-Konzept)

**Kontext:**

Plandateinamen haben unterschiedliche Formate je nach Projekt/Plantyp. Wie soll der User dem System beibringen, wo Nummer, Index, Geschoß etc. stecken?

**Entscheidung:**

Hybrid-Ansatz: Dateiname wird an Trennzeichen gesplittet (-, _, .) → klickbare Segment-Blöcke in der GUI. User klickt auf ein Segment und weist es einem Feld zu (planNumber, planIndex, geschoss, haus, etc.). Bei Bedarf: Zeichen-Level Fallback via Toggle-Button für Feinauswahl innerhalb eines Segments.

Verfügbare Felder: Pflicht (planNumber, planIndex), System (projectNumber, description, ignore), bau-spezifisch vordefiniert (geschoss, haus, planart, objekt, bauteil, bauabschnitt, stiege, achse, zone, block), plus benutzerdefinierte Felder.

**Konsequenzen:**

- 3-Schritt-Wizard: Typ wählen → Muster definieren → Ordnerstruktur festlegen
- Ordner-Hierarchie frei konfigurierbar: User wählt per Checkbox welche Felder Ordner-Ebenen werden
- Plantyp immer Ebene 1 (fix)
- Ordner-Reihenfolge nur beim Profil-Erstellen festlegbar

---

## ADR-023: Arbeitsteilung — Claude schreibt Code, Herbert committet

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Alle Chats, mehrfach bekräftigt

**Kontext:**

Herbert hat keinen Programmier-Hintergrund. Claude schreibt allen Code. Herbert kopiert den Code in Visual Studio, testet, und committet selbst.

**Entscheidung:**

- Claude schreibt allen Code und gibt ihn als SUCHE/ERSETZE-Blöcke oder Download-Dateien
- Herbert kopiert, testet lokal, committet und pusht selbst
- Claude ist explizit verboten, direkt auf GitHub zu pushen (einzige Ausnahme war BACKLOG.md, jetzt auch nicht mehr)
- Claude verifiziert nach jedem Push per `github:get_file_contents`, ob der Code tatsächlich auf dem Remote ist
- XAML-Dateien als Download (nicht als PowerShell here-strings — Encoding-Probleme)
- Multi-line Code nicht in Terminal pasten (Zeilen werden konkateniert)

**Konsequenzen:**

- Commit-Format: `[vX.Y.Z] Modul, Typ: ShortTitle`
- Semantic Versioning: Minor für Features, Patch für Fixes
- Herbert hat volle Kontrolle über den Git-Verlauf
- Bei Build-Fehlern nach neuen Dateien: "Erstellen → Projektmappe bereinigen" (Clean Solution)
- GitHub-State immer verifizieren (Diskrepanzen zwischen lokalem und Remote-State sind vorgekommen)

---

## ADR-024: Adressbuch getrennt von Projekt-Beteiligten

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Tab 3 Beteiligte Implementierung

**Kontext:**

Im Tab 3 werden Projekt-Beteiligte erfasst (Architekt, Statiker, ÖBA etc.). Herbert möchte die Kontaktdaten auch projektübergreifend wiederverwenden und später mit Outlook-Kontakten synchronisieren.

**Entscheidung:**

Zwei getrennte Ebenen:
- **project_participants** (projektbezogen): Rolle im Projekt + Kontaktdaten direkt gespeichert.
- **contacts** (zentral, kommt später): Personen/Firmen projektübergreifend, Outlook-kompatibel.
- Verknüpfung über `contact_id` FK in project_participants (Feld vorbereitet, aktuell leer).

**Konsequenzen:**

- Tab 3 funktioniert sofort ohne Adressbuch
- Kontaktdaten zunächst pro Projekt dupliziert (akzeptabel für V1)
- Späterer Umbau: Daten aus contacts-Tabelle lesen statt direkt
- Outlook-Sync läuft über contacts-Tabelle

---

## ADR-025: Status vereinfacht — nur Active und Completed

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Tab 1 Stammdaten Implementierung

**Kontext:**

Ursprünglich hatte ProjectStatus drei Werte: Active, Completed, Archived. Archived ist redundant — Archivierung ist eine Aktion (Ordner verschieben), kein Status.

**Entscheidung:**

Nur zwei Status: Active und Completed. Archivierung als Feature #12 separat.

**Konsequenzen:**

- StatusColorConverter: Grün = Active, Rot = Completed
- Archiv-Button vorbereitet aber disabled
- Einfacheres UI

---

## ADR-026: Portal-Typen als editierbare Liste

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Tab 4 Portale + Links Implementierung

**Kontext:**

Bauherren nutzen verschiedene Portale (InfoRaum, PlanRadar, PlanFred, Bau-Master, Dalux). Hardcoded Enum oder editierbare Liste?

**Entscheidung:**

Editierbare Liste in settings.json (PortalTypes), änderbar über ✎-Button. Gleicher Ansatz wie ProjectTypes, BuildingTypes, ParticipantRoles, LevelNames.

**Konsequenzen:**

- Neue Portale ohne Code-Änderung hinzufügbar
- Links mit LinkType "Portal" links, "Custom" rechts im 2-Spalten-Layout
- Dashboard-Vorschau zeigt nur konfigurierte Links

---

## ADR-027: KI-API-Import für Datenextraktion

**Datum:** 2026-03  
**Status:** 🟡 Konzept  
**Herkunft:** Tab 3 Firmenliste-Import, Plankopf-Konzept

**Kontext:**

Mehrere Features erfordern Extraktion strukturierter Daten aus unstrukturierten Quellen (Firmenlisten-PDF, Planköpfe, Planlisten). Manuelles Parsing per Regex/Heuristik zu fehleranfällig.

**Entscheidung:**

Zweistufiger Ansatz:
- **Phase 1 (manuell):** App zeigt Prompt → User kopiert zu Claude/ChatGPT → fügt Antwort ein → App parst JSON
- **Phase 2 (automatisch):** App ruft KI-API direkt auf (ChatGPT oder Claude API) → empfängt JSON
- **Systemeinstellungen:** Auswahl zwischen ChatGPT API und Claude API (Anthropic API)
- **Offline-Fallback:** Manueller Ablauf bleibt immer verfügbar

**Anwendungsfälle:**

- Firmenliste importieren (Tab 3)
- Plankopf-Extraktion (Index, Revision, Plannummer)
- Index-Import (Planlisten aus PDF)
- Zukünftige Imports

**Konsequenzen:**

- JSON als Standard-Austauschformat
- Service-Interface `IKiImportService` mit Implementierungen für Claude und ChatGPT
- Prompt-Templates als versionierte Ressourcen in der App
- API-Keys sicher speichern (Windows Credential Manager, nicht in settings.json)

---

## ADR-028: Theme-System mit Resource Dictionaries

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Phase 1 Teil 2, UI/UX Review Session

**Kontext:**

Die ersten Views (MainWindow, SettingsView, ProjectEditDialog) hatten alle Farben, Schriftgrößen und Styles direkt in den XAML-Dateien als hardcoded Werte (`#007ACC`, `#2D2D30`, `FontSize="14"`). Bei 5+ Views wurde das unübersichtlich — eine Farbänderung erforderte Suchen/Ersetzen in jeder Datei. Die UI/UX Guidelines v2.0 definierten ein Design-System mit Token, aber die Umsetzung fehlte.

**Entscheidung:**

Zentrales Theme-System mit 5 Resource Dictionaries im Ordner `Themes/` des App-Projekts:

- **Colors.xaml** — Alle Farb-Token als SolidColorBrush (Background, Surface, Text, Accent, Status-Farben)
- **Typography.xaml** — Schriftgrößen-Stufen (XS bis XXL, Segoe UI)
- **Buttons.xaml** — Button-Varianten (Primary, Secondary, Danger, Ghost, Nav)
- **DataGrid.xaml** — Header, Row, Cell, Zebra-Variante
- **Dialogs.xaml** — Dialog-Basis, Tabs, Cards, Tooltips, Separatoren

Alle Dictionaries werden in `App.xaml` per `MergedDictionaries` geladen. Views verwenden ausschließlich `{StaticResource TokenName}` statt hardcoded Werte.

**Alternativen:**

- *Third-Party Theme (MahApps, Material Design):* Mächtiger, aber externe Abhängigkeit, schwer anzupassen, Overkill für BPM.
- *Weiter hardcoded:* Funktioniert, aber wird bei wachsender Codebasis unwartbar.
- *Ein einzelnes großes Styles.xaml:* Weniger Dateien, aber unübersichtlich bei 50+ Styles.

**Konsequenzen:**

- Farbänderungen nur an einer Stelle (Colors.xaml)
- Konsistentes Aussehen über alle Views
- Light Theme später einfach als zweites Color-Set möglich
- Migration der bestehenden Views (SettingsView, ProjectEditDialog) auf Token steht noch aus — erst nach PlanManager
- CODING_STANDARDS.md um UI-Naming-Konventionen erweitert

---

## ADR-029: Arbeitspaket als zentrales Verbindungskonzept

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Phase 1 Teil 2, Kalkulations-Modul Konzept

**Kontext:**

BPM hat mehrere Module die inhaltlich zusammenhängen: Kalkulation (Soll-Mengen), Arbeitseinteilung (wer arbeitet wo), Zeiterfassung (Stunden), Bautagebuch (tägliches Protokoll), Nachkalkulation (Soll/Ist). Die Frage war: Wie verbinden diese Module ihre Daten?

Verglichen wurde mit professionellen Kalkulations-Tools die mit "Vorgängen" oder "Arbeitspaketen" arbeiten. Herbert hat bestehende Excel-Tabellen (Kalkulation_v2.xlsx, 44 Blätter nach LB-H Leistungsgruppen) die als Referenz dienten.

**Entscheidung:**

Das **Arbeitspaket** (`work_packages` Tabelle) ist die zentrale Entität. Ein Arbeitspaket = Bauteil + Geschoß + Tätigkeit + Soll-Menge. Beispiel: "H5 / EG / Mauerwerk 38er / 198 m²".

Alle Module buchen auf Arbeitspakete:
- Arbeitseinteilung: wer → welches Paket (täglich)
- Zeiterfassung: Stunden fließen über Zuordnung in Pakete
- Bautagebuch: Auto-Vorschlag aus zugewiesenen Paketen
- Nachkalkulation: Soll-Stunden vs. Ist-Stunden pro Paket

Arbeitspaket referenziert bestehende Tabellen `building_parts` und `building_levels` per FK — keine Änderung an bestehender Architektur nötig, nur neue Tabellen.

**Konsequenzen:**

- 7 neue Tabellen geplant (work_packages, work_assignments, employees, time_entries, lv_positions, performance_catalog, project_difficulty)
- Bestehende Tabellen bleiben unverändert — nur neue FKs zeigen auf sie
- DB-SCHEMA.md als zentrales Leitdokument (ADR-031)
- Konzeptdokument: `ModuleKalkulation.md`

---

## ADR-030: Abschluss-Erfassung statt Tages-Aufmaß

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Phase 1 Teil 2, Kalkulations-Modul Konzept

**Kontext:**

Professionelle Nachkalkulations-Tools erwarten tägliche Mengenerfassung pro LV-Position (z.B. "heute 12 m² Mauerwerk gemauert"). Herbert hat Erfahrung mit solchen Excel-Tabellen — sie scheitern in der Praxis, weil der Polier auf der Baustelle nicht jeden Abend 40 Spalten befüllen kann. Die Folge: leere Tabellen, geschätzte Werte, wertlose Daten.

**Entscheidung:**

Abschluss-Erfassung statt täglicher Mengenerfassung. Der Polier muss täglich nur eine einfache Sache tun: die Arbeitseinteilung (wer arbeitet an welchem Paket). Die Stunden werden automatisch aus der Zeiterfassung berechnet. Erst wenn ein Arbeitspaket fertig ist ("Mauerwerk H5/EG abgeschlossen"), bestätigt der Polier die tatsächliche Menge. Dann berechnet das System die Leistungswerte (m²/Ah, h/m²) und speichert sie im Erfahrungskatalog.

**Alternativen:**

- *Tägliches Aufmaß:* Theoretisch genauer, in der Praxis undurchführbar. Leere Tabellen nach 2 Wochen.
- *Gar keine Mengen:* Dann keine Nachkalkulation möglich.
- *KI-basierte Schätzung:* Zu ungenau, kein Vertrauen in die Daten.

**Konsequenzen:**

- Täglicher Aufwand für Polier: 2 Minuten (Arbeitseinteilung), nicht 20 Minuten (Aufmaß)
- Genauigkeit auf Arbeitspaket-Ebene (nicht Tagesebene) — reicht für Praxis
- Erfahrungskatalog wächst automatisch mit jedem abgeschlossenen Paket
- Konzeptdokument: `ModuleKalkulation.md` Kapitel 6

---

## ADR-031: DB-SCHEMA.md als zentrales Leitdokument

**Datum:** 2026-03  
**Status:** ✅ Entschieden  
**Herkunft:** Phase 1 Teil 2

**Kontext:**

Mit wachsender Anzahl an Modulen (Kalkulation, Zeiterfassung, Bautagebuch, Task-Management) und geplanten Tabellen entstand das Risiko, dass DB-Schema-Entwürfe über viele Dokumente verstreut und inkonsistent werden. Jedes Modul-Konzept hatte eigene Tabellen-Entwürfe.

**Entscheidung:**

Ein zentrales Dokument `Docs/DB-SCHEMA.md` ist die **einzige Quelle der Wahrheit** für die gesamte Datenbankstruktur. Modul-Konzepte referenzieren hierher statt eigene Schemas zu definieren. Das Dokument enthält:

1. Implementierte Tabellen mit exaktem SQL
2. Geplante Tabellen mit SQL-Entwürfen
3. Beziehungsdiagramm (FK-Übersicht)
4. Modul-Zuordnung (wer besitzt/schreibt, wer liest)
5. Schema-Migrationshistorie
6. Naming-Konventionen

**Konsequenzen:**

- Jede Schema-Änderung wird zuerst in DB-SCHEMA.md geplant
- Modul-Konzepte verweisen auf DB-SCHEMA.md statt SQL zu wiederholen
- Keine Inkonsistenzen zwischen Modulen
- Implementiert als `Docs/DB-SCHEMA.md` (v1.5)

---

## ADR-032: ITaskManagementService — nicht an ClickUp gebunden

**Datum:** 2026-03  
**Status:** 🟡 Konzept  
**Herkunft:** Phase 1 Teil 2, ClickUp-Integration Diskussion

**Kontext:**

Herbert nutzt ClickUp für die Materialbestellung auf Baustellen (Bauleiter, Dispo, Lager, Einkauf). Die Frage war: Soll BPM direkt gegen die ClickUp-API bauen, oder eine Abstraktionsschicht verwenden?

**Entscheidung:**

Interface-basierte Architektur. BPM spricht nicht direkt mit ClickUp, sondern mit einem `ITaskManagementService`. Dahinter steckt die konkrete Implementierung. ClickUp ist die erste, aber nicht die einzige.

Geplante Implementierungen:
- `ClickUpTaskService` — Herberts Setup (erste Implementierung)
- `AsanaTaskService`, `TrelloTaskService`, `MondayTaskService`, `MicrosoftPlannerTaskService` — Zukunft
- `LocalTaskService` — Offline-Fallback (nur SQLite, kein externes Tool)

**Alternativen:**

- *Direkt gegen ClickUp-API:* Schneller zu bauen, aber Vendor Lock-in. Andere Firmen nutzen andere Tools.
- *Kein Task-Integration:* Materialbestellung bleibt komplett im externen Tool. BPM hat keine Übersicht.

**Konsequenzen:**

- In Systemeinstellungen: Dropdown "Welches Projektmanagement-Tool?"
- API-Keys in Windows Credential Manager
- `material_orders` Tabelle in bpm.db mit `external_task_id` + `external_system` Spalten
- Verkaufsargument: "Funktioniert mit deinem bestehenden Tool"
- Konzeptdokument: `ModuleTaskManagement.md`

---

## ADR-033: Multi-User — 3 Modi (eigene DB, geteilte DB, Server)

**Datum:** 2026-03  
**Status:** 🟡 Konzept  
**Herkunft:** Phase 1 Teil 2

**Kontext:**

BPM ist aktuell Single-User. Wenn mehrere Poliere/Bauleiter die gleiche App nutzen sollen, braucht es Multi-User-Support. Die Frage war: Wie aufwendig und welche Optionen?

**Entscheidung:**

Drei Modi, schrittweise aktivierbar:

| Modus | Beschreibung | Komplexität |
|-------|-------------|-------------|
| **A: Eigene DB** | Jeder User hat sein eigenes bpm.db (so wie jetzt). Solo-Betrieb. | Null (ist schon so) |
| **B: Geteilte DB** | bpm.db liegt auf einem Netzlaufwerk oder Cloud-Ordner. Write-Lock mit Heartbeat (ADR-020). Read-Only Fallback wenn Lock belegt. | Mittel |
| **C: Server** | ASP.NET Minimal API auf einem Raspberry Pi (oder anderem Rechner) im LAN. Desktop + Mobile verbinden sich per REST API. Server besitzt die DB exklusiv — kein Sync-Konflikt. | Höher |

Technisch wird ein `IDataService` Interface eingeführt mit 3 Implementierungen: `LocalDataService` (A), `SharedDbDataService` (B), `ServerDataService` (C). Umschaltung in den Systemeinstellungen.

**Alternativen:**

- *Cloud-DB (Azure/Firebase):* Internet-Pflicht, Abo — widerspricht Offline-Prinzip.
- *CRDTs:* Automatische Merge-Konflikte — zu komplex für den Nutzen.

**Konsequenzen:**

- Modus A sofort verfügbar (ist der Status quo)
- Modus B erfordert: IDataService Refactoring + Write-Lock + Read-Only Fallback
- Modus C erfordert: ASP.NET Minimal API + REST-Endpoints + Server-Setup-Anleitung
- Kein Berechtigungsmanagement geplant (Vertrauensbasis im kleinen Team)
- Konzeptdokument: `MultiUserKonzept.md`

---

## ADR-034: Modul-Aktivierung + Offline-Lizenzierung

**Datum:** 2026-03  
**Status:** 🟡 Konzept  
**Herkunft:** Phase 1 Teil 2

**Kontext:**

Herbert plant langfristig, BPM an andere Baufirmen zu verkaufen. Dafür braucht es zwei Dinge: Module müssen ein-/ausschaltbar sein (aufgeräumte Oberfläche), und es braucht ein Bezahlmodell pro Modul.

**Entscheidung:**

**Modul-Aktivierung:** In den Systemeinstellungen gibt es eine Seite "Module" mit Ein/Aus-Schalter pro Modul. Nur aktive Module erscheinen in der Sidebar. Einstellungen und PlanManager sind immer an (Basis). Abhängigkeiten werden geprüft (z.B. Bautagebuch braucht Kalkulation).

**Lizenzierung:** Offline-fähige Lizenzdateien (`.bpm-license`) pro Modul. Keine Online-Aktivierung — passt zur Offline-Philosophie. Technisch: JSON-Payload mit HMAC-SHA256 Signatur (shared secret). Enthält Kundenname, freigeschaltete Module, Ablaufdatum.

30-Tage-Testversion pro Modul: Erstaktivierung wird lokal gespeichert (verschlüsselt in `%LocalAppData%`). Nach 30 Tagen → Modul gesperrt bis Lizenz importiert wird.

Verkaufsmodell:
- **Basis (kostenlos):** Einstellungen + PlanManager + Dashboard
- **Zusatzmodule (einzeln):** Bautagebuch, Zeiterfassung, Kalkulation, Foto, Outlook, Vorlagen, Wetter
- **Premium:** KI-Assistent, Task-Management, Mobile PWA
- **Keine Abos** — Einmalkauf pro Modul, Updates inklusive innerhalb der Major-Version

**Alternativen:**

- *Online-Aktivierung:* Piraterie-Schutz besser, aber Internet-Pflicht.
- *Alles kostenlos:* Kein Geschäftsmodell.
- *Abo-Modell:* Herbert will explizit keine Abos — "Ich will nicht der nächste PlanRadar sein."

**Konsequenzen:**

- Modulare Sidebar ist architektonisch bereits vorbereitet (ADR-001, separate DLL-Projekte)
- `ModuleRegistry` als zentrale Klasse für Aktivierungsstatus
- LicenseValidator als Service in Infrastructure
- Konzeptdokument: `ModuleAktivierungLizenzierung.md`

---

## ADR-035: IExternalCommunicationService — zentrales Privacy Gate

**Datum:** 2026-04  
**Status:** ✅ Entschieden  
**Herkunft:** DSGVO-Analyse + externe Reviews (Claude + ChatGPT)

**Kontext:**

BPM hat mehrere Module die externe HTTP-Calls machen werden: KI-Assistent (OpenAI/Anthropic), GIS (Google Maps, GIS Steiermark), Wetter (OpenMeteo), Task-Management (ClickUp/Asana). Ohne zentralen Kontrollpunkt gibt es keinen Überblick welche Daten nach außen gehen, keinen Kill-Switch und kein Audit-Log. DSGVO Art. 25 verlangt „Datenschutz durch Technikgestaltung" — das bedeutet: ein zentraler Enforcement Point, nicht verteilte HttpClient-Calls in jedem Modul.

**Entscheidung:**

Ein `IExternalCommunicationService` in `BauProjektManager.Infrastructure/Communication/` ist der einzige erlaubte Weg für HTTP-Calls an externe Dienste. Direkter `HttpClient`-Zugriff für externe APIs ist verboten.

Der Service ist kein reiner Logger, sondern ein **Policy Gate** das aktiv entscheidet:
```csharp
public enum DataClassification
{
    ClassA,  // Keine Personendaten (Koordinaten, Hashes, Wetter)
    ClassB,  // Personenbezogene Daten (Kontakte, Mitarbeiter)
    ClassC   // Sensible Drittdaten (LVs, Bescheide)
}

public interface IExternalCommunicationService
{
    Task<HttpResponseMessage> SendAsync(
        string module,
        HttpRequestMessage request,
        DataClassification classification,
        string purpose,
        CancellationToken ct = default);

    bool IsModuleAllowed(string module);
    List<ExternalCallLogEntry> GetRecentLog(int count = 50);
}
```

**Policy-Regeln (zentral im Service, nicht im Modul):**

| Prüfung | Konsequenz |
|---|---|
| Modul in Einstellungen deaktiviert | Blockiert |
| Globaler Kill-Switch aktiv | Blockiert alles |
| Klasse C ohne Anonymisierung | Default: Blockiert. Nur mit explizitem User-Override + Zweckangabe |
| Auto-Calls für Modul nicht freigeschaltet | Blockiert Hintergrund-Sync |
| KI-Modul ohne DPA-Bestätigung | Blockiert |

**Audit-Log** in SQLite (`external_call_log`) mit `classification`, `purpose`, `decision_reason` (z.B. `allowed_class_a`, `blocked_module_disabled`, `allowed_user_confirmed`).

**Alternativen:**

- *Jedes Modul macht eigene HttpClient-Calls:* Einfacher zu implementieren, aber kein zentraler Kontrollpunkt. Datenschutz-Logik wäre über den gesamten Code verstreut. Audit unmöglich.
- *Middleware/Proxy-Server:* Zu aufwändig für eine Desktop-App. Sinnvoll bei Web-Backend, nicht bei WPF.
- *Nur Logging ohne Enforcement:* Audit möglich, aber kein aktiver Schutz. Policy-Verletzungen werden nur dokumentiert, nicht verhindert.

**Konsequenzen:**

- Alle Module mit externem Kontakt (KI, GIS, Wetter, Task-Management) müssen `IExternalCommunicationService` nutzen
- Einstellungen → neuer Tab „Datenschutz & Externe Dienste" mit Toggle pro Modul und Audit-Log-Anzeige
- `DataClassification` Enum in Domain-Projekt (keine externe Abhängigkeit)
- `ExternalCommunicationService` in Infrastructure-Projekt
- Kill-Switch sofort wirksam — ein Toggle sperrt alle externen Calls
- Für V1 (Einstellungen + PlanManager) nicht nötig, da keine externen Calls. Implementierung vor dem ersten Online-Modul
- Detailliertes Konzept: [DSGVO-Architektur.md](DSGVO-Architektur.md)

---

*Dokument wird laufend aktualisiert wenn neue Architekturentscheidungen getroffen werden.*