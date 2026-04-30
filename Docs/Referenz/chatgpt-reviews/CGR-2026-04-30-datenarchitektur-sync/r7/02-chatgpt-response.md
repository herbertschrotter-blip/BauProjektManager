# ChatGPT-Antwort — CGR-2026-04-30-datenarchitektur-sync — Runde 7

**Modell:** GPT-5.4
**Format:** Canvas — "Review Runde 7 — Windows-only Phase 0/1 Multi-User Live-Sync"
**Eingegangen:** 2026-04-30

---

[Vollantwort - Hauptergebnisse:]

## Block A — CG-NAT-Test der Firma-Glasfaser

**Konkrete 3-Schritt-Test-Anleitung:**
1. Router-Webinterface öffnen (192.168.0.1, 192.168.1.1, 10.0.0.1, fritz.box) → WAN-IPv4 notieren
2. Im Browser whatismyip.com / ifconfig.me / ipinfo.io → öffentliche IP notieren
3. Vergleichen

**Privat/CG-NAT IP-Bereiche:** 10.x, 172.16-31.x, 192.168.x, 100.64.x

**Fall 1 (öffentliche IP, kein CG-NAT):** Bester Fall - Firmen-Server + Domain + HTTPS möglich, keine Tailscale pro User nötig.

**Fall 2 (CG-NAT):** Tailscale Free 2026 = bis 6 User (Korrektur zu früherem 3-User-Stand), Personal Plus $8/User/Monat. Bei 7-10 User wird Windows-VPS billiger.

## Block B — Caddy-Klärung

**Caddy macht:** HTTPS-Termination, Let's Encrypt automatisch, Reverse Proxy zu ASP.NET.

**Wann nötig:**
- Pure HTTP im LAN: NEIN (Kestrel direkt)
- Self-signed HTTPS LAN: optional
- Public HTTPS mit Let's Encrypt: JA (einfachster Weg)
- Mehrere Apps: JA

**ASP.NET-Alternativen:** Kestrel direkt (HTTP) oder mit Zertifikat (HTTPS, aber LE-Automatisierung fehlt), IIS (Windows-IT vertraut, aber komplexer Setup).

**Empfehlung Phase 0/1:** Caddy nur wenn HTTPS/Baustelle über Internet. LAN-Test ohne Caddy.

## Block C — Phase-0+1-Setup-Plan

**Server-Hardware (Hauptrechner ist OUT):**
- Option 1: Mini-PC im Firmenbüro (Intel N100/i5, 16-32 GB RAM, 500-1TB NVMe, Windows 11 Pro, 300-700€ einmalig, 5-15W Strom)
- Option 2: Vorhandener Firmenserver (wenn da)
- Option 3: Windows-VPS (Strato VC 2-8: 12€/Monat, 2 vCores, 8 GB RAM, 120 GB, Windows Server 2025)

**Stack-Setup-Schritte:**
1. PostgreSQL 17 EDB Installer als Windows-Service (Datenbank `bpm`, User `bpm_app`)
2. ASP.NET Core 10 Worker Service mit `UseWindowsService()` — Code-Skelett gegeben
3. `sc.exe create BpmServer binPath=...` für Service-Registrierung
4. Auto-Start + Recovery konfiguriert
5. Serilog File + Event Log
6. PowerShell pg_dump-Backup als Scheduled Task
7. Caddy nur wenn HTTPS gewünscht

**Auth-Strategie ab JETZT:**
- ASP.NET Identity in PostgreSQL
- JWT 15-30 min Access + 30-Tage Refresh Token pro Gerät
- Rollen: admin, bauleiter, polier, gast (polier jetzt schon wegen Baustellenzugriff)
- Login-Flow für WPF-Client

## Block D — Connectivity 5-10 User

**Fall 1 öffentliche IP:** Domain + DNS A-Record + Router-Portforward 80/443 + Caddy + HTTPS. Kein VPN pro User.

**Fall 2 CG-NAT — Empfehlung Windows-VPS bei >6 User:**
- Tailscale Free bis 6 User OK, Personal Plus 8$/User/Monat ab 7
- Cloudflare Tunnel + Access gratis bis 50 User, aber WPF-Client-Auth komplexer
- Windows-VPS Strato 12€/Monat + Domain ~25€/Jahr — bei 7-10 User kostengünstigste Lösung

**Architektur-Disziplin BPM-Client:**
- ServerUrl konfigurierbar
- HTTP/HTTPS unterstützen
- Topologieneutral: `http://server-pc:5000`, `https://bpm.firma.at`, `http://100.x.y.z:5000`

## Block E — Roadmap

**Phase 0 (jetzt-3 Monate):** CG-NAT-Test, Spike 0 (ProjectDatabase syncfähig), Server-Skelett, PostgreSQL local, Identity-Entwurf, Connectivity-Entscheidung.

**Phase 0.5 (3-6 Monate):** 24/7-Server bereitstellen (Mini-PC oder VPS), Services, Identity produktiv, erste Pull/Push-Strecke clients/projects, 2-Client-Test, Backup+Restore.

**Phase 1 (6-24 Monate):** 5-10 User intern produktiv, Live-Sync, recognition_profiles in DB, Server-gewinnt, Backup-Automatik, Module fertig.

**Phase Verkauf (24+ Monate):** Inno Setup Installer, Lizenzfile, Update-Mechanismus, AD optional, Kunden-IT-Doku.

**ADR-053 verbindlich:** 15 Punkte gegeben (Windows-only, Single-Tenant, 5-10 User Phase 0/1, PostgreSQL+ASP.NET-Services, Caddy nur Public, ServerUrl konfigurierbar, Pull/Push, Spike 0, Identity+JWT, Rollen, DataClassification, device_id, Topologieneutralität, kein Installer/Lizenz/AD jetzt, Phase Verkauf später).

## ✅ Einigkeit
- Windows-only gilt vollständig
- Phase 0/1 = 5-10 User Live-Sync
- Hauptrechner als 24/7-Server raus
- ASP.NET Core 10 + PostgreSQL 17 bleibt
- ASP.NET Identity ab jetzt nötig
- Caddy nur für HTTPS/Public
- CG-NAT-Test entscheidet Hardware-Wahl
- BPM-Client topologieneutral

## ⚠️ Widerspruch
- Kein anderes OS als Windows
- Tailscale nicht als Standard ab 7+ User
- Cloudflare Tunnel nicht als erster Standard für WPF-Live-Sync
- Kein HTTP über Internet
- Kein Aufschieben von Auth bei Multi-User

## ❓ Rückfragen
1. WAN-IP vs whatismyip-Ergebnis im Firmennetz?
2. Vorhandener 24/7 Windows-Server im Büro oder nur Arbeitsplatz-PCs?
3. 5-6 oder 7-10 User in ersten 6 Monaten realistisch?
4. Bauleiter/Polier Firmenlaptops oder auch Mobilgeräte (Tablet/Smartphone)?
