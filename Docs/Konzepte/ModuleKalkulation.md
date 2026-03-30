# BauProjektManager — Modul: Kalkulation & Bauzeitprognose

**Erstellt:** 29.03.2026  
**Status:** Konzept (Won't have V1)  
**Abhängigkeiten:** Zeiterfassung, Bautagebuch  
**Vorbilder:** Herberts Excel-Tools `Kalkulation_v2.xlsx` + `Ziegelberechnung_ESS_St__Georgen.xlsm`  
**Architektur-Auswirkung:** Erweiterung der bestehenden DB, keine Umbau nötig

---

## 1. Ziel

Aus eigenen Erfahrungswerten (gesammelt über Arbeitseinteilung + Zeiterfassung) die Bauzeit für neue Projekte schätzen. Keine Tabellenbuch-Werte, keine Normen — echte Messdaten von echten Baustellen mit echten Mannschaften.

**Kernformel:**
```
Soll-Menge × Erfahrungswert × Erschwernis = geschätzte Arbeitsstunden
```

**Beispiel:**
```
850 m² Mauerwerk 38er × 0,62 h/m² × 1,15 (Hanglage) = 606 Arbeitsstunden
÷ 4 Mann × 8h = 32 Ah/Tag → 19 Arbeitstage
```

---

## 2. Kernproblem: Warum scheitern Nachkalkulationen in der Praxis?

### 2.1 Bisheriger Ansatz (Excel Kalkulation_v2)

40+ Spalten pro Messung: Datum, Bauvorhaben, Objekt, Bauteil, System, Hersteller, Produkt, Maße (l,b,h,t,d), m², m³, kg, Stk, FA, Stunden, LH, EH/LH, LH/EH, Lohnkosten, Arbeitsschritt, Maschinen (1-3), Werkzeug (1-3), Bemerkungen.

**Problem:** Zu aufwändig für tägliche Erfassung. Wurde nie konsequent durchgezogen.

### 2.2 Tägliches Aufmaß ist unrealistisch

Professionelle Tools (bau-mobil, 123erfasst, BauSU) wollen tägliche Mengenerfassung pro LV-Position. Das funktioniert in der Praxis nicht, weil:

- **Betonwände:** 8 Leute arbeiten gleichzeitig an Schalen, Bewehren, Schließen und Betonieren — Hand in Hand, zeitlich überlappend. Man kann nicht pro Tag sagen "3h Schalen, 2h Bewehren, 3h Betonieren".
- **Mauerwerk:** Die Kolonne mauert eine Woche am EG. Man weiß die Gesamt-m² (aus dem Plan) und die Gesamtstunden. Tägliches Aufmaß wäre Unsinn.
- **Schalung/Bewehrung bei Decken:** Geht über mehrere Tage pro Bauteil. Die Menge (m², to) kennt man pro Bauteil aus dem Plan.

### 2.3 BPM-Lösung: Abschluss-Erfassung statt Tages-Aufmaß

**Nicht:** "Wie viel hast du heute geschafft?"  
**Sondern:** "Wann war das Arbeitspaket fertig — die Stunden zähle ich selber mit."

Der Polier muss nur:
1. **Morgens** (2 min): Leute den Arbeitspaketen zuweisen
2. **Bei Abschluss** (30 sek): "Fertig" drücken → Menge bestätigen

Die Stunden sammelt BPM automatisch im Hintergrund.

---

## 3. Zentrales Konzept: Das Arbeitspaket

### 3.1 Was ist ein Arbeitspaket?

Ein Arbeitspaket = **Bauteil + Geschoß + Tätigkeit + Soll-Menge**

Das ist die zentrale Einheit auf die alles gebucht wird: Stunden, Leute, Material, Fortschritt.

**Beispiele:**

| Bauteil | Geschoß | Tätigkeit | Soll-Menge | Einheit | Quelle |
|---------|---------|-----------|-----------|--------|--------|
| H5 | EG | Mauerwerk 38er | 198 | m² | Ziegelberechnung |
| H5 | EG | Betonwand komplett | 85 | m² Schalung | DokaCad |
| H5 | EG | Decke Schalung | 170 | m² | DokaCad |
| H5 | EG | Decke Bewehrung | 6,4 | to | Bewehrungsplan |
| H5 | EG | Decke Betonieren | 34 | m³ | Betonbestellung |
| H5 | 1.OG | Mauerwerk 38er | 185 | m² | Ziegelberechnung |

### 3.2 Wand-Komplett vs. Einzel-Schritte

Bei Betonwänden arbeiten Schalen, Bewehren, Betonieren gleichzeitig und überlappend. Deshalb gibt es zwei Modi:

**Modus "Gesamtpaket" (Standard für Wände):**
- Ein Arbeitspaket "Betonwand komplett H5/EG" mit m² Schalung als Leiteinheit
- Alle Stunden (Schalen + Bewehren + Betonieren + Ausschalen) laufen auf dieses eine Paket
- Ergebnis: Gesamt-h/m² für den kompletten Wandtakt

**Modus "Einzelschritte" (für Decken, Bodenplatten):**
- Separate Pakete: "Decke Schalung", "Decke Bewehrung", "Decke Betonieren"
- Schritte sind zeitlich getrennt → sinnvoll einzeln zu erfassen
- Bewehrung wird separat oder mit Beton zusammen erfasst (Polier entscheidet)

**Umschalter:** Checkbox pro Arbeitspaket: "☐ Einzelschritte separat erfassen"

### 3.3 Soll-Mengen — woher kommen sie?

Die Mengen pro Arbeitspaket kommen aus verschiedenen Quellen, die der Polier schon hat:

| Quelle | Liefert | Für welche Pakete |
|--------|---------|------------------|
| **Ziegelberechnung (Excel)** | m² Mauerwerk pro BT+Geschoß, Ziegeltyp | Mauerwerk |
| **DokaCad** | m² Schalung pro Takt, lfm, Ecken, Höhe, Sichtbeton | Wände, Decken |
| **Betonbestellung (Excel)** | m³ Beton, Güte, BT+Geschoß+Abschnitt | Betonieren |
| **Bewehrungslisten (Statiker)** | to/kg Bewehrung pro Bauteil | Bewehrung |
| **LV-Import** | Alle Positionen mit Massen | Alles (wenn LV vorhanden) |
| **Manuell** | Polier tippt es ein | Alles was nirgends herkommt |

Die Mengen sind in der Regel BEKANNT bevor die Arbeit beginnt — aus Plan, Berechnung oder Bestellung. Kein zusätzliches Aufmaß nötig.

---

## 4. Datenfluss — wie alles zusammenhängt

### 4.1 Übersicht

```
SOLL-MENGEN (einmalig, bei Projektstart)
├── Ziegelberechnung → m² Mauerwerk pro BT/Geschoß
├── DokaCad → m² Schalung pro Takt
├── Betonbestellung → m³ Beton pro BT/Geschoß
├── LV-Import → alle Positionen mit Massen
├── Manuell → was sonst nirgends herkommt
│
▼
┌─────────────────────┐
│    WORK_PACKAGES     │ ← Arbeitspakete (BT + Geschoß + Tätigkeit + Soll-Menge)
│  "Was muss gebaut    │    = die zentrale Tabelle
│   werden?"           │
└──────────┬──────────┘
           │
    ┌──────┼──────────────────┐
    ▼      ▼                  ▼
┌────────┐ ┌──────────┐ ┌──────────┐
│ARBEITS-│ │ ZEIT-     │ │ BAUTAGE- │
│EINTEIL.│ │ ERFASSUNG │ │ BUCH     │
│wer→was │ │ wer→h     │ │ Wetter,  │
│(tägl.) │ │ (tägl.)   │ │ Fotos,   │
└───┬────┘ └────┬──────┘ │ Notizen  │
    │            │        └──────────┘
    └──────┬─────┘
           ▼
    ┌──────────────┐
    │ Ah pro Paket │ ← automatisch:
    │ (summiert)   │    Einteilung × Stunden
    └──────┬───────┘
           │
           ▼ bei "Fertig"-Klick
    ┌──────────────┐
    │ NACHKALK     │    198 m² ÷ 123 Ah
    │ Menge ÷ Ah   │  = 1,61 m²/Ah
    └──────┬───────┘
           │
           ▼
    ┌──────────────┐
    │ ERFAHRUNGS-  │ ← wächst mit jedem Projekt
    │ KATALOG      │
    └──────┬───────┘
           │
           ▼
    ┌──────────────┐
    │ BAUZEIT-     │ ← neue Baustelle:
    │ PROGNOSE     │    Soll-Mengen × Erfahrung × Erschwernis
    └──────────────┘
```

### 4.2 Was der Polier täglich tun muss

**Morgens (2 Minuten):** Arbeitseinteilung — Leute den Arbeitspaketen zuweisen. Genau wie im bestehenden Excel-Blatt: Matrix mit Arbeitern links, Tätigkeiten oben, "x" setzen.

**Abends (1 Minute):** Bautagebuch bestätigen — BPM baut den Eintrag automatisch zusammen aus Arbeitseinteilung + Zeiterfassung + Wetterdaten. Polier ergänzt Besonderheiten, Fotos, Notizen. Ein Klick → fertig.

**Bei Abschluss einer Tätigkeit (einmalig, 30 Sekunden):** "Mauerwerk EG H5 fertig" → BPM schlägt Menge vor (198 m² aus Ziegelberechnung) → Polier bestätigt → Nachkalk wird automatisch berechnet.

### 4.3 Was AUTOMATISCH passiert (ohne Zutun des Poliers)

- Arbeitsstunden pro Arbeitspaket summieren (aus Einteilung × Zeiterfassung)
- Start-/Enddatum pro Paket erkennen (erster/letzter Tag mit Zuordnung)
- Fortschritt berechnen (Status: geplant → in Arbeit → fertig)
- Leistungswert berechnen bei "Fertig" (Menge ÷ Ah)
- Bautagebuch-Vorschlag zusammenbauen
- Wetterdaten automatisch erfassen (API)

### 4.4 Was NIE manuell eingegeben werden muss

- Arbeitsstunden pro Tätigkeit (kommt aus Einteilung × Zeiterfassung)
- Von/Bis-Datum (BPM merkt sich den ersten und letzten Zuordnungstag)
- Leistungswert h/EH (wird berechnet)
- Bautagebuch-Grundgerüst (wird automatisch vorgeschlagen)

---

## 5. Arbeitseinteilung (täglich)

### 5.1 Herkunft

Herbert hat in seiner Excel-Arbeitszeiterfassung bereits ein Arbeitseinteilungs-Blatt: Matrix mit Arbeitern in Zeilen, Tätigkeiten als farbige Balken in Spalten, "x" zum Zuweisen. Blau + durchgestrichen = Urlaub/Krank/Kranfahrer. Rechts die Stunden pro Mann, rot markiert wenn jemand fehlt. Unten pro Tätigkeit die zugeordneten Arbeiter gelistet.

Das hat die Excel überladen — Zeiterfassung + Arbeitseinteilung + Leistungserfassung in einer Tabelle. Besser: eigenes Modul mit Zugriff auf die Stunden-DB.

### 5.2 BPM-Umsetzung

```
┌─────────────────────────────────────────────────────────────────┐
│ Arbeitseinteilung: ÖWG Dobl — Mittwoch, 05.11.2025             │
│                                                                  │
│          │ ██ Attika  │ ██ Bew.Decke │ ██ Tische  │ ██ SW Kanal│
│          │   (grün)   │    (blau)    │  (gelb)    │  (dk.grün) │
│ ─────────┼────────────┼─────────────┼────────────┼────────────│
│ Biskup D.│     x      │             │            │            │ 8h
│ Jelic M. │            │             │     x      │            │ 8h
│ Funtek J.│            │             │            │            │ 8h
│ ░Kuhar Z░│            │             │            │            │ U
│ Cahunek I│     x      │      x      │            │            │ 8h
│ ░Biskup Z░│           │             │            │            │ K
│ ░Cahun. J░│           │             │            │            │ U
│ Dula A.  │            │      x      │            │            │ 8h
│ Krunoslav│            │      x      │            │            │ 8h
│ Sostaric │            │             │            │     x      │ 8h
│ Andrij.  │            │             │     x      │            │ 3h
│ Mandic M.│            │             │            │            │ 8h
│ ░Skomec M░│           │             │            │            │ K
│ Lorenci L│            │      x      │     x      │            │ 8h
│ Kosutic I│            │             │            │            │ 8h
│ ─────────┼────────────┼─────────────┼────────────┼────────────│
│ Leute:   │     2      │      4      │     2      │     1      │
│ Ah:      │    16      │     32      │    11      │     8      │
│ ─────────┴────────────┴─────────────┴────────────┴────────────│
│                                                                  │
│ Legende: ░░░ = Urlaub (U), Krank (K), Kranfahrer               │
│ [Speichern]  [Vom Vortag übernehmen]  [Drucken]                │
└─────────────────────────────────────────────────────────────────┘
```

**Features:**
- Tätigkeiten = aktive Arbeitspakete des Projekts (aus work_packages)
- Arbeiter = Mitarbeiterliste (aus employees)
- "x" setzen = Zuordnung (work_assignments)
- Stunden kommen aus Zeiterfassung (time_entries)
- "Vom Vortag übernehmen" — die meisten Tage ändern sich wenig
- Farbcodes für Tätigkeiten (konfigurierbar)
- Abwesende (U, K, Sonderaufgaben) farbig markiert

### 5.3 Bautagebuch-Vorschlag

Am Abend baut BPM aus der Arbeitseinteilung automatisch den Bautagebuch-Eintrag:

```
┌─────────────────────────────────────────────────────────────────┐
│ Bautagebuch: 05.11.2025 — ÖWG Dobl                             │
│                                                                  │
│ Wetter: ☀️ 12°C (automatisch)                                   │
│                                                                  │
│ Tätigkeiten (automatisch aus Arbeitseinteilung):                │
│ • Attika: Biskup D., Cahunek I. — 16 Ah                        │
│ • Bewehrung Decke Rampe: Cahunek I., Dula A., Krunoslav,       │
│   Lorenci L. — 32 Ah                                            │
│ • Tische ausschalen: Jelic M., Andrijevic (3h), Lorenci L.     │
│   — 11 Ah                                                       │
│ • SW Kanal: Sostaric M. — 8 Ah                                 │
│                                                                  │
│ Personal: 10 anwesend, 3 abwesend (2× U, 1× K)                │
│                                                                  │
│ Besonderheiten: [________________________________]  ← Polier    │
│ Fotos: [+ Foto hinzufügen]                                     │
│                                                                  │
│ [✓ Bestätigen]  [Bearbeiten]                                    │
└─────────────────────────────────────────────────────────────────┘
```

---

## 6. Erfassungslogik pro Bauteiltyp

### 6.1 Betonwände (Gesamtpaket)

```
Arbeitspaket: "Betonwand H5/EG — Takt 1"
Modus: Gesamtpaket (Schalen+Bewehren+Betonieren+Ausschalen)
Leiteinheit: m² Schalung (aus DokaCad)

Tag 1: Schalen + Bewehren beginnen → 8 Mann × 8h = 64 Ah
Tag 2: Schließen + Betonieren      → 8 Mann × 8h = 64 Ah
                                     ─────────────────────
                                     Gesamt: 128 Ah

"Fertig" → 85 m² Schalung (aus DokaCad)
Ergebnis: 85 m² ÷ 128 Ah = 0,66 m²/Ah (Wandtakt komplett)

Zusatzinfos (optional): 24 m³ Beton C25/30, Sichtbeton: Nein
```

**Warum Gesamtpaket:** 8 Leute arbeiten gleichzeitig an verschiedenen Schritten. Einer stellt Schalung vor, zwei bewehren am gestrigen Takt, drei schließen vorgestern und einer betoniert. Die Schritte überlappen sich — einzelne Zuordnung ist unmöglich.

### 6.2 Decken + Bodenplatten (Einzelschritte)

```
Arbeitspaket 1: "Decke H5/EG — Schalung"
Tag 5-6: 4 Mann × 2 Tage = 64 Ah
"Fertig" → 170 m² (aus DokaCad)
Ergebnis: 170 m² ÷ 64 Ah = 2,66 m²/Ah

Arbeitspaket 2: "Decke H5/EG — Bewehrung"
Tag 7: 6 Mann × 8h = 48 Ah
"Fertig" → 6,4 to (vom Statiker)
Ergebnis: 6,4 to ÷ 48 Ah = 0,13 to/Ah

Arbeitspaket 3: "Decke H5/EG — Betonieren"
Tag 8: 4 Mann × 6h = 24 Ah (+ Betonierzeit erfassen)
"Fertig" → 34 m³ C25/30 (aus Betonbestellung)
Ergebnis: 34 m³ ÷ 24 Ah = 1,42 m³/Ah
```

**Warum Einzelschritte:** Die Arbeitsschritte bei Decken sind zeitlich getrennt. Erst schalen (Tag 1-2), dann bewehren (Tag 3), dann betonieren (Tag 4). Sinnvoll einzeln zu erfassen.

**Bewehrung bei Decken:** Kann separat erfasst werden (eigenes Arbeitspaket) ODER mit Beton zusammen (ein Paket "Decke komplett"). Polier entscheidet.

### 6.3 Mauerwerk (pro Geschoß)

```
Arbeitspaket: "Mauerwerk 38er H5/EG"
Menge: 198 m² (aus Ziegelberechnung — schon bekannt!)

Tag 11: 3 Mann × 8h + 1 Mann × 8h (Lehrling) = 32 Ah
Tag 12: 4 Mann × 10h = 40 Ah
Tag 13: 4 Mann × 10h = 40 Ah
Tag 14: 3 Mann × 7h + 1 Mann × 6h = 27 Ah (Donnerstag kurz)
                                     ─────────────────────
                                     Gesamt: 139 Ah (?)

Korrektur: Lehrling zählt nur halb → 123 Ah
"Fertig" → 198 m² (aus Ziegelberechnung bestätigt)
Ergebnis: 198 m² ÷ 123 Ah = 1,61 m²/Ah
```

**Warum pro Geschoß:** Die m² pro Geschoß kennt der Polier aus der Ziegelberechnung. Tägliches Aufmaß ist unnötig — er weiß nicht wieviel m² am Dienstag fertig wurden, aber er weiß wann das Geschoß insgesamt fertig ist.

---

## 7. LV-Import und Verknüpfung

### 7.1 ÖNORM A 2063 (österreichischer Standard)

In Österreich regelt die ÖNORM A 2063 den Datenaustausch im AVA-Prozess (Ausschreibung, Vergabe, Abrechnung). Das Format ist XML mit definiertem Schema. Dateierweiterungen: `.ONLV` (Leistungsverzeichnis), `.ONLB` (Leistungsbeschreibung). Struktur: Hauptgruppe (HG) → Obergruppe (OG) → Leistungsgruppe (LG) → Position mit Nummer, Kurztext, Langtext, Menge, Einheit, Einheitspreis. Standardisierte Leistungsbücher: LB-HB (Hochbau), LB-HT (Haustechnik). GAEB ist das deutsche Pendant.

### 7.2 Drei Import-Wege für BPM

| Weg | Aufwand | Beschreibung |
|-----|---------|-------------|
| **Excel-Import** | Gering | LV als Excel (.xlsx) importieren — Spalten zuordnen (Position, Text, Menge, Einheit). |
| **KI-Import** | Mittel | PDF des LV an KI-API senden → JSON mit Positionen und Massen (ADR-027) |
| **ÖNORM/GAEB-Parser** | Hoch | XML-Schema parsen, nur Positionsebene. Kein voller AVA-Prozess nötig. |

**Empfehlung:** Excel + KI-API zuerst. ÖNORM-Parser optional. Die meisten Poliere bekommen das LV als Ausdruck oder PDF, nicht als ÖNORM-Datenträger.

### 7.3 LV-Position → Arbeitspaket Zuordnung

LV-Positionen können optional mit Arbeitspaketen verknüpft werden. Das ermöglicht einen Soll-Ist-Vergleich auf Positionsebene.

```
LV-Position "02.03.01 Mauerwerk 38er Plan" (850 m²)
    ├── Arbeitspaket H5/EG  → 198 m² (✅ fertig, 123 Ah, 1,61 m²/Ah)
    ├── Arbeitspaket H5/1.OG → 185 m² (🔵 in Arbeit, 64 Ah bisher)
    ├── Arbeitspaket H6/EG  → 210 m² (⬜ geplant)
    └── Arbeitspaket H6/1.OG → 195 m² (⬜ geplant)
                               ────────
                        Summe: 788 m² von 850 m² (92,7%)
```

**Praxis-Vereinfachung:** Man muss nicht jede Stunde auf eine LV-Position verteilen. Man verknüpft nur die "tragenden Positionen" (die großen Brocken). Kleinpositionen bleiben unverknüpft.

---

## 8. Nachkalkulation — Ergebnisse

### 8.1 Leistungskatalog (wächst automatisch)

| Tätigkeit | Einheit | Ø Leistung | Min | Max | Messungen | Projekte |
|-----------|---------|-----------|-----|-----|-----------|----------|
| Mauerwerk 38er | h/m² | 0,62 | 0,50 | 0,80 | 12 | 3 |
| Betonwand komplett | h/m² Sch. | 1,51 | 1,20 | 1,80 | 8 | 2 |
| Schalung Decke | h/m² | 0,38 | 0,30 | 0,50 | 8 | 2 |
| Bewehrung | h/to | 11,5 | 8,4 | 15,0 | 14 | 4 |
| Betonieren (Pumpe) | h/m³ | 0,24 | 0,14 | 0,46 | 18 | 5 |
| Kellerdeckendämmung | h/m² | 0,27 | 0,19 | 0,58 | 11 | 2 |

Kann initial aus den bestehenden Excel-Daten (Kalkulation_v2.xlsx) befüllt werden.

### 8.2 Erschwernisfaktoren

Pro Projekt konfigurierbar:

| Erschwernis | Faktor | Beispiel |
|-------------|--------|---------|
| Normal | 1,00 | Flaches Gelände, gute Zufahrt |
| Hanglage | 1,15 | +15% Zeit |
| Enge Zufahrt / Innenstadtbaustelle | 1,20 | Material weiter tragen |
| Winterarbeit | 1,25 | Aufwärmen, Abdecken, kurze Tage |
| Komplizierte Geometrie | 1,10–1,30 | Schräge Wände, Rundungen |
| Kleine Flächen / viele Abschnitte | 1,15 | Viel Rüstzeit |
| Hohe Geschosse (> 3m) | 1,10 | Gerüst, längere Wege |
| Lehrlingsanteil hoch | 1,10 | Mehr Anleitung |

### 8.3 Bauzeitprognose für neues Projekt

**Input:** Soll-Mengen der neuen Baustelle + Erfahrungswerte + Erschwernisse + Kolonnenstärke.

```
┌─────────────────────────────────────────────────────────────────┐
│ Bauzeitprognose: ÖWG Dobl-Zwaring                               │
│ Kolonne: [4] FA  ×  [8] h/Tag                                  │
│ Erschwernisse: Hanglage (1,15) ☑  Winterarbeit (1,25) ☐        │
├──────────────────┬────────┬───────┬───────┬──────┬──────────────┤
│ Position         │ Masse  │ h/EH  │ Faktor│  Ah  │ Tage         │
├──────────────────┼────────┼───────┼───────┼──────┼──────────────┤
│ Mauerwerk 38er   │ 850 m² │ 0,62  │ 1,15  │  606 │   19         │
│ Betonwand kompl. │ 420 m² │ 1,51  │ 1,15  │  729 │   23         │
│ Schalung Decke   │ 680 m² │ 0,38  │ 1,15  │  297 │    9         │
│ Bewehrung        │  45 to │ 11,5  │ 1,00  │  518 │   16         │
│ Betonieren       │ 320 m³ │ 0,24  │ 1,00  │   77 │    2         │
├──────────────────┼────────┼───────┼───────┼──────┼──────────────┤
│ Gesamt           │        │       │       │ 2.227│  ~70 Tage    │
│ + 10% Puffer     │        │       │       │      │  ~77 Tage    │
│                  │        │       │       │      │  = 15 Wochen │
└──────────────────┴────────┴───────┴───────┴──────┴──────────────┘
│ [LV importieren]  [Erschwernisse]  [PDF Export]                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 9. Braucht man KI?

Für die reine Berechnung: **Nein.** Soll-Menge × Erfahrungswert × Erschwernis ist Multiplikation.

**KI wird interessant für:**
- LV-Positionen automatisch den richtigen Leistungswerten zuordnen (verschiedene LV-Formulierungen → gleiche Tätigkeit)
- Erschwernisse aus der Baubeschreibung automatisch erkennen
- Plausibilitätsprüfung ("70 Tage Rohbau bei 4 Mann — das passt")
- Vergleich mit Branchenwerten wenn eigene Daten noch dünn sind
- LV-Import aus PDF per KI-API (ADR-027)
- Bautagebuch-Text automatisch formulieren aus Stichpunkten

---

## 10. Bestehende Architektur — Erweiterung, kein Umbau

### 10.1 Was schon da ist (unverändert)

```
projects            → Projekt-Kopf           ✅ bleibt
building_parts      → Bauteile (H5, H6...)   ✅ bleibt
building_levels     → Geschoße (KG, EG...)   ✅ bleibt
project_participants→ Beteiligte Firmen       ✅ bleibt
clients             → Auftraggeber           ✅ bleibt
```

### 10.2 Was neu dazukommt (nur neue Tabellen)

```
employees           → Mitarbeiter                NEU
work_packages       → Arbeitspakete              NEU ← ZENTRAL
lv_positions        → LV-Positionen (importiert) NEU
work_assignments    → Arbeitseinteilung (tägl.)  NEU
time_entries        → Zeiterfassung (tägl.)      NEU
performance_catalog → Erfahrungswerte            NEU
project_difficulty  → Erschwernisfaktoren        NEU
```

### 10.3 Verbindung über Foreign Keys

```
Bestehend                          Neu
─────────                          ───
building_parts ←──FK──── work_packages.building_part_id
building_levels ←──FK──── work_packages.level_id
projects ←──FK──── work_packages.project_id
                          │
                          ├──FK──── work_assignments.work_package_id
                          ├──FK──── lv_positions.project_id
                          └──FK──── time_entries.project_id
```

### 10.4 Solution-Struktur (unveränderte Architektur)

```
BauProjektManager.Domain           → WorkPackage.cs, Employee.cs (NEU)
BauProjektManager.Infrastructure   → DB Schema v1.6+ (neue Tabellen)
BauProjektManager.Kalkulation      → Neues WPF Class Library Projekt
```

Dependency-Regel bleibt gleich: Kalkulation → Domain + Infrastructure. Kein bestehendes Projekt wird geändert.

---

## 11. DB-Schema (Entwurf)

```sql
-- Mitarbeiter (auch für Zeiterfassung relevant)
CREATE TABLE employees (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    short_name TEXT,                -- "Biskup D."
    qualification TEXT,             -- "Facharbeiter" / "Lehrling" / "Polier"
    hourly_rate REAL,               -- Bruttomittellohn
    active INTEGER DEFAULT 1,
    notes TEXT
);

-- Arbeitspakete — DIE ZENTRALE TABELLE
CREATE TABLE work_packages (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    project_id TEXT NOT NULL,
    building_part_id TEXT,           -- FK → building_parts (H5)
    level_id TEXT,                   -- FK → building_levels (EG)
    activity TEXT NOT NULL,          -- "Mauerwerk 38er"
    lv_position_id INTEGER,         -- FK → lv_positions (optional)
    planned_quantity REAL,           -- 198 (Soll-Menge)
    unit TEXT NOT NULL,              -- "m²"
    source TEXT,                     -- "Ziegelberechnung" / "DokaCad" / "LV" / "Manuell"
    track_separately INTEGER DEFAULT 0, -- 1 = Einzelschritte, 0 = Gesamtpaket
    color TEXT,                      -- Farbcode für Arbeitseinteilung
    sort_order INTEGER,

    -- Status (wird automatisch aktualisiert)
    status TEXT DEFAULT 'planned',   -- planned / in_progress / completed
    started_at TEXT,
    completed_at TEXT,

    -- Ist-Werte (automatisch berechnet)
    actual_hours REAL DEFAULT 0,
    actual_quantity REAL,
    performance_value REAL,          -- m²/Ah oder h/m² (berechnet)
    notes TEXT,

    FOREIGN KEY (project_id) REFERENCES projects(id),
    FOREIGN KEY (building_part_id) REFERENCES building_parts(id),
    FOREIGN KEY (level_id) REFERENCES building_levels(id),
    FOREIGN KEY (lv_position_id) REFERENCES lv_positions(id)
);

-- LV-Positionen (importiert aus LV)
CREATE TABLE lv_positions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    project_id TEXT NOT NULL,
    position_number TEXT NOT NULL,   -- "02.03.01"
    short_text TEXT NOT NULL,        -- "Mauerwerk 38er Plan"
    quantity REAL,                   -- 850 (Gesamt-Soll aus LV)
    unit TEXT,                       -- "m²"
    unit_price REAL,                 -- optional (für Kostenvergleich)
    completed_quantity REAL DEFAULT 0, -- Summe aus verknüpften Arbeitspaketen
    FOREIGN KEY (project_id) REFERENCES projects(id)
);

-- Arbeitseinteilung (täglich: wer arbeitet an welchem Paket)
CREATE TABLE work_assignments (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    date TEXT NOT NULL,               -- "2025-11-05"
    employee_id INTEGER NOT NULL,     -- FK → employees
    work_package_id INTEGER NOT NULL, -- FK → work_packages
    hours REAL,                       -- aus time_entries (kann manuell überschrieben werden)
    notes TEXT,
    FOREIGN KEY (employee_id) REFERENCES employees(id),
    FOREIGN KEY (work_package_id) REFERENCES work_packages(id)
);

-- Zeiterfassung (täglich: wer war da, wieviele Stunden)
CREATE TABLE time_entries (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    date TEXT NOT NULL,
    employee_id INTEGER NOT NULL,
    project_id TEXT NOT NULL,
    hours REAL NOT NULL,              -- 8.0
    absence_type TEXT,                -- NULL / "U" / "K" / "Kranfahrer" / "Sonstiges"
    notes TEXT,
    FOREIGN KEY (employee_id) REFERENCES employees(id),
    FOREIGN KEY (project_id) REFERENCES projects(id)
);

-- Erfahrungswerte (Leistungskatalog)
CREATE TABLE performance_catalog (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    activity TEXT NOT NULL,           -- "Mauerwerk 38er"
    unit TEXT NOT NULL,               -- "m²"
    hours_per_unit REAL NOT NULL,     -- 0.62
    project_id TEXT,                  -- Quelle
    work_package_id INTEGER,          -- Quelle
    measured_at TEXT,
    quantity REAL,
    total_hours REAL,
    workers INTEGER,
    notes TEXT,
    FOREIGN KEY (project_id) REFERENCES projects(id)
);

-- Erschwernisfaktoren pro Projekt
CREATE TABLE project_difficulty (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    project_id TEXT NOT NULL,
    factor_name TEXT NOT NULL,        -- "Hanglage"
    factor_value REAL NOT NULL,       -- 1.15
    FOREIGN KEY (project_id) REFERENCES projects(id)
);
```

---

## 12. GUI-Mockups

### 12.1 Arbeitspakete pro Projekt (Baudaten-Übersicht)

```
┌─────────────────────────────────────────────────────────────────┐
│ Arbeitspakete: ÖWG Dobl — Haus 5                                │
│                                                                  │
│ [Import: Ziegel-Excel]  [Import: LV]  [+ Manuell hinzufügen]   │
├────┬────────────────┬───────┬──────┬────────┬──────┬────────────┤
│ Ge │ Tätigkeit      │ Soll  │ EH   │ Status │  Ah  │ Leistung   │
├────┼────────────────┼───────┼──────┼────────┼──────┼────────────┤
│ EG │ Mauerwerk 38er │  198  │ m²   │ ✅ Fert│  123 │ 1,61 m²/Ah │
│ EG │ Wand komplett  │   85  │ m²Sch│ ✅ Fert│  128 │ 0,66 m²/Ah │
│ EG │ Decke Schalung │  170  │ m²   │ 🔵 Läuf│   64 │    —       │
│ EG │ Decke Bewehrung│  6,4  │ to   │ ⬜ Offen│    — │    —       │
│ EG │ Decke Beton    │   34  │ m³   │ ⬜ Offen│    — │    —       │
│1.OG│ Mauerwerk 38er │  185  │ m²   │ ⬜ Offen│    — │    —       │
│1.OG│ Wand komplett  │   72  │ m²Sch│ ⬜ Offen│    — │    —       │
│1.OG│ Decke Schalung │  165  │ m²   │ ⬜ Offen│    — │    —       │
├────┴────────────────┴───────┴──────┴────────┴──────┴────────────┤
│ Gesamt: 8 Pakete, 2 fertig, 1 in Arbeit, 5 offen               │
└─────────────────────────────────────────────────────────────────┘
```

### 12.2 Leistungskatalog

```
┌─────────────────────────────────────────────────────────────────┐
│ Leistungskatalog — Meine Erfahrungswerte        [Excel-Import] │
├────────────────┬────────┬───────┬──────┬──────┬────────┬────────┤
│ Tätigkeit      │Einheit │  Ø    │ Min  │ Max  │Messung.│Projekte│
├────────────────┼────────┼───────┼──────┼──────┼────────┼────────┤
│ Mauerwerk 38er │ h/m²   │ 0,62  │ 0,50 │ 0,80 │   12   │   3    │
│ Wand komplett  │ h/m²Sch│ 1,51  │ 1,20 │ 1,80 │    8   │   2    │
│ Schalung Decke │ h/m²   │ 0,38  │ 0,30 │ 0,50 │    8   │   2    │
│ Bewehrung      │ h/to   │ 11,5  │  8,4 │ 15,0 │   14   │   4    │
│ Betonieren     │ h/m³   │ 0,24  │ 0,14 │ 0,46 │   18   │   5    │
│ Kellerdecken-  │ h/m²   │ 0,27  │ 0,19 │ 0,58 │   11   │   2    │
│ dämmung        │        │       │      │      │        │        │
├────────────────┴────────┴───────┴──────┴──────┴────────┴────────┤
│ 📊 6 Tätigkeiten, 71 Messungen, 18 Projekte                    │
└─────────────────────────────────────────────────────────────────┘
```

### 12.3 Fertigmeldung

```
┌─────────────────────────────────────────────────────────────────┐
│ ✅ Arbeitspaket abschließen                                     │
│                                                                  │
│ Paket: Mauerwerk 38er — H5 / EG                                │
│                                                                  │
│ Soll-Menge:    198 m²  (aus Ziegelberechnung)                   │
│ Ist-Menge:     [198] m²  ← bestätigen oder korrigieren         │
│                                                                  │
│ Zeitraum:      11.03. — 14.03.2025 (4 Tage)                    │
│ Arbeitsstunden: 123 Ah (automatisch aus Arbeitseinteilung)      │
│                                                                  │
│ ──────────────────────────────────────────────                   │
│ Ergebnis:      198 m² ÷ 123 Ah = 1,61 m²/Ah                   │
│                123 Ah ÷ 198 m² = 0,62 h/m²                     │
│ ──────────────────────────────────────────────                   │
│                                                                  │
│ Bemerkungen:   [________________________________]               │
│                                                                  │
│ [Abschließen & in Leistungskatalog übernehmen]    [Abbrechen]   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 13. Bestehende Excel-Tools als Datenquelle

### 13.1 Kalkulation_v2.xlsx — Migration

Die bestehenden Messdaten (14 Leistungsgruppen, echte Projekte wie ÖWG Deutschfeistritz, AVL P1, ENW Martinsgasse) können in den Leistungskatalog importiert werden. Das gibt sofort einen Grundstock an Erfahrungswerten.

**Vorhandene Leistungsgruppen:**
- 03 Leitungsgräben, 06 Schächte
- 07 Betonierarbeiten, Stahlbetonbewehrung, Wände/Stützen schalen
- 08 Mantelbetonsteine
- 09 Dämmung, Türelemente
- 11 Meterriss
- 12 Waagrechte Abdichtungen
- 13 Randbegrenzungen, Betonsteinpflaster
- 16 Elementdecken, Hohlwände

### 13.2 Ziegelberechnung — Verbindung zu BPM

Die BT-Sheets der Ziegelberechnung (RDOK, FFOK, RDUK, RBH, Thermofuß) haben identische Struktur wie BPM Tab 2 Bauwerk (BuildingPart/BuildingLevel). Die m² Mauerwerk pro BT+Geschoß können direkt als Soll-Mengen in Arbeitspakete fließen.

Langfristig könnte die Ziegelberechnung (Scharrenrechner, Produkt-DB, Wandcode-System, Bestellverwaltung) als eigenes BPM-Submodul integriert werden.

