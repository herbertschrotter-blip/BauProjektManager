# BauProjektManager — Modul: Kalkulation & Bauzeitprognose

**Erstellt:** 29.03.2026  
**Status:** Konzept (Won't have V1)  
**Abhängigkeiten:** Bautagebuch-Modul, Zeiterfassung  
**Vorbilder:** Herberts Excel-Tools `Kalkulation_v2.xlsx` + `Ziegelberechnung_ESS_St__Georgen.xlsm`

---

## 1. Ziel

Aus eigenen Erfahrungswerten (gesammelt im Bautagebuch) die Bauzeit für neue Projekte schätzen. Keine Tabellenbuch-Werte, keine Normen — echte Messdaten von echten Baustellen mit echten Mannschaften.

**Kernformel:**
```
LV-Masse × Erfahrungswert × Erschwernis = geschätzte Arbeitsstunden
```

**Beispiel:**
```
850 m² Mauerwerk 38er × 0,62 h/m² × 1,15 (Hanglage) = 606 Arbeitsstunden
÷ 4 Mann × 8h = 32 Ah/Tag → 19 Arbeitstage
```

---

## 2. Bestehende Excel-Tools (Analyse)

### 2.1 Kalkulation_v2.xlsx

Nachkalkulations-Datenbank mit echten Messdaten. 44 Sheets, strukturiert nach LB-H Leistungsgruppen.

**Aufbau pro Leistungsgruppe:**
- `XX_Name_Tab` — Messdaten-Tabelle (Rohdaten von der Baustelle)
- `XX_Name_Ber` — Berechnungs-Blatt (Durchschnittswerte)

**Spalten der Tab-Blätter (40+ Spalten):**
- Datum, Bauvorhaben, Objekt, Bauteil
- System, Hersteller, Produkt
- Maße (l, b, h, t, d, m², m³, kg, Stk, m1)
- Facharbeiter (FA), Stunden (h), Leistungsstunden (LH)
- **EH/LH** (Einheit pro Leistungsstunde) — die zentrale Kennzahl
- **LH/EH** (Leistungsstunden pro Einheit) — Umkehrwert
- Anteil Lohnkosten € / EH
- Arbeitsschritt, Maschinen (1-3), Werkzeug (1-3)
- Bemerkungen

**Vorhandene Leistungsgruppen mit echten Daten:**
- 03 Leitungsgräben (ÖWG Deutschfeistritz, PTR Bad Radkersburg)
- 06 Schächte (ÖWG Deutschfeistritz)
- 07 Betonierarbeiten (ÖWG Deutschfeistritz, AVL P1, ENW Martinsgasse)
- 07 Stahlbetonbewehrung (ÖWG Deutschfeistritz, AVL P1, ENW Martinsgasse)
- 07 Wände/Stützen schalen (ENW Martinsgasse)
- 08 Mantelbetonsteine (ÖWG Deutschfeistritz)
- 09 Dämmung (ÖWG Deutschfeistritz, AVL P1)
- 09 Türelemente (ÖWG Heiligenkreuz, Deutschfeistritz)
- 11 Meterriss (ÖWG Flurgasse)
- 12 Waagrechte Abdichtungen (ÖWG Deutschfeistritz, Heiligenkreuz)
- 13 Randbegrenzungen (ÖWG Heiligenkreuz, ETH Bad Radkersburg)
- 13 Betonsteinpflaster (ETH Bad Radkersburg)
- 16 Elementdecken (AVL P1, ÖWG Deutschfeistritz)
- 16 Hohlwände (AVL P1)

**Beispiel-Ergebnisse aus der Excel:**
- Betonieren Bodenplatte: Ø 4,91 m³/LH (Pumpe vs. Kran unterschiedlich)
- Bewehrung Bodenplatte: 91,2 kg/LH
- Schalung Wände/Stützen: 3,76 m²/LH
- Kellerdeckendämmung: 4,69 m²/LH (variiert nach Raumhöhe und Montage)
- Elementdecken verlegen: 11,71 m²/LH

**Problem:** 40+ Spalten pro Zeile → zu aufwändig für tägliche Erfassung auf der Baustelle.

### 2.2 Ziegelberechnung_ESS_St__Georgen.xlsm

Ziegelbedarfsrechner mit VBA. 34 Sheets. Für ein konkretes Projekt (ESS St. Georgen).

**Features:**
- **Scharrenrechner:** Berechnet Kombination Normal-Scharren (24,9 cm) + Höhenausgleich-Scharren (21,9 cm) für eine gegebene Raumhöhe
- **Produkt-DB:** Komplette Wienerberger-Formatbibliothek (50er, 44er, 38er, 25er; Plan/Objekt Plan/EFH; mit Maßen, Gewichten, Stk/Pal, Stk/m²)
- **Wandcode-System:** "5-0-0-1" = Haus 5, Geschoß 0, Top 0, Wand 1
- **CSV-Import:** Plananmerkungen (Wandcode, Format, Länge, Fenster/Türen) aus PDF-Viewer exportiert
- **Fenster-Abzüge:** Typliste mit b, h, Rohbauhöhe (RPH), m² — automatischer Abzug von Wandfläche
- **Flächenberechnung:** Pro Wand: Länge × Raumhöhe - Fenster/Türen = Netto-Mauerwerksfläche
- **BT-Sheets (BT1-BT6):** Pro Bauteil/Haus: RDOK, FFOK, RDUK, RBH, Decke, Bodenaufbau, Thermofuß — **identische Struktur wie BPM Tab 2 Bauwerk!**
- **Bestellverwaltung:** Geplant vs. bestellt vs. geliefert → noch zu liefern (pro Format + Überlager)
- **Nachkalkulation:** m² gemauert ÷ Arbeitsstunden = m²/Ah. Ergebnis: **1,61 m²/Ah für 38er Ziegel** (ESS St. Georgen, Haus 5, EG, 198 m², 123 Ah, 4 Tage)

**Verbindung zu BPM:** Die Geschoss-Daten aus BPM Tab 2 (RDOK/FBOK/RDUK, BuildingPart/BuildingLevel) könnten direkt in eine Ziegelberechnung einfließen. Das Datenmodell existiert bereits.

---

## 3. BPM-Ansatz: Vereinfacht + Automatisch

### 3.1 Grundidee

Statt 40 Spalten pro Messung → **5 Felder im Bautagebuch:**

1. **Was** wurde gemacht (Dropdown: Mauerwerk, Schalung, Bewehrung, Betonieren...)
2. **Wieviel** (Menge + Einheit: 45 m², 12 m³, 3.5 to)
3. **Wer** (Anzahl Arbeitskräfte — kommt aus Zeiterfassung)
4. **Wie lange** (Stunden — kommt aus Zeiterfassung)
5. **Besonderheiten** (optional: enger Raum, Lehrling, Regen)

→ BPM rechnet automatisch: 45 m² ÷ (3 Mann × 8h) = **1,88 m²/Ah**

### 3.2 Vier Datenquellen

| Quelle | Liefert | Modul |
|--------|---------|-------|
| **Bautagebuch** | Tätigkeit + Menge pro Tag | Bautagebuch-Modul |
| **Zeiterfassung** | Wer war da, wie viele Stunden | Zeiterfassungs-Modul |
| **Leistungskatalog** | Erfahrungswerte (h/Einheit pro Tätigkeit) | Kalkulation (NEU) |
| **LV der neuen Baustelle** | Positionen + Massen | KI-Import oder Excel |

### 3.3 Leistungskatalog (automatisch befüllt)

Wächst mit jedem Projekt. Struktur:

| Tätigkeit | Einheit | Ø Leistung | Min | Max | Messungen | Projekte |
|-----------|---------|-----------|-----|-----|-----------|----------|
| Mauerwerk 38er | h/m² | 0,62 | 0,50 | 0,80 | 12 | 3 |
| Schalung Wand | h/m² | 0,27 | 0,22 | 0,35 | 8 | 2 |
| Bewehrung | h/to | 11,5 | 8,4 | 15,0 | 14 | 4 |
| Betonieren (Pumpe) | h/m³ | 0,24 | 0,14 | 0,46 | 18 | 5 |
| Kellerdeckendämmung | h/m² | 0,27 | 0,19 | 0,58 | 11 | 2 |

Kann auch initial aus den bestehenden Excel-Daten befüllt werden (Migration).

### 3.4 Erschwernisfaktoren

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

### 3.5 Bauzeitprognose

**Input:**
- LV-Positionen mit Massen (importiert per Excel oder KI-API)
- Geplante Kolonnen-Stärke (z.B. 4 FA Rohbau)
- Erschwernisse für das neue Projekt

**Berechnung pro Position:**
```
Masse × Erfahrungswert × Erschwernis = Arbeitsstunden
Arbeitsstunden ÷ (Kolonne × Tagesstunden) = Arbeitstage
```

**Output:**
```
Mauerwerk gesamt:    26 Tage
Schalung + Beton:    18 Tage
Bewehrung:           12 Tage
Sonstiges:            8 Tage
────────────────────────────
Rohbau geschätzt:   ~64 Arbeitstage
+ Puffer 10%:       ~70 Arbeitstage
÷ 5 Tage/Woche:    ~14 Wochen
```

---

## 4. Braucht man KI?

Für die reine Berechnung: **Nein.** LV × Erfahrungswert × Erschwernis ist Multiplikation.

**KI wird interessant für:**
- LV-Positionen automatisch den richtigen Leistungswerten zuordnen (verschiedene LV-Formulierungen → gleiche Tätigkeit)
- Erschwernisse aus der Baubeschreibung automatisch erkennen
- Plausibilitätsprüfung ("26 Tage für 850 m² bei 4 Mann — das passt")
- Vergleich mit Branchenwerten wenn eigene Daten noch dünn sind
- LV-Import aus PDF per KI-API (ADR-027)

---

## 5. DB-Schema (Entwurf)

```sql
-- Leistungskatalog: Erfahrungswerte
CREATE TABLE performance_catalog (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    activity TEXT NOT NULL,          -- "Mauerwerk 38er"
    unit TEXT NOT NULL,              -- "m²"
    hours_per_unit REAL NOT NULL,    -- 0.62
    project_id TEXT,                 -- Quelle (proj_001)
    measured_at TEXT,                -- Datum der Messung
    quantity REAL,                   -- gemessene Menge
    workers INTEGER,                 -- Anzahl Arbeitskräfte
    total_hours REAL,                -- Gesamt-Arbeitsstunden
    notes TEXT                       -- Besonderheiten
);

-- Erschwernisfaktoren pro Projekt
CREATE TABLE project_difficulty (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    project_id TEXT NOT NULL,
    factor_name TEXT NOT NULL,       -- "Hanglage"
    factor_value REAL NOT NULL,      -- 1.15
    FOREIGN KEY (project_id) REFERENCES projects(id)
);

-- Bauzeitprognose (gespeicherte Schätzungen)
CREATE TABLE time_estimates (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    project_id TEXT NOT NULL,
    activity TEXT NOT NULL,
    lv_quantity REAL NOT NULL,       -- Masse aus LV
    estimated_hpu REAL NOT NULL,     -- verwendeter h/Einheit-Wert
    difficulty_factor REAL DEFAULT 1.0,
    estimated_hours REAL NOT NULL,   -- Ergebnis
    crew_size INTEGER,
    estimated_days REAL,
    created_at TEXT,
    FOREIGN KEY (project_id) REFERENCES projects(id)
);
```

---

## 6. Implementierungsreihenfolge

| Phase | Was | Abhängigkeit |
|-------|-----|-------------|
| 1 | **PlanManager fertig** (V1 Must) | — |
| 2 | **Bautagebuch** mit Leistungserfassung (Tätigkeit + Menge) | PlanManager fertig |
| 3 | **Leistungskatalog** automatisch aus Bautagebuch befüllen | Bautagebuch |
| 4 | **Migration** bestehender Excel-Daten in den Katalog | Leistungskatalog |
| 5 | **LV-Import** per Excel oder KI-API | KI-Import (ADR-027) |
| 6 | **Bauzeitrechner** — LV × Katalog × Erschwernisse = Prognose | LV-Import + Katalog |
| 7 | **KI-Zuordnung** — LV-Positionen automatisch matchen | KI-Assistent |

---

## 7. GUI-Skizze

### 7.1 Leistungskatalog (Einstellungen oder eigene Seite)

```
┌─────────────────────────────────────────────────────┐
│ Leistungskatalog                           [Import] │
├──────────────┬────────┬───────┬─────┬─────┬─────────┤
│ Tätigkeit    │Einheit │  Ø    │ Min │ Max │Messungen│
├──────────────┼────────┼───────┼─────┼─────┼─────────┤
│ Mauerwerk 38 │ h/m²   │ 0,62  │0,50 │0,80 │   12    │
│ Schalung Wand│ h/m²   │ 0,27  │0,22 │0,35 │    8    │
│ Bewehrung    │ h/to   │ 11,5  │ 8,4 │15,0 │   14    │
│ Betonieren   │ h/m³   │ 0,24  │0,14 │0,46 │   18    │
└──────────────┴────────┴───────┴─────┴─────┴─────────┘
```

### 7.2 Bauzeitprognose (pro Projekt)

```
┌─────────────────────────────────────────────────────┐
│ Bauzeitprognose: ÖWG Dobl-Zwaring                   │
│ Kolonne: [4] FA  ×  [8] h/Tag                      │
│ Erschwernisse: Hanglage (1,15) ☑                    │
├──────────────┬────────┬───────┬───────┬─────────────┤
│ Position     │ Masse  │ h/EH  │ Faktor│ Tage        │
├──────────────┼────────┼───────┼───────┼─────────────┤
│ Mauerwerk 38 │ 850 m² │ 0,62  │ 1,15  │   19        │
│ Schalung     │ 420 m² │ 0,27  │ 1,15  │    4        │
│ Bewehrung    │  45 to │ 11,5  │ 1,00  │   16        │
│ Betonieren   │ 320 m³ │ 0,24  │ 1,00  │    2        │
├──────────────┼────────┼───────┼───────┼─────────────┤
│ Gesamt       │        │       │       │ ~41 Tage    │
│ + 10% Puffer │        │       │       │ ~45 Tage    │
│              │        │       │       │ = 9 Wochen  │
└──────────────┴────────┴───────┴───────┴─────────────┘
│ [LV importieren]  [Erschwernisse]  [PDF Export]     │
└─────────────────────────────────────────────────────┘
```

---

## 8. Verbindung zu anderen Modulen

```
Bautagebuch ──→ Leistungskatalog ──→ Bauzeitprognose
     ↑                                      ↑
Zeiterfassung                          LV-Import (KI)
     ↑                                      ↑
Tab 2 Bauwerk (RDOK/RDUK) ──→ Ziegelberechnung (Zukunft)
```

- **Tab 2 Bauwerk** liefert Geschoss-Daten (RDOK, FBOK, RDUK) → Raumhöhe für Scharrenrechner
- **Bautagebuch** liefert Tätigkeit + Menge → automatische Messwerte
- **Zeiterfassung** liefert Arbeitskräfte + Stunden → Leistungsstunden (LH)
- **KI-Import** kann LV aus PDF extrahieren (ADR-027)
- **Ziegelberechnung** könnte als Spezialmodul die Produkt-DB + Scharrenrechner integrieren

---

## 9. Abgrenzung

**Dieses Modul ist NICHT:**
- Ein vollständiges Kalkulationsprogramm (ersetzt keine ABK, keine AUER-Kalkulation)
- Ein Angebotskalkulationstool (keine Materialpreise, keine Gemeinkosten)
- Ein Bauzeitplan-Tool (kein Gantt-Chart, keine Abhängigkeiten)

**Dieses Modul IST:**
- Ein Erfahrungswerte-Sammler (wie schnell arbeitet MEINE Kolonne?)
- Ein Bauzeitschätzer (wie lange brauche ich für die nächste Baustelle?)
- Ein Werkzeug für den Polier, nicht für den Kalkulanten

---

*Kernfrage: "Brauche ich das um Pläne zu sortieren?" — Nein. Deshalb Won't have V1.*
