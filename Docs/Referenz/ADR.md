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
- Kapitel: Fortlaufende ADRs (ADR-001 bis ADR-064)
- Pflichtlesen: keine (gezieltes Nachschlagen per ADR-Nummer)
- Fachliche Invarianten:
  - Statusmodell: Decision Status (Proposed/Accepted/Superseded/Deprecated) getrennt von Implementation Status (Not Started/Partial/Implemented)
  - ADR-Nummern fortlaufend — nie wiederverwenden
  - Superseded-ADR bleibt erhalten mit Verweis auf Nachfolger
  - Jede ADR hat: Status, Kontext, Entscheidung, Konsequenzen

---

﻿# BauProjektManager — Architecture Decision Records (ADR)

**Erstellt:** 29.03.2026
**Aktualisiert:** 13.04.2026
**Version:** 1.3
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
| 013 | .bpm-manifest als Projektordner-Ausweis | ⬅️ Superseded by ADR-046 | 2026-03 |
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
| 033 | Multi-User: 3 Modi (eigene DB, geteilte DB, Server) | ⬅️ Superseded by ADR-053 | 2026-03 |
| 034 | Modul-Aktivierung + Offline-Lizenzierung | 🟡 Konzept | 2026-03 |
| 035 | IExternalCommunicationService — zentrales Privacy Gate | ✅ Entschieden | 2026-04 |
| 036 | IPrivacyPolicy — austauschbare Policy für Internal/Commercial | ✅ Entschieden | 2026-04 |
| 037 | ISyncTransport — austauschbarer Sync-Transport (Folder/HTTP) | ⬅️ Superseded by ADR-053 | 2026-04 |
| 038 | IAccessControlService — rollenbasierte Projektfreigabe | 🟡 Partially superseded by ADR-053 | 2026-04 |
| 039 | Einheitliches ID-Schema — ULID als Primärschlüssel | ✅ Accepted / Not Started | 2026-04 |
| 040 | Migrations- und Versionierungsstrategie (DB + JSON) | ✅ Accepted / Not Started | 2026-04 |
| 041 | Recovery / Degraded Mode | ✅ Accepted / Not Started | 2026-04 |
| 042 | Secrets und Credentials | ✅ Accepted / Not Started | 2026-04 |
| 043 | Dev-Tools — Lokales Debug-Toolset für Entwicklung | ✅ Entschieden / Not Started | 2026-04 |
| 044 | Icons.xaml — Zentrale Icon-Registry | ✅ Entschieden / Implemented | 2026-04 |
| 045 | IndexSource — Dreistufiges Modell für Plan-Index-Erkennung | ✅ Entschieden | 2026-04 |
| 046 | .bpm/ Ordner — Manifest-Split und Profilablage im Projektordner | ✅ Entschieden | 2026-04 |
| 047 | Datenarchitektur + Sync — State-based lokal, change-based sync | 🟡 Partially superseded by ADR-053 | 2026-04 |
| 048 | Ansichtsprofile als UI-Sichtschicht über Modul-Aktivierung | ✅ Accepted / Not Started | 2026-04 |
| 049 | Pfad-Resolution Option C — relativer folder_name + Manifest-Fallback | ✅ Entschieden | 2026-04 |
| 050 | Source of Truth je Betriebsmodus (DB-Schema v2.1 mit Sync-Spalten) | ✅ Entschieden | 2026-04 |
| 051 | Client ist local-first — Server nur Auth + Sync + Autorität | ✅ Entschieden | 2026-04 |
| 052 | Lokaler Benutzerkontext über IUserContext statt lokaler Authentifizierung | ✅ Entschieden | 2026-04 |
| 053 | Server-Sync-Architektur — Windows-only Stack, Phase 0/1 VPS, Phase Verkauf On-Premise | ✅ Entschieden | 2026-04 |
| 054 | PlanManager Import Identity & Gruppierung | ✅ Entschieden | 2026-04 |
| 055 | IPersistenceRegistry — dynamisches Persistenz-Inventar als Single Source of Truth | ✅ Entschieden | 2026-05 |
| 056 | Segmenttyp-Architektur (BPM-108) — fieldTypeId + SemanticRole Zwei-Schichten-Modell | ✅ Entschieden | 2026-05 |
| 058 | Plan-Archiv-Persistenz (BPM-109) — Drei-Ebenen-Modell + Foundation Slice | ✅ Entschieden | 2026-06 |
| 059 | Recognition v2 / Plan-Erfassung — Manuelle Erstaufnahme (Strategie B) + Radial-UI | ✅ Entschieden | 2026-06 |
| 060 | Vereinheitlichte Dateisystem-Ports für alle Module | ✅ Entschieden | 2026-06 |
| 061 | DB als einzige Ordner-Wahrheit + DocumentTargetPathResolver | ✅ Entschieden | 2026-06 |
| 062 | Zentraler PDF-Render-Port (IPdfRenderService) | ✅ Entschieden | 2026-08 |
| 063 | PDF-Text-Port (IPdfTextService) + PdfPig-Freigabe | ✅ Entschieden | 2026-08 |
| 064 | Import-Transaktions-Härtung — idempotente Journal-/Recovery-/Undo-Semantik | ✅ Entschieden | 2026-08 |

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

**Betrifft:** ADR-047 (Datenklassifizierung Shared/Local bestimmt welche Daten in Domain vs. lokal)

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

**Datum:** 2026-03 (Erweiterung BPM-082: 2026-05)
**Status:** ✅ Entschieden
**Herkunft:** Architektur-Session
**Erweitert durch:** BPM-082 Cross-Review 17.04.2026 ([CGR-2026-04-17-bpm-082-segment-recognition](chatgpt-reviews/CGR-2026-04-17-bpm-082-segment-recognition/README.md))

**Kontext:**

Beim Anlernen von Plantyp-Mustern gibt es zwei Konzepte: Das verbindliche Profil für ein Projekt, und der Vorschlag aus einer Musterbibliothek.

**Entscheidung (Grundlage):**

- **RecognitionProfile** = verbindlich pro Projekt/Plantyp, gespeichert in `.bpm/profiles/<id>.json` (Cloud-Speicher, pro Projekt — ADR-046)
- **PatternTemplate** = Vorschlag aus Musterbibliothek, gespeichert in `pattern-templates.json` (Cloud-Speicher, global)

Beim Anlegen eines neuen Profils vergleicht das System mit bestehenden Templates und schlägt Übernahme vor. Neues Profil wird automatisch als Template gespeichert.

**Konsequenzen:**

- Kein Machine Learning, keine Blackbox — immer User-Bestätigung
- Templates synchen über Cloud-Speicher (auf beiden Geräten gleiche Vorschläge)
- Sync-Konfliktrisiko gering (Templates werden selten bearbeitet)

### Erweiterung BPM-082: Segment-basierte Erkennung (2026-05, SchemaVersion 3)

**Anlass:** Das urspruengliche Modell speicherte beim Profil-Wizard nur
`Method` (prefix/contains/regex) und `Pattern`, **aber keine Segment-Position**.
Dadurch matchte z.B. eine `contains: "PROT"`-Regel sowohl `PROJ-PROT-2025-01.pdf`
(gewollt) als auch `RK-PROTOKOLL-EG.pdf` (nicht gewollt). UI-Versprechen
(positionsgenaue Erkennung im Wizard Schritt 5) und Code-Verhalten (lose
Substring-Suche) waren entkoppelt.

**Entscheidungen (Konsens R3, 15 Punkte):**

1. **`segment` als Default-Methode** (positionsgenauer Token-Vergleich).
2. **`regex` als Fallback** fuer Spezialfaelle (Statiknummernkreise wie
   `^5998-2\d{2}_`, Dateien ohne saubere Delimiter).
3. **`prefix` und `contains` werden komplett entfernt** — keine Legacy-
   Toleranz (Fruehphasen-Prinzip in INDEX.md).
4. **`RecognitionRule.SegmentPosition: int?`** als persistiertes Feld.
   Pflicht bei `Method=segment`, ignoriert bei `Method=regex`.
5. **AND-Semantik bei Multi-Rules** eines Profils — alle Rules muessen
   matchen. Kein Operator-Layer (KISS).
6. **`SchemaVersion = 3`** fuer alle neu gespeicherten Profile.
7. **`RecognitionContext`** als Hilfstyp im Recognizer mit `FileName`,
   `FileStem`, `Tokens` (IReadOnlyList<string>).
8. **`FileNameParser` als gemeinsame Tokenisierungsquelle** fuer Wizard
   und Recognizer — kein zusaetzlicher Tokenizer-Service. Verhindert
   Drift zwischen Lern- und Laufzeitpfad.
9. **Variable-Segment-Warnung im Wizard Schritt 5** — UI-Hinweis bei
   markierten PlanNumber/PlanIndex/Datum/numerischen Segmenten. Kein
   Hard-Fail, Speichern bleibt erlaubt.
10. **ADR-010 wird erweitert**, kein neuer ADR (dieser Abschnitt).
11. **`ProfileManager.Load`/`LoadById` verwirft Profile** mit
    invalider Identitaet (Id leer, DocumentTypeName leer), fehlender
    Tokenization, leerer Recognition oder ungueltigen Rules. Loggt mit
    `Log.Error`. Der Recognizer sieht somit nie kaputte Profile.
12. **`MatchesSegment` schlank halten** — Position-Check + Token-Vergleich
    `OrdinalIgnoreCase`, keine Sonderlogik (kein Trim/Strip).
13. **Profil-Minimum-Validierung** (`IsProfileLoadable`): `Id`,
    `DocumentTypeName`, `Tokenization != null`, `Recognition.Count > 0`,
    alle `Rule.IsValid()`-Checks.
14. **Lokaler RecognitionContext-Cache pro `Recognize(...)`-Aufruf** mit
    Schluessel `(fileName, profile.Id)`. Kein langlebiger Feldcache.
15. **Doc-Pflege als eigener Sub 082.07** (dieser Eintrag, BACKLOG #20,
    GLOSSAR, PlanManager Kap. 14).

**Test-Szenarien aus Review R3:** 10 reale Baustellen-Beispiele aus
[CGR-2026-04-17-bpm-082-segment-recognition/r3/02-chatgpt-response.md](chatgpt-reviews/CGR-2026-04-17-bpm-082-segment-recognition/r3/02-chatgpt-response.md)
sind als Unit-Tests in `DocumentTypeRecognizerTests` und
`FileNameParserTests` umgesetzt.

**Implementierung:**

- 082.01 — Datenmodell + IsValid + SchemaVersion 3 (v0.27.0, `d82b05f`)
- 082.02 — Recognizer + RecognitionContext + segment + Cache (v0.28.27, `11fb3ca`)
- 082.06a — Core-Tests (v0.28.28, `e1b1db1`, 82 Tests)
- 082.03 — Wizard speichert segment-Rules (v0.28.29, `3f5f2af`)
- 082.04 — Wizard-UI Cleanup + Variable-Warnung + Tests (v0.28.30, `742300e`)
- 082.05 — Legacy raus + ProfileManager.Load-Validation (v0.28.31, `d4c1f17`)
- 082.06b + 082.06c — Wizard-/Persistence-Tests + Load-Toleranz (v0.28.32, `468bf56`)
- 082.07 — Doc-Pflege (dieser Eintrag)

**Reset-Anweisung fuer Fruehphasen-Setups:**

Profile aus dem alten Schema (mit `Method=prefix` oder `contains`) werden
beim Laden mit `Log.Error` verworfen und sind nicht mehr matchbar. Aktion
fuer Tester: betroffene `.bpm/profiles/*.json`-Dateien loeschen, im Wizard
neu anlegen. Disk-Reset via DevTools → Reset-Tab → Quick-Reset "All" oder
manuell pro Projekt.

### Erweiterung BPM-109: `document_key` bekommt FK-Bezug zu plan_documents (2026-06)

Mit ADR-058 (Plan-Archiv-Persistenz) wird der vom `DocumentKeyBuilder`
erzeugte `document_key` nicht mehr direkt als Identitäts-String in
`plan_revisions` gespeichert, sondern landet in **`plan_documents.document_key`**
(`UNIQUE`-Spalte). `plan_revisions.document_id` ist die stabile FK-Referenz für
Cross-Modul-Verknüpfungen.

**Recognition-Logik selbst ist nicht betroffen:**
- `RecognitionProfile.IdentityFields`, `RecognitionRule.Method`/`Pattern`/
  `SegmentPosition`, der `FileNameParser` und die `DocumentTypeRecognizer`-
  Match-Methoden bleiben unverändert.
- Der `DocumentKeyBuilder` produziert weiterhin denselben Natural-Key — er
  wird nur an einer anderen Tabellenstelle persistiert.

**Bautagebuch/Foto/Vorlagen-Module verwenden NICHT `document_key`-Strings**,
sondern `plan_documents.id` als FK-Ziel. `document_key` bleibt für
Debug/Export/Migration nützlich.

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
**Status:** ⬅️ Superseded by ADR-046 (.bpm/ Ordner ersetzt einzelne .bpm-manifest Datei)
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

Nur zwei Status: Active und Completed. Archivierung ist kein eigener Status, sondern eine Aktion: Status auf Completed setzen + Projektordner von BasePath nach ArchivePath verschieben. Pfad-Resolution über Option C (relativer folder_name + Manifest-Fallback bei Umbenennung). Feature #12.

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

Zentrales Theme-System mit 8 Resource Dictionaries im Ordner `Themes/` des App-Projekts:

- **Colors.xaml** — Alle Farb-Token als SolidColorBrush (Background, Surface, Text, Accent, Status-Farben)
- **Typography.xaml** — Schriftgrößen-Stufen (XS bis XXL, Segoe UI)
- **Buttons.xaml** — Button-Varianten (Primary, Secondary, Danger, Ghost, Nav)
- **Inputs.xaml** — TextBox, ComboBox, DatePicker, CheckBox Styles
- **DataGrid.xaml** — Header, Row, Cell, Zebra-Variante
- **Tabs.xaml** — TabControl + TabItem mit Unterstrich-Style
- **Dialogs.xaml** — Dialog-Basis, Cards, Tooltips, Separatoren
- **Icons.xaml** — Zentrale Icon-Registry mit Emoji-String-Resources (ADR-044)

Ursprünglich 5 Dictionaries. Erweitert um Inputs.xaml und Tabs.xaml (7), dann Icons.xaml (8, ADR-044).

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
**Status:** ⬅️ **Superseded by ADR-053** (2026-04-30)
**Herkunft:** Phase 1 Teil 2

> **Hinweis:** Modus B (LAN Shared SQLite mit Write-Lock) wurde nie implementiert und ist durch direkten Sprung zu Modus C überflüssig. ADR-053 reduziert die Modi auf A (Solo lokal) + C (Server), wobei C als Windows-VPS in Phase 0/1 + On-Premise beim Kunden in Phase Verkauf realisiert wird. Der Inhalt unten ist historische Referenz.

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
**Status:** ⬅️ **Superseded by ADR-053** (2026-04-30)
**Herkunft:** Multi-User Architektur-Diskussion (Claude + ChatGPT), Analyse von PlanRadar/Procore/Dalux

> **Hinweis:** `FolderSyncTransport` ist verworfen (Cloud-Drive ist kein Message-Bus, Wegwerf-Engineering). `HttpSyncTransport` wird durch BPM-eigenes `IBpmSyncClient` ersetzt — kein generisches Transport-Interface mehr, sondern domänenspezifisches Sync-Interface mit Pull/Push-Vertrag. Der Inhalt unten ist historische Referenz.

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
**Status:** 🟡 **Partially superseded by ADR-053** (2026-04-30)
**Herkunft:** Multi-User Architektur-Diskussion (Claude + ChatGPT)

> **Hinweis:** Phase-2-Modell mit `project_shares` Tabelle und Pseudo-RBAC ist verworfen. Phase-3-RBAC bleibt im Kern gültig, aber wird durch ADR-053 konkretisiert: ASP.NET Core Identity + JWT + `project_memberships`-Tabelle, Rollen V1 reduziert auf `admin`, `bauleiter`, `polier`, `gast`. `disponent` und `lohnbüro` erst mit zugehörigen Modulen. Kein Multi-Tenant-RLS (Single-Tenant pro Installation).

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
**Status:** ✅ Entschieden / Partial (v0.17.2: 3 Tabs + 4 Reset-Optionen, planmanager.db-Reset fehlt noch)
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
**Status:** 🟡 **Partially superseded by ADR-053** (2026-04-30)
**Herkunft:** Claude + ChatGPT Cross-Review (4 Runden, 10.04.2026)
**Konzeptdokument:** [DatenarchitekturSync.md](../Konzepte/DatenarchitekturSync.md) (vollständig superseded)

