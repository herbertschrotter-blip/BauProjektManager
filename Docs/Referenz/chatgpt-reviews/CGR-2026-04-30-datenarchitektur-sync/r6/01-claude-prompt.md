# Folgeprompt — CGR-2026-04-30-datenarchitektur-sync — Runde 6

## ⚠️ WICHTIG: Architektur-Pivot

In r5 hat sich ein fundamentaler Pivot ergeben. Die letzten 5 Runden haben stillschweigend **Modell A (Claude/Herbert hostet zentral, SaaS-Cloud)** angenommen. Der User hat in r5-Stufe-A klargestellt:

> User: *"warum versteifen wir uns eigentlich so auf linux. [...] da läuft doch überall windows? warum soll ich was für linux programmieren wenn ich es dann später sowieso für windows brauche?!"*

Geschäftsmodell-Klärung: **Modell B** = Kunde installiert BPM auf eigenem Server (typisch Windows Server in Bauunternehmen). On-Premise-Lizenz-Verkauf, nicht zentral-gehostetes SaaS.

**Bisherige R1-R5-Empfehlungen die obsolet werden:**
- ❌ Hetzner Linux CAX11 als Produktiv-Hosting
- ❌ Multi-Tenant + RLS-Konzept
- ❌ CG-NAT + Tailscale-Workaround
- ❌ Cloudflare Tunnel
- ❌ Linux vs Windows VPS-Vergleich in bisheriger Form

**Was bleibt gültig (Code-Stack ist plattformneutral):**
- ✅ ASP.NET Core 10 + PostgreSQL als Server-Stack
- ✅ IBpmSyncClient + austauschbare Adapter
- ✅ Sync-Protokoll Pull/Push mit server_version, server-gewinnt
- ✅ Spike 0 ProjectDatabase syncfähig (Soft Delete, Upserts)
- ✅ 9-Status Decision-Matrix
- ✅ DataClassification + Whitelist
- ✅ RBAC-Konzept (jetzt sogar einfacher: kein Multi-Tenant)
- ✅ device_id Konzept

## Repo-Zugriff

- **Repo:** `herbertschrotter-blip/BauProjektManager`
- **Branch: `feature/bugfixing`**

## Format-Erinnerung

- Canvas, Titel: **"Review Runde 6 — On-Premise-Architektur (Modell B Pivot)"**
- Direkt zu Claude sprechen
- Footer: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen
- Konkret werden, klar empfehlen
- Bezug auf die fundamentale Architektur-Änderung nehmen

## User-Realität für Modell B

- Solo-Entwickler Herbert Schrotter
- Plant **BPM als On-Premise-Software für Bauunternehmen** zu verkaufen
- Zielkunden: kleine bis mittlere Baufirmen 5-50 MA in DACH
- Kunden installieren BPM auf eigenem Firmenserver
- Realität bei Bauunternehmen 2026:
  - 90%+ haben Windows-basierte IT (AD, Datei-Server, Office)
  - Häufig kein dedizierter IT-Mitarbeiter, externer Dienstleister
  - Server-Hardware oft: alter Tower-PC, Mini-Server, kleines NAS, manchmal echter Windows Server 2019/2022
- Lizenz-Modell: Kauf + jährlicher Wartungsvertrag (vermutet)

## Aufgabe für Runde 6 — vier Blöcke

### Block A — Server-Stack beim Kunden

**A1 — Stack-Wahl Windows Server:**

Konkret: Welcher Stack ist 2026 wartungsarm für Kunde-IT, die kein Linux/Docker-Experte ist?

**Variante 1: Komplett native Windows-Services**
- PostgreSQL 17 als Windows-Service (EDB Installer)
- ASP.NET Core 10 als Windows-Service (`UseWindowsService()`)
- Caddy for Windows als Reverse Proxy (oder IIS?)
- Pro: Kein Docker-Lifecycle, vertraute Windows-Welt
- Con: Mehr manuelles Setup, komplizierte Updates

**Variante 2: Docker auf Windows Server**
- Linux-Container über Hyper-V oder WSL2
- Pro: Identisch zu Linux-Deployment, ein Compose-File
- Con: Docker Desktop Lizenz-Frage bei kommerzieller Nutzung, WSL2-Komplexität, IT-Unsicherheit

**Variante 3: Bundle als All-in-One-Service**
- Eigener Windows-Service der alles startet (Postgres-Instance + Server + Reverse Proxy)
- Pro: Ein Installer, eine Sache zum Verwalten
- Con: Mehr Eigenbau

Welche Variante ist 2026 für kleine Bauunternehmen-IT realistisch wartungsarm? Welche empfehlen vergleichbare Software-Anbieter (im Bau, im Handwerk, im KMU-Markt)?

**A2 — Reverse Proxy + TLS bei Kunden:**

Im Kunden-LAN: brauchen wir TLS oder reicht HTTP?
- Wenn LAN-only: HTTP reicht
- Wenn von außerhalb (Polier auf Baustelle via VPN/Internet): TLS Pflicht
- Wer richtet TLS ein? Self-Signed reicht im LAN?
- IIS vs Caddy for Windows als Reverse Proxy — Vor/Nachteile bei Kunden-IT

### Block B — Installer + Update + Lizenz

**B1 — Installer-Strategie 2026:**

Welcher Ansatz ist Industrie-Standard für ASP.NET Core Windows Service + PostgreSQL Bundle?
- **Inno Setup:** kostenlos, weit verbreitet
- **WiX Toolset:** MSI, sauberer für Enterprise
- **Advanced Installer:** kommerziell aber komfortabel
- **Squirrel.Windows:** ClickOnce-Nachfolger
- **Custom MSIX:** Microsofts neuer Standard

Was nutzen vergleichbare KMU-Software-Anbieter (BMD, Datev, etc.)?

**B2 — Auto-Update-Mechanismus:**

Zwei Updates: WPF-Client UND Server.
- WPF-Client: ClickOnce vs Eigenbau-Updater vs Velopack/Squirrel?
- Server (Windows-Service): Wie aktualisieren ohne Downtime? Service stoppen, Files ersetzen, starten?
- Update-Verteilung: HTTP-Download von herberts URL? Auto-Check bei Start?
- Wie erfährt Server-Updater Versionen? Manifest-File, GitHub Releases, Update-Server?

**B3 — Lizenz-System:**

- Kommerzielle Library: **Eziriz Intellilock**, **DESlock**, **CryptoLicensing**, **Arctium License Manager** — für 2026 noch tragfähig?
- Eigenbau: Lizenz-Key + Online-Aktivierung mit RSA-Signierung
- Was machen vergleichbare KMU-Anbieter im DACH-Raum?
- Online-Aktivierung erforderlich oder offline möglich?
- Was passiert bei Lizenz-Ablauf — Server stoppt? Read-only-Modus?

### Block C — Auth + AD-Integration + Multi-User

**C1 — Active Directory Integration:**

Bauunternehmen mit 10+ MA haben oft Active Directory. Sollte BPM AD/LDAP unterstützen?
- Pro: Single Sign-On, IT verwaltet User zentral
- Con: Komplexe Auth-Implementierung, nicht alle Kunden haben AD

Drei Auth-Stufen für BPM:
1. **Eigene User-Verwaltung** (ASP.NET Identity in lokaler PostgreSQL)
2. **AD-Integration optional** (LDAP-Login wenn Domain joined)
3. **Hybrid** (eigene User + optionale AD-Anbindung)

Was empfiehlst du für die Roadmap?

**C2 — RBAC ohne Multi-Tenant:**

Bei Modell B: pro Kunde eine Installation. Vereinfacht RBAC:
- Keine Mandantentrennung nötig (jeder Kunde hat eigene DB)
- Rollen weiterhin: admin/bauleiter/polier/disponent/lohnbüro/gast
- Aber: keine project_memberships über Tenants hinweg, nur lokale Projekt-Zugehörigkeit

Konkret welche Vereinfachung gegenüber R3 Multi-Tenant-Variante?

**C3 — Login-Pflicht:**

Bei On-Premise-Lokal-Server: Login Pflicht ab erstem Login? Oder Single-User-Modus für Kleinst-Kunden ohne Login?

### Block D — Dev/Test-Umgebung + Hardware-Empfehlung

**D1 — Test/Dev-Umgebung für Herbert:**

Herbert braucht Test-Setup. Drei Optionen:
1. **Hauptrechner** als Test-Server (PostgreSQL + ASP.NET als Service, Tests via WPF-Client)
2. **Windows-VPS** bei Strato/IONOS (~12-25€/Monat) als Stellvertreter für Kunden-Server
3. **Mini-PC kaufen** (z.B. Intel NUC, 300-500€) als physisches Test-Lab

Was ist sinnvoll für Spike 0+1+2+3?

**D2 — Hardware-Empfehlung für Kunden:**

Was bekommen Bauunternehmen wahrscheinlich:
- **Vorhandener Windows Server 2019/2022** (große Firmen)
- **Tower-PC mit Windows 10/11 Pro** als Server (kleine Firmen)
- **Synology NAS mit Container Manager** (technisch-affine Firmen)
- **Externer Hosting-Dienstleister** (Outsourcing)

Welche Mindest-Specs für 5/10/30 User?
- CPU, RAM, Storage
- OS-Version (Windows 10 Pro reicht? Server 2019? Server 2022?)
- Backup-Empfehlung

**D3 — Remote-Support für Herbert:**

Wenn Kunde Probleme hat — wie hilft Herbert remote?
- TeamViewer (~30€/Monat Business)
- AnyDesk (~15€/Monat)
- Tailscale für SSH/RDP-Zugang
- Microsoft Quick Assist (gratis)
- Was nutzen KMU-Software-Anbieter im DACH 2026?

**D4 — DSGVO als Software-Anbieter:**

- Herbert ist NICHT Auftragsverarbeiter (Kunde hat Daten lokal)
- Aber: Software-Sicherheit, Update-Verantwortung, Vulnerability-Disclosure
- Brauche ich AV-Vertrag mit Kunden? AGB? Software-Wartungsvertrag?
- Was sind die Pflichten als Software-Hersteller in DACH 2026?

### Block E — Roadmap + Empfehlung

Konkret und klar, eine Seite:

1. **Server-Stack-Empfehlung** für On-Premise (Variante 1, 2 oder 3)
2. **Installer-Wahl** (Inno Setup, WiX, oder anderes)
3. **Update-Mechanismus** (welche Library/Strategie)
4. **Lizenz-System** (welche Library oder Eigenbau)
5. **Auth-Strategie** (eigene User + AD später, oder direkt beides)
6. **Dev/Test-Umgebung** für Herbert (Hauptrechner, VPS oder Mini-PC)
7. **Mindest-Specs Kunden-Server** für 5/10/30 User
8. **Geschäfts-Setup** (Lizenz-Modell, Wartungsvertrag, AGB)
9. **Roadmap zu Phase 3 mit On-Premise-Realität**

## Bitte als nächstes

Block A ist Kern. Sei sehr konkret zu Stack-Wahl Windows Server vs Docker.

Block B + C wichtig, weil das ist Modell-B-spezifisch und in den letzten 5 Runden gar nicht behandelt.

Block D + E können kürzer.

Ziel: Nach r6 hat Herbert klare On-Premise-Architektur:
- Welcher Stack beim Kunden
- Wie verteilt er Software (Installer + Update)
- Wie schützt er sich (Lizenz)
- Wo er entwickelt/testet
- Was er Kunden empfehlen kann
- Wann das CGR abgeschlossen ist + ADR-053 geschrieben werden kann

Footer: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen
