---
doc_id: adr
doc_type: adr
authority: source_of_truth
status: active
owner: herbert
topics: [architektur-entscheidungen, adr, modularer-monolith, sqlite, ulid, datenschutz, wpf, mvvm]
read_when: [neue-architekturentscheidung, bestehende-adr-prüfen, adr-status-ändern, entscheidung-nachschlagen]
related_docs: [architektur, db-schema, dsvgo-architektur, planmanager]
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: Alle Architecture Decision Records des BPM-Projekts — Kontext, Entscheidung, Konsequenzen
- Autorität: source_of_truth
- Lesen wenn: Neue Architekturentscheidung treffen, bestehende ADR prüfen, Status ändern, Entscheidung nachschlagen
- Nicht zuständig für: Implementierungs-Details (→ jeweilige Modul-Docs), Code-Standards (→ CODING_STANDARDS.md)
- Kapitel: Fortlaufende ADRs (ADR-001 bis ADR-046+)
- Pflichtlesen: keine (gezieltes Nachschlagen per ADR-Nummer)
- Fachliche Invarianten:
  - Statusmodell: Decision Status (Proposed/Accepted/Superseded/Deprecated) getrennt von Implementation Status (Not Started/Partial/Implemented)
  - ADR-Nummern fortlaufend — nie wiederverwenden
  - Superseded-ADR bleibt erhalten mit Verweis auf Nachfolger
  - Jede ADR hat: Status, Kontext, Entscheidung, Konsequenzen

---

﻿# BauProjektManager — Architecture Decision Records (ADR)

**Erstellt:** 29.03.2026
**Aktualisiert:** 04.04.2026
**Version:** 1.2
**Kontext:** Alle Entscheidungen aus Architektur-Sessions, Review-Runden (ChatGPT + Claude), und Implementierungs-Chats.

### Statusmodell

| Ebene | Werte | Bedeutung |
|-------|-------|-----------|
| **Decision Status** | Proposed → Accepted → Superseded → Deprecated | Ist die Architekturentscheidung getroffen? |
| **Implementation Status** | Not Started → Partial → Implemented | Ist sie im Code umgesetzt? |

Ein ADR kann "Accepted" sein ohne implementiert zu sein (z.B. ADR-035: Entscheidung getroffen, Umsetzung vor erstem Online-Modul).

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
| 019 | Mobile PWA statt Native App | ✅ Accepted / Not Started | 2026-03 |
| 020 | Write-Lock mit Heartbeat für Shared SQLite im LAN | ✅ Accepted / Not Started | 2026-03 |
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
| 032 | ITaskManagementService — nicht an ClickUp gebunden | ✅ Accepted / Not Started | 2026-03 |
| 033 | Multi-User: 3 Modi (eigene DB, geteilte DB, Server) | 🟡 Konzept | 2026-03 |
| 034 | Modul-Aktivierung + Offline-Lizenzierung | 🟡 Konzept | 2026-03 |
| 035 | IExternalCommunicationService — zentrales Privacy Gate | ✅ Entschieden | 2026-04 |
| 036 | IPrivacyPolicy — austauschbare Policy für Internal/Commercial | ✅ Entschieden | 2026-04 |
| 037 | ISyncTransport — austauschbarer Sync-Transport (Folder/HTTP) | ✅ Accepted / Not Started | 2026-04 |
| 038 | IAccessControlService — rollenbasierte Projektfreigabe | 🟡 Konzept | 2026-04 |
| 039 | Einheitliches ID-Schema — ULID als Primärschlüssel | ✅ Accepted / Not Started | 2026-04 |
| 040 | Migrations- und Versionierungsstrategie (DB + JSON) | ✅ Accepted / Not Started | 2026-04 |
| 041 | Recovery / Degraded Mode | ✅ Accepted / Not Started | 2026-04 |
| 042 | Secrets und Credentials | ✅ Accepted / Not Started | 2026-04 |
| 043 | Dev-Tools — Lokales Debug-Toolset für Entwicklung | ✅ Entschieden / Not Started | 2026-04 |
| 044 | Icons.xaml — Zentrale Icon-Registry | ✅ Entschieden / Implemented | 2026-04 |
| 045 | IndexSource — Dreistufiges Modell für Plan-Index-Erkennung | ✅ Entschieden | 2026-04 |
| 046 | .bpm/ Ordner — Manifest-Split und Profilablage im Projektordner | ✅ Entschieden | 2026-04 |
| 047 | Datenarchitektur + Sync — State-based lokal, change-based sync | ✅ Entschieden | 2026-04 |

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

SQLite ist die einzige Wahrheitsquelle für alle BPM-Kerndaten (Projekte, Pläne, Stammdaten, Kalkulation, Bautagebuch). JSON dient nur als generierter Export für VBA-Interop (`registry.json`) und als selten geänderte Konfiguration (`settings.json`, `profiles.json`). Wenn JSON korrupt wird, kann es aus SQLite neu generiert werden.

**Ausnahme:** Das Zeiterfassungs-Modul — hier bleibt Excel die Single Source of Truth für Roh-Zeitbuchungen (ADR-018). BPM schreibt per ClosedXML in die Excel-Tabelle und liest Aggregate in SQLite für Kalkulation und Bautagebuch. Diese SQLite-Kopien sind **abgeleitet, nicht führend**.

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

**Modulinteraktionsregeln (verbindlich):**
- Feature-Module (PlanManager, Settings, Foto etc.) referenzieren **nicht gegenseitig**. Keine Projekt-Referenz von PlanManager → Settings oder umgekehrt.
- Gemeinsame Verträge (Modelle, Interfaces, Enums) liegen in **Domain**. Gemeinsame technische Dienste in **Infrastructure**.
- UI-Navigation und Modulverdrahtung passieren **ausschließlich im App-Projekt** (App.xaml.cs + MainWindow.xaml).

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

## ADR-013: .bpm-manifest als Projektordner-Ausweis (v2)

**Datum:** 2026-03 (v2: 2026-04)
**Status:** ✅ Entschieden
**Herkunft:** Architektur-Session (v2: Projekt-Import Konzept)

**Kontext:**

v1: Wenn ein Projektordner umbenannt wird (im Explorer), verliert die App den Bezug zum Projekt. Wie erkennt die App den Ordner wieder?

v2: Zusätzlich zum Ausweis-Zweck soll das Manifest auch als portabler Projekt-Snapshot für Import und Übergabe dienen (Polier → Bauleiter, Alt-Projekt migrieren, Backup/Restore).

**Entscheidung:**

Jeder Projektordner enthält eine versteckte `.bpm-manifest`-Datei (JSON) mit **allen Projektdaten** die für einen vollständigen Import nötig sind:

- Ausweis-Daten: Projekt-ID, Projektnummer, Name, Registry-Pfad
- Stammdaten: FullName, Status, ProjectType, Tags, Notes
- Auftraggeber: Firma, Kontakt, Telefon, E-Mail
- Adresse + Koordinaten + Grundstück
- Zeitplan: Projektstart, Baubeginn, geplantes/tatsächliches Ende
- Bauteile mit Geschossen (inkl. RDOK/FBOK/RDUK)
- Beteiligte mit Rollen
- Portale + Links
- Ordnerstruktur (relative Pfade)
- Meta: SchemaVersion, UpdatedAtUtc, CreatedByMachine

Das Manifest wird automatisch geschrieben bei: Neues Projekt anlegen, Projekt bearbeiten (Speichern). Keine DB-IDs im Manifest — nur fachliche Daten. Eigene DTOs (ManifestClient, ManifestLocation etc.) statt direkte Domain-Modelle.

Import-Szenarien:
- **Aus .bpm-manifest:** Ordner wählen → Manifest lesen → alles vorausgefüllt → Bestätigen → DB-Eintrag
- **Bestehenden Ordner importieren:** Ordner wählen → Struktur scannen → Stammdaten ergänzen → DB-Eintrag + Manifest erzeugen

**Konsequenzen:**

- Robuste Pfad-Erkennung auch nach Umbenennung (wie v1)
- `.bpm-manifest` hat Hidden + ReadOnly Attribute (unsichtbar, schreibgeschützt)
- BPM entfernt ReadOnly temporär beim Aktualisieren, setzt es danach wieder
- Syncht über Cloud-Speicher (liegt im Projektordner)
- Portabler Projekt-Snapshot — unabhängig von der lokalen SQLite-DB
- Ermöglicht Projekt-Übergabe zwischen BPM-Instanzen ohne gemeinsame DB
- SchemaVersion für Vorwärtskompatibilität bei Manifest-Erweiterungen
- Domain-Klasse: BpmManifest.cs mit eigenen DTOs (keine DB-IDs)
- Infrastructure-Service: BpmManifestService (Read/Write/ScanFolder)

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

Nein. Excel bleibt die Single Source of Truth für Roh-Zeitbuchungen. WPF liefert nur eine schöne Eingabemaske (Dark Theme, Dropdowns, Kalender). Daten werden per ClosedXML direkt in die Excel-Tabelle `tbl_Zeiten` geschrieben (append-only). Excel behält alle Formeln, Power Query, Pivot und Auswertungen. Baustellen-Dropdown kommt aus `bpm.db` / `registry.json`.

**Abgrenzung zu ADR-002 (SQLite als SoR):**
- Excel = **führend** für Roh-Zeitbuchungen (wer, wann, Stunden, Abwesenheit)
- SQLite = **abgeleiteter Schatten** — darf Aggregate/Kopien für Kalkulation und Bautagebuch halten, ist aber nicht führend
- Bei Widerspruch zwischen Excel und SQLite gilt Excel

**Alternativen:**

- *Alles in SQLite:* Würde Excel-Formeln/Pivot/Power Query verlieren. Lohnbüro liest aktuell Excel direkt über OneDrive.
- *COM Interop:* Erfordert Excel auf dem Rechner. ClosedXML braucht kein Excel.

**Konsequenzen:**

- ClosedXML als NuGet-Paket, kein Excel nötig zum Schreiben
- Excel-Architektur muss vorher fertig sein (tbl_Zeiten Schema etc.)
- Kein eigenes Überstunden-Modul in C# — das macht Excel

---

## ADR-019: Mobile PWA statt Native App

**Datum:** 2026-03
**Status:** ✅ Accepted / Not Started
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

## ADR-020: Write-Lock mit Heartbeat für Shared SQLite im LAN

**Datum:** 2026-03
**Status:** ✅ Accepted / Not Started
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

**Scope:** Gilt für Modus B (geteilte SQLite im LAN-Netzlaufwerk). Wird für Cloud-basierte Szenarien durch ADR-037 (Event-basierter Sync) abgelöst. Heartbeat-Lock und Event-Versionierung koexistieren nicht — je nach Modus gilt eines.

**Betrifft:** ADR-033, ADR-037

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

Zentrales Theme-System mit 7 Resource Dictionaries im Ordner `Themes/` des App-Projekts:

- **Colors.xaml** — Alle Farb-Token als SolidColorBrush (Background, Surface, Text, Accent, Status-Farben)
- **Typography.xaml** — Schriftgrößen-Stufen (XS bis XXL, Segoe UI)
- **Buttons.xaml** — Button-Varianten (Primary, Secondary, Danger, Ghost, Nav)
- **Inputs.xaml** — TextBox, ComboBox, DatePicker, CheckBox Styles
- **DataGrid.xaml** — Header, Row, Cell, Zebra-Variante
- **Tabs.xaml** — TabControl + TabItem mit Unterstrich-Style
- **Dialogs.xaml** — Dialog-Basis, Cards, Tooltips, Separatoren

Ursprünglich 5 Dictionaries. Erweitert um Inputs.xaml und Tabs.xaml für vollständige Formular- und Tab-Abdeckung — Styles lagen vorher verstreut in Dialogs.xaml und lokalen Views.

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

**Geltungsgrenze:**
- **ADRs** definieren Prinzipien und Entscheidungen (z.B. "TEXT-IDs mit Präfix", ADR-039)
- **DB-SCHEMA.md** definiert Tabellen, Spalten, FKs, SQL und Naming-Konventionen — die operative Referenz
- **Modulkonzepte** dürfen Datenbedarf beschreiben, aber kein konkurrierendes Schema führen

---

## ADR-032: ITaskManagementService — nicht an ClickUp gebunden

**Datum:** 2026-03
**Status:** ✅ Accepted / Not Started
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
| **B: Geteilte DB** | bpm.db auf LAN-Netzlaufwerk (NICHT Cloud-Ordner). Write-Lock mit Heartbeat (ADR-020). Read-Only Fallback wenn Lock belegt. Cloud-basierte Zusammenarbeit läuft ausschließlich über Event-/Dateisync (ADR-037). | Mittel |
| **C: Server** | ASP.NET Minimal API auf einem Raspberry Pi (oder anderem Rechner) im LAN. Desktop + Mobile verbinden sich per REST API. Server besitzt die DB exklusiv — kein Sync-Konflikt. | Höher |

Technisch wird ein `IDataService` Interface eingeführt mit 3 Implementierungen: `LocalDataService` (A), `SharedDbDataService` (B), `ServerDataService` (C). Umschaltung in den Systemeinstellungen.

**Alternativen:**

- *Cloud-DB (Azure/Firebase):* Internet-Pflicht, Abo — widerspricht Offline-Prinzip.
- *CRDTs:* Automatische Merge-Konflikte — zu komplex für den Nutzen.

**Konsequenzen:**

- Modus A sofort verfügbar (ist der Status quo)
- Modus B erfordert: IDataService Refactoring + Write-Lock + Read-Only Fallback
- Modus C erfordert: ASP.NET Minimal API + REST-Endpoints + Server-Setup-Anleitung
- Kein Berechtigungsmanagement in Modus A/B (Vertrauensbasis). RBAC ab Modus C — siehe ADR-038.
- Shared SQLite ist ein optionaler LAN-Sondermodus, nicht der Standard-Evolutionspfad. Standard-Evolution: Modus A → Event-Sync (ADR-037) → Server (Modus C).
- **Einordnung Event-Sync:** ADR-037 ist kein eigener Betriebsmodus, sondern ein Synchronisationsmechanismus zwischen lokalen Instanzen auf dem Evolutionspfad von Modus A zu Modus C. Jede Instanz behält ihre eigene lokale bpm.db — es gibt keine geteilte Datenbank im Cloud-Szenario.
- Konzeptdokument: `MultiUserKonzept.md`

**Betrifft:** ADR-004, ADR-020, ADR-037, ADR-038

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

## ADR-036: IPrivacyPolicy — austauschbare Policy für Internal/Commercial

**Datum:** 2026-04
**Status:** ✅ Entschieden
**Herkunft:** DSGVO-Architektur-Review, ChatGPT-Empfehlung (Option D + Strategy Pattern)

**Kontext:**

Die DSGVO-Architektur (v1.2) definiert strikte Policy-Regeln für externe Kommunikation: Default-Block für Klasse C, DPA-Pflicht für KI, Audit-Log mit Zweckangabe. Für Herbert als einzigen Nutzer ist dieser volle Stack Overhead — er will sein eigenes LV an ChatGPT schicken können ohne Pflicht-Zweckfeld und Checkbox. Für die Verkaufsversion muss aber alles strikt eingehalten werden. Die Frage war: Wie trennt man Internal und Commercial sauber, ohne zwei Codebasen oder unsichere Runtime-Flags?

Fünf Optionen wurden evaluiert:
- Option A (Compile-Time `#if`): Zwei Binaries, divergierendes Verhalten, Bugs nur in einem Build — abgelehnt
- Option B (Runtime-Flag in settings.json): Single Point of Failure, User kann DSGVO umgehen — abgelehnt
- Option C (Feature Flags pro Regel): Overengineering, 10+ Schalter — abgelehnt
- Option D (Austauschbarer Service via DI): Sauber, wartbar, kein doppelter Code — **gewählt**
- Option E (Alles immer + UX optimieren): Philosophisch sauber, aber bremst Solo-Dev — abgelehnt

**Entscheidung:**

Strategy Pattern: Die Datenschutz-Entscheidungslogik wird als eigene Komponente (`IPrivacyPolicy`) vom `IExternalCommunicationService` getrennt. Der Service führt aus, die Policy entscheidet. Zwei Implementierungen, ein Codepfad.
```csharp
// BauProjektManager.Domain/Privacy/
public interface IPrivacyPolicy
{
    PolicyDecision Evaluate(
        string module,
        DataClassification classification,
        string purpose);
}

// BauProjektManager.Infrastructure/Communication/
public class RelaxedPrivacyPolicy : IPrivacyPolicy
{
    // Internal: alles erlaubt, loggt mit "internal_mode"
}

public class StrictPrivacyPolicy : IPrivacyPolicy
{
    // Commercial: volle DSGVO-Logik (Block, DPA-Check, User-Confirmation)
}
```

DI-Registrierung über Compliance-Modus der Lizenz (ADR-034), NICHT über settings.json:
```csharp
if (license.RequiresStrictCompliance)
    services.AddSingleton<IPrivacyPolicy, StrictPrivacyPolicy>();
else
    services.AddSingleton<IPrivacyPolicy, RelaxedPrivacyPolicy>();
```

**Begriffsdefinition:** `RequiresStrictCompliance` wird durch die signierte Lizenzdatei bestimmt und steuert ausschließlich die Auswahl der Privacy Policy. Er ist unabhängig von Modulfreischaltungen, Preisstufe und UI-Einstellungen. "Strict Compliance" = App läuft bei Dritten (Firmenkunden) die eigene DSGVO-Pflichten haben. "Relaxed" = interner Betrieb (Herbert).

**Alternativen:**

- *Compile-Time Split:* Zwei verschiedene Binaries. Bugs tauchen nur in einem Build auf. Testing-Hölle. Abgelehnt.
- *Runtime-Flag:* Ein Setting in settings.json entscheidet über Compliance. „Oops, falsches Setting" ist kein valider DSGVO-Grund. Abgelehnt.
- *Immer strikt + UX weicher:* Philosophisch korrekt, aber Herbert bremst sich selbst aus. Nicht pragmatisch für Solo-Dev.

**Konsequenzen:**

- `IPrivacyPolicy` Interface im Domain-Projekt (keine externe Abhängigkeit)
- `RelaxedPrivacyPolicy` und `StrictPrivacyPolicy` in Infrastructure
- `ExternalCommunicationService` bekommt Policy per Constructor Injection — entscheidet nicht selbst
- Beide Policies nutzen denselben Service — kein doppelter HTTP/Logging-Code
- `RelaxedPrivacyPolicy` loggt trotzdem ins Audit-Log (mit `decision_reason: "internal_mode"`)
- Compliance-Modus (`RequiresStrictCompliance`) darf NIEMALS durch User-Settings steuerbar sein
- Session-Override (optional): `IPrivacyContext.IsTrustedSession` für temporäres Abschalten von Klasse-B-Warnungen im Commercial-Modus. Klasse C bleibt IMMER blockiert
- Für V1 nicht relevant (keine Online-Module). Implementierung vor dem ersten Online-Modul zusammen mit ADR-035
- Detailliertes Konzept: [DSVGO-Architektur.md](DSVGO-Architektur.md) Kapitel 4.3

---

## ADR-037: ISyncTransport — austauschbarer Sync-Transport (Folder/HTTP)

**Datum:** 2026-04
**Status:** ✅ Accepted / Not Started
**Herkunft:** Multi-User Architektur-Diskussion (Claude + ChatGPT), Analyse von PlanRadar/Procore/Dalux

**Kontext:**

Das Multi-User-Konzept sieht zwei Sync-Phasen vor: Phase 2 (JSON-Events über Cloud-Ordner für 2–3 Nutzer) und Phase 3 (REST API Server für 5–10+ Nutzer). Um nicht zweimal die Sync-Logik zu bauen, muss der Transportkanal austauschbar sein — die Payload-Struktur und Konfliktbehandlung bleiben gleich.

**Entscheidung:**

Ein `ISyncTransport` Interface im Domain-Projekt mit zwei Implementierungen:

- `FolderSyncTransport` (Phase 2): Schreibt/liest JSON-Event-Dateien aus einem Cloud-Ordner. Append-only Events, jeder Client tracked verarbeitete Events selbst.
- `HttpSyncTransport` (Phase 3): POST/GET gegen ASP.NET Minimal API.

Beide verwenden dasselbe `SyncEnvelope`-Format mit `eventId`, `entityType`, `baseVersion`, `newVersion`, `permissionScope`.

**Konsequenzen:**

- Sync-Kernlogik (Konflikterkennung, Versionsprüfung, Event-Verarbeitung) wird einmal geschrieben
- Transport wechselt per DI — kein Code-Umbau bei Phase-Wechsel
- Phase 2 braucht keinen Server — nur einen geteilten Cloud-Ordner
- Konzeptdokument: [MultiUserKonzept.md](../Konzepte/MultiUserKonzept.md) Kapitel 5 + 7.2

---

## ADR-038: IAccessControlService — rollenbasierte Projektfreigabe

**Datum:** 2026-04
**Status:** 🟡 Konzept
**Herkunft:** Multi-User Architektur-Diskussion (Claude + ChatGPT)

**Kontext:**

Mehrere Rollen (Bauleiter, Polier, Disponent, Einkäufer, Lohnbüro) sollen am selben Projekt arbeiten, aber jeder sieht nur seinen Teil. Das bisherige Konzept (ADR-033) hatte keine Berechtigungen — nur Vertrauensbasis. Für den Verkauf und größere Teams braucht es ein echtes Berechtigungsmodell.

**Entscheidung:**

Zweistufiger Ansatz:

- **Phase 2 (einfach):** `project_shares` Tabelle mit `permission` Enum (full, read, plans_only, diary_write). Reicht für 2–3 Nutzer.
- **Phase 3 (RBAC):** `users`, `roles`, `project_user_role` Tabellen mit `module_flags` (JSON: welche Module in welcher Stufe). Reicht für 5–10 Nutzer.

`IAccessControlService` Interface in Domain, Implementierung in Infrastructure. Für V1: `NoOpAccessControlService` (alles erlaubt). Ergänzt, nicht ersetzt die bestehende `IPrivacyPolicy` — zwei unabhängige Schichten (Zugriffskontrolle + Datenschutz).

**Konsequenzen:**

- Interface in Domain (keine Abhängigkeiten), Implementierung in Infrastructure
- Berechtigungsmatrix aus DSGVO-Architektur (Kap. 10.2) wird in Code umgesetzt
- Bei Phase 2 (JSON-Sync): Berechtigungen nicht erzwingbar, nur organisatorisch (empfängerspezifische Ordner)
- Bei Phase 3 (Server): Server erzwingt Berechtigungen serverseitig
- Konzeptdokument: [MultiUserKonzept.md](../Konzepte/MultiUserKonzept.md) Kapitel 6 + 7.3

---

## ADR-039: Einheitliches ID-Schema — ULID als Primärschlüssel

**Datum:** 2026-04
**Status:** ✅ Accepted / Not Started
**Herkunft:** ID-Schema Review (Claude + ChatGPT, 4 Runden, 04.04.2026)
**Supersedes:** ADR-039 v1 (TEXT mit Präfix + seq, `MAX(seq)+1`)

**Kontext:**

Das ursprüngliche ID-Schema (v1) verwendete `seq INTEGER PRIMARY KEY AUTOINCREMENT` + `id TEXT UNIQUE NOT NULL` mit Präfix (`proj_001`, `bpart_042`). ID-Generierung über `MAX(seq)+1`. Ein 4-Runden-Review (Claude + ChatGPT) identifizierte drei fundamentale Probleme:

1. **`MAX(seq)+1` ist nicht robust** — Race Conditions, Löschungslücken, Import-Verzerrung
2. **Zwei ID-Spalten pro Tabelle** — `seq` war technisch untergenutzt (nie als FK, nie extern)
3. **Nicht sync-fähig** — Herberts reales Arbeitsmodell erfordert Floating Master, Offline-Phasen auf 2-3 Geräten, kein zentraler Server. Lokal hochgezählte IDs kollidieren bei Offline-Merge.

Alle etablierten Offline-Sync-Systeme (CouchDB/PouchDB, SQLite Sync/CRDTs, PowerSync, Turso) verwenden String-basierte global eindeutige IDs. Keines verwendet `INTEGER PRIMARY KEY` für sync-fähige Tabellen.

**Entscheidung:**

**ULID als einziger Primärschlüssel für ALLE Tabellen** — in `bpm.db` und `planmanager.db`. Keine `seq` Spalte, keine INTEGER IDs, keine Ausnahmen.
```sql
CREATE TABLE projects (
    id TEXT PRIMARY KEY,           -- ULID: "01HV8M2Q9AJ3W1XK7R4F5N6T8C"
    project_number TEXT NOT NULL DEFAULT '',
    name TEXT NOT NULL DEFAULT '',
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE building_parts (
    id TEXT PRIMARY KEY,           -- ULID
    project_id TEXT NOT NULL,      -- FK → projects.id (ULID)
    short_name TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);

CREATE INDEX idx_building_parts_project_id ON building_parts(project_id);
```

**Warum ULID statt UUID:**
- Chronologisch sortierbar (enthält Zeitstempel) — nützlich für `ORDER BY id`
- Kürzer als UUID (26 vs. 36 Zeichen)
- Offline erzeugbar ohne zentrale Koordination
- Kollisionsfrei über Gerätegrenzen

**ID-Generierung:**

Zentral über `IIdGenerator` Interface in Domain, Implementierung in Infrastructure. Nie `Ulid.NewUlid()` direkt im Code verstreut.
```csharp
// BauProjektManager.Domain
public interface IIdGenerator
{
    string NewId();
}

// BauProjektManager.Infrastructure
public sealed class UlidIdGenerator : IIdGenerator
{
    public string NewId() => Ulid.NewUlid().ToString();
}
```

**Geltungsbereich — ALLE Tabellen, keine Ausnahmen:**

| Datenbank | Tabellen | ID-Typ |
|-----------|----------|--------|
| `bpm.db` | Alle Stamm-/Projektdaten (18 Tabellen) | ULID |
| `planmanager.db` | Journal, Actions, ActionFiles | ULID |
| Jede zukünftige Tabelle | — | ULID |

**Lesbarkeit:**

ULIDs sind nicht menschenlesbar. Die Lesbarkeit wird über fachliche Felder sichergestellt:

| Entität | Lesbare Identifikation | Beispiel |
|---------|----------------------|---------|
| Projekt | `project_number + name` | „202406 ÖWG-Dobl" |
| Bauteil | `short_name + description` | „H5 — Haus Nr. 5" |
| Geschoss | `name` | „EG", „1.OG" |
| Beteiligter | `role + company` | „Statiker — Müller ZT GmbH" |
| Arbeitspaket | `activity + building + level` | „Mauerwerk 38er / H5 / EG" |
| In Logs | Fachlicher Kontext + ULID-Suffix | „Projekt ÖWG-Dobl (01HV…)" |

Keine generische `display_number` Spalte auf jeder Tabelle — das wäre Overengineering.

**VBA-Kompatibilität:**

VBA liest `registry.json` → sieht nur Strings. Egal ob `"proj_42"` oder `"01HV8M2Q9AJ3W1XK7R4F5N6T8C"` — VBA parst beide als String. Kein Einfluss auf VBA-Makros.

**Zusätzliche Pflichtfelder auf sync-relevanten Tabellen:**

- `created_at TEXT NOT NULL` — Erstellungszeitpunkt
- `updated_at TEXT NOT NULL` — Letzter Änderungszeitpunkt
- `origin_device_id TEXT` (optional) — Für spätere Konfliktanalyse/Debugging

**Alternativen (evaluiert im Review):**

- *INTEGER PRIMARY KEY:* SQLite-nativ, performant, lesbar. Aber: lokal generiert, kollidiert bei Offline-Merge. Späteres Nachrüsten von `sync_id` ist schmerzhafter Umbau.
- *INTEGER + spätere sync_id TEXT:* Pragmatisch für Single-User, aber bewusst eingebauter späterer Umbau. Review-Ergebnis: „nicht tun wenn Multi-User bald kommt."
- *TEXT mit Präfix (proj_001):* Lesbar, aber `MAX(seq)+1` fragil, nicht global eindeutig, zwei Spalten (seq+id) Overhead.
- *UUID/GUID:* Global eindeutig, aber 36 Zeichen, nicht sortierbar.

**Konsequenzen:**

- Refactoring: 8 bestehende Tabellen von `seq + id TEXT` auf `id TEXT PRIMARY KEY` (ULID) umbauen. Wenige Testdaten, jetzt noch einfach.
- NuGet-Dependency: ULID-Library (z.B. `Cysharp/Ulid` oder `NUlid`)
- DB-SCHEMA.md komplett aktualisieren — alle Tabellen, FK-Typen, Naming-Konventionen
- `GenerateNextId()` Methode in ProjectDatabase.cs entfällt — wird durch `IIdGenerator.NewId()` ersetzt
- `RegistryJsonExporter`: ID kommt direkt aus DB (ist bereits ein String)
- Multi-User Event-Sync (ADR-037) wird erheblich vereinfacht — keine ID-Mapping-Schicht nötig

**Hierarchie:** Diese ADR dokumentiert die Entscheidung (Prinzip). Die operativen Details (Tabellen-Definitionen, FK-Regeln, Indizes) leben in DB-SCHEMA.md (ADR-031).

## ADR-040: Migrations- und Versionierungsstrategie (DB + JSON)

**Datum:** 2026-04
**Status:** ✅ Accepted / Not Started
**Herkunft:** Kern-Dokumenten-Review + ADR-Review (Claude + ChatGPT)

**Kontext:**

BPM hat mehrere persistente Datenquellen die sich über die Zeit strukturell ändern: SQLite-Datenbanken (`bpm.db`, `planmanager.db`) und JSON-Konfigurationsdateien (`settings.json`, `profiles.json`, `pattern-templates.json`, `registry.json`). Ohne definierte Migrationsstrategie drohen inkonsistente Zustände bei App-Updates — besonders kritisch bei einer Offline-Desktop-App ohne zentrale Updatekontrolle.

**Entscheidung:**

Automatische Forward-Only-Migration bei App-Start mit Backup.

**DB-Migration (SQLite):**
- Schema-Version wird bei App-Start geprüft (`schema_version` Tabelle)
- Bei älterer Version: automatische Migration (ALTER TABLE, CREATE TABLE IF NOT EXISTS)
- Vor jeder Migration: `bpm.db` → `bpm.db.bak` kopieren
- **Forward-Only:** Kein automatischer Rollback. Bei Fehler: Migration abbrechen, Backup wiederherstellen, User informieren
- Harte Abbruchbedingung: Wenn DB-Version **neuer** als App-Version → App startet nicht (Schutz vor Downgrade-Schäden)

**JSON-Migration:**
- Jede JSON-Datei hat ein `schemaVersion` Feld (oder `registryVersion` bei registry.json)
- Bei fehlenden Feldern: Default-Werte ergänzen (rückwärtskompatibel)
- Bei unbekannten Feldern: ignorieren (vorwärtskompatibel)
- Bei korruptem JSON: Datei umbenennen (.corrupt), Defaults neu erstellen, User informieren
- `registry.json` wird komplett aus SQLite neu generiert — keine Migration nötig (ADR-002)

**Alternativen:**

- *Kein automatisches Migrationssystem:* Einfacher, aber App bricht bei Schema-Änderungen. Nicht tragbar für Offline-App.
- *EF Core Migrations:* Zu schwer für SQLite + Solo-Projekt. Manuelles SQL reicht.
- *Rollback-fähige Migration:* Deutlich komplexer. Forward-Only + Backup ist pragmatischer.

**Konsequenzen:**