> **Hinweis:** Folgende Punkte aus ADR-047 sind durch ADR-053 ersetzt:
> - **Punkt 4** (Outbox/Inbox Pattern) — durch IBpmSyncClient Pull/Push ersetzt
> - **Punkt 5** (Snapshots + Events Initial-Sync) — durch klassisches Pull mit server_version-Checkpoint ersetzt
> - **Punkt 6** (12 Sync-Metadaten-Spalten) — durch 7-Spalten-Modell ersetzt (ADR-050)
> - **Punkt 9** (Phase 2 als bewusst temporäre Übergangsarchitektur) — Phase-Modell entfällt (Modell B On-Premise)
> - **Punkt 11** (Aggregate `diary_days`/`diary_notes` für Multi-Writer-Cloud-Sync) — überflüssig durch Server-Authority
>
> **Was bleibt gültig (explizit bestätigt durch ADR-053):**
> - **Punkt 1** (state-based lokal, change-based zwischen Clients)
> - **Punkt 2** (4-Klassen-Datenmodell A/B/C/D)
> - **Punkt 3** (sensitive Daten in eigenen Tabellen)
> - **Punkt 7** (User-Modell jetzt — durch ASP.NET Identity in ADR-053 konkretisiert)
> - **Punkt 10** (Phase 3: PostgreSQL serverseitig — bestätigt)

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

---

## ADR-049: Pfad-Resolution Option C — relativer folder_name + Manifest-Fallback

**Datum:** 2026-04
**Status:** ✅ Entschieden
**Implementierung:** Not Started — **Post-V1 Zielarchitektur** (V1 verwendet weiterhin gespeicherten root_path)
**Herkunft:** Docs-Audit Archivierungs-Diskussion (Claude + Herbert, 11.04.2026)

**Kontext:**

Projekte haben in der DB ein `root_path` Feld mit absolutem Pfad zum Projektordner. Beim Archivieren (Ordner von BasePath nach ArchivePath verschieben) muss dieser Pfad aktualisiert werden. Ebenso bei Ordner-Umbenennung im Explorer. Drei Optionen wurden evaluiert:

- **Option A (relativ):** DB speichert nur `folder_name`, voller Pfad wird zur Laufzeit aus `BasePath/ArchivePath + Status + FolderName` berechnet. Einfachste Lösung, aber bricht bei Explorer-Umbenennung.
- **Option B (Manifest-Scan):** Kein Pfad in der DB. App scannt bei Start alle Ordner nach `.bpm/manifest.json` und findet Projekte über `projectId`. Robust, aber Scan-Overhead.
- **Option C (A + B als Fallback):** Relativer `folder_name` als Default. Wenn der berechnete Pfad nicht existiert → Fallback auf Manifest-Scan. Robust UND performant.

**Entscheidung:**

Option C. `root_path` wird ersetzt durch berechneten Pfad aus `folder_name` + Status. Bei Nicht-Existenz Fallback auf `.bpm/manifest.json`-Scan.

```csharp
string GetProjectPath(Project p)
{
    // Primär: berechnet aus folder_name + Status
    var basePath = p.Status == ProjectStatus.Active
        ? settings.BasePath
        : settings.ArchivePath;
    var calculated = Path.Combine(basePath, p.FolderName);

    if (Directory.Exists(calculated))
        return calculated;

    // Fallback: Manifest-Scan über projectId
    return ScanForManifest(p.Id, settings.BasePath, settings.ArchivePath);
}
```

**Konsequenzen:**

- `root_path` in der DB wird zu `folder_name` (nur der Ordnername, nicht der volle Pfad)
- Archivierung: Nur Status auf Completed setzen + Ordner physisch verschieben. Kein DB-Pfad-Update nötig.
- Reaktivierung: Status auf Active + Ordner zurück verschieben. Kein DB-Pfad-Update nötig.
- Explorer-Umbenennung: Fallback findet den Ordner über `.bpm/manifest.json`
- `registry.json`: `rootPath` wird weiterhin als absoluter Pfad berechnet (nicht aus DB gelesen)
- 6 C#-Dateien betroffen: ProjectDatabase.cs, SettingsViewModel.cs, ProjectEditDialog.xaml.cs, RegistryJsonExporter.cs, PlanManagerViewModel.cs, ProjectDetailViewModel.cs, ProfileWizardViewModel.cs
- DB-Migration: `root_path` Spalte bleibt, wird aber mit relativem `folder_name` befüllt

**Alternativen (abgelehnt):**

- *Absoluter Pfad beibehalten:* Einfachster Code, aber Archivierung und Umbenennung erfordern DB-Update + registry.json-Update. Fragil.
- *Nur Manifest-Scan (Option B):* Robust, aber unnötiger Scan-Overhead bei jedem Start wenn der Pfad korrekt ist.

**Betrifft:** ADR-004, ADR-013, ADR-025, ADR-046

---

## ADR-050: Source of Truth je Betriebsmodus

**Datum:** 2026-04-15
**Status:** ✅ Entschieden
**Implementierung:** Partial — Sync-Felder-Konvention ab sofort, Server-Implementierung Post-V1
**Herkunft:** 3-Runden Cross-Review Claude/ChatGPT (ServerArchitektur-Konzept)

**Kontext:**

BPM hat drei Betriebsmodi (ADR-033). Bisher galt pauschal "SQLite ist System of Record" (Architektur-Doc). Mit dem geplanten Server-Modus (Modus C) entsteht ein Widerspruch: Wenn mehrere Benutzer über einen Server arbeiten, kann nicht gleichzeitig die lokale SQLite jedes Clients SoR sein.

**Entscheidung:**

Source of Truth ist kontextabhängig pro Betriebsmodus:

| Modus | SoR | SQLite-Rolle | Server-Rolle |
|-------|-----|-------------|-------------|
| A (Solo/Offline) | Lokale SQLite | System of Record | nicht vorhanden |
| C (Server) | PostgreSQL am Server | Offline-Cache + Pending Changes | SoR + Auth + Fachregeln |

Im Server-Modus gilt: "Server gewinnt" bei Daten-Konflikten. Der Server erzwingt zusätzlich Fachregeln (z.B. keine Änderung freigegebener Buchungen).

**Konsequenzen:**

- Architektur-Doc muss die pauschale Aussage "SQLite ist SoR" auf Modus A einschränken
- Sync-Felder (created_at, created_by, last_modified_at, last_modified_by, sync_version, is_deleted) ab sofort in jede neue Tabelle
- ULID als Primary Key, clientseitig erzeugt
- Zeitstempel immer UTC
- Soft Delete für sync-relevante Tabellen

**Betrifft:** ADR-033 (3 Modi), Architektur-Doc Kapitel 2 (Speicherstrategie)

---

## ADR-051: Client ist local-first — Server nur Auth + Sync + Autorität

**Datum:** 2026-04-15
**Status:** ✅ Entschieden
**Implementierung:** Not Started — Konzeptionell ab sofort gültig, Implementierung Post-V1
**Herkunft:** 3-Runden Cross-Review Claude/ChatGPT (ServerArchitektur-Konzept)

**Kontext:**

Im Server-Modus könnte der Client entweder direkt den Server für Reads/Writes nutzen (online-first) oder weiterhin lokal arbeiten und nur synchronisieren (local-first). Da Baustellen häufig kein oder schlechtes Internet haben, muss die App jederzeit voll funktionsfähig sein — auch ohne Serververbindung.

**Entscheidung:**

Der Client arbeitet in JEDEM Betriebsmodus local-first:

- **Reads:** Immer aus lokaler SQLite
- **Writes:** Immer in lokale SQLite
- **Server-Kontakt nur für:** Login / Token-Refresh, Sync (Push/Pull), Erstsync, Recovery
- **Keine gemischten Read-Pfade:** UI liest nie direkt vom Server

Offline-Verhalten: JWT abgelaufen → lokales Arbeiten NICHT blockiert. Re-Auth erst vor nächstem Sync erforderlich.

Im Server-Modus werden ALLE Projektdaten lokal gecacht (kein selektiver Sync in V1/V2).

**Konsequenzen:**

- Keine API-Calls in ViewModels oder Application Services für fachliche Reads
- Sync ist ein eigener Hintergrund-Prozess, nicht Teil der Use-Case-Pipeline
- Lokaler Benutzerkontext über settings.json ("localUserName") für Modus A, über JWT-Claims für Modus C
- Writes laufen über Application Services die Metadaten setzen (userId, utcNow, syncVersion)

**Betrifft:** ADR-050 (SoR je Modus), ADR-033 (3 Modi), ADR-035 (IExternalCommunicationService)

---

## ADR-052: Lokaler Benutzerkontext über IUserContext statt lokaler Authentifizierung

**Datum:** 2026-04-16
**Status:** ✅ Entschieden
**Implementierung:** Partial (IUserContext + LocalUserContext implementiert, DI-Verdrahtung ausstehend)
**Herkunft:** 2-Runden Cross-Review Claude/ChatGPT (Auth-Strategie)

**Kontext:**

BPM braucht ab sofort eine Benutzeridentität für `created_by`/`last_modified_by` in Sync-fähigen Tabellen (ADR-050). Drei Optionen wurden evaluiert: A) localUserName in settings.json, B) lokale User-Tabelle mit Passwort/Login, C) Windows-Login (`Environment.UserName`).

Option B (lokale Auth) wurde verworfen: Ohne Server schützt ein lokales Passwort nichts — die SQLite liegt unverschlüsselt auf der Platte. Der Aufwand (User-Tabelle, Passwort-Hashing, Login-Dialog, Reset-Logik) steht in keinem Verhältnis zum Nutzen. Zudem entsteht Legacy-Code der bei Server-Einführung entsorgt werden muss.

**Entscheidung:**

Lokaler Benutzerkontext über `IUserContext`-Interface (Domain-Projekt):

```csharp
public interface IUserContext
{
    string UserId { get; }
    string DisplayName { get; }
    UserContextSource Source { get; }
}

public enum UserContextSource { Local, Server }
```

- **Modus A:** `LocalUserContext` (Infrastructure) liest aus settings.json:
  - `localUserId` = `MachineName\UserName` (automatisch, technisch, nur intern)
  - `localUserName` = lesbarer Anzeigename (manuell pflegbar in Einstellungen)
- **Modus C:** `JwtUserContext` (Infrastructure) liest aus JWT-Claims
- `created_by`/`last_modified_by` = immer `IUserContext.DisplayName`
- Kein `IsAuthenticated` in Modus A, keine E-Mail, keine lokale User-Tabelle

**Konsequenzen:**

- `IUserContext` + `UserContextSource` ins Domain-Projekt
- `LocalUserContext` ins Infrastructure-Projekt
- `localUserId` + `localUserName` als neue Felder in `AppSettings` + settings.json
- `localUserName` Default: `Environment.UserName`, änderbar in Einstellungen
- `localUserId` Default: `Environment.MachineName\Environment.UserName`
- Services setzen `created_by`/`last_modified_by` über `IUserContext.DisplayName`
- `created_by`/`last_modified_by` sind Anzeige-/Auditnamen, keine Authentitätsnachweise

**Betrifft:** ADR-050 (SoR je Modus), ADR-051 (Local-First), CODING_STANDARDS Kap. 19.6

---

## ADR-053: Server-Sync-Architektur — Windows-only Stack, Phase 0/1 VPS, Phase Verkauf On-Premise

**Datum:** 2026-04-30
**Status:** ✅ Entschieden
**Implementierung:** Not Started — Spike 0 (ProjectDatabase syncfähig) als erster Code-Schritt
**Herkunft:** 7-Runden Cross-Review Claude/ChatGPT (CGR-2026-04-30-datenarchitektur-sync)

**Kontext:**

BPM hat heute keine Server-Sync-Architektur. Lokale `bpm.db` läuft gerätelokal, `project.json` wird zwar geschrieben aber nur als Manuell-Import-Tool genutzt. Mit der Anforderung "5-10 User parallel arbeitend in Herberts Firma + Live-Sync zwischen Büro und Baustelle" und der Roadmap "spätere Verkaufbarkeit als On-Premise-Software" wurde eine Architektur-Entscheidung in 7 Cross-Review-Runden mit ChatGPT erarbeitet.

Drei fundamentale Pivots im Lauf der Diskussion:
1. **Geschäftsmodell B** statt A: Kunde installiert auf eigenem Server (On-Premise) statt Herbert hostet zentral SaaS
2. **Windows-only komplett:** kein Linux für Entwicklung, Test oder Produktion
3. **Multi-User Live-Sync ab Phase 0/1** statt Solo-Einzelnutzer: 5-10 User parallel arbeitend, 24/7-Server nötig

**Entscheidung:**

### Code-Stack (gilt für Phase 0/1 + Phase Verkauf)

1. **Windows-only** für Entwicklung, Test, Produktion
2. **Server-Stack:** PostgreSQL 17 als Windows-Service + ASP.NET Core 10 Worker Service (`UseWindowsService()`) + Caddy for Windows als optionaler Reverse-Proxy für HTTPS
3. **WPF-Client** topologieneutral mit konfigurierbarer `ServerUrl` (HTTP und HTTPS, beliebige Server-URLs)
4. **`IBpmSyncClient`** als BPM-eigenes Sync-Interface mit austauschbaren Adaptern (kein Vendor-Lock-in zu Supabase/Datasync/Library X)
5. **Sync-Protokoll:** Pull/Push mit `server_version`, Server-gewinnt-Konflikt-Strategie, keine Merge-UI in Phase 0/1
6. **Single-Tenant** pro Installation — keine Multi-Tenant-Architektur, keine Postgres RLS

### Auth (ab Phase 0.5 nötig wegen Multi-User)

7. **ASP.NET Core Identity** in PostgreSQL für User-Verwaltung
8. **JWT** Access Token (15-30 min) + **Refresh Token** pro Gerät (30 Tage)
9. **Rollen Phase 0/1:** `admin`, `bauleiter`, `polier`, `gast`. `disponent`/`lohnbüro` erst mit zugehörigen Modulen.
10. **Servermodus zwingend Login-pflichtig**, Modus A (lokal-Solo) bleibt ohne Login

### Daten

11. **DataClassification + Whitelist** pro Sync-DTO (Klasse-A/B/C laut ADR-047)
12. **`device_id`** in `device-settings.json` + separater `IDeviceContext` für Sync-History/Audit (zusätzlich zu `IUserContext` aus ADR-052)
13. **`recognition_profiles`** wandert in DB-Tabelle (post Spike 0). `.bpm/profiles/*.json` bleibt im Servermodus Export/Backup, nicht Source of Truth.

### Hosting Phase 0/1 (Solo + Herberts eigene Firma, 24 Monate, 5-10 User)

14. **Windows-VPS in EU** als Phase-0/1-Server (Strato VC 2-8 oder vergleichbar, ~12€/Monat, Windows Server 2025, 2 vCores, 8 GB RAM, 120 GB SSD)
15. **Domain** (z.B. `bpm.firma.at`) mit DNS A-Record auf VPS-IP
16. **HTTPS via Caddy + Let's Encrypt** automatisch
17. **PostgreSQL 17 + ASP.NET Core 10 als Windows-Services** auf VPS
18. **Backup:** PowerShell `pg_dump` als Scheduled Task + Provider-Snapshots
19. **Connectivity:** Direkte HTTPS-URL — kein VPN, kein Tailscale, kein Cloudflare Tunnel pro User
20. **Verworfene Hosting-Alternativen:** Hauptrechner-24/7, Synology DS124/DS224+, Linux-VPS, Hetzner Cloud (kein Windows), Tailscale-Premium (zu teuer ab 7+ User), Cloudflare Tunnel (DSGVO/Auth-Komplexität)

### Hosting Phase Verkauf (24+ Monate, On-Premise bei Kunden)

21. **Windows Server beim Kunden:** Windows 11 Pro für Kleinst-Kunden (1-5 User), Windows Server 2022/2025 ab 10 User
22. **Inno Setup Installer** für Server + Client (kostenlos, scriptbar, etabliert in KMU-Markt)
23. **Signierte Lizenzdatei** (Ed25519/RSA, offline-fähig, kein harter Stopp bei Wartungsablauf — Software läuft weiter, Updates/Support gesperrt)
24. **AD/LDAP-Integration optional** (Bauunternehmen mit AD)
25. **Auto-Update** für WPF-Client (Velopack) + manueller Server-Update durch Admin

### Frühphasen-Konformität

26. **Keine Migration** für Schema-Änderungen — DB-Reset bei Schema-Update
27. **Keine Backward-Compatibility** in Loadern/Deserializern
28. Single-Tenant heute reicht — Multi-Tenant nur falls in 5+ Jahren SaaS-Modell zusätzlich gewünscht

**Spike-Reihenfolge:**

1. **Spike 0:** ProjectDatabase syncfähig machen (Soft Delete + gezielte Upserts statt Replace-All-Listen) — bereits im Tracker (post-v1)
2. **Spike 1:** ASP.NET Core 10 Worker Service Skelett + PostgreSQL lokal + erste `/health`-Endpoint
3. **Spike 2:** ASP.NET Identity + JWT + erster Login-Flow
4. **Spike 3:** Sync-Endpoints für `clients` + `projects` (Pull/Push, server_version)
5. **Spike 4:** VPS-Setup mit Domain + Caddy + HTTPS
6. **Spike 5:** Multi-Client-Test mit 2 lokalen SQLite-Instanzen + Server

**Konsequenzen:**

- WPF-Client braucht von Anfang an `ServerUrl`-Konfiguration (kein Hardcode auf localhost o.ä.)
- ASP.NET-Code muss plattformneutral bleiben (keine Windows-Pfade hardcoded, keine PowerShell-Calls, keine COM)
- DB-Schema bleibt gleich für Phase 0/1 + Phase Verkauf — nur Hosting unterscheidet sich
- Keine Mobile-App in Phase 0/1 (BPM-Mobile bleibt post-v1, BACKLOG-Eintrag)
- Recognition Profiles werden mittelfristig von `.bpm/profiles/*.json` in DB-Tabelle migriert (Frühphase: Reset, kein Migration-Code)
- `DatenarchitekturSync.md` wird nicht komplett superseded, aber FolderSync/Event-Outbox-Pfad wird durch ADR-053 superseded. 4-Klassen-Datenmodell (ADR-047) und Local-First-Prinzip (ADR-051) bleiben gültig.

**Alternativen:**

Verworfene Optionen mit Begründung:
- *Eigenbau β3 (OneDrive-JSON-Events):* Wegwerf-Engineering, Cloud-Drive ist kein Message-Bus
- *CouchDB + PouchDB.NET:* Datenmodell-Wechsel von relational zu dokumentorientiert, schlechter BPM-Fit
- *Supabase Hosted:* Vendor-Lock-in, Realtime ist Notification nicht Sync, US-Anbieter
- *Linux-VPS + Docker:* User-Vorgabe (will keine Linux-Erfahrung), Kosten-Ersparnis rechtfertigt Lernaufwand für Solo-Entwickler nicht
- *Synology DS224+:* 600-800€ Anschaffung wirtschaftlich schlechter als VPS bei reinem BPM-Hosting
- *Hauptrechner als 24/7-Server:* unrealistisch für 5-10 User Multi-User Live-Sync (Updates, Reboots, Strom, Verfügbarkeit)
- *Tailscale Premium für 7+ User:* teurer als Windows-VPS
- *Multi-Tenant + RLS:* nicht nötig für On-Premise-Modell, Komplexität ohne Nutzen

**Dokumentation:**

- Vollständige Diskussion in [Docs/Referenz/chatgpt-reviews/CGR-2026-04-30-datenarchitektur-sync/](chatgpt-reviews/CGR-2026-04-30-datenarchitektur-sync/) mit 7 Runden + 28 archivierten Dateien
- Kernergebnisse in [README.md](chatgpt-reviews/CGR-2026-04-30-datenarchitektur-sync/README.md) der Serie

**Betrifft:** ADR-046 (.bpm/), ADR-047 (4-Klassen-Datenmodell), ADR-050 (DB-Schema v2.1), ADR-051 (Local-First), ADR-052 (IUserContext), `Docs/Konzepte/DatenarchitekturSync.md` (FolderSync-Pfad superseded), `Docs/Konzepte/ServerArchitektur.md` (bleibt relevant für Phase Verkauf)

### Erweiterung BPM-109: `project_id`-Redundanz in `planmanager.db` (2026-06)

Mit ADR-058 (Plan-Archiv-Persistenz) trägt `plan_documents` explizit eine
**`project_id`-Spalte**, obwohl `planmanager.db` bereits pro Projekt existiert.
Die Redundanz ist bewusst — Begründung:

- **Sync-Robustheit:** Wenn `planmanager.db` exportiert oder in eine gemeinsame
  Server-DB gestreamt wird (ADR-051 Phase Verkauf), bleibt die Projekt-Zuordnung
  intakt — kein impliziter Kontext aus dem Dateinamen.
- **Debug/Export-Hilfe:** Direkte Inspektion der `plan_documents`-Tabelle (z.B.
  in SQLite-Browser) zeigt sofort, zu welchem Projekt die Daten gehören.
- **Spätere Multi-Tenant-Vorbereitung:** Falls eine logische DB irgendwann
  Daten mehrerer Projekte hält (z.B. Sync-Server), ist der FK schon da.
- **Kosten:** Eine TEXT-Spalte je Document — vernachlässigbar.

Diese Begründung gilt analog für `plan_revisions`, `plan_document_segments` etc.
nicht — diese hängen über FK an `plan_documents` und erben damit die Projekt-Zuordnung.

## ADR-054: PlanManager Import Identity & Gruppierung

**Datum:** 2026-04 (rückwirkend dokumentiert 2026-05)
**Status:** ✅ Entschieden
**Implementierung:** ✅ Implemented — Schema + 7-Stufen-Pipeline (v0.25.13, Phase F), Recovery-Workflow (v0.27.23–v0.27.24, RecoveryDecisionService + RecoveryExecutorService mit 10 Tests, BPM-016 done)
**Herkunft:** 3-Runden Cross-Review Claude/ChatGPT (10.04.2026, Teil 10), Praxis-Pipeline-Konzept (15.04.2026, Teil 17), Auto-Link-Regeln und Stage-Konzept (15.04.2026, Cross-Review-Konsens)

**Hinweis zur Nummer:** Diese ADR war ursprünglich als ADR-050 reserviert (siehe Teil 17, 15.04.2026). Am gleichen Tag wurde ADR-050 jedoch im Konzept-Serverstruktur-Chat für „Source of Truth je Betriebsmodus" vergeben, ohne dass die ursprüngliche Reservierung berücksichtigt wurde. Da ADR-Nummern nicht wiederverwendet werden (Statusmodell-Regel im Kopf der ADR.md), erhält diese ADR jetzt die nächste freie Nummer (ADR-053 wurde inzwischen ebenfalls für die Server-Sync-Architektur vergeben).

**Kontext:**

Der PlanManager muss beim Import von Plänen entscheiden, welche Datei welcher fachlichen Identität entspricht: Ist das ein neuer Plan, eine neue Revision eines bestehenden Plans, eine geänderte Datei zu derselben Revision, oder ein zu archivierender Vorgänger? Diese Entscheidung ist die Grundlage für die gesamte Import-Pipeline (Versionierung, Archivierung, Auto-Link, Undo, Recovery).

Im Zuge des PlanManager-Konzepts wurden mehrere konkrete Strukturen im Schema und Code etabliert, die in keiner ADR begründet sind:

1. Drei-Tabellen-Identity-Hierarchie für den Import (`import_journal` → `import_actions` → `import_action_files`)
2. `document_key` als fachliche Identity statt Dateipfad oder Plannummer allein
3. n:m-Verknüpfung zwischen `plan_revisions` und `plan_files` über `revision_file_links`
4. `md5_hash` als universeller Pflicht-Fingerabdruck auf allen Dateien (auch bei IndexSource=FileName)
5. `action_status` (pending/completed/failed) auf Action-Ebene als Recovery-Anker
6. `origin_mode` mit drei Werten als Herkunfts-Audit
7. Stage-Konzept (Unknown/Draft/Final) als separater Aspekt, bewusst NICHT Teil des `document_key`
8. `tokenization` als profilgebundene Vorbedingung der document_key-Bildung
9. `includeInIdentity`-Flag für Custom-Felder
10. `MultiplePlanNumbers → ReviewRequired` als V1-Strategie

Diese Entscheidungen wurden in mehreren Cross-Review-Runden mit ChatGPT (Runden 1-3 am 10.04.2026, weitere am 15.04.2026) abgestimmt und sind bereits im Code (PlanManagerDatabase v0.25.13, 7-Stufen-Pipeline) und in `Docs/Module/PlanManager.md` v2.0 implementiert. Die ADR holt die fehlende Architekturbegründung nach.

**Entscheidung:**

### 1. Drei-Tabellen-Identity-Hierarchie für Import

Der Import wird über drei aufeinander aufbauende Tabellen abgebildet, jede mit eigenem ULID-Primärschlüssel:

| Tabelle | Verantwortung | Beziehung |
|---------|---------------|-----------|
| `import_journal` | Ein Import-Vorgang (Batch) — Zeitpunkt, Status, Quellpfad, Profil, Maschine | 1 Journal hat n Actions |
| `import_actions` | Eine fachliche Aktion pro Plan-Revision — Action-Typ, Status, document_key, Plannummer/Index | 1 Action hat 1..n Files |
| `import_action_files` | Eine physische Datei pro Aktion — Name, Pfade, Hash, Größe | n:1 zu Action |

**Begründung:** Eine Revision besteht aus 1..n Dateien (ADR-007), daher reicht eine 2-Tabellen-Lösung mit Datei-pro-Action nicht. Die dritte Tabelle ermöglicht sauberes Rückwärts-Undo (über `action_order`) und Teilfehler-Handling (über `action_status` pro Aktion). Der separate `import_journal`-Eintrag erlaubt das Tracking der gesamten Batch-Operation unabhängig von Einzelaktionen.

Verworfene Alternativen:
- 2-Tabellen-Lösung mit festem PDF/DWG-Paar (in der Praxis nicht haltbar — siehe ADR-007)
- INTEGER-IDs (durch ADR-039 ohnehin ausgeschlossen)

### 2. document_key als fachliche Identity

`document_key` ist eine deterministisch aus `identityFields` des RecognitionProfile gebildete Zeichenkette, die ein Dokument fachlich identifiziert.

```
document_key := join("_", values_of(identityFields_in_resolution_order))
Beispiel: identityFields = ["documentType", "planNumber", "haus"]
       → document_key = "Polierplan_103_H64"
```

Die Bildung erfolgt im `DocumentKeyBuilder`-Service (Stufe 5 der 7-Stufen-Pipeline) auf Basis der vom `ImportContextResolver` (Stufe 4) gelieferten Felder.

**Begründung:** Dateiname und Dateipfad sind keine zuverlässigen Identity-Indikatoren. Praxis-Beispiel aus Teil 17: gleiche Dateinamen in verschiedenen Ordnern können verschiedene Dokumente sein (z.B. `Wand_01.pdf` als Schalungsplan-Dokument vs. Bewehrungsplan-Dokument). Identity muss aus fachlichen Feldern (Plannummer, Geschoss, Bauteil etc.) gebildet werden, nicht aus Dateinamen-Stamm.

### 3. Auto-Link über vier Bedingungen

Dateien werden nur dann automatisch zu einer Revision gruppiert, wenn ALLE vier Bedingungen erfüllt sind:

1. Gleicher `document_key`
2. Gleiche `document_type` (DocumentTypeId)
3. Gleicher Revisionsstand
4. Erlaubte Extension-Kombination (z.B. pdf+dwg, pdf+dxf)

Kein Auto-Link nur wegen gleichem Dateinamen-Stamm. Verletzungen einer Bedingung führen zu separaten Revisionen oder zu manueller Verknüpfung durch den Benutzer.

### 4. n:m zwischen plan_revisions ↔ plan_files

Die Verknüpfung zwischen Revisionen und physischen Dateien erfolgt über die Tabelle `revision_file_links` als n:m-Beziehung mit den Feldern `link_mode` (auto|manual) und `is_primary`.

**Begründung:** In der Baupraxis enthält eine einzelne DWG häufig die Geometrie für mehrere Pläne (Sammel-DWG für ein ganzes Bauteil), während die zugehörigen PDFs für jede Revision einzeln geliefert werden. Eine 1:n-Beziehung mit nullable `revision_id` würde diesen Fall nicht abbilden, da eine Datei mehreren Revisionen zugeordnet sein kann.

Eine Datei ohne Eintrag in `revision_file_links` ist standalone (taucht als „nicht zugeordnet" auf). Sobald ein Link existiert, gilt die Datei als zugeordnet und verschwindet aus der Standalone-Liste. Der Rückweg ist explizit (Link entfernen).

### 5. origin_mode als Herkunfts-Audit

`plan_files.origin_mode` mit drei Werten dokumentiert die ursprüngliche Verknüpfungsherkunft:

| Wert | Bedeutung |
|------|-----------|
| `autoGrouped` | Beim Import automatisch über die vier Auto-Link-Bedingungen verknüpft |
| `manualLinked` | Vom Benutzer manuell verknüpft |
| `standalone` | Initial keine Verknüpfung |

`origin_mode` ist ein historisches Audit-Feld — der **aktuelle Zustand** „hat Links oder nicht" ergibt sich ausschließlich aus der Tabelle `revision_file_links`, nicht aus diesem Feld. Das Feld wird beim ersten Import gesetzt und nicht nachträglich umgeschrieben, wenn der Verknüpfungsstatus später durch manuelle Aktionen ändert.

### 6. md5_hash als universeller Pflicht-Fingerabdruck

`plan_files.md5_hash` ist `NOT NULL` für ALLE Dateien, unabhängig von der `IndexSource` des zugehörigen Profils. Zusätzlich wird `file_size` als Sekundär-Prüfwert gespeichert (Doppel-Fingerabdruck).

**Begründung:** Der Hash hat mehrere Verwendungen jenseits der reinen Änderungserkennung bei IndexSource=None:

- SKIP_IDENTICAL-Erkennung (Datei am Zielort bereits identisch vorhanden)
- Wiedererkennung nach Umbenennung (Pfad ändert, Hash bleibt)
- Cache-Rebuild auf zweitem Gerät (bekannte Dateien wiederfinden)
- DWG-Veraltet-Warnung über die Link-Tabelle (Hash-Vergleich der verknüpften Dateien)

Daher ist der Hash auch bei IndexSource=FileName Pflicht. Eine ältere Schema-Version hatte den Hash optional — diese Lockerung wurde im Cross-Review (10.04.2026, Teil 10) bewusst zurückgenommen.

Verworfene Alternativen:
- `file_size` allein (zu unspezifisch)
- Stärkere Hashes (SHA-256) für V1 nicht nötig — Hash dient nicht der Sicherheit sondern der Identität (siehe Konzeptdoc PlanManager.md Kap. 12.2)

### 7. action_status als Recovery-Anker auf zwei Ebenen

Der Import-Status wird auf zwei Ebenen geführt:

| Ebene | Spalte | Werte | Funktion |
|-------|--------|-------|----------|
| Batch | `import_journal.status` | pending, completed, failed, undone | Recovery-Trigger („gibt es offene Batches?") |
| Aktion | `import_actions.action_status` | pending, completed, failed | Recovery-Detail („welche Aktion war wo abgebrochen?") |

**Begründung:** Bei Absturz während eines Imports (Stromausfall, App-Crash, Datei-Lock) muss der nächste App-Start unterscheiden können zwischen „kein Import läuft" und „Import war mittendrin". Der Journal-Status liefert den Trigger, der Action-Status liefert das Detail für eine punktgenaue Wiederaufnahme oder Reparatur.

**Implementierungsstand (Stand 2026-05):** Recovery ist vollständig implementiert. `RecoveryDecisionService` (pure Funktion, 5 Unit-Tests) entscheidet auf Basis der Action-Status-Verteilung über Forward / Rollback / Cleanup. `RecoveryExecutorService` (Disk + DB, 5 Integration-Tests) führt die gewählte Strategie aus. Die Recovery-Szenarien sind als manueller Smoketest unter `Docs/Test/Recovery-Szenarien.md` dokumentiert. Diese ADR friert das Datenmodell-Konzept ein, BPM-016 ist done (v0.27.23–v0.27.24, Branch `feature/bugfixing`).

### 8. Stage-Konzept als separater Aspekt

Jedes Dokument hat eine Stage im Review-/Freigabe-Lifecycle, gespeichert als eigenes Feld (nicht in `document_key`):

| Stage | Bedeutung | Erkennung |
|-------|-----------|-----------|
| `Unknown` | Default — keine Stage-Information vorhanden | Standardwert wenn keine Marker gefunden |
| `Draft` | VORABZUG, Vorab, VA | Ordnername (z.B. `_VORABZUG`) oder Dateiname enthält Marker |
| `Final` | Endgültig freigegeben | Hauptordner + expliziter Index ohne Draft-Marker |

**Wichtig:** Default ist `Unknown`, NICHT `Final`. Die ausdrückliche Trennung verhindert, dass undeklarierte Dokumente fälschlich als final eingestuft werden.

Stage ist **bewusst nicht Teil des `document_key`**. Begründung: Ein Dokument behält seine fachliche Identität auch wenn es vom Vorabzug zur finalen Version wird. Würde Stage in den Schlüssel einfließen, würde derselbe Plan in Draft- und Final-Phase als zwei verschiedene Dokumente erscheinen, was die Versionierung und Archivierung sabotieren würde.

### 9. tokenization als profilgebundene Vorbedingung

Die Bildung des `document_key` setzt eine korrekte Segmentierung des Dateinamens voraus. Diese ist im RecognitionProfile-Schema v2 als `tokenization`-Block konfigurierbar:

```json
"tokenization": {
  "delimiters": ["-", "_"],
  "collapseRepeatedDelimiters": false,
  "firstTokenDelimiter": null
}
```

| Feld | Zweck |
|------|-------|
| `delimiters` | Liste der Trennzeichen (profilgebunden, nicht global) |
| `collapseRepeatedDelimiters` | Mehrere aufeinanderfolgende Trenner als ein Trennblock behandeln |
| `firstTokenDelimiter` | Sondertrenner für das erste Token (z.B. Leerzeichen nach Plan-Code) |

Punkt (`.`) ist als Trenner profilgebunden zulässig, nicht global. Leerzeichen als globaler Splitter ist verboten — ausschließlich als `firstTokenDelimiter` erlaubt (kontrollierter Sonderfall).

**Begründung:** Verschiedene Büros/Statiker liefern Pläne mit unterschiedlichen Dateinamen-Konventionen (Polierplan-Format ≠ Statikplan-Format ≠ Architekt-Format). Eine globale Tokenization würde Profile gegenseitig stören. Profilgebundene Tokenization ist Voraussetzung dafür, dass `identityFields` aus den richtigen Segmenten gelesen werden — und damit dafür, dass `document_key` deterministisch wird.

### 10. includeInIdentity-Flag für Custom-Felder

Custom-FieldType-Felder im RecognitionProfile sind nur dann Teil des `document_key`, wenn das Profil sie explizit mit `includeInIdentity = true` markiert. Default ist `false`.

**Begründung:** Custom-Felder dienen oft nur der Anzeige oder Sortierung (z.B. Bauabschnitt-Bezeichnung), sind aber nicht identitätsbildend. Eine pauschale Aufnahme aller Custom-Felder in den Schlüssel würde fachlich gleiche Dokumente in unterschiedliche Identitäten zerteilen.

### 11. MultiplePlanNumbers → ReviewRequired in V1

Wenn der Parser im Dateinamen mehr als eine Plannummer erkennt, wird der Import für diese Datei nicht automatisch fortgesetzt:

- `Warnings += MultiplePlanNumbers`
- Status: `Unknown` oder `ReviewRequired`
- Kein Auto-Linking, kein Auto-Rename
- Kein Wizard zur Auswahl der „primären Nummer" in V1

Der Benutzer entscheidet manuell in der Import-Vorschau. V2+ kann hier eine geführte Wahl ergänzen.

**Begründung:** Mehrfach-Plannummern sind in der Praxis selten und uneindeutig. Eine Heuristik („nimm die erste") wäre falsch oft genug, um Vertrauen in die automatische Zuordnung zu untergraben. Manuelle Review ist in V1 die ehrliche Lösung.

**Konsequenzen:**

- 6 Tabellen in `planmanager.db`: `plan_revisions`, `plan_files`, `revision_file_links` (Cache-Schicht) + `import_journal`, `import_actions`, `import_action_files` (Journal-Schicht). Implementiert in `PlanManagerDatabase.cs` (v0.25.13, Phase F).
- 7-Stufen-Analyse-Pipeline ist die einzige Quelle der `document_key`-Bildung: Scan → Fingerprint → Parse → Resolve Context → Build Identity → Version Decision → Execution Plan.
- RecognitionProfile-Schema v2 (Pflicht) mit `tokenization`, `indexExtraction`, `documentTypeId`, `includeInIdentity`. Migration v1→v2 erfolgt beim Laden alter Profile.
- `md5_hash NOT NULL` auf allen Dateien — Migration bestehender Profile/Imports muss Hashes nachpflegen.
- Recovery-Logik (BPM-016) baut auf der zweistufigen Status-Struktur auf — implementiert in `RecoveryDecisionService` + `RecoveryExecutorService` (v0.27.23–v0.27.24, 10 automatisierte Tests).
- Stage und Custom-Identity-Felder sind eigenständige Aspekte, die orthogonal zur `document_key`-Bildung wirken — keine versteckten Kopplungen.
- DocumentKey-Format („`A_B_C`") ist ein Implementation Detail des `DocumentKeyBuilder`. Wenn `planIndex` leer/null ist, wird das Feld NICHT angehängt (kein leerer Trenner-Tail).

**Verworfene Alternativen (zusammengefasst):**

- 2-Tabellen-Import-Hierarchie ohne separate Datei-Ebene → ADR-007 widerspricht
- Dateiname/Pfad als Identity → in der Praxis nicht eindeutig
- 1:n Verknüpfung mit nullable revision_id → bildet Sammel-DWG nicht ab
- md5_hash optional bei IndexSource=FileName → unterläuft SKIP/Cache/Veraltet-Warnung
- Stage als Bestandteil des document_key → bricht Versionierung über den Lifecycle
- Globale Tokenization → stört Profile gegenseitig
- Auto-Wahl bei MultiplePlanNumbers → Heuristik-Risiko zu hoch für V1

**Betrifft:** ADR-007 (1..n Dateien pro Revision), ADR-008 (10-Schritte Import-Workflow), ADR-009 (Undo-Journal), ADR-010 (RecognitionProfile/PatternTemplate), ADR-022 (Segment-Parsing), ADR-039 (ULID), ADR-045 (IndexSource), ADR-046 (.bpm/-Ordner)

**Offen / spätere ADRs:**

- IndexSource=PlanHeader und document_key-Bildung bei nachträglich erkanntem Index (Post-V1, mit Plankopf-Modul)

---

## ADR-055: IPersistenceRegistry — dynamisches Persistenz-Inventar als Single Source of Truth

**Datum:** 2026-05-04
**Status:** ✅ Entschieden
**Implementierung:** ✅ Implemented (v0.28.13–v0.28.16)
**Herkunft:** BPM-104 — DevTools-Reset deckte nur 2 von 11 Persistenz-Punkten ab; statische Doku-Tabelle in DB-SCHEMA.md driftet von Code-Realität ab.

**Kontext:**

BPM persistiert in mindestens 11 verschiedenen Files (DBs, Configs, Logs, Projekt-spezifische Files). Bisher gab es nur eine statische Auflistung in `DB-SCHEMA.md` Kap. 10.1, die nicht alle Files (z.B. Logs, planmanager.db, profiles) abdeckte. DevTools-Reset operierte hartkodiert auf 2 Pfaden. Folge: User konnte nicht granular reset, Doku driftete vom Code ab.

**Entscheidung:**

Hybrid-Persistenz-Inventar:

1. **In-Memory Registry** (`IPersistenceRegistry`, Singleton via DI): Services registrieren ihre Persistenz-Files beim Init mit `Register(PersistenceEntry)`.
2. **Filesystem-Scan** (`RescanFilesystem`): ergänzt nicht-registrierte / verwaiste Files (z.B. alte Logs, Profiles aus inaktiven Projekten) durch Scan bekannter Patterns:
   - `%LocalAppData%\BauProjektManager\*` (Configs, DBs)
   - `%LocalAppData%\BauProjektManager\Logs\BPM_*.log`
   - `%LocalAppData%\BauProjektManager\Projects\*\planmanager.db`
   - `<BasePath>\.AppData\BauProjektManager\*` (CloudShared)
   - `<ProjectRoot>\.bpm\` rekursiv (ProjectLocal)

`PersistenceEntry` enthält DisplayName, AbsolutePath, Type (Database/Config/Log/ProjectData/Cache/Other) und Scope (Local/CloudShared/ProjectLocal).

DevTools liest Inventar dynamisch im Reset-Tab und zeigt es mit Multi-Select-Checkboxen + Pro-Item Aktions-Buttons (Ordner öffnen / Datei öffnen mit Standard-App / Im Explorer markieren).

**Konsequenzen:**

- Code = Single Source of Truth. Doku driftet nicht mehr.
- Neue persistierende Services müssen `Register()` aufrufen ODER ihr Pfad-Pattern muss vom FS-Scan abgedeckt sein.
- DB-SCHEMA.md Kap. 10.1 listet nur die wichtigsten Patterns als Übersicht — konkrete Liste kommt zur Laufzeit aus Registry.
- FS-Scan-Patterns müssen bei neuen Persistenz-Zonen (z.B. wenn .bpm/cache/ eingeführt wird) erweitert werden.

**Betrifft:** ADR-013 (.bpm-manifest), ADR-046 (.bpm/-Ordner), ADR-052 (Settings-Split), DB-SCHEMA.md Kap. 10.1

---

## ADR-056: Segmenttyp-Architektur (BPM-108) — `fieldTypeId` + `SemanticRole` Zwei-Schichten-Modell

**Datum:** 2026-05-18
**Status:** ✅ Entschieden (Sign-off via CGR-2026-05-12-segmenttyp-architektur r3)
**Implementierung:** Phase A ✅ Implemented (v0.28.44) · Phase B ✅ Implemented (v0.28.45) · Phase C ✅ Implemented (v0.28.46–v0.28.50)
**Herkunft:** BPM-108 — erkannt im Zuge BPM-080.05 Schritt 2 (Token-Drag&Drop, "+ Eigenes"-Chip). `FieldType`-Enum hardcoded an mehreren Stellen, User kann weder eigene Typen anlegen noch Reihenfolge ändern. Cross-Review mit ChatGPT (3 Runden) führte zur unten beschriebenen Architektur.

**Kontext:**

Im PlanManager war `FieldType` (Enum) bisher gleichzeitig:
- UI-Katalog (Wizard Schritt 2 Chip-Liste, Theme-Farben)
- Fachlicher Schlüsselname in JSON-Profilen (`segments[].fieldType`)
- Pflichtfeld-Trigger (`PlanNumber` muss zugewiesen sein)
- Identity-Trigger (`PlanNumber` + `Haus` + `Bauteil` → `identityFields`)
- Hierarchie-Auswahl (Schritt 4: `Geschoss`/`Haus`/`Bauteil`/…)
- Rename-/Folder-Template-Token (`{plan_number}`)
- Variable-Segment-Heuristik (`PlanNumber`/`PlanIndex`/`Date`)

User-eigene Klassifikationen ("Akustik-Klasse", "Brandschutzklasse") waren nicht persistierbar. Built-in-Reihenfolge nicht änderbar. Built-ins nicht deaktivierbar.

**Entscheidung:**

Einführung eines Zwei-Schichten-Modells in `bpm.db` (Tabellen `segment_type_groups` + `segment_types`):

1. **`fieldTypeId`** — persistente Referenz pro Segmenttyp.
   - Built-in: snake_case String (z. B. `plan_number`, `geschoss`)
   - Custom: ULID via `IIdGenerator`
   - Unveränderlich nach Anlage

2. **`SemanticRole`** — kleine Enum für fachliche Sonderfälle (`None`, `PlanNumber`, `PlanIndex`, `ProjectNumber`, `Date`, `Description`, `Spatial`, `Ignore`).
   - Bei Built-ins seed-definiert und im Manager **read-only**
   - Bei Custom-Typen **immer `NULL`** (rein dekorative Klassifikation)

3. **`token_key`** — separate, stabile Schreibweise für Templates (`renameSchema`, `folderHierarchy`). Bei Built-ins identisch zu `id`. Bei Custom-Typen aus Namen generiert (snake_case mit Konflikt-Suffix). Unveränderlich nach Anlage.

4. **Built-ins editierbar** für `name`, `color`, `group_id`, `sort_order`, `is_active`. Update-Policy via `user_modified_*`-Flags: App-Update überschreibt nur nicht user-modifizierte Felder.

5. **Soft-Delete only.** Profile referenzieren `fieldTypeId`; gelöschte/deaktivierte Typen werden bei Wizard-Reopen mit Badge gerendert. Lookup ist ungefiltert. Auto-Import wird bei Missing-ID in `identityFields`/`folderHierarchy`/`renameSchema`/`indexExtraction` blockiert (`ProfileHealth = MissingSegmentTypes`).

6. **`RecognitionRule` bleibt unverändert** (BPM-082-kompatibel: `method`/`pattern`/`segmentPosition`). `fieldTypeId` gehört in `segments[]`, nicht in `recognition[]`.

7. **Frühphase = Reset.** Schema-Version 4 für JSON-Profile (strikt, keine Migration). Alte Profile werden via explizitem DevTool-Befehl nach `<project>/.bpm/profiles/_archiv/schema-reset-YYYYMMDD-HHMMSS/` verschoben. `pattern-templates.json` analog. Normaler Loader verwirft `schemaVersion != 4`.

**Spatial-Built-ins** (`SemanticRole.Spatial`): `geschoss`, `haus`, `bauteil`, `bauabschnitt`, `stiege`, `zone`, `block`, `achse`, `objekt`.

**Konsequenzen:**

- Wizard-Validierung (Pflicht/Identity/Hierarchie/Variable) prüft `SemanticRole`, nicht den `FieldType`-Enum.
- Custom-Typen sind nie identitätsbildend (keine versehentliche Identity-Drift).
- Built-in-Namen können vom User umbenannt werden (z. B. „Plannummer" → „Plan-Nr."), die fachliche Rolle bleibt intakt — der Manager zeigt die Rolle read-only mit Warntext „Wird automatisch Teil der Dokument-Identität".
- DSGVO: Klasse A (UI-/Profilkonfiguration, kein Personenbezug). `SegmentTypeDto` ist whitelist-fähig für ADR-053-Sync.
- Sync-Reihenfolge topologisch: `segment_type_groups` → `segment_types` → Profile.

**Implementierungsphasen:**

- **Phase A (BPM-108):** Domain (`SegmentTypeDefinition`, `SegmentTypeGroupDefinition`, `SegmentSemanticRole`) + SQLite-Tabellen + `ISegmentTypeRepository` + `ISegmentTypeCatalog` + Seed-Service.
- **Phase B:** Profilformat v4 (`ProfileSegment.FieldTypeId`), `IdentityFields`/`FolderHierarchy`/`RenameSchema` auf IDs/`token_key`, `ProfileManager.Load` strikt v4, `ProfileHealth`-Validator, DevTool-Archivierung.
- **Phase C:** `ProfileWizardViewModel` auf `ISegmentTypeCatalog`, `FileNameSegment.FieldTypeId`, Inline-Popover „+ Eigenes" mit Token-Vorschau, Manager-Dialog mit Built-in-Rollenanzeige read-only.

**Betrifft:** ADR-010 (Plan-Erkennung — Profile-Format), ADR-050 (Sync-Felder), ADR-053 (Server-Sync), DB-SCHEMA.md Kap. 4 (neue Tabellen), PlanManager.md Kap. 13/14.

**Referenz:** [Docs/Referenz/chatgpt-reviews/CGR-2026-05-12-segmenttyp-architektur/](../Referenz/chatgpt-reviews/CGR-2026-05-12-segmenttyp-architektur/) — 3 Runden Cross-Review mit ChatGPT GPT-5.4 (16 Dateien).

---

## ADR-058: Plan-Archiv-Persistenz (BPM-109) — Drei-Ebenen-Modell + Foundation Slice

**Datum:** 2026-06-08
**Status:** ✅ Entschieden (Sign-off via CGR-2026-06-08-plan-archiv-architektur r2)
**Implementierung:** Not Started — Foundation Slice (.01–.04 + .05a Stub) ist V1-Sperrposten
**Herkunft:** BPM-109 — erkannt im Chat Teil 41 (Bautagesbericht-Use-Case „zeige damals aktuelle Pläne für Haus H1 EG"). Cross-Review mit ChatGPT (2 Runden) führte zur unten beschriebenen Architektur und Foundation-Slice-Begrenzung.

**Kontext:**

Das aktuelle PlanManager-Schema (v1.0, 6 Tabellen) speichert Plan-Identität als verketteten String (`document_key` = `"polierplan|103|h5|gr|e1"`) und sortiert Pläne in eine Ordnerhierarchie. Es reicht für V1-Import + Sortierung, aber nicht für zeitbezogene Cross-Modul-Abfragen, die mehrere geplante Module (BPM-056 Bautagebuch, BPM-057 Foto, BPM-061 Vorlagen) benötigen:

- Filter „alle Polierpläne für Haus H1 + Geschoss EG am 15.06.2025" geht heute nur über `LIKE '%h1%'` — fehleranfällig, langsam, fängt `H10`/`EH1`/`Stiege H1` mit.
- Status-Wechsel `current → archived` hat keinen Zeitstempel → Zeitreise unmöglich.
- Cross-Modul-Verknüpfungen (Bautagebuch-Fußnote → Plan-Revision) brauchen stabilen FK, nicht Identitäts-String.

**Entscheidung:**

Drei-Ebenen-Modell analog Industrie-Standard (Procore, Aconex, think project!) — aber **als Foundation Slice**, nicht als kompletter Plattform-Refactor.

1. **`plan_documents` (NEU)** — logisches Dokument über alle Revisionen hinweg. FKs `building_part_id` + `building_level_id`. `document_key UNIQUE`. Stabile Entität, auf die Cross-Modul-Links zeigen.

2. **`plan_revisions` (UMGEBAUT)** — FK auf `plan_documents`. `revision_status` CHECK `current/superseded/rejected`. Zeitstempel `current_from` + `superseded_at` für Zeitreise. UNIQUE-Index auf `(document_id) WHERE status = 'current'`.

3. **`plan_document_segments` (NEU)** — KV-Tabelle für extrahierte Segmentwerte (haus, geschoss, bauteil, …) mit FK auf `segment_types` aus ADR-056. Spalten `segment_key` (Denormalisierung für Debug) + `raw_value` + `normalized_value`.

4. **`plan_revision_events` (NEU)** — minimaler Audit-Trail für Statuswechsel. CHECK `event_type IN (created/made_current/superseded/file_linked/manual_override)`. Kein voller Before/After-Snapshot pro Spalte.

5. **`plan_context_links` (NEU)** — Cross-Modul-Verknüpfung. **PFLICHT: `resolution_mode = 'fixed_revision'`** — `target_revision_id` wird beim Speichern eines Berichts/Fotos festgezogen, nicht dynamisch `current_at_time` aufgelöst. Sonst verändern rückwirkende Korrekturen alte Berichte.

6. **`building_part_aliases` (NEU)** — relational, nicht JSON. Auto-Learn-Mapping für Stammdaten. Stufe 1: exakte Normalisierung + Preview-Warnung bei fehlendem Match. Kein Fuzzy-Match, kein Auto-Anlegen ohne User-Bestätigung.

7. **Bestehend bleibt:** `plan_files`, `revision_file_links`, `import_journal`, `import_actions`, `import_action_files` — Import-Journal speichert „was hat dieser Import gemacht", `plan_revision_events` speichert „was ist mit dieser Revision passiert". Beide existieren parallel.

8. **`IPlanLookupService`** — öffentliche API für konsumierende Module:
   - `FindCurrentPlansAsync(projectId, buildingPartId, buildingLevelId, documentTypeIds, atUtc)` — Zeitreise-Query
   - `CreatePlanContextSnapshotAsync(sourceModule, sourceId, atUtc, filters)` — schreibt `plan_context_links` mit `fixed_revision`

9. **Foundation Slice = V1-Sperrposten:**
   - `.01 Schema v2 neu erzeugen`
   - `.02 Domain Models + Repository`
   - `.03 Pipeline-Grundgerüst` (Import schreibt Document + Revision + Segments)
   - `.04 Revision-Zeitlogik` (`current_from`, `superseded_at`, Events)
   - `.05a IPlanLookupService Interface-Stub` (nur Vertrag, keine Implementation)

10. **Nicht V1-blockierend (post-V1):**
    - `.05 IPlanLookupService Implementation` mit Query-Logik — parallel zu BPM-056
    - `.06 Stammdaten-Mapping mit Preview-UI`
    - `.07 vollständige Doku/GLOSSAR/BACKLOG/Architektur-Update`
    - `plan_context_links` aktiv nutzen (kommt mit BPM-056)
    - Alias-Verwaltung-UI
    - Bautagebuch-/Foto-/Vorlagen-Integration

11. **Frühphase = Reset.** Keine Migration. Bei Schema-v2-Einführung: User löscht `planmanager.db`, BPM erstellt sie beim nächsten Start neu. Schema-Reset für Profile (`.bpm/profiles/*.json`) ist NICHT nötig — die JSON-Format-Definition bleibt v4 (siehe ADR-056).

12. **Stop-Punkte für Foundation-Slice-Sprint:**
    - Schema-v2 erfordert >30 % Re-Design von BPM-080.05 → Stopp, Plan-Archiv nach V1 schieben
    - >40 Pipeline-Tests gebrochen + Ursachen nicht lokal auf Repository → Stopp
    - Import-Journal/Undo wackelt → **sofort** Stopp
    - Dateiverschiebung + DB-Commit inkonsistent → **sofort** Stopp
    - `.01–.04` dauern >10 PT → Foundation Slice gescheitert

**Konsequenzen:**

- BPM-080.05 (Wizard-WPF) und BPM-081 (ImportPreviewDialog) **komplett pausiert** bis `.01–.04` durch — auch UI-Layer ruht, um Wegwerfware zu vermeiden.
- BPM-006 (ProjectDetailView) kann parallel laufen (UI-Polish ohne Persistenzbezug).
- V1-Release verzögert sich um ~1–2 Wochen (geschätzte 8,5–10,5 PT Aufwand für Foundation Slice).
- `RecognitionRule` aus BPM-082 bleibt unverändert (Recognition-Logik separat von Persistenz). ADR-010 bekommt nur Hinweis zur `document_key`-FK-Bedeutung.
- ADR-053 (Sync-Strategie) bekommt Hinweis zur `project_id`-Redundanz in `planmanager.db`.
- BPM-092 (`recognition_profiles` in DB) ist nicht Voraussetzung — kommt unabhängig nach diesem Ticket.
- Cross-Modul-Snapshots auf `revision_id` (nicht `document_id + current_at_time`) sind **fachliche Invariante**: ein historischer Bericht muss immer dieselbe Revision zeigen, auch nach Korrekturen.

**DSGVO:** Klasse A (technische Persistenz, kein Personenbezug). `PlanDocument`/`PlanRevision`/`SegmentValue` sind whitelist-fähig für ADR-053-Sync.

**ISO 19650 / Industrie-Standard:**

Das Modell entspricht den Drei-Ebenen-Mustern von Procore (Drawing/Revision/Sheet), Aconex (Document/Revision/File) und think project! (CONCLUDE CDE). Bewusst NICHT übernommen:
- Suitability-Codes (S0–S4) — für Polier-Alltag overkill
- Transmittals / Freigabe-Workflows — kein Versand-/Approval-Portal
- Generic Custom-Fields — Segmenttypen aus ADR-056 reichen
- OCR-Plankopf als Persistenz-Voraussetzung — `IndexSource = PlanHeader` bleibt post-V1
- Revision-Branching — Baupläne sind linear genug (`current → superseded`)

**Betrifft:** ADR-010 (Recognition — `document_key` jetzt FK-Bezug), ADR-050 (Sync-Felder — 6 Spalten auf neuen Tabellen), ADR-053 (Server-Sync — `project_id`-Redundanz), ADR-056 (Segmenttypen — `plan_document_segments.segment_type_id` FK), DB-SCHEMA.md Kap. 6, PlanManager.md (7-Stufen-Pipeline + Document-Resolve-Stage).

**Referenz:** [Docs/Referenz/chatgpt-reviews/CGR-2026-06-08-plan-archiv-architektur/](../Referenz/chatgpt-reviews/CGR-2026-06-08-plan-archiv-architektur/) — 2 Runden Cross-Review mit ChatGPT GPT-5.4 (11 Detail-Verbesserungen R1 + 4 Roadmap-Korrekturen R2 übernommen).

---

### ADR-058-Addendum: Cross-DB Soft References (2026-06-08, CGR r3)

**Status:** ✅ Entschieden (Sign-off via CGR-2026-06-08-plan-archiv-architektur r3)
**Herkunft:** Beim Vorbereiten von BPM-109.01 aufgefallen — die in DB-SCHEMA Kap. 6.7 / ADR-058 skizzierten neuen Tabellen in `planmanager.db` deklarierten FKs auf `building_parts`/`building_levels`/`segment_types`, die in `bpm.db` liegen. Cross-Review r3 (Claude + ChatGPT GPT-5.4) bestätigte Option A.

**Problem:** `planmanager.db` (per-Projekt-Cache) und `bpm.db` (zentrale Stammdaten) sind getrennte SQLite-Dateien. **SQLite erzwingt keine Foreign Keys über getrennte Datenbankdateien hinweg** — auch `ATTACH` aktiviert keine Cross-DB-FK-Constraints. Eine DDL, die harte FKs aus `planmanager.db` auf `bpm.db`-Tabellen deklariert, ist technisch falsch.

**Entscheidung (Option A — 2 DBs behalten):**

1. **Architektur-Invariante:** `planmanager.db` bleibt pro Projekt lokale, rebuildbare PlanManager-Cache-/Journal-DB. Bezüge auf `bpm.db`-Tabellen sind **logische Referenzen** (`TEXT`-Spalten ohne `FOREIGN KEY`), die durch Import-/Lookup-Services validiert werden. Harte FKs bleiben **nur innerhalb derselben DB-Datei**.

2. **Muster:** *„System-of-record DB + rebuildable bounded cache DB"* — **nicht** „Database per Module" (das wäre ein Anti-Pattern beim modularen Monolithen). Der Split `bpm.db` ↔ `planmanager.db` ist durch **Disposability + Projektkardinalität + Sync-Politik** gerechtfertigt, nicht durch Modul-Trennung. Ein künftiges Modul (z.B. Foto) bekommt nur dann eine eigene DB, wenn es ebenfalls einen rebuildbaren lokalen Cache mit eigenem Lebenszyklus braucht.

3. **Keine Konsolidierung vor V1:** Plan-Tabellen nach `bpm.db` zu verschieben (Option B) wurde verworfen — zu wenig Nutzen (4 FKs) für zu viel Sync-/Reset-/Blast-Radius-Kosten (~5–8 PT netto + Kollision mit den Foundation-Slice-Stop-Punkten). Würde nur sinnvoll, wenn PlanManager-Daten zu primären (nicht-rebuildbaren) Records werden — dann eigenes ADR „PlanArchive as System of Record".

**Drei Sub-Entscheidungen (CGR r3):**

- **(a) `building_part_aliases` → `bpm.db`** statt `planmanager.db`. Damit zentral, gesynct, mit **hartem FK** auf `building_parts(id)` (gleiche Datei), plus `project_id` + Sync-Felder (ADR-050). Reduziert die Cross-DB-Soft-References von 4 auf **3** (verbleibend: `plan_documents.building_part_id`, `plan_documents.building_level_id`, `plan_document_segments.segment_type_id`). Siehe DB-SCHEMA Kap. 4.11.

- **(b) Stammdaten-Löschung mit Planbezug = Soft-Delete + Warnbadge.** Löschen von `building_parts`/`building_levels` mit aktiven Planreferenzen wird **nicht hart blockiert** (konsistent mit ADR-050/ADR-056 Soft-Delete-Policy), sondern erlaubt mit Warnung + Badge im PlanManager. Der App-Level Delete Guard ist **post-V1**; im Foundation Slice nur als Invariante dokumentiert.

- **(c) Doku-Vehikel = dieses ADR-058-Addendum** (kein eigenständiges ADR-059), plus DDL-Korrektur in DB-SCHEMA Kap. 6.7.

**Service-Härtung (Scope):**
- **BPM-109.01:** DDL-Fix (Cross-DB-FK-Klauseln raus, SoftRef-Kommentare + Cross-DB-Hinweis), harte Innen-FKs erhalten.
- **BPM-109.03:** Import-Time-Validation (`ResolveBuildingPart`/`ResolveSegmentType` gegen `bpm.db`; deckt sich mit ADR-056-Health-Logik).
- **post-V1:** App-Level Delete Guard, `PlanReferenceHealth`-Revalidate-Command, `ATTACH bpm.db`-Kapselung ausschließlich in `IPlanLookupService` (kein UI-/Repo-SQL über die Grenze).

**Offener Punkt:** `plan_context_links` ist **kein** rebuildbarer Cache, sondern autorierte Cross-Modul-Verknüpfung (nicht aus Dateisystem rekonstruierbar). Spannung zum „disposable cache"-Modell. Für den Foundation Slice bleibt die Tabelle wie in ADR-058 in `planmanager.db` (nur angelegt, aktiv erst mit BPM-056). **Heimat neu bewerten, wenn BPM-056-Sync kommt.**

**Erweiterung Drei-Zeiten-Modell (BPM-109.04/.04b, Teil 42):** Eine Plan-Revision trägt drei Zeiten:
- **`current_from`/`superseded_at`** — technisches Gültigkeitsfenster (Supersede-Kette). Invariante: `superseded_at`(alt) == `current_from`(neu), ein `actionTime` pro Import-Aktion → Zeitreise lückenlos.
- **`received_at`** — Hinzufügedatum (Import), immer bekannt.
- **`released_at`** — Freigabedatum des Index (fachlich präziser). Quellen-Priorität: **Plankopf-OCR (post-V1) > manuell (post-V1) > Dateiname (selten)**; `NULL` solange unbekannt. Spalte ab v0.28.62 reserviert (Frühphase = keine Migration), Befüllung post-V1.
- **Bautagebuch-Regel (post-V1):** effektives Datum = `released_at` wenn vorhanden, sonst `received_at`; bei Fallback **visuell markiert** (Farbe + Hinweis „Importdatum"). Geliefert via `IPlanLookupService` (`EffectiveDate`/`IsDateFallback`). Damit bleibt die `fixed_revision`-Invariante kompatibel: ein Bericht zeigt die festgezogene Revision mit ihrem effektiven Datum.

**Referenz:** CGR-2026-06-08-plan-archiv-architektur **r3** (DB-Grenze).

---

## ADR-059: Recognition v2 / Plan-Erfassung — Manuelle Erstaufnahme (Strategie B) + Radial-UI

**Datum:** 2026-06-09
**Status:** ✅ Entschieden (Sign-off via CGR-2026-06-09-plan-erkennung r3)
**Implementierung:** Not Started — Feldkey-Fix V1-blockierend, Radial-Erfassung V1, Auto-Extraktion post-V1
**Herkunft:** Praxis-Import Statik (5998er) in Teil 42 → positionsbasierte Erkennung sortiert falsch (`\1`, `\KG`, `\(1)`). 3-Runden-Cross-Review mit ChatGPT.

**Kontext:**

Das bisherige Erkennungs-Modell extrahiert Identitätsfelder (haus/geschoss/plannummer) **positionsbasiert** aus dem Dateinamen (`FileParseService` über `segDef.Position`). Bau-Plan-Dateinamen sind in der Praxis chronisch uneinheitlich (jedes Büro/jede Quelle anders, variable Token-Anzahl, Kopiermarker `(1)`, Index am Plannummer-Token geklebt). Voll-Auto-Erkennung ist damit prinzipiell gedeckelt. Zusätzlich erscheint dasselbe Bauteil als „Haus 64"/„H64"/„Haus66"/„H66".

**Entscheidung:**

1. **MVP = Strategie B (manuelle Erstaufnahme + deterministisches Matching), nicht Voll-Auto-Erkennung.** Der Mensch vergibt die fachliche Identität **einmal pro Plan**; die Maschine macht danach das eng begrenzte, zuverlässige Matching: MD5-Dublette → Skip, neuer Index eines bekannten Dokuments → neue Revision/Supersede, sonst → Erstaufnahme.

2. **Auto-Extraktion (Strategie A) ist nur Assist, nie Entscheider.** Harte Grenze: nur `ManualConfirmed` oder `ExistingDocumentMatch` dürfen schreiben/verschieben; `AutoSuggested` füllt nur Preview-Felder vor. (`enum ImportIdentitySource`.)

3. **`document_key` ID-basiert** aus manuell bestätigten Stammdaten (`document_type_id` + `building_part_id` [+ `building_level_id`] + `plan_number`), nicht aus Anzeigenamen/Alias. Zielordner = `building_parts.name` (kanonisch), nicht Alias/Dateiname.

4. **Matching-Semantik:** MD5 = Dublettenbeweis (≠ Revisionsbeweis); Plannummer = nur Suchanker; finale Identität über gespeicherten `document_key`/User-Bestätigung. Neue Dokumente ohne bestätigte Identität werden nie automatisch importiert.

5. **V1-UI = Radial-/Nautilus-Menü** als primäre Erfassungsgeste (Herberts Mockup `02_ManuellSortieren.html`, überarbeitet zu echter konzentrischer Ring-/Fächer-Geometrie). Bedingungen:
   - **Pending Assignments:** Radial schreibt nur einen Vorschlag-Zustand; finaler Import erst nach Preview/Bestätigung (Journal vor Dateioperationen bleibt).
   - **Harte Caps:** max. 3 Kaskadenringe (**Plantyp → Bauteil → Geschoss**); Plantyp ≤8 Segmente (~7 fix: Polierplan/Bewehrung/Schalung/Fertigteil/Doka-Schalung/Leica-Vermessung/Protokoll); Bauteil ≤8 direkt / 9–16 paginiert / ≥17 Favoriten+Quick-Filter / ≥25 Listen-Fallback Pflicht; Bulk 2–8 direkt / 9–20 Zusatzbestätigung / >20 Fallback.
   - **Capture-vs-Update-Buckets:** A Dubletten / B Update-Karten / C manuelle Erstaufnahme (→ Radial) / D Konflikte. Nur Bucket C öffnet das Radial; matched Updates überspringen es.
   - **Dauerhaftes rechtes Detail-Panel** als Kontroll-/Bulk-/Fallback-Fläche (PlanNummer/Index-Kandidaten, Zielpfad, editierbar).
   - **Undo zweistufig:** vor Import = Pending-Assignment rückgängig; nach Import = bestehendes Import-/Undo-Journal.

6. **Design-Detail-Entscheidungen (Herbert):** Geschoss als **3. Radial-Ring** (≤6 direkt, ab 7 Liste); Bauteil-Sortierung **kontextbasiert** (Kandidat + zuletzt verwendet, dann natural sort); „+ Bauteil" als **Inline-Schnellanlage** (+ Link zu Projekt-Einstellungen); PDF+DWG default **„eine Revision"**-Vorschlag (im Panel bestätigt); Listen-Fallback als **dauerhaftes Panel** (kein separater Dialog).

7. **Kombi-Pläne (mehrere Plantypen in einer Datei):** in V1 KEIN Auto-Split in mehrere `plan_documents`; stattdessen Plantyp „Kombiplan/Sonstiges" + Tags/`plan_document_segments`. Aufspaltung nur manuell/ausdrücklich.

**Scope V1 vs post-V1:**

- **V1-MUSS:** Feldkey-Bug-Fix (s.u.); manuelle Erstaufnahme als Workflow (Radial + Panel + Buckets); Lightweight PlanNummer/Index-Kandidaten-Extractor (+ Kopiermarker-Strip); `document_key` aus Stammdaten-IDs; plan_documents/revisions/files sauber schreiben; MD5-Dublette; Update-Vorschlag gegen bekanntes Dokument; Supersede/Journal (BPM-109).
- **Post-V1:** frei konfigurierbare `FieldExtractionRule` (Regex-Named-Captures, BPM-007.02/.03 großer Teil); Alias-Mapping (BPM-109.06, `building_part_aliases`); OCR/Plankopf (+ `released_at`-Befüllung); echte Zero-Touch-Erkennung; „Bauteil fixieren"-Modus; Matrix-/Board-Komfortansicht.

**Feldkey-Bug (V1-blockierend, strategie-unabhängig):** `FileParseService` schreibt `extractedFields` mit Key = `FieldTypeId` (`plan_number`/`plan_index`), `ImportWorkflowService` liest `plannumber`/`planindex` → `ClassifiedImportFile.PlanNumber` + `RevisionToken` sind null. Fix: zentrale `SegmentTypeIds`-Konstanten, beidseitig verwenden.

**Konsequenzen:**

- BPM-007.02/.03 werden gesplittet (LightweightPlanTokenExtractor V1 / FieldExtractionRule post-V1).
- BPM-109.06 (Alias) + OCR-/KI-Modul sind explizit post-V1, nicht V1-blockierend.
- BPM-080.05 (ProfileWizard) verliert an Gewicht für V1 (Auto-Profile ist nicht mehr der MVP-Kern); Schwerpunkt verschiebt sich zur manuellen Erstaufnahme-UI.
- Neuer V1-Task „Manuelle Erstaufnahme + Radial-UI + Pending Assignments + Bucket-Matching".
- Mockup `02_ManuellSortieren.html` zu echter Ring-/Fächer-Geometrie überarbeiten.

**DSGVO:** Klasse A (technische Erkennung/Sortierung, kein Personenbezug). Cloud-KI bewusst vermieden (offline-first + Plandaten); falls OCR/LLM, dann lokal (ONNX/Tesseract/Windows.Media.Ocr) — neue Libraries nur mit Freigabe.

**Betrifft:** ADR-010 (Recognition-Profile), ADR-022 (Dateiname-Parsing), ADR-056 (Segmenttypen), ADR-058 (Schema v2.0 — trägt B unverändert), PlanManager.md, DB-SCHEMA.md Kap. 6.7.

**Referenz:** [Docs/Referenz/chatgpt-reviews/CGR-2026-06-09-plan-erkennung/](../Referenz/chatgpt-reviews/CGR-2026-06-09-plan-erkennung/) — 3 Runden Cross-Review mit ChatGPT GPT-5.4 (r1 Feld-Extraktionsmodell + Feldkey-Bug, r2 Strategie-Pivot A→B, r3 Radial-UI Sign-off).

### ADR-059-Addendum: Typabhängiges Unterteilungs-Schema + Dokumenttyp-Stammdaten (2026-06-11, Teil 43)

**Kontext:** Die Mockup-Iteration zu BPM-111.01 ergab, dass Ring 2 des Radials NICHT immer „Bauteil" ist: Protokolle unterteilen nach Protokollart (Baubesprechung/Bautagesbericht/Sicherheit/Abnahme), Fertigteilpläne nach Kategorie (Wände/Decken/Stiegen). `building_parts`/`building_levels` existieren bereits als Stammdaten in `bpm.db` (Kap. 4.4/4.5) — was fehlt, sind Dokumenttyp-Stammdaten mit Unterteilungs-Konfiguration.

**Entscheidungen:**

1. **Neue Tabelle `document_types` in bpm.db** (projekt-scoped wie `building_parts`, harter FK auf `projects`, ULID + 6 Sync-Spalten nach Kap. 9.3): `name`, `folder_name`, `color_hex` (Radial-Segmentfarbe), **`ring2_source` CHECK IN (`building_parts`, `categories`, `none`)**, `sort_order`, `is_builtin`. Ring 3 (Geschoss) gibt es implizit nur bei `ring2_source='building_parts'` (`building_levels` je Bauteil).
2. **Neue Tabelle `document_type_categories` in bpm.db** (harter FK auf `document_types`): typgebundene Kategorien (`name`, `folder_name`, `sort_order` + Sync-Spalten).
3. **Verortung bpm.db, nicht planmanager.db** — konsistent mit dem ADR-058-Addendum (Stammdaten zentral + syncbar, `planmanager.db` bleibt Plan-Archiv). `plan_documents.document_type_id` bleibt Cross-DB-Soft-Reference (TEXT, kein FK); die Anzahl der Soft-Refs steigt nicht.
4. **Ordnernamen-Regel (verbindlich, aus Mockup-Spez):** `folder_name` wird genau EINMAL beim Anlegen erzeugt (`IPlanValueNormalizer.NormalizeForFolderName`) und gespeichert — Präfixe wie „00 " bleiben erhalten, Umbenennen des Anzeigenamens ändert den Ordnernamen nicht automatisch, nur die App legt Ordner an/um. Entscheidung: eigenes Feld statt Präfix-Template (Template optional später). **`building_parts` erhält dafür zusätzlich eine `folder_name`-Spalte** (bisher nur `short_name`).
5. **Seed bei Projektanlage** (= „Default-Ordnerstruktur ist der Seed der Stammdaten", DB ab dann führend): Built-in-Typen Polierplan `#185FA5`, Statik `#534AB7`, Bewehrung `#993C1D`, Schalung `#1F7280`, Architektur `#0F6E56` (alle `building_parts`) · Fertigteile `#6E6E6E` (`categories`: Wände/Decken/Stiegen) · Protokolle `#555555` (`categories`: Baubesprechung/Bautagesbericht/Sicherheit/Abnahme). „+ Neu…" in jeder Ringebene = Schnellanlage in diese Stammdaten + sofortige physische Ordner-Anlage; Pflege (Umbenennen/Löschen/Sortieren) in den Projekt-Einstellungen.
6. **document_key präzisiert:** räumlich = `document_type_id + building_part_id [+ building_level_id] + plan_number`; kategorial (Typen ohne Plannummer) = `document_type_id + category_id + (Datum ?? Dateiname)`. Die Erstaufnahme (CaptureConfirmService) stellt auf Stammdaten-IDs um, sobald die Ringe daraus gespeist werden (BPM-111.05 Slice 2).

**Frühphase:** Neue Tabellen sind additiv (`CREATE TABLE IF NOT EXISTS`); die `folder_name`-Spalte in `building_parts` ist eine Bestandsänderung → **Reset-Anweisung: bpm.db löschen, BPM legt sie beim nächsten Start neu an** (keine Migration, INDEX.md-Frühphasenregel).

**Konsequenzen:** DB-SCHEMA.md Kap. 4.12/4.13 neu + Kap. 4.4 ergänzt; Einstellungen Tab 2 braucht post-Slice eine Pflege-UI für Typen/Kategorien; BPM-111.05 Slice 2 baut den Ring-Daten-Service auf diesen Tabellen auf.

---

## ADR-060: Vereinheitlichte Dateisystem-Ports für alle Module

**Datum:** 2026-06-24
**Status:** ✅ Entschieden (Sign-off via CGR-2026-06-22-bpm-architektur, 4 Runden)
**Implementierung:** 🟡 In Progress — BPM-112: Slice 0 (FS-Ports + `LocalFileSystem`-Adapter + DI + `FakeFileStore`/Contract-Tests) done (v0.28.85, `e60fa3c`); Slices 1–6 (System.IO-Migration, ~29 Stellen) offen.
**Herkunft:** Live-Test BPM-111.05 (Teil 44) → Herberts Ausgangsfrage: braucht BPM ein vereinheitlichtes Dateisystem-Interface für alle Module? 4-Runden-Cross-Review mit ChatGPT GPT-5 Thinking.

**Kontext:**

`System.IO` (`Directory`/`File`/`Path`) ist über ~29 Dateien in allen Schichten verstreut — auch in Views/ViewModels (z.B. `ProjectEditDialog.xaml.cs`, `FolderTemplateControl.xaml.cs`, `SettingsViewModel.cs`). Kein vereinheitlichtes Interface → nicht testbar (kein Mock des Dateisystems), und Cloud-Sync-/DSGVO-/Pfadlogik fransen über die Codebasis aus. Es gibt einen `ProjectFolderService` und einen transaktionalen `ImportExecutionService`, aber keine gemeinsame Abstraktion.

**Entscheidung:**

1. **Ports & Adapters (Hexagonal).** Drei schmale Ports in `Domain.Interfaces`:
   - `IFileSystemReader` — `FileExists`/`DirectoryExists`/`EnumerateFiles`/`EnumerateDirectories`/`GetFileInfo` (→ `FileInfoSnapshot`)/`OpenRead`
   - `IFileSystemWriter` — `CreateDirectory`/`MoveFile`/`DeleteFile`/`CopyFile`
   - `IPathService` — `Combine`/`GetDirectoryName`/`GetFileName`/`GetExtension`/`GetRelativePath`
   Ein Adapter `LocalFileSystem` (Infrastructure) implementiert alle drei, via DI (Singleton) an alle Module. **Kein direktes `System.IO` mehr außerhalb des Adapters.**

2. **Eigenes schmales Interface statt `System.IO.Abstractions`** (Projektregel keine neuen Libraries ohne Freigabe; BPM braucht nur wenige kontrollierte Operationen mit BPM-Verhalten: Logging, DSGVO-Pfadmaskierung, Same-Volume-Prüfung, Cloud-Vorsicht). `FileInfoSnapshot`-Record statt `System.IO.FileInfo`.

3. **Zwei Zusatz-Ports für den In-App-Explorer:** `IFileLauncher` (`OpenFile` via ShellExecute / `OpenFolder` / `RevealInExplorer` / `CopyPathToClipboard`) — NICHT im File-Port. Später `IShareService` (Windows-Share-Sheet); echte Cloud-Share-Links bewusst **out-of-scope** (Provider-APIs, Online-Zwang, DSGVO/Berechtigungen).

4. **Schichtregel:** Views/ViewModels nie `System.IO`; PlanManager/Settings keine direkten `File`/`Directory`/`Path`-Aufrufe; Infrastructure = echte `System.IO`; Domain nur Interfaces/Modelle. Bestehende High-Level-Services (`ProjectFolderService`, `ImportExecutionService`, `RecoveryExecutorService`, `CaptureConfirmService`) bleiben fachlich, werden nur von direktem `System.IO` entkoppelt.

5. **Test:** In-Memory-Fake (`FakeFileStore`) für Unit-Tests (Pfad/Importplan/Recovery/Seed); Temp-Verzeichnis-Integrationstests für echte Move-/overwrite-/Lock-/Same-Volume-Semantik.

**Umsetzung (Slices):** 0 Ports+Adapter+DI · 1 Scanner/Reader (`ImportScanService`, Hash/MD5) · 2 Pfadberechnung · 3 transaktionaler Import (Hochrisiko, `ImportExecutionService`) · 4 DB-Pfade (`PlanManagerDatabase`/`ProjectDatabase` nur Pfad/Ordneranlage, NICHT die SQLite-Connection) · 5 Settings/Views + `ProjectFolderService` · 6 In-App-Explorer (erst nach stabilen Ports).

**Konsequenzen:**

- Testbar, zentral auditierbar, eine Stelle für Cloud-/Lock-/DSGVO-Pfadlogik.
- Etwas Boilerplate; Port klein halten (nur benötigte Operationen, kein Nachbau von `System.IO`).
- Eng verzahnt mit ADR-061 (der `DocumentTargetPathResolver` nutzt `IPathService`; der Import nutzt die Writer-/Reader-Ports).

**Alternativen verworfen:** `System.IO.Abstractions` (NuGet — jetzt nicht: Lib-Regel + zu breite Fläche); God-Interface `IFileStorage` (Coupling, schwer testbar).

**DSGVO:** Klasse A (technische Dateioperationen). Pfade/Dateinamen können Personenbezug tragen → DSGVO-Pfadmaskierung beim Logging zentral im Adapter.

**Betrifft:** ADR-053 (Server-Sync — Ports erleichtern spätere Remote-Adapter), ADR-061 (Resolver/Import nutzen die Ports), Architektur-Doc (Schichtgrenzen), DSVGO-Architektur.md.

**Referenz:** [Docs/Referenz/chatgpt-reviews/CGR-2026-06-22-bpm-architektur/](../Referenz/chatgpt-reviews/CGR-2026-06-22-bpm-architektur/) — 4 Runden Cross-Review (r1 Port-Zuschnitt/Testbarkeit, r2–r4 Slices/Sign-off). ClickUp: BPM-112.

---

## ADR-061: DB als einzige Ordner-Wahrheit + DocumentTargetPathResolver

**Datum:** 2026-06-24
**Status:** ✅ Entschieden (Sign-off via CGR-2026-06-22-bpm-architektur, 4 Runden)
**Implementierung:** ✅ Umgesetzt — BPM-113 komplett: Slices 0.1–0.6c done (v0.28.86–.98). `profile.TargetFolder` entfernt, `RecognitionProfile`/`PatternTemplate` SchemaVersion 5, Zielpfad ausschließlich über `DocumentTargetPathResolver`. Ergänzt/präzisiert ADR-059-Addendum.
**Herkunft:** Live-Test BPM-111.05 (Teil 44) — Radial-Import legte „Polierplan" an statt das vorhandene „01 Polierpläne" zu treffen.

**Kontext:**

Zwei getrennte Ordner-Wahrheiten, die sich nicht kennen: `AppSettings.FolderTemplate` erzeugt physische Plan-Ordner mit Positions-Präfix (`Polierpläne` → „01 Polierpläne"), während `document_types.folder_name` der normalisierte Typname war („Polierplan"). Zusätzlich war `profile.TargetFolder` eine dritte Wahrheit im klassischen Profil-Import. Ergebnis: Drift, neue Ordner statt Treffer der Vorlage.

**Entscheidung:** Nach Bootstrap ist die DB die EINZIGE fachliche Ordner-/Typ-Wahrheit; `FolderTemplate` ist nur Bootstrap-Quelle beim Projekt-Setup.

1. **Schema `document_types`:** + `key` (TEXT NOT NULL, `UNIQUE(project_id, key)`, nach Anlage gesperrt, ≠ UI-Name) + `root_relative_path` (TEXT NOT NULL, echter Root je Typ: „01 Planunterlagen" / „06 Protokolle"; CHECK `<> ''`) + `folder_name` (Typordner unter Root, **leer bei Root-Typ**). `document_type_categories.folder_name` = echter (ggf. präfixierter) Kategorieordner. **`building_levels` erhält `folder_name`** = `"{PrefixString} {Name}"` (z.B. `-01 KG` / `00 EG` / `01 OG1`), beim Anlegen erzeugt, rename-stabil (ON CONFLICT unangetastet).

2. **Template trägt Typ-Metadaten:** `FolderTemplateEntry` UND `SubFolderEntry` bekommen optionale Typ-Metadaten (`CreatesDocumentType`, `DocumentTypeKey`, `DocumentTypeDisplayName`, `Ring2Source?`, `Categories` mit `HasPrefix` je Kategorie). **Regel:** Ein Template-Node wird Dokumenttyp GENAU DANN wenn `CreatesDocumentType == true` (keine implizite Ableitung aus HasPrefix/Name/Position/Kategorien). Ein Hauptordner kann Container ODER Typ ODER beides sein (nur explizit). **Protokolle = eigener Root-Typ** (Hauptordner „06 Protokolle", `folder_name` leer). Der hardcodierte `_builtins`-Seed in `DocumentTypeSeedService` entfällt → Seed aus `FolderTemplate`, nur beim Setup.

3. **`DocumentTargetPathResolver`** (neuer Service in PlanManager): Zielpfad = `root_relative_path / folder_name(if!=leer) / Ring2 / Ring3 / fileName`, AUSSCHLIESSLICH aus DB-Stammdaten + erkannten/gewählten IDs. **Fail-Fast** bei fehlendem Ring-Wert (kein Teilpfad). Auflösung priorisiert: Id → key-exact → name/folder_name-exact-normalized → Fail. **KEIN Fuzzy** im Resolver (gehört in vorgelagerte Erkennung). `Ring2Source.BuildingParts` → `building_parts`/`building_levels` (projektspezifisch); `Categories` → `document_type_categories`; `None` → kein Ring2/3. `ResolvedDocumentTarget`-Record fließt durch `ImportDecision` bis `ImportExecutionService`.

4. **`profile.TargetFolder` wird gebrochen** (`RecognitionProfile` SchemaVersion 5, Feld entfällt, `DocumentTypeId` führend). Profil-Import + Radial-Erfassung konvergieren auf denselben Resolver. `ProfileWizard` wählt Dokumenttyp statt Zielordner. `ProjectPaths.Plans` bleibt nur Convenience/Navigation, NICHT Resolver-Input.

5. **Transaktionalität:** Journal VOR Move + temp-im-Zielordner (`.bpm_tmp`) + atomic rename (Temp→final im Zielordner) + idempotente Recovery. **Atomicity-Garantie gilt nur für den finalen Rename**; der Transfer aus dem globalen Eingang ist journalisiert/recovery-fähig. Cross-Root-Move (Eingang „01 Planunterlagen/_Eingang" → „06 Protokolle") unkritisch (gleicher Projektroot = gleiches Volume). Locks: einfacher Retry (3×). NICHT bauen: verteilte Locks, FileSystemWatcher-Sync-Engine, OneDrive-API, 2PC.

6. **DB-Scope = Modell A:** Die DB ist ein kuratierter Index NUR der bewusst erfassten Plandokumente (`plan_documents`/`plan_revisions`), KEIN Vollspiegel des Projektbaums. Der In-App-Explorer liest das Dateisystem live. Startup-Reconcile prüft NUR die getrackte Teilmenge (Exists+Size first, Hash nur bei Bedarf); Drift-Status `MissingOnDisk`/`ChangedOnDisk`/`RelinkCandidate`; MD5-Relink nur als Vorschlag, nie automatisch. Getrackte Dateien sind im Explorer nicht frei verschieb-/löschbar (nur über Journal-Service).

7. **„+ Neu…"-Schnellanlage (Radial) = kleiner MVP-Pflichtdialog** (Name + Ablagebereich-Dropdown Default „01 Planunterlagen" + Unterteilung Bauteil/Geschoss | Kategorien | Keine + editierbarer Ordnername). `key` auto-generiert aus Name + danach gesperrt; `folder_name` ohne Präfix bei User-Typen. Normalisierung in Creation-/Seed-Services, nicht in der Low-Level-DB-Methode. Ein globaler Import-Eingang in V1, Ziele in mehrere Roots.

**Scope-Grenze (Post-V1):** genau EINE Ring-2-Strategie pro Dokumenttyp. Kombinierte Hierarchien (Kategorie + Bauteil + Geschoss, z.B. `04 Fertigteilpläne/01 Wände/Haus A/00 EG/`) sind post-V1 (z.B. via `document_type_folder_segments`).

**Frühphase:** Schema-Änderungen an `document_types`/`building_levels`/`RecognitionProfile` → **Reset-Anweisung statt Migration:** `bpm.db` + projektbezogene `planmanager.db` + `.bpm/profiles/*.json` + ggf. `settings.json` löschen, BPM erstellt/seedet beim nächsten Start neu (INDEX.md-Frühphasenregel).

**Umsetzung (Slices):** 0.1 Domain Models → 0.2 DB-Schema → 0.3 ProjectDatabase → 0.4 Seed → 0.5 Resolver → 0.6 Import-Break (additiv zuerst, `TargetFolder` zuletzt entfernen, jeder Zwischenstand baubar). **Slice 3a aus BPM-111.05** („+ Neu…" im Ring, uncommitted, 346 Tests grün) geht in Slice 0 auf — die Typ-Erzeugung wird aufs neue Modell gehoben.

**Alternativen verworfen:** `root_key` abstrakt (zu kompliziert; `root_relative_path` passt zu offline-first); Mapping-Tabelle document_type→Ordner (zu viel Indirektion für V1); DB-Vollspiegel (Modell B — „halbes DMS"); Hybrid-Teilindex (Modell C — unscharfe Grenze); `profile.TargetFolder` behalten (dritte Wahrheit).

**Betrifft:** ADR-059 + Addendum (präzisiert die generische Seed-Definition), ADR-058 (Drei-Ebenen-Persistenz), ADR-060 (nutzt die FS-Ports), DB-SCHEMA.md Kap. 4.12/4.13 + Kap. 4.5 (building_levels.folder_name), PlanManager.md.

**Referenz:** [Docs/Referenz/chatgpt-reviews/CGR-2026-06-22-bpm-architektur/](../Referenz/chatgpt-reviews/CGR-2026-06-22-bpm-architektur/) — 4 Runden Cross-Review (r2 Schema/Import-Break, r3 Multi-Root/Protokolle, r4 Slice-0-Tiefe/Sign-off). ClickUp: BPM-113.

---

## ADR-062: Zentraler PDF-Render-Port (IPdfRenderService)

**Datum:** 2026-08-26
**Status:** ✅ Entschieden (Herbert, Teil 47) — Engine seit v0.28.121 PDFium/Docnet (siehe Addendum Engine-Konsolidierung)
**Implementierung:** 🟡 In Progress — Port + Vorschau umgesetzt (v0.28.112–.121): `PlanPreviewPanel` als integrierte Panel-Spalte (Variante B statt Fenster, v0.28.115), Plankopf-Start A4 rechts unten, Zoom/Pan/Blättern, `IFileLauncher`/`LocalFileLauncher`; Engine-Wechsel auf `PdfiumPdfService` (v0.28.121); offen: DWG-Pairing (C3 — „Andocken ans MainWindow" ist durch den Panel-Umbau entfallen)
**Herkunft:** BPM-111.06 Slice C (angedockte PDF-Vorschau) + Herberts Frage: zentrales PDF-System für alle Module statt Modul-Einzellösungen?

**Kontext:**

Die Plan-Vorschau (BPM-111.06) braucht In-App-PDF-Rendering; die Spez (Mockup 02_ManuellSortieren, ADR-059-Umfeld) legt `Windows.Data.Pdf` ohne Drittanbieter-Library fest. Weitere Module werden künftig PDFs anzeigen (Bautagebuch-Anhänge, Foto, KI-Assistent, post-V1 evtl. Bearbeitung). `Windows.Data.Pdf` ist eine WinRT-API und braucht ein Windows-SDK-TFM (`net10.0-windows10.0.xxxxx`) — würde jedes Modul die API direkt nutzen, müssten alle Referenzierer das TFM anheben und jedes Modul eigene PDF-Pfade pflegen.

**Entscheidung:**

1. **Port im Domain-Layer:** `IPdfRenderService` in `Domain.Interfaces` mit puren .NET-Signaturen — `Task<int> GetPageCountAsync(Stream pdf)` und `Task<byte[]> RenderPageAsPngAsync(Stream pdf, int pageIndex, int pixelWidth)`. Kein WPF-, kein WinRT-Typ im Port (PNG-Bytes statt `ImageSource`).
2. **Eine Implementierung im Composition Root:** `WindowsPdfRenderService` via `Windows.Data.Pdf` im App-Projekt, DI-registriert (Singleton). **TFM-Bump NUR App** auf `net10.0-windows10.0.19041.0` mit `SupportedOSPlatformVersion 10.0.17763` (Win10 1809 bleibt Mindest-OS). Module, Domain, Infrastructure, Tests behalten ihre TFMs.
3. **Module konsumieren nur den Port** (Constructor Injection) und wandeln die PNG-Bytes selbst in `BitmapImage`. Viewer-UI (Zoom, Andocken, Plankopf-Ausschnitt) bleibt Modulsache; ein gemeinsames Viewer-Control wird erst extrahiert, wenn ein zweites Modul es braucht.
4. **PDF-Bearbeitung = post-V1, eigener Port:** `Windows.Data.Pdf` kann nur rendern. Bearbeitung (Anmerkungen, Stempel, Formulare) bekommt später einen eigenen Port + Engine-Entscheidung (Drittanbieter → Freigabe-Regel + neues ADR). Durch den Port-Schnitt ist auch die Render-Engine austauschbar, ohne Module anzufassen.

**Konsequenzen:**

- Ein Ansprechpartner für PDF-Rendering in allen Modulen; Engine austauschbar; Module testbar (Port mockbar).
- Minimaler Build-Eingriff (nur App-TFM) statt solution-weitem Bump.
- PNG-Bytes-Roundtrip kostet etwas Speicher gegenüber direktem `WriteableBitmap` — bewusst in Kauf genommen (Entkopplung > Mikro-Optimierung; Vorschau rendert einzelne Seiten, keine Massen).

**Addendum (Teil 47, Herbert):** PDF-**Bearbeitung** erfolgt bewusst dauerhaft **extern** — das Vorschau-Fenster bietet „In Standard-App öffnen" (via `IFileLauncher`, ADR-060), Windows-Standardprogramm übernimmt. Ein In-App-Edit-Port ist nicht mehr geplant, solange kein konkreter Bedarf entsteht (Punkt 4 bleibt als Fallback-Option dokumentiert). Außerdem liefert `RenderPageAsPngAsync` seit v0.28.112 ein `PdfPageRender`-Record (PNG **+ physische Blattgröße in mm**, rotationsbereinigt aus der MediaBox) statt nackter PNG-Bytes — Grundlage für viewer-seitige Ausschnitte in Realgrößen (Plankopf-Start = A4 rechts unten).

**Addendum Engine-Konsolidierung (Teil 47, v0.28.121, BPM-118):** Die Punkte 1–3 gelten unverändert (Port in Domain, Module konsumieren nur den Port) — die **Engine dahinter wurde ersetzt**. Beim Umsetzen der Text-Zuweisung (ADR-063) traf das Koordinaten-Mapping zwischen zwei getrennten Engines (`Windows.Data.Pdf` fürs Rendern, PdfPig für Text) wiederholt daneben. Konsequenz — fachliche Invariante: **nie wieder zwei PDF-Engines mit eigenem Koordinaten-Mapping.** Seither bedient **EINE Engine — PDFium via `Docnet.Core` 2.6.0 (MIT, Freigabe Herbert Teil 47)** — beide Ports: `PdfiumPdfService` (**Infrastructure**, nicht mehr App) implementiert `IPdfRenderService` UND `IPdfTextService`; Pixel und Text-Boxen stammen aus derselben Pipeline („Acrobat-Weg"), Viewer-Pixel ↔ mm bleibt eine einzige lineare Umrechnung. Details: `RenderPageAsync` liefert seither **rohe BGRA32-Pixel** (`PdfPageRender.PixelsBgra`, top-down, Stride = Breite×4) statt PNG-Bytes — der Viewer baut daraus direkt sein Bitmap; der **App-TFM-Bump aus v0.28.112 wurde zurückgebaut** (kein Windows-SDK-TFM mehr nötig, alle Projekte wieder einheitlich); `WindowsPdfRenderService` ist entfernt. Docnet-Fallstricke sind im Service dokumentiert (Box-Normalisierung Top/Bottom, Alpha-Compositing auf Weiß, adaptives Nachrendern ~7 px/mm mit Deckel 7200 px).

**Alternativen verworfen:** Implementierung in Infrastructure (TFM-Bump machte Infrastructure Windows-SDK-gebunden, Tests-Kette müsste mitziehen); Drittanbieter-Renderer wie PdfiumViewer (Lib-Regel: keine neuen Libraries ohne Freigabe, für reines Rendern unnötig); Rendering direkt im PlanManager (TFM-Bump aller Referenzierer + Modul-Silo statt zentralem Dienst).

**DSGVO:** Klasse A — rein lokales Rendering, keine externen Verbindungen; Planinhalte verlassen das Gerät nicht.

**Betrifft:** ADR-060 (wendet dessen Port-Prinzip auf PDF-Rendering an), ADR-045 (Plankopf-Extraktion post-V1 nutzt ggf. denselben Stream, bleibt eigene Entscheidung), Architektur-Doc (Schichtgrenzen unverändert: Port in Domain, Adapter im Composition Root).

**Referenz:** ClickUp BPM-111.06 / Subtask Slice C (86cahy45b), Teil 47.

---

## ADR-063: PDF-Text-Port (IPdfTextService) + PdfPig-Freigabe

**Datum:** 2026-08-26
**Status:** ✅ Entschieden (Herbert, Teil 47 — inkl. Library-Freigabe PdfPig); Implementierungs-Engine abweichend von Punkt 2 (siehe Addendum)
**Implementierung:** ✅ Fertig — BPM-118 (v0.28.121 UI-Fluss, v0.28.122 Persistenz); Engine = PDFium/Docnet statt PdfPig (Addendum Engine-Konsolidierung)
**Herkunft:** Wunsch Teil 47: Plandaten (Änderungshinweis, Index-Datum, Segmente) direkt aus der PDF-Vorschau per Text-Markieren + Rechtsklick zuweisen, statt sie abzutippen.

**Kontext:**

`Windows.Data.Pdf` (ADR-062) rendert ausschließlich Bitmaps — es gibt keine Textebene im Viewer. Für „Text markieren → zuweisen" braucht es Wort-Text **mit Koordinaten**. CAD-exportierte Pläne haben praktisch immer eine echte PDF-Textebene → **kein OCR nötig** (Entscheidung Herbert). Zuweisungsziele existieren bereits: `plan_revisions.released_at` (Index-Datum, seit BPM-109 vorgesehen) und der Segmenttyp-Katalog aus BPM-108 (`ISegmentTypeCatalog`).

**Entscheidung:**

1. **Port im Domain-Layer:** `IPdfTextService` in `Domain.Interfaces` — z. B. `Task<IReadOnlyList<PdfWord>> GetWordsAsync(Stream pdf, int pageIndex, CancellationToken)`; `PdfWord` = Text + BoundingBox **in mm, rotationsbereinigt** — dasselbe Koordinatensystem wie `PdfPageRender` (ADR-062), damit Viewer-Pixel ↔ PDF-mm eine einzige Umrechnung bleibt.
2. **Implementierung `PdfPigTextService` in Infrastructure** via **PdfPig** (UglyToad.PdfPig, **MIT — Library-Freigabe Herbert Teil 47**): rein verwaltetes C#, keine nativen DLLs, offline, Wort-/Buchstaben-Koordinaten. Kein Windows-SDK-TFM nötig → Infrastructure bleibt `net10.0`. MIT-Lizenztext wird der App beigelegt (Über/Lizenzen).
3. **UI-Fluss (BPM-118):** Rechteck-Markieren im Vorschau-Panel → Wörter in der Region einsammeln → Rechtsklick-Kontextmenü mit zwei Gruppen: **Revisions-Ziele** (Änderungshinweis → `plan_revisions.change_note`, Index-Datum → `released_at`) + **„Zuweisen als Segment"** dynamisch aus dem `ISegmentTypeCatalog` (BPM-108, inkl. benutzerdefinierter Typen — nichts hartcodiert).
4. **Kein OCR:** PDFs ohne Textebene zeigen einen Hinweis, Werte werden manuell getippt. OCR bleibt bewusste post-V1-Option (ADR-045-Umfeld); PdfPig ist zugleich das in ADR-045 vorgesehene Werkzeug für die spätere Plankopf-Extraktion.
5. **Schema:** `plan_revisions` + `change_note TEXT NOT NULL DEFAULT ''`. **Frühphase:** projektbezogene `planmanager.db` löschen statt Migration (INDEX.md-Frühphasenregel).

**Konsequenzen:**

- Zwei PDF-Engines parallel — bewusst: Rendern (`Windows.Data.Pdf`, App) und Textlesen (PdfPig, Infrastructure) sind getrennte Ports und einzeln austauschbar. *(Superseded durch Addendum: eine Engine für beide Ports.)*
- Erster konkreter Baustein Richtung ADR-045-Plankopf-Extraktion (gleicher Port, später automatisiert statt manuell markiert).
- Erste neue Drittanbieter-Library seit Projektregel-Einführung — Freigabeprozess (Herbert) durchlaufen und hier dokumentiert.

**Addendum Engine-Konsolidierung (Teil 47/48, v0.28.121–.122, BPM-118):** Die „zwei Engines parallel"-Annahme hat sich in der Umsetzung **nicht bewährt**: Das Koordinaten-Mapping zwischen `Windows.Data.Pdf`-Pixeln und PdfPig-Textboxen traf wiederholt daneben (unterschiedliche Rotations-/MediaBox-Behandlung). Entscheidung Teil 47: **EINE Engine — PDFium via `Docnet.Core` 2.6.0 (MIT, Freigabe Herbert)** bedient beide Ports; `PdfiumPdfService` (Infrastructure) implementiert `IPdfRenderService` und `IPdfTextService` aus derselben Pipeline. Der Port-Schnitt aus Punkt 1 blieb dabei unverändert gültig — genau er hat den Engine-Tausch ohne Modul-Änderungen ermöglicht. **PdfPig bleibt freigegeben, aber nur als Test-Builder** (PDF-Erzeugung in Unit-Tests, Version 0.1.16). ⚠️ **Paket-ID-Warnung:** Das korrekte NuGet-Paket heißt **„PdfPig"** — die ID **„UglyToad.PdfPig" ist gekapert** und darf NICHT referenziert werden. Der UI-Fluss aus Punkt 3 wurde als **klassische Word-Textauswahl** (I-Beam, Leserichtung, durchgehender Balken je Zeile) statt Rechteck-Aufziehen umgesetzt; das Zuweisungs-Menü (Revision / Zuweisen als Segment aus `ISegmentTypeCatalog`) wie entschieden. Persistenz komplett seit v0.28.122: `change_note`/`released_at` via `InsertRevision`, Segmentwerte via `UpsertSegment` (`plan_document_segments`, letzte Zuweisung gewinnt).

**Alternativen verworfen:** iText 7 (AGPL — für Closed-Source-Verkauf teure Kommerzlizenz nötig); PDFium-Wrapper (native DLLs, Deployment-Aufwand, Wrapper-Pflege); PDFsharp (keine verlässlichen Text-Koordinaten); OCR-Ansatz (unnötig bei vorhandener Textebene, deutlich schwergewichtiger).

**DSGVO:** Klasse A — rein lokale Textextraktion, keine externen Verbindungen.

**Betrifft:** ADR-062 (gemeinsames mm-Koordinatensystem), ADR-045 (bekommt sein Extraktions-Werkzeug), ADR-056 (Segmenttypen als Zuweisungsziele), DB-SCHEMA.md (plan_revisions.change_note), Mockup-Spez 02_ManuellSortieren.

**Referenz:** ClickUp BPM-118, Teil 47.

---

## ADR-064: Import-Transaktions-Härtung — idempotente Journal-/Recovery-/Undo-Semantik

**Datum:** 2026-08-27
**Status:** ✅ Entschieden (beidseitiges Sign-off via CGR-2026-08-27-bpm-architektur r3: ChatGPT GPT-5.4 + Claude + Herbert)
**Implementierung:** Not Started — ClickUp **BPM-120** (Slices H0 + T0–T8, 15 Akzeptanzkriterien im Ticket); BPM-112 Slice 3 (= BPM-112.03) wird als T1 miterledigt. Start nach BPM-111.06 (done), Empfehlung: vor weiteren Slices auf dem mutierenden Importpfad.
**Herkunft:** Review einer externen ChatGPT-12-Diagramm-Analyse des Gesamtprojekts, 3 Runden Cross-Review mit Code-Verifikation (Stand v0.28.120). Auslöser: Der Crash-Korridor zwischen Dateisystem-Mutation und `planmanager.db`-Update ist der größte offene technische Befund vor V1.

**Kontext:**

ADR-061 Punkt 5 (Journal vor Move + `.bpm_tmp` + atomic rename + idempotente Recovery) ist beschlossen, aber im Import-Pfad **nicht umgesetzt** — BPM-113 lieferte Resolver/Schema, nicht die Transaktions-Härtung. Verifizierte Lücken (Code-Stand v0.28.120):

- `import_actions` werden einzeln unmittelbar vor jeder Operation angelegt, nicht vollständig vorab; `archive_path` wird als `null` journalisiert, der echte Archivpfad entsteht ad hoc.
- `ImportExecutionService` verschiebt direkt per `File.Move` (kein Temp, kein atomic rename); Dubletten-Löschung läuft unjournalisiert nach dem Action-Loop.
- Recovery sieht nur `journal.status = 'pending'` (teilweise fehlgeschlagene Imports werden terminal `failed` und ignoriert) und wiederholt nur Datei-Moves, stellt das Planarchiv (Revision/Supersede/Events) nicht her.
- `ImportUndoService` führt nach fehlgeschlagenen Datei-Reverses den DB-Rollback und `MarkImportUndone` **bedingungslos** aus (Disk halb zurück, DB komplett zurück, Import „undone").
- Der V1-Radial-Workflow (ADR-059) läuft real über genau diese Strecke (`CaptureConfirmService` → `ImportExecutionService.Execute`, Konstruktion per `new`).

Das sind die ADR-058-Stop-Fälle („Import-Journal/Undo wackelt → sofort Stopp", „Dateiverschiebung + DB-Commit inkonsistent → sofort Stopp").

**Entscheidung:**

Elf verbindliche Invarianten (Sign-off r3), umzusetzen via BPM-120:

1. **Idempotenz-Kerninvariante:** Eine journalisierte ImportAction muss aus jedem zulässigen Zwischenzustand idempotent auf den definierten Endzustand gebracht werden können — Dateisystem UND Plan-Cache. (Die konkrete Klasse — z.B. `ImportActionExecutor` — ist Implementierungsdetail, kein Architekturvertrag.)
2. **Vorab-Journalisierung:** Alle geplanten Actions stehen VOR der ersten Mutation vollständig im Journal, inkl. deterministischer `source_path`/`destination_path`/`archive_path`.
3. **Atomarer Action-Abschluss:** `action_status = completed` wird in derselben SQLite-Transaction gesetzt wie die zugehörigen fachlichen Writes (Document/Revision/File/Link/Events).
4. **Gemeinsamer Apply-Pfad:** Normaler Import und Recovery Forward nutzen dieselbe fachliche Apply-Logik; `RecoveryExecutorService` verliert seine vereinfachte Move-Eigenlogik.
5. **Undo-Reihenfolge:** DB-Rollback + `MarkImportUndone` NUR nach vollständig erfolgreichem Disk-Reverse (LIFO, nach Preflight); scheitert ein Disk-Reverse, bleibt der Vorgang reparierbar. DB-Rollback läuft in einer Transaction. Gemeinsamer Kern `ApplyForward`/`ApplyReverse` — kein Framework, kein Command-Bus.
6. **Status-Semantik:** `pending` = recovery-pflichtig (blockiert neuen Confirm); `failed` erst terminal nach vollständigem Rollback oder bewusster Cleanup-/Abbruchentscheidung.
7. **`skipDuplicate` (Bucket A):** Bestätigte MD5-Dubletten werden als echte Action (`action_type = skipDuplicate`, Source-Pfad + MD5 + Größe) journalisiert und beim Confirm direkt gelöscht — journalisiert + recovery-fähig, aber **bewusst nicht undo-bar** (journalisiert ≠ undo-bar; Inhalt liegt MD5-identisch im Bestand, kein Papierkorb). Recovery-Endzustand: redundante Inbox-Kopie existiert nicht mehr UND gleicher MD5 im getrackten Bestand verifiziert (Lookup über die getrackte Teilmenge nach ADR-061 Modell A, kein Verzeichnis-Scan); sonst RecoveryConflict, nie blind `completed`. Undo-Präzisierung: gemischter Import → undo-fähige Actions zurück, skipDuplicate bleibt gelöscht, Journal darf `undone` werden; reiner skipDuplicate-Import → kein Undo anbieten.
8. **Schema-Änderung (Frühphase, keine Migration):** `import_actions.destination_path` und (bei Nutzung) `import_action_files.destination_path` werden nullable. Betroffene Datei: projektbezogene `planmanager.db` — User löscht sie, BPM erzeugt sie beim nächsten Start neu.
9. **H0 — ein V1-Importweg:** Der klassische Profil-Import („Import starten" / `OnStartImport` / `ImportPreviewDialog`) wird VOR der Härtung aus dem V1-Nutzerpfad genommen (Legacy-Klassen bleiben vorerst im Repo). Damit für V1 gestrichen: Skip-only-Fix, `DocumentTypeRecognizer.IsConflict`-Fix, Preview-UX-Ausbau des alten Dialogs, LearnIndex-Profil-Lernen.
10. **Parallelität:** PDF-Port-Arbeit (ADR-062/063) darf parallel laufen, solange sie den Importpfad nicht berührt. Keine weiteren Features, die auf dem mutierenden Import-/Undo-/Recovery-Pfad aufbauen, bevor die Invarianten erfüllt sind; reine UI-/Preview-/PDF-Arbeit ist entkoppelt.
11. **Explorer-Abgrenzung:** In-App-Dateibrowser folgt ADR-061 Modell A (Live-FS + kuratierter Planindex; getrackte Pläne nur über journalisierte Operationen) und startet erst nach stabilen Ports (ADR-060 Slice 6). Reklassifizierung getrackter Pläne (physischer Move ändert fachliche Zuordnung) ist ein eigener Domain-Workflow, kein Explorer-Feature. Architektur-Diagramm dazu erst bei Feature-Start.

**Umsetzung (Slice-Folge BPM-120, jeder Zwischenstand baubar + grün):** H0 Cutover → T0 Characterization-Tests (bekannte Fehler NICHT als Soll festschreiben) → T1 FS-Ports + fault-fähiger `FakeFileStore` + lokale Constructor Injection (= ADR-060 Slice 3; kein Composition-Root-Großumbau) → T2 Vorab-Journalisierung → T3 `.bpm_tmp` + atomic rename + 3× Lock-Retry → T4 DB-Transaction pro Action + idempotenter DB-Apply → T5 Recovery Forward über gemeinsamen Apply-Pfad → T6 failed/pending-Semantik → T7 Undo-Härtung → T8 Fault-/Crash-Matrix (Abbruch nach jedem Schritt × Forward/Rollback/Undo).

**Konsequenzen:**

- Die ADR-058-Stop-Invarianten werden erstmals technisch einlösbar; die Crash-Fenster FS ↔ DB sind testbar (Fault-Injection via FakeFileStore + Temp-/SQLite-Integrationstests).
- `CaptureConfirmService` erhält den Executor via Constructor Injection statt internem `new` (eine Instanz-Welt für klassischen und Radial-Pfad, solange beide existieren).
- Bewusst NICHT gebaut: 2PC, verteilte Locks, FileSystemWatcher-Sync-Engine, OneDrive-API, Papierkorb/Quarantäne für Dubletten, Event Sourcing.
- Außerhalb des Scopes, aber im Review festgehalten: ID-basierter `document_key` (`BuildManualDocumentKey` nutzt noch Bauteil-/Geschoss-NAMEN statt Stammdaten-IDs) bleibt ADR-059/BPM-111-Abnahmepunkt (Kommentar an BPM-111).

**DSGVO:** Klasse A (technische Import-/Journal-Persistenz, kein Personenbezug). Pfad-Logging über die ADR-060-Ports zentral maskierbar.

**Betrifft:** ADR-058 (Stop-Punkte + Journal-Haltbarkeit), ADR-059 (ein V1-Importweg, Buckets), ADR-060 (Slice 3 wird in BPM-120 erledigt), ADR-061 (P5 wird umgesetzt und DB-seitig präzisiert), DB-SCHEMA.md Kap. 6 (`import_actions`/`import_action_files` — `destination_path` nullable, mit BPM-120 nachziehen), PlanManager.md (Import-/Recovery-/Undo-Kapitel).

**Referenz:** [Docs/Referenz/chatgpt-reviews/CGR-2026-08-27-bpm-architektur/](../Referenz/chatgpt-reviews/CGR-2026-08-27-bpm-architektur/) — 3 Runden Cross-Review (r1 Delta-Analyse + Task-Schnitt, r2 Diagramm-Nachlieferung + Undo-Befund + H0, r3 Sign-off + 15 Akzeptanzkriterien). ClickUp: BPM-120.

---

*Dokument wird laufend aktualisiert wenn neue Architekturentscheidungen getroffen werden.*