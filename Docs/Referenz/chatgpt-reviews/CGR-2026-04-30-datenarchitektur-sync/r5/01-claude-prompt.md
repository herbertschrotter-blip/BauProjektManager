# Folgeprompt — CGR-2026-04-30-datenarchitektur-sync — Runde 5

## Repo-Zugriff

- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/bugfixing`**

## Format-Erinnerung

- Canvas, Titel: **"Review Runde 5 — VPS final: Linux vs Windows"**
- Direkt zu Claude sprechen
- Footer: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen
- Konkret werden, klar empfehlen
- Konkrete Provider, Modelle, Preise 2026, ehrliche Lernkurven-Einschätzung

## Stand nach Runde 4

User hat selbst erkannt: **VPS ist über 3 Jahre günstiger als Hauptrechner-24/7 oder Synology**, plus löst CG-NAT-Problem komplett. Damit ist VPS die strategische Wahl.

3-Jahres-TCO-Vergleich (aus r4):
- VPS Hetzner CAX11: ~190-300€
- Synology DS224+: ~680-920€ (inkl. Anschaffung + Strom)
- Desktop 24/7: ~400-950€ (Strom + Verschleiß)

ABER User hat klar gesagt:
> *"gibt es den hetzner server auch mit windows os? ich kenne mich bei linux gar nicht aus"*

Das ist der entscheidende offene Punkt für r5. Linux VPS sind ~5€/Monat, Windows VPS ~15-30€/Monat (3-6× Aufschlag wegen Lizenz).

## Aufgabe für Runde 5 — vier Blöcke

### Block A — Linux vs Windows VPS: ehrliche Bewertung

**A1 — Linux-Lernkurve mit Docker Compose realistisch:**

User hat keine Linux-Erfahrung. Beantworte konkret:

1. **Wie viel Linux muss User wirklich können?**
   - Bei Container-basiertem Setup (Docker Compose): Welche Befehle braucht er täglich? (ssh, docker compose up/down/logs, ...)
   - Welche Befehle braucht er bei Fehlerdiagnose? (systemctl, journalctl, top, df, ...)
   - Realistische Lernkurve: 1 Tag, 1 Woche, 1 Monat?
   - Welche Tutorials/Kurse als Einstieg (gratis, deutsch, 2026)?

2. **Tools die Linux-Pain reduzieren:**
   - VS Code Remote-SSH-Extension: wie gut ersetzt das lokales Arbeiten?
   - Portainer als Docker-Web-UI: lohnt sich das für Solo?
   - Caddy als Reverse-Proxy: wirklich "0-Config" oder doch komplex?
   - Provider-Snapshots vs eigene Backup-Skripte

3. **Wann reißt die Linux-Lernkurve den User runter?**
   - Bei Standard-Setup laufend OK: wahrscheinlich
   - Bei Fehlern/Crashes: kritischer Moment
   - Bei Sicherheits-Update-Zwang: realistisch nervig?
   - Solo ohne IT-Support: was ist der schlimmste Fall?

**A2 — Windows VPS realistisch:**

1. **Welche Provider bieten Windows VPS in EU mit DSGVO-AVV?**
   - Strato V-Server Windows
   - IONOS Cloud Server Windows
   - Azure VM B-Series Windows
   - AWS Lightsail Windows
   - Andere?

2. **Konkrete Modelle 2026 + Preise:**
   - Vergleichbar zu Hetzner CAX11 (2 vCPU, 4 GB RAM, 40 GB SSD)
   - Welcher Anbieter, welches Modell, welcher Preis pro Monat
   - Lizenz-Modell (inklusive vs separate)?
   - Backup-/Snapshot-Pakete inkludiert?

3. **Windows Server für ASP.NET + PostgreSQL:**
   - Windows Server 2022/2025 — ASP.NET native + PostgreSQL als Windows-Service?
   - Docker auf Windows Server (Container Linux Mode oder Windows Containers)?
   - Reverse Proxy: IIS oder Caddy für Windows oder Traefik?
   - Welcher Stack ist 2026 wartungsarm auf Windows Server?

**A3 — Konkrete Kostenrechnung 3 Jahre:**

```
Linux Variante (Hetzner CAX11):
- 5,34€ × 36 Monate = 192€
- Backup Storage Box ~3,49€ × 36 = 126€
- Total: ~318€

