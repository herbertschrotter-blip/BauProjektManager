# BauProjektManager — Mobile Web-App Konzept

**Version:** 0.3  
**Datum:** 30.03.2026 (basierend auf Session 28.03.2026)  
**Status:** Konzept (Won't have V1 — Desktop muss erst stabil sein)  
**Abhängigkeiten:** Bautagebuch-Modul (Desktop), PlanManager (Desktop)  
**ADRs:** ADR-019 (PWA statt Native), ADR-020 (Write-Lock mit Heartbeat)

---

## 1. Zielsetzung

Eine browserbasierte Web-App (PWA) für das Smartphone mit zwei Kernfunktionen auf der Baustelle:

- **Bautagebuch-Einträge** direkt vor Ort erfassen (Wetter, Personal, Tätigkeiten, Vorkommnisse)
- **Pläne ansehen** — PDF-Pläne aus den Projektordnern am Handy öffnen und durchblättern

Die App muss **offline-fähig** sein (kein garantiertes WLAN auf der Baustelle) und sich bei Netzwerkzugang automatisch synchronisieren.

---

## 2. Sync-Architektur — Zwei Optionen (beide offen gehalten)

### Option A: Cloud-Sync (wie OneNote)

Die Mobile-App greift über die **Microsoft Graph API** (oder entsprechende Cloud-API) direkt auf Dateien im Cloud-Speicher zu. Kein eigener Server nötig.

```
SMARTPHONE (PWA)
│
├── Service Worker (Offline-Cache)
├── IndexedDB (lokale Einträge)
└── Sync-Queue
        │
        ▼ Internet (Mobilfunk/WLAN)
    Cloud-API (Graph API / Google Drive API)
        │
        ▼
    Cloud-Speicher (.AppData/)
        │ automatischer Sync
        ▼
    Desktop (WPF) liest gleiche Dateien
```

**Vorteile:** Kein Server nötig, funktioniert von überall, Sync über Cloud-Infrastruktur.  
**Nachteile:** Kein SQLite über Cloud möglich → JSON-Dateien pro Tag. Cloud-API erfordert App-Registration.

**Dateiformat für Bautagebuch (Cloud-Variante):**
```
.AppData/Bautagebuch/
  202512_ÖWG-Dobl-Zwaring/
    2026-03-28.json
    2026-03-29.json
```
Ein JSON-File pro Tag pro Projekt. So schreibt immer nur ein Gerät pro Datei, und Cloud-Sync funktioniert konfliktfrei.

### Option B: Lokaler Server (ASP.NET Minimal API)

Die Mobile-App verbindet sich per REST API mit einem kleinen Server im LAN. Der Server läuft auf dem PC, Laptop, oder einem Raspberry Pi.

```
SMARTPHONE (PWA)
│
├── Service Worker (Offline-Cache)
├── IndexedDB (lokale Einträge)
└── Sync-Queue
        │
        ▼ WLAN (lokales Netzwerk)
    ASP.NET Minimal API
    (auf PC/Laptop/Raspi)
        │
        ▼
    SQLite (bpm.db)
    Projektordner (PDF-Pläne)
```

**Vorteile:** Volle SQLite-Power, kein Internet nötig, kein Cloud-Dienst.  
**Nachteile:** Server muss laufen, nur im LAN erreichbar, Raspi-Setup nötig.

### Empfehlung

Beide Optionen offen halten. Option A für Herbert allein (schon Cloud-Speicher vorhanden). Option B für Firmen-Setup mit mehreren Nutzern (passt zu ADR-033 Multi-User Modus C).

---

## 3. Architektur-Übersicht (Option B — Server)

```
┌─────────────────────────────────────────────────┐
│  BAUSTELLE (Smartphone Browser)                 │
│                                                 │
│  ┌───────────────┐    ┌──────────────────┐      │
│  │ Bautagebuch   │    │ Plan-Viewer      │      │
│  │ (Einträge)    │    │ (PDF cached)     │      │
│  └───────┬───────┘    └────────┬─────────┘      │
│          │                     │                 │
│  ┌───────┴─────────────────────┴─────────┐      │
│  │       Service Worker (PWA)            │      │
│  │  - Offline-Cache (App + Daten)        │      │
│  │  - Background Sync Queue             │      │
│  │  - IndexedDB für lokale Einträge     │      │
│  └───────────────────┬───────────────────┘      │
└──────────────────────┼──────────────────────────┘
                       │ WLAN/Netz
┌──────────────────────┼──────────────────────────┐
│  BÜRO / ZUHAUSE      │                          │
│                      ▼                          │
│  ┌───────────────────────────────────────┐      │
│  │  BPM Web-Server (ASP.NET Minimal API)│      │
│  │  - GET/POST /api/tagebuch            │      │
│  │  - GET /api/plaene/{projekt}         │      │
│  │  - GET /api/projekte                 │      │
│  └───────────────────┬───────────────────┘      │
│                      │                          │
│  ┌───────────────────┴───────────────────┐      │
│  │  BauProjektManager Desktop (WPF)     │      │
│  │  - SQLite (bpm.db)                   │      │
│  │  - Projektordner (PDF-Pläne)         │      │
│  └───────────────────────────────────────┘      │
└─────────────────────────────────────────────────┘
```

---

## 4. Offline-Strategie (PWA)

### Prinzip: "Sync When You Can, Work When You Can't"

**Morgens (WLAN im Büro/Container):**
1. App öffnen → Service Worker synchronisiert automatisch
2. Projektliste + aktuelle Pläne werden gecached
3. Gestrige Offline-Einträge werden hochgeladen

**Tagsüber (Baustelle, kein Netz):**
1. App funktioniert vollständig offline
2. Bautagebuch-Einträge werden lokal in IndexedDB gespeichert
3. Pläne aus dem Cache anzeigen
4. Sync-Queue sammelt alle Änderungen

**Abends (WLAN verfügbar):**
1. Background Sync schickt alle Einträge zum Server/Cloud
2. Desktop-App sieht die neuen Einträge sofort

### Konflikt-Behandlung

Da nur das Handy Einträge *erstellt* und der Desktop sie *liest/verwaltet*, ist das Konfliktrisiko minimal. Regel: **Mobile erstellt, Desktop verwaltet.** Falls doch ein Konflikt entsteht (gleicher Eintrag auf zwei Geräten bearbeitet), gewinnt der neuere Zeitstempel mit Warnung.

---

## 5. Write-Lock Mechanismus (ADR-020)

Wenn Desktop und Mobile gleichzeitig schreiben wollen:

| Aktion | Wer | Verhalten |
|--------|-----|-----------|
| Bautagebuch-Eintrag erstellen | Mobile | Immer erlaubt (append-only) |
| Bautagebuch-Eintrag bearbeiten | Desktop | Lock für den Eintrag |
| Projekt-Einstellungen ändern | Desktop | Exklusiver Lock |
| Plan-Import | Desktop | Exklusiver Lock |
| Lesen (alles) | Beide | Immer erlaubt |

**Heartbeat:** Alle 60 Sekunden sendet der Lock-Halter ein "ich bin noch da". Auto-Release nach ~3 verpassten Heartbeats (~3 Minuten). Bei Offline: großzügigerer Timeout (30 Min).

---

## 6. Technologie-Stack

| Komponente | Technologie | Begründung |
|---|---|---|
| Backend/API | ASP.NET Minimal API (.NET 10) | Gleiche Plattform wie Desktop-App |
| Frontend | HTML + Vanilla JS | Kein Framework-Overhead, PWA-fähig |
| Offline | PWA (Service Worker + IndexedDB) | Standard, funktioniert auf jedem Handy |
| Datenformat | JSON + SQLite | Konsistent mit Desktop-Architektur |
| Sync | REST-API (Option B) oder Cloud-API (Option A) | Beide Optionen offen |
| Pläne | PDF-Viewer im Browser | Pläne werden bei Sync gecached |

**Offene Entscheidung:** Blazor WASM (C# überall, schwerer) vs. Vanilla JS (leichter, zweite Sprache). Entscheidung wenn es soweit ist.

---

## 7. API-Endpunkte (Option B — Server)

```
GET    /api/projekte                  → Projektliste
GET    /api/projekte/{id}             → Projektdetails
GET    /api/projekte/{id}/plaene      → Planliste mit Metadaten
GET    /api/plaene/{datei}            → PDF-Download (cached)
GET    /api/tagebuch/{projektId}      → Alle Einträge eines Projekts
GET    /api/tagebuch/{projektId}/{datum} → Eintrag für ein Datum
POST   /api/tagebuch                  → Neuen Eintrag erstellen
PUT    /api/tagebuch/{id}             → Eintrag bearbeiten
POST   /api/sync                      → Batch-Upload (alle Offline-Einträge)
GET    /api/wetter/{lat}/{lon}        → Aktuelles Wetter
```

---

## 8. Hardware-Optionen für Server (Option B)

| Hardware | Kosten | Stromverbrauch | Leistung |
|----------|--------|---------------|----------|
| Raspberry Pi 5 (4GB) | ~€80 | ~5W | Für BPM mehr als genug |
| Raspberry Pi 4 (4GB) | ~€60 | ~5W | Ebenfalls ausreichend |
| Alter Laptop/PC | €0 | ~30-80W | Overkill, aber gratis |
| Intel NUC / Mini-PC | ~€200 | ~15W | Wenn Raspi nicht reicht |

Empfehlung: Raspberry Pi 5 mit .NET 10 ARM64. Setup in ~30 Minuten: .NET SDK installieren, `dotnet publish` → eine Datei kopieren → fertig.

---

## 9. Voraussetzungen (bevor Mobile sinnvoll ist)

| Voraussetzung | Status |
|--------------|--------|
| PlanManager fertig (V1) | ⬜ In Arbeit |
| Bautagebuch-Modul (Desktop) | ⬜ Konzept vorhanden |
| Stabile Desktop-App | ⬜ |
| API-Endpunkte definiert | ✅ (dieses Dokument) |
| PWA-Grundgerüst | ⬜ |
| Write-Lock implementiert | ⬜ Konzept (ADR-020) |

---

## 10. Abgrenzung

**Die Mobile-App ist NICHT:**
- Ein Ersatz für die Desktop-App (keine Projektanlage, kein Import, kein Profil-Anlernen)
- Eine native App (kein App Store, kein Download)
- Mandantenfähig (eine Firma, ein Server)

**Die Mobile-App IST:**
- Ein schlanker Begleiter für zwei Aufgaben: Bautagebuch + Pläne lesen
- Offline-fähig (PWA)
- Automatisch synchronisierend
- Ohne Installation nutzbar (URL im Browser)

---

*Kernfrage: "Brauche ich das um Pläne zu sortieren?" — Nein. Deshalb Won't have V1.*  
*Aber: Bautagebuch am Handy schreiben und Pläne am Handy anschauen sind die zwei häufigsten Wünsche von Polieren auf der Baustelle.*