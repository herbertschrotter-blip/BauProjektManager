# CGR-2026-04-30-datenarchitektur-sync — Datenarchitektur & Sync-Strategie

**Thema:** Sync-Mechanismus für BPM Phase 0/1 + Roadmap zu späterem Verkauf
**Zeitraum:** 2026-04-30 (1 Tag, 7 Runden)
**Ursprungs-Chat:** Session BPM-009 Tief-Audit + Profil-Sharing-Diskussion
**Status:** ✅ **Abgeschlossen**

---

## Ausgangslage

User Herbert Schrotter arbeitet als Solo-Entwickler an BPM (BauProjektManager). Bei der Diskussion um Profil-Bibliothek wurde klar: BPM hat heute keinen automatischen Sync zwischen Geräten. Die CGR-Serie sollte klären: Welche Sync-Architektur ist die richtige für BPM heute und in der Zukunft?

User-Anforderung wörtlich: *"ich will nichts neu erfinden. ich will gängige geprüfte funktionierende technik wenn möglich"*.

## Verlauf der 7 Runden — drei fundamentale Pivots

### Runde 1 — Drei Optionen zur Diskussion

ChatGPT bewertet:
- Option 1: Eigenbau β3 (OneDrive-JSON-Events nach DatenarchitekturSync.md)
- Option 2: Self-hosted CouchDB + PouchDB.NET
- Option 3: Hosted Supabase (Postgres + Auth + RLS)

**Empfehlung:** Option 3 als Richtung, aber nicht "Realtime = Sync". Server-Pfad mit ASP.NET + PostgreSQL.

[r1/](./r1/)

### Runde 2 — Hybrid-Hosting + Profile-Sync + Spike-Plan

- **Hybrid-Hosting:** Nicht zwei Backends, sondern Protokoll-Hybrid via `IBpmSyncClient` mit austauschbaren Adaptern
- **Profile:** B2 (in DB) statt B1 (JSON) oder B3 (Library + Instance)
- **Spike-Reihenfolge:** Spike 0 (DB syncfähig) → Spike 1 (CommunityToolkit.Datasync) → Spike 2 (Eigener Minimal-API) → Spike 3 (Supabase)
- **Korrektur:** CommunityToolkit.Datasync 10.0.0 statt veraltetes Microsoft.Datasync

[r2/](./r2/)

### Runde 3 — Hosting-Detail + Auth + RBAC + ADR-053-Struktur

- **Hosting-Empfehlung:** EU-VPS für Phase 3 (Hetzner CAX11 Linux)
- **Auth:** ASP.NET Core Identity + JWT
- **Rollen:** admin/bauleiter/polier/disponent/lohnbüro/gast (V1: nur admin/bauleiter/gast)
- **Branchen-Praxis:** PlanRadar/Capmo/Dalux dominieren DACH-Bau-SaaS
- **ADR-053-Struktur:** 13 verbindliche Punkte, 7 offene für Spike-Ergebnisse

[r3/](./r3/)

### Runde 4 — Kostenrealität: VPS günstiger als 0€-Optionen

User-Erkenntnis: *"sollte ich einen server mieten? das wäre fast günstiger oder?"*

3-Jahres-TCO (Hetzner CAX11): ~318€ Linux-VPS vs ~680-900€ Synology vs ~400-950€ Desktop-24/7 Strom.
**VPS ist die günstigste Option** wenn man Strom + Anschaffung ehrlich rechnet.

[r4/](./r4/)

### Runde 5 — Linux vs Windows VPS

User-Frage: *"gibt es den hetzner server auch mit windows os? ich kenne mich bei linux gar nicht aus"*

ChatGPT empfiehlt **Hetzner CAX11 + Ubuntu 24.04 LTS + Docker Compose + Caddy**. Linux-Lernkurve mit Runbook beherrschbar (~6 tägliche Befehle), Windows-VPS 300-550€ teurer über 3 Jahre.

**Aber: User-Pivot in Stufe-A:**
> *"warum versteifen wir uns eigentlich so auf linux. [...] da läuft doch überall windows? warum soll ich was für linux programmieren wenn ich es dann später sowieso für windows brauche?!"*

→ **PIVOT 1: Geschäftsmodell-Frage.** User wählt Modell B (Kunde installiert auf eigenem Server, On-Premise) statt Modell A (Herbert hostet zentral SaaS).

[r5/](./r5/)

### Runde 6 — On-Premise-Architektur

ChatGPT pivotiert komplett: Windows-Server beim Kunden + native Windows-Services (PostgreSQL + ASP.NET) + Inno Setup Installer + signierte Lizenzdatei + ASP.NET Identity.

**Aber: User-Pivot in Stufe-A:**
> *"Bevor wir um den heißen brei herumreden. zuerst mal will ich es nur selbst und in meiner firma einsetzen bis es fertig programmiert ist!! das dauert sicher noch 2 jahre. Linux werde ich sicher niemals freiwillig verwenden. nicht mal zum entwickeln oder testen!! also das nimm komplett raus."*

→ **PIVOT 2: Linux komplett raus + Phase 0/1 = 2 Jahre eigene Firma.** Verkauf erst nach 2 Jahren.

[r6/](./r6/)

### Runde 7 — Windows-only Multi-User Live-Sync

User klargestellt: *"live sync ist unbedingt gewünscht. während der entwicklungs/testphase werde ich auch die bauleiter in der firma miteinbeziehen. also muss ich syncen jederzeit. nicht nur am abend!!"*

→ **PIVOT 3: 5-10 User Multi-User Live-Sync ab Phase 0/1.** Hauptrechner als Server raus, 24/7-Server-Verfügbarkeit nötig.

ChatGPT empfiehlt: CG-NAT-Test, dann Mini-PC oder Windows-VPS. Caddy nur für HTTPS/öffentlich.

**User-Antworten in Stufe-A:**
- CG-NAT-Test in Firma nicht möglich (kein Router-Zugriff)
- Vorhandener Firmen-Server unklar zugänglich
- 5-6 User in ersten 6 Monaten
- Nur Windows-Laptops/Surface, Mobile später

→ **Finale Hosting-Entscheidung: Windows-VPS für Phase 0/1.**

[r7/](./r7/)

---

## Finales Architektur-Ergebnis

### Code-Stack (gilt für Phase 0/1 + Phase Verkauf)

✅ **ASP.NET Core 10** als Server-Framework (plattformneutral)
✅ **PostgreSQL 17** als DB (auf Windows-Server installiert)
✅ **WPF-Client** (Windows-Desktop, topologieneutral mit konfigurierbarer ServerUrl)
✅ **`IBpmSyncClient`** als BPM-eigenes Sync-Protokoll mit austauschbaren Adaptern
✅ **Pull/Push-Sync** mit `server_version`, Server-gewinnt
✅ **ASP.NET Identity + JWT + Refresh Tokens** ab Phase 0.5 (Multi-User)
✅ **DataClassification + Whitelist** pro Sync-DTO
✅ **device_id** in `device-settings.json` + separater `IDeviceContext`
✅ **Single-Tenant** pro Installation (keine Multi-Tenant-RLS)

### Hosting Phase 0/1 (Solo + eigene Firma, 2 Jahre, 5-10 User)

✅ **Windows-VPS** (Strato VC 2-8 oder vergleichbar, ~12€/Monat)
✅ **Windows Server 2025** als OS
✅ **PostgreSQL 17 als Windows-Service**
✅ **ASP.NET Core 10 als Windows-Service** (`UseWindowsService()`)
✅ **Caddy for Windows** für HTTPS via Let's Encrypt
✅ **Domain** (z.B. `bpm.deinefirma.at`) mit DNS A-Record
✅ **Backup:** PowerShell pg_dump als Scheduled Task + Provider-Snapshots
✅ **Connectivity:** Direkte HTTPS-URL, kein VPN/Tailscale pro User nötig

### Hosting Phase Verkauf (24+ Monate, On-Premise bei Kunden)

✅ **Windows Server beim Kunden** (Windows 11 Pro für Kleinst, Windows Server 2022/2025 ab 10 User)
✅ **Inno Setup Installer** für Server + Client
✅ **Signierte Lizenzdatei** (Ed25519/RSA, offline-fähig, kein harter Stopp bei Wartungsablauf)
✅ **AD/LDAP-Integration optional**
✅ **Auto-Update** für WPF-Client (Velopack) + manueller Server-Update durch Admin
✅ **Same Code-Stack** wie Phase 0/1 — keine Architektur-Umarbeitung nötig

### Was bewusst NICHT in Phase 0/1 gebaut wird

❌ Kein Installer (manuelles Setup auf VPS reicht)
❌ Kein Lizenz-System (Herbert ist sein eigener Lizenznehmer)
❌ Keine AD-Integration (ASP.NET Identity reicht)
❌ Kein Auto-Update (manuelles Deployment)
❌ Keine Multi-Tenant-Architektur (Single-Tenant pro Installation)
❌ Kein Code-Signing-Zertifikat (interne Nutzung)
❌ Kein Cloudflare Tunnel, kein Tailscale (öffentliche IP via VPS)
❌ Keine Mobile-App (BPM-Mobile bleibt post-v1-Thema)

### Was die Code-Architektur jetzt sicherstellen muss

🔒 **BPM-Client topologieneutral** — funktioniert mit `http://server:5000`, `https://bpm.firma.at`, beliebige Server-URLs
🔒 **`IBpmSyncClient` als BPM-eigenes Interface** — keine Lock-in zu Supabase/Datasync/Library X
🔒 **Plattformneutraler ASP.NET-Code** — keine Windows-Pfade, kein PowerShell-Call, keine COM
🔒 **Konfiguration externalisiert** — appsettings.json + Environment Variables
🔒 **DataClassification von Anfang an** — pro DTO, auch wenn DSGVO-Whitelist später kommt

## Kernergebnisse / Was kommt in ADR-053

15 verbindliche Punkte (siehe [r7/04-user-decisions.md](./r7/04-user-decisions.md)):

1. Windows-only für Entwicklung, Test, Produktion
2. Phase 0/1: Windows-VPS in EU (Strato VC 2-8 oder vergleichbar)
3. Stack: PostgreSQL 17 Windows-Service + ASP.NET Core 10 Worker Service + Caddy for Windows
4. Domain + HTTPS via Let's Encrypt
5. ASP.NET Identity + JWT + Refresh Tokens
6. Rollen Phase 0/1: admin, bauleiter, polier, gast
7. BPM-Client: ServerUrl konfigurierbar, HTTP/HTTPS, topologieneutral
8. Sync: Pull/Push, server_version, Server-gewinnt
9. Spike 0: ProjectDatabase syncfähig (Soft Delete + Upserts)
10. DataClassification + Whitelist pro DTO
11. device_id in device-settings.json + IDeviceContext
12. Single-Tenant pro Installation (keine Multi-Tenant/RLS)
13. recognition_profiles in DB (post Spike 0)
14. Frühphase: keine Migration, DB-Reset bei Schema-Änderungen
15. Phase Verkauf später: Inno Setup, Lizenz, AD-Integration optional

## Resultierende Folge-Tasks (Tracker)

- **Spike 0:** ProjectDatabase syncfähig (Soft Delete + gezielte Upserts) — bereits im Backlog
- **Server-Skelett:** ASP.NET Core 10 Worker Service mit `UseWindowsService()`
- **VPS-Setup:** Strato Windows VC 2-8 + PostgreSQL 17 + Domain
- **Auth:** ASP.NET Identity + JWT + Refresh Tokens
- **Sync-Endpoints:** Pull/Push mit server_version
- **Recognition Profiles:** in DB statt JSON-Datei (post Spike 0)

## Verwandte Dokumente

- [DatenarchitekturSync.md](../../../Konzepte/DatenarchitekturSync.md) — historisches Konzept, FolderSync-Pfad superseded durch ADR-053
- [ServerArchitektur.md](../../../Konzepte/ServerArchitektur.md) — Phase-3-Zielbild (bleibt relevant für Modell A wenn jemals zentral hosted)
- [ADR.md](../../ADR.md) — ADR-046 (.bpm/), ADR-047 (4-Klassen), ADR-050 (DB-Schema v2.1), ADR-051 (Local-First), ADR-052 (IUserContext), **ADR-053 (Server-Sync-Architektur — neu)**

## Verworfene Optionen / Gelernte Lektionen

- ❌ **OneDrive als Sync-Bus** (Eigenbau β3) — Wegwerf-Engineering
- ❌ **CouchDB + PouchDB.NET** — Datenmodell-Wechsel von relational zu dokumentorientiert
- ❌ **Supabase Free als Produktiv-Stack** — Vendor-Lock-in, US-Hosting
- ❌ **Linux + Docker** — User-Vorgabe (will keine Linux-Erfahrung haben)
- ❌ **Synology DS124/DS224+ kaufen für BPM** — wirtschaftlich schlechter als VPS
- ❌ **Hauptrechner als 24/7-Server** — nicht für Multi-User-Produktion
- ❌ **Tailscale als Standard für 7+ User** — wird teurer als VPS
- ❌ **Cloudflare Tunnel** — DSGVO + Auth-Komplexität für WPF-Client
- ❌ **Multi-Tenant-RLS** — Single-Tenant On-Premise reicht
- ❌ **AD-Integration in V1** — nice-to-have, nicht must-have

**Wichtigste Lektion:** Geschäftsmodell-Frage (Modell A/B/C) muss VOR Hosting-Entscheidung geklärt werden. R1-R5 haben stillschweigend Modell A angenommen — User-Pivot in R5/R6 hat fundamental neu kalibriert.

## Status: Abgeschlossen

Alle 7 Runden inhaltlich vollständig. Architektur-Entscheidung steht. ADR-053 wird im nächsten Schritt geschrieben. Tracker-Tasks werden im nächsten Schritt angelegt. Spike 0 kann beginnen.
