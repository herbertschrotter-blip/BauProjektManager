---
doc_id: konzept-server-architektur
doc_type: concept
authority: secondary
status: active
owner: herbert
topics: [server, api, auth, sync, offline-first, postgresql, jwt, rbac, nachkalkulation]
read_when: [server-modus, multi-user, auth, sync, api-design, nachkalkulation]
related_docs: [architektur, db-schema, dsvgo-architektur, konzept-multi-user, konzept-datenarchitektur-sync]
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: Zielarchitektur für Server-Modus (Modus C) — ASP.NET API, Auth, Sync, PostgreSQL
- Autorität: secondary (Ergebnis aus 3-Runden Cross-Review Claude/ChatGPT)
- Lesen wenn: Server-Modus, Multi-User, Auth, Sync, API-Design, Nachkalkulation
- Nicht zuständig für: Sync-Tabellen/Events (→ DatenarchitekturSync.md), Mobile-App (→ BPM-Mobile-Konzept.md)
- Kapitel:
  - 1. Zweck und Zielzustand
  - 2. Datenmodell (geplant)
  - 3. Workflow
  - 4. Technische Umsetzung
  - 5. Abhängigkeiten
  - 6. No-Gos / Einschränkungen
  - 7. Offene Fragen
- Pflichtlesen:
  - Kapitel 1.2 (Sofort gültige Regeln) bei jeder neuen Tabelle/Service
  - Kapitel 4.2 (Auth) bei Login/Rollen-Implementierung
- Fachliche Invarianten:
  - Modus A: SQLite = SoR. Modus C: PostgreSQL = SoR, SQLite = Offline-Cache
  - Client ist IMMER local-first — Reads lokal, Writes lokal, Server nur für Auth + Sync
  - Server gewinnt bei Daten-Konflikten, erzwingt aber auch Fachregeln
  - Single-Tenant: eine Firma = ein Server
  - Kein CQRS, kein Outbox/Inbox — bewährte Bausteine statt Eigenentwicklung

---

# BauProjektManager — Server-Architektur (Modus C)

**Erstellt:** 15.04.2026
**Version:** 1.0
**Status:** Konzept (Ergebnis 3-Runden Cross-Review Claude/ChatGPT)
**Relevante ADRs:** ADR-033 (3 Modi), ADR-020 (Write-Lock), ADR-035 (IExternalCommunicationService), ADR-050 (SoR je Modus), ADR-051 (Local-First)
**Verwandte Dokumente:**
- [MultiUserKonzept.md](MultiUserKonzept.md) — Roadmap/Übergang (Phasen 1–3)
- [DatenarchitekturSync.md](DatenarchitekturSync.md) — Sync-Mechanismus, Outbox/Inbox
- [DSVGO-Architektur.md](../Kern/DSVGO-Architektur.md) — Datenschutz, Rollenmatrix
- [BPM-Mobile-Konzept.md](BPM-Mobile-Konzept.md) — PWA nutzt Server-Modus

---
## 1. Zweck und Zielzustand

### 1.1 Warum Server?

BPM wird langfristig von mehreren Personen gleichzeitig genutzt:
Polier auf der Baustelle, Bauleiter im Büro, Lohnbüro für Zeiterfassung,
Geschäftsführung für Auswertungen. Der Haupttreiber ist das geplante
Nachkalkulationsmodul (Bestellungen, Lieferscheine, Lohnstunden, NU-Rechnungen).

### 1.2 Sofort gültige Regeln

Diese Regeln gelten AB SOFORT für jeden neuen Code — unabhängig davon
ob der Server schon existiert:

**Jede neue fachliche Tabelle bekommt:**

```sql
id                  TEXT PRIMARY KEY,  -- ULID, clientseitig erzeugt
created_at          TEXT NOT NULL,     -- immer UTC
created_by          TEXT,              -- Modus A: aus settings.json, Modus C: aus JWT
last_modified_at    TEXT NOT NULL,     -- immer UTC
last_modified_by    TEXT,
sync_version        INTEGER NOT NULL DEFAULT 0,
is_deleted          INTEGER NOT NULL DEFAULT 0
```