Windows Variante (welcher Provider?):
- ?€/Monat × 36 = ?€
- Backup inkl. ja/nein
- Total: ?
```

→ Beziffere den realen Aufschlag für Windows.

**A4 — Empfehlung mit klarer Begründung:**

Welche Wahl für Solo-Entwickler ohne Linux-Erfahrung?
- Lohnt der Linux-Lernaufwand sich für ~10€/Monat Ersparnis (über 3 Jahre = ~360€)?
- Oder ist Windows VPS pragmatisch besser (vertraute Umgebung, mehr Zeit für BPM-Code)?
- Gibt es einen **Mittelweg** (z.B. mit verwaltetem Provider, Plesk-Panel, Cloud-Panel)?

### Block B — Falls Linux: Empfohlener Setup-Pfad

Wenn die Empfehlung "Linux + Docker" lautet:

**B1 — Konkrete Setup-Schritte für Hetzner CAX11:**

```text
1. Hetzner Cloud Account anlegen
2. CAX11 mit Ubuntu 24.04 LTS bestellen
3. SSH-Key generieren + auf Server hochladen
4. Erste SSH-Verbindung
5. ?
```

Liste die Schritte 1-15 konkret auf, mit konkreten Befehlen wo möglich. Welche Tools auf dem User-PC (PuTTY/Windows Terminal/VS Code Remote-SSH)?

**B2 — Docker Compose YAML komplett:**

Lieferbares `docker-compose.yml` für BPM-Server (PostgreSQL + ASP.NET + Caddy), wie es auf dem VPS ausgerollt wird. Plus `Caddyfile` für TLS-Auto via Let's Encrypt.

**B3 — Domain-Setup:**

- User braucht eine Domain für Let's Encrypt-TLS, oder?
- Oder reicht Hetzner-Subdomain `xxx.hetzner-cloud.de`?
- Domain bei welchem Anbieter (Hetzner, Strato, INWX, ...)? Preis 2026?
- DNS-Setup-Schritte konkret?

**B4 — Backup-Strategie konkret:**

- Hetzner Snapshots vs Storage Box?
- `pg_dump`-Cron-Job, wie?
- Restore-Test-Anleitung
- Was kosten reale Backups über 3 Jahre?

### Block C — Falls Windows VPS: Empfohlener Setup-Pfad

Wenn die Empfehlung "Windows VPS" lautet:

**C1 — Konkrete Setup-Schritte:**

Welcher Provider, welches Modell, wie konfigurieren?

**C2 — Stack-Wahl auf Windows Server:**

- PostgreSQL als Windows-Service oder Docker Container?
- ASP.NET als Windows-Service oder als IIS Site oder Docker?
- Welcher Reverse Proxy (IIS, Caddy für Windows, nginx-Windows)?

**C3 — RDP für Wartung:**

- RDP-Setup, Sicherheit (Port-Wechsel, MFA, Fail2ban-Äquivalent)?
- Tailscale für Admin-Zugriff statt öffentliches RDP?

**C4 — Backup auf Windows VPS:**

- Provider-Snapshots
- `pg_dump` als Scheduled Task
- Was anders als bei Linux?

### Block D — Antworten auf r4-Rückfragen

ChatGPT hatte in r4 vier Rückfragen. Bitte beantworten:

**1. HOT-Router WAN-IP-Test:**
- Wie geht der Test konkret? Schritt für Schritt
- Was wenn HOT keine WAN-IP im Webinterface zeigt?
- Welche Tools auf dem PC reichen (whatismyip + traceroute)?

**2. Tailscale auf Firmenlaptop erlaubt?**
- Wenn Firmenlaptop Domain-joined ist: was sagt typische Firmen-IT-Policy?
- Workarounds wenn Tailscale blockiert ist?

**3. HTTP nur in Tailscale-Netz oder sofort HTTPS?**
- Bei VPS-Pfad ist HTTPS automatisch via Caddy/Let's Encrypt — also obsolet
- Bei Hauptrechner-Pfad mit Tailscale: HTTP intern reicht?

**4. Synology-Zweck:**
- User hat klargestellt: Synology nicht primär für BPM, eher Idee
- Empfehlung: Synology nur kaufen wenn ohnehin Backup/Dateizentrale gewünscht — bestätigt das?

### Block E — Empfehlung Endstand

Konkret, klar, eine Seite:

1. **Hosting-Entscheidung:** Linux VPS (welcher Provider/Modell) oder Windows VPS (welcher Provider/Modell)?
2. **Setup-Aufwand:** PT für Linux-Pfad vs Windows-Pfad
3. **Kosten 3 Jahre:** Beziffert
4. **Risiken:** Was kann schiefgehen, wie absichern
5. **Roadmap zu Phase 3:** Wann skalieren, was passiert
6. **DSGVO:** Empfohlener Provider mit AVV

## Bitte als nächstes

Block A entscheidet alles. Sei dort sehr ehrlich zur Lernkurve. User ist Solo-Entwickler, keine IT-Backup, hat Familie, kann nicht 3 Wochen Linux-Tutorials machen. Aber er kann auch nicht 30€/Monat verbrennen für Bequemlichkeit.

Block B oder C — je nachdem was Block A empfiehlt. Wenn sehr unsicher: beide kurz, Empfehlung am Ende.

Block D + E kurz.

Ziel: Nach r5 entscheidet User direkt:
- Linux oder Windows VPS, welcher Provider, welches Modell
- Setup kann beginnen oder zumindest Spike 0 + ADR-053-Schreiben

Footer: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen
