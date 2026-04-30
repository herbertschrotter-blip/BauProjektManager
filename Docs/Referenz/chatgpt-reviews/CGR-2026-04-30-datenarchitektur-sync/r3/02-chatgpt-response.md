# ChatGPT-Antwort — CGR-2026-04-30-datenarchitektur-sync — Runde 3

**Modell:** GPT-5.4
**Format:** Canvas — "Review Runde 3 — Datenarchitektur & Sync"
**Eingegangen:** 2026-04-30

---

Claude,

Runde 3 konkretisiert die Umsetzung vor ADR-053. Meine Empfehlung bleibt stabil: **kein Doppelbackend bauen, sondern BPM-eigenes Sync-Protokoll + ASP.NET/PostgreSQL als Zielpfad; CommunityToolkit.Datasync 10.0.0 als erster Framework-Spike; eigener Minimal-API-Row-Sync als Fallback.** Hosting-seitig empfehle ich Herbert **EU-VPS als Ziel für Phase 3**, aber **heute noch keinen Serverbetrieb erzwingen**.

## Block A — Server-Hosting + Deployment + Branchen-Praxis am Bau

### A1 — Branchen-Praxis am Bau 2026

Bei kleinen bis mittleren Baufirmen in D/A/CH sehe ich 2026 grob vier Realitäten:

1. **Microsoft-365-/SharePoint-/OneDrive-Welt als De-facto-Datei-Backbone**
   Viele kleine Firmen haben M365, Outlook, Teams, OneDrive/SharePoint. Pläne, Fotos, Protokolle und Excel-Listen liegen dort, auch wenn fachliche Datenstruktur und Berechtigungen oft unsauber sind. Das ist praktisch, weil es jeder bedienen kann.

2. **SaaS-Fachlösungen für Baustelle/Bauleitung**
   PlanRadar, Capmo, docu tools, thinkproject/Kontextwork, Dalux, Fieldwire etc. sind typische Patterns: Cloud, mobile App, Offline-Fähigkeit, Mängel, Fotos, Pläne, Bautagebuch, Aufgaben, Berichte. PlanRadar wirbt z.B. mit >170.000 Nutzern in 75+ Ländern; Capmo positioniert sich explizit für DACH und nennt sehr viele DACH-Projekte. Preise sind meist nutzer-/projekt-/volumenbasiert, oft nicht transparent öffentlich.

3. **Klassische Branchen-/ERP-Software plus Dateiablage**
   Kalkulation/Lohn/Fibu läuft oft in etablierter kaufmännischer Software; Baustellen-Doku separat in SaaS oder Excel/Word/PDF. Voll integrierte Eigenserver-Lösungen sind bei 5–50 MA selten, außer es gibt einen IT-Dienstleister.

4. **On-Prem nur noch gezielt**
   Firmenserver im Büro gibt es weiterhin für Fileserver, Drucker, alte ERP/SQL-Systeme, aber neue Baustellen-Apps gehen eher Richtung SaaS/Cloud/VPS. Grund: Tablets, Poliere unterwegs, externe Partner, kein stabiler VPN-/Serverbetrieb im Kleinbetrieb.

DSGVO-Realität: Formal braucht man AVV/DPA, EU-Hosting/geeignete Garantien, Rollen, Löschkonzept, Backups, Protokolle. Praktisch nutzen kleine Firmen oft M365/Dropbox/WhatsApp/E-Mail/Excel, solange es funktioniert. Für BPM heißt das: **nicht akademisch überbauen, aber keine Architektur wählen, die DSGVO später unmöglich macht.** Also: EU-Hosting, AVV-fähiger Anbieter, Rollen, Audit für Klasse C, keine Service-Secrets im Client.

### A2 — Optionen für Herbert

#### 1. Eigener Firmenserver im Büro

**Pro**

- Daten physisch unter eigener Kontrolle.
- Gut, wenn ohnehin Windows Server, USV, Backup, IT-Betreuer vorhanden sind.
- Niedrige laufende Cloudkosten.
- Lokales LAN schnell.

**Contra**

- Für Baustelle/Surface unterwegs braucht es VPN, DynDNS/feste IP, Firewall, TLS, Monitoring.
- Stromausfall, Internet-Ausfall, Routertausch = Sync tot.
- Backup und Updates müssen aktiv betrieben werden.
- Für Solo-Entwickler unnötige Betriebsverantwortung.

**Benötigt**

- Mini-Server oder vorhandener Server, 16 GB RAM, SSD/NVMe, USV.
- Linux oder Windows Server.
- Docker oder native PostgreSQL + ASP.NET Runtime.
- Reverse Proxy: Caddy oder nginx.
- Offsite-Backup.
- VPN oder öffentliches HTTPS mit Firewall-Härtung.

**Wartung**: 1–3 Stunden/Monat realistisch, plus Störungen.

**Urteil**: Für Herbert erst sinnvoll, wenn eine Firma mit Büro-IT existiert. Nicht als erster Phase-3-Pfad.

#### 2. Heim-Server / NAS

**Pro**

- Günstig, wenn Synology/QNAP ohnehin vorhanden.
- Daten bleiben „bei Herbert".
- Für private Tests gut.

**Contra**

- Geschäftsdaten daheim sind organisatorisch/DSGVO-seitig heikel: physische Sicherheit, Backup, Zugriff, Trennung privat/geschäftlich.
- Consumer-Internet, wechselnde IP, Portfreigaben, Router, Strom.
- NAS-Docker-Stacks sind bei PostgreSQL/ASP.NET möglich, aber weniger sauber als VPS.
- Wenn Herbert im Urlaub/Netzausfall: Server weg.

**Benötigt**

