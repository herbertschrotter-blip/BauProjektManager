# Folgeprompt — CGR-2026-04-30-datenarchitektur-sync — Runde 7

## ⚠️ HARTE USER-VORGABEN (gelten ab JETZT, kein Diskussionsraum)

User Herbert hat in r6+r7-Stufe-A klargestellt:

### 1. Linux komplett raus

> *"Linux werde ich sicher niemals freiwillig verwenden. nicht mal zum entwickeln oder testen!! also das nimm komplett raus und erkläre das auch chatgpt."*

→ Kein Linux. Nicht als Spike. Nicht als Test. Nicht als Hosting. Nicht als "optional". Wenn du Linux trotzdem erwähnst, ignoriert der User den Teil.

### 2. Windows-Stack für ALLES — Entwicklung, Test, Produktion

PostgreSQL für Windows (EDB Installer), ASP.NET Core 10 als Windows Worker Service, optional Caddy for Windows.

### 3. Phase 0+1 = nächste 2 Jahre = Solo + Herberts eigene Firma

Kein Verkauf in dieser Zeit. Funktional fertig programmieren, intern produktiv nutzen.

**ABER (neu klargestellt in r7-Stufe-A):**

> *"live sync ist unbedingt gewünscht. während der entwicklungs/testphase werde ich auch die bauleiter in der firma miteinbeziehen. also muss ich syncen jederzeit. nicht nur am abend!!"*

**Konkrete Phase-0+1-Realität:**
- **5-10 User** insgesamt (Herbert + Bauleiter + ggf. Polier)
- **Live-Sync zwingend** — nicht nur abends-Sync
- **Multi-User parallel** — mehrere Bauleiter gleichzeitig schreibend
- **24/7-Server-Verfügbarkeit nötig** — auch wenn Herbert im Urlaub
- **Mitarbeiter auf Baustelle** — müssen vom Mobilgerät auf Server zugreifen

### 4. Firma-Internet-Anschluss

User-Antwort r7-Stufe-A: *"Glasfaser/Breitband, weiss aber nicht ob feste IP"*

→ CG-NAT-Test ist Voraussetzung für Architektur-Entscheidung. ChatGPT muss in r7 sagen:
- Wie macht User den CG-NAT-Test (konkrete Anleitung)
- Was bedeutet das Ergebnis je nach Antwort

### 5. Architektur-Disziplin für späteren Verkauf

In 2 Jahren soll Verkauf an externe Kunden ohne Architektur-Umbau möglich sein. Aber: in Phase 0+1 KEIN Code für Installer/Lizenz/AD/RBAC bauen, der heute nicht gebraucht wird.

## Reaktion auf R6-Empfehlungen

User-Antworten zu R6-Rückfragen:
1. **Windows 11 Pro für Kleinstkunden:** Ja, akzeptiert (aber irrelevant in Phase 0+1)
2. **PostgreSQL automatisch oder Kunde-IT:** Installer bietet beides (aber Phase Verkauf, nicht jetzt)
3. **Caddy:** *"wozu brauch ich caddy?"* → Klärungsbedarf in R7
4. **ADR-053 umbenennen:** Erst nach finalem Review

## Repo-Zugriff

- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/bugfixing`**

## Format-Erinnerung

- Canvas, Titel: **"Review Runde 7 — Windows-only Phase 0/1 Multi-User Live-Sync"**
- Direkt zu Claude sprechen
- Footer: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen
- **KEIN Linux mehr empfehlen, auch nicht "optional"**
- Konkret werden, klar empfehlen
- Pragmatisch für Phase 0+1, mit Disziplin-Hinweisen für Phase Verkauf

## Stand was bleibt aus R1-R6 (Code-Stack)

✅ ASP.NET Core 10 + PostgreSQL 17 als Server-Stack (läuft auf Windows wunderbar)
✅ IBpmSyncClient + austauschbare Adapter
✅ Sync-Protokoll Pull/Push, server_version, Server-gewinnt
✅ Spike 0 ProjectDatabase syncfähig (Soft Delete + Upserts) — wichtig
✅ Soft Delete-Logik
✅ DataClassification + Whitelist
✅ device_id in device-settings.json

## Stand was klar ist aus R6 (Windows On-Premise)

✅ Native Windows-Services für Server-Stack (PostgreSQL Windows Service + ASP.NET Worker Service)
✅ Single-Tenant pro Installation (kein Multi-Tenant, keine RLS)
✅ Inno Setup für Installer (später Phase Verkauf, nicht jetzt)
✅ Eigene signierte Lizenzdatei (später, nicht jetzt)
✅ ASP.NET Identity lokal + JWT (jetzt schon nötig wegen Multi-User in Phase 0+1)
✅ Hardware-Empfehlungen für Kunden bekannt

## Aufgabe für Runde 7 — fünf Blöcke

### Block A — CG-NAT-Test der Firma-Glasfaser

**A1 — Konkrete Test-Anleitung:**

Wie testet Herbert ob seine Firma-Glasfaser eine öffentliche IP hat oder hinter CG-NAT?
- Schritt-für-Schritt-Anleitung Router-Webinterface
- Vergleich Router-WAN-IP vs whatismyip.com
- Welche IP-Bereiche sind privat/CG-NAT (10.x, 100.64.x, 172.16-31.x, 192.168.x)
- Was wenn Router keine WAN-IP zeigt (alternative Tests)

**A2 — Architektur-Entscheidung je nach Test-Ergebnis:**

**Fall 1: Öffentliche IP, kein CG-NAT (idealer Fall)**
- Server in der Firma kann öffentlich erreichbar gemacht werden
- DNS via Domain (z.B. bpm.deinefirma.at)
- Glasfaser-Anbieter mit fester IP oder DynDNS
- HTTPS via Caddy + Let's Encrypt
- Jeder Bauleiter öffnet BPM mit `https://bpm.deinefirma.at`
- **Keine Tailscale-Installation nötig pro Mitarbeiter**

**Fall 2: CG-NAT auch bei Glasfaser (Pech)**
- Server nicht direkt öffentlich erreichbar
- Tailscale für 5-10 User: Free Tier reicht NICHT (nur 3 User)
- Tailscale Personal Plus: ~5$/User/Monat = ~30$/Monat für 5-10 User
- Tailscale Premium: ab 6 User ~18$/User
- Alternative: Cloudflare Tunnel (gratis, eine HTTPS-URL)
- Alternative: Windows VPS in Cloud als Server (12-25€/Monat)

Welche Fall-2-Lösung ist für 5-10 User am wartungsärmsten und kostengünstigsten?

### Block B — Caddy-Klärung

**B1 — Wozu Caddy?**

User-Frage wörtlich: *"wozu brauch ich caddy?"*. Beantworte konkret:

- Was macht Caddy genau (Reverse Proxy + automatisches HTTPS)?
- In welchen Fällen ist Caddy nötig vs ASP.NET kann es selbst?
  - Pure HTTP im LAN: Caddy nötig?
  - Self-signed HTTPS im LAN: Caddy nötig?
  - Public HTTPS mit Let's Encrypt: Caddy nötig?
  - Mehrere Apps auf einem Server: Caddy nötig?
- Was sind ASP.NET-eigene Alternativen (Kestrel direkt, dev-cert, Windows-Zertifikatsspeicher)?
- IIS als Alternative auf Windows Server?

**B2 — Empfehlung für Phase 0+1 mit 5-10 User Multi-User Live-Sync:**

Wenn Server-Erreichbarkeit öffentlich (Fall 1): brauchen wir HTTPS? Wenn ja, was am einfachsten (Caddy vs IIS vs ASP.NET selbst)?

### Block C — Konkreter Phase-0+1-Setup-Plan

**C1 — Server-Hardware für 24/7 + 5-10 User:**

Hauptrechner reicht NICHT mehr (24/7-Bedarf, Multi-User). Drei realistische Optionen:

1. **Mini-PC im Firmenbüro** (Intel NUC / Beelink, 300-600€ einmalig, läuft 24/7, ~5W Strom)
2. **Vorhandener Firmen-Server** falls vorhanden
3. **Windows-VPS** (Strato VC 2-8, 12€/Monat, immer erreichbar)

Empfehlung mit Begründung für Herberts Setup (5-10 User, Live-Sync, eigene Firma, ggf. CG-NAT-Glasfaser).

**C2 — Stack-Setup auf gewählter Hardware:**

Konkrete Schritte:
1. PostgreSQL 17 Windows installieren als Service
2. ASP.NET Core 10 Worker Service mit `UseWindowsService()` — Code-Beispiel
3. Service mit `sc.exe` registrieren
4. Auto-Start nach Reboot
5. Logs (Serilog auf Disk + Windows Event Log?)
6. Backup-Skript (PowerShell pg_dump scheduled task)
7. (Falls HTTPS) Caddy oder ASP.NET-eigenes HTTPS

**C3 — Auth-Strategie für 5-10 User in Phase 0+1:**

Multi-User braucht ASP.NET Identity AB JETZT (nicht erst Phase Verkauf):
- 5-10 User in der Firma
- Mehrere Bauleiter parallel
- created_by/last_modified_by braucht reale User-Identität

Empfehlung:
- ASP.NET Core Identity in PostgreSQL
- JWT + Refresh Tokens
- Admin-User-Setup beim Server-Start
- Welche Rollen mindestens (admin, bauleiter, vielleicht polier?)

### Block D — Connectivity konkret für 5-10 User

**D1 — Bei Fall 1 (öffentliche IP):**

- Glasfaser-Anbieter feste IP buchen oder DynDNS-Service?
- Domain registrieren (.at ~15€/Jahr)
- DNS A-Record auf Firma-IP
- Caddy für HTTPS via Let's Encrypt
- Setup-Aufwand für Bauleiter: nur BPM-Client öffnen, Server-URL eingeben — fertig

**D2 — Bei Fall 2 (CG-NAT):**

Variante A: **Tailscale Premium** für 5-10 User
- Kosten: ~30-180€/Monat (je nach User-Anzahl und Plan)
- Setup-Aufwand pro User: Tailscale-App installieren + Account/Login

Variante B: **Cloudflare Tunnel** + Cloudflare Access
- Gratis bis 50 User
- Server hat ausgehende Verbindung zu Cloudflare
- HTTPS-URL public, aber via Cloudflare Access geschützt
- DSGVO: US-Anbieter
- Setup pro User: Cloudflare Access Login (Email/SSO)

Variante C: **Windows-VPS in Cloud**
- Strato VC 2-8: 12€/Monat
- Server in der Cloud, immer erreichbar
- Mitarbeiter via HTTPS-URL, kein VPN
- BPM-Server läuft direkt auf VPS

Welche Variante für 5-10 User am wartungsärmsten + kostengünstigsten?

**D3 — Architektur-Disziplin:**

BPM-Client muss konfigurierbar sein:
- Server-URL aus Settings
- HTTP/HTTPS unterstützen
- Funktioniert mit `http://server-pc:5000`, `https://bpm.firma.at`, `http://100.x.y.z:5000` (Tailscale-IP)
- Spätere Kunden: gleiches Pattern, andere URL

Was muss BPM-Client von Anfang an können um spätere Kunden-Setups zu unterstützen?

### Block E — Roadmap angepasst + Endstand-Empfehlung

**E1 — Phase 0+1 Roadmap (nächste 2 Jahre, 5-10 User Multi-User Live-Sync):**

```
Phase 0 (jetzt - 3 Monate):
  Spike 0: ProjectDatabase syncfähig
  Spike 1: ASP.NET Worker Service Skelett auf Hauptrechner
  CG-NAT-Test der Firma-Glasfaser
  Connectivity-Entscheidung (öffentlich vs Tailscale vs VPS)

Phase 0.5 (3-6 Monate):
  Mini-PC oder VPS einrichten als 24/7-Server
  PostgreSQL + ASP.NET als Windows-Services
  ASP.NET Identity + JWT für Multi-User
  Erste Sync-Tests Solo + 1 Bauleiter

Phase 1 (6-24 Monate):
  Multi-User produktiv für Herberts Firma
  Backup-Strategie automatisch
  Profile in DB (recognition_profiles)
  Konflikt-Behandlung mit Server-gewinnt
  Alle Module fertig programmiert

Phase Verkauf (24+ Monate):
  Inno Setup Installer
  Lizenz-System
  AD-Integration (optional)
  Auto-Update
  Dokumentation für Kunden-IT
  Code bleibt 95% gleich!
```

**E2 — Konkrete Empfehlung Endstand:**

1. CG-NAT-Test JETZT in der Firma machen
2. Je nach Ergebnis: öffentliche-IP-Pfad oder Tailscale/VPS-Pfad
3. Server-Hardware-Wahl (Mini-PC, VPS, vorhandener Firmen-Server)
4. Spike 0 starten (ProjectDatabase syncfähig)
5. Multi-User-Auth jetzt mitdenken (ASP.NET Identity)
6. Caddy nur wenn HTTPS-Pflicht (öffentlich)

**E3 — ADR-053-Inhalt für Phase 0+1:**

Was kommt verbindlich in ADR-053 für die Multi-User-Live-Sync-Phase?

## Bitte als nächstes

Block A: Konkrete CG-NAT-Test-Anleitung + beide Fälle vorbereiten.

Block B: Caddy-Frage klar beantworten — wann brauche ich es WIRKLICH.

Block C: Konkreter Setup-Plan für 24/7-Server mit 5-10 User Multi-User.

Block D: Connectivity-Optionen für 5-10 User mit klarer Empfehlung.

Block E: Roadmap + ADR-053-Inhalt.

**KEIN Linux, auch nicht erwähnen.**

Footer: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen

Ziel nach r7: User kann CG-NAT-Test machen, Connectivity-Entscheidung treffen, Server-Hardware wählen, Spike 0 beginnen. ADR-053 kann anschließend geschrieben werden.
