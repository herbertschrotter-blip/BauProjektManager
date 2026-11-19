---
doc_id: konzept-plattform-drei-programme
doc_type: concept
authority: secondary
status: draft
owner: herbert
topics: [plattform, firmenserver, vpn, active-directory, schnittstellen, baukostenmanager, baulogistikmanager, nachkalkulation, browser, pwa, updates, module]
read_when: [server-architektur, schnittstelle-baukostenmanager, schnittstelle-baulogistikmanager, client-wpf-oder-browser, nachkalkulation, updates-und-module, lieferpruefung-maengel]
related_docs: [adr, konzept-mobile, konzept-kalkulation, konzept-zeiterfassung, konzept-server-architektur, architektur, dsgvo-architektur]
related_code: []
supersedes: []
---

## AI-Quickload
- Zweck: Entwurf – BPM als eines von drei Firmenprogrammen auf gemeinsamer Server-Plattform
- Autorität: secondary
- Lesen wenn: Server-Architektur, Schnittstellen zu BauKostenManager oder BauLogistikManager, WPF oder Browser, Nachkalkulation, Updates und Module, Lieferprüfung und Mängel
- Nicht zuständig für: PlanManager-Fachlogik (→ PlanManager-Docs), entschiedene Sync-Architektur (→ ADR-053, solange ADR-068 nicht angenommen ist)
- Kapitel:
  - 1. Zweck und Zielzustand
  - 2. Datenmodell (geplant)
  - 3. Workflow
  - 4. Technische Umsetzung
  - 5. Abhängigkeiten
  - 6. No-Gos / Einschränkungen
  - 7. Offene Fragen
- Pflichtlesen:
  - Kapitel 6 bei jeder Änderung an Server, Schnittstellen, Geld- oder Lohndaten
- Fachliche Invarianten:
  - Entwurf: nichts davon gilt, bis Herbert ADR-068 und ADR-069 annimmt
  - Geld (Preise, Lohnsätze, Lohnkosten, Rechnungen, IBAN) nur im BauKostenManager, nie im BPM
  - Kein Programm greift direkt auf die Datenbank eines anderen zu – nur über Schnittstellen
  - Zugriff nur aus dem Firmennetz oder über das Firmen-VPN

---

> ⚠️ **Entwurf vom 19.11.2026 – nicht entschieden.** Entstanden aus der Planung für den Firmenbetrieb
> (ADR-053 Phase 0/1). Verbindlich erst mit der Annahme von ADR-068 und ADR-069.
> Bis dahin gelten ADR-051 und ADR-053 unverändert.

# BauProjektManager — Konzept: Plattform mit drei Programmen

**Version:** 0.1 (Entwurf)
**Datum:** 19.11.2026
**Status:** Entwurf zur Prüfung durch Herbert
**ADRs:** ADR-068 (Plattform), ADR-069 (Client) – beide Proposed

---

## 1. Zweck und Zielzustand

Für den Firmenbetrieb sind drei Programme geplant. Jede Gruppe arbeitet in genau einem, das Geld bleibt im Büro.

| Programm | Wer | Was es kann | Welche Daten es hält |
|----------|-----|-------------|----------------------|
| **BauKostenManager** (BKM, neu) | Einkauf, Buchhaltung, Geschäftsführung; Bauleitung für Freigaben | Anfragen, Preisspiegel, Bestellungen, Rechnungslauf (Eingangsrechnungen prüfen und freigeben), Zahlungsdatei, Buchhaltungs- und Lohnexport, Kosten-Nachkalkulation, Auswertungen | alles mit Geld: Preise, Preisvereinbarungen, Rechnungen, IBAN, Freigaben, Verrechnungssätze, Lohnarten und Lohnsätze |
| **BauProjektManager** (BPM) | Bauleitung, Poliere, Vorarbeiter – am PC und am Handy | Pläne (PlanManager), Bautagebuch, Fotos, Stundenaufzeichnung, Material bestellen, Lieferung prüfen mit Mängelprotokoll, Baustellenkalender mit Kransperren, Mengen- und Stunden-Nachkalkulation | Baustellendaten ohne Preise |
| **BauLogistikManager** (BLM, neu) | Lager, Fahrer, Disponent | Lager, interne Lieferscheine, Disposition, Touren, Fuhrpark | Mengen, Lagerplätze, Fahrzeuge, Touren, Zeiten, Kilometer – ohne Preise |

**Zielzustand BPM:** ein Server-Programm auf dem Firmenserver, bedient im Browser am PC und als installierbare
Web-App am Handy (Vorschlag ADR-069). Die Fachlogik (Domain, Infrastructure, PlanManager) bleibt und läuft
serverseitig; neu entsteht die Oberfläche.

## 2. Datenmodell (geplant)

**Wer führt welche Stammdaten:**
- BKM führt Teilfirmen, Lieferanten, Artikel (mit Preisen) und Bestellungen. BPM und BLM bekommen Kopien ohne Preise.
- Bauvorhaben: offen, welches Programm führt (Kapitel 7).
- Artikel tragen für die Logistik zusätzlich Ladeeinheit, Maße und Gewicht (BLM), aber nie einen Preis außerhalb des BKM.

**Neue BPM-Daten (Arbeitsnamen):**

| Bereich | Inhalt |
|---------|--------|
| Materialbestellung | Bestellwunsch der Baustelle: Bauvorhaben, Lieferort, Wunschtermin, Positionen in Mengen |
| Lieferprüfung | je Lieferung und Position: bestellt, geliefert, passt / andere Menge / Mangel; Lieferschein-Foto |
| Mängelprotokoll | Nummer je Bauvorhaben (z. B. 14-M-0007), Art, Menge, Fotos, Notiz, Wunsch der Baustelle, Unterschrift des Fahrers oder Vermerk „verweigert“, Status |
| Baustellenkalender | Betonagen, Fertigteilmontage, Kransperren mit Zeitfenster |
| Stunden | je Mitarbeiter, Baustelle, Tätigkeit; Freigabe durch Vorarbeiter oder Bauleitung – ohne Lohnsätze |

**Was aus dem BPM verschwindet:** die restricted-Tabellen aus `ModuleKalkulation.md` mit Lohnsätzen
(`hourly_rate`), Einheitspreisen (`unit_price`) und Lohnkosten. Sie gehören in den BKM.

**Schema-Regel:** Bis zum Ende der Frühphase gilt INDEX.md „Projekt-Phase“ (Reset statt Migration). Mit den ersten
Firmendaten endet die Frühphase; ab dann Migrationen (ADR-068 P.8, Zeitpunkt entscheidet Herbert).

## 3. Workflow

**Was über die Schnittstellen fließt:**
- **BKM → BPM und BLM:** Bauvorhaben, Artikel und Bestellungen, immer ohne Preise.
- **BPM → BKM:** freigegebene Stunden, Lieferprüfungen und Mängelprotokolle, Lieferschein-Fotos, Bestellwünsche für den Einkauf, Mengen für die Nachkalkulation.
- **BPM ↔ BLM:** Materialbestellungen und Kransperren hin; Lieferstatus, Ankunftszeit und freie Plätze auf Touren zurück.
- **BLM → BKM:** Lagerbewegungen, interne Lieferscheine, Transportzeiten und Kilometer.

**Lieferprüfung mit Mängelprotokoll (BPM am Handy):**
1. Der Polier öffnet die erwartete Lieferung (aus der Bestellung) oder fotografiert den Lieferschein.
2. Je Position: passt, andere Menge oder Mangel.
3. Mangel: Art, Menge, Fotos aus der App, Notiz (auch per Sprache), Wunsch der Baustelle. Funktioniert ohne Netz.
4. Übergabe: Der Fahrer unterschreibt am Handy „unter Vorbehalt übernommen“; verweigert er, wird das vermerkt.
5. Das Mängelprotokoll geht an den BKM; der Einkauf gibt es frei und schickt es an den Lieferanten.
6. Der Polier sieht den Stand (freigegeben, gesendet, Antwort, Rechnung gesperrt). Preise sieht er nicht.

Handy-Mockups dazu liegen als Claude-Artefakt vor (noch nicht in `Docs/Mockups/seiten.json`). Übernahme in die
Seitenliste über den Mockup-Ablauf, sobald ADR-069 entschieden ist.

**Nachkalkulation in zwei Stufen:**
- **BPM:** Mengen, Stunden, Aufwandswerte (z. B. Stunden je m²), Soll gegen Ist – ohne Geld, auch für den Polier.
- **BKM:** Kosten in Euro aus den gemeldeten Mengen und Stunden, mit Rechnungen, Preisen, Lohn- und
  Verrechnungssätzen. Die Bauleitung sieht das Ergebnis im BKM.

## 4. Technische Umsetzung

**Gleicher Stack für alle drei:** C# / .NET 10, ASP.NET Core als Windows-Dienst, PostgreSQL – wie ADR-053 P.1–2.
Ein gemeinsames Grundgerüst als Projektvorlage: Anmeldung, Rechte, Journal, Melden-Knopf, Sicherung, Update-Ablauf.

**Betrieb:** Firmenserver oder eine VM darauf. Zugriff nur aus dem Firmennetz oder über das Firmen-VPN, internes
Zertifikat statt öffentlicher Domain. Anmeldung mit dem Windows-Konto (Active Directory), Rollen aus AD-Gruppen.

**Schnittstellen:** HTTP-API im Firmennetz. Jede Meldung trägt eine eindeutige ID, der Sender schreibt sie in dieselbe
Transaktion wie die Änderung (Outbox) und stellt sie zu, bis der Empfänger quittiert; der Empfänger erkennt Doppeltes.
Verträge sind versioniert und tragen eine DataClassification (ADR-035). Felder mit Geld sind in Verträgen an BPM und
BLM verboten.

**Client (Vorschlag ADR-069):** Browser am PC, installierbare Web-App (PWA, ADR-019) am Handy mit Offline-Speicher für
Lieferprüfung, Mängel, Stunden und Tagebuch. Die WPF-Oberfläche wird eingefroren und läuft als Übergang weiter.

**Fachlogik wiederverwenden:** Dank der Dateisystem-Ports (ADR-060) läuft die PlanManager-Logik auch auf dem Server.
Plandateien liegen dann zentral in der Dateiablage des Servers statt im Cloud-Ordner jedes PCs.

**Updates:**
1. Eine neue Version entsteht im Repo; die automatischen Tests laufen.
2. Je Programm ein Installationspaket (Dienst und Web-Oberfläche).
3. Zuerst auf dem Testsystem mit einer Kopie der echten Daten einspielen und prüfen.
4. Auf dem Firmenserver sichert ein Skript die Datenbank, stoppt den Dienst, spielt die neue Version ein, führt die
   Umbauschritte der Datenbank aus und startet den Dienst. Schlägt etwas fehl, geht es zurück auf die alte Version
   und die Sicherung.
5. PCs und Handys: nichts zu installieren. Der Browser lädt die neue Version beim nächsten Öffnen; die Handy-App
   meldet „neue Version“ und überträgt vorher noch nicht gesendete Daten.
6. Schnittstellen bleiben eine Version lang abwärtskompatibel, damit die drei Programme nicht gleichzeitig
   aktualisiert werden müssen.

**Module:** Ein Modul ist ein Teil des Programms (eigenes Projekt bzw. eigene DLL wie heute PlanManager und Settings).
Es kommt mit einem Update auf den Server, seine Tabellen entstehen über die Umbauschritte der Datenbank, eingeschaltet
wird es per Einstellung je Teilfirma oder Rolle. Abgeschaltet bleiben seine Daten erhalten. Auf PCs und Handys wird
kein Modul installiert. Bliebe die WPF-Oberfläche, bräuchte jeder PC eigene Updates (Velopack, ADR-053 P.25).

## 5. Abhängigkeiten

| Doc / ADR | Was sich bei Annahme ändert |
|-----------|-----------------------------|
| ADR-053 | Hosting, Zugang, Anmeldung, DB-Reset (ADR-068 P.3, P.4, P.8) |
| ADR-051 | local-first gilt nur noch für die Handy-App (ADR-069) |
| ADR-019 | PWA bleibt, wird Haupt-Client am Handy |
| ADR-018 | Excel als Wahrheitsquelle für Stunden neu prüfen – Stunden liegen dann im BPM-Server |
| ADR-035 | DataClassification gilt auch für die Schnittstellen-Verträge |
| ADR-060 | Voraussetzung, damit die Fachlogik serverseitig läuft |
| `ModuleKalkulation.md` | Lohnsätze, Einheitspreise, Lohnkosten wandern in den BKM |
| `ModuleZeiterfassung.md` | Stunden ohne Lohnsätze; Lohnarten und Lohnexport im BKM |
| `BPM-Mobile-Konzept.md` | neue Funktionen: Material bestellen, Lieferprüfung, Mängelprotokoll, Stunden |
| `ServerArchitektur.md` | Rollen `disponent` und `lohnbüro` gehören in BLM bzw. BKM |

## 6. No-Gos / Einschränkungen

- Keine Preise, Lohnsätze, Lohnkosten, Rechnungen oder IBAN im BPM – auch nicht in Schnittstellen-Daten an BPM oder BLM.
- Kein direkter Datenbankzugriff zwischen den Programmen.
- Kein Zugang aus dem Internet ohne VPN, kein Hosting außerhalb der Firma.
- Keine neuen WPF-Oberflächen, solange ADR-069 offen ist; Arbeit an der Fachlogik bleibt erlaubt.
- Keine neue Bibliothek ohne Freigabe (CLAUDE.md).

## 7. Offene Fragen

1. Client: Browser und Handy-App (Vorschlag ADR-069) oder WPF am PC und Browser nur am Handy?
2. Bleibt eine Einzelplatz-Version ohne Firmenserver für andere Kunden? Mit Browser-Client reicht dafür der Dienst
   auf demselben PC.
3. Welches Programm führt die Bauvorhaben?
4. Wann endet die Frühphase (erste Firmendaten), ab wann gelten Migrationen?
5. Reicht „Server gewinnt“ (ADR-053 P.5) für offline erfasste Mängel und Stunden am Handy?
6. Plandateien zentral auf dem Server statt im Cloud-Ordner je PC – passt das für alle Projekte?