### 13.3 Arbeitseinteilung (Excel)

Das bestehende Excel-Blatt (Matrix Arbeiter × Tätigkeiten, "x" zuweisen, U/K markiert) ist das direkte Vorbild für die BPM-Arbeitseinteilung. Die Überladung der Excel (Zeiterfassung + Einteilung + Leistungserfassung in einer Datei) wird in BPM aufgelöst: jedes Thema wird ein eigenes Modul, aber sie greifen auf die gleiche Datenbank zu.

---

## 14. Abgrenzung

**Dieses Modul ist NICHT:**
- Ein vollständiges Kalkulationsprogramm (ersetzt keine ABK, keine AUER-Kalkulation)
- Ein Angebotskalkulationstool (keine Materialpreise, keine Gemeinkosten, kein K7-Blatt)
- Ein Bauzeitplan-Tool (kein Gantt-Chart, keine Abhängigkeiten zwischen Vorgängen)
- Ein voller AVA-Prozess (keine Rechnungslegung, kein Preisspiegel)

**Dieses Modul IST:**
- Ein Erfahrungswerte-Sammler ("wie schnell arbeitet MEINE Kolonne?")
- Ein Bauzeitschätzer ("wie lange brauche ich für die nächste Baustelle?")
- Ein Soll-Ist-Vergleich auf Bauteil-Ebene ("liegen wir im Plan?")
- Ein Werkzeug für den Polier, nicht für den Kalkulanten

