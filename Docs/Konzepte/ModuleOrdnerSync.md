---
doc_id: konzept-ordner-sync
doc_type: concept
authority: secondary
status: active
owner: herbert
topics: [ordner-sync, filesystem, cloud-speicher, ordnerstruktur, abgleich]
read_when: [ordner-sync-feature, filesystem-abgleich, ordnerstruktur-sync]
related_docs: [architektur, konzept-datenarchitektur-sync]
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: Konzept für Ordner-Sync — Dateisystem-Abgleich zwischen Geräten über Cloud-Speicher
- Autorität: secondary
- Lesen wenn: Ordner-Sync-Feature, Filesystem-Abgleich
- Nicht zuständig für: Daten-Sync (→ DatenarchitekturSync.md), PlanManager-Import (→ PlanManager.md)
- Pflichtlesen: keine
- Fachliche Invarianten:
  - Cloud-Speicher-neutral — kein OneDrive-spezifischer Code
  - Nur Dateisystem-Operationen — keine DB-Sync-Logik hier

---

# BauProjektManager — Ordner-Sync (Modul-Konzept)

**Version:** 1.0
**Datum:** 09.04.2026
**Status:** Konzept (Post-PlanManager-V1)
**Autor:** Herbert + Claude
**Priorität:** Should — wichtig für Baustellen-Alltag, aber V1 geht ohne

**Verwandte Dokumente:**
- [PlanManager.md](../Module/PlanManager.md) — Kernmodul, Sync liefert Dateien an _Eingang/
- [BauProjektManager_Architektur.md](../Kern/BauProjektManager_Architektur.md) — Speicherstrategie
- [MultiUserKonzept.md](MultiUserKonzept.md) — ADR-033, ADR-037
- [DSVGO-Architektur.md](../Kern/DSVGO-Architektur.md) — Klasse B bei Personendaten in Dokumenten
- [ADR.md](../Referenz/ADR.md) — ADR-004 (Cloud-Sync-Strategie)

---

## 1. Problem

Auf Baustellen arbeiten mehrere Personen an denselben Projektunterlagen:

- **Polier (Herbert):** Hat den Projektordner auf OneDrive, sortiert Pläne mit BPM
- **Bauleiter:** Hat Pläne auf dem Firmenserver (`\\SERVER\Projekte\ÖWG\`)
- **Planer/Statiker:** Liefern Pläne über Portal oder E-Mail

**Das Problem:** Der Bauleiter hat ungeordnete Pläne am Server. Herbert muss sie manuell
rüberkopieren, dann sortieren. Umgekehrt: Herbert sortiert Pläne, aber der Bauleiter sieht
die saubere Struktur nicht weil sie auf Herberts OneDrive liegt.

**Die Lösung:** BPM synchronisiert ausgewählte Ordner bidirektional zwischen zwei Standorten.
Neue/geänderte Dateien vom Bauleiter landen automatisch im `_Eingang/` → PlanManager sortiert.
Sortierte Dateien werden zurückgesynct → Bauleiter hat die saubere Struktur.

---

## 2. Kernkonzept

### 2.1 Sync-Paare pro Projekt

Jedes Projekt kann **1..n Sync-Paare** haben. Ein Sync-Paar verbindet einen lokalen Ordner
mit einem Remote-Ordner:

```
Lokaler Ordner (OneDrive)              Remote-Ordner (Server/Netzlaufwerk)
─────────────────────────              ─────────────────────────────────────
01 Planunterlagen/         ←→          \\SERVER\Projekte\ÖWG\Pläne\
04 Protokolle/             ←→          \\SERVER\Projekte\ÖWG\Protokolle\
02 Fotos/                  ←           \\SERVER\Projekte\ÖWG\Fotos\  (nur lesen)
```

### 2.2 Sync-Richtungen

| Richtung | Symbol | Bedeutung |
|----------|--------|-----------|
| Bidirektional | ←→ | Änderungen in beide Richtungen syncen |
| Nur empfangen | ← | Nur vom Remote holen (Bauleiter → Polier) |
| Nur senden | → | Nur zum Remote schicken (Polier → Bauleiter) |

### 2.3 Sync-Modus

| Modus | Beschreibung | Wann |
|-------|-------------|------|
| **Manuell** | User klickt „Jetzt syncen" | Default für V1 |
| **Automatisch** | FileSystemWatcher/Polling, synct bei Änderung | Post-V1, optional aktivierbar |
| **Beim Import** | Sync läuft automatisch VOR dem PlanManager-Import | Post-V1, Komfort-Feature |

### 2.4 Integration mit PlanManager

Der Sync ist ein **Eingangskanal** — er liefert Dateien an den `_Eingang/`. Der PlanManager
macht den Rest (Analyse, Vorschau, Sortieren).

```
Remote-Ordner (Bauleiter)
    │
    ▼ Sync (neue/geänderte Dateien)
    │
_Eingang/ (lokaler Briefkasten)
    │
    ▼ PlanManager (Analyse → Vorschau → Import)
    │
Zielordner/ (sauber sortiert)
    │
    ▼ Rück-Sync (optional)
    │
Remote-Ordner (Bauleiter sieht saubere Struktur)
```

---

## 3. Sync-Logik

### 3.1 Änderungserkennung

Wie erkennt BPM ob eine Datei geändert wurde?

| Methode | Pro | Contra |
|---------|-----|--------|
| **Zeitstempel (LastWriteTime)** | Schnell | Unzuverlässig bei Netzlaufwerken |
| **MD5-Hash + file_size** | Zuverlässig | Langsamer (muss Datei lesen) |
| **Kombination** | Best of both | Standard-Empfehlung |

**Empfehlung V1:** Schnellcheck über Zeitstempel + file_size. Nur bei Verdacht (gleiche Größe,
anderer Zeitstempel) den MD5 berechnen. Das nutzt die MD5-Infrastruktur die der PlanManager
sowieso hat.

### 3.2 Konfliktbehandlung

Was passiert wenn dieselbe Datei auf beiden Seiten geändert wurde?

| Situation | Aktion |
|-----------|--------|
| Nur auf Remote geändert | → In _Eingang/ kopieren |
| Nur lokal geändert | → Zum Remote kopieren (bei Richtung → oder ←→) |
| Beide Seiten geändert | → **Konflikt!** User entscheidet |
| Datei auf Remote gelöscht | → Warnung anzeigen, nicht lokal löschen |
| Datei lokal gelöscht | → Nicht zum Remote löschen (Sicherheit) |

**Konflikt-Dialog:**
```
┌─────────────────────────────────────────────────────┐
│ Sync-Konflikt: S-103-D_TG Wände.pdf                │
│                                                      │
│ Lokal:  Geändert am 09.04.2026 14:32 (2,4 MB)     │
│ Remote: Geändert am 09.04.2026 15:10 (2,5 MB)     │
│                                                      │
│ [ Lokal behalten ]  [ Remote übernehmen ]  [ Beide ] │
└─────────────────────────────────────────────────────┘
```

Bei „Beide": Remote-Version bekommt Suffix `_Server` und landet im _Eingang/.

### 3.3 Was wird NICHT gesynct

- Versteckte Dateien (`.bpm-manifest`, `_plan_index.json`, `.AppData/`)
- Temporäre Dateien (`~$...`, `.tmp`, `.lock`)
- `_Archiv/` Ordner (nur lokale Versionshistorie)
- Dateien über 500 MB (konfigurierbar)

---

## 4. UI-Einordnung

### 4.1 Wo konfigurieren?

**Im Projektdetail (PlanManager → Projekt → Tab „Sync"):**

Der Sync ist projektspezifisch — jedes Projekt hat andere Sync-Partner.
Im Projektdetail neben „Automatisch (Profile)" und „Manuell sortieren" ein dritter Tab: **„Sync"**.

```
┌─────────────────────────────────────────────────────────┐
│ 202512_ÖWG-Dobl-Zwaring                [Import starten]│
├─────────────────────────────────────────────────────────┤
│ Automatisch (Profile) │ Manuell sortieren │ ★ Sync ★   │
├─────────────────────────────────────────────────────────┤
│                                                          │
│ SYNC-PAARE                                              │
│ ┌─────────────────────────────────────────────────────┐│
│ │ 01 Planunterlagen  ←→  \\SERVER\ÖWG\Pläne\         ││
│ │ Letzter Sync: 09.04.2026 14:32 · 3 neue Dateien    ││
│ │ [Jetzt syncen]  [Bearbeiten]  [Entfernen]           ││
│ └─────────────────────────────────────────────────────┘│
│ ┌─────────────────────────────────────────────────────┐│
│ │ 04 Protokolle  ←  \\SERVER\ÖWG\Protokolle\         ││
│ │ Letzter Sync: 08.04.2026 09:15 · Aktuell           ││
│ │ [Jetzt syncen]  [Bearbeiten]  [Entfernen]           ││
│ └─────────────────────────────────────────────────────┘│
│                                                          │
│ [+ Neues Sync-Paar anlegen]                             │
│                                                          │
│ EINSTELLUNGEN                                           │
│ [☐] Automatisch syncen bei App-Start                   │
│ [☐] Automatisch syncen vor Import                      │
│ Max. Dateigröße: [500] MB                               │
│                                                          │
│ SYNC-LOG (letzte 10)                                    │
│ 09.04. 14:32 ← 3 Dateien von \\SERVER (2,1 MB)        │
│ 08.04. 09:15 ← 0 Dateien (aktuell)                    │
│ 07.04. 16:45 → 5 Dateien zu \\SERVER (12,3 MB)        │
└─────────────────────────────────────────────────────────┘
```

### 4.2 Sync-Paar konfigurieren (Dialog)

```
┌─────────────────────────────────────────────────────┐
│ Sync-Paar bearbeiten                                │
│                                                      │
│ Lokaler Ordner *                                    │
│ [01 Planunterlagen                          ] [📂]  │
│ (Dropdown aus Projektordnern)                       │
│                                                      │
│ Remote-Ordner *                                     │
│ [\\SERVER\Projekte\ÖWG\Pläne\              ] [📂]  │
│ (UNC-Pfad oder Laufwerkspfad)                       │
│                                                      │
│ Richtung                                            │
│ (●) Bidirektional ←→                                │
│ ( ) Nur empfangen ←                                 │
│ ( ) Nur senden →                                    │
│                                                      │
│ Neue Dateien vom Remote:                            │
│ (●) In _Eingang/ ablegen (PlanManager sortiert)     │
│ ( ) Direkt in Zielordner kopieren (1:1 Struktur)   │
│                                                      │
│ Unterordner einbeziehen: [☑]                        │
│                                                      │
│ [Abbrechen]                          [Speichern]    │
└─────────────────────────────────────────────────────┘
```

**Wichtige Entscheidung: Wohin landen Remote-Dateien?**
- **In _Eingang/:** PlanManager erkennt, sortiert, versioniert → volle Kontrolle
- **Direkt 1:1:** Remote-Struktur wird gespiegelt → für Ordner die nicht vom PlanManager verwaltet werden (z.B. Fotos, Dokumente)

### 4.3 Einstellungen (System)

In den globalen Einstellungen ein kleiner Abschnitt für Sync-Defaults:

- Default-Serverpfad (z.B. `\\SERVER\Projekte\`)
- Default-Richtung (bidirektional)
- Auto-Sync beim App-Start (global an/aus)
- Max. Dateigröße

Die eigentliche Konfiguration bleibt pro Projekt (Tab „Sync").

### 4.4 Sidebar

Kein eigener Sidebar-Eintrag. Sync ist Teil des PlanManager-Projektdetails.
Optional: Badge in Sidebar „3 neue vom Server" wenn Auto-Sync aktiv.

---

## 5. Technische Architektur

### 5.1 Service-Schicht

```
BauProjektManager.PlanManager/
├── Services/
│   ├── FolderSyncService.cs           ← Sync-Logik (Vergleich, Kopieren, Konflikte)
│   ├── SyncPairManager.cs             ← Sync-Paare laden/speichern
│   └── SyncConflictResolver.cs        ← Konflikt-Erkennung und -Dialog
```

### 5.2 Daten

Sync-Paare werden in `profiles.json` gespeichert (pro Projekt, synct über Cloud):

```json
{
  "syncPairs": [
    {
      "id": "01HV...",
      "localFolder": "01 Planunterlagen",
      "remotePath": "\\\\SERVER\\Projekte\\ÖWG\\Pläne",
      "direction": "bidirectional",
      "targetMode": "inbox",
      "includeSubfolders": true,
      "autoSync": false,
      "maxFileSizeMb": 500,
      "lastSyncAt": "2026-04-09T14:32:00Z",
      "lastSyncFileCount": 3
    }
  ]
}
```

Sync-Log in `planmanager.db`:

```sql
CREATE TABLE sync_log (
    id TEXT PRIMARY KEY,               -- ULID
    sync_pair_id TEXT NOT NULL,
    timestamp TEXT NOT NULL,
    direction TEXT NOT NULL,           -- "pull", "push", "bidirectional"
    files_pulled INTEGER DEFAULT 0,
    files_pushed INTEGER DEFAULT 0,
    files_conflict INTEGER DEFAULT 0,
    files_skipped INTEGER DEFAULT 0,
    total_bytes INTEGER DEFAULT 0,
    status TEXT NOT NULL,              -- "completed", "partial", "failed"
    error_message TEXT
);
```

### 5.3 Netzwerk-Anforderungen

- **UNC-Pfade** (`\\SERVER\...`) und **Mapped Drives** (`Z:\...`) unterstützen
- **Offline-Handling:** Wenn Remote nicht erreichbar → Warnung, kein Absturz
- **Timeout:** Konfigurierbar, Default 30 Sekunden pro Datei
- **Keine Internet-Abhängigkeit:** Sync läuft über lokales Netzwerk, nicht über Cloud-APIs

---

## 6. Datenschutz (DSGVO)

| Prüfpunkt | Ergebnis |
|-----------|----------|
| Externe Kommunikation? | Nein — LAN/Netzlaufwerk, kein Internet |
| Datenklasse | **Klasse A** (Pläne, Protokolle) bis **Klasse B** (wenn Dokumente Personendaten enthalten) |
| IExternalCommunicationService? | Nein — kein HTTP, nur Dateisystem-Operationen |
| Audit-Log? | Ja — sync_log Tabelle (wann, was, wohin) |
| Löschung? | Sync löscht NIE Dateien — nur kopieren |

**Wichtig:** Sync löscht NIEMALS Dateien auf der Remote-Seite. Auch wenn eine Datei lokal
gelöscht wurde, wird sie nicht auf dem Server gelöscht. Sicherheit geht vor Konsistenz.

---

## 7. Abgrenzung

| Feature | Ordner-Sync | PlanManager | Multi-User (ADR-033) |
|---------|-------------|-------------|---------------------|
| Dateien kopieren | Ja | Nein | Nein |
| Dateien sortieren | Nein | Ja | Nein |
| Dateien versionieren | Nein | Ja | Nein |
| DB syncen | Nein | Nein | Ja (Event-Sync ADR-037) |
| Gleichzeitig schreiben | Nein (1 Richtung) | Nein | Ja (Write-Lock) |

**Ordner-Sync ≠ Multi-User.** Sync kopiert nur Dateien zwischen Ordnern. Es gibt keine
geteilte Datenbank, kein Write-Lock, keine Berechtigungen. Wenn echte Zusammenarbeit
nötig wird → Multi-User-Konzept (ADR-033).

---

## 8. Implementierungsreihenfolge

| Prio | Feature | Beschreibung |
|------|---------|-------------|
| 1 | Sync-Paar anlegen | Dialog: Lokal + Remote + Richtung |
| 2 | Manueller Sync | Button „Jetzt syncen" → Vergleich → Kopieren |
| 3 | Sync-Vorschau | „3 neue, 1 Konflikt" vor dem Kopieren anzeigen |
| 4 | Konflikt-Dialog | User entscheidet bei beidseitigen Änderungen |
| 5 | Sync-Log | Historie in planmanager.db |
| 6 | Auto-Sync | FileSystemWatcher/Polling (Post-V1) |
| 7 | Pre-Import-Sync | Automatisch vor PlanManager-Import |

---

## 9. Offene Architektur-Entscheidungen (spätere ADRs)

| Frage | Optionen | Entscheidung |
|-------|----------|-------------|
| Änderungserkennung | Zeitstempel vs. MD5 vs. Kombination | Offen |
| Automatischer Sync | FileSystemWatcher vs. Polling | Offen |
| Sync-Paare speichern | In profiles.json vs. eigene sync.json | Tendenz: profiles.json |
| Sammel-DWG Sync | DWG auf Remote die lokal mehreren Revisionen zugeordnet ist | Offen |

---

*Dieses Feature wird erst nach PlanManager V1 implementiert. Der PlanManager muss funktionieren
bevor ein Sync-Kanal Sinn macht — der Sync liefert nur Dateien an den Eingang, der PlanManager
sortiert sie.*

*Kernfrage: „Brauche ich das um Dokumente zu sortieren?" — Nein, aber ich brauche es um
Dokumente vom Bauleiter zu BEKOMMEN die ich dann sortieren kann.*
