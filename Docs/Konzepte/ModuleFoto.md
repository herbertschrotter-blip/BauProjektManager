# BauProjektManager — Konzept: Foto-Management

**Erstellt:** 27.03.2026  
**Aktualisiert:** 29.03.2026  
**Version:** 0.2 (erweitert mit PhotoFolder V2 Erfahrungen)  
**Status:** Nach V1 — Prio 1 der Nach-V1-Module  
**Abhängigkeit:** BPM Settings (Projekt-Pfade), optional Bautagebuch-Modul  
**Referenz-Implementation:** [PhotoFolder V2](https://github.com/herbertschrotter-blip/03_Foto-Viewer-V2) (PowerShell, Branch `dev`)

---

## 1. Zielsetzung

### Problem

Baustellenfotos liegen unsortiert in Projektordnern oder auf dem Handy. Um ein bestimmtes Foto zu finden, muss Herbert den Explorer öffnen, durch Datumsordner klicken, große Dateien als Vorschau laden. Für das Bautagebuch braucht er bestimmte Fotos eines Tages — manuelles Durchsuchen kostet Zeit.

### Lösung

Ein WPF-Modul direkt im BauProjektManager (kein separater Server, kein Browser). Zeigt Baustellenfotos als Thumbnail-Grid an, mit Filtern nach Datum/Projekt/Tags, Vollbild-Viewer, und direkter Bautagebuch-Integration.

### Warum kein Server wie in PhotoFolder V2?

PhotoFolder V2 läuft als lokaler HTTP-Server mit Web-UI im Browser. Das war nötig weil PowerShell keine native GUI hat. BPM hat WPF — der Viewer läuft direkt in der App. Kein Server-Start, kein Browser, kein Port.

**Was von PhotoFolder V2 übernommen wird:** Die Logik (Scanner, Thumbnails, Caching, Video-Handling, EXIF). **Was wegfällt:** HTTP-Server, HTML/JS/CSS Frontend, Runspace-Pool, HLS-Streaming.

---

## 2. Architektur

### 2.1 Integration in BPM

```
BauProjektManager.exe
│
├── BauProjektManager.Foto/              ← Neues WPF Class Library Projekt
│   ├── ViewModels/
│   │   ├── FotoViewModel.cs             ← Hauptansicht
│   │   ├── FotoViewerViewModel.cs       ← Vollbild-Viewer
│   │   └── FotoFilterViewModel.cs       ← Filter/Suche
│   ├── Views/
│   │   ├── FotoView.xaml                ← Thumbnail-Grid
│   │   ├── FotoViewerDialog.xaml        ← Vollbild mit Navigation
│   │   └── FotoFilterPanel.xaml         ← Seitenleiste Filter
│   └── Services/
│       ├── MediaScannerService.cs       ← Ordner-Scan (aus PhotoFolder)
│       ├── ThumbnailService.cs          ← Thumbnail-Generierung + Cache
│       ├── ExifService.cs               ← EXIF-Daten lesen (Datum, GPS)
│       ├── VideoThumbnailService.cs     ← Video-Thumbnails (FFmpeg)
│       └── GeoMatchService.cs           ← Foto-GPS vs. Projekt-Koordinaten
│
└── BauProjektManager.Infrastructure/
    └── Media/
        └── FFmpegWrapper.cs             ← FFmpeg Process-Wrapper (geteilt)
```

**Dependency-Regel:** Foto → Domain + Infrastructure (wie alle Feature-Module).

### 2.2 Kein Server — direkt WPF

| PhotoFolder V2 (PowerShell) | BPM Foto-Modul (C#/WPF) |
|----------------------------|-------------------------|
| HttpListener auf Port 8888 | Kein Server — WPF Controls direkt |
| HTML/JS/CSS im Browser | XAML Views in der App |
| REST API für Thumbnails | ThumbnailService liefert BitmapImage |
| Runspace Pool für Parallelität | Task Parallel Library (async/await) |
| Route-Matching via Regex | MVVM Navigation (wie Settings/PlanManager) |
| Frontend JavaScript (~68KB) | WPF Bindings + Commands |

---

## 3. Features (aus PhotoFolder V2 übernommen + BPM-spezifisch)

### 3.1 Kern-Features (V1 des Moduls)

| Feature | Quelle | Beschreibung |
|---------|--------|-------------|
| **Thumbnail-Grid** | PhotoFolder | Fotos als Raster, einstellbare Größe und Spalten |
| **Ordner-Scan** | PhotoFolder | Rekursiv, konfigurierbare Dateiendungen |
| **Thumbnail-Caching** | PhotoFolder | Hash-basiert (Pfad + LastWriteTimeUtc), on-demand |
| **Vollbild-Viewer** | PhotoFolder | Zoom, Navigation (Pfeiltasten), EXIF-Overlay |
| **Projekt-Filter** | BPM-spezifisch | Fotos nach BPM-Projekt filtern (Pfad aus bpm.db) |
| **Datum-Filter** | BPM-spezifisch | Kalender-Navigation, Fotos eines bestimmten Tages |
| **Bautagebuch-Auswahl** | BPM-spezifisch | Checkbox pro Foto → "Ins Bautagebuch übernehmen" |
| **EXIF-Daten** | PhotoFolder | Aufnahmedatum, GPS-Koordinaten, Kamera-Info |
| **Keyboard-Navigation** | PhotoFolder | Pfeiltasten, Enter (Vollbild), Delete, Escape |
| **Light/Dark Theme** | PhotoFolder | Übernimmt BPM Dark Theme |

### 3.2 Erweiterte Features (später)

| Feature | Quelle | Beschreibung |
|---------|--------|-------------|
| **Tag-System** | BPM-spezifisch | Schlagwörter pro Foto (Schalung, Bewehrung, Mangel) |
| **Bewertung (Sterne)** | PhotoFolder (SQLite Stufe 3) | 1–5 Sterne, Filtern/Sortieren |
| **Geodaten-Prüfung** | BPM-spezifisch | Foto-GPS vs. Projekt-Koordinaten → Falschzuordnung erkennen |
| **Auto-Tagging** | BPM-spezifisch | Tags aus Bautagebuch-Text vorschlagen |
| **Video-Thumbnails** | PhotoFolder | FFmpeg-basiert, animierte GIF-Previews |
| **Video-Wiedergabe** | PhotoFolder | MediaElement in WPF (kein HLS nötig, direkt abspielen) |
| **Duplikat-Finder** | PhotoFolder | Nach Dateigröße/Hash, Vorschlag zum Löschen |
| **Ordner-Statistik** | PhotoFolder | Dateianzahl, Größe, Typen pro Projekt |

### 3.3 Features die NICHT übernommen werden

| Feature | Warum nicht |
|---------|-----------|
| HTTP-Server + Web-UI | Nicht nötig — WPF ist die UI |
| HLS-Streaming | WPF MediaElement spielt Videos direkt ab |
| Datei-Sortierer (FileSorter) | Eigenständiges Tool, bleibt in PhotoFolder |
| Flatten & Move | Eigenständiges Tool, bleibt in PhotoFolder |
| Archiv-Extraktion (ZIP/RAR) | Nicht relevant für Baustellenfotos |
| Settings via Web-UI | BPM Einstellungen-Seite |

---

## 4. Thumbnail-System (aus PhotoFolder V2)

### 4.1 Hash-basiertes Caching

Bewährtes Konzept aus PhotoFolder, 1:1 übernehmen:

```csharp
// Hash = SHA256(absoluterPfad + lastWriteTimeUtc)
// Wenn Quelldatei sich ändert → Hash ändert sich → neuer Thumbnail
// Kein Manifest nötig, selbst-validierend

public string GetThumbnailHash(string filePath)
{
    var info = new FileInfo(filePath);
    var input = $"{info.FullName}|{info.LastWriteTimeUtc:O}";
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)))[..16];
}
```

### 4.2 Speicherort

```
%LocalAppData%\BauProjektManager\Thumbnails\
├── a1b2c3d4e5f6.jpg    ← Thumbnail (200px breit)
├── f6e5d4c3b2a1.jpg
└── ...
```

### 4.3 Thumbnail-Generierung

| Medientyp | Methode | Paket |
|-----------|---------|-------|
| **Bilder** (JPG, PNG, WEBP) | SkiaSharp oder ImageSharp | NuGet |
| **Videos** (MP4, MOV, AVI) | FFmpeg (Frame extrahieren) | Externer Prozess |
| **Animierte GIF-Previews** | FFmpeg (erste 3 Sekunden) | Externer Prozess |

**Lesson Learned aus PhotoFolder:** On-Demand statt Pre-Generation. Thumbnails beim ersten Anzeigen erstellen, nicht beim Scan. Schnellerer Start, weniger Speicher.

### 4.4 WPF Thumbnail-Anzeige

```csharp
// Effizient: Nur Thumbnail-Größe dekodieren, nicht die volle 10MB-Datei
var bitmap = new BitmapImage();
bitmap.BeginInit();
bitmap.UriSource = new Uri(thumbnailPath);
bitmap.DecodePixelWidth = 200;  // WPF skaliert effizient
bitmap.CacheOption = BitmapCacheOption.OnLoad;
bitmap.EndInit();
bitmap.Freeze();  // Thread-safe machen
```

---

## 5. GUI-Konzept (WPF)

### 5.1 Hauptansicht

```
╔══════════════════════════════════════════════════════════════════╗
║  📷 Fotos — 202512_ÖWG-Dobl-Zwaring                           ║
╠══════════════════════════════════════════════════════════════════╣
║  ┌─ Filter ──────────┐  ┌─ Thumbnail-Grid ──────────────────┐  ║
║  │                    │  │                                    │  ║
║  │ Projekt:           │  │  ┌──────┐ ┌──────┐ ┌──────┐     │  ║
║  │ [ÖWG-Dobl      ▼] │  │  │ 📷   │ │ 📷   │ │ 📷   │     │  ║
║  │                    │  │  │09:14 │ │09:31 │ │10:42 │     │  ║
║  │ Datum:             │  │  │☐  ★★ │ │☐     │ │☑  ★★★│     │  ║
║  │ [27.03.2026] [◀][▶]│  │  └──────┘ └──────┘ └──────┘     │  ║
║  │                    │  │  ┌──────┐ ┌──────┐ ┌──────┐     │  ║
║  │ Tags:              │  │  │ 📷   │ │ 📷   │ │ 🎬   │     │  ║
║  │ ☑ Schalung         │  │  │11:05 │ │13:07 │ │13:22 │     │  ║
║  │ ☑ Bewehrung        │  │  │☐     │ │☑     │ │☐     │     │  ║
║  │ ☐ Mangel           │  │  └──────┘ └──────┘ └──────┘     │  ║
║  │                    │  │                                    │  ║
║  │ Bewertung:         │  │  12 Fotos, 1 Video                │  ║
║  │ ★★★☆☆ und höher   │  │  2 ausgewählt                     │  ║
║  │                    │  │                                    │  ║
║  │ Ordner:            │  └────────────────────────────────────┘  ║
║  │ 📁 2026-03-27      │                                         ║
║  │ 📁 2026-03-26      │  [Ins Bautagebuch] [Im Explorer]       ║
║  │ 📁 2026-03-25      │  [Vollbild]        [EXIF anzeigen]     ║
║  └────────────────────┘                                         ║
╚══════════════════════════════════════════════════════════════════╝
```

### 5.2 Vollbild-Viewer

```
╔══════════════════════════════════════════════════════════════════╗
║                                                          [X]    ║
║                                                                  ║
║                    ┌──────────────────────┐                      ║
║                    │                      │                      ║
║       [◀]          │      FOTO            │         [▶]          ║
║                    │      (Zoom mit       │                      ║
║                    │       Mausrad)        │                      ║
║                    │                      │                      ║
║                    └──────────────────────┘                      ║
║                                                                  ║
║  IMG_20260327_0914.jpg | 4032×3024 | 8.2 MB | 09:14:23         ║
║  GPS: 47.0523, 15.3891 | Tags: Schalung, OG | ★★★☆☆           ║
║  [☑ Für Bautagebuch] [Tags bearbeiten] [Bewertung] [Löschen]  ║
╚══════════════════════════════════════════════════════════════════╝
```

Keyboard: ← → (Navigation), +/- oder Mausrad (Zoom), Escape (Schließen), Delete (Löschen), 1-5 (Bewertung).

---

## 6. EXIF + Geodaten

### 6.1 EXIF auslesen

```csharp
public class PhotoInfo
{
    public string FileName { get; set; }
    public string FullPath { get; set; }
    public DateTime DateTaken { get; set; }       // EXIF DateTimeOriginal oder Datei-Datum
    public double? Latitude { get; set; }          // GPS aus EXIF
    public double? Longitude { get; set; }
    public long FileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? CameraModel { get; set; }
    public string? Tags { get; set; }              // Aus BPM-DB, nicht EXIF
    public int? Rating { get; set; }               // 1-5 Sterne
    public string ProjectId { get; set; }          // Zuordnung zu BPM-Projekt
}
```

**NuGet-Paket für EXIF:** `MetadataExtractor` (bewährt, MIT-Lizenz, liest JPG/PNG/HEIC).

### 6.2 Geodaten-Prüfung (BPM-spezifisch)

Foto-GPS-Koordinaten vs. Projekt-Koordinaten aus `ProjectLocation`:

```
Foto GPS:     47.0523, 15.3891 (EXIF)
Projekt GPS:  47.0510, 15.3880 (ProjectLocation, umgerechnet aus EPSG:31258)
Distanz:      ~150m → ✅ Passt (innerhalb Baustellen-Radius)

Foto GPS:     47.1200, 15.4500 (EXIF)
Projekt GPS:  47.0510, 15.3880
Distanz:      ~8km → ⚠️ Warnung: "Foto möglicherweise falsch zugeordnet"
```

Konfigurierbarer Radius (z.B. 500m). Fotos mit zu großer Distanz werden markiert. User kann bestätigen oder anderem Projekt zuweisen.

---

## 7. Daten-Speicherung

### 7.1 Phase 1 — Datei-basiert (kein DB)

Für den Start: Keine eigene Datenbank für Fotos. Tags und Bewertungen in einer JSON-Datei pro Projekt:

```
OneDrive/.AppData/BauProjektManager/Projects/202512_OeWG-Dobl/
└── photo-metadata.json
```

```json
{
  "photos": [
    {
      "fileName": "IMG_20260327_0914.jpg",
      "tags": ["Schalung", "OG"],
      "rating": 3,
      "inBautagebuch": true,
      "bautagebuchDate": "2026-03-27"
    }
  ]
}
```

### 7.2 Phase 2 — SQLite (später)

Wenn das Modul wächst, Migration zu SQLite (wie PhotoFolder V2 geplant hat):

```sql
CREATE TABLE photos (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    path            TEXT UNIQUE NOT NULL,
    filename        TEXT NOT NULL,
    project_id      TEXT NOT NULL,
    date_taken      TEXT,
    latitude        REAL,
    longitude       REAL,
    file_size       INTEGER,
    width           INTEGER,
    height          INTEGER,
    thumb_hash      TEXT,
    first_seen      TEXT NOT NULL
);

CREATE TABLE photo_tags (
    photo_id    INTEGER REFERENCES photos(id),
    tag         TEXT NOT NULL,
    PRIMARY KEY (photo_id, tag)
);

CREATE TABLE photo_ratings (
    photo_id    INTEGER PRIMARY KEY REFERENCES photos(id),
    stars       INTEGER NOT NULL CHECK(stars BETWEEN 1 AND 5),
    rated_at    TEXT NOT NULL
);
```

Das Schema ist kompatibel mit dem PhotoFolder V2 DB-Schema (kann zusammengeführt werden).

---

## 8. Video-Handling

### 8.1 Was von PhotoFolder V2 übernommen wird

| Feature | In PhotoFolder V2 | In BPM |
|---------|-------------------|--------|
| Video-Thumbnails | FFmpeg Frame-Extraktion | ✅ Gleich (FFmpegWrapper) |
| Animierte GIF-Previews | FFmpeg → GIF | ✅ Übernehmen |
| Video-Wiedergabe | HLS im Browser | ❌ WPF MediaElement (direkt abspielen) |
| Codec-Konvertierung | FFmpeg H.264 | 🟡 Nur wenn nötig (die meisten Handyvideos sind H.264) |
| Video-Metadaten | FFprobe | ✅ Gleich |

### 8.2 FFmpeg-Abhängigkeit

FFmpeg/FFprobe wird geteilt mit anderen Modulen (Plankopf-Extraktion braucht es nicht, aber Foto + Video schon). Liegt lokal im BPM-Verzeichnis:

```
%LocalAppData%\BauProjektManager\
└── Tools/
    ├── ffmpeg.exe
    └── ffprobe.exe
```

**Lesson Learned aus PhotoFolder:** FFmpeg immer lokal, nie über System-PATH suchen. `Get-Command` bzw. `Process.Start` mit absolutem Pfad.

---

## 9. Bautagebuch-Integration

### 9.1 Workflow

1. User öffnet Foto-Modul, filtert auf Projekt + Datum
2. Wählt Fotos per Checkbox aus (☑)
3. Klickt "Ins Bautagebuch"
4. Bautagebuch-Modul übernimmt die Foto-Pfade für den Tageseintrag
5. Im Bautagebuch: Fotos einzelnen Abschnitten zuordnen (Schalung, Bewehrung etc.)
6. PDF-Export: Ausgewählte Fotos einbetten, Rest nur referenziert

### 9.2 Druckauswahl

Nicht alle Fotos kommen ins PDF. Pro Foto:
- ☑ Im Druck (wird ins PDF eingebettet)
- ☐ Nur referenziert (Pfad im Anhang, kein Bild)

---

## 10. Lessons Learned aus PhotoFolder V2 (für C# relevant)

| Erfahrung aus PowerShell | Konsequenz für C# |
|--------------------------|-------------------|
| Parallel-Processing 5-8x schneller für Thumbnails | `Parallel.ForEachAsync` nutzen |
| Config-Caching (60s TTL) spart massive I/O | `IOptions<T>` mit DI, oder manueller Cache |
| On-Demand > Pre-Generation für Thumbnails | Lazy-Loading in WPF (VirtualizingStackPanel) |
| Hash-basiertes Caching (Pfad + Modified) | 1:1 übernehmen, kein Manifest |
| Deep-Merge für Config | Nicht nötig in C# (stark typisierte Config-Klasse) |
| GDI+/System.Drawing für Thumbnails | SkiaSharp oder ImageSharp (Cross-Platform, moderner) |
| Runspace-Overhead für Parallelität | Entfällt — async/await + TPL nativ |
| `Set-StrictMode` / Null-Crashes | Nullable Reference Types in C# |
| Route-Regex Reihenfolge | Entfällt — kein HTTP-Server |
| FFmpeg immer lokal, nie PATH | Absoluter Pfad in Config/Settings |

---

## 11. NuGet-Pakete

| Paket | Zweck |
|-------|-------|
| `MetadataExtractor` | EXIF-Daten lesen (Datum, GPS, Kamera) |
| `SixLabors.ImageSharp` oder `SkiaSharp` | Thumbnail-Generierung (Bilder) |
| `CommunityToolkit.Mvvm` | MVVM (wie alle BPM-Module) |
| `Microsoft.Data.Sqlite` | Nur wenn Phase 2 (SQLite) umgesetzt wird |

FFmpeg als externer Prozess, kein NuGet nötig.

---

## 12. Umsetzungsreihenfolge

| Phase | Was | Aufwand |
|-------|-----|--------|
| **1** | MediaScannerService — Ordner rekursiv scannen, Dateien auflisten | Klein |
| **2** | ThumbnailService — Hash-basiert, on-demand, Cache in LocalAppData | Mittel |
| **3** | FotoView — WPF Thumbnail-Grid mit VirtualizingStackPanel | Mittel |
| **4** | ExifService — Datum, GPS aus EXIF lesen | Klein |
| **5** | FotoViewerDialog — Vollbild mit Zoom und Navigation | Mittel |
| **6** | Projekt-Filter + Datum-Navigation | Klein |
| **7** | Video-Thumbnails via FFmpeg | Mittel (FFmpegWrapper) |
| **8** | Bautagebuch-Integration (Checkbox → Übernahme) | Klein (wenn Bautagebuch existiert) |
| **9** | Tag-System + Bewertung (JSON erstmal, SQLite später) | Mittel |
| **10** | Geodaten-Prüfung (Foto-GPS vs. Projekt-Koordinaten) | Klein |

**Geschätzt:** Phase 1–6 in ~5 Sessions, Phase 7–10 in ~3–5 Sessions.

---

## 13. Offene Entscheidungen

| Frage | Optionen | Entscheidung |
|-------|---------|-------------|
| Thumbnail-Library? | SkiaSharp (Google, schnell) vs. ImageSharp (SixLabors, reines C#) | ⬜ Testen |
| HEIC-Support? | iPhone-Fotos sind oft HEIC statt JPG | ⬜ Prüfen ob ImageSharp/SkiaSharp das kann |
| Video direkt in WPF abspielen? | MediaElement (eingebaut) vs. LibVLCSharp (mächtiger) | ⬜ MediaElement erstmal |
| PhotoFolder V2 weiterpflegen? | Als eigenständiges Tool behalten oder komplett in BPM aufgehen? | ⬜ Erstmal parallel, später entscheiden |
| RAW-Fotos? | CR2, ARW etc. von DSLR-Kameras | ⬜ Nicht relevant für Baustellenfotos (Handy = JPG) |

---

*Dieses Konzept kombiniert die BPM-spezifischen Anforderungen (Projekt-Filter, Bautagebuch, Geodaten) mit den bewährten Lösungen aus PhotoFolder V2 (Scanner, Thumbnails, Caching, Video). Die PowerShell-Implementation dient als Referenz — die C#-Version wird die gleiche Logik nutzen, aber ohne HTTP-Server und mit nativer WPF-UI.*
