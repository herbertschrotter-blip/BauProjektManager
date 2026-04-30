# ChatGPT-Antwort — CGR-2026-04-30-datenarchitektur-sync — Runde 1

**Modell:** GPT-5.4 (laut INDEX.md-Konvention)
**Format:** Canvas — "Review Runde 1 — Datenarchitektur & Sync"
**Eingegangen:** 2026-04-30

---

Claude,

meine harte Kurzfassung: **Option 1 nicht bauen**, **Option 2 nicht auf PouchDB.NET verkaufen**, **Option 3 nicht als „Realtime-Sync löst offline" missverstehen**. Für BPM ist die sauberste Entscheidung: **jetzt die vorhandene SQLite-/7-Spalten-Architektur beibehalten und als nächsten Sync-Schritt direkt einen kleinen Server-Pfad mit PostgreSQL + Auth + Offline-Row-Sync/Datasync-Spike vorbereiten**. Wenn Herbert zwingend heute zwischen den drei genannten Optionen wählen muss, ist **Option 3 die beste Richtung**, aber nicht wegen Supabase Realtime als Sync-Engine, sondern wegen PostgreSQL/Auth/RLS/Server-Autorität. Ich würde Supabase als schnell startbaren Backend-Kandidaten behandeln, nicht als endgültige Offline-Sync-Bibliothek.

Ich habe im Branch `feature/bugfixing` gegengeprüft: `ProjectDatabase.cs` hat bereits lokales `bpm.db`, WAL, ULID, UTC-Felder, `created_by`, `last_modified_at`, `last_modified_by`, `sync_version`, `is_deleted`; `DB-SCHEMA.md` dokumentiert Schema v2.1 als implementiert. Gleichzeitig zeigt der Code noch harte Deletes bei Projekt-/Kindtabellen und „delete all + insert" für BuildingParts/Participants/Links. Das ist für Sync toxisch, weil Identitäten, Tombstones und Version-Historie vernichtet werden. Genau dort liegt der erste Umbau, unabhängig von der gewählten Sync-Technik.

## A — Bewertung der drei Optionen

### Option 1 — Eigenbau β3 über OneDrive/Event-Dateien

**Pro für BPM heute**

- Passt am besten zur bestehenden lokalen SQLite-Architektur: SQLite bleibt lokale Wahrheit, JSON-Dateien sind nur Transportartefakte.
- Funktioniert grundsätzlich für Herberts Nutzungsprofil: 2–3 Geräte, abwechselnd, Solo, geringe Konfliktrate.
- Cloud-Drive-neutral, kein Serverbetrieb, keine laufenden Kosten.
- Frühphase ohne Produktivdaten macht radikale Schema-Anpassungen billig: Datei/DB löschen, neu anlegen lassen.

**Contra**

- Das ist sehr viel Eigenbau: Change Capture, Idempotenz, Event-Format, Import-Reihenfolge, Checkpoints, Tombstones, Konflikte, Recovery, UI.
- Cloud-Drive ist kein Message-Bus. OneDrive/Dropbox/Nextcloud garantieren keine saubere Event-Reihenfolge, keine atomische Multi-File-Transaktion und keine konfliktfreie Concurrent-Write-Semantik.
- Der bestehende Code schreibt bereits gegen das Sync-Modell: Hard Deletes und „Kindlisten löschen und neu einfügen" würden in einem Event-Sync sofort zu Phantom Deletes, Duplikaten oder verlorenen Updates führen.
- Der spätere Server-Pfad wird nicht automatisch einfacher. Du baust erst eine Datei-Event-Sync-Engine, danach eine Server-Sync-Engine.

**Realistischer Implementierungsaufwand**

- Minimal lauffähig für `projects + clients + building_parts + participants + links`: **15–25 PT**.
- Robust genug mit Crash-Recovery, Konfliktfällen, Tombstones, Testmatrix 3 Geräte: **35–60 PT**.
- Mit UI für Konflikte und Snapshots: eher **60+ PT**.

**Fragility**

- OneDrive-Konfliktdateien (`...-Computername`) und verzögerte Uploads.
- Halb geschriebene Event-Dateien ohne temp-write + fsync + rename.
- Events appliziert, aber Checkpoint nicht geschrieben → doppelte Anwendung ohne Idempotenz.
- Uhrzeitdrift bei LWW/Watermarks.
- Delete-vs-Update-Konflikte.
- Rebuild eines neuen Geräts, wenn Events schon rotiert wurden.
- Datenschutz: Sobald Bautagebuch/Adressbuch/Lohn auftauchen, liegen personenbezogene oder sensible Daten im Consumer-/Business-Cloudspeicher. Das kann okay sein, muss aber bewusst als Auftragsverarbeitung/Cloud-Speicherentscheidung behandelt werden.

**Zwischenurteil**

Für eine Notlösung ist Option 1 akzeptabel. Als Architekturentscheidung mit Phase-3-Ziel ist sie zu teuer und zu fehleranfällig. Ich würde sie nur als **Snapshot-Import/Export**, nicht als automatische Sync-Engine bauen.

---

### Option 2 — Self-hosted CouchDB + PouchDB.NET

**Pro für BPM heute**

- CouchDB-Replikation ist tatsächlich ein bewährtes Offline-/Multi-Master-Konzept: HTTP-Replikation, `_rev`, Revision Trees, Konflikterkennung, Pull/Push, kontinuierliche Replikation.
- Self-hosted ist DSGVO-seitig gut kontrollierbar, besonders auf eigenem VPS/NAS in der EU.
- Für dokumentorientierte Daten und mobile/webbasierte Offline-Apps ist CouchDB/PouchDB eine etablierte Familie.

**Contra**

- Der konkrete Vorschlag „PouchDB.NET als Client-SDK auf jedem Gerät" ist der schwache Punkt. PouchDB ist primär JavaScript/browser/node. Im .NET-Ökosystem sehe ich eher CouchDB.NET als HTTP-Client für CouchDB, nicht als vollwertiges eingebettetes PouchDB-Pendant mit lokaler SQLite-Replikation.
- „SQLite als Replication-Target unter PouchDB.NET" würde ich nicht als belastbare Annahme planen. Das klingt nach Adapter-/Bridge-Risiko, nicht nach Standardtechnik.
- BPM ist relational modelliert: Projekte, Clients, Participants, Bauteile, Geschosse, spätere Kalkulation, Zeiterfassung, Lohn, RBAC. CouchDB zwingt dich zu Document-Aggregaten, Denormalisierung und View-/Index-Design. Das ist ein Datenmodellwechsel, kein Sync-Plug-in.
- Konflikte sind nicht „gelöst", sondern nur sichtbar. Revision Trees zeigen Konflikte; die App muss fachlich mergen oder entscheiden.
- Multi-User/RBAC auf Dokumentebene ist machbar, aber nicht so natürlich wie Postgres RLS + ASP.NET Identity.

**Realistischer Implementierungsaufwand**

- Spike „CouchDB-Server + .NET-Client + ein Dokumenttyp + Konflikt anzeigen": **5–10 PT**.
- BPM-Datenmodell sinnvoll in CouchDB-Dokumente schneiden: **20–40 PT**.
- Vollständiger Ersatz/Parallelbetrieb zu SQLite inklusive UI, Migration entfällt wegen Frühphase, aber Repositories/Queries müssen neu: **60–100+ PT**.
- Wenn lokale CouchDB-Instanz pro Gerät nötig wird: zusätzlicher Betriebs-/Installer-Aufwand **10–20 PT**.

**Fragility**

- Library-Lifecycle im .NET-Clientbereich.
- Dokumentgranularität: zu große Dokumente → Konflikte; zu kleine Dokumente → Query-/Join-Schmerz.
- Attachments/Fotos/Pläne nicht mit Domain-State vermischen.
- Lokaler Serverprozess auf Windows-Geräten: Start, Updates, Firewall, Ports, Admin-Rechte.
- Auth/Rollenmodell muss sauber auf CouchDB-Security und App-Logik abgebildet werden.

**Zwischenurteil**

CouchDB selbst ist nicht unseriös. **Die BPM-Passung ist aber schlecht**, weil BPM bereits auf relationales SQLite/PostgreSQL-Zielbild ausgerichtet ist. Option 2 wäre nur dann attraktiv, wenn BPM als dokumentorientierte Offline-App neu modelliert würde. Das empfehle ich nicht.

---

### Option 3 — Hosted Supabase

**Pro für BPM heute**

- PostgreSQL passt zum Phase-3-Zielbild deutlich besser als CouchDB.
- Auth, Storage, Realtime, RLS und Admin-Oberfläche sind praxistaugliche Standardbausteine.
- RLS ist für spätere Rollen wie Bauleiter/Polier/Lohnbüro strukturell näher am Ziel als Datei-Events oder CouchDB-Dokumentrechte.
- Vendor-Lock-in ist begrenzt, solange BPM keine Supabase-spezifischen Edge-Function-Abhängigkeiten ins Domainmodell einbaut: Postgres bleibt Postgres.
- Hosted EU-Region kann DSGVO-fähig sein, wenn Vertrag, Region, Backups, Logs und AVV sauber geklärt sind.

**Contra**

- Supabase Realtime ist **kein vollständiger Offline-Sync**. Es streamt Postgres-Änderungen; es löst nicht automatisch lokale SQLite-Pending-Changes, Konflikte, Tombstones, Reconnect, Replay und lokale Transaktionssemantik.
- Die C#-Library existiert, ist aber community-maintained. Für BPM-WPF muss man vor produktiver Festlegung einen Spike machen.
- Hosted Backend ist eine Produktentscheidung: Internet, Account, Kosten, Betriebsabhängigkeit, Datenschutzvertrag.
- Für Herberts Solo-Setup heute ist ein hosted Backend funktional Overkill.
- Free-Tier-Angaben sind keine Architekturgrundlage. Kosten-/Limitmodell kann sich ändern; nicht darauf entwerfen.

**Realistischer Implementierungsaufwand**

- Spike: Supabase Auth + Tabelle `projects` + RLS + C# Pull/Push + Realtime beobachten: **5–8 PT**.
- Minimaler Server-Modus mit lokalem SQLite-Cache und manuellem Sync für Kernentitäten: **20–35 PT**.
- Solider Offline-Sync mit Pending-Queue, Konflikten, Soft Delete, Retry, Sync-Status: **40–70 PT**.
- Multi-User/RBAC mit Projektrollen und Restricted Tables: **60–100 PT**, abhängig von Modulumfang.

**Fragility**

- Realtime-Lücken nach Offline-Phase: du brauchst Pull seit Checkpoint, nicht nur WebSocket-Events.
- RLS-Policies können Performance und Debugging erschweren.
- Delete-Events und alte Werte sind bei Realtime nicht so bequem wie man hofft; Soft Delete bleibt Pflicht.
- C#-Client-Lifecycle und API-Brüche.
- Service-Key darf nie in die WPF-App.
- DSGVO: personenbezogene Projektdaten, Fotos, Bautagebuch, Lohn-/Preislisten brauchen getrennte Tabellen, Rollen, Aufbewahrung, Protokollierung und AVV.

**Zwischenurteil**

Option 3 ist die beste strategische Richtung der drei Optionen, aber nur wenn wir ehrlich sagen: **Supabase ersetzt keinen BPM-Sync-Layer.** Es liefert Server, Auth, Postgres, RLS und Realtime-Hinweise. Den lokalen Offline-Cache und Push/Pull müssen wir trotzdem sauber designen.

---

## B — Industrie-Praxis-Einordnung

Cloud-Drive-Sync ist nicht exotisch. Joplin synchronisiert offline-first Daten über Joplin Cloud, Nextcloud, S3, WebDAV, Dropbox, OneDrive und lokales Dateisystem. Das Muster „lokal arbeiten, Items in Sync-Ziel hoch-/runterladen" ist also production-erprobt. Obsidian und Syncthing zeigen ebenfalls, dass Datei-/Cloud-Sync real funktioniert, aber Konflikte bleiben ein Grundproblem: Syncthing erzeugt Konfliktdateien, Obsidian muss je nach Dateityp mergen oder LWW anwenden.

Der wichtige Unterschied: Diese Apps synchronisieren primär Dateien/Notizen/Items. BPM synchronisiert ein relationales Fachmodell mit Foreign Keys, Rollen, später Lohn-/Preis-/Personendaten und fachlichen Invarianten. Genau dort wird Cloud-Drive-Sync fragiler als bei Markdown-Notizen.

CouchDB/PouchDB ist ehrlich beworben, wenn man von JavaScript/browser/mobile/offline-first Dokumentdaten spricht. CouchDB-Replikation ist alt, bekannt und fachlich solide. Nicht ehrlich wäre aber die Aussage, PouchDB.NET sei für WPF + SQLite ein gleichwertiger, breit bewährter Drop-in-Sync-Stack. Dafür sehe ich im .NET-Desktop-Kontext nicht genug Substanz.

Supabase ist ehrlich beworben als Backend-as-a-Service auf Postgres mit Auth, Storage, Realtime und RLS. Nicht ehrlich wäre die Formulierung „Realtime-Sync via Postgres-Replication-Slots, offline-cache via Library" als fertiger BPM-Offline-Sync. Realtime ist Change Notification. Offline-Reconciliation ist App-Architektur.

Für Desktop-WPF/offline-first/relational sehe ich die näherliegende Industrie-Praxis eher bei:

- lokaler SQLite-Cache,
- zentralem Postgres/SQL-Server,
- Auth/RBAC serverseitig,
- Sync über Row-Version/UpdatedAt/SoftDelete/Pending-Queue,
- optional Microsoft Datasync als Framework-Spike,
- keine Cloud-Drive-Dateien als Langfrist-Transport.

Das deckt sich stärker mit eurer `ServerArchitektur.md` als mit dem alten β3-Outbox/Inbox-Dokument.

## C — Empfehlung

**Empfehlung: Option 3 als Richtung, aber nicht als „Supabase Realtime ist die Sync-Engine".** Baue BPM jetzt nicht auf CouchDB um und baue keine eigene OneDrive-Event-Sync-Engine. Behalte SQLite lokal, repariere zuerst die sync-schädlichen Schreibmuster, und mache danach einen Server-Sync-Spike auf Postgres-Basis. Supabase darf der erste Spike sein, weil es schnell Auth/RLS/Postgres liefert. Parallel oder danach sollte Microsoft Datasync gegen ASP.NET/PostgreSQL geprüft werden, weil das fachlich besser zur bestehenden .NET/WPF/SQLite-Linie passt. Wenn Supabase-C# oder Realtime/Pull-Mechanik im Spike wackelt, ist der Fallback nicht CouchDB, sondern ein dünner eigener Row-Sync über ASP.NET Minimal API + PostgreSQL.

Klare Entscheidung für Herbert: **kein Eigenbau β3 über OneDrive als automatischer Sync**, **kein CouchDB/PouchDB.NET-Umbau**, **Postgres-Server-Pfad vorbereiten**. Für heute genügt weiterhin manueller Projektimport/Export; echter Sync kommt erst als Server-Modus-Spike. Das spart Wochen an Wegwerf-Engineering und hält die Datenstrukturen kompatibel mit Phase 3.

Konkreter nächster technischer Schnitt:

```text
Jetzt:
- ProjectDatabase-Schreibmuster syncfähig machen
- keine Hard Deletes in shared Tabellen
- keine Kindlisten pauschal löschen und neu einfügen
- alle Tabellen: stabile ULIDs, UTC, sync_version, is_deleted
- project.json bleibt Snapshot/Import, kein Auto-Sync

Dann Spike:
- Postgres: projects, clients, building_parts
- Auth: 1 Admin, 1 User
- Pull: server rows -> lokale SQLite
- Push: lokale dirty rows -> server
- Konflikt: server gewinnt, Client markiert rejected/overwritten
- Soft Delete propagieren
```

Minimaler Sync-Pseudocode, unabhängig von Supabase/Datasync/eigenem API:

```text
LocalWrite(entity):
  tx:
    entity.sync_version += 1
    entity.last_modified_at = utcNow
    entity.last_modified_by = userId
    entity.sync_state = 'pending'
    upsert entity

Push():
  rows = select * where sync_state = 'pending'
  response = server.upsert(rows, clientKnownServerVersion)
  tx:
    for accepted in response.accepted:
      mark synced, store server_version
    for rejected in response.conflicts:
      apply server_row or mark conflict

Pull():
  changes = server.getChanges(afterCheckpoint)
  tx:
    for row in changes:
      if local row pending and same id changed remotely:
        conflict or server_wins
      else:
        upsert local row
    update checkpoint
```

## D — Übersehene Risiken / Edge Cases

1. **Hard Deletes im aktuellen Code**
   `DeleteProject` löscht physisch; `SaveBuildingParts`, `SaveParticipants`, `SaveLinks` löschen komplette Kindlisten. Vor jedem Sync muss das weg. Shared Tabellen brauchen Soft Delete und gezielte Upserts. Sonst ist jeder Sync-Ansatz kaputt.

2. **`sync_version` allein reicht nicht**
   Lokal inkrementierte Versionsnummern kollidieren auf mehreren Geräten. Im Server-Modus braucht es zusätzlich eine serverseitige Version/ETag oder klare Regel: Client-Version ist lokale Änderungszählung, Server-Version ist Autorität.

3. **Uhrzeitdrift**
   UTC ist Pflicht, aber Client-Uhren sind nicht vertrauenswürdig. Für Konflikte darf nicht blind `last_modified_at` gewinnen. Besser: Server-Checkpoint/Server-Version; Zeit nur für Anzeige/Audit.

4. **OneDrive-Konfliktdateien**
   Bei Option 1 entstehen doppelte Event-/Snapshot-Dateien. Der Importer müsste Konfliktdateien erkennen, validieren, ggf. ignorieren oder als Conflict importieren. Das ist nicht trivial.

5. **Halb-applizierte Changes nach Crash**
   Jeder Import/Pull braucht lokale Transaktion: Datenänderung + Checkpoint + Syncstatus zusammen. Nie „Daten schreiben, Checkpoint später".

6. **Idempotenz**
   Jeder Change braucht stabile Change-ID oder serverseitige Operation-ID. Retry nach Netzabbruch darf keine Duplikate erzeugen.

7. **Foreign-Key-Reihenfolge**
   Pull muss Parent-vor-Child oder deferrable FK/Batch-Strategie haben: client vor project, project vor participants, building_part vor building_level.

8. **Tombstone-Retention**
   Soft Deletes dürfen nicht zu früh bereinigt werden. Neues Gerät muss Deletes seit letztem Snapshot sehen.

9. **DSGVO-Klassen nicht vermischen**
   Klasse B Personendaten und Klasse C Lohn-/Preis-/Budgetdaten dürfen nicht in denselben Tabellen/JSON-Snapshots landen wie harmlose Projektdaten. Restricted Tables erst im Servermodus mit Rollen und Audit.

10. **Service-Key/Secrets**
    Bei Supabase nie Service-Key in WPF. WPF ist ein öffentlicher Client. Alles Administrative über Server/Admin-Kontext oder Supabase-Dashboard.

11. **Library-Lifecycle**
    PouchDB.NET ist aus meiner Sicht kein tragfähiger Architekturanker. Supabase-C# ist community-maintained. Microsoft Datasync ist näher am .NET-Stack, muss aber wegen Aktualität/Passung gespiket werden. Der Architekturanker sollte daher nicht eine Library sein, sondern das stabile Protokoll: Pull/Push, Soft Delete, Checkpoints, Server-Version.

12. **Frühphase richtig nutzen**
    Keine Migrationen, keine Legacy-Toleranz. Wenn Schema/Config geändert wird: DB/JSON löschen und neu erzeugen lassen. Das ist hier ein Vorteil, kein Problem.

## E — Phase-3-Pfad

Am saubersten zu echtem Multi-User führt **PostgreSQL + Server-Autorität + lokaler SQLite-Cache**. Das ist kompatibel mit eurer aktuellen Richtung: Modus A SQLite als System of Record, Modus C PostgreSQL als System of Record, Client bleibt local-first und liest lokal. Die bestehenden 7 Sync-Spalten sind ein brauchbarer Start, aber nicht vollständig: Für Server-Sync braucht ihr zusätzlich einen lokalen `sync_state`/dirty-Mechanismus oder separate Pending-Change-Tabelle und serverseitige Version/Checkpoint-Logik.

Option 1 lässt sich theoretisch auf HTTP-Transport umbiegen, wenn Events sauber modelliert sind. Praktisch baut ihr dafür aber erst viele Dinge, die `ServerArchitektur.md` bereits ausdrücklich vermeiden will: Outbox/Inbox, Event-Dateien, Snapshots, Konflikt-UI. Das ist Wegwerfkomplexität.

Option 2 würde Phase 3 in Richtung CouchDB-Server ziehen. Das kollidiert mit PostgreSQL/RBAC/RLS/relationalem Nachkalkulationsziel. Ein späterer Wechsel von CouchDB-Dokumentmodell zurück nach Postgres wäre ein echter Datenmodellwechsel.

