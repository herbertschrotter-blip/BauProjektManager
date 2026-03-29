# BauProjektManager

Modulare Desktop-App für Baustellen-Management in Österreich (Steiermark). Offline-fähig, lokal auf OneDrive, kein Cloud-Abo.

## Vision

Eine einzige App für den gesamten Arbeitsalltag auf der Baustelle: Pläne sortieren, Fotos verwalten, Bautagebuch führen, Stunden erfassen. Bestehende VBA-Makros lesen eine automatisch generierte Registry.

```
  BauProjektManager.exe ──→ SQLite (bpm.db)
                               ↓
                            registry.json (auto-generiert, read-only)
                               ↓
  Outlook VBA ←────────────────┘
  Excel VBA  ←─────────────────┘
  PowerShell (PhotoFolder) ←───┘
```

Ausführliche Vision: [Docs/VISION.md](Docs/VISION.md)

## Aktueller Stand

**Version:** v0.12.7 (29.03.2026)

| Komponente | Status |
|-----------|--------|
| App-Shell + Navigation | ✅ Erledigt |
| Einstellungen (Projekte, Pfade, Ordnerstruktur) | ✅ Erledigt |
| SQLite-Datenbank + Auto-IDs | ✅ Erledigt |
| registry.json Export (VBA-kompatibel) | ✅ Erledigt |
| Automatische Projektordner-Erstellung | ✅ Erledigt |
| PlanManager (Pläne sortieren) | ⬜ In Arbeit |

## Techstack

- **Sprache:** C# (.NET 10 LTS)
- **GUI:** WPF (XAML), Dark Theme
- **Pattern:** MVVM (CommunityToolkit.Mvvm)
- **Datenbank:** SQLite (Microsoft.Data.Sqlite)
- **Logging:** Serilog (File + Console)
- **Excel:** ClosedXML
- **PDF:** QuestPDF (Export), PdfPig (Import)
- **VBA-Export:** registry.json (flach, read-only)

## Solution-Struktur

```
BauProjektManager.sln
│
├── src/
│   ├── BauProjektManager.App/             ← EXE (Shell, DI, MainWindow)
│   ├── BauProjektManager.Domain/          ← Modelle, Enums (keine Abhängigkeiten)
│   ├── BauProjektManager.Infrastructure/  ← SQLite, JSON, FileSystem, Logging
│   ├── BauProjektManager.Settings/        ← Einstellungen-Modul (WPF)
│   └── BauProjektManager.PlanManager/     ← PlanManager-Modul (WPF)
│
├── Tools/
│   └── Get-ProjektOrdner.ps1              ← PowerShell Analyse-Tool
│
└── Docs/                                  ← Dokumentation
```

## Dokumentation

| Dokument | Inhalt |
|----------|--------|
| [Architektur v1.5](Docs/BauProjektManager_Architektur.md) | Technische Spezifikation, Datenmodell, Import-Workflow, GUI-Mockups |
| [ADR](Docs/ADR.md) | 23 Architekturentscheidungen mit Kontext und Alternativen |
| [Vision](Docs/VISION.md) | Produktvision, Schmerzpunkte, Zielgruppe, Erfolgskriterien |
| [Dependency Map](Docs/DEPENDENCY-MAP.md) | Solution-Struktur + externes Ökosystem mit Datenflüssen |
| [Changelog](Docs/CHANGELOG.md) | Versionshistorie ab v0.0.0 |
| [Backlog](Docs/BACKLOG.md) | Feature-Liste mit Status und Priorisierung |
| [Coding Standards](Docs/CODING_STANDARDS.md) | Code-Richtlinien für C#/WPF |

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

## Voraussetzungen

- Windows 10/11
- Visual Studio Community 2022 (oder neuer)
- .NET 10 SDK
- Smart App Control muss deaktiviert sein (für selbst-kompilierte DLLs)
- Start via F5 in Visual Studio (nicht `dotnet run`)

## Lizenz

MIT License — siehe [LICENSE](LICENSE)

---

*Entwickelt für den praktischen Einsatz auf Baustellen in der Steiermark, Österreich.*