- Migration-Code in `ProjectDatabase.cs` (Infrastructure), aufgerufen bei App-Start
- Backup-Verzeichnis: `%LocalAppData%\BauProjektManager\Backups\pre-migration\`
- JSON-Migration in `AppSettingsService.cs`
- Logging: Jede Migration wird geloggt (Version alt → neu, Dauer, Erfolg/Fehler)

**Betrifft:** ADR-002, ADR-004, ADR-016, ADR-031, ADR-039

---

## ADR-041: Recovery / Degraded Mode

**Datum:** 2026-04
**Status:** ✅ Accepted / Not Started
**Herkunft:** Kern-Dokumenten-Review + ADR-Review (Claude + ChatGPT)

**Kontext:**

BPM ist eine Offline-Desktop-App mit lokaler Persistenz. Dateien können korrupt werden (Stromausfall, Cloud-Sync-Fehler, manuelle Manipulation). Ohne definierte Recovery-Strategie startet die App bei Problemen einfach nicht — der User steht ohne Fehlermeldung da.

**Entscheidung:**

Dreistufiges Zustandsmodell bei App-Start:

| Zustand | Bedingung | Verhalten |
|---------|-----------|-----------|
| **Normal** | Alle Dateien lesbar, Schema aktuell | Normaler Start |
| **Eingeschränkt** | settings.json fehlt/korrupt ODER Cloud-Dateien fehlen | Start mit Defaults, Hinweis-Banner, Einstellungen öffnen |
| **Blockiert** | bpm.db korrupt ODER Schema-Version neuer als App | Kein Start. Reparaturdialog: Backup wiederherstellen oder DB zurücksetzen |

**Recovery-Aktionen pro Dateityp:**

| Datei | Problem | Aktion |
|-------|---------|--------|
| `settings.json` | Fehlt oder korrupt | Defaults erstellen, User informieren |
| `profiles.json` | Fehlt | Leeres Profil, PlanManager fordert Neuanlernen |
| `registry.json` | Fehlt oder korrupt | Aus SQLite neu generieren (ADR-002) |
| `bpm.db` | Korrupt | Reparaturdialog: Backup anbieten, ggf. leere DB erstellen |
| `bpm.db` | Zukunfts-Schema | App-Start blockieren, Update-Hinweis |
| `planmanager.db` | Korrupt | Cache-Rebuild aus Dateisystem anbieten (ADR-004) |
| Cloud-Dateien | Nicht erreichbar | Weiterarbeiten mit lokalen Daten, Sync-Warnung |

**Alternativen:**

- *Kein Recovery:* App crasht bei Problemen. Nicht akzeptabel für Baustellen-Einsatz.
- *Vollautomatische Reparatur:* Riskant — könnte Daten ungewollt überschreiben. User-Bestätigung bei destruktiven Aktionen ist sicherer.

**Konsequenzen:**

- `StartupHealthCheck` Service in Infrastructure, aufgerufen in App.xaml.cs vor MainWindow
- Prüfreihenfolge: bpm.db → settings.json → Cloud-Pfade → planmanager.db (pro Projekt)
- Reparaturdialog als eigenes WPF-Fenster (nicht MainWindow-abhängig)
- Logging: Jeder Recovery-Versuch wird geloggt

**Betrifft:** ADR-002, ADR-004, ADR-040

---

## ADR-042: Secrets und Credentials — zentrale Sicherheitsentscheidung

**Datum:** 2026-04
**Status:** ✅ Accepted / Not Started
**Herkunft:** Kern-Dokumenten-Review + ADR-Review (Claude + ChatGPT)

**Kontext:**

BPM verwaltet mehrere Arten sensibler Daten: API-Keys für externe Dienste (OpenAI, Google Maps, ClickUp), Lizenzsignaturen (HMAC-SHA256 shared secret), und potenziell lokale Verschlüsselungsschlüssel (SQLCipher). Bisher sind die Entscheidungen dazu über mehrere ADRs verstreut (ADR-027: Windows Credential Manager, ADR-034: HMAC-Signatur, ADR-035/036: DPAPI). Es fehlt eine zentrale Sicherheitsentscheidung.

**Entscheidung:**

Alle Secrets werden über DPAPI (Windows Data Protection API) geschützt. Kein Klartext, nirgends.

**Speicherorte:**

| Secret-Typ | Speicher | Mechanismus |
|------------|----------|-------------|
| API-Keys (OpenAI, Google, ClickUp etc.) | `%LocalAppData%\BauProjektManager\` | DPAPI (`ProtectedData.Protect`, Scope: CurrentUser) |
| Lizenz-Signatur-Secret | Im Build eingebettet (embedded resource) | HMAC-SHA256 Verifikation, kein User-Zugriff |
| SQLCipher-Key (Zukunft) | Aus DPAPI abgeleitet | An Windows-User + Maschine gebunden |

**Verbote (absolut):**

- ❌ Secrets in `settings.json`, `registry.json`, `.bpm-manifest` oder anderem JSON
- ❌ Secrets in Git (Quellcode, .csproj, Ressourcen-Dateien)
- ❌ Secrets in Serilog-Logs (auch nicht maskiert)
- ❌ Secrets in `external_call_log` (Audit-Log)
- ❌ Secrets in Cloud-synced Ordnern
- ❌ Hardcoded Secrets im Quellcode (einzige Ausnahme: embedded HMAC-Secret für Lizenzverifikation — siehe Sicherheitshinweis unten)

**Sicherheitshinweis Lizenz-Secret:**
Die Offline-Lizenzprüfung per eingebettetem HMAC-Secret ist **manipulationserschwerend, nicht manipulationssicher**. Jeder der die App ernsthaft analysiert kann ein embedded Secret prinzipiell extrahieren. Ziel ist eine einfache Offline-Freischaltung und Hürde gegen triviale Manipulation, kein kryptographisch starker Kopierschutz. Falls härterer Schutz nötig wird (z.B. bei hohem Piraterie-Risiko), braucht es ein anderes Modell (Online-Aktivierung, Hardware-Binding, Dongle).

**Backup/Export:**

- API-Keys sind **nicht exportierbar** — bei Gerätewechsel muss der User Keys neu eingeben
- Lizenzdateien (`.bpm-license`) sind portabel — können auf neues Gerät kopiert werden
- Kein automatischer Secret-Sync zwischen Geräten

**Alternativen:**

- *Windows Credential Manager:* Ähnlich wie DPAPI, aber UI-basiert. DPAPI ist programmatisch einfacher und reicht für Desktop-App.
- *Azure Key Vault:* Cloud-basiert — widerspricht Offline-Prinzip.
- *Eigene Verschlüsselung (AES etc.):* Wo ist dann der Schlüssel für den Schlüssel? DPAPI löst das über Windows.

**Konsequenzen:**

- `SecretStore` Service in Infrastructure mit `Store(key, value)` / `Retrieve(key)` Methoden
- DPAPI bindet an Windows-User + Maschine — bei Benutzerwechsel/Neuinstallation gehen Secrets verloren (akzeptabel)
- Einstellungs-UI zeigt API-Keys als `••••••••` mit "Ändern"-Button, nie im Klartext
- Coding Standard (CODING_STANDARDS Kap. 17.4): DPAPI Pflicht, Klartext verboten

**Betrifft:** ADR-027, ADR-032, ADR-034, ADR-035, ADR-036

---

## ADR-043: Dev-Tools — Lokales Debug-Toolset für Entwicklung

**Datum:** 2026-04
**Status:** ✅ Entschieden / Not Started
**Herkunft:** ChatGPT + Claude Review-Gespräch (4 Runden, 05.04.2026)

**Kontext:**

Als Einzelentwickler ohne CI/CD braucht Herbert einen einfachen Weg, um während der Entwicklung Testdaten zurückzusetzen und Logs einzusehen — ohne Explorer, ohne Terminal, ohne externes Tool. Konkrete Anforderungen: DB löschen für sauberen Neustart (z.B. nach Schema-Änderungen), Log schnell lesen, nur für Entwicklung sichtbar.

Drei Optionen wurden evaluiert:
- **Option A** (verstecktes `#if DEBUG`-Menü in der App) — gewählt
- **Option B** (separates `DevTools.exe` Projekt) — abgelehnt
- **Option C** (PowerShell-Skripte) — nur als persönlicher Komfort, nicht als Projektmechanismus

**Entscheidung:**

Option A: Ein Dev-Dialog im `#if DEBUG`-Block der WPF-Shell. Kein zweites Binary, kein separates Projekt.

**DB-Reset-Mechanismus:**

Button → App schließt → gehärteter Batch löscht DB → App startet neu. Der Löschvorgang findet zwischen den zwei Prozessen statt (alle Handles freigegeben). Kein Pending-Reset-Marker nötig.

Der Batch-Prozess (GUID-Name, Self-Delete) übernimmt:
1. Wartet auf Prozessende (max. 30 Sekunden, Retry-Zähler)
2. Löscht `bpm.db`, `bpm.db-wal`, `bpm.db-shm`
3. Prüft alle drei Dateien auf Existenz (Retry-Loop, max. 30 Versuche)
4. Startet App neu
5. Bei Fehlschlag: `%TEMP%\bpm_reset_failed.txt` mit Zeitstempel
6. Self-Delete des Batch-Scripts

**Dev-Dialog Funktionen:**
- Betroffene Dateipfade anzeigen vor dem Reset
- Button „Lokale Datenbank löschen und neu starten" (explizite Benennung)
- Log-Verzeichnis im Explorer öffnen
- Letzte 200 Zeilen der aktuellen Log-Datei inline anzeigen

**Alternativen (abgelehnt):**

- *Separates `DevTools.exe`-Projekt:* Zweite `.csproj`, Build-Abhängigkeit, Deploy-Abhängigkeit, muss synchron mit Hauptprojekt gehalten werden — zu viel Overhead für ein Dev-only Feature.
- *Pending-Reset (Marker-File beim nächsten Start):* Sauber, aber unnötig wenn der Batch-Ansatz denselben Effekt einfacher erreicht.
- *Direktes Löschen aus laufendem Prozess:* SQLite WAL/SHM + offene Handles → inkonsistenter State. Verboten.
- *PowerShell als Primärmechanismus:* Nicht GUI-only, ExecutionPolicy-Risiko, mehr Kontextwechsel.

**Konsequenzen:**

- `IDeveloperToolsService` in Domain, `DeveloperToolsService` in Infrastructure/Dev
- Dev-Menüpunkt und Dialog nur in `#if DEBUG`-Blöcken — nie in Release sichtbar
- Batch-Encoding: CP850 für korrekte Sonderzeichen in Windows-Pfaden
- `Directory.CreateDirectory()` im `ProjectDatabase`-Konstruktor stellt sicher, dass das DB-Verzeichnis nach Reset beim nächsten Start neu angelegt wird
- Settings/JSON-Reset: separater Button, kommt als späterer Schritt
- Seed-/Testdaten-Mechanismus: zukünftiger Schritt, fachlich getrennt vom technischen DB-Reset

---

## ADR-044: Icons.xaml — Zentrale Icon-Registry

**Datum:** 2026-04
**Status:** ✅ Entschieden / Implemented
**Herkunft:** Phase 1 Teil 8, UI-Refactoring

**Kontext:**

BPM verwendete 14 verschiedene Emoji-Zeichen (📂, 📁, 🗑, ✎, ▲, ▼, ⚙, 🛠, 📋, 📄, 🔍, 👤, ✏, 📝) hardcoded in 10 XAML- und C#-Dateien — insgesamt ~40 Stellen. Laut UI_UX_Guidelines (Kap. 8.7) sind Emojis provisorisch und werden bei einem UI-Refresh durch Segoe Fluent Icons ersetzt. Ohne zentrale Verwaltung hätte dieser Umstieg ein aufwändiges Suchen/Ersetzen über die gesamte Codebasis erfordert.

**Entscheidung:**

Neue `Icons.xaml` als 8. ResourceDictionary in `Themes/`. Alle Icons als `sys:String`-Resources mit einheitlicher Namenskonvention `Icon[Kategorie][Objekt/Aktion]`.

Drei Nutzungsmuster:

```xml
<!-- 1. Reine Icon-Buttons (Content ist nur das Icon) -->
<Button Content="{StaticResource IconActionBrowse}"/>

<!-- 2. Icon + Text in Buttons/Headers/Labels -->
<TextBlock>
    <Run Text="{StaticResource IconFolderOpen}"/>
    <Run Text=" Ordner öffnen"/>
</TextBlock>

<!-- 3. C#-Code (wo kein StaticResource möglich) -->
var icon = (string)Application.Current.FindResource("IconStatusWarning");
```

18 Icon-Definitionen in 5 Kategorien: Navigation (3), Ordner/Dateien (3), Aktionen (9), Richtung (2), Status/Personen (3).

Beim späteren Umstieg auf Segoe Fluent Icons: Nur Icons.xaml anpassen (Emoji → Glyph-Codes) und ggf. `FontFamily` am Icon-`Run` setzen. Alle 40 Referenzen bleiben unverändert.

**Alternativen:**

- *Weiter hardcoded:* Funktioniert, aber Umstieg auf Fluent Icons erfordert Änderungen in 10+ Dateien.
- *Geometry/Path-Icons:* Skaliert perfekt, aber aufwändiger zu pflegen und für Emoji-Phase unnötig.
- *Icon-Font direkt:* Segoe Fluent Icons sofort einsetzen. Möglich, aber Emojis reichen für V1 und sind universell lesbar.

**Konsequenzen:**

- `Icons.xaml` in App.xaml zwischen Colors.xaml und Typography.xaml geladen (keine Abhängigkeiten)
- ADR-028 auf 8 ResourceDictionaries nachgezogen
- C#-Code mit Emojis (z.B. DevToolsDialog Reset-Labels) nutzt `const string` als Brücke
- Neue UI-Elemente MÜSSEN Icons aus Icons.xaml referenzieren — hardcoded Emojis sind ab sofort verboten
- Bei Bedarf: Icon-`Run` kann einen eigenen Style bekommen (`BpmIconRun` mit FontFamily) für den Fluent-Umstieg

---

## ADR-045: IndexSource — Dreistufiges Modell für Plan-Index-Erkennung

**Datum:** 2026-04
**Status:** ✅ Entschieden
**Herkunft:** PlanManager Konzeptphase (Claude + Herbert, 09.04.2026)

**Kontext:**

Beim Einsortieren von Plänen muss der PlanManager den aktuellen Index (Revision) eines Plans kennen — um zu entscheiden ob ein Plan neu ist, ob er einen älteren Index ersetzt (Archivierung), oder ob er identisch ist (überspringen). In der Praxis gibt es drei Szenarien:

1. **Index im Dateinamen:** z.B. `S-103-D_TG Wände.pdf` — Index „D" ist direkt parsbar aus dem Segment. Standard bei vielen Projekten.
2. **Kein Index erkennbar:** z.B. `S-103_TG Wände.pdf` — kein Index im Dateinamen, auch nicht aus dem PDF extrahierbar (V1). Dateiname bei neuer Version identisch. Nur MD5-Hash zeigt ob sich was geändert hat.
3. **Index im Plankopf (Revisionstabelle):** z.B. ÖWG-Projekt — Polierpläne haben keinen Index im Dateinamen, aber der Plankopf im PDF enthält die Revisionstabelle mit Index „D". Der Polier will den echten Index sehen und kontrollieren.

Ohne explizite Konfiguration müsste der PlanManager raten — das widerspricht dem Prinzip „keine Annahmen treffen".

**Entscheidung:**

Pro Projekt und Plantyp (= im RecognitionProfile) wird ein `IndexSource` Feld gespeichert. Drei Werte:

| Wert | Verhalten | Archivierung | Wann |
|------|-----------|-------------|------|
| `FileName` | Index aus Dateinamen-Segment geparst (Pflichtfeld `planIndex` im Profil) | Alte Indizes → `_Archiv/` nach Buchstabe | Standard, wenn Index im Dateinamen steht |
| `None` | Kein Index vorhanden. MD5-Hash-Vergleich bei gleichem Dateinamen | Bei geändertem Hash → alte Datei ins `_Archiv/` mit Timestamp-Suffix (z.B. `_2026-04-09`) | Wenn weder Dateiname noch PDF den Index liefert |
| `PlanHeader` | Index wird aus dem Plankopf im PDF gelesen (PdfPig oder KI-API) | Wie `FileName` — Index ist bekannt, Archivierung nach Buchstabe | Post-V1 (Modul Plankopf-Extraktion) |

**V1-Scope:** `FileName` und `None` werden implementiert. `PlanHeader` ist als Enum-Wert vorhanden, aber die Implementierung kommt mit dem Plankopf-Extraktions-Modul (siehe `Docs/Konzepte/Moduleplanheader.md`).

**UI im Profil-Wizard:** Nach der Segment-Zuweisung (Schritt 1) zeigt der Wizard eine Frage: „Hat dieser Plantyp einen Index im Dateinamen?" — Toggle Ja/Nein. Bei „Ja" → `FileName`, Segment `planIndex` muss zugewiesen sein. Bei „Nein" → `None`. Später kommt eine dritte Option „Index aus Plankopf lesen" hinzu.

