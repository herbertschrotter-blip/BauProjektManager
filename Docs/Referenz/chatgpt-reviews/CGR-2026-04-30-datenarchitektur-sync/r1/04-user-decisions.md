# User-Entscheidungen — CGR-2026-04-30-datenarchitektur-sync — Runde 1

**Stand:** 2026-04-30 nach Stufe-A

---

## Antworten zu den 3 Stufe-A-Fragen

### 1. Phase-3-Server: hosted EU-BaaS oder self-hosted?

**User:** *"kann man beides haben?"*

**Interpretation:** User möchte wissen ob ein **Hybrid-Setup** möglich ist — Server-Backend austauschbar zwischen hosted (Supabase) und self-hosted (ASP.NET+Postgres). Implikation: Architektur sollte das Backend abstrahieren, damit Backend-Wechsel ohne Code-Umbau im Client möglich ist.

**Folge:** Diese Frage geht in Runde 2 an ChatGPT — konkrete Bewertung ob Hybrid-Hosting für BPM sinnvoll ist oder Komplexitäts-Overkill.

### 2. Sync-Scope am Anfang

**User:** *"es gibt stand jetzt noch kein bautagebuch oder sonstiges. nur stammdaten und profile für planmanager."*

**Klargestellt:** Im aktuellen Code-Stand existieren nur:
- Stammdaten (`projects`, `clients`, `building_parts`, `building_levels`, `project_participants`, `project_links`)
- Profile (`<projekt>/.bpm/profiles/<id>.json` als RecognitionProfile via JSON-Datei)

Bautagebuch, Foto, Adressbuch, Zeiterfassung etc. sind **noch nicht implementiert**. DSGVO-Klasse-B-Daten gibt es noch nicht.

**Folge für Runde 2:**
- Sync-Scope = Stammdaten + Profile
- DSGVO-Aufwand minimal in Phase 1 (kaum Personendaten)
- Profile-Sync ist eigenes Sub-Thema: liegt heute pro Projekt als JSON, soll in Server-Sync rein oder bleibt als JSON?

### 3. Wie weiter

**User:** *"Direkt Runde 2 mit ChatGPT"*

**Folge:** Folgeprompt für r2 wird vorbereitet. Inhalte:
- Frage 1 (Hybrid-Hosting möglich?) als Hauptthema
- Profile-Sync-Strategie als zweiter Block
- Konkrete Spike-Reihenfolge
- Konflikt-Strategie "Server gewinnt"
- ProjectDatabase syncfähig-Refactor: vor oder neben dem Spike?

## Nicht beantwortet (kommt eventuell in Runde 3)

- Login-Pflicht ab wann?
- VPS organisatorisch okay?
- Server-gewinnt akzeptabel als Konflikt-Strategie?

→ Diese Detail-Fragen werden in Runde 2 von ChatGPT pragmatisch bewertet, nicht direkt vom User.
