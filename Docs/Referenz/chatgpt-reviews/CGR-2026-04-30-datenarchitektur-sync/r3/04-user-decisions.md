# User-Entscheidungen — CGR-2026-04-30-datenarchitektur-sync — Runde 3

**Stand:** 2026-04-30 nach r3 + Folgefrage

---

## User-Korrektur an ChatGPTs Hosting-Empfehlung

**User:** *"mir geht es beim hosting vor allem darum das keine zusatzkosten anfallen! warum kein firmenserver oder einfach pc?"*

**Interpretation:** ChatGPT hat in r3 EU-VPS empfohlen (5-20€/Monat). Der User hat **0€ Zusatzkosten als Hauptkriterium** und fragt zurecht warum die kostenlosen Optionen (Hauptrechner-PC, Firmenserver, Heimserver) nicht ernsthaft betrachtet wurden.

→ Folge: r4 muss diese 0€-Optionen seriös bewerten, mit Fokus auf User-Realität.

## Hardware-Stand des Users

**User:** *"Hauptrechner + 2-3 weitere PCs, kein NAS"*

**Konsequenz:** Für r4 sind die realistischen 0€-Optionen:
- Lokaler Hauptrechner als Server + Tailscale für Zugriff von Laptop/Surface
- (Firmenserver entfällt — nicht vorhanden)
- (Heim-NAS entfällt — nicht vorhanden, Anschaffung wäre 400€ Zusatzkosten)

→ Folge: r4-Fokus auf "Lokaler Hauptrechner als Server" mit konkreter Setup-Anleitung.

## Wie weiter

**User:** *"Runde 4 mit Hosting-Fokus 0€ (Empfohlen)"*

→ r4-Folgeprompt wird mit drei Schwerpunkten geschrieben:
1. Konkretes 0€-Hosting-Setup auf User-Hauptrechner (PostgreSQL + ASP.NET, Windows Service vs Docker, Tailscale-Setup)
2. Backup-Strategie ohne externe Kosten
3. Wann wird VPS-Switch unvermeidlich (Phase-3-Realismus)

Plus: Antworten auf die 4 r3-Rückfragen von ChatGPT in r4 mitgegeben werden:
- F1 Legacy `buildings`-Tabelle: JA, entfernen
- F2 Server-Versionierung: pro Projekt/Scope
- F3 `sync_history`: minimale Tabelle mit Retention
- F4 `device_id`: JA, in `device-settings.json`

## Stufe-A-Fragen (nicht formal gestellt, durch User-Folgefrage ersetzt)

ChatGPT hatte 4 Rückfragen in r3 — die werden in r4 als Vorab-Antworten mitgegeben, nicht separat stufenweise abgefragt.
