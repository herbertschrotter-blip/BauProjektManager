# ChatGPT-Antwort — CGR-2026-04-30-datenarchitektur-sync — Runde 2

**Modell:** GPT-5.4
**Format:** Canvas — "Review Runde 2 — Datenarchitektur & Sync"
**Eingegangen:** 2026-04-30

---

Claude,

Runde 2 verschiebt die Empfehlung präziser: **BPM sollte nicht zwei vollständige Backends gleichzeitig unterstützen.** BPM sollte stattdessen **ein generisches Sync-Protokoll im Client definieren** und **zuerst einen self-hosted-/ASP.NET-kompatiblen Serverpfad bauen oder spiken**. Supabase kann als schneller Postgres/Auth/RLS-Spike dienen, aber nicht als primäre Architekturachse. Die stabile Achse ist: `BPM Client ↔ IBpmSyncClient ↔ Pull/Push Contract ↔ Server Adapter ↔ PostgreSQL`.

Ich habe im Branch `feature/bugfixing` nochmals gegengeprüft: Aktuell sind wirklich nur die sechs Stammdaten-Tabellen in `bpm.db` plus PlanManager-Profile relevant. `PlanManagerDatabase.cs` bleibt sauber local-only für Cache/Journal/Undo; das darf nicht in den Server-Sync geraten. `ProjectDatabase.cs` bleibt der kritische Umbaupunkt, weil Sync-Spalten zwar existieren, aber Hard Deletes und Kindlisten-Replace noch gegen Sync arbeiten.

## Block A — Hybrid-Hosting: machbar, aber nur als Protokoll-Hybrid

### 1. Soll BPM beide Backends sprechen können?

**Nicht als gleichwertige Doppelimplementierung in V1.** Das wäre Overkill. Zwei vollwertige Backendpfade bedeuten doppelte Auth, doppelte Fehlerfälle, doppelte Doku, doppelte Tests, doppelte Supportoberfläche. Für ein Solo-Projekt ist das der klassische Architektur-Fetisch: technisch elegant, praktisch teuer.

**Ja als saubere Abstraktion auf Protokollebene.** Der Client darf nicht wissen, ob hinten Supabase, ASP.NET Minimal API oder CommunityToolkit.Datasync hängt. Er soll nur ein fachliches Sync-Interface kennen.

Empfohlener Layer:

```text
WPF / ViewModels
  ↓
Application Services
  ↓
Local Store: ProjectDatabase / zukünftige Repositories
  ↓
Sync Application Layer
  - ISyncCoordinator
  - ISyncClient
  - IAuthSessionProvider
  - ISyncCheckpointStore
  ↓
Infrastructure Adapter
  - SupabaseSyncClient       optional Spike
  - BpmServerSyncClient      ASP.NET/Minimal API
  - DatasyncSyncClient       falls Toolkit übernommen wird
```

Kein Strategy-Pattern in ViewModels. Kein Backend-Switch quer durch die App. Der Switch sitzt ausschließlich in Infrastructure/DI-Konfiguration.

Minimaler Contract:

```text
interface IBpmSyncClient
  AuthenticateAsync()
  PullAsync(projectId, afterCheckpoint) -> SyncPullResult
  PushAsync(projectId, pendingChanges) -> SyncPushResult
  GetServerTimeAsync()
  GetCapabilitiesAsync() -> { supportsRealtimeSignal, supportsRls, protocolVersion }
```

Wichtig: `IBpmSyncClient` ist **BPM-eigen**, nicht Supabase-eigen, nicht Datasync-eigen. Das schützt die App.

### 2. Backend-spezifisch vs. generisch

**Generisch:**

- Entitäten und DTOs: `ProjectDto`, `ClientDto`, `BuildingPartDto`, `RecognitionProfileDto`.
- Pull/Push-Vertrag: `changes since checkpoint`, `accepted`, `rejected`, `serverVersion`, `deleted rows`.
- Checkpoints pro Tabelle/Projekt.
- Soft Delete.
- Retry/Idempotenz.
- Sync-Status lokal: pending/synced/rejected/conflict.
- Server-gewinnt-Regel.
- Audit-Grundlogik.

**Backend-spezifisch:**

- Auth: Supabase JWT vs. ASP.NET Identity/JWT vs. Datasync Auth-Handler.
- Realtime-Signal: Supabase Realtime vs. SignalR vs. Datasync Refresh/Pull-Timer.
- RLS/Authorization: Postgres RLS direkt bei Supabase vs. ASP.NET Policy/EF Filter + optional Postgres RLS.
- Storage/Attachments später: Supabase Storage vs. eigener Blob/File-Endpoint.
- Admin/Userverwaltung.
- Deployment/Secrets.

Der Fehler wäre, Supabase-Realtime in das BPM-Syncmodell einsickern zu lassen. Realtime bleibt nur ein Signal: „Pull jetzt ausführen".

### 3. Wenn nur ein Pfad: welcher?

Wenn Herbert **einen** strategisch klugen Pfad wählen soll: **self-hosted ASP.NET + PostgreSQL mit CommunityToolkit.Datasync-Spike zuerst**.

Begründung: BPM ist .NET/WPF/SQLite/Postgres-orientiert. Der bestehende Architekturentwurf spricht ASP.NET, PostgreSQL, lokale SQLite, Server-Authority und Offline-Cache. CommunityToolkit.Datasync ist 2026 aktiver und näher am Stack als das alte Microsoft.Datasync-Paket; es unterstützt ASP.NET Core/.NET, WPF und PostgreSQL/EF-Core-nahe Backends. Supabase ist schneller für Auth/RLS-Prototyping, aber als Client-Sync-Foundation im WPF-Kontext weniger kontrollierbar.

### 4. Migration Supabase → eigener Server realistisch?

**Datenmigration:** realistisch und moderat, wenn das Schema BPM-eigen bleibt.

- Supabase ist Postgres. Dump/Restore oder ETL ist machbar.
- Tabellen, ULIDs, Soft Deletes, server_version, memberships können übernommen werden.
- RLS-Policies und Auth-User sind der knifflige Teil, nicht die Fachdaten.
- Supabase Auth → ASP.NET Identity ist keine 1:1-Migration. Passwörter/Hashes würde ich nicht übertragen wollen; besser: User neu einladen/Passwort setzen.
- Realtime-Subscriptions werden ersetzt durch SignalR/Polling/Datasync-Refresh.

**Aufwand bei sauberem BPM-Protokoll:** 3–8 PT für kleine Datenmenge + neues Auth-Onboarding. 10–20 PT, wenn RLS, Storage, Audit, Rollen und mehrere Module schon produktiv sind.

**Umgekehrt eigener Server → Supabase:** Daten ebenfalls machbar; Policies/Auth/Storage wieder der Aufwand.

### 5. Empfehlung für Block A

**Erst eine produktive Backend-Linie, zweite nur nachrüsten wenn echter Bedarf entsteht.**

Aber: Die Client-Architektur muss von Tag 1 so geschnitten sein, dass der Adapter austauschbar bleibt. Also nicht „beide Backends bauen", sondern „BPM-Protokoll bauen, einen Adapter produktiv machen". Meine Präferenz für den produktiven Pfad: **CommunityToolkit.Datasync oder eigener ASP.NET Minimal API über PostgreSQL**. Supabase nur als schneller Gegencheck/Spike für Postgres+Auth+RLS, nicht als Zielbindung.

## Block B — Profile-Sync-Strategie

### Bewertung B1/B2/B3

#### B1 — Profile bleiben JSON im Projektordner

**Pro:**

- Funktioniert heute schon für Solo-Multi-Device.
- Minimaler Aufwand.
- Profile ändern sich selten.
- Kein DB-Schema nötig.

**Contra:**

- Zwei Wahrheiten: DB-Sync über Server, Profile über OneDrive.
- Kein sauberer User-Kontext, keine Version, kein Server-Checkpoint.
- Konflikte bleiben Cloud-Drive-Konflikte.
- Servermodus wäre unvollständig: Ein neues Gerät bekommt Stammdaten vom Server, aber Profile nur wenn OneDrive-Projektordner korrekt vorhanden ist.

**Urteil:** Als Übergang okay, als Server-Sync-Ziel falsch.

#### B2 — Profile wandern in DB

**Pro:**

- Einheitlicher Sync-Pfad.
- Gleiche Sync-Spalten, gleiche Server-Authority, gleiche Checkpoints.
- Neues Gerät bekommt alles Relevante über Server.
- Einfach zu implementieren: Profile sind selten geändert und fachlich eigenständige Entitäten.

**Contra:**

- JSON-Datei-Pfad aus ADR-046 verliert seine Rolle als führender Speicherort.
- PlanManager muss Profile über einen Service laden, nicht direkt aus Dateien.
- Export/Backup-Dateien müssen aus DB generiert werden.

**Urteil:** Beste Option für Phase-3-Server-Sync.

Vorschlag Tabelle:

```sql
recognition_profiles (
  id TEXT PRIMARY KEY,
  project_id TEXT NOT NULL,
  name TEXT NOT NULL,
  document_type TEXT NOT NULL,
  profile_json TEXT NOT NULL,
  created_at TEXT NOT NULL,
  created_by TEXT NOT NULL DEFAULT '',
  last_modified_at TEXT NOT NULL,
  last_modified_by TEXT NOT NULL DEFAULT '',
  sync_version INTEGER NOT NULL DEFAULT 0,
  is_deleted INTEGER NOT NULL DEFAULT 0,
  FOREIGN KEY(project_id) REFERENCES projects(id)
);
```

Für die Frühphase: keine Migration. Alte JSON-Dateien dürfen gelöscht/neu exportiert werden. Wenn Import gewünscht ist, nur als manuelles Dev-/Import-Tool, nicht als Legacy-Toleranz im Loader.

#### B3 — Globale Bibliothek + Projektinstanz

**Pro:**

- Fachlich richtig: Ein Template ist ein Vorschlag, ein Projektprofil ist die konkrete Instanz.
- Gute User-Erwartung: „Ich habe ein bewährtes Profil und übernehme es ins Projekt."
- Konflikte sinken, weil Projektinstanzen unabhängig sind.
- Passt zu Klasse-C-Reference-Daten.

**Contra:**

- Mehr UI und mehr Begriffe: Bibliothek, Vorlage, Kopie, Update, Abgleich.
- Versionierung von Templates wird relevant: Was passiert, wenn die Library-Vorlage geändert wird, nachdem Projekt A eine Kopie hat?
- Für den aktuellen Codeumfang nicht nötig, wenn nur Projektprofile existieren.

**Urteil:** Fachlich sinnvoll, aber nicht als erster Sync-Schritt. B3 ist eine gute Zielstruktur nach B2, nicht davor.

### Was passiert mit ADR-046 `<Projekt>/.bpm/profiles/`?

Der Pfad bleibt, aber seine Rolle ändert sich:

```text
Heute:
  .bpm/profiles/*.json = führender Speicher für PlanManager-Profile

Server-Sync-Ziel:
  recognition_profiles in bpm.db/server = führend
  .bpm/profiles/*.json = Export/Backup/Interop/Diagnose, optionaler Cache
```

Ich würde ADR-046 nicht löschen, sondern präzisieren: `.bpm/profiles/` ist im Servermodus **abgeleiteter Export**, nicht Source of Truth. Im reinen Solo/ohne Server darf der Dateipfad noch führend bleiben, aber sobald Server-Sync aktiv ist, gilt DB/Server.

### Empfehlung Block B

**B2 jetzt als Ziel wählen. B3 später ergänzen. B1 nur als Übergang beibehalten.**

Der erste Server-Sync sollte Profile als DB-Entität behandeln. Die globale Bibliothek ist sinnvoll, aber erst wenn der einfache Projektprofil-Sync funktioniert. Sonst baut ihr vor dem Fundament schon die Komfortetage.

## Block C — Spike-Reihenfolge konkret

Deine vorgeschlagene Reihenfolge ist fast richtig, aber ich würde sie straffen und einen Punkt korrigieren:

**Nicht mehr „Microsoft Datasync" spiken, sondern „CommunityToolkit.Datasync".** Das alte Azure/Microsoft.Datasync-Paket ist archiviert/abgelöst. 2026 ist die relevante Linie CommunityToolkit.Datasync. Es gibt 10.0.0-Pakete aus Februar 2026, .NET-10-/ASP.NET-Core-10-Ausrichtung, WPF ist in den getesteten Clientplattformen genannt, PostgreSQL ist als unterstützbares DB-Ziel über EF/Core-Repository-Pfad vorgesehen. Das macht den Spike wieder sinnvoll.

### Empfohlene Reihenfolge

```text
Spike 0: ProjectDatabase syncfähig machen
Spike 1: CommunityToolkit.Datasync gegen ASP.NET + PostgreSQL
Spike 2: Eigener Minimal-API Row-Sync als Fallback/Benchmark
Spike 3: Supabase-Spike nur für Auth/RLS/Realtime-Erkenntnis
Entscheidung: Toolkit übernehmen oder eigenen Minimal-API bauen
```

Warum Supabase nach hinten? Weil die eigentliche strategische Frage nicht „kann Supabase Postgres?" lautet. Ja, kann es. Die entscheidende Frage ist: „Gibt es einen .NET-nativen Offline-Sync-Pfad, der BPM viel Eigenbau spart?" Das muss zuerst geklärt werden. Wenn CommunityToolkit.Datasync passt, braucht Supabase höchstens noch als Hosting/Auth-Alternative diskutiert werden.

### Spike 0 — Pflicht vor allem

Deine Punkte stimmen, aber ich würde sie schärfen:

```text
- Hard Deletes in shared Tabellen entfernen
- DeleteProject: is_deleted=1 rekursiv/gezielt, kein DELETE
- SaveBuildingParts/Participants/Links: kein Replace-All
- Upsert je Kind-ID
- entfernte UI-Kinder als is_deleted=1 markieren
- Load-Queries filtern is_deleted=0
- sync_version nur bei echter Änderung erhöhen
- stabile ULID bleibt erhalten
- DB-Datei löschen und neu anlegen lassen, keine Migration
```

Aufwand: **1–3 Tage**, realistisch eher 2–3, weil Kindlisten-Diff sauber sein muss.

Zu prüfen:

- Speichern eines Projekts verändert unveränderte Kind-IDs nicht.
- Entfernen eines Participants erzeugt Soft Delete.
- Umbenennen eines BuildingPart ist Update, kein Delete+Insert.
- Projekt löschen löscht nicht physisch.
- Export `project.json` ignoriert gelöschte Rows.

### Spike 1 — CommunityToolkit.Datasync

Scope:

```text
- ASP.NET Core 10 Server
- PostgreSQL via EF Core
- Tabelle: projects oder clients, nicht alle sechs
- WPF/.NET 10 Client
- lokale SQLite/offline store prüfen
- Pull all
- lokale Änderung offline
- Push online
- Server-gewinnt-Konflikt
- Soft Delete
- Auth-Stub reicht, noch kein echtes Rollenmodell
```

Prüfpunkte:

- Funktioniert mit bestehender SQLite-Strategie oder verlangt es eigenen Offline-Store?
- Können BPM-Tabellen mit `id`, `last_modified_at`, `sync_version`, `is_deleted` sauber gemappt werden, oder erzwingt das Toolkit eigene Felder?
- Wie sieht Conflict Handling konkret aus?
- Wie wird serverseitige Autorisierung pro Projekt implementiert?
- Kann der Server PostgreSQL ohne Azure-Abhängigkeit betreiben?
- Wie viel Boilerplate entsteht pro Tabelle?
- Ist die Library-Doku ausreichend für Solo-Wartung?

Entscheidungskriterien:

```text
GO, wenn:
  - WPF + SQLite + ASP.NET + Postgres stabil laufen
  - Soft Delete und Konflikte sauber modellierbar sind
  - keine Azure-Bindung entsteht
  - pro Tabelle wenig Sondercode nötig ist

NO-GO, wenn:
  - bestehende lokale DB-Struktur stark umgebaut werden muss
  - Konflikt-/Authmodell zu undurchsichtig ist
  - Debugging schlechter ist als eigener Row-Sync
```

Aufwand: **1–2 Tage**.

### Spike 2 — Eigener Minimal-API Row-Sync

Den würde ich nicht erst als Notausgang nach wochenlanger Datasync-Erkundung machen, sondern direkt als Benchmark danebenstellen. Klein halten:

```text
POST /sync/push
GET  /sync/pull?projectId=&afterServerVersion=
GET  /sync/bootstrap?projectId=
```

Servermodell:

```text
server_version BIGINT GENERATED BY SERVER
updated_at_server_utc
is_deleted
```

Push-Regel:

```text
if client.base_server_version < server.current_version:
  reject or server_wins
else:
  accept, assign new server_version
```

Prüfpunkte:

- Wie viele Zeilen Code für eine Entität?
- Ist Debugging trivial?
- Ist idempotenter Retry einfach?
- Wie leicht lässt sich RLS/Policy später ergänzen?
- Kann man denselben DTO-Vertrag für alle Stammdaten nutzen?

Aufwand: **2–3 Tage**.

### Spike 3 — Supabase

Supabase-Spike würde ich nicht streichen, aber begrenzen:

```text
- projects-Tabelle in Supabase Postgres
- Auth: 1 User
- RLS: User sieht nur eigene/project_membership Rows
- C# Pull/Push über REST/PostgREST oder Supabase-C# prüfen
- Realtime nur als Signal: bei Event -> Pull auslösen
```

Prüfpunkte:

- Ist die C#-Library für WPF robust genug?
- Wie gut ist Fehlerdiagnose bei RLS?
- Kann Service-Key vollständig aus Client rausbleiben?
- Wie würden Backups/Export zu eigenem Postgres aussehen?
- Wie viel Supabase-spezifischer Code entsteht?

Aufwand: **1 Tag**.

### Entscheidungspunkt

Nach diesen Spikes gibt es nur drei mögliche Ergebnisse:

```text
A: CommunityToolkit.Datasync passt
   -> produktiver Pfad: ASP.NET + PostgreSQL + Toolkit

B: Toolkit passt nicht, Minimal-API ist überschaubar
   -> produktiver Pfad: eigener Row-Sync

C: Herbert will sofort hosted und akzeptiert Supabase-Abhängigkeit
   -> Supabase als erster Server, aber BPM-Sync-Contract bleibt eigen
```

Meine Prognose: **B oder A**, nicht C.

## Block D — Konfliktstrategie + DSGVO + Edge Cases

### 1. Server gewinnt: reicht das?

Für Herberts aktuelles Setup: **Ja, Server gewinnt reicht. Keine Konflikt-UI in Phase 1.**

Realistische Konflikte bei Solo/2–3 PCs abwechselnd:

- PC A ändert Projektname offline, PC B ändert denselben Projektname bevor A gepusht hat.
- PC A entfernt einen Beteiligten, PC B ändert Telefonnummer desselben Beteiligten.
- PC A bearbeitet BuildingPart-Liste, PC B sortiert/ändert dieselben Bauteile.
- Profil wird auf Surface und Desktop verändert, bevor beide synchronisiert haben.

Bei „abwechselnd, nicht parallel" ist die Häufigkeit niedrig. Die Hauptursache ist nicht echte Parallelität, sondern vergessener Sync vor Gerätewechsel oder ein Gerät war länger offline.

Pragmatische Strategie:

```text
Phase 1:
  - Server gewinnt
  - lokaler Pending Change wird bei Konflikt überschrieben oder rejected
  - Sync-Log zeigt: geändert von Gerät/User/Zeit
  - kein Merge-Dialog

Phase 1.5:
  - einfacher Änderungsverlauf/Audit sichtbar
  - optional „lokale Änderung erneut anwenden" für Textfelder

Phase 2+:
  - Konflikt-UI nur für echte Fachmodule mit hoher Konfliktwahrscheinlichkeit
```

Für Stammdaten ist Konflikt-UI Overkill. Audit reicht, plus saubere Sync-Statusmeldung: „Lokale Änderung wurde durch Serverstand ersetzt."

Wichtig: `last_modified_at` vom Client darf nicht allein entscheiden. Der Server vergibt `server_version`. Konfliktregel basiert auf `base_server_version`, nicht auf Uhrzeit.

### 2. DSGVO-Whitelist Phase 1

Auch wenn Klasse-B/C-Module noch nicht existieren: Stammdaten sind nicht DSGVO-frei. Es gibt personenbezogene oder personenbeziehbare Felder.

Potentiell Klasse B:

```text
clients:
  contact_person
  phone
  email
  notes
  company, wenn Einzelunternehmen oder personenbezogene Firma

projects:
  street
  house_number
  postal_code
  city
  municipality
  cadastral_kg
  cadastral_gst
  coordinate_east
  coordinate_north
  notes
  full_name/name, wenn Personenname enthalten ist

project_participants:
  contact_person
  phone
  email
  company, wenn personenbezogen/Einzelunternehmen
  role

project_links:
  url, wenn personenbezogene Token, Share-Links oder Portalpfade enthalten sind
  name, wenn Personenbezug enthalten ist

building_parts/building_levels:
  meist Klasse A/Bauwerksdaten, aber personenbeziehbar über Projektkontext

recognition_profiles:
  normalerweise Klasse A/C-Reference, außer Namen/Patterns enthalten Projekt-/Personendaten
```

Empfehlung: **Whitelist jetzt minimal verdrahten, aber nicht als großes DSGVO-Framework bauen.**

Das heißt konkret:

```text
- DataClassification enum im Domain/Contracts-Bereich
- pro Sync-DTO eine statische Klassifikation: A/B/C
- Sync-Client darf nur whitelisted DTO-Typen pushen/pullen
- Phase 1 Whitelist: projects, clients, building_parts, building_levels, project_participants, project_links, recognition_profiles
- Klasse C bleibt gesperrt, bis Server-Rollen/Audit existieren
- kein externes Senden ohne IExternalCommunicationService
```

Nicht nötig in Phase 1:

- Feldweises Masking.
- Consent-UI.
- Vollständiger Datenschutzbericht.
- Löschfristen-Automation.
- DPA-/Provider-Workflow im Code.

Aber: Die Klassifikation sollte jetzt ins Sync-Protokoll, damit später kein Wildwuchs entsteht.

### 3. Library-Lifecycle Datasync 2026

Stand 2026: **nicht mehr auf `Microsoft.Datasync.*` als Zukunftspfad setzen.** Das alte Azure/Microsoft-Datasync-Repo ist archiviert und verweist auf `CommunityToolkit/Datasync`. Die alten Microsoft-Datasync-NuGets sind bei 6.1.0 aus 2024 stehen geblieben.

Der aktive Kandidat ist **CommunityToolkit.Datasync**:

- CommunityToolkit/Datasync, .NET Foundation/Community Toolkit Umfeld.
- NuGet-Version 10.0.0 wurde im Februar 2026 veröffentlicht.
- Ziel: ASP.NET Core 10.x / .NET 10.x.
- WPF ist als getestete Clientplattform genannt.
- Datenbankpfad umfasst unter anderem PostgreSQL, SQLite, EF-Core-nahe Stores bzw. Repository-Pattern.

Das ist gut genug für einen Spike, aber noch kein Freibrief. Downloadzahlen und Ökosystem sind nicht vergleichbar mit EF Core oder ASP.NET Identity. Deshalb: Spike ja, Architekturanker nein. Architekturanker bleibt das BPM-eigene Pull/Push-Protokoll.

Alternativen im .NET-Stack, falls CommunityToolkit.Datasync nicht passt:

```text
1. Eigener Minimal-API Row-Sync mit EF Core + PostgreSQL
   - meine bevorzugte Fallback-Option
   - wenig Magie, gut debugbar

2. ASP.NET + SignalR nur als Realtime-Signal
   - ergänzt Row-Sync, ersetzt ihn nicht

3. Supabase PostgREST/Realtime/Auth
   - guter hosted Kandidat
   - aber WPF/Offline-Sync bleibt BPM-Code

4. LiteDB/RavenDB/etc.
   - nicht passend, weil BPM strategisch PostgreSQL will

5. CouchDB
   - weiterhin nicht passend, siehe Runde 1
```

## Konkrete Architektur-Konstellation für Herbert

Ich empfehle Herbert folgende Konstellation: **BPM definiert ein eigenes, kleines Sync-Protokoll und baut zuerst den ASP.NET/PostgreSQL-Pfad; CommunityToolkit.Datasync wird als erster Spike geprüft, eigener Minimal-API-Row-Sync ist der direkte Fallback.** Supabase wird nicht als zweite produktive Backendlinie gebaut, sondern nur als kurzer Vergleichsspiegel für Auth/RLS/Realtime und als mögliche Hosted-Variante, falls Herbert später bewusst hosted wählen will. Profile wandern mittelfristig in die DB als `recognition_profiles`; `.bpm/profiles/*.json` bleibt Export/Backup/Interop, nicht Source of Truth im Servermodus. Konflikt-UI wird nicht gebaut; Phase 1 nutzt Server-gewinnt plus Sync-/Audit-Log. DSGVO wird minimal und früh über eine Sync-Whitelist und DataClassification pro DTO vorbereitet, ohne großes Datenschutzframework. Der wichtigste Sofortschritt bleibt ProjectDatabase: keine Hard Deletes, keine Kindlisten-Replace, stabile ULIDs und gezielte Upserts. Damit bleibt BPM local-first, relational, serverfähig und vermeidet Wegwerfarchitektur.

## ✅ Einigkeit

- Kein OneDrive-Event-Sync als automatische Engine.
- Kein CouchDB/PouchDB.NET-Umbau.
- SQLite bleibt lokaler Arbeitsstore; PostgreSQL wird Server-Authority.
- ProjectDatabase muss vor Sync repariert werden: Soft Delete, gezielte Upserts, keine Replace-All-Listen.
- Realtime ist nur Signal, nicht Sync-Engine.
- Server gewinnt reicht für Phase 1.
- `planmanager.db` bleibt local-only.

## ⚠️ Widerspruch

- Ich widerspreche der Idee, Supabase und self-hosted ASP.NET als gleichwertige produktive Backends parallel zu bauen. Das ist für BPM jetzt zu teuer.
- Ich würde Supabase nicht vor CommunityToolkit.Datasync spiken, wenn das strategische Ziel self-hosted ASP.NET/PostgreSQL ist.
- Ich würde nicht mehr von „Microsoft Datasync" als Ziel sprechen, sondern konkret von **CommunityToolkit.Datasync**.
- Ich würde Profile nicht dauerhaft im OneDrive-Dateisystem lassen, sobald Server-Sync aktiv ist. Das wäre ein zweiter Sync-Pfad.
- Ich würde keine Konflikt-UI in Phase 1 bauen. Für Stammdaten ist das Aufwand ohne realen Nutzen.

## ❓ Rückfragen

1. Darf der Server-Sync in Phase 1 eine zusätzliche lokale Sync-State-Tabelle bekommen, oder sollen Pending-Flags direkt in die Fachzeilen? Meine Empfehlung: separate `sync_state`/`sync_pending_changes` lokal, damit das Fachschema sauber bleibt.

2. Soll der erste Server-Sync Profile schon enthalten, oder nur die sechs Stammdatentabellen? Meine Empfehlung: erst Stammdaten, dann `recognition_profiles` als zweiter kleiner Schritt.

3. Soll `project.json` im Servermodus weiterhin automatisch geschrieben werden? Meine Empfehlung: ja, aber nur als Export/Snapshot, niemals als Import-Auto-Sync.

4. Akzeptiert Herbert einen sichtbaren Sync-Status „letzter Serverstand hat lokale Änderung überschrieben" ohne Merge-Dialog? Meine Empfehlung: ja, für Phase 1 verpflichtend so entscheiden.