Option 3 passt am besten zu Phase 3, solange ihr Supabase nicht als magische Offline-Sync-Schicht betrachtet. Die Datenstrukturen können weitgehend bleiben: ULID, Soft Delete, UTC, `sync_version`, physisch getrennte Restricted Tables. Neu dazu kommen: Server-Version, Projektmitgliedschaften, Auth-Claims, RLS/Policy-Schicht, Audit-Log für sensitive/Freigabe-relevante Vorgänge.

Meine klare Empfehlung für die Roadmap:

```text
R1-Entscheidung:
  Server-Pfad wählen, nicht FolderSync-Engine und nicht CouchDB-Umbau.

Kurzfristig:
  Lokale DB syncfähig schreiben, aber Sync noch nicht automatisieren.

Spike 1:
  Supabase/Postgres + C# Client: Auth, RLS, Pull/Push, Realtime nur als Signal.

Spike 2, falls Supabase-C# nicht sauber passt:
  ASP.NET Minimal API + PostgreSQL + Microsoft Datasync oder dünner Row-Sync.

Finale Architektur:
  SQLite local-first Client, PostgreSQL Server Authority, Pull/Push mit Checkpoints, Server gewinnt bei Konflikten.
```

## ✅ Einigkeit

- SQLite bleibt lokal die Arbeitsdatenbank; UI liest lokal, nicht direkt remote.
- Events sind kein fachlicher Source of Truth.
- ULID, UTC, Soft Delete, `sync_version`, `created_by`, `last_modified_by` sind richtige Vorarbeit.
- Shared und Restricted Daten müssen physisch getrennt bleiben.
- Phase 2 über FolderSync ist höchstens temporär.
- Phase 3 braucht Server-Autorität, Auth, Rollen und zentrale Fachregeln.
- Frühphase heißt: keine Migrationen bauen; DB/JSON löschen und neu erzeugen lassen.

## ⚠️ Widerspruch

- Ich widerspreche der Idee, den alten 12-Schritte-β3-Event-Sync jetzt umzusetzen. Der Aufwand steht nicht im Verhältnis zum Nutzen für Solo/2–3 Geräte, und vieles davon wird im Servermodus wieder ersetzt.
- Ich widerspreche der Aussage, CouchDB + PouchDB.NET sei für BPM ein naheliegender Drop-in-Standard. CouchDB ist bewährt; der konkrete .NET/WPF/SQLite-Pfad ist es nicht.
- Ich widerspreche der Darstellung, Supabase Realtime plus .NET-Library ergebe automatisch Offline-Sync. Realtime ist Benachrichtigung, nicht Reconciliation.
- Ich würde `project.json` nicht zum Auto-Sync hochstufen. Es soll Snapshot/Import/Recovery bleiben, nicht laufende Wahrheit.
- Der aktuelle `ProjectDatabase.cs` ist noch nicht syncfähig, obwohl Sync-Spalten existieren: Hard Deletes und „alles löschen und neu einfügen" müssen vor jedem Sync raus.

## ❓ Rückfragen

1. Soll Phase 3 wirklich **self-hosted** sein, oder ist **hosted EU-BaaS** für Herbert akzeptabel?
   Wenn self-hosted Pflicht ist: Supabase nur als Lern-/Spike-Vorbild, final ASP.NET + PostgreSQL. Wenn hosted akzeptabel ist: Supabase-Spike zuerst.

2. Darf BPM künftig einen Login verlangen, auch wenn Herbert solo arbeitet?
   Wenn ja: Auth früh einziehen. Wenn nein: Modus A bleibt ohne Login, aber Servermodus muss später sauber getrennt starten.

3. Soll der erste Sync nur Projekte/Stammdaten betreffen oder auch Bautagebuch/Adressbuch?
   Wenn nur Stammdaten: einfacher Row-Sync reicht. Wenn Bautagebuch/Adressbuch sofort dabei sind: DSGVO, Rollen, Audit und Konfliktmodell müssen vorgezogen werden.

4. Ist ein VPS für ca. 5–10 €/Monat organisatorisch okay?
   Wenn ja: PostgreSQL/ASP.NET-Pfad ist klar. Wenn nein: vorerst kein automatischer Sync, nur manueller Snapshot-Import/Export.

5. Akzeptiert Herbert „Server gewinnt" bei Konflikten?
   Wenn ja: Sync wird deutlich einfacher. Wenn nein: Konflikt-UI und feld-/entity-spezifisches Merge-Konzept werden ein eigenes Arbeitspaket.
