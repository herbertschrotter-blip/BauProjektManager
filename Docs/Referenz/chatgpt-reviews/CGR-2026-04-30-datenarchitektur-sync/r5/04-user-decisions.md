# User-Entscheidungen — CGR-2026-04-30-datenarchitektur-sync — Runde 5

**Stand:** 2026-04-30 nach r5 + Modell-B-Pivot

---

## Fundamentale Klärung: Geschäftsmodell

User-Frage in r5-Folge: *"warum versteifen wir uns eigentlich so auf linux. ich wüsste nicht welche kleine firma einen linux server [...] da läuft doch überall windows? warum soll ich was für linux programmieren wenn ich es dann später sowieso für windows brauche?!"*

→ Wichtige Klarstellung von Claude: Code ist plattformneutral (ASP.NET Core), aber das Hosting-Modell war nie geklärt.

## Geschäftsmodell-Entscheidung

**User:** *"B: Kunde installiert auf eigenem Server"*

**Konsequenzen:**
- On-Premise-Modell statt Cloud-SaaS
- Pro Kunde eigene Installation (kein Multi-Tenant)
- Windows Server bei Kunden häufig (Bauunternehmen-Realität)
- Lizenz-Modell statt Abo
- Kunde-IT macht Wartung (oder Remote von Herbert)
- DSGVO: Herbert ist Software-Anbieter, nicht Auftragsverarbeiter

## Technische Wahl

**User:** *"Windows VPS für Spike (wenn vor allem Modell B geplant)"*

**Bedeutung:**
- Spike-Server soll auf Windows laufen (Stellvertreter für Kunden-Server)
- Nicht Hetzner Linux CAX11 wie ChatGPT in r5 empfohlen
- Stack-Wahl muss Windows-tauglich sein

## Was R6 klären muss

User stimmte zu: *"R6 mit ChatGPT: On-Premise-Architektur diskutieren (Empfohlen)"*

Themen für R6:
1. Server-Stack beim Kunden: Windows Server + native Services oder Docker auf Windows
2. Installer-Strategie: MSI/Inno Setup/WiX, Auto-Update
3. Auth + AD-Integration: LDAP für Bauunternehmen mit AD
4. Lizenz-System: Eigenbau vs kommerzielle Library
5. Server-Hardware-Empfehlungen für Kunden (5/10/30 User)
6. Test/Dev-Umgebung für Herbert
7. Backup-Strategie für Kunden
8. Remote-Support-Tools
9. Multi-User auf einem Server (ohne Multi-Tenant)
10. DSGVO als Software-Anbieter

## Was aus R1-R5 gültig bleibt

**Code-Stack (plattformneutral):**
- ASP.NET Core 10 + PostgreSQL
- IBpmSyncClient + austauschbare Adapter
- Pull/Push-Protokoll mit server_version
- Spike 0 ProjectDatabase syncfähig (Soft Delete + Upserts)
- 9-Status Decision-Matrix
- DataClassification + Whitelist
- RBAC-Konzept (vereinfacht ohne Multi-Tenant)

**Was obsolet wird:**
- VPS-Hosting-Detail Hetzner CAX11
- CG-NAT + Tailscale-Diskussion
- Multi-Tenant + RLS
- Linux vs Windows VPS-Bewertung in der bisherigen Form