**Weitere Pflichtregeln:**
- Zeitstempel immer UTC (`DateTime.UtcNow`), nie lokale Zeit
- Soft Delete für alle sync-relevanten Tabellen (kein Hard Delete)
- IDs immer clientseitig erzeugen (ULID)
- Writes nur über Application Services, nie direkt aus ViewModels
- Kein direkter HttpClient — nur über IExternalCommunicationService
- `localUserName` in settings.json als Benutzerkontext für Modus A

### 1.3 Betriebsmodi — Source of Truth

| Modus | SoR | SQLite-Rolle | Server-Rolle |
|-------|-----|-------------|-------------|
| A (Solo/Offline) | SQLite | System of Record | nicht vorhanden |
| C (Server) | PostgreSQL | Offline-Cache + Pending Changes | SoR + Auth + Fachregeln |

**Entscheidung:** Im Server-Modus ist der Server die fachliche Autorität.
Lokale SQLite dient als Offline-Cache. "Server gewinnt" bei Daten-Konflikten.
Der Server erzwingt zusätzlich Fachregeln (z.B. keine Änderung freigegebener Buchungen).

### 1.4 Client-Verhalten — Local-First

Der Client arbeitet in JEDEM Modus lokal:

- **Reads:** Immer aus lokaler SQLite
- **Writes:** Immer in lokale SQLite
- **Server-Kontakt nur für:**
  - Login / Token-Refresh
  - Sync (Push lokale Änderungen, Pull Server-Änderungen)
  - Erstsync / Recovery

**Keine gemischten Read-Pfade.** UI liest nie direkt vom Server.
Sync aktualisiert im Hintergrund die lokale SQLite.

### 1.5 Caching

Im Server-Modus werden ALLE Projektdaten lokal gecacht.
Kein selektiver Sync in V1/V2 (Baustelle hat oft kein Internet).

---
## 2. Datenmodell (geplant)

### 2.1 Fachliche Tabellen — Sync-Felder

Siehe Kapitel 1.2. Jede neue Tabelle bekommt die 7 Sync-Spalten.
Bestehende Tabellen werden bei nächster Migration nachgerüstet.

### 2.2 Identity-Tabellen (nur Server)

ASP.NET Identity Standardtabellen (AspNetUsers, AspNetRoles, etc.).
Werden NICHT im Client-SQLite gespiegelt.

Client cached lokal nur:
- Aktueller User-ID + Display Name
- Rollen-/Projektzuordnungen als Snapshot
- Geräte-/Session-Metadaten

### 2.3 Projektzuordnung

```sql
project_memberships
-------------------
id                  TEXT PRIMARY KEY,  -- ULID
project_id          TEXT NOT NULL,     -- FK auf projects
user_id             TEXT NOT NULL,     -- FK auf AspNetUsers.Id (Server)
role                TEXT NOT NULL,     -- Projektrolle
created_at          TEXT NOT NULL,
created_by          TEXT,
last_modified_at    TEXT NOT NULL,
last_modified_by    TEXT,
sync_version        INTEGER NOT NULL DEFAULT 0,
is_deleted          INTEGER NOT NULL DEFAULT 0
```

### 2.4 Audit-Log

```sql
audit_log
---------
id                  TEXT PRIMARY KEY,  -- ULID
entity_type         TEXT NOT NULL,     -- z.B. 'PurchaseOrder', 'TimeEntry'
entity_id           TEXT NOT NULL,
action              TEXT NOT NULL,     -- z.B. 'Created', 'Approved', 'Modified'
changed_by          TEXT NOT NULL,
changed_at          TEXT NOT NULL,     -- UTC
payload_json        TEXT,              -- optionale Details
sync_version        INTEGER NOT NULL DEFAULT 0,
is_deleted          INTEGER NOT NULL DEFAULT 0
```

Pflicht für: Nachkalkulation, Bestellungen, Freigaben, Statuswechsel.

---

## 3. Workflow

### 3.1 Normaler Betrieb (Server-Modus)

```
1. User öffnet App → Login (JWT + Refresh Token)
2. App prüft: Server erreichbar?
   Ja → Pull: Änderungen seit letztem Sync holen → lokale SQLite aktualisieren
   Nein → Offline weiterarbeiten
3. User arbeitet normal (Reads/Writes lokal)
4. Bei Änderung: sync_version++, last_modified_at/by setzen
5. Im Hintergrund oder manuell: Push lokale Änderungen zum Server
6. Server prüft Fachregeln, merged, bestätigt
7. Client aktualisiert sync_version auf Server-Werte
```

### 3.2 Offline-Szenario

- JWT abgelaufen → lokales Arbeiten NICHT blockiert
- Änderungen sammeln sich lokal an
- Sobald online: Re-Auth → Token erneuern → Sync
- Erst nach erfolgreicher Re-Auth wird synchronisiert

### 3.3 Erstsync / Neues Gerät

```
1. Login am Server
2. Vollständiger Pull aller Projektdaten
3. Lokale SQLite wird befüllt
4. Ab dann: normaler Betrieb
```

### 3.4 Konflikte

- **Grundregel:** Server gewinnt bei Daten-Konflikten
- **Erkennung:** sync_version + last_modified_at + last_modified_by
- **Serverseitige Fachregeln:** Server LEHNT ungültige Operationen AB:
  - Freigegebene Buchungen: keine nachträgliche Änderung
  - Bestätigte Zeiteinträge: nur durch Bauleiter/Admin korrigierbar
  - Abgeschlossene Bestellungen: kein Delete

---

## 4. Technische Umsetzung

### 4.1 Server-Stack

Alles bewährte .NET-Bausteine, keine Eigenentwicklung:

| Baustein | Technologie | Zweck |
|----------|------------|-------|
| API | ASP.NET Core Minimal API | Endpoints, Routing, JSON |
| Auth | ASP.NET Identity + JWT | User, Passwort, Rollen, Tokens |
| ORM | Entity Framework Core | DB-Zugriff, Migrations |
| DB | PostgreSQL | Server-Datenbank (gratis, Open Source) |
| Sync | Microsoft.Datasync (First Choice) | Offline-Sync-Bibliothek |
| Hosting | Linux VPS (Hetzner, ~4€/Monat) | Docker oder direkt |

**Sync-Strategie:** Datasync wird per Spike evaluiert (projects + clients,
Login, Push/Pull, Konflikt, Soft Delete). Falls Showstopper: dünner eigener
Row-Sync basierend auf sync_version + last_modified_at. Kein Outbox/Inbox/Event-System.

### 4.2 Auth & User-Management

**User-Verwaltung:**
- Admin legt User an (kein Self-Service / Self-Registration)
- Passwort-Reset durch Admin (temporäres Passwort, bei nächstem Login ändern)
- Kein Mailserver nötig in V1

**Tokens:**
- Access Token: 15–30 Minuten
- Refresh Token: 7–30 Tage, serverseitig widerrufbar, an Gerät gebunden
- Offline: lokales Arbeiten trotz abgelaufenem JWT, Re-Auth vor Sync

**Passwort-Policy:**
- Min. 12 Zeichen
- Keine erzwungene Sonderzeichen-Pflicht
- Lockout bei Fehlversuchen
- MFA optional, nicht V1-blockierend

**Multi-Device:**
- Erlaubt
- Refresh Token pro Gerät
- Serverseitiges Revoke pro Gerät

### 4.3 RBAC — Rollen und Berechtigungen

Zwei Ebenen, nicht feiner:

**Ebene 1 — Systemrolle:**

| Rolle | Rechte |
|-------|--------|
| Admin | User anlegen/deaktivieren, Passwort-Reset, alle Projekte |
| Mitarbeiter | Nur zugewiesene Projekte, gemäß Projektrolle |

**Ebene 2 — Projektrolle:**

| Rolle | Zugriff |
|-------|---------|
| Bauleiter | Alles sehen und bearbeiten, Bestellungen freigeben |
| Polier | Stunden erfassen, Tagesberichte, Fotos, Lieferscheine |
| Einkauf | Bestellungen, Lieferscheine, NU-Rechnungen |
| Lohnbüro | Nur Zeiterfassung und Lohndaten |
| Lesen | Nur Lesezugriff auf Projektdaten |

Nicht auf Bauwerk-/Bauteil-Ebene. Zu teuer für Solo-Entwickler.

### 4.4 Solution-Struktur (Ziel)

```
BauProjektManager.Domain          → nichts
BauProjektManager.Application     → Domain
BauProjektManager.Infrastructure  → Application + Domain
BauProjektManager.Client.Wpf      → Application + Contracts
BauProjektManager.Contracts        → nichts (API-DTOs, Sync-Envelopes)
BauProjektManager.Server           → Application + Contracts + Infrastructure
```

**Contracts-Projekt** enthält:
- API-Request/Response-DTOs
- Sync-Envelopes
- Fehlercodes
- Paging-/Filter-Contracts
- KEINE Domain-Entities

**Dependency-Regel bleibt:** Domain → NICHTS. DTOs nie ins Domain-Projekt.

### 4.5 Admin-Panel (minimal)

Einfache Admin-Seite im Server, kein großes Backoffice:
- User anlegen / deaktivieren
- Passwort zurücksetzen
- Rolle zuweisen
- Projektzuordnung pflegen
- Geräte/Sessions widerrufen

---

## 5. Abhängigkeiten

### 5.1 NuGet-Pakete (Server, geplant)

| Paket | Zweck |
|-------|-------|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | Auth, User, Rollen |
| Microsoft.AspNetCore.Authentication.JwtBearer | JWT-Token-Validierung |
| Npgsql.EntityFrameworkCore.PostgreSQL | EF Core Provider für PostgreSQL |
| Microsoft.Datasync.Server | Offline-Sync (falls Spike erfolgreich) |

### 5.2 Modul-Abhängigkeiten

- **PlanManager** muss VOR Server fertig sein (V1-Kernfeature)
- **Zeiterfassung** liefert Lohnstunden für Nachkalkulation
- **Nachkalkulation** ist der Haupttreiber für Server-Modus
- **Foto-Modul** profitiert von Server-Sync, ist aber nicht blockierend

### 5.3 Infrastruktur

- Linux VPS mit Docker (Hetzner, ~4€/Monat)
- PostgreSQL (im Container oder nativ)
- HTTPS Pflicht (Let's Encrypt)

---

## 6. No-Gos / Einschränkungen

- **Keine Mandantenfähigkeit** — Single-Tenant, eine Firma = ein Server
- **Kein CQRS / MediatR / Command-Handler** — Services mit userId/utcNow reichen
- **Kein Outbox/Inbox/Event-System** — Datasync oder dünner Row-Sync
- **Kein selektiver Sync** in V1/V2 — alles lokal cachen
- **Keine gemischten Read-Pfade** — UI liest nie direkt vom Server
- **Keine Identity-Tabellen im Client** — nur User-Snapshot lokal
- **Kein Self-Service** für User-Registrierung oder Passwort-Reset in V1
- **Keine selbstgestrickte Identity** — ASP.NET Identity nutzen
- **Kein Hard Delete** für sync-relevante Tabellen
- **Keine lokale Zeitzone** in Timestamps — nur UTC

---

## 7. Offene Fragen

### 7.1 Datasync-Spike (nächster Schritt nach PlanManager V1)

Spike-Umfang:
- Tabellen: projects + clients
- Login mit ASP.NET Identity
- Pull all → lokale SQLite
- Lokale Änderung → Push
- Konfliktfall (Server gewinnt)
- Soft Delete Propagation

**Entscheidung nach Spike:**
- Datasync passt → übernehmen, ADR schreiben
- Datasync hat Showstopper → dünner Row-Sync (sync_version-basiert)

### 7.2 Showstopper-Kriterien für Datasync

1. Autorisierung pro Datensatz/Projekt nicht möglich
2. Soft Delete + Vollcache problematisch
3. Fachliche Audit-Einträge schwer mitzuschreiben
4. PostgreSQL + EF Core + Self-Hosted nicht stabil

### 7.3 Offene Entscheidungen

- Exakte Token-Laufzeiten (Access: 15 oder 30 Min? Refresh: 7 oder 30 Tage?)
- Admin-Panel: eigene Seite im Server oder WPF-Dialog?
- Nachkalkulation: formales LV oder nur BPM-Projektstruktur als Kostenzuordnung?
- Sync-Intervall: manuell, bei App-Start, oder periodisch im Hintergrund?

---

## Änderungshistorie

| Datum | Version | Änderung |
|-------|---------|----------|
| 15.04.2026 | 1.0 | Erstversion nach 3-Runden Cross-Review Claude/ChatGPT |
