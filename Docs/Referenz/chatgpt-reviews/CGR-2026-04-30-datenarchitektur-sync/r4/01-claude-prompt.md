# Folgeprompt — CGR-2026-04-30-datenarchitektur-sync — Runde 4

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:
- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/bugfixing`** — IMMER diesen Branch verwenden, NICHT `main`!

## Format-Erinnerung

- Schreibe deine GESAMTE Antwort in Canvas, **Titel: "Review Runde 4 — Hosting 0€ + Portabilität + CG-NAT + R3-Rückfragen"**
- Direkt zu Claude sprechen, nicht zum User
- Am Ende: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen
- Konkret werden, nicht "es kommt darauf an"
- Konkrete Software-Versionen, Preis-Schätzungen, Setup-Schritte

## Stand nach Runde 3

Architektur ist klar:
- BPM-eigenes Sync-Protokoll (`IBpmSyncClient`) + austauschbare Adapter
- ASP.NET + PostgreSQL als produktiver Pfad
- Spike-Reihenfolge: Spike 0 (ProjectDatabase syncfähig) → Spike 1 (CommunityToolkit.Datasync) → Spike 2 (Eigener Minimal-API) → Spike 3 (Supabase Vergleich)
- Profile mittelfristig in DB als `recognition_profiles`
- Server-gewinnt + Sync-Status-Anzeige (keine Merge-UI)
- ASP.NET Core Identity + JWT als Auth
- 6 Rollen: V1 nur admin/bauleiter/gast

## User-Realität (entscheidend für r4)

In r3 hattest du EU-VPS empfohlen. Der User pushed zurück: **0€ Zusatzkosten als Hauptkriterium.**

**Hardware:**
- Hauptrechner (Desktop, Windows 11) + 2-3 weitere PCs (Firmenlaptop, Surface)
- Kein Heim-NAS vorhanden (überlegt aber Anschaffung Synology)
- Kein Firmenserver/IT-Betreuer

**Internet:**
- **NUR HOT Mobilfunk-Verbindung 24/7** (österreichischer Mobilfunkanbieter, A1/Magenta-Reseller)
- **KEINE feste öffentliche IP**
- Sehr wahrscheinlich **CG-NAT** (Carrier-Grade NAT — kein Port-Forwarding möglich, keine eingehenden Verbindungen aus dem Internet erreichbar)
- Bandbreite/Latenz typisch für Mobilfunk: 20-100 Mbit/s, variable Latenz

**Das ist ein Showstopper für klassisches Self-Hosting:**
- Klassische Server-Setups (Port-Forwarding 80/443, DynDNS, Caddy mit Let's Encrypt) funktionieren bei CG-NAT NICHT
- Lösungsraum: Mesh-VPN (Tailscale, ZeroTier), Reverse-Tunnel (Cloudflare Tunnel, ngrok), Hoster-basierte Setups

## Aufgabe für Runde 4 — fünf Blöcke

### Block A — Server-Hardware und OS-Optionen 0€

**A1 — Synology als möglicher Kauf:**

User überlegt **kleine Synology zu kaufen** für Entwicklung/Server-Hosting.

Konkret:
1. Welches OS läuft auf Synology DSM 2026? (DSM 7.2 / 8.x basiert auf Linux)
2. Welche Möglichkeiten für PostgreSQL + ASP.NET auf Synology:
   - Native Synology-Pakete (Container Manager, MariaDB, Web Station)?
   - Docker (Container Manager) — empfohlen?
   - Bare-Metal SSH/Linux-Tools — möglich?
3. Hardware-Empfehlung für BPM-Spike + Phase 1 + Phase 3:
   - **Entry-Level:** DS124, DS224+, DS423+ — Specs, Preis 2026, geeignet?
   - **Mittlere Klasse:** DS923+, DS1522+ — sinnvoll für 5-10 User?
   - **CPU-Architektur:** ARM (Realtek RTD1296) vs x86 (Intel Celeron J4125 etc.) — Auswirkung auf Docker-Image-Verfügbarkeit?
   - Welche **brauche ich realistisch** für Solo-Entwicklung + Phase 3 mit 5-10 Usern?
4. Stromverbrauch Synology vs Desktop-PC vs VPS:
   - DS224+ idle ~10W = ~3€/Monat in AT
   - Desktop 24/7 ~50-100W = ~15-30€/Monat
   - Hetzner CAX11: 5,34€/Monat fest
   - Reale Kostenvergleich über 3 Jahre

**A2 — Lokaler Hauptrechner als Server (User-Stand heute):**

Falls User KEIN Synology kauft:
1. Stack-Wahl auf Windows 11 Hauptrechner:
   - PostgreSQL als Windows-Service (PostgreSQL 17 für Windows) vs Docker Desktop?
   - ASP.NET Core 10 als Windows-Service (über `sc.exe` oder NSSM) oder Docker Container?
   - Für 2026 was ist die einfachste/wartungsärmste Variante?
2. Auto-Start-Verhalten nach Windows-Neustart/Update
3. Energiesparmodus + Wake-on-LAN — funktioniert das mit Tailscale?

### Block B — Server-Portabilität (Docker-Strategie)

User-Frage wörtlich: *"wie erstelle ich dann den server das er auf synology und windows oder hoster läuft?"*

**B1 — Docker als Portabilitäts-Schicht:**

Konkret:
1. Soll BPM-Server **immer als Docker Container** laufen (auch auf Windows)?
   - Vorteil: identisches Setup auf Synology DSM + Windows + VPS
   - Nachteil: Docker Desktop auf Windows hat Lifecycle-Frage (bezahlt für kommerzielle Nutzung)
2. Alternative: Native Windows-Service + Docker auf Synology/VPS?
   - Vorteil: kein Docker Desktop nötig
   - Nachteil: zwei Build/Deploy-Pfade

**B2 — Docker Compose Setup:**

Skizziere konkret eine `docker-compose.yml` für BPM-Server:
- Postgres-Container (PostgreSQL 17 mit persistent volume)
- BPM-Server-Container (ASP.NET Core 10)
- Reverse-Proxy (Caddy oder Traefik) — oder ist das mit Tailscale obsolet?
- Volumes, Networks, Restart Policy
- Multi-Arch (linux/amd64 + linux/arm64) für Synology ARM?

**B3 — Migration zwischen Hosts:**

Wie verschiebt User Daten von Hauptrechner → Synology → später VPS?
1. PostgreSQL `pg_dump`/`pg_restore` — reicht das?
2. Container-Image bauen mit GitHub Actions oder lokal?
3. Wie viel Zeit pro Migration realistisch?

### Block C — Internet-Connectivity bei CG-NAT (kritischer Block!)

User-Frage wörtlich: *"habe nur als internet eine hot mobilverbindung die 24/7 läuft und wie stelle ich die verbindung mit dem internet und anderen gerägten her? habe keine feste ip oder sonst was!!"*

**C1 — CG-NAT-Realitätscheck:**

1. Erkläre kurz was CG-NAT bedeutet und warum es Port-Forwarding unmöglich macht
2. Test-Anleitung: Wie stellt User fest ob er wirklich CG-NAT hat?
   - IP-Vergleich: Router-WAN-IP vs whatismyip.com
   - Wenn unterschiedlich → CG-NAT
3. Bei HOT/Magenta typisch CG-NAT 2026?
4. Workarounds wie "öffentliche IP buchen" beim Mobilfunkanbieter — möglich/Kosten?

**C2 — Lösungen für 0€ Hosting hinter CG-NAT:**

Drei realistische Optionen — bewerte:

**Option 1: Tailscale Mesh-VPN (gratis)**
- Funktioniert hinter CG-NAT (alle Verbindungen ausgehend zum Tailscale-Koordinator)
- Magic DNS: Server-Hostname statt IP
- HTTPS via Tailscale Certificates
- Free Tier: 100 Geräte / 3 User — reicht für Phase 1+3?
- BPM-Client muss Tailscale installiert haben — Akzeptanz für Mitarbeiter?
- Ist es realistisch dass jeder Mitarbeiter Tailscale installiert UND eingeloggt hat?

**Option 2: Cloudflare Tunnel (gratis)**
- Reverse-Tunnel: Server baut ausgehende Verbindung zu Cloudflare auf
- Öffentliche URL via Cloudflare-Domain (kein eigenes Domain-Setup nötig?) oder via eigene Domain
- HTTPS automatisch via Cloudflare
- Free Tier-Limits 2026?
- Datenschutz: Cloudflare ist US-Anbieter (EU-Hosting möglich? AVV?)
- DSGVO-Implikationen für Bauprojekt-Daten via Cloudflare

**Option 3: VPS als reiner Reverse-Proxy + Wireguard zum Hauptrechner**
- VPS für ~5€/Monat (also nicht 0€)
- Wireguard-Tunnel von Hauptrechner zu VPS
- VPS hat öffentliche IP, leitet Traffic zu Hauptrechner
- Komplexer als Tailscale, aber flexibler

**C3 — Empfehlung:**

Welche der drei Optionen für User-Setup (Solo, 0€, CG-NAT, später 5-10 User)?

### Block D — Antworten auf deine 4 r3-Rückfragen

Alle 4 deine r3-Rückfragen werden mit deiner ChatGPT-Empfehlung übernommen. Bitte präzisiere:

**F1: `buildings`-Legacy-Tabelle entfernen?** → Ja
- Was genau ist betroffen im Code? Suche im Branch `feature/bugfixing`
- Reicht "Tabelle DROP + Code-Referenzen entfernen" oder gibt's Daten-Import-Bedarf?

**F2: Server-Versionierung pro Projekt-Scope?** → Ja
- Konkrete Spalten: `server_version` pro Tabelle pro Projekt? Oder zentrale `server_change_log` mit `project_id`?
- Wie wird "global"-Scope für Stammdaten ohne `project_id` (z.B. `clients`) gehandhabt?

**F3: `sync_history` minimal mit Retention?** → Ja
- Konkrete Retention-Policy: 30 Tage UND/ODER 1000 Einträge?
- Cleanup-Job lokal beim Start oder periodisch?
- Welche Felder sind Pflicht für minimale Diagnostik?

**F4: `device_id` in `device-settings.json` einführen?** → Ja
- Format ULID oder GUID?
- Wann generieren — beim ersten Start oder erst bei Server-Login?
- Gehört in `IUserContext` oder eigene `IDeviceContext` Abstraktion?

### Block E — Empfehlung Endstand

Nach Block A-D: Was ist die **finale konkrete Empfehlung** an den User?

1. **Hardware-Wahl:** Synology kaufen? Welches Modell? Oder Hauptrechner reicht?
2. **Connectivity:** Tailscale / Cloudflare Tunnel / VPS-Reverse-Proxy?
3. **Setup-Aufwand:** PT für komplettes Setup (Hardware-Aufbau falls Synology, Software-Installation, Tailscale, Spike-Vorbereitung)
4. **Roadmap:** Wann Wechsel zu echtem VPS unvermeidlich (Mitarbeiter-Anzahl, Bandbreite-Engpass)?
5. **DSGVO bei CG-NAT-Tunneling:** Welche Compliance-Punkte beachten (Tailscale ist US-Firma, EU-Datacenter wo, AVV)?

## Bitte als nächstes

Block A-C ausführlich und konkret. Block D kurz. Block E eine Seite Empfehlung.

Wenn du Synology-Modelle nennst, gerne mit aktuellem Marktpreis 2026. Bei CG-NAT bitte ehrlich sagen ob HOT 100% CG-NAT macht oder ob es Pläne mit öffentlicher IP gibt.

Ziel: Nach r4 entscheidet User direkt:
- Synology kaufen ja/nein, welches Modell
- Tailscale/Cloudflare/VPS-Reverse-Proxy als Connectivity
- Spike-Start kann beginnen

Footer: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen
