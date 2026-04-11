---
doc_id: konzept-aktivierung-lizenzierung
doc_type: concept
authority: secondary
status: active
owner: herbert
topics: [lizenzierung, modul-aktivierung, testversion, verkaufsmodell, sidebar, offline-lizenz]
read_when: [modul-aktivierung, lizenz-system, verkaufsmodell, testversion]
related_docs: [architektur, vision]
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: Konzept für Modul-Aktivierung (Ein/Aus-Schalter) und Offline-Lizenzierung
- Autorität: secondary
- Lesen wenn: Modul-Aktivierung, Lizenz-System, Verkaufsmodell, Testversion
- Nicht zuständig für: Modul-Implementierung (→ jeweilige Modul-Docs)
- Kapitel:
  - 1. Zweck und Zielzustand
  - 2. Datenmodell (geplant)
  - 3. Workflow
  - 4. Technische Umsetzung
  - 5. Abhängigkeiten
  - 6. No-Gos / Einschränkungen
  - 7. Offene Fragen
- Pflichtlesen: keine
- Fachliche Invarianten:
  - Keine Abos — Einmalzahlung pro Modul
  - Offline-fähige Lizenzvalidierung — kein Activation-Server
  - Testdaten bleiben bei Ablauf erhalten — nur Zugriff gesperrt

---

# BauProjektManager — Konzept: Modul-Aktivierung & Lizenzierung

**Erstellt:** 30.03.2026  
**Version:** 1.1 (Refactoring auf DOC-STANDARD)  
**Status:** Konzept (Won't have V1)  
**Abhängigkeiten:** Keine (kann unabhängig implementiert werden)  
**Ziel:** Module ein-/ausschaltbar machen + Bezahlmodell für späteren Verkauf vorbereiten

---

## 1. Zweck und Zielzustand

Zwei Dinge die zusammengehören:

1. **Modul-Aktivierung:** Nur aktive Module werden in der Sidebar angezeigt. Wer nur den PlanManager braucht, sieht kein Bautagebuch, keine Zeiterfassung, keine Kalkulation. Aufgeräumte Oberfläche.

2. **Lizenzierung:** Wenn BPM verkauft wird, soll jedes Modul einzeln lizenzierbar sein. Basismodule kostenlos oder günstig, Zusatzmodule kostenpflichtig. 30-Tage-Testversion pro Modul. Alles offline-fähig — kein Online-Zwang.

---

## 2. Datenmodell (geplant)

Lizenz- und Aktivierungsdaten:

- `settings.json` → `activeModules` Dictionary (welche Module aktiv)
- `trial.dat` → verschlüsselt in `%LocalAppData%` (Testversions-Startdaten)
- `.bpm-license` → signierte JSON-Datei (Lizenz pro Modul)

### Modulliste

| Modul | Standard | Beschreibung | Lizenz-Kategorie |
|-------|---------|-------------|-----------------|
| Einstellungen | ☑ Immer an | Projekte, Pfade, Ordnerstruktur | Basis (kostenlos) |
| PlanManager | ☑ An | Pläne sortieren, versionieren | Basis (kostenlos) |
| Dashboard | ☐ Aus | Zentrale Übersicht, Widgets | Basis (kostenlos) |
| Bautagebuch | ☐ Aus | Tägliches Protokoll, Auto-Fill | Zusatzmodul |
| Zeiterfassung | ☐ Aus | WPF-Maske → Excel | Zusatzmodul |
| Kalkulation | ☐ Aus | Nachkalkulation, Bauzeitprognose | Zusatzmodul |
| Foto-Management | ☐ Aus | Viewer, Tags, Geodaten | Zusatzmodul |
| Outlook | ☐ Aus | COM Interop, Anhänge | Zusatzmodul |
| Wetter | ☐ Aus | API pro Baustelle | Zusatzmodul |
| Vorlagen | ☐ Aus | Excel/Word befüllen | Zusatzmodul |
| KI-Assistent | ☐ Aus | LV-Analyse, ChatGPT/Claude | Premium |
| Task-Management | ☐ Aus | ClickUp/Asana Integration | Premium |
| Mobile PWA | ☐ Aus | Handy-Zugriff (Server nötig) | Premium |

### Modul-Abhängigkeiten

| Modul | Braucht |
|-------|---------|
| Kalkulation | Zeiterfassung (für Arbeitsstunden) |
| Bautagebuch | Zeiterfassung (für Anwesenheit), optional Kalkulation (für Arbeitspakete) |
| Mobile PWA | Multi-User Server-Modus |
| Task-Management | Optional Kalkulation (für Arbeitspakete) |

### Lizenz-Datei Schema

```json
{
  "licenseId": "LIC-2026-001",
  "licensee": "Baufirma Mustermann GmbH",
  "email": "herbert@mustermann.at",
  "issuedAt": "2026-06-15",
  "modules": {
    "planManager": { "type": "full", "expiresAt": null },
    "diary": { "type": "full", "expiresAt": null },
    "timeTracking": { "type": "full", "expiresAt": null },
    "calculation": { "type": "trial", "expiresAt": "2026-07-15" },
    "aiAssistant": { "type": "none" }
  },
  "maxInstallations": 3,
  "signature": "base64-encoded-hmac-sha256..."
}
```

### Lizenz-Typen pro Modul

| Typ | Bedeutung | In der GUI |
|-----|-----------|-----------|
| **full** | Unbegrenzt freigeschaltet | ✅ Lizenziert |
| **trial** | 30 Tage Testversion, dann gesperrt | 🟡 Testversion (noch 12 Tage) |
| **expired** | Testversion abgelaufen | 🔴 Abgelaufen — [Lizenz kaufen] |
| **none** | Nicht lizenziert | ⬜ Nicht freigeschaltet — [Testen] |

---

## 3. Workflow

### Modul-Aktivierung

In den Systemeinstellungen gibt es eine Seite "Module" mit einer Liste aller verfügbaren Module. Jedes Modul hat einen Ein/Aus-Schalter. Nur aktive Module erscheinen in der Sidebar.

Wenn ein Modul aktiviert wird das ein anderes braucht:

```
User aktiviert "Kalkulation"
→ Hinweis: "Kalkulation benötigt Zeiterfassung. Zeiterfassung wird ebenfalls aktiviert."
→ [OK] [Abbrechen]
```

### Testversion (30 Tage)

```
1. User aktiviert Modul "Kalkulation" in Einstellungen
2. BPM: "Kalkulation ist nicht lizenziert. 30 Tage kostenlos testen?"
3. User klickt [Ja, testen]
4. BPM speichert Startdatum verschlüsselt in %LocalAppData%
5. 30 Tage voller Zugriff
6. Nach 30 Tagen: "Testversion abgelaufen. Lizenz kaufen?"
7. Modul wird deaktiviert (Daten bleiben erhalten!)
8. Bei Lizenz-Import: Modul wird wieder aktiviert, Daten sind noch da
```

**Wichtig:** Daten die während der Testversion erfasst wurden, werden NICHT gelöscht. Nur der Zugriff auf das Modul wird gesperrt.

### Lizenz-Import

User bekommt `.bpm-license` Datei per E-Mail → importiert in BPM Einstellungen → Module werden freigeschaltet.

### GUI — Einstellungen → Module

```
┌─────────────────────────────────────────────────────────────────┐
│ Einstellungen → Module                                           │
│                                                                  │
│ Aktiviere nur die Module die du brauchst.                       │
│ Inaktive Module werden in der Sidebar ausgeblendet.             │
│                                                                  │
│ ┌───────────────────────────────────────────────────────────┐   │
│ │ BASISMODULE (immer verfügbar)                             │   │
│ ├───────────────────┬──────┬────────────────────────────────┤   │
│ │ ⚙️ Einstellungen   │  ☑   │ Projekte, Pfade, Ordner       │   │
│ │ 📁 PlanManager    │ [☑]  │ Pläne sortieren, versionieren  │   │
│ │ 📊 Dashboard      │ [☐]  │ Zentrale Übersicht, Widgets    │   │
│ └───────────────────┴──────┴────────────────────────────────┘   │
│                                                                  │
│ ┌───────────────────────────────────────────────────────────┐   │
│ │ ZUSATZMODULE                                              │   │
│ ├───────────────────┬──────┬────────────────────────────────┤   │
│ │ 📓 Bautagebuch    │ [☑]  │ Tägliches Protokoll, Auto-Fill │   │
│ │ ⏱️ Zeiterfassung  │ [☑]  │ WPF-Maske → Excel              │   │
│ │ 📐 Kalkulation    │ [☐]  │ Nachkalkulation, Bauzeitprogn. │   │
│ │ 📷 Foto           │ [☐]  │ Viewer, Tags, Geodaten         │   │
│ │ 📧 Outlook        │ [☐]  │ COM Interop, Anhänge           │   │
│ │ 🌤 Wetter         │ [☐]  │ API pro Baustelle              │   │
│ │ 📄 Vorlagen       │ [☐]  │ Excel/Word befüllen            │   │
│ └───────────────────┴──────┴────────────────────────────────┘   │
│                                                                  │
│ ┌───────────────────────────────────────────────────────────┐   │
│ │ PREMIUM                                                   │   │
│ ├───────────────────┬──────┬────────────────────────────────┤   │
│ │ 🤖 KI-Assistent   │ [☐]  │ LV-Analyse, ChatGPT/Claude    │   │
│ │ 📋 Task-Mgmt      │ [☐]  │ ClickUp/Asana Integration     │   │
│ │ 📱 Mobile PWA     │ [☐]  │ Handy-Zugriff (Server nötig)  │   │
│ └───────────────────┴──────┴────────────────────────────────┘   │
│                                                                  │
│ [Speichern]                                                      │
└─────────────────────────────────────────────────────────────────┘
```

### GUI — Einstellungen → Lizenz

```
┌─────────────────────────────────────────────────────────────────┐
│ Einstellungen → Lizenz                                           │
│                                                                  │
│ Aktueller Status:                                                │
│ ┌───────────────────┬──────────────────┬───────────────────────┐│
│ │ Modul             │ Status           │ Aktion                ││
│ ├───────────────────┼──────────────────┼───────────────────────┤│
│ │ PlanManager       │ ✅ Basis (frei)  │                       ││
│ │ Dashboard         │ ✅ Basis (frei)  │                       ││
│ │ Bautagebuch       │ ✅ Lizenziert    │                       ││
│ │ Zeiterfassung     │ ✅ Lizenziert    │                       ││
│ │ Kalkulation       │ 🟡 Test (12 Tage)│ [Lizenz kaufen]      ││
│ │ Foto              │ ⬜ Nicht aktiv    │ [30 Tage testen]     ││
│ │ KI-Assistent      │ 🔴 Test abgelauf.│ [Lizenz kaufen]      ││
│ └───────────────────┴──────────────────┴───────────────────────┘│
│                                                                  │
│ Lizenz:        Baufirma Mustermann GmbH                         │
│ Lizenz-ID:     LIC-2026-001                                     │
│ Gültig seit:   15.06.2026                                       │
│ Installationen: 2 von 3                                          │
│                                                                  │
│ [Lizenz importieren (.bpm-license)]  [Lizenz kaufen (Website)]  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. Technische Umsetzung

### 4.1 Lizenzierung

Offline-fähig, kein Online-Zwang, kein Activation-Server. Die Lizenz ist eine signierte Datei die der Käufer per E-Mail bekommt und in BPM importiert.

#### Lizenz-Kategorien

| Kategorie | Module | Preis (Idee) |
|-----------|--------|-------------|
| **Basis (kostenlos)** | Einstellungen, PlanManager, Dashboard | 0 € |
| **Zusatzmodul (einzeln)** | Bautagebuch, Zeiterfassung, Kalkulation, Foto, Outlook, Wetter, Vorlagen | je X €/einmalig |
| **Premium (einzeln)** | KI-Assistent, Task-Management, Mobile PWA | je Y €/einmalig |
| **Komplett-Paket** | Alles | Z € (Rabatt) |

**Keine Abos.** Einmalzahlung pro Modul. Updates für 1 Jahr inklusive, danach optional.

#### Trial-Schutz

Das Startdatum der Testversion wird verschlüsselt gespeichert:

```
%LocalAppData%\BauProjektManager\trial.dat
Verschlüsselt mit: Machine-GUID + App-Signatur
Enthält: { "calculation": "2026-06-15", "diary": "2026-05-01" }
```

Manipulation erschweren (nicht verhindern) — bei System-Datum-Rückstellung: Prüfung gegen letztes bekanntes Datum. Kein perfekter Schutz (ist auch nicht nötig).

#### Lizenz-Validierung (offline)

```csharp
public class LicenseValidator
{
    private const string SecretKey = "..."; // In App kompiliert
    
    public LicenseResult Validate(string licenseFilePath)
    {
        var json = File.ReadAllText(licenseFilePath);
        var license = JsonSerializer.Deserialize<LicenseFile>(json);
        
        // 1. Signatur prüfen (HMAC-SHA256)
        var expectedSig = ComputeHmac(json.WithoutSignature(), SecretKey);
        if (license.Signature != expectedSig)
            return LicenseResult.Invalid("Ungültige Signatur");
        
        // 2. Module prüfen
        foreach (var module in license.Modules)
        {
            if (module.Type == "trial" && module.ExpiresAt < DateTime.Today)
                module.Type = "expired";
        }
        
        return LicenseResult.Valid(license);
    }
}
```

Kein Online-Check. Der Secret Key ist in der App kompiliert. Nicht unknackbar, aber für eine Polier-Software im Bau-Bereich völlig ausreichend.

### 4.2 Verkaufsmodell (Zukunft)

| Modell | Beschreibung | Vorteile | Nachteile |
|--------|-------------|---------|-----------|
| **Einmalzahlung** | Einmal kaufen, für immer nutzen | Einfach, verständlich, kein Abo-Frust | Kein wiederkehrender Umsatz |
| **Einmal + Update-Abo** | Einmal kaufen + optionales Jahres-Update | Wiederkehrender Umsatz, User hat die Wahl | Etwas komplexer |
| **Modul-Pakete** | Basis kostenlos, Module einzeln oder im Paket | Niedrige Einstiegshürde, Upselling möglich | Mehr Verwaltung |

**Empfehlung:** Modul-Pakete mit Einmalzahlung. Basis kostenlos → User probiert es aus → kauft Zusatzmodule.

#### Preisfindung (Ideen, nicht verbindlich)

| Paket | Module | Preisidee |
|-------|--------|----------|
| **Basis** | Einstellungen, PlanManager, Dashboard | Kostenlos |
| **Einzelmodul** | z.B. Bautagebuch oder Zeiterfassung | 49–99 € |
| **Baustellenpaket** | Bautagebuch + Zeiterfassung + Kalkulation | 199 € |
| **Komplett** | Alle Module | 399 € |
| **Update-Paket** (optional) | 1 Jahr Updates für alle lizenzierten Module | 99 €/Jahr |

Verkaufsweg: Eigene Website → Download (Basis kostenlos) → Lizenz-Kauf (Stripe/PayPal) → `.bpm-license` per E-Mail → Import in BPM.

### 4.3 Implementierung

#### AppSettings Erweiterung

```csharp
public class ModuleSettings
{
    public bool PlanManager { get; set; } = true;
    public bool Dashboard { get; set; } = false;
    public bool Diary { get; set; } = false;
    public bool TimeTracking { get; set; } = false;
    public bool Calculation { get; set; } = false;
    public bool Photos { get; set; } = false;
    public bool Outlook { get; set; } = false;
    public bool Weather { get; set; } = false;
    public bool Templates { get; set; } = false;
    public bool AiAssistant { get; set; } = false;
    public bool TaskManagement { get; set; } = false;
    public bool MobilePwa { get; set; } = false;
}
```

#### Modul-Registry

```csharp
public record ModuleInfo(
    string Id,           // "diary"
    string DisplayName,  // "Bautagebuch"
    string Icon,         // "📓"
    string Description,  // "Tägliches Protokoll, Auto-Fill"
    string Category,     // "Basis" | "Zusatz" | "Premium"
    string[] Dependencies, // ["timeTracking"]
    Type ViewType        // typeof(DiaryView)
);

public static class ModuleRegistry
{
    public static readonly List<ModuleInfo> AllModules = new()
    {
        new("settings", "Einstellungen", "⚙️", "Projekte, Pfade, Ordner", "Basis", [], typeof(SettingsView)),
        new("planManager", "PlanManager", "📁", "Pläne sortieren", "Basis", [], typeof(PlanManagerView)),
        new("diary", "Bautagebuch", "📓", "Tägliches Protokoll", "Zusatz", ["timeTracking"], typeof(DiaryView)),
        // ... usw.
    };
}
```

#### Sidebar-Builder

```csharp
private void BuildSidebar()
{
    var settings = _settingsService.Load();
    var license = _licenseValidator.LoadCurrent();
    
    foreach (var module in ModuleRegistry.AllModules)
    {
        if (module.Id == "settings") { AddSidebarItem(module); continue; }
        if (!settings.ActiveModules.IsActive(module.Id)) continue;
        
        var licStatus = license?.GetModuleStatus(module.Id) ?? "full";
        if (licStatus == "expired") continue;
        
        AddSidebarItem(module, licStatus);
    }
}
```

---

## 5. Abhängigkeiten

| Was | Wann | Aufwand |
|-----|------|--------|
| **Modul-Aktivierung** (settings.json + Sidebar) | Wenn zweites Modul fertig ist | Klein (1-2h) |
| **Modul-Einstellungsseite** (GUI) | Wenn > 3 Module existieren | Mittel (halber Tag) |
| **Testversion (30 Tage)** | Vor erstem Verkauf | Mittel (1 Tag) |
| **Lizenz-Datei + Validierung** | Vor erstem Verkauf | Mittel (1-2 Tage) |
| **Website + Zahlungsanbieter** | Vor erstem Verkauf | Separat (nicht BPM-Code) |

**Für V1:** Nichts davon nötig. Alle Module sind "an" (es gibt ja nur Einstellungen + PlanManager). Die Modul-Aktivierung kommt wenn das dritte Modul fertig ist.

---

## 6. No-Gos / Einschränkungen

**Dieses Konzept ist NICHT:**
- Ein DRM-System (kein Online-Activation, kein Hardware-Dongle)
- Ein Abo-Modell (keine monatlichen Kosten)
- Unknackbar (bewusste Entscheidung — ehrliche Kunden zahlen)

**Dieses Konzept IST:**
- Eine saubere Modul-Steuerung für aufgeräumte Oberfläche
- Ein einfaches Lizenzmodell das offline funktioniert
- Vorbereitung für späteren Verkauf, ohne jetzt Aufwand zu machen

---

## 7. Offene Fragen

- Wie wird mit Updates umgegangen? Patch-Updates kostenlos, Feature-Updates nur mit Update-Paket?
- Soll es eine "Firmen-Lizenz" geben (unbegrenzte Installationen)?
- Wie reagiert BPM wenn die Lizenz-Datei gelöscht wird? (Fallback auf Trial-Status?)

---

*Kernfrage: "Brauche ich das um Pläne zu sortieren?" — Nein. Deshalb Won't have V1.*  
*Aber: Modul-Aktivierung ist einfach und sollte implementiert werden sobald das dritte Modul existiert.*

*Änderungen v1.0 → v1.1 (11.04.2026):*
*- Frontmatter + AI-Quickload ergänzt (DOC-STANDARD)*
*- Kapitelstruktur auf concept-Vorlage refactort*
*- Kap. 2 (Datenmodell) als Skelett ergänzt*
*- Kap. 7 (Offene Fragen) als Skelett ergänzt*
*- Kein Inhalt gelöscht — nur umgruppiert*
