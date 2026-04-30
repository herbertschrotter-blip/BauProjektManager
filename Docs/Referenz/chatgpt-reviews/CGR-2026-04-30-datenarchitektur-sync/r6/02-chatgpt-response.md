# ChatGPT-Antwort — CGR-2026-04-30-datenarchitektur-sync — Runde 6

**Modell:** GPT-5.4
**Format:** Canvas — "Review Runde 6 — On-Premise-Architektur (Modell B Pivot)"
**Eingegangen:** 2026-04-30

---

[Vollantwort gekürzt - Hauptergebnisse:]

## Block A — Server-Stack beim Kunden

**Empfehlung: Variante 1 — native Windows-Services.**
- PostgreSQL 17 als Windows-Service
- ASP.NET Core 10 als Windows-Service mit `UseWindowsService()`
- Caddy for Windows als optional für TLS/Reverse Proxy
- WPF-Client im LAN

Begründung:
- Passt zu KMU-Bau-IT (Windows-geprägt, AD, RDP, Dienste, Firewall)
- Kein Docker-Lifecycle beim Kunden
- IT-Dienstleister verstehen Windows-Dienste, Ereignisanzeige
- PostgreSQL Windows Installer von EDB ist offiziell etabliert
- Geringste Support-Reibung

Variante 2 (Docker) und 3 (All-in-One-Service) verworfen.

## Block B — Installer + Update + Lizenz

**Installer:** Inno Setup V1 (kostenlos, scriptbar, Standard für Windows-Service-Installer in KMU). Später WiX/Advanced Installer wenn Enterprise-Bedarf.

**Update:**
- V1 Server: Manueller Update-Installer durch Admin
- V1 Client: Inno Setup Update vom Server
- V1.5 Client: Velopack Auto-Updater
- V1.5 Server: Update-Manifest + Admin-Hinweis
- Versions-Kompatibilität Server↔Client prüfen

**Lizenz:** Eigene signierte Lizenzdatei (Ed25519/RSA), Public Key im Server, Private Key bei Herbert. Offline-fähig V1. Bei Wartungsablauf: Software läuft weiter, Updates/Support gesperrt — kein harter Stopp.

## Block C — Auth + AD-Integration + Multi-User

**Auth-Roadmap:**
- V1: ASP.NET Identity lokal + JWT + Refresh Token
- V1.5: AD-User-Import manuell
- V2: LDAP/AD optional
- V3: OIDC/Entra ID

**RBAC:** Vereinfacht ohne Multi-Tenant. Pro Kunde eigene DB. Rollen: V1 admin/bauleiter/gast, V1.5 polier, V2 disponent/lohnbüro.

**Login-Pflicht:** Servermodus immer Login-pflichtig. Solo-Lokal weiter ohne Login (Modus A).

## Block D — Dev/Test-Umgebung + Hardware

**Test:** Hauptrechner reicht für Spike 0/1. Vor erstem Kunden Mini-PC (Intel NUC, 250-600€) oder Windows-VPS für Installer/Remote-Test.

**Kunden-Hardware:**
- 5 User: Windows 11 Pro, 4 Kerne, 16 GB RAM, 250 GB SSD
- 10 User: Windows Server 2022/2025, 4-6 Kerne, 16-32 GB RAM, 500 GB SSD
- 30 User: Windows Server, 8 Kerne, 32-64 GB RAM, 1 TB SSD, IT-Dienstleister Pflicht

**Remote-Support:** Microsoft Quick Assist gratis, später AnyDesk/TeamViewer.

**DSGVO:** Kunde ist Verantwortlicher (Daten lokal). Herbert: AVV/Fernwartungsklausel im Wartungsvertrag, keine Telemetry ohne Hinweis.

## Block E — Roadmap

Phase 0 jetzt: ADR-053 On-Premise pivotieren, Spike 0
Phase 1 Server Alpha: Windows-PostgreSQL+ASP.NET-Service, clients/projects Pull/Push
Phase 2 Installierbarkeit: Inno Server+Client Installer, Admin-User-Setup, pg_dump-Backup
Phase 3 Multi-User On-Prem: ASP.NET Identity, Rollen, sync_state, recognition_profiles
Phase 4 Kundenfähigkeit: Update, Lizenzfile, Support-Prozess, IT-Doku
Phase 5: AD/LDAP, weitere Module, Docker als Optionalpfad

## ✅ Einigkeit / ⚠️ Widerspruch / ❓ Rückfragen

**Einigkeit:**
- ASP.NET Core 10 + PostgreSQL bleibt richtig
- IBpmSyncClient, Pull/Push, Server-gewinnt bleiben
- Spike 0 weiter wichtig
- RBAC ohne Multi-Tenant deutlich einfacher
- On-Premise macht Backup/Installer/Update/Lizenz/Support zu Kernthemen

**Widerspruch:**
- Linux/VPS-Fixierung als Produktivziel falsch
- Docker als Pflicht beim Kunden falsch
- Multi-Tenant/RLS als Kernarchitektur falsch
- AD/LDAP in V1 falsch
- Harter Lizenz-Stopp bei Wartungsablauf falsch

**Rückfragen:**
1. Windows 11 Pro für Kleinstkunden? → User: Ja
2. PostgreSQL automatisch oder Kunde-IT vorher? → User: Installer bietet beides
3. Caddy sofort oder optional? → User: Wozu brauche ich das?
4. ADR-053 jetzt umbenennen? → User: Erst nach Final-Review

---

**User-Folge-Reaktion:**
> "Bevor wir um den heißen brei herumreden. zuerst mal will ich es nur selbst und in meiner firma einsetzen bis es fertig programmiert ist!! das dauert sicher noch 2 jahre. ich will nur jetzt schon sicher gehen das es später ohne viel aufwand auch verkauft werden kann!! das solltest du berücksichtigen. Linux werde ich sicher niemals freiwillig verwenden. nicht mal zum entwickeln oder testen!! also das nimm komplett raus und erkläre das auch chatgpt. [...] wozu brauch ich caddy? [...] wie funktionierrt die kommunikation dann vom server zb in der firma, zur baustelle am laptop?"

→ R7-Pivot:
1. Phase 0/1 = Solo + eigene Firma 2 Jahre, KEIN Verkauf
2. Linux komplett raus — niemals, nicht mal Test
3. Windows-only für Entwicklung UND Produktion
4. Frage: Server↔Baustelle Connectivity
5. Frage: Caddy nötig oder ASP.NET selbst genug
6. Architektur muss in 2 Jahren ohne Umbau verkaufbar sein
