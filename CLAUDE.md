# CLAUDE.md — BauProjektManager

## Projekt

BauProjektManager (BPM) — modulare WPF Desktop-App für österreichische
Baustellen-Manager (Poliere, Bauleiter). Offline-first, Cloud-Speicher-neutral.

- **Version:** In `Directory.Build.props` im Repo-Root
- **Stack:** C# / .NET 10 LTS, WPF, SQLite, CommunityToolkit.Mvvm, Serilog, ClosedXML
- **Architektur:** Modularer Monolith — Single EXE, Module als separate DLLs

## Wichtige Einstiegspunkte

- **INDEX.md** — Projekt-Router. ZUERST lesen bei jeder größeren Aufgabe.
- **Docs/Referenz/DOC-STANDARD.md** — Frontmatter, Quickload, Kapitelvorlagen, Skill-Laderegel.

## Docs lesen — Reihenfolge

1. INDEX.md → welche Doc?
2. Frontmatter + AI-Quickload → relevant? welches Kapitel?
3. Fachliche Invarianten prüfen → Kernregeln die nie gebrochen werden dürfen
4. Pflichtlesen-Kapitel laden → immer wenn Modul betroffen
5. Weitere Kapitel nur bei Bedarf

## Solution-Struktur

```
src/
├── BauProjektManager.App/          ← WPF Shell, Themes, Dialoge
├── BauProjektManager.Domain/       ← Models, Interfaces, Enums
├── BauProjektManager.Infrastructure/ ← SQLite, Services, Persistence
├── BauProjektManager.PlanManager/  ← PlanManager Modul (V1 Kernfeature)
└── BauProjektManager.Settings/     ← Einstellungen Modul
```

## Coding Standards (Kurzversion)

- PascalCase Klassen/Methoden, _camelCase private Felder, IName Interfaces
- Max 300-400 Zeilen/Klasse, 30-40 Zeilen/Methode
- Nullable Reference Types AKTIV, kein null-forgiving ohne Grund
- CommunityToolkit.Mvvm: ObservableObject, RelayCommand
- KEINE hardcoded Colors/FontSize — nur Theme-Tokens
- Constructor Injection, keine new-Instanzen für Services
- SQLite parametrisiert, NIE String-Building
- Serilog, KEINE Personendaten in Logs
- Alle externen HTTP über IExternalCommunicationService + DataClassification

## Git

- Format: [vX.Y.Z] Modul, Typ: Kurztitel
- Typen: Feature / Fix / Change / Refactor / Perf / Docs
- NIE git push — Herbert pusht selbst
- NIE neue Libraries ohne Freigabe

## Ausführliche Standards

- Architektur: Docs/Kern/BauProjektManager_Architektur.md
- DB Schema: Docs/Kern/DB-SCHEMA.md
- DSGVO: Docs/Kern/DSVGO-Architektur.md
- UI/UX: Docs/Referenz/UI_UX_Guidelines.md
- Backlog: Docs/Kern/BACKLOG.md
