# BauProjektManager

Modulare Desktop-App für Baustellen-Management in Österreich (Steiermark). Offline-fähig, lokal, Sync über beliebigen Cloud-Speicher, kein Cloud-Abo.

## Vision

Eine einzige App für den gesamten Arbeitsalltag auf der Baustelle: Pläne sortieren, Fotos verwalten, Bautagebuch führen, Stunden erfassen. Sync über beliebigen Cloud-Speicher. Bestehende VBA-Makros lesen eine automatisch generierte Registry.

```
  BauProjektManager.exe ──→ SQLite (bpm.db)
                               ↓
                            registry.json (auto-generiert, read-only)
                               ↓
  Outlook VBA ←────────────────┘
  Excel VBA  ←─────────────────┘
  PowerShell (PhotoFolder) ←───┘
```

Ausführliche Vision: [Docs/Referenz/VISION.md](Docs/Referenz/VISION.md)

## Aktueller Stand

**Version:** steht in `Directory.Build.props`, alle Änderungen in [CHANGELOG](Docs/Referenz/CHANGELOG.md).
Laufende Arbeit: Branch `feature/planmanager-v1` (V1 = Einstellungen + PlanManager).

| Komponente | Status |
|-----------|--------|
| App-Shell + Navigation | ✅ Erledigt |
| Einstellungen (Projekte, Pfade, Ordnerstruktur) | ✅ Erledigt |
| SQLite-Datenbank (`bpm.db`, `planmanager.db` je Projekt) | ✅ Erledigt |
| registry.json Export (VBA-kompatibel) | ✅ Erledigt |
| Automatische Projektordner-Erstellung | ✅ Erledigt |
| ProjectEditDialog (5 Tabs: Stammdaten, Bauwerk, Beteiligte, Portale, Ordner) | ✅ Erledigt |
| Theme-System (Resource Dictionaries, Dark Theme) | ✅ Erledigt |
| PlanManager: Explorer, Manuell sortieren (Radial), Mit Claude sortieren, Plandaten, Erkennung | 🔨 V1 in Arbeit, Kern läuft |
| PlanManager: Import mit Journal, Undo, Recovery, Archiv | ✅ Erledigt |
| PDF-Vorschau mit Textauswahl (PDFium) | ✅ Erledigt |

## Techstack

- **Sprache:** C# (.NET 10 LTS)
- **GUI:** WPF (XAML), Dark Theme
- **Pattern:** MVVM (CommunityToolkit.Mvvm), DI-Container (Microsoft.Extensions.DependencyInjection)
- **Datenbank:** SQLite (Microsoft.Data.Sqlite), IDs als ULID
- **Logging:** Serilog (File + Console)
- **PDF:** PDFium via Docnet.Core (Vorschau und Textebene, eine Engine – ADR-063-Addendum)
- **Tests:** xUnit
- **Geplant (nach V1):** ClosedXML (Excel), QuestPDF (PDF-Export)
- **VBA-Export:** registry.json (flach, read-only)

## Solution-Struktur

```
BauProjektManager.slnx
│
├── src/
│   ├── BauProjektManager.App/             ← EXE (Shell, DI, MainWindow, Themes)
│   ├── BauProjektManager.Domain/          ← Modelle, Enums, Ports (keine Abhängigkeiten)
│   ├── BauProjektManager.Infrastructure/  ← SQLite, JSON, Dateisystem, PDF, Logging
│   ├── BauProjektManager.Settings/        ← Einstellungen-Modul (WPF)
│   └── BauProjektManager.PlanManager/     ← PlanManager-Modul (WPF)
│
├── tests/
│   └── BauProjektManager.Tests/           ← xUnit-Tests
│
├── Tools/                                 ← Seitenliste, Datenliste, Mockup-Theme, PowerShell-Hilfen
│
└── Docs/                                  ← Kern/, Module/, Konzepte/, Referenz/, Test/, Mockups/
```

Einstieg in die Doku: [INDEX.md](INDEX.md).

## Dokumentation

### Kern (bei jeder Code-Änderung relevant)

| Dokument | Inhalt |
|----------|--------|
| [Architektur](Docs/Kern/BauProjektManager_Architektur.md) | Technische Spezifikation, Datenmodell, Import-Workflow, GUI-Mockups |
| [DB-Schema](Docs/Kern/DB-SCHEMA.md) | Zentrales DB-Leitdokument (Ist + geplant, 18 Tabellen) |
| [Coding Standards](Docs/Kern/CODING_STANDARDS.md) | Code-Richtlinien für C#/WPF + Datenschutz (Kap. 17) |
| [DSGVO-Architektur](Docs/Kern/DSGVO-Architektur.md) | Privacy Engineering, IPrivacyPolicy, Datenklassifizierung A/B/C |
| [Backlog](Docs/Kern/BACKLOG.md) | Feature-Liste mit Status und Priorisierung |

### Referenz (lesen wenn Thema aufkommt)

| Dokument | Inhalt |
|----------|--------|
| [ADR](Docs/Referenz/ADR.md) | Architekturentscheidungen (ADR-001 bis ADR-067) mit Kontext und Alternativen |
| [Vision](Docs/Referenz/VISION.md) | Produktvision, Schmerzpunkte, Zielgruppe, Erfolgskriterien |
| [Dependency Map](Docs/Referenz/DEPENDENCY-MAP.md) | Solution-Struktur + externes Ökosystem mit Datenflüssen |
| [UI/UX Guidelines](Docs/Referenz/UI_UX_Guidelines.md) | Komplettes Design-System (Dark Theme, Token, Komponenten) |
| [WPF UI Architecture](Docs/Referenz/WPF_UI_Architecture.md) | Theme-System, Resource Dictionaries, Shell-Layout |
| [Changelog](Docs/Referenz/CHANGELOG.md) | Versionshistorie ab v0.0.0 |
| [Glossar](Docs/Referenz/GLOSSAR.md) | Bau-Begriffe, Rollen, Abkürzungen |
| [UX Flows](Docs/Referenz/UX_Flows.md) | Haupt-Workflows, Screen States |

### Modul-Konzepte (Docs/Konzepte/)

| Modul | Prio | Konzept |
|-------|------|---------|
| Foto-Management | 1 | [ModuleFoto.md](Docs/Konzepte/ModuleFoto.md) |
| Zeiterfassung | 2 | [ModuleZeiterfassung.md](Docs/Konzepte/ModuleZeiterfassung.md) |
| Bautagebuch | 3 | [ModuleBautagebuch.md](Docs/Konzepte/ModuleBautagebuch.md) |
| Dashboard | 4 | [ModuleDashboard.md](Docs/Konzepte/ModuleDashboard.md) |
| Outlook | 5 | [ModuleOutlook.md](Docs/Konzepte/ModuleOutlook.md) |
| Plankopf-Extraktion | 6 | [Moduleplanheader.md](Docs/Konzepte/Moduleplanheader.md) |
| GIS-Integration | 7 | [ModuleGIS.md](Docs/Konzepte/ModuleGIS.md) |
| Wetter | 8 | [ModuleWetter.md](Docs/Konzepte/ModuleWetter.md) |
| Vorlagen | 9 | [ModuleVorlagen.md](Docs/Konzepte/ModuleVorlagen.md) |
| KI-Assistent | — | [ModuleKiAssistent.md](Docs/Konzepte/ModuleKiAssistent.md) |
| Task-Management | — | [ModuleTaskManagement.md](Docs/Konzepte/ModuleTaskManagement.md) |
| Kalkulation | — | [ModuleKalkulation.md](Docs/Konzepte/ModuleKalkulation.md) |
| Multi-User / Server | — | [ServerArchitektur.md](Docs/Konzepte/ServerArchitektur.md) (Sync: ADR-053; MultiUserKonzept ist historisch) |
| Lizenzierung | — | [ModuleAktivierungLizenzierung.md](Docs/Konzepte/ModuleAktivierungLizenzierung.md) |

## Voraussetzungen

- Windows 10/11
- Visual Studio Community 2022 (oder neuer)
- .NET 10 SDK
- Smart App Control muss deaktiviert sein (für selbst-kompilierte DLLs)
- Start via F5 in Visual Studio (nicht `dotnet run`)

## Lizenz

Noch keine Lizenz festgelegt (privates Projekt). Das Lizenzmodell kommt mit einem späteren Verkauf (Lizenzsignatur nach
ADR-053 P.23).

---

*Entwickelt für den praktischen Einsatz auf Baustellen in der Steiermark, Österreich.*
