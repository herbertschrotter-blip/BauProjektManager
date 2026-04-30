# Folgeprompt — CGR-2026-04-30-datenarchitektur-sync — Runde 2

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:
- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/bugfixing`** — IMMER diesen Branch verwenden, NICHT `main`!
- Bei JEDEM Dateizugriff den Branch `feature/bugfixing` angeben.

## Format-Erinnerung

- Schreibe deine GESAMTE Antwort in Canvas, **Titel: "Review Runde 2 — Datenarchitektur & Sync"**
- Direkt zu Claude sprechen, nicht zum User
- Am Ende: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen
- Klar werden, nicht "es kommt darauf an"
- Kompakt, Code/Pseudocode nur wo nötig

## Stand nach Runde 1

Wir sind uns einig:
- Kein Eigenbau β3 über OneDrive als automatischer Sync
- Kein CouchDB/PouchDB.NET-Umbau (BPM ist relational, nicht dokumentorientiert)
- **Postgres-Server-Pfad als Phase-3-Vorbereitung**
- SQLite local-first Client + PostgreSQL Server-Authority + Pull/Push mit Checkpoints + Server gewinnt bei Konflikten
- ProjectDatabase muss VOR jedem Sync syncfähig werden: keine Hard Deletes, keine Kindlisten-Replace

## User-Entscheidungen seit Runde 1

### 1. Hosting-Strategie — User-Frage: "kann man beides haben?"

Herbert möchte wissen, ob ein **Hybrid-Setup** möglich ist:
- BPM könnte konfigurierbar sein zwischen **hosted Supabase** und **self-hosted ASP.NET+Postgres**
- Backend austauschbar, ohne dass der Client umgebaut wird
- Z.B. Solo-User → Supabase (schnell), später migration zu eigenem Server wenn Multi-User real wird

### 2. Sync-Scope — Klargestellt

Der aktuelle BPM-Code hat **nur** folgende synchen-würdige Daten:
- **Stammdaten** in `bpm.db`: `projects`, `clients`, `building_parts`, `building_levels`, `project_participants`, `project_links`
- **Profile** als JSON-Dateien: `<projekt>/.bpm/profiles/<id>.json` (RecognitionProfile für PlanManager — pro Projekt im OneDrive)

Module wie Bautagebuch, Foto, Adressbuch, Zeiterfassung, Lohn etc. **existieren noch nicht im Code**. DSGVO-Klasse-B/C-Daten sind aktuell nicht relevant — kommen erst mit zukünftigen Modulen.

### 3. Weiter mit Runde 2

User möchte konkretere Architektur-Klärung in dieser Runde, statt direkt Code-Tasks zu starten.

## Aufgabe für Runde 2

Beantworte die folgenden vier Blöcke. Klar, mit Empfehlung. Kompakt.

### Block A — Hybrid-Hosting: machbar oder Komplexitäts-Overkill?

Frage: Soll BPM-Client beide Backends sprechen können (hosted Supabase **und** self-hosted ASP.NET+Postgres), oder ist das Overkill und User soll sich für **einen** Pfad entscheiden?

Konkret:
1. **Wenn machbar:** Wie sieht eine saubere Abstraktion aus? Adapter-Pattern? Strategy? Welcher Layer?
2. **Wenn machbar:** Welche **Anteile** sind Backend-spezifisch (Auth, Realtime), welche sind generisch (Pull/Push-Protokoll)?
3. **Wenn Overkill:** Welcher der beiden Pfade ist für Herbert die strategisch klügere **einzige** Wahl, mit klarer Begründung?
4. **Migration zwischen Backends später:** Wie aufwändig ist Supabase → eigener Server (oder umgekehrt) wenn Daten schon drin liegen? Realistisch?
5. Empfehlung: **eine** Pfad-Wahl ODER **beide** unterstützen ODER **erst eine, dann optional zweite nachrüsten**?

### Block B — Profile-Sync-Strategie

Profile liegen heute als JSON-Dateien in `<Projekt>/.bpm/profiles/<id>.json`. Sync passiert via OneDrive-File-Sync — funktioniert für Solo-Multi-Device, weil Profile selten geändert werden, aber konzeptuell nicht sauber (kein Konflikt-Handling, kein User-Kontext).

Drei Sub-Optionen für Profile im Server-Sync-Modell:

- **B1** Profile bleiben JSON-Dateien im Projekt-Ordner (OneDrive-Sync wie heute), Server kümmert sich nur um DB-Inhalte
- **B2** Profile wandern in die DB (neue Tabelle `recognition_profiles` mit `project_id` FK + Sync-Spalten), syncen wie Stammdaten
- **B3** Hybrid: Profile in **globaler Bibliothek auf Server** (Klasse-C-Reference) + Pro-Projekt-Instanz in DB als Kopie (User-Erwartung "Library + Instance Pattern" aus früherer Diskussion)

Bewerte konkret:
1. Welche Sub-Option passt am besten zu Server-Sync-Architektur?
2. Was passiert mit dem ADR-046 `<Projekt>/.bpm/profiles/`-Pfad — bleibt er als Backup/Cache oder wird obsolet?
3. Profile-Bibliothek (B3) — sinnvoll oder Komplexitäts-Falle?

### Block C — Spike-Reihenfolge konkret

Wenn der User in Block A "beides möglich" oder "Supabase first" entscheidet, was ist die optimale **Spike-Reihenfolge**?

Vorschlag den ich diskutieren möchte:

```
Spike 0 (Pflicht vor allem): ProjectDatabase syncfähig machen
  - Hard Deletes raus, Soft Delete via is_deleted
  - SaveBuildingParts/Participants/Links: gezielte Upserts statt Replace
  - Stabile ULIDs für ALLE Kindentitäten
  Aufwand: M-L (1-3 Tage)

Spike 1: Supabase-Spike auf Postgres
  - 1 Admin-Account, 1 User, projects-Tabelle mit RLS
  - C# Pull/Push für eine Entität
  - Realtime nur als Signal beobachten, nicht als Engine
  Aufwand: S (1 Tag)

Spike 2: Microsoft Datasync Framework (falls 2026 noch tragfähig)
  - Self-hosted ASP.NET-Backend gegen lokales Postgres
  - Vergleich zu Supabase: Setup-Aufwand, Lifecycle, .NET-Integration
  Aufwand: S-M (1-2 Tage)

Spike 3 (falls Datasync nicht passt): Eigener Minimal-API
  - ASP.NET Minimal API + EF Core + PostgreSQL
  - Pull/Push-Endpoints, Server-Version-Authority
  Aufwand: M (2-3 Tage)

Entscheidungs-Punkt nach Spikes: Welcher wird produktiv-Stack?
```

Frage: Stimmt die Reihenfolge? Reihenfolge tauschen, weglassen, ergänzen? Konkrete Tipps für jeden Spike was geprüft werden muss.

### Block D — Konflikt-Strategie + DSGVO + Edge Cases pragmatisch

**1. Server-gewinnt-Strategie:**
ChatGPT hat in Runde 1 als Frage gestellt: *"Akzeptiert Herbert Server gewinnt bei Konflikten?"*. Bewerte pragmatisch: Was sind die **realistischen Konfliktszenarien** für Solo-Multi-Device-Setup? Wenn 2-3 PCs **abwechselnd** genutzt werden (nicht parallel), wie häufig ist Konflikt überhaupt? Reicht Server-gewinnt + Audit-Log oder braucht es Konflikt-UI?

**2. DSGVO-Whitelist Phase 1:**
Da heute nur Stammdaten + Profile syncen sollen, ist DSGVO-Risiko minimal. Welche **konkreten Felder** in `bpm.db`-Tabellen sind potentiell personenbezogen (Klasse-B)?
- `project_participants` — Name, Funktion → Klasse B, brauchen aber Sync für Multi-User
- `clients` — Firma, ggf. Ansprechpartner-Name → Klasse B
- `projects` — Bauadresse → Klasse B
- ?

Soll Phase 1 schon Whitelist + DataClassification verdrahten oder das in Phase 1.5 nachrüsten?

**3. Library-Lifecycle Microsoft Datasync 2026:**
Du hattest Microsoft Datasync Framework in Runde 1 als Spike-Kandidat genannt. Wie ist der **konkrete Stand 2026**? Wird die Lib noch aktiv gepflegt (GitHub Releases, NuGet)? Welche Alternativen gibt es im .NET-Stack falls nicht?

## Bitte als nächstes

Beantworte A-D. Empfehlung am Ende: **Welche konkrete Architektur-Konstellation** soll Herbert wählen, mit Begründung in 5-8 Sätzen?

Sub-Themen die explizit NICHT in dieser Runde sind:
- Konflikt-UI-Design (nach Spike-Wahl)
- Auth-Provider-Wahl (Supabase vs ASP.NET Identity vs Auth0)
- Server-Hosting-Provider-Wahl (Hetzner vs OVH vs Heim-NAS)

Footer:
- ✅ Einigkeit
- ⚠️ Widerspruch
- ❓ Rückfragen
