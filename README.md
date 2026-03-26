# BauProjektManager

Baustellen-Ökosystem für lokales Projektmanagement im Bauwesen (Österreich / Steiermark).

## Vision

Mehrere Desktop-Tools greifen auf eine **gemeinsame Projekt-Registry** zu — keine Doppeleingaben, keine Cloud-Abhängigkeit, volle Offline-Fähigkeit.

```
  Outlook VBA ──┐
  Excel VBA ────┤
  PlanManager ──┼──→  registry.json  ←── Zentrale Datenquelle
  PhotoFolder ──┤      (OneDrive-Sync)
  WetterApp ────┘
```

## Apps im Ökosystem

| App | Technologie | Funktion | Status |
|-----|------------|----------|--------|
| **PlanManager** | C# + WPF | Pläne sortieren, versionieren, Planlisten abgleichen | 🔨 In Entwicklung |
| **PhotoFolder** | PowerShell | Baustellenfotos per GPS/EXIF sortieren | ✅ Existiert |
| **MasterApp** | C# + WPF | Projekte anlegen, Registry verwalten | 📋 Geplant |
| **Outlook-VBA** | VBA | Emails/Anhänge in Projektordner sortieren | 📋 Geplant |
| **Excel-Vorlagen** | VBA | Beton-/Ziegeltabellen mit Projektdaten | 📋 Geplant |

## Kernfeatures PlanManager

- **Dateinamen-Parsing**: Muster pro Plantyp anlernen (Segment-Klick-GUI)
- **Automatische Sortierung**: Pläne in Ordnerstruktur nach Plantyp/Geschoß/Haus
- **Index-Versionierung**: Alte Planstände automatisch archivieren
- **Planlisten-Abgleich**: Soll vs. Ist (Excel/CSV/PDF Import)
- **Multi-Device**: OneDrive-Sync zwischen PC und Laptop
- **VBA-kompatible Registry**: Outlook + Excel greifen auf gleiche Projektdaten zu

## Techstack

- **Sprache:** C# (.NET 9)
- **GUI:** WPF (XAML)
- **Pattern:** MVVM
- **Registry:** JSON (VBA-kompatibel)
- **Excel:** ClosedXML
- **PDF:** QuestPDF (Export), PdfPig (Import)
- **Testing:** xUnit

## Projektstruktur

```
BauProjektManager/
├── Shared/                    ← Gemeinsame Libraries (Registry, Logging)
├── PlanManager/               ← Hauptapp: Pläne sortieren
├── Docs/                      ← Architektur-Dokumentation
├── README.md
├── LICENSE
└── .gitignore
```

## Dokumentation

Siehe [`Docs/BauProjektManager_Architektur.md`](Docs/BauProjektManager_Architektur.md) für die vollständige Architektur-Spezifikation mit:
- Registry-Schema (JSON)
- Alle GUI-Dialoge (14 Mockups)
- Dateinamen-Parsing Konzept
- Phasenplan
- Technische Entscheidungen

## Voraussetzungen

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- Windows 10/11
- Visual Studio 2022 oder VS Code + C# Extension

## Lizenz

MIT License — siehe [LICENSE](LICENSE)

---

*Entwickelt für den praktischen Einsatz auf Baustellen in der Steiermark, Österreich.*