**Import-Vorschau bei `None`:**
- Gleicher Dateiname + gleicher MD5 → Status „Identisch (übersprungen)"
- Gleicher Dateiname + anderer MD5 → Status „Geändert — alte Version wird archiviert"
- Neuer Dateiname → Status „Neu"

**Import-Vorschau bei `PlanHeader` (Post-V1):**
- PlanManager liest den Index aus dem PDF und zeigt ihn in der Vorschau-Tabelle
- User sieht den echten Index **bevor** der Import ausgeführt wird
- Volle Kontrolle und Sicherheit

**Alternativen:**

- *Nur FileName-basiert:* Projekte ohne Index im Dateinamen (wie ÖWG Polierpläne) können nicht sauber verarbeitet werden.
- *Immer MD5-Vergleich:* Funktioniert technisch, aber der User hat keine Kontrolle über den Index. Kein Wissen welcher Index aktuell ist.
- *Automatische Erkennung:* System rät ob Index vorhanden ist — fragil, fehleranfällig, widerspricht Projektprinzipien.

**Konsequenzen:**

- `IndexSource` Enum in Domain: `FileName`, `None`, `PlanHeader`
- Feld `indexSource` im RecognitionProfile (profiles.json)
- Import-Workflow Schritt 4 (Classify) berücksichtigt IndexSource für Versionierungs-Logik
- Bei `None`: MD5-Hash wird in `import_action_files.md5_hash` gespeichert (Schema steht schon, ADR-009)
- Bei `PlanHeader`: Abhängigkeit zu PdfPig oder KI-API (ADR-027) — erst Post-V1
- Profil-Wizard bekommt zusätzlichen Schritt/Toggle nach Segment-Zuweisung
- BACKLOG.md: Feature #20 (Plantyp-Erkennung) und #22 (profiles.json) müssen IndexSource berücksichtigen

**Betrifft:** ADR-008, ADR-009, ADR-010, ADR-022, ADR-027

---

## ADR-046: .bpm/ Ordner — Manifest-Split und Profilablage im Projektordner

**Datum:** 2026-04
**Status:** ✅ Entschieden
**Herkunft:** PlanManager-Entwicklung (Claude + Herbert, 10.04.2026), ChatGPT-Review Empfehlung Settings-Split
**Supersedes:** ADR-013 v2 (einzelne `.bpm-manifest`-Datei)

**Kontext:**

Die bisherige `.bpm-manifest`-Datei (ADR-013 v2) vereint zwei Aufgaben in einer Datei: Ordner-Wiedererkennung ("Ausweis") und vollständiger Projektexport (alle 5 Tabs aus ProjectEditDialog). Mit dem PlanManager kommen weitere projektbezogene Daten hinzu: Plantyp-Profile (`profiles.json`) und später ein Bestandsmanifest (`plan-index.json`). Diese gehören zum Projektordner (synct über Cloud-Speicher), nicht in `.AppData/`. Die bisherige Lösung — alles in einer Datei — skaliert nicht.

Zusätzlich hat ChatGPT im Review empfohlen, `AppSettings.cs` aufzuspalten (maschinenlokal vs. fachlich/global). Das wird NACH PlanManager V1 umgesetzt, aber die `.bpm/`-Struktur ist architektonisch vorbereitet dafür.

**Entscheidung:**

Die einzelne `.bpm-manifest`-Datei wird durch einen **versteckten `.bpm/` Ordner** pro Projektordner ersetzt:

```
Projektordner/
├── .bpm/                          ← Versteckter Ordner (Hidden)
│   ├── manifest.json              ← Schlank: Identität + Module-Flags
│   ├── project.json               ← Vollständiger Projektexport (wie bisherige .bpm-manifest)
│   ├── profiles/                  ← PlanManager: eine JSON-Datei pro Profil
│   │   ├── <profilname>.json
│   │   └── ...
│   └── plan-index.json            ← PlanManager: Bestandsmanifest (später)
├── 01 Planunterlagen/
└── ...
```

**manifest.json (schlank):**
```json
{
  "schemaVersion": 2,
  "projectId": "01HV8M2Q9AJ3W1XK7R4F5N6T8C",
  "projectNumber": "202512",
  "name": "ÖWG-Dobl-Zwaring",
  "updatedAtUtc": "2026-04-10T14:30:00Z",
  "createdByMachine": "Desktop_PC",
  "modules": {
    "planManager": true,
    "foto": false,
    "bautagebuch": false
  }
}
```

**project.json (Vollexport):**
Enthält dieselben Daten wie die bisherige `.bpm-manifest` (Stammdaten, Client, Location, Timeline, BuildingParts, Participants, Links, Paths, Tags, Notes). Keine DB-IDs, eigene DTOs. Wird bei jedem Speichern aktualisiert.

**profiles/ (PlanManager-Profile):**
Pro Plantyp-Profil eine eigene JSON-Datei. Synct über Cloud-Speicher zwischen Geräten. Bisher in `.AppData/Projects/<P>/profiles.json` — wandert jetzt in den Projektordner, weil Profile zum Projekt gehören und zwischen Geräten verfügbar sein müssen.

**Code-Impact:**

| Datei | Änderung |
|-------|----------|
| `BpmManifestService.cs` | Aufsplitten in `ManifestService` (schlank, `.bpm/manifest.json`) + `ProjectExportService` (Vollabbild, `.bpm/project.json`) |
| `BpmManifest.cs` (Domain) | Neues schlankes `ManifestV2` Modell (nur ID, Nummer, Name, Machine, Modules) |
| `SettingsViewModel.cs` | 4 Stellen anpassen (AddProject, EditProject, ImportFromManifest, ImportFromFolder) — schreibt jetzt in `.bpm/` statt `.bpm-manifest` |
| Neuer `ProfileManager.cs` | PlanManager/Services/ — liest/schreibt `.bpm/profiles/<name>.json` |
| `.bpm-manifest` (alt) | Migration: beim ersten Zugriff alte Datei lesen → `.bpm/`-Ordner erstellen → alte Datei löschen |

**Ordner-Attribute:**
- `.bpm/` Ordner: Hidden-Attribut (wie bisherige `.bpm-manifest`)
- Dateien innerhalb: Normal (kein ReadOnly — war bei der einzelnen Datei nötig, beim Ordner reicht Hidden)
- Ordner wird bei Projekt-Erstellung automatisch angelegt

**Vorwärtsmigration:**
Beim Öffnen eines Projekts prüft die App:
1. Existiert `.bpm/manifest.json`? → Neues Format, normal weiter
2. Existiert `.bpm-manifest` (alt)? → Migration: Alte Datei lesen, `.bpm/`-Ordner erstellen, `manifest.json` + `project.json` schreiben, alte Datei löschen
3. Existiert keines? → Leerer Ordner (Import-Szenario)

**Alternativen:**

- *Einzelne Datei beibehalten + Profile in .AppData:* Funktioniert, aber Profile synchen nicht über Cloud-Speicher zum zweiten Gerät. Herbert sortiert Pläne auf beiden Geräten (ADR-004).
- *Alles in einer großen Manifest-Datei:* Wird immer größer — Profile, Plan-Index, Module-Flags. Nicht wartbar.
- *Profile in SQLite:* Lokal, syncht nicht. Erfordert separaten Export/Import-Mechanismus.

**Konsequenzen:**

- Projektordner hat jetzt einen versteckten Unterordner statt einer versteckten Datei — für Kollegen/Partner weiterhin unsichtbar
- Profile synchen automatisch über Cloud-Speicher (wie bisher `settings.json`)
- `ManifestService` bleibt schlank — nur Ausweis-Funktion, schnell zu lesen
- `ProjectExportService` schreibt den Vollexport — nur bei Speichern, nicht bei jedem Zugriff
- PlanManager `ProfileManager` liest/schreibt direkt in `.bpm/profiles/` — kein Umweg über `.AppData`
- Migration von altem Format ist einmalig und automatisch
- Spätere Module (Foto, Bautagebuch) können eigene Dateien in `.bpm/` ablegen

**Betrifft:** ADR-004, ADR-010, ADR-013, ADR-039, ADR-040

---

## ADR-047: Datenarchitektur + Sync — State-based lokal, change-based sync

**Datum:** 2026-04
**Status:** ✅ Entschieden
**Herkunft:** Claude + ChatGPT Cross-Review (4 Runden, 10.04.2026)
**Konzeptdokument:** [DatenarchitekturSync.md](../Konzepte/DatenarchitekturSync.md)

**Kontext:**

BPM wird nicht nur Solo-Betrieb sein — Multi-User mit 10+ Nutzern (Bauleiter, Poliere, Disponent, Einkäufer, Lohnbüro) ist realistisches Zielszenario. Die bisherige Architektur (SQLite lokal, JSON-Events über Cloud-Ordner) musste grundsätzlich durchdacht werden: Welche Daten syncen, welche bleiben lokal, wie funktioniert Konfliktbehandlung, was muss jetzt schon vorbereitet werden?

**Entscheidung:**

1. **State-based lokal, change-based zwischen Clients.** SQLite ist einzige lokale Wahrheit. Events sind Replikationsmechanismus, nicht Source of Truth. Kein Full Event Sourcing.

2. **4-Klassen-Datenmodell:**
   - A: Local-only (Logs, Undo, Caches, Device-Settings)
   - B: Shared domain (Projekte, Bauteile, Bautagebuch, Arbeitseinteilung)
   - C: Shared reference (ProjectTypes, BuildingTypes, FolderTemplate)
   - D: Restricted (Lohnsätze, Einheitspreise — erst mit Server Phase 3)

3. **Sensitive Daten in eigenen Tabellen** (nicht als Spalten-Flags). `employees` + `employee_compensation`, `lv_positions` + `lv_pricing` etc.

4. **Outbox/Inbox Pattern:** Domain-Mutation → change_log → sync_outbox in einer Transaktion. Separater Exporter schreibt Events. Separater Importer liest Events.

5. **Snapshots + Events:** Initial-Sync über modulare Snapshots (root-snapshot.json + diary/work/plans.snapshot.json). Danach nur Delta-Events. Snapshot-Trigger: 100 Events ODER 7 Tage.

6. **Volle Sync-Metadaten** auf allen Shared-Tabellen (12 Spalten inkl. entity_version, is_deleted, origin_device_id, last_change_id).

7. **User-Modell jetzt** (users + user_devices + roles + project_memberships). Stabile Identitäten für Audit/Sync/Berechtigungen.

8. **settings.json Split jetzt:** device-settings.json (lokal) + shared-config.json (Cloud).

9. **Phase 2 ist bewusst temporäre Übergangsarchitektur** für kleine Teams (2-3 User) mit projektbasierter Sichtbarkeit. Keine Sicherheits- oder Skalierungsarchitektur. Exit-Kriterien zu Phase 3 definiert.

10. **Phase 3: PostgreSQL serverseitig**, SQLite clientseitig. Gleiches Fachmodell, anderer Betriebsmodus.

11. **Aggregate-Design reduziert Konflikte:** Bautagebuch aufgeteilt in diary_days + diary_notes (mehrere Poliere können gleichzeitig schreiben).

**Alternativen (evaluiert):**

- *Full Event Sourcing:* Events als Wahrheit, Tabellen nur Views. Zu komplex für Solo-Entwickler (Rebuild, Schema-Evolution, Debugging). Abgelehnt.
- *CRDTs:* Automatischer Merge ohne Konflikte. Für strukturierte Daten (Projekte, Bauteile) nicht passend. Abgelehnt.
- *Last-Write-Wins global:* Einfach, aber Datenverlust bei gleichzeitigen Änderungen. Nur für Soft-Deletes auf bereits gelöschte Datensätze akzeptabel.
- *Shared SQLite über Cloud-Speicher:* SQLite + OneDrive-Sync = korrupte DB. Offiziell von SQLite abgeraten. Abgelehnt.

**Konsequenzen:**

- 12 neue Tabellen in bpm.db (users, user_devices, roles, user_roles, project_memberships, change_log, sync_outbox, sync_applied_events, sync_conflicts, diary_days, diary_notes, employee_compensation + weitere)
- Alle bestehenden Shared-Tabellen bekommen 12 Sync-Metadaten-Spalten (Migration v2.0)
- settings.json wird aufgespalten (Breaking Change für bestehende Installationen)
- Transaktionale Mutation Boundary (IChangeTrackedDb + ChangeContext) wird zentrale Infrastruktur
- FolderSyncTransport als Phase-2-Übergang, HttpSyncTransport als Phase-3-Ziel
- Code-Umbau-Reihenfolge: 12 Schritte definiert (siehe DatenarchitekturSync.md Kap. 11)

**Betrifft:** ADR-002, ADR-004, ADR-033, ADR-037, ADR-038, ADR-039, ADR-040, ADR-046

---

## ADR-048: Ansichtsprofile als UI-Sichtschicht über Modul-Aktivierung

**Datum:** 11.04.2026
**Status:** Accepted
**Implementierung:** Not Started (Post-V1)
**Betrifft:** Shell-Navigation, Sidebar, Modul-Aktivierung, Settings

**Kontext:**

Verschiedene Nutzerrollen (Polier, Bauleiter, Disponent, Lohnverrechnung) benötigen unterschiedliche Module. Statt alle Module zu zeigen und den User manuell filtern zu lassen, sollen vordefinierte Ansichtsprofile eine rollennahe Sidebar bieten. Gleichzeitig darf kein Berechtigungssystem entstehen, das mit zukünftigem RBAC (ADR-038) kollidiert.

**Entscheidung:**

1. **Ansichtsprofile (ViewProfiles) sind reine UI-Sichtprofile.** Sie steuern ausschließlich die Sidebar-Sichtbarkeit. Keine Berechtigungen, keine Lese-/Schreibrechte, kein Access Control.

2. **Schichtung der Modul-Sichtbarkeit:**
   - Lizenz / Verfügbarkeit → welche Module technisch freigeschaltet sind
   - Ansichtsprofil → welche Module standardmäßig sichtbar sein sollen
   - Benutzer-Override → manuelle Ein-/Ausblendung
   - Kernmodule → immer sichtbar (Einstellungen)

3. **Effektive Sichtbarkeit wird zentral aufgelöst** über einen `IModuleVisibilityResolver`-Service. Die Shell rendert nur das Ergebnis, enthält keine Sichtbarkeitslogik.

4. **Built-in-Profile sind schreibgeschützt** und werden zentral im Code definiert (`IModuleProfileCatalog`). Benutzer können sie duplizieren und als eigene Profile anpassen. App-Updates können Standardprofile aktualisieren ohne User-Anpassungen zu zerstören.

5. **Begriffstrennung:** „Ansichtsprofil" / `ViewProfile` für UI-Sichtbarkeit. „Rolle" bleibt zukünftigen Zugriffskonzepten vorbehalten.

6. **Fallback:** Ungültige oder fehlende Profile fallen auf „alle lizenzierten Module + Kernmodule" zurück. Eine leere Sidebar darf nie entstehen.

**Alternativen (evaluiert):**

- *activeModules als einzige Wahrheit:* Vermischt Lizenz, Profil und User-Override in einem flachen Dictionary. Bei Profilwechsel geht die Information verloren was manuell geändert wurde. Abgelehnt.
- *RBAC direkt implementieren:* Überengineered für V1 (Single-User). Kommt mit Multi-User in späteren Phasen.
- *Eigenes Konzeptdokument statt Architektur-Abschnitt:* Erst bei Implementierung nötig. Architekturprinzip jetzt in BauProjektManager_Architektur.md Kap. 1.4 verankert.

**Konsequenzen:**

- Neues Kapitel 1.4 in BauProjektManager_Architektur.md
- Bei Implementierung: `IModuleVisibilityResolver`, `IModuleProfileCatalog`, Erweiterung von settings.json
- `activeModules` in settings.json wird bei Implementierung ersetzt durch `selectedProfileId` + `visibilityOverrides`
- Profilwahl im SetupDialog (Ersteinrichtung) + Einstellungen → Arbeitsprofil (dauerhaft änderbar)

---

*Dokument wird laufend aktualisiert wenn neue Architekturentscheidungen getroffen werden.*