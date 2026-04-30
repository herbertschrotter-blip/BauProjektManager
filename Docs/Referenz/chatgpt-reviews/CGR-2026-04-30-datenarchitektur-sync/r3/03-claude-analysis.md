# Claude-Analyse — CGR-2026-04-30-datenarchitektur-sync — Runde 3

**Stand:** 2026-04-30 nach ChatGPT-Antwort r3

---

## Bewertung der Antwort

ChatGPT in r3 ist **sehr fundiert und konkret umsetzbar**. Vier Stärken:

1. **Block A1 Branchen-Praxis (Bau 2026)** ist Gold: 4 Realitäten klar benannt (M365/SaaS-Fachlösungen/ERP+Dateiablage/On-Prem-Restbestand). PlanRadar/Capmo/Dalux als Marktstandard genannt, DSGVO-Realität ehrlich.

2. **Block B1 Spike 0** ist direkt umsetzbar: konkrete Methoden in `ProjectDatabase.cs` benannt, Soft-Delete-Refactor + Upsert-Diff-Logik mit Pseudo-Code, manuelle Test-Szenarien (Test 1-6), 2-3 PT realistisch. Plus Stolperfallen (z.B. `ON CONFLICT DO UPDATE` erhöht Version auch bei identischem Payload).

3. **Block C2 RBAC** passt zu Bau-Branche: 6 Rollen (admin/bauleiter/polier/disponent/lohnbüro/gast) mit klaren Verantwortungsgrenzen. `project_memberships` Tabelle + RLS-Prinzip.

4. **Block D Sync-Tabellen** scharfe Korrektur an meinem Vorschlag: `operation` ist Pflicht, `state='synced'` weglassen (keine Zeile = clean), `scope` für Checkpoints, `details_json` für History.

## Eine Schwäche im Hosting-Block

**Block A2/A3 Hosting-Empfehlung** ist VPS-zentriert. ChatGPT hat alle Optionen behandelt, aber für Phase-3-Setup (5-10 Mitarbeiter) bewertet — nicht für die User-Realität "Solo, 0€ Zusatzkosten gewünscht".

User-Korrektur: *"mir geht es beim hosting vor allem darum das keine zusatzkosten anfallen! warum kein firmenserver oder einfach pc?"*

→ Folge: r4 mit Fokus 0€-Hosting-Lösung für User-Stand (Hauptrechner + 2-3 PCs, kein NAS).

## Wo ich ChatGPT zu 100% folge

- **Spike 0 als Pflicht-Pre-Step:** ProjectDatabase syncfähig vor allem anderen
- **Spike-Reihenfolge:** CommunityToolkit.Datasync (Spike 1) > eigener Minimal-API (Spike 2) > Supabase (Spike 3) — nicht umgekehrt
- **Auth-Empfehlung:** ASP.NET Core Identity + JWT + Refresh Tokens
- **Login-Strategie:** Phase 1 lokal kein Login, Server-Modus erzwingt Login
- **Sync-Tabellen-Modell:** drei Tabellen (sync_state_local, sync_checkpoints, sync_history) mit den Korrekturen
- **`clients` als erster Spike-Kandidat** statt `projects`
- **DatenarchitekturSync.md teilweise superseden** durch ADR-053 (nicht löschen, FolderSync-Pfad superseded, 4-Klassen-Modell bleibt)

## Wo ich nuancieren würde

**1. Hosting-Realismus für 0€-Kriterium**
Die Bewertung "Lokaler Hauptrechner = nur Dev-Modus" war zu hart. Mit **Tailscale (gratis) + Hauptrechner als Server** ist Phase 1 problemlos abdeckbar. Phase 3 (5-10 Mitarbeiter) wird kritisch, aber das ist 2027+.

**2. ChatGPTs vier Rückfragen aus r3 beantworten**
- F1 (Legacy `buildings`-Tabelle entfernen): JA — User hat Frühphase, kein Schmerz
- F2 (Server global vs pro Projekt versionieren): pro Projekt/Scope, passt zu Membership/RLS
- F3 (`sync_history` persistieren oder Serilog reicht): minimale Tabelle mit Retention (30 Tage / 1000 Einträge)
- F4 (`device_id` einführen): JA, einfache stabile ULID in `device-settings.json`

Alle 4 ChatGPT-Empfehlungen sind sinnvoll — könnten in ADR-053 übernommen werden.

## Empfehlung an User

1. **r4 mit Hosting-Fokus** macht Sinn:
   - Wie sieht 0€-Setup auf Hauptrechner + Tailscale konkret aus
   - Welcher Stack (PostgreSQL + ASP.NET als Windows Service oder Docker?)
   - Backup-Strategie ohne externe Kosten (zweite HDD reicht?)
   - Wann wird VPS-Switch unvermeidlich (Anzahl-User, Verfügbarkeit, Strom-Realität)
   - Plus die 4 r3-Rückfragen beantworten

2. **Architektur-Richtung steht** — r4 ist Detail-Klärung, nicht neue Strategie

3. **Nach r4:** CGR-Serie abschließen, ADR-053 schreiben, Tracker-Tasks anlegen