- NAS mit Docker/Container Manager, besser x86 als ARM.
- UPS, externe Backups, VPN/Tailscale.
- TLS/Reverse Proxy.

**Kosten**: Strom 5–15 €/Monat plus Hardware; versteckte Wartung höher als VPS.

**Urteil**: Gut für Labor/Spielwiese, nicht als professioneller Multi-User-Server.

#### 3. Lokaler Hauptrechner als Server

**Pro**

- Minimaler Einstieg.
- Keine externe Infrastruktur.
- Gut für Dev-Demo im LAN.

**Contra**

- Wenn Desktop aus ist, schläft oder Windows Update macht: Sync tot.
- Von Baustelle aus nur mit VPN/Portforwarding erreichbar.
- Keine echte Server-Disziplin: Backups, Monitoring, TLS, Dienstbetrieb.
- Schlechter Fit für 5–10 User.

**Benötigt**

- Windows-Dienst oder Docker Desktop.
- Feste LAN-IP, Firewall-Regeln.
- Optional Tailscale/WireGuard.

**Urteil**: Nur als lokaler Entwicklungsmodus. Nicht als Architekturziel.

#### 4. EU-VPS

**Pro**

- Bester Kompromiss aus Kontrolle, Kosten und Professionalität.
- 24/7 erreichbar für Desktop, Firmenlaptop, Surface, später Poliere.
- EU-Anbieter mit AVV/DPA, Rechenzentrum in DE/EU möglich.
- ASP.NET + PostgreSQL + Caddy/nginx + Docker Compose ist Standardtechnik.
- Migration/Backup kontrollierbar.

**Contra**

- Herbert betreibt Server: Updates, Backup, Security, Monitoring.
- Public Internet Exposure: TLS, Firewall, Fail2ban/Rate Limits, Secrets sauber nötig.
- Keine Managed-DB-Komfortfunktionen, außer separat gebucht.

**Benötigt**

- VPS: 2 vCPU, 4 GB RAM, 40–80 GB SSD für Phase 3 ausreichend.
- Ubuntu LTS oder Debian stable.
- Docker Compose.
- PostgreSQL 16/17, ASP.NET Runtime/Container, Caddy als Reverse Proxy.
- Backups: täglicher `pg_dump` + wöchentliches Volume-Backup + Offsite-Kopie.
- Monitoring: Uptime-Kuma oder Provider-Monitoring, Log-Rotation.

**Kosten 2026**

- Hetzner CAX11: ca. 5,34 €/Monat inkl. 19% MwSt. nach April-2026-Anpassung.
- CAX21: ca. 9,51 €/Monat inkl. 19% MwSt.
- Backups/Snapshots/Storage: realistisch +2–8 €/Monat.
- Summe Phase 3: **8–20 €/Monat**.

**Urteil**: Beste strategische Wahl für BPM Phase 3.

#### 5. Hosted EU-BaaS, z.B. Supabase EU

**Pro**

- Schnell startklar: Postgres, Auth, RLS, Storage, Realtime.
- Weniger Serverwartung.
- EU-Region und AVV-fähiger Anbieter möglich.
- Sehr gut für Spike, Demo, frühe Hosted-Variante.

**Contra**

- BPM müsste Supabase-Spezifika sauber kapseln.
- Offline-Sync bleibt BPM-Logik.
- Kosten ab Pro typischerweise 25 USD/Monat plus Usage; offizielles Billing nennt Free mit 500 MB DB/50k MAU/1 GB Storage, Pro mit 8 GB DB/100k MAU/100 GB Storage plus Overages.
- Debugging von RLS/Auth/Realtime in WPF kann nerven.
- Späterer eigener Server bedeutet Auth-/Policy-Migration.

**Urteil**: Gut als Spike und optional hosted Shortcut, aber nicht als Zielarchitektur für Herbert, wenn self-hosted ASP.NET/Postgres ohnehin gewünscht ist.

### A3 — Konkrete Hosting-Empfehlung

**Jetzt, Solo/2–3 PCs:** Kein Serverzwang. Weiter local-first, OneDrive nur für Projektdateien/Profile-Export/Projektordner, `project.json` als Snapshot. Spike lokal mit Docker/PostgreSQL oder temporärem VPS.

**Später, Phase 3 mit 5–10 Usern:** EU-VPS mit ASP.NET + PostgreSQL. Nicht Heimserver, nicht Desktop-Server, nicht Firmenserver ohne IT-Betrieb.

Konkreter Ziel-Stack:

```text
VPS:
  Hetzner/Netcup/Strato EU, 2 vCPU, 4 GB RAM, 40–80 GB SSD
OS:
  Ubuntu LTS oder Debian stable
Runtime:
  Docker Compose
Services:
  bpm-server ASP.NET container
  postgres container oder native PostgreSQL
  caddy reverse proxy mit Let's Encrypt
  backup job: pg_dump + encrypted offsite copy
  uptime monitor
```

Setup-Schritte später:

```text
1. VPS anlegen, SSH-Key, Firewall: 22 nur admin/IP, 80/443 öffentlich
2. Docker + Compose installieren
3. Caddy/nginx mit TLS einrichten
4. PostgreSQL mit persistentem Volume + Backup-User
5. bpm-server deployen
6. Health endpoint /health
7. Backup: daily pg_dump, weekly restore-test
8. BPM-Client Server-URL + Login konfigurieren
```

Kosten: **0 €/Monat heute**, **8–20 €/Monat Phase 3 VPS**, **25+ USD/Monat Supabase Pro**, wenn hosted BaaS gewählt wird.

## Block B — Konkreter Spike-Plan

### B1 — Spike 0: `ProjectDatabase` syncfähig machen

Ziel: Der lokale Store darf keine Sync-relevanten Identitäten mehr zerstören. Danach ist noch kein Server-Sync vorhanden, aber die DB ist sync-tauglich.

#### Betroffene Methoden in `ProjectDatabase.cs`

**Direkt betroffen:**

```text
SaveProject(Project project)
DeleteProject(string projectId)
SaveClient(Client client, string projectId)
SaveBuildingParts(string projectId, List<BuildingPart> parts)
SaveParticipants(string projectId, List<ProjectParticipant> participants)
SaveLinks(string projectId, List<ProjectLink> links)
LoadAllProjects()
LoadBuildingParts(string projectId)
LoadBuildingLevels(string buildingPartId)
LoadParticipants(string projectId)
LoadLinks(string projectId)
ProjectExistsByPath(string rootPath)
ProjectExists(string projectId)
```

**Soft-Delete-Refactor:**

- `DeleteProject`: kein `DELETE`; stattdessen `is_deleted=1`, `last_modified_at`, `last_modified_by`, `sync_version=sync_version+1` für Projekt und abhängige Shared-Kinder.
- Kind-„Entfernen" in BuildingParts/Participants/Links: Soft Delete je entfernte ID.
- `Load...`: überall `WHERE is_deleted = 0`, außer bei internen Diff-/Existenzabfragen.

**Upsert-Diff statt Replace-All:**

- `SaveBuildingParts` darf nicht mehr alle Levels und Parts löschen.
- `SaveParticipants` darf nicht mehr alle Teilnehmer löschen.
- `SaveLinks` darf nicht mehr alle Links löschen.

#### Neue Helper-Logik

```text
SaveProject(project):
  tx begin
    upsert client
    upsert project
    SyncChildren(building_parts, existingIds, incomingIds)
    SyncChildren(building_levels per part)
    SyncChildren(project_participants)
    SyncChildren(project_links)
  tx commit
```

Kind-Diff:

```text
existing = SELECT id FROM table WHERE parent_id=@pid AND is_deleted=0
incoming = ids from UI model

for item in incoming:
  if item.id empty: item.id = NewUlid()
  if exists including deleted:
    UPDATE ... is_deleted=0, sync_version=sync_version+1 only if values changed
  else:
    INSERT ... sync_version=0

for id in existing - incoming:
  UPDATE table SET is_deleted=1, last_modified_at=@now, last_modified_by=@user, sync_version=sync_version+1
```

Wichtig: „nur wenn Werte geändert" ist nicht Perfektionismus. Sonst erzeugt jedes Speichern künstliche Sync-Konflikte.

#### Neue Spalten/Indexe

Die sechs Tabellen haben bereits Sync-Spalten. Ergänzen würde ich in Spike 0 nur Indexe und optional lokale Sync-State-Tabellen noch nicht voll nutzen.

Indexe:

```sql
CREATE INDEX IF NOT EXISTS idx_clients_is_deleted ON clients(is_deleted);
CREATE INDEX IF NOT EXISTS idx_projects_is_deleted ON projects(is_deleted);
CREATE INDEX IF NOT EXISTS idx_building_parts_project_deleted ON building_parts(project_id, is_deleted);
CREATE INDEX IF NOT EXISTS idx_building_levels_part_deleted ON building_levels(building_part_id, is_deleted);
CREATE INDEX IF NOT EXISTS idx_participants_project_deleted ON project_participants(project_id, is_deleted);
CREATE INDEX IF NOT EXISTS idx_links_project_deleted ON project_links(project_id, is_deleted);
```

Ich würde **noch keine `server_version` Spalte in Spike 0** hinzufügen. Spike 0 ist lokale Semantik. Server-Version kommt mit Spike 1/2, abhängig von Toolkit vs eigener Row-Sync.

#### Manuelle Tests ohne Testprojekt

Test 1 — Kind-ID-Stabilität:

```text
Projekt anlegen mit 2 Participants.
DB prüfen: IDs notieren.
Projekt öffnen, nur Name ändern, speichern.
DB prüfen: Participant-IDs unverändert, sync_version der Participants unverändert.
```

Test 2 — Kind-Update:

```text
Participant Telefonnummer ändern.
DB prüfen: gleiche ID, sync_version +1, last_modified_at aktualisiert.
```

Test 3 — Kind-Delete:

```text
Participant in UI entfernen.
DB prüfen: row existiert weiter, is_deleted=1.
LoadParticipants zeigt ihn nicht mehr.
```

Test 4 — Re-Add/Undo-artig:

```text
Gleichen Participant mit vorhandener ID wieder in Modell aufnehmen.
DB prüfen: is_deleted=0, keine neue ID.
```

Test 5 — Projekt löschen:

```text
DeleteProject ausführen.
DB prüfen: project + children is_deleted=1, keine physische Löschung.
LoadAllProjects zeigt Projekt nicht.
```

Test 6 — Export:

```text
project.json/registry.json erzeugen.
Gelöschte Rows dürfen nicht exportiert werden.
```

#### Erfolgskriterien Spike 0

- Kein Hard Delete in Shared-Tabellen mehr.
- Speichern unveränderter Kindlisten verändert keine Kind-IDs und keine Kind-Versionen.
- Entfernen bedeutet Tombstone, nicht DELETE.
- Alle Load-Methoden filtern `is_deleted=0`.
- `sync_version` steigt nur bei echter Änderung.
- DB kann gelöscht und neu aufgebaut werden; keine Migrationslogik nötig.

#### Stolperfallen

- `ON CONFLICT DO UPDATE` erhöht Version auch bei identischem Payload, wenn nicht geprüft wird.
- Levels hängen an Parts; wenn Part-ID neu erzeugt wird, sind alle Levels „neu".
- UI-Modelle müssen IDs behalten; wenn Mapping IDs verliert, ist Diff unmöglich.
- `ProjectExistsByPath` muss gelöschte Projekte ignorieren oder bewusst Reaktivierung unterstützen.
- Physische FK-Cascades dürfen nicht mehr der normale Delete-Pfad sein.
- `buildings` Legacy-Tabelle: entweder aus Sync-Scope ausschließen oder genauso soft-delete-fähig machen. Ich würde sie aus neuem Sync-Scope ausschließen und mittelfristig löschen lassen, da Frühphase.

Aufwand: **2–3 PT** realistisch.

### B2 — Spike 1: CommunityToolkit.Datasync gegen ASP.NET + PostgreSQL

#### Library/Version

Konkreter Kandidat:

```text
CommunityToolkit.Datasync.Client 10.0.0
CommunityToolkit.Datasync.Server 10.0.0
CommunityToolkit.Datasync.Server.Abstractions 10.0.0
CommunityToolkit.Datasync.Server.OpenApi oder Swashbuckle 10.0.0 optional
```

Stand: NuGet 10.0.0 veröffentlicht am 16.02.2026, Ziel `net10.0` bei Serverpaketen, Client `10.0.0`, CommunityToolkit/.NET-Foundation-Umfeld. GitHub beschreibt Datasync als Client-Server-System für Datenbanktabellen-Synchronisation, getestet u.a. mit WPF. Trotzdem: Downloads sind moderat, also Spike-Pflicht.

PostgreSQL-Anbindung muss im Spike verifiziert werden. Datasync hat Server-Abstraktionen/Repository-Muster; falls kein direkter „PostgreSQL-Paketname" existiert, wird EF Core/Npgsql oder eigenes Repository genutzt.

#### Eine Tabelle als Spike-Kandidat

**Empfehlung: `clients`, nicht `projects`.**

Begründung:

- Weniger Spalten, keine Kindlisten, keine Pfade, keine Bauteile.
- Enthält personenbezogene Klasse-B-Felder: guter DSGVO-/Whitelist-Test.
- `projects` hat FK auf `clients`; für ersten Sync ist `clients` isolierter.
- Konfliktfall „Telefonnummer geändert" ist leicht testbar.

Nicht `building_parts`, weil Parent-FK und Kinddiff zuerst ablenken. Nicht `projects`, weil zu breit.

#### Auth-Stub

Kein echtes RBAC im Spike. Trotzdem nicht anonym arbeiten.

```text
User: spike-admin
UserId: fixed ULID or GUID
JWT: dev signing key local
Claims:
  sub = userId
  name = "Spike Admin"
  role = "admin"
```

ASP.NET:

```text
AddAuthentication(JwtBearer)
AddAuthorization(policy: sync_user requires authenticated)
```

Für den ersten Spike reicht ein Dev-Token-Endpoint:

```text
POST /dev/login -> JWT
```

Nicht in Produktion übernehmen.

#### Konflikt-Test

Setup:

```text
Server: PostgreSQL mit clients row C1, server_version 1
Client A: lokale SQLite Kopie C1 base_version 1
Client B: lokale SQLite Kopie C1 base_version 1
```

Ablauf:

```text
Client A offline: phone = "111"; pending
Client B online: phone = "222"; push accepted -> server_version 2
Client A online: push base_version 1
Erwartung: Server gewinnt, A bekommt reject/overwrite mit server row phone="222", server_version=2
```

Datasync-spezifisch prüfen:

- Gibt es native Konfliktmeldung?
- Kann BPM „server wins" deterministisch erzwingen?
- Kann lokale SQLite-Zeile nach Serverstand überschrieben werden?
- Wie wird Soft Delete behandelt?

#### Erfolgskriterien: Toolkit übernehmen ja/nein

**GO:**

- WPF-Client kann offline ändern und später pushen.
- ASP.NET + PostgreSQL funktioniert ohne Azure-Zwang.
- Soft Delete funktioniert sauber.
- Konflikt „Server gewinnt" ist deterministisch implementierbar.
- Lokale bestehende SQLite-Struktur muss nicht radikal ersetzt werden.
- Pro Tabelle bleibt Code überschaubar.
- Logging/Debugging ist verständlich.

**NO-GO:**

- Toolkit erzwingt Feldnamen/Modelle, die BPM stark verbiegen.
- Konfliktbehandlung ist undurchsichtig.
- Lokaler Offline-Store kollidiert mit bestehendem `bpm.db`.
- PostgreSQL-Pfad braucht zu viel Custom-Repository-Code, sodass eigener Row-Sync einfacher wäre.
- Dokumentation lässt kritische WPF/PostgreSQL-Fragen offen.

#### Diagnose-Tools

```text
Server logs: Serilog console + file, request id, user id, table, row id
PostgreSQL: pgAdmin oder DBeaver
SQLite: DB Browser for SQLite
HTTP: Postman oder Bruno
Traffic: Fiddler/HTTP logging optional
Datasync: Client logs auf Verbose
Testdaten: zwei lokale AppData-Ordner simulieren Client A/B
```

#### Startpunkte/Tutorials

- CommunityToolkit/Datasync GitHub Repo und Docs.
- NuGet README der Pakete 10.0.0.
- Offizielle Samples zu Server/Client/Offline-Store, sofern 10.x-kompatibel.
- ASP.NET Core JWT Bearer Microsoft Learn für Auth-Stub.
- Npgsql EF Core Provider Docs für PostgreSQL.

#### PT-Aufwand

- Minimaler HTTP/Server/DB-Setup: **0,5 PT**.
- Datasync Server + Client + eine Tabelle: **1 PT**.
- Konflikt/Offline/Soft Delete Test: **0,5–1 PT**.

Realistisch: **2 PT**. Wenn PostgreSQL-Repository hakelt: **3 PT**, dann hart entscheiden.

### B3 — Spike 2 + Spike 3 kurz

#### Spike 2 — eigener Minimal-API Row-Sync

Anders als Spike 1: keine Framework-Magie, BPM kontrolliert Protokoll.

Mini-Schritte:

```text
1. Server-Tabelle clients mit server_version BIGINT
2. GET /sync/clients?afterVersion=0
3. POST /sync/clients/push mit rows + base_server_version
4. Server accepted/rejected response
5. Client schreibt sync_state + checkpoint
6. Zwei-Client-Konflikttest identisch wie Spike 1
```

Server-Version:

```text
clients.server_version BIGINT NOT NULL
server_change_log(version BIGSERIAL, table_name, entity_id, operation, changed_at)
```

Entscheidung: Wenn dieser Spike in 2–3 PT transparent läuft und Datasync mehr verbiegt als hilft, **eigener Row-Sync gewinnt**.

#### Spike 3 — Supabase 1-Tages-Spike

Mini-Schritte:

```text
1. Supabase EU-Projekt erstellen
2. clients-Tabelle mit RLS
3. Supabase Auth User
4. C# Login + Pull clients
5. C# Push Update
6. Realtime-Subscription: Event nur als Pull-Signal
7. Export/Dump prüfen
```

Prüfen:

- C#-Client reif genug für WPF?
- RLS-Fehler debugbar?
- Kein Service-Key im Client?
- Latenz/Offline-Reconnect okay?
- Migration zu eigenem Postgres plausibel?

## Block C — Auth-Provider-Wahl + RBAC

### C1 — Auth-Provider-Vergleich

#### Supabase Auth

**Pro**

- Schnell startklar.
- JWT, Email/Password, Magic Link, OAuth.
- Eng mit Postgres/RLS integriert.
- Free/Pro-Limits für kleine Teams sehr großzügig.
- EU-Hosting möglich.

**Contra**

- Bindet Auth stark an Supabase-Projekt.
- Migration zu ASP.NET Identity später nicht 1:1, insbesondere Passwörter.
- WPF-Client muss Flow sauber abbilden.
- Für self-hosted ASP.NET-Ziel nicht die natürlichste Wahl.

**Urteil**: Gut für Supabase-Spike, nicht Ziel für self-hosted BPM.

#### ASP.NET Core Identity

**Pro**

- Passt direkt zu ASP.NET/PostgreSQL-Ziel.
- Volle Kontrolle über User, Rollen, Refresh Tokens, Geräte.
- Keine externen Auth-Kosten.
- Gut integrierbar mit JWT Bearer für WPF-API.
- Admin legt User an: passt zu kleiner Baufirma.

**Contra**

- Herbert betreibt Passwort-Reset, Lockout, Token-Refresh, Security selbst.
- Mehr Code als Supabase/Auth0.
- UI/Admin-Panel nötig.

**Urteil**: Beste Wahl für BPM Phase 3.

#### Auth0

**Pro**

- Sehr ausgereift, gute Hosted-Login-Flows, MFA, Social/OIDC.
- Schnell und sicher für professionelle Produkte.

**Contra**

- Externer Identity-Provider zusätzlich zum eigenen Server.
- Kosten/MAU/Features können unübersichtlich werden.
- Für 5–10 Bau-User überdimensioniert.
- Organisations-/B2B-Features können kostenrelevant sein.

**Urteil**: Zu viel für Herbert.

#### Microsoft Entra ID

**Pro**

- Ideal, wenn Firma ohnehin M365/Entra nutzt.
- Firmenaccounts, MFA, Conditional Access, Geräteverwaltung.
- Für größere Bauunternehmen gut.

**Contra**

- Viele kleine Baufirmen haben zwar M365, aber keine sauber administrierte Entra-Disziplin.
- Gast-/Polier-/Subunternehmer-Accounts können organisatorisch mühsam werden.
- Für Solo-/kleines Team zu schwergewichtig.

**Urteil**: Später als optionaler Enterprise-Login denkbar, nicht V1.

#### Eigenbau Username+Password+JWT ohne Identity

**Pro**

- Wenig Code am Anfang.
- Volle Kontrolle.

**Contra**

- Passwort-Hashing, Lockout, Reset, Refresh Token, Revocation, MFA, Security-Bugs.
- Man baut nach, was ASP.NET Identity schon kann.
- Kein guter Grund.

**Urteil**: Nicht machen.

### Auth-Empfehlung

**ASP.NET Core Identity + JWT Bearer + Refresh Tokens pro Gerät.**

Rollout:

```text
Spike 1:
  Dev-JWT-Stub, kein echtes Identity

Phase 3 Alpha:
  ASP.NET Identity, Admin legt User an, Passwort initial setzen

Phase 3 Beta:
  Refresh Tokens pro Gerät, Device-Revoke, Lockout

Später optional:
  Entra ID / OIDC als zusätzlicher Login für Firmenkunden
```

### C2 — RBAC-Konzept

Rollen für BPM:

```text
admin
  alles, User, Rollen, Projekte, Serververwaltung

bauleiter
  Projekt-Stammdaten, Pläne, Bautagebuch, Teilnehmer, Aufgaben, Freigaben
  keine Lohndaten/Personalakten standardmäßig

polier
  zugewiesene Projekte lesen
  Bautagebuch, Fotos, Planstatus, Tagesmeldungen schreiben
  keine Kosten/Lohn/Preise

disponent
  Personal-/Material-/Terminplanung
  eingeschränkter Projektzugriff
  keine Preis-/Geschäftsführungsdaten standardmäßig

lohnbuero
  Zeiterfassung, Lohn-relevante Daten, Personal
  Projekt nur soweit für Stunden/Kostenstelle nötig

gast
  read-only auf zugewiesene Projekte/Pläne/Dokumente
```

Datenmodell:

```sql
users -> ASP.NET Identity Tabellen
project_memberships (
  id TEXT PRIMARY KEY,
  project_id TEXT NOT NULL,
  user_id TEXT NOT NULL,
  project_role TEXT NOT NULL, -- admin/bauleiter/polier/gast
  is_deleted INTEGER NOT NULL DEFAULT 0
)

system_roles über Identity Roles:
  admin, employee
```

Keine zu feingranulare Bauteil-/Gewerk-RBAC in V1. Projekt + Modul reicht.

#### Postgres RLS-Abbildung

RLS-Prinzip:

```sql
-- User darf Projektzeilen sehen, wenn Membership existiert oder admin ist
EXISTS (
  SELECT 1 FROM project_memberships pm
  WHERE pm.project_id = row.project_id
    AND pm.user_id = current_setting('app.user_id')
    AND pm.is_deleted = 0
)
```

Für Tabellen ohne `project_id`, z.B. `clients`, Zugriff über Projektbezug oder separate Company-Scope-Regel. Im ersten Servermodell kann Server-API filtern; RLS später härten.

Wichtig: Bei ASP.NET + EF Core ist RLS optional als zweite Schutzschicht. Die primäre Autorisierung liegt zunächst in ASP.NET Policies/Services, weil sie leichter zu debuggen ist.

#### ASP.NET Authorization Policies

```text
Policy CanReadProject(projectId): admin or membership exists
Policy CanEditProject(projectId): admin or role in [bauleiter]
Policy CanWriteSiteLog(projectId): admin or role in [bauleiter, polier]
Policy CanViewWageData: admin or lohnbuero
Policy CanManageUsers: admin
```

Rollout-Reihenfolge:

```text
1. admin
2. bauleiter
3. gast/read-only
4. polier
5. lohnbuero/disponent erst wenn Module existieren
```

Für den aktuellen Sync reicht: `admin` und `member`/`bauleiter`.

### C3 — Login-Pflicht ab wann?

**Phase 1 lokal: kein Login.** Weiter `IUserContext`/LocalUserContext. Herbert soll nicht täglich Login spielen, solange kein Server existiert.

**Server-Modus: Login Pflicht.** Sobald Push/Pull gegen Server aktiv ist, braucht es Auth. Lokale Offline-Arbeit nach abgelaufenem Token bleibt erlaubt; Sync erst nach Re-Auth.

Nachrüsten ist sauber möglich, weil ADR-052/IUserContext bereits die richtige Nahtstelle bietet:

```text
Modus A:
  IUserContext = LocalUserContext aus settings/Windows

Modus C:
  IUserContext = AuthenticatedUserContext aus JWT Claims
```

Nicht machen: Login in Phase 1 erzwingen, nur um später vorbereitet zu sein. Das wäre UX-Kosten ohne Nutzen.

## Block D — ADR-053-Struktur + Sync-Tabellen

### D1 — ADR-053: was rein, was offen?

Dein Vorschlag stimmt. Ich würde ADR-053 verbindlich, aber nicht library-spezifisch schreiben.

#### Verbindlich rein

```text
Titel:
ADR-053: Server-Sync-Zielarchitektur für BPM Phase 3

Entscheidung:
1. BPM bleibt local-first: Reads/Writes lokal in SQLite.
2. Server-Modus: PostgreSQL ist Server-Authority.
3. Client spricht nur BPM-eigenes Sync-Protokoll über IBpmSyncClient.
4. Backend-Adapter sind austauschbar, DI in Infrastructure, nie ViewModels.
5. Produktiver Zielpfad: ASP.NET + PostgreSQL; Toolkit vs eigener Row-Sync entscheidet Spike.
6. Kein OneDrive-Event-Sync, kein CouchDB/PouchDB.NET.
7. Konfliktstrategie Phase 1: Server gewinnt, keine Merge-UI.
8. Lokale Sync-State/Pending-Tabellen getrennt von Fachzeilen.
9. Profile-Ziel: recognition_profiles in DB; .bpm/profiles Export/Backup.
10. project.json bleibt Snapshot/Export, nie Auto-Sync-Quelle.
11. DSGVO: DataClassification pro DTO + Sync-Whitelist; Klasse C erst mit Rollen/Audit.
12. Spike-Reihenfolge: 0 ProjectDatabase, 1 CommunityToolkit.Datasync, 2 Minimal API, 3 Supabase Vergleich.
13. Frühphase: keine Migrationen, DB/JSON löschen und neu anlegen lassen.
```

#### Offen lassen

```text
- CommunityToolkit.Datasync vs eigener Minimal-API als finale Implementierung
- Exaktes Hosting: VPS konkret erst nach Phase-3-Start
- Supabase nur als optionaler Hosted-Spike, nicht final entschieden
- Vollständiges RBAC für spätere Module
- Konflikt-UI für zukünftige Module
- Profile-Library B3
- Storage/Fotos/Pläne-Serverstrategie
```

#### Ich würde zusätzlich fixieren

- **Erster Sync-Scope:** `clients`, `projects`, `building_parts`, `building_levels`, `project_participants`, `project_links`; danach `recognition_profiles`.
- **Nicht im Sync-Scope:** `planmanager.db`, Logs, Undo, Import-Journal, lokale Caches.
- **Server-Version statt Uhrzeit für Konfliktentscheidung.** Das sollte verbindlich rein.

### D2 — Was passiert mit `DatenarchitekturSync.md`?

Nach ADR-053 sollte `DatenarchitekturSync.md` nicht komplett gelöscht werden, aber sein Status muss härter werden:

```text
status: historical / partially superseded
superseded_by: [ADR-053, ServerArchitektur.md]
```

Was gültig bleibt:

- 4-Klassen-Datenmodell.
- SQLite als lokale Wahrheit in Modus A.
- Local-first-Grundsatz.
- Shared vs Restricted physisch trennen.
- Konfliktarme Aggregate.

Was superseded wird:

- Outbox/Inbox als verpflichtendes Design.
- OneDrive/Event-Dateien als automatische Sync-Engine.
- Snapshot/Event-Retention als Phase-2-Kern.
- 12-Spalten-Modell, soweit durch 7-Spalten-Konvention ersetzt.
- FolderSync als Roadmap-Schritt.

ADR-053 sollte `DatenarchitekturSync.md` **nicht vollständig fachlich superseden**, sondern nur den Sync-Mechanismus. Besserer Satz:

```text
ADR-053 supersedes the FolderSync/Event-Outbox implementation path of DatenarchitekturSync.md. The data classification and local-first principles remain valid unless contradicted by ADR-053 or ServerArchitektur.md.
```

### D3 — Sync-Tabelle-Struktur konkret

Dein Modell ist gut, aber ich würde es leicht ändern.

Kernproblem: `sync_state` pro Entität ist gut für Status, aber für Push braucht man auch den lokalen Payload/Operation oder eine Möglichkeit, Änderungen aus der Fachzeile zuverlässig zu rekonstruieren. Da BPM Phase 1 server-gewinnt und Full-Row-Upserts nutzt, kann `sync_state` reichen, wenn Push immer aktuelle Fachzeile liest. Für Deletes reicht das nur, wenn Tombstone in Fachzeile bleibt. Das ist okay.

#### Empfehlung: drei lokale Tabellen

```sql
CREATE TABLE sync_state_local (
  entity_table TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  state TEXT NOT NULL,              -- pending | synced | rejected | conflict
  operation TEXT NOT NULL,          -- upsert | delete
  base_server_version INTEGER,      -- Version, die lokal zuletzt bekannt war
  last_local_change_at TEXT NOT NULL,
  last_sync_attempt_at TEXT,
  retry_count INTEGER NOT NULL DEFAULT 0,
  error_code TEXT,
  error_message TEXT,
  PRIMARY KEY (entity_table, entity_id)
);

CREATE INDEX idx_sync_state_pending
ON sync_state_local(state, last_local_change_at);
```

Änderung gegenüber deinem Vorschlag: `operation` ist Pflicht. Sonst muss Push raten, ob pending eine Änderung oder ein Delete ist. `error_code` zusätzlich zu `error_message`, weil UI/Retry nicht auf Text parsen darf.

```sql
CREATE TABLE sync_checkpoints (
  scope TEXT NOT NULL,               -- global | project:<id>
  entity_table TEXT NOT NULL,
  highest_server_version INTEGER NOT NULL DEFAULT 0,
  last_pull_at TEXT,
  last_successful_push_at TEXT,
  PRIMARY KEY (scope, entity_table)
);
```

Änderung: `scope` ergänzen. Langfristig braucht ihr pro Projekt/Scope Checkpoints. Sonst wird Pull bei vielen Projekten unnötig global oder Sicherheitsmodell schwierig.

```sql
CREATE TABLE sync_history (
  id TEXT PRIMARY KEY,
  entity_table TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  operation TEXT NOT NULL,           -- pull | push | reject | conflict-server-won | mark-pending
  server_version INTEGER,
  performed_at TEXT NOT NULL,
  device_id TEXT NOT NULL,
  user_id TEXT NOT NULL,
  details_json TEXT
);

CREATE INDEX idx_sync_history_entity
ON sync_history(entity_table, entity_id);

CREATE INDEX idx_sync_history_time
ON sync_history(performed_at);
```

Änderung: `details_json` ergänzen. Für Diagnose reichen kleine strukturierte Details: alter/neuer Status, Fehlercode, Serverantwort. Kein vollständiger personenbezogener Payload ins History-Log schreiben.

#### Was fehlt serverseitig?

Lokal reicht das nicht. Server braucht eine monotone Version:

```sql
-- pro synchronisierter Server-Tabelle
server_version BIGINT NOT NULL;
server_modified_at TEXT NOT NULL;
server_modified_by TEXT NOT NULL;
```

Oder zentrale Change-Tabelle:

```sql
server_change_log (
  server_version BIGSERIAL PRIMARY KEY,
  entity_table TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  operation TEXT NOT NULL,       -- upsert | delete
  project_id TEXT,
  changed_at TEXT NOT NULL,
  changed_by TEXT NOT NULL
);
```

Für eigenen Row-Sync ist `server_change_log` sehr hilfreich. Für Datasync kann das Toolkit eigene Mechanik haben; ADR-053 sollte daher nur „serverseitig monotone Server-Version/Checkpoint" festschreiben, nicht die genaue Tabelle.

#### Was ist zu viel?

- `sync_history` kann schnell wachsen. Für Phase 1: 30 Tage oder letzte 1.000 Einträge behalten.
- `state='synced'` in `sync_state_local` für jede Entität ist optional. Ich würde `sync_state_local` nur für `pending`, `rejected`, `conflict` halten und `synced` nach Erfolg löschen. Der Serverstand steht über Checkpoint und Fachzeile. Das hält die Tabelle klein.

Besser:

```text
sync_state_local enthält nur Abweichungen vom Normalzustand.
Keine Zeile = synced/clean.
```

Dann lautet `state`:

```text
pending | rejected | conflict
```

Wenn du UI „alles synchron" brauchst, nimm Checkpoint + keine Pending-Zeilen.

#### In welchem Spike nötig?

**Spike 0:**

- Noch nicht zwingend.
- Optional nur DDL vorbereiten, aber nicht produktiv nutzen.
- Fokus auf Soft Delete/Upsert.

**Spike 1:**

- `sync_state_local` und `sync_checkpoints` nötig.
- `sync_history` minimal oder nur Logging-Datei, wenn Datasync selbst Status führt.

**Spike 2:**

- Alle drei Tabellen nötig, weil eigener Row-Sync ohne Framework-Diagnose sonst blind ist.

#### UI-Kommunikation für `conflict/rejected`

Keine Merge-UI. Nur Sync-Status-Anzeige:

```text
Sync-Status: 1 lokale Änderung wurde vom Serverstand überschrieben.
Details anzeigen:
  Tabelle: clients
  Datensatz: Max Mustermann GmbH
  Grund: Server hatte neuere Version
  Server geändert von: Herbert / Desktop / 2026-04-30 14:32 UTC
  Lokale Änderung verworfen am: 2026-04-30 15:10 UTC
```

Interne Zustände:

```text
pending:
  wird beim nächsten Sync gepusht

rejected:
  Server hat Änderung abgelehnt, App hat Serverstand angewendet
  nach Anzeige/History kann state gelöscht werden

conflict:
  nur wenn Serverstand nicht automatisch angewendet werden konnte
  Phase 1 möglichst vermeiden; server-wins sollte in rejected enden
```

Ich würde in Phase 1 `conflict` nur für technische Ausnahmefälle verwenden, nicht für normale Datenkonflikte. Normale Datenkonflikte sind `rejected/server-won`.

## Abschluss-Empfehlung

**Hosting:** Heute kein produktiver Server; für Phase 3 EU-VPS mit Docker, ASP.NET, PostgreSQL, Caddy und täglichem Backup. Kein Heimserver, kein Desktop-Server, kein Firmenserver ohne IT-Betrieb. Kosten später realistisch 8–20 €/Monat.

**Spike 0:** 2–3 PT in `ProjectDatabase.cs`: Hard Deletes raus, Load-Filter `is_deleted=0`, gezielte Upsert-Diff-Logik für BuildingParts/Levels/Participants/Links, stabile Kind-ULIDs, Version nur bei echter Änderung. Danach ist BPM lokal syncfähig, auch ohne Server.

**Spike 1:** 2 PT CommunityToolkit.Datasync 10.0.0 mit ASP.NET + PostgreSQL + WPF gegen eine Tabelle `clients`, Dev-JWT, zwei lokale SQLite-Clients, Soft Delete und Server-gewinnt-Konflikt. Danach harte GO/NO-GO-Entscheidung.

**Auth:** ASP.NET Core Identity + JWT + Refresh Tokens pro Gerät für Phase 3. Kein Login in lokalem Phase-1-Modus; Servermodus erzwingt Login. Supabase Auth/Auth0/Entra nur optional spätere Adapter, nicht V1.

**ADR-053:** Verbindlich die Zielarchitektur, Sync-Protokoll, Spike-Reihenfolge, Server-gewinnt, DTO-Whitelist, Profile-in-DB-Ziel, lokale Sync-State-Tabellen, kein OneDrive-Event-Sync. Offen bleiben konkrete Sync-Library, finaler Hosting-Anbieter, vollständiges RBAC und spätere Konflikt-UI.

## ✅ Einigkeit

- ASP.NET + PostgreSQL ist strategischer Zielpfad.
- BPM braucht `IBpmSyncClient` und eigene Contracts, nicht Supabase-/Toolkit-Leakage in ViewModels.
- `planmanager.db` bleibt local-only.
- Profile wandern mittelfristig in DB, `.bpm/profiles` bleibt Export/Backup.
- Server gewinnt in Phase 1; keine Merge-UI.
- DataClassification + Sync-Whitelist werden früh vorbereitet.
- `ProjectDatabase.cs` ist der erste technische Hebel.

## ⚠️ Widerspruch

- Ich würde **nicht** jetzt Supabase als ersten Spike setzen. Erst CommunityToolkit.Datasync, dann eigener Minimal-API-Benchmark, dann Supabase als Hosted-Vergleich.
- Ich würde **keinen Heimserver/NAS** als Phase-3-Ziel empfehlen. Für Geschäftsdaten und 5–10 User ist EU-VPS sauberer.
- Ich würde `sync_state` nicht dauerhaft für jede synchronisierte Zeile mit `state='synced'` befüllen. Keine Zeile sollte „clean/synced" bedeuten.
- Ich würde `clients` statt `projects` als ersten Datasync-Spike nehmen. Weniger Spalten, keine FK-Kaskade, leichterer Konflikttest.
- Ich würde RBAC V1 nicht mit allen Rollen starten. Erst `admin`, `bauleiter/member`, `gast`; Polier/Lohnbüro erst mit Modulen.

## ❓ Rückfragen

1. Soll Spike 0 auch die Legacy-Tabelle `buildings` endgültig aus dem aktiven Modell entfernen? Meine Empfehlung: ja, wenn im Code nicht mehr fachlich nötig; Frühphase nutzen.

2. Soll der spätere Server alle Projekte global versionieren oder pro Projekt eigene Checkpoints führen? Meine Empfehlung: pro Projekt/Scope, weil es besser zu Membership/RLS passt.

3. Soll `sync_history` in SQLite wirklich persistiert werden oder reicht für Phase 1 Serilog + letzte Fehler in `sync_state_local`? Meine Empfehlung: minimale Tabelle, aber mit Retention.

4. Soll im lokalen Modus bereits ein `device_id` eingeführt werden? Meine Empfehlung: ja, einfache stabile ULID in `device-settings.json`, weil Sync-History sonst später lückenhaft ist.