---

## 15. Implementierungsreihenfolge

| Phase | Was | Abhängigkeit |
|-------|-----|-------------|
| 1 | **PlanManager fertig** (V1 Must) | — |
| 2 | **Zeiterfassung** (employees + time_entries) | PlanManager fertig |
| 3 | **Arbeitseinteilung** (work_assignments, tägliche Matrix) | Zeiterfassung |
| 4 | **Arbeitspakete** (work_packages, Soll-Mengen erfassen) | Arbeitseinteilung |
| 5 | **Bautagebuch** mit Auto-Vorschlag aus Einteilung | Arbeitspakete |
| 6 | **Fertigmeldung + Nachkalkulation** (Leistungskatalog befüllen) | Arbeitspakete |
| 7 | **Migration** bestehender Excel-Daten in den Katalog | Leistungskatalog |
| 8 | **LV-Import** per Excel oder KI-API | KI-Import (ADR-027) |
| 9 | **Bauzeitrechner** — Soll-Mengen × Katalog × Erschwernisse | LV-Import + Katalog |
| 10 | **KI-Zuordnung** — LV-Positionen automatisch matchen | KI-Assistent |

---

*Kernfrage: "Brauche ich das um Pläne zu sortieren?" — Nein. Deshalb Won't have V1.*  
*Aber: Dieses Modul ist langfristig der größte Mehrwert von BPM — es macht den Polier zum datengetriebenen Bauleiter.*