# User-Entscheidungen — CGR-2026-04-30-datenarchitektur-sync — Runde 7

**Stand:** 2026-04-30 nach Stufe-A r7

---

## Antworten zu ChatGPTs 4 Rückfragen

### 1. CG-NAT-Test im Firmennetz

**User:** *"mir ist es unmöglich momentan zu testen. ich habe keinen zugriff darauf. für die entwicklungsphase müsste ich glaube ich einen server mieten!"*

→ **Entscheidung: Windows-VPS mieten für Phase 0/1.**

CG-NAT-Frage damit obsolet — VPS hat eigene öffentliche IP. Kein Router-Zugriff in der Firma nötig.

### 2. Vorhandener Firmen-Server

**User:** *"Server vorhanden, aber unklar ob ich Zugriff habe"*

→ Realistisch nicht nutzbar für Phase 0/1. Klärung Adminrechte/IT-Dienstleister wäre Aufwand. VPS ist sauberer.

### 3. User-Anzahl in ersten 6 Monaten

**User:** *"5-6 User (Tailscale Free reicht)"*

→ Bei VPS-Wahl ist Tailscale ohnehin obsolet. Aber: 5-6 User ist bestätigt — kleinste VPS-Klasse (Strato VC 2-8 oder ähnlich) reicht.

### 4. Bauleiter/Polier-Geräte

**User:** *"Mix: Windows-Laptops jetzt, Mobile-Pläne später"*

→ WPF-Client funktioniert auf allen Mitarbeiter-Geräten. Mobile (BPM-Mobile) bleibt post-v1-Thema.

## Konsolidierte Hosting-Entscheidung Phase 0/1

**Windows-VPS in EU (DSGVO-tauglich):**

```
Provider-Kandidaten:
- Strato Windows V-Server VC 2-8: 12€/Monat
  2 vCores, 8 GB RAM, 120 GB Storage, Windows Server 2025
- IONOS Windows VPS M: ~16-18€/Monat
  2 vCore, 4 GB RAM, 120 GB NVMe
- Andere mit Windows + EU-Hosting: prüfbar

Empfehlung: Strato VC 2-8 (Preis-/Leistungs-Sieger)
```

## Konsequenzen für Architektur

**Was jetzt entschieden ist:**

✅ Server-Hardware: Windows-VPS (kein Mini-PC, kein Hauptrechner)
✅ Server-Stack: PostgreSQL 17 Windows + ASP.NET Core 10 Worker Service + Caddy (für HTTPS via Let's Encrypt)
✅ Connectivity: Domain + DNS + HTTPS direkt — keine Tailscale, kein VPN
✅ Auth: ASP.NET Identity + JWT von Anfang an
✅ User: 5-6 in ersten 6 Monaten, skalierbar
✅ Geräte: Nur Windows-Laptops/Surface in Phase 0/1
✅ Mobile: erst post-v1 (BPM-Mobile)
✅ Code-Disziplin: BPM-Client topologieneutral, ServerUrl konfigurierbar

**Was im ADR-053 verbindlich rein muss:**

1. Windows-only für Entwicklung, Test, Produktion
2. Phase 0/1: Windows-VPS in EU (Strato VC 2-8 oder vergleichbar)
3. Stack: PostgreSQL 17 Windows-Service + ASP.NET Core 10 Worker Service + Caddy for Windows
4. Domain + HTTPS via Let's Encrypt
5. ASP.NET Identity + JWT + Refresh Tokens
6. Rollen Phase 0/1: admin, bauleiter, polier, gast
7. BPM-Client: ServerUrl konfigurierbar, HTTP/HTTPS, topologieneutral
8. Sync: Pull/Push, server_version, Server-gewinnt
9. Spike 0: ProjectDatabase syncfähig (Soft Delete + Upserts)
10. DataClassification + Whitelist
11. device_id in device-settings.json
12. Single-Tenant pro Installation (keine Multi-Tenant/RLS)
13. recognition_profiles in DB (post Spike 0)
14. Frühphase: keine Migration, DB-Reset bei Schema-Änderungen
15. Phase Verkauf später: Inno Setup Installer, Lizenz, AD-Integration optional

## CGR-Serie Status

**Architektur-Diskussion ist vollständig.** Die 7 Runden waren:

- R1: Server-Pfad statt FolderSync/CouchDB
- R2: IBpmSyncClient + ASP.NET/PostgreSQL + austauschbare Adapter
- R3: Spike-Plan + RBAC + ADR-053-Struktur
- R4: VPS über 3J. günstiger als 0€-Optionen, CG-NAT-Realität
- R5: Linux vs Windows — User-Pivot zu Modell B
- R6: On-Premise-Architektur, Windows-Stack
- R7: Multi-User Live-Sync, 5-10 User Phase 0/1, Connectivity-Detail

**Resultat:** Klare Architektur, klare Hosting-Wahl, klare Roadmap.

## Nächste Schritte (kein R8 nötig)

1. CGR-Serie abschließen (README finalisieren, INDEX auf "Abgeschlossen")
2. ADR-053 schreiben mit den 15 verbindlichen Punkten
3. Tracker-Tasks anlegen:
   - Spike 0: ProjectDatabase syncfähig (existiert schon im Tracker, evtl. neu priorisieren)
   - Windows-VPS einrichten (post Spike 0)
   - ASP.NET-Server-Skelett bauen
   - PostgreSQL-Setup-Doku
   - ASP.NET Identity Integration
   - Erste Sync-Endpoints
4. Spike 0 starten — der Code-Vorbereitungsschritt
