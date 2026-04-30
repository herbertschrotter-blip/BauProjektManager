# Claude-Analyse — CGR-2026-04-30-datenarchitektur-sync — Runde 5

**Stand:** 2026-04-30 nach ChatGPT-Antwort r5 + User-Pivot

---

## ChatGPT-Antwort r5: technisch sauber, aber falsche Annahme

ChatGPT hat in r5 sehr fundiert empfohlen:
- Hetzner CAX11 + Ubuntu 24.04 LTS + Docker Compose + Caddy
- Linux-Lernkurve mit Runbook beherrschbar
- 3-Jahres-TCO Linux ~350€ vs Windows ~650-900€
- Konkreter Setup-Plan mit 15 Schritten + komplette docker-compose.yml

**Aber**: Die ganze Empfehlung gilt für **Modell A (du hostest zentral)**.

## User-Pivot: Modell B (On-Premise beim Kunden)

User hat klargestellt:
- *"warum versteifen wir uns eigentlich so auf linux. ich wüsste nicht welche kleine firma einen linux server oder wenn sie wollen einen linux os pc haben. da läuft doch überall windows?"*
- *"warum soll ich was für linux programmieren wenn ich es dann später sowieso für windows brauche?!"*

→ User plant **Modell B** (Kunde installiert auf eigenem Server, häufig Windows).

## Was die letzten 5 Runden für Modell B noch wert sind

**Bleibt gültig (Code-Stack ist plattformneutral):**
- ASP.NET Core 10 + PostgreSQL als Server-Stack
- IBpmSyncClient + austauschbare Adapter
- Sync-Protokoll Pull/Push, server_version, server-gewinnt
- Spike 0 ProjectDatabase syncfähig
- Soft Delete + Upsert-Logik
- 9-Status-Decision-Matrix
- DataClassification + Whitelist
- RBAC-Konzept (sogar einfacher: kein Multi-Tenant)
- device_id Konzept

**Wird obsolet oder neu gedacht:**
- VPS-Hosting-Empfehlung (Hetzner CAX11) → war für Modell A
- CG-NAT + Tailscale + Cloudflare-Diskussion → irrelevant in Kunden-LAN
- Multi-Tenant + RLS → entfällt (pro Kunde eigene Installation)
- Linux vs Windows VPS-Bewertung → anders gewichtet
- Cloud-Backup-Strategie → Kunde-IT verantwortlich

## Konsequenzen für R6

R6 muss komplett neue Themen behandeln:

1. **Server-Stack beim Kunden:** Windows Server + native Services (ASP.NET als Service + PostgreSQL Windows-Service + Caddy für Windows oder IIS) — oder Linux-Container auf Windows Server (Docker Desktop ist Lizenz-Thema)
2. **Installer-Strategie:** MSI? Inno Setup? WiX? Update-Mechanismus für WPF-Client + Server
3. **Auth + AD-Integration:** Bauunternehmen mit 10+ MA haben oft Active Directory — sollte BPM LDAP-Login unterstützen?
4. **Lizenz-System:** Eigenbau vs kommerzielle Library, Online-Aktivierung
5. **Server-Hardware-Empfehlungen für Kunden:** Specs für 5/10/30 User (Mini-PC, NAS, Tower-Server, vorhandener Windows Server)
6. **Test/Dev-Umgebung für Herbert:** Spike auf Hauptrechner reicht? Windows-VPS als Stellvertreter? Mini-PC kaufen?
7. **Backup-Strategie für Kunden:** Integriertes Tool oder nur Anleitung
8. **Remote-Support:** TeamViewer/AnyDesk/Tailscale für dich als Admin
9. **Multi-User auf einem Server:** Auth/Rollen ohne Multi-Tenant
10. **DSGVO als Software-Anbieter:** AV-Vertrag mit Kunden, Software-Hardening, Update-Verantwortung

## Empfehlung an User

R6-Prompt schreiben mit Fokus On-Premise-Architektur. Die ganze Hosting-/CG-NAT/VPS-Diskussion war für Cloud-Modell A, ist obsolet für Modell B. ChatGPT muss den Architektur-Pivot kennen und nicht alte Empfehlungen wiederholen.

Nach R6 sollte klar sein:
- Welcher Stack beim Kunden (Windows Server native vs Container)
- Installer + Update-Mechanismus
- Hardware-Empfehlung für 5/10/30-User-Kunden
- Test-Umgebung für Herbert
- Aufwands-Schätzung Phase 3 mit On-Premise-Komplexität

Dann CGR abschließen, ADR-053 schreiben (mit On-Premise-Fokus), Tracker-Tasks anlegen.
