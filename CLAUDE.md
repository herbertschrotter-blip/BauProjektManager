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

- PascalCase Klassen/Methoden/Konstanten/static readonly, _camelCase private Instanzfelder, IName Interfaces
- Max 300-400 Zeilen/Klasse, 30-40 Zeilen/Methode
- Nullable Reference Types AKTIV, kein null-forgiving ohne Grund
- CommunityToolkit.Mvvm: ObservableObject, RelayCommand
- KEINE hardcoded Colors/FontSize — nur Theme-Tokens
- DI zweistufig: Services, Ports und DB-Klassen nur per DI (Container bzw. Factory je Projekt), kein new; ViewModels per Konstruktor, Views dürfen View-first zusammensetzen; Interfaces nur für Ports und Test-Nähte (Ausnahmen: CODING_STANDARDS 8.3)
- SQLite: Werte immer parametrisiert, NIE String-Building; Tabellen-/Spaltennamen nur aus Konstanten/Whitelist
- Statisches Serilog (`Log.*`), KEINE Personendaten in Logs; Pfade nur zentral maskiert (Datei-/Ordnername), Windows-Benutzername nie
- Alle externen HTTP über IExternalCommunicationService + DataClassification

## Git

- Format, Module, Version, Pre-Commit-Checks und Push: siehe `## Skill-Profil` → `### Commit` (Push-Policy `user-only`: Herbert pusht selbst)
- Typen: Feature / Fix / Change / Refactor / Perf / Docs
- NIE neue Libraries ohne Freigabe

## Ausführliche Standards

- Architektur: Docs/Kern/BauProjektManager_Architektur.md
- DB Schema: Docs/Kern/DB-SCHEMA.md
- DSGVO: Docs/Kern/DSGVO-Architektur.md
- UI/UX: Docs/Referenz/UI_UX_Guidelines.md
- Backlog: Docs/Kern/BACKLOG.md

## Skill-Profil

- Profil-Version: 1
- Projekt-ID: bpm
- Repo: herbertschrotter-blip/BauProjektManager
- Branch-Policy: current

### Checks
- build: dotnet build BauProjektManager.slnx [src/**; tests/**]
- test: dotnet test tests/BauProjektManager.Tests/BauProjektManager.Tests.csproj [src/**; tests/**]

### Commit
- Format: [vX.Y.Z] <Modul>, <Typ>: <Kurztitel> (Modul auch kombiniert mit +, z.B. INF+Docs)
- Module: App=src/BauProjektManager.App/**; DevTools=src/BauProjektManager.App/DevTools*; Domain=src/BauProjektManager.Domain/**; INF=src/BauProjektManager.Infrastructure/**; PlanManager=src/BauProjektManager.PlanManager/**; Settings=src/BauProjektManager.Settings/**; Tests=tests/**; Docs=Docs/**, INDEX.md, CLAUDE.md, .claude/skill-config/**; Build=Directory.Build.props, BauProjektManager.slnx, .gitignore
- Versionsquelle: Directory.Build.props#Version
- Versionsregel: jeder Commit +1 PATCH (auch Feature) mit Eintrag in Docs/Referenz/CHANGELOG.md; MINOR/MAJOR nur auf Ansage von Herbert
- Push-Policy: user-only
- Pre-Commit-Checks: build; test
- Doku-Check: ref:INDEX.md#6-doc-pflege-policy

### Tracker
- Provider: clickup
- Config: .claude/skill-config/tracker.md
