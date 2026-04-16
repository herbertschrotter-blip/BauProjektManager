---
doc_id: coding-standards
doc_type: policy
authority: source_of_truth
status: active
owner: herbert
topics: [naming, formatierung, mvvm, wpf, xaml, di, async, testing, json, sicherheit, git, performance, datenschutz]
read_when: [neue-klasse, neuer-service, neues-viewmodel, code-review, xaml-erstellen, datenschutz-code]
related_docs: [architektur, dsvgo-architektur, db-schema]
related_code: [src/BauProjektManager.App/, src/BauProjektManager.Domain/, src/BauProjektManager.Settings/, src/BauProjektManager.PlanManager/]
supersedes: []
---

## AI-Quickload
- Zweck: Verbindliche Code-Richtlinien für C#, WPF/XAML, MVVM, Git und Datenschutz im BPM-Projekt
- Autorität: source_of_truth
- Lesen wenn: Neue Klasse/Service/ViewModel, Code-Review, XAML erstellen, Datenschutz-relevanter Code
- Nicht zuständig für: DB-Schema (→ DB-SCHEMA.md), Architektur-Entscheidungen (→ Architektur.md, ADR.md), Privacy-Architektur (→ DSVGO-Architektur.md)
- Kapitel:
  - 1. Namensgebung
  - 2. Code-Formatierung
  - 3. Klassen-Struktur
  - 4. Variablen & Typen
  - 5. Methoden
  - 6. Error-Handling
  - 7. MVVM Pattern (WPF-spezifisch)
  - 8. Services & Dependency Injection
  - 9. Dokumentation
  - 10. XAML-Konventionen (WPF)
  - 11. Async/Await
  - 12. Testing
  - 13. JSON-Konventionen (Registry & Config)
  - 14. Sicherheit & Dateisystem
  - 15. Git Commit Standards
  - 16. Performance-Richtlinien
  - 17. Datenschutz im Code (PFLICHT)
  - 18. WPF Code-Behind Dialoge
  - 19. Sync-Vorbereitung & Datenkonventionen (PFLICHT)
- Pflichtlesen:
  - Kapitel 7 (MVVM) bei neuem ViewModel oder View
  - Kapitel 10 (XAML) bei neuer XAML-Datei
  - Kapitel 17 (Datenschutz) bei externem API-Call oder Personendaten
  - Kapitel 19 (Sync-Vorbereitung) bei neuer Tabelle oder neuem Service
- Fachliche Invarianten:
  - App/Shell-XAML: Farben über Theme-Tokens (StaticResource) — nie hardcoded Hex
  - Modul-XAML + Custom-Dialog-Windows: DynamicResource (Pflicht, da App-Resources erst zur Laufzeit aufgelöst werden)
  - Keine Business-Logik im Code-Behind — nur View-Logik
  - Datenschutz-Entscheidungen nie im ViewModel — immer über IPrivacyPolicy
  - Logging: Keine Personendaten, nur IDs (Serilog Structured Logging)
  - Neue fachliche Tabellen: ULID + 6 Sync-Spalten + UTC + Soft Delete (Kapitel 19)
  - Writes über Services mit IUserContext.DisplayName für created_by/last_modified_by (Kapitel 19.5/19.6, ADR-052)

---

# BauProjektManager — Coding Standards

**Version:** 1.0.0  
**Datum:** 26.03.2026  
**Gilt für:** C# (.NET 10 LTS), WPF (XAML), MVVM Pattern
**Quellen:** Microsoft .NET Conventions, Google C# Style Guide, Community Best Practices  

---

## 1. Namensgebung

### 1.1 Übersicht

| Element | Schreibweise | Beispiel | Falsch |
|---------|-------------|---------|--------|
| Klassen | PascalCase | `PlanImportService` | `planImportService` |
| Methoden | PascalCase | `ImportPlans()` | `importPlans()` |
| Interfaces | IPascalCase | `IRegistryService` | `RegistryService` |
| Properties | PascalCase | `PlanCount` | `planCount` |
| Events | PascalCase | `ImportCompleted` | `importCompleted` |
| Enums | PascalCase (Singular) | `PlanStatus` | `PlanStatuses` |
| Enum-Werte | PascalCase | `PlanStatus.Active` | `PlanStatus.ACTIVE` |
| Lokale Variablen | camelCase | `planCount` | `PlanCount` |
| Parameter | camelCase | `fileName` | `FileName` |
| Private Felder | _camelCase | `_registry` | `registry` |
| Konstanten | PascalCase | `MaxRetryCount` | `MAX_RETRY_COUNT` |
| Namespaces | PascalCase | `BauProjektManager.Services` | `bauprojektmanager.services` |
| Dateien | Wie die Klasse | `PlanImportService.cs` | `planImportService.cs` |

### 1.2 Detaillierte Regeln

**Keine ungarische Notation:**
```csharp
// RICHTIG
int counter;
string name;
List<Plan> plans;

// FALSCH — keine Typ-Prefixe
int iCounter;
string strName;
List<Plan> lstPlans;
```

**Keine Abkürzungen (außer bekannte):**
```csharp
// RICHTIG
UserGroup userGroup;
CustomerId customerId;
XmlDocument xmlDocument;
HtmlHelper htmlHelper;

// FALSCH
UserGroup usrGrp;
Assignment empAssignment;
```

**Abkürzungen mit 3+ Zeichen in PascalCase:**
```csharp
// RICHTIG
HtmlHelper htmlHelper;
FtpTransfer ftpTransfer;
PdfDocument pdfDocument;

// FALSCH
HTMLHelper hTMLHelper;
FTPTransfer fTPTransfer;

// Ausnahme: 2-Buchstaben Abkürzungen bleiben UPPERCASE
UIControl uiControl;
IOStream ioStream;
```

**Boolean-Variablen mit Verb-Prefix:**
```csharp
// RICHTIG
bool isActive;
bool hasPermission;
bool canExecute;
bool shouldArchive;

// FALSCH
bool active;
bool permission;
bool archive;
```

**Async-Methoden mit Async-Suffix:**
```csharp
// RICHTIG
public async Task<Plan> LoadPlanAsync(string id)
public async Task ImportPlansAsync()

// FALSCH
public async Task<Plan> LoadPlan(string id)
public async Task ImportPlans()
```

### 1.3 Namespace-Struktur

```csharp
// 5-Projekte-Struktur (ADR-006) — kein Shared-Projekt!
BauProjektManager                              // Root-Namespace (App-Projekt)
BauProjektManager.Domain.Models                // Fachmodelle (Project, Client, Building...)
BauProjektManager.Domain.Models.PlanManager    // PlanManager-spezifische Models (FileNameSegment, FieldType...)
BauProjektManager.Domain.Interfaces            // Service-Verträge (IDialogService, IDeveloperToolsService)
BauProjektManager.Domain.Enums                 // Enums (ProjectStatus, DataClassification)
BauProjektManager.Infrastructure.Persistence   // SQLite, JSON, Dateisystem (ProjectDatabase, AppSettingsService)
BauProjektManager.Infrastructure.Dev           // Dev-only Services (DeveloperToolsService)
BauProjektManager.Settings.ViewModels          // Einstellungen-MVVM (SettingsViewModel)
BauProjektManager.Settings.Views               // Einstellungen-XAML (SettingsView, ProjectEditDialog)
BauProjektManager.PlanManager.ViewModels       // PlanManager-MVVM (PlanManagerViewModel, ProfileWizardViewModel)
BauProjektManager.PlanManager.Views            // PlanManager-XAML
BauProjektManager.PlanManager.Services         // PlanManager-Logik (FileNameParser, ProfileManager)
```

---

## 2. Code-Formatierung

### 2.1 Klammern (Allman-Style)

Jede geschweifte Klammer auf einer eigenen Zeile:

```csharp
// RICHTIG (Allman-Style)
public class PlanImportService
{
    public void ImportPlan(Plan plan)
    {
        if (plan.IsNew)
        {
            SavePlan(plan);
        }
        else
        {
            UpdatePlan(plan);
        }
    }
}

// FALSCH (K&R-Style — nicht verwenden!)
public class PlanImportService {
    public void ImportPlan(Plan plan) {
        if (plan.IsNew) {
            SavePlan(plan);
        }
    }
}
```

### 2.2 Immer Klammern verwenden

Auch bei einzelnen Anweisungen — verhindert Fehler:

```csharp
// RICHTIG
if (plan.IsNew)
{
    SavePlan(plan);
}

// FALSCH — gefährlich!
if (plan.IsNew)
    SavePlan(plan);
```

### 2.3 Einrückung

- **4 Spaces** (keine Tabs)
- Continuation-Zeilen: 4 Spaces extra eingerückt

### 2.4 Leerzeilen

```csharp
public class PlanImportService
{
    private readonly IRegistryService _registry;
    private readonly IFileParser _parser;
    // Leerzeile nach Feld-Deklarationen

    public PlanImportService(IRegistryService registry, IFileParser parser)
    {
        _registry = registry;
        _parser = parser;
    }
    // Leerzeile zwischen Methoden

    public void ImportPlan(Plan plan)
    {
        // Logische Blöcke mit Leerzeile trennen
        var existingPlan = FindExistingPlan(plan.Number);

        if (existingPlan == null)
        {
            CreateNewPlan(plan);
        }
        else
        {
            UpdateExistingPlan(existingPlan, plan);
        }
    }
}
```

### 2.5 Zeilenlänge

- Maximal **120 Zeichen** pro Zeile
- Bei langen Methodensignaturen umbrechen:

```csharp
// Kurz genug — eine Zeile
public Plan FindPlan(string number, string index)

// Zu lang — umbrechen
public ImportResult ImportPlanWithArchiving(
    Plan newPlan,
    Plan existingPlan,
    string archivePath,
    ImportOptions options)
{
    // ...
}
```

### 2.6 Leerzeichen

```csharp
// RICHTIG — Leerzeichen um Operatoren
int result = a + b;
bool isValid = count > 0 && name != null;

// RICHTIG — Leerzeichen nach Komma
public void Process(string name, int count, bool flag)

// RICHTIG — Leerzeichen nach Keywords
if (condition)
for (int i = 0; i < count; i++)
while (isRunning)

// FALSCH — kein Leerzeichen vor Klammer bei Methodenaufruf
Process (name, count);  // FALSCH
Process(name, count);   // RICHTIG
```

---

## 3. Klassen-Struktur

### 3.1 Reihenfolge innerhalb einer Klasse

```csharp
public class PlanImportService : IImportService
{
    // 1. Konstanten
    private const int MaxRetryCount = 3;
    private const string ArchiveFolderName = "_Archiv";

    // 2. Statische Felder
    private static readonly ILogger Logger = LogManager.GetLogger();

    // 3. Private Felder
    private readonly IRegistryService _registry;
    private readonly IFileParser _parser;
    private int _importCount;

    // 4. Konstruktoren
    public PlanImportService(IRegistryService registry, IFileParser parser)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    // 5. Properties
    public int ImportCount => _importCount;
    public bool IsRunning { get; private set; }

    // 6. Events
    public event EventHandler<ImportProgressEventArgs>? ProgressChanged;

    // 7. Public Methoden
    public async Task<ImportResult> ImportAsync(string sourcePath)
    {
        // ...
    }

    // 8. Internal/Protected Methoden
    internal void ValidateProfile(TypeProfile profile)
    {
        // ...
    }

    // 9. Private Methoden
    private Plan ParseFileName(string fileName)
    {
        // ...
    }

    // 10. Nested Types (am Ende, sparsam verwenden)
    private class ImportContext
    {
        // ...
    }
}
```

### 3.2 Eine Klasse pro Datei

```
// RICHTIG
Plan.cs                  → enthält class Plan
PlanImportService.cs     → enthält class PlanImportService
IImportService.cs        → enthält interface IImportService

// FALSCH — mehrere Klassen in einer Datei
Models.cs                → enthält Plan, Project, TypeProfile
```

**Ausnahme:** Kleine zusammengehörige Records/Enums dürfen zusammen:
```csharp
// PlanStatus.cs — Enum + zugehöriger Record ist OK
public enum PlanStatus
{
    New,
    Updated,
    Unchanged,
    Unknown
}

public record PlanStatusInfo(PlanStatus Status, string Description);
```

### 3.3 Klassengröße

- Maximal **300-400 Zeilen** pro Klasse
- Wenn größer → aufteilen in kleinere Klassen
- Single Responsibility Principle: Eine Klasse hat EINE Aufgabe

---

## 4. Variablen & Typen

### 4.1 var verwenden (wenn Typ offensichtlich)

```csharp
// RICHTIG — Typ ist aus der rechten Seite offensichtlich
var plans = new List<Plan>();
var registry = new RegistryService();
var result = await ImportAsync();
var stream = File.Create(path);

// RICHTIG — Primitive Typen explizit
int count = 0;
string name = "Polierplan";
bool isActive = true;
double ratio = 0.5;

// FALSCH — Typ ist nicht offensichtlich, var vermeiden
var result = GetResult();       // Was ist result? Unklar!
Plan result = GetResult();      // RICHTIG — Typ sichtbar
```

### 4.2 Null-Handling

```csharp
// Nullable Reference Types aktivieren (in .csproj)
// <Nullable>enable</Nullable>

// RICHTIG — Null-Checks auf public Methoden
public void ImportPlan(Plan plan)
{
    ArgumentNullException.ThrowIfNull(plan);
    // ...
}

// RICHTIG — Null-Coalescing
string name = plan.Name ?? "Unbekannt";
var cache = _cache ?? throw new InvalidOperationException("Cache not initialized");

// RICHTIG — Null-Conditional
int? count = plans?.Count;
string? description = plan?.Profile?.Description;

// RICHTIG — Pattern Matching für Null-Check
if (plan is not null)
{
    ProcessPlan(plan);
}

// RICHTIG — Null-Coalescing Assignment
_cache ??= new PlanCache();
```

### 4.3 Collections

```csharp
// RICHTIG — Interface-Typ für Rückgaben
public IReadOnlyList<Plan> GetPlans() => _plans.AsReadOnly();
public IEnumerable<Plan> FindPlans(string query) => _plans.Where(p => p.Name.Contains(query));

// RICHTIG — Konkrete Typen für lokale Variablen
var plans = new List<Plan>();
var lookup = new Dictionary<string, Plan>();
var uniqueNumbers = new HashSet<string>();

// RICHTIG — ObservableCollection für WPF Binding
public ObservableCollection<PlanViewModel> Plans { get; } = new();
```

### 4.4 String-Handling

```csharp
// RICHTIG — String Interpolation
string message = $"Plan {plan.Number} imported to {targetPath}";

// RICHTIG — Verbatim String für Pfade
string path = @"C:\Users\Herbert\OneDrive\02Arbeit";

// RICHTIG — Raw String für mehrzeiligen Text (C# 11+)
string json = """
    {
        "name": "Test",
        "value": 42
    }
    """;

// RICHTIG — String.IsNullOrWhiteSpace für Checks
if (string.IsNullOrWhiteSpace(planNumber))
{
    throw new ArgumentException("Plan number cannot be empty", nameof(planNumber));
}

// RICHTIG — StringComparison für Vergleiche
if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
{
    // ...
}

// FALSCH — String-Concatenation in Schleifen
string result = "";
foreach (var plan in plans)
{
    result += plan.Name + ", ";  // FALSCH — erzeugt viele String-Objekte
}

// RICHTIG — StringBuilder für Schleifen
var sb = new StringBuilder();
foreach (var plan in plans)
{
    sb.Append(plan.Name);
    sb.Append(", ");
}
```

---

## 5. Methoden

### 5.1 Kleine Methoden

Eine Methode macht EINE Sache. Maximal 30-40 Zeilen.

```csharp
// RICHTIG — Aufgeteilt in kleine, verständliche Methoden
public async Task<ImportResult> ImportAsync(string sourcePath)
{
    var files = ScanSourceFolder(sourcePath);
    var classified = ClassifyFiles(files);
    var preview = BuildPreview(classified);

    if (!await ConfirmImportAsync(preview))
    {
        return ImportResult.Cancelled;
    }

    return await ExecuteImportAsync(classified);
}

// FALSCH — Riesenmethode mit 200 Zeilen die alles macht
public async Task<ImportResult> ImportAsync(string sourcePath)
{
    // ... 200 Zeilen Code ...
}
```

### 5.2 Parameter

```csharp
// RICHTIG — Maximal 4-5 Parameter
public Plan CreatePlan(string number, string index, string description, PlanType type)

// Zu viele Parameter? → Options-Objekt verwenden
public ImportResult Import(ImportOptions options)

public class ImportOptions
{
    public string SourcePath { get; init; } = string.Empty;
    public bool OverwriteExisting { get; init; }
    public bool ArchiveOldIndex { get; init; } = true;
    public int MaxFiles { get; init; } = int.MaxValue;
}
```

### 5.3 Return Early (Guard Clauses)

```csharp
// RICHTIG — Guard Clauses am Anfang, dann Hauptlogik
public Plan? FindPlan(string number)
{
    if (string.IsNullOrWhiteSpace(number))
    {
        return null;
    }

    if (!_cache.ContainsKey(number))
    {
        return null;
    }

    return _cache[number];
}

// FALSCH — Tief verschachtelt
public Plan? FindPlan(string number)
{
    if (!string.IsNullOrWhiteSpace(number))
    {
        if (_cache.ContainsKey(number))
        {
            return _cache[number];
        }
    }
    return null;
}
```

### 5.4 Expression-bodied Members

```csharp
// RICHTIG — Für einfache Einzeiler
public string FullName => $"{FirstName} {LastName}";
public bool IsActive => Status == ProjectStatus.Active;
public override string ToString() => $"Plan {Number}-{Index}";

// FALSCH — Für komplexe Logik (normalen Body verwenden)
public string FullName => string.IsNullOrWhiteSpace(LastName) 
    ? FirstName 
    : $"{FirstName} {LastName}".Trim();  // Zu komplex für =>
```

---

## 6. Error-Handling

### 6.1 Exceptions

```csharp
// RICHTIG — Spezifische Exceptions fangen
try
{
    var content = await File.ReadAllTextAsync(path);
    return JsonSerializer.Deserialize<Registry>(content);
}
catch (FileNotFoundException ex)
{
    _logger.LogWarning("Registry not found at {Path}", path);
    return CreateDefaultRegistry();
}
catch (JsonException ex)
{
    _logger.LogError(ex, "Registry corrupt at {Path}", path);
    throw new RegistryCorruptException(path, ex);
}

// FALSCH — Allgemeine Exception fangen
try
{
    // ...
}
catch (Exception ex)  // NIEMALS so (außer als letzter Fallback auf Top-Level)
{
    // Schluckt ALLE Fehler
}

// FALSCH — Leerer Catch-Block
try
{
    // ...
}
catch { }  // NIEMALS — Fehler werden unsichtbar!
```

### 6.2 Eigene Exceptions

```csharp
/// <summary>
/// Wird ausgelöst wenn die Registry-Datei beschädigt ist.
/// </summary>
public class RegistryCorruptException : Exception
{
    public string FilePath { get; }

    public RegistryCorruptException(string filePath, Exception innerException)
        : base($"Registry file is corrupt: {filePath}", innerException)
    {
        FilePath = filePath;
    }
}

/// <summary>
/// Wird ausgelöst wenn ein Dateiname nicht geparst werden kann.
/// </summary>
public class PlanParseException : Exception
{
    public string FileName { get; }
    public string ProfileName { get; }

    public PlanParseException(string fileName, string profileName)
        : base($"Cannot parse '{fileName}' with profile '{profileName}'")
    {
        FileName = fileName;
        ProfileName = profileName;
    }
}
```

### 6.3 throw vs throw ex

```csharp
// RICHTIG — Stack Trace bleibt erhalten
catch (IOException ex)
{
    _logger.LogError(ex, "File operation failed");
    throw;  // Stack Trace bleibt!
}

// FALSCH — Stack Trace geht verloren
catch (IOException ex)
{
    _logger.LogError(ex, "File operation failed");
    throw ex;  // Stack Trace wird zurückgesetzt!
}
```

### 6.4 Using/Dispose Pattern

```csharp
// RICHTIG — using Statement für IDisposable
using var stream = File.OpenRead(path);
using var reader = new StreamReader(stream);
var content = await reader.ReadToEndAsync();

// RICHTIG — using Block (älterer Stil, auch OK)
using (var stream = File.OpenRead(path))
{
    // Stream wird am Ende automatisch disposed
}

// FALSCH — Vergessen zu disposen
var stream = File.OpenRead(path);
var content = ReadContent(stream);
// stream wird nie geschlossen → Speicher-Leak!
```

---

## 7. MVVM Pattern (WPF-spezifisch)

### 7.1 Projektstruktur

```
PlanManager/
├── App.xaml                        ← Einstiegspunkt + Globale Ressourcen
├── App.xaml.cs
├── Models/                         ← Reine Datenklassen (keine UI-Referenzen!)
│   ├── Plan.cs
│   ├── Project.cs
│   ├── TypeProfile.cs
│   ├── ImportResult.cs
│   └── PlanStatus.cs
├── ViewModels/                     ← Logik + State für die GUI
│   ├── PlanManagerViewModel.cs      ← partial class : ObservableObject
│   ├── ProjectDetailViewModel.cs
│   ├── ImportPreviewViewModel.cs
│   └── ProfileWizardViewModel.cs
├── Views/                          ← XAML Dateien (rein visuell)
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs          ← Minimal! Nur DataContext setzen
│   ├── ProjectDetailView.xaml
│   ├── ImportPreviewDialog.xaml
│   └── SegmentAssignerDialog.xaml
├── Services/                       ← Geschäftslogik (in Domain + Infrastructure)
│   ├── FileNameParser.cs
│   ├── DocumentTypeRecognizer.cs
│   ├── ImportWorkflowService.cs
│   └── ProfileManager.cs
├── Converters/                     ← WPF Value Converters
│   ├── StatusToColorConverter.cs
│   └── BoolToVisibilityConverter.cs
└── Resources/                      ← Styles, Icons, Strings
    ├── Styles.xaml
    ├── Colors.xaml
    └── Icons/
```

### 7.2 Model (Datenklasse)

Models sind reine Daten — keine Logik, keine UI-Referenzen, kein INotifyPropertyChanged:

```csharp
namespace BauProjektManager.PlanManager.Models;

/// <summary>
/// Repräsentiert einen einzelnen Bauplan (PDF + optional DWG).
/// </summary>
public class Plan
{
    public string PlanNumber { get; set; } = string.Empty;
    public string PlanIndex { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? PartnerFileName { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string Md5Hash { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
    public List<string> ArchivedIndexes { get; set; } = new();
}
```

### 7.3 ViewModel (GUI-Logik)

ViewModel erbt von `ObservableObject` (CommunityToolkit.Mvvm) und nutzt `[ObservableProperty]` für Binding-Properties und `[RelayCommand]` für Commands (ADR-015):

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// ViewModel für die Projektliste im PlanManager.
/// partial class ist Pflicht für CommunityToolkit Source Generators.
/// </summary>
public partial class PlanManagerViewModel : ObservableObject
{
    private readonly ProjectDatabase _db = new();

    // [ObservableProperty] generiert automatisch:
    // - public Property "Projects" (PascalCase)
    // - OnPropertyChanged wird automatisch aufgerufen
    // - Feld muss private + _camelCase sein
    [ObservableProperty]
    private ObservableCollection<PlanProjectItem> _projects = [];

    [ObservableProperty]
    private PlanProjectItem? _selectedProject;

    [ObservableProperty]
    private string _statusText = "";

    // [RelayCommand] generiert automatisch:
    // - public IRelayCommand LoadProjectsCommand
    // - Async-Methoden werden korrekt behandelt
    [RelayCommand]
    private async Task LoadProjectsAsync()
    {
        var projects = _db.LoadAllProjects();
        Projects.Clear();
        foreach (var p in projects)
        {
            Projects.Add(new PlanProjectItem(p));
        }
        StatusText = $"{Projects.Count} Projekte geladen";
    }
}
```

**WICHTIG — CommunityToolkit Regeln:**
- Klasse muss `partial` sein (Source Generators)
- Felder: `private` + `_camelCase` → generiert `PascalCase` Property
- Kein manuelles `OnPropertyChanged` nötig
- Kein eigenes `BaseViewModel` — `ObservableObject` ist die Basisklasse
- Kein eigenes `RelayCommand` — kommt vom NuGet

### 7.4 View (XAML)

View enthält NUR XAML und minimalen Code-Behind:

```xml
<!-- MainWindow.xaml -->
<Window x:Class="BauProjektManager.PlanManager.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="BauProjektManager — PlanManager"
        Width="900" Height="600">

    <Grid>
        <ListBox ItemsSource="{Binding Projects}"
                 SelectedItem="{Binding SelectedProject}">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <StackPanel>
                        <TextBlock Text="{Binding Name}" FontWeight="Bold"/>
                        <TextBlock Text="{Binding Status}"/>
                    </StackPanel>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </Grid>
</Window>
```

```csharp
// MainWindow.xaml.cs — MINIMAL Code-Behind
namespace BauProjektManager.PlanManager.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;  // Das ist ALLES was hier passiert
    }
}
```

### 7.5 MVVM-Regeln

| Regel | Beschreibung |
|-------|-------------|
| View kennt ViewModel | Über DataContext + Data Binding |
| ViewModel kennt View NICHT | Niemals Window/Control Referenzen im ViewModel |
| ViewModel kennt Model | Liest/schreibt Daten |
| Model kennt niemanden | Reine Datenklasse |
| Minimaler Code-Behind | `InitializeComponent()`, `DataContext`, UI-Orchestrierung (Dialog-Öffnung, Fokus, Navigation). Keine Business-Logik. |
| Commands statt Events | `ICommand` für Button-Clicks, nicht Click-EventHandler |
| ObservableCollection | Für Listen die sich ändern (nicht `List<T>`) |

### 7.6 RelayCommand (CommunityToolkit.Mvvm)

**Keine eigene RelayCommand-Klasse nötig.** CommunityToolkit.Mvvm liefert `RelayCommand` und `AsyncRelayCommand` per NuGet (ADR-015). Commands werden über das `[RelayCommand]` Attribut generiert:

```csharp
public partial class ProjectDetailViewModel : ObservableObject
{
    // Synchroner Command — generiert OpenFolderCommand
    [RelayCommand]
    private void OpenFolder(string path)
    {
        Process.Start("explorer.exe", path);
    }

    // Async Command — generiert ImportPlansCommand
    [RelayCommand]
    private async Task ImportPlansAsync()
    {
        // ...
    }

    // Command mit CanExecute — generiert DeleteCommand + NotifyCanExecuteChanged
    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete()
    {
        // ...
    }

    private bool CanDelete() => SelectedProject is not null;
}
```

**VERBOTEN:**
- Eigene `RelayCommand`-Klasse implementieren (existiert nicht mehr im Projekt)
- `ICommand` manuell implementieren
- `CommandManager.RequerySuggested` direkt verwenden

### 7.7 Value Converters

```csharp
namespace BauProjektManager.PlanManager.Converters;

/// <summary>
/// Konvertiert PlanStatus zu einer Farbe für die GUI.
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PlanStatus status)
        {
            return status switch
            {
                PlanStatus.New => Brushes.Green,
                PlanStatus.Updated => Brushes.Orange,
                PlanStatus.Unchanged => Brushes.Gray,
                PlanStatus.Unknown => Brushes.Red,
                _ => Brushes.Black
            };
        }

        return Brushes.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
```

---

## 8. Services & Dependency Injection

### 8.1 Interface + Implementation

Jeder Service hat ein Interface — das ermöglicht Testing und Austauschbarkeit:

```csharp
// Interface
namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Liest und schreibt die zentrale Projekt-Registry.
/// </summary>
public interface IRegistryService
{
    Task<List<Project>> GetProjectsAsync();
    Task<Project?> GetProjectByIdAsync(string projectId);
    Task SaveProjectAsync(Project project);
    Task<string> GetRegistryPathAsync();
}

// Implementation
public class RegistryService : IRegistryService
{
    private readonly string _registryPath;
    private readonly ILogService _logger;

    public RegistryService(string registryPath, ILogService logger)
    {
        _registryPath = registryPath;
        _logger = logger;
    }

    public async Task<List<Project>> GetProjectsAsync()
    {
        // Implementation...
    }

    // ...
}
```

### 8.2 Dependency Injection Setup

```csharp
// App.xaml.cs — Services registrieren
public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();

        // Services registrieren
        services.AddSingleton<IRegistryService, RegistryService>();
        services.AddSingleton<IFileParserService, FileParserService>();
        services.AddSingleton<IPlanCompareService, PlanCompareService>();
        services.AddSingleton<ILogService, LogService>();

        // ViewModels registrieren
        services.AddTransient<MainViewModel>();
        services.AddTransient<ProjectDetailViewModel>();

        // Views registrieren
        services.AddTransient<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
```

---

## 9. Dokumentation

### 9.1 XML-Dokumentation auf Public Members

```csharp
/// <summary>
/// Service für den Import von Bauplänen aus dem Eingangsordner.
/// Erkennt Plantypen, vergleicht mit Bestand und sortiert ein.
/// </summary>
public class PlanImportService : IImportService
{
    /// <summary>
    /// Importiert alle Pläne aus dem Eingangsordner eines Projekts.
    /// </summary>
    /// <param name="project">Das Projekt dessen Eingangsordner gescannt wird.</param>
    /// <param name="options">Import-Optionen (Überschreiben, Archivieren, etc.).</param>
    /// <returns>Ergebnis mit Zusammenfassung aller Aktionen.</returns>
    /// <exception cref="ArgumentNullException">Wenn project null ist.</exception>
    /// <exception cref="DirectoryNotFoundException">Wenn der Eingangsordner nicht existiert.</exception>
    public async Task<ImportResult> ImportAsync(Project project, ImportOptions? options = null)
    {
        // ...
    }
}
```

### 9.2 Inline-Kommentare

```csharp
// RICHTIG — Erkläre WARUM, nicht WAS
// MD5 statt SHA256 weil wir nur Datei-Gleichheit prüfen, keine Sicherheit
var hash = ComputeMd5(filePath);

// RICHTIG — Erkläre nicht-offensichtliche Entscheidungen
// VBA kann keine verschachtelten JSON-Objekte parsen,
// deshalb speichern wir Buildings als Pipe-getrennten String
string buildings = string.Join("|", project.Buildings.Select(b => b.ToString()));

// FALSCH — Erklärt was der Code macht (das sieht man selbst)
// Erhöhe counter um 1
counter++;

// FALSCH — Kommentar ist veraltet
// Prüfe ob Plan existiert
DeletePlan(plan);  // Kommentar passt nicht zum Code!
```

### 9.3 TODO/FIXME/HACK Kommentare

```csharp
// TODO: PDF-Parsing für Planlisten implementieren (Phase 2)
// FIXME: MD5-Berechnung blockiert UI Thread bei großen Dateien
// HACK: OneDrive lockt Dateien manchmal — 3 Retries als Workaround
```

---

## 10. XAML-Konventionen (WPF)

### 10.1 Formatierung

```xml
<!-- RICHTIG — Ein Attribut pro Zeile bei >2 Attributen -->
<Button Content="Import starten"
        Command="{Binding ImportCommand}"
        IsEnabled="{Binding CanImport}"
        Margin="10,5"
        Padding="15,8"/>

<!-- RICHTIG — Alles in einer Zeile bei <=2 Attributen -->
<TextBlock Text="{Binding ProjectName}" FontWeight="Bold"/>

<!-- FALSCH — Alles in einer Zeile wenn zu lang -->
<Button Content="Import starten" Command="{Binding ImportCommand}" IsEnabled="{Binding CanImport}" Margin="10,5" Padding="15,8"/>
```

### 10.2 Namensgebung in XAML

```xml
<!-- Elemente die im Code-Behind referenziert werden: x:Name in camelCase -->
<TextBox x:Name="searchBox" Text="{Binding SearchQuery}"/>

<!-- NICHT x:Name verwenden wenn nicht nötig — Binding reicht! -->
<!-- FALSCH -->
<TextBlock x:Name="projectNameLabel" Text="{Binding ProjectName}"/>
<!-- RICHTIG — kein x:Name nötig -->
<TextBlock Text="{Binding ProjectName}"/>
```

### 10.3 UI-Naming-Konventionen

| Element | Konvention | Beispiel |
|---------|-----------|---------|
| View (XAML) | `<Modul><Funktion>View.xaml` | `SettingsView.xaml`, `PlanManagerView.xaml` |
| Dialog (XAML) | `<Funktion>Dialog.xaml` | `ProjectEditDialog.xaml`, `ImportPreviewDialog.xaml` |
| ViewModel | `<Funktion>ViewModel.cs` | `SettingsViewModel.cs`, `ProjectEditViewModel.cs` |
| UserControl | `<Funktion>Control.xaml` | `ProjectListControl.xaml`, `RibbonControl.xaml` |
| Style-Key | `Bpm<Typ><Variante>` | `BpmButtonPrimary`, `BpmTextBoxDefault` |
| Color-Key | `Bpm<Zweck>` | `BpmAccentPrimary`, `BpmBgSurface`, `BpmTextSecondary` |

### 10.4 Resource Dictionary Struktur

Alle Farben, Styles und Templates in zentralen Resource Dictionaries:

```xml
<!-- Resources/Styles.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">

    <!-- Farben zentral definieren -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="#2E75B6"/>
    <SolidColorBrush x:Key="SuccessBrush" Color="#4CAF50"/>
    <SolidColorBrush x:Key="WarningBrush" Color="#FF9800"/>
    <SolidColorBrush x:Key="ErrorBrush" Color="#F44336"/>

    <!-- Styles zentral definieren -->
    <Style x:Key="HeaderText" TargetType="TextBlock">
        <Setter Property="FontSize" Value="18"/>
        <Setter Property="FontWeight" Value="Bold"/>
        <Setter Property="Margin" Value="0,0,0,10"/>
    </Style>

    <Style x:Key="PrimaryButton" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="Padding" Value="15,8"/>
        <Setter Property="Margin" Value="5"/>
    </Style>

</ResourceDictionary>
```

### 10.4 Data Binding Regeln

```xml
<!-- RICHTIG — Binding mit Mode angeben wenn nicht Default -->
<TextBox Text="{Binding PlanNumber, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>

<!-- RICHTIG — Binding mit FallbackValue für Null-Sicherheit -->
<TextBlock Text="{Binding Description, FallbackValue='Keine Beschreibung'}"/>

<!-- RICHTIG — Binding mit StringFormat -->
<TextBlock Text="{Binding LastImport, StringFormat='Letzter Import: {0:dd.MM.yyyy HH:mm}'}"/>

<!-- FALSCH — Logik im Code-Behind statt Binding -->
<!-- projectNameLabel.Text = viewModel.ProjectName;  // Nicht so! -->
```

---

## 11. Async/Await

### 11.1 Grundregeln

```csharp
// RICHTIG — async/await für I/O Operationen
public async Task<Registry> LoadRegistryAsync()
{
    var json = await File.ReadAllTextAsync(_registryPath);
    return JsonSerializer.Deserialize<Registry>(json)!;
}

// RICHTIG — ConfigureAwait(false) in Library-Code (nicht in ViewModel!)
// In Services:
public async Task<string> ComputeHashAsync(string filePath)
{
    using var stream = File.OpenRead(filePath);
    var hash = await MD5.HashDataAsync(stream).ConfigureAwait(false);
    return Convert.ToHexString(hash);
}

// FALSCH — .Result oder .Wait() verwenden (Deadlock-Gefahr!)
var registry = LoadRegistryAsync().Result;  // DEADLOCK!
LoadRegistryAsync().Wait();                 // DEADLOCK!

// FALSCH — async void (außer Event-Handler)
public async void ImportPlans() { }         // FALSCH — Exceptions gehen verloren
public async Task ImportPlansAsync() { }    // RICHTIG
```

### 11.2 UI-Thread Beachten

```csharp
// RICHTIG — Lange Operationen nicht auf UI-Thread
public async Task ImportAsync()
{
    IsRunning = true;  // UI Property — OK auf UI Thread

    // Schwere Arbeit auf Background-Thread
    var result = await Task.Run(() =>
    {
        return ScanAndClassifyFiles();  // Dauert lang — nicht UI blockieren
    });

    // Zurück auf UI-Thread für UI-Updates
    Plans.Clear();
    foreach (var plan in result)
    {
        Plans.Add(plan);  // ObservableCollection — muss auf UI Thread
    }

    IsRunning = false;
}
```

---

## 12. Testing

### 12.1 Test-Projektstruktur

```
PlanManager.Tests/
├── Services/
│   ├── FileParserServiceTests.cs
│   ├── PlanCompareServiceTests.cs
│   └── RegistryServiceTests.cs
├── ViewModels/
│   ├── MainViewModelTests.cs
│   └── ImportPreviewViewModelTests.cs
└── TestData/
    ├── sample-registry.json
    └── sample-plans/
```

### 12.2 Test-Namensgebung

```csharp
// Format: MethodName_Scenario_ExpectedResult
[Fact]
public void ParseFileName_ValidPolierplan_ReturnsCorrectPlanNumber()
{
    // Arrange
    var parser = new FileParserService();

    // Act
    var result = parser.ParseFileName("S-103-C_TG Wämde.pdf", polierProfile);

    // Assert
    Assert.Equal("103", result.PlanNumber);
    Assert.Equal("C", result.PlanIndex);
}

[Fact]
public void ParseFileName_UnknownFormat_ThrowsPlanParseException()
{
    var parser = new FileParserService();

    Assert.Throws<PlanParseException>(() =>
        parser.ParseFileName("random_file.pdf", polierProfile));
}

[Fact]
public async Task ImportAsync_NewPlan_CreatesInTargetFolder()
{
    // ...
}
```

### 12.3 Arrange-Act-Assert Pattern

```csharp
[Fact]
public void ComparePlans_NewerIndex_ReturnsUpdated()
{
    // Arrange — Setup
    var existingPlan = new Plan { PlanNumber = "103", PlanIndex = "C" };
    var newPlan = new Plan { PlanNumber = "103", PlanIndex = "D" };
    var comparer = new PlanCompareService();

    // Act — Eine Aktion ausführen
    var result = comparer.Compare(existingPlan, newPlan);

    // Assert — Ergebnis prüfen
    Assert.Equal(PlanStatus.Updated, result.Status);
    Assert.Equal("C", result.OldIndex);
    Assert.Equal("D", result.NewIndex);
}
```

---

## 13. JSON-Konventionen (Registry & Config)

### 13.1 Regeln für registry.json

| Regel | Grund |
|-------|-------|
| camelCase für Keys | Standard in JSON |
| Keine verschachtelten Objekte in Projekt-Daten | VBA-Kompatibilität |
| Koordinaten als separate Felder | `coordinateEast` statt `coordinates.east` |
| Listen als Komma/Pipe-getrennte Strings | VBA `Split()` kompatibel |
| Datum als ISO-String `YYYY-MM-DD` | VBA `CDate()` kompatibel |
| Pfade relativ zu `rootPath` | Portabilität zwischen Geräten |
| UTF-8 ohne BOM | Universelle Kompatibilität |

### 13.2 Serialisierung in C#

```csharp
// RICHTIG — System.Text.Json mit camelCase
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

var json = JsonSerializer.Serialize(registry, options);
await File.WriteAllTextAsync(path, json, Encoding.UTF8);

// RICHTIG — Deserialisierung
var registry = JsonSerializer.Deserialize<Registry>(json, options);
```

---

## 14. Sicherheit & Dateisystem

### 14.1 Pfade

```csharp
// RICHTIG — Path.Combine statt String-Concatenation
string fullPath = Path.Combine(project.RootPath, project.PlansPath, "_Eingang");

// FALSCH — String-Concatenation für Pfade
string fullPath = project.RootPath + "\\" + project.PlansPath + "\\_Eingang";

// RICHTIG — Path-Traversal verhindern
string safePath = Path.GetFullPath(userInput);
if (!safePath.StartsWith(allowedBasePath))
{
    throw new UnauthorizedAccessException("Path outside allowed directory");
}
```

### 14.2 Datei-Operationen

```csharp
// RICHTIG — Prüfen ob Datei/Ordner existiert
if (!File.Exists(sourcePath))
{
    throw new FileNotFoundException("Source file not found", sourcePath);
}

if (!Directory.Exists(targetDir))
{
    Directory.CreateDirectory(targetDir);
}

// RICHTIG — Erst kopieren, dann Quelle löschen (statt Move bei Cross-Drive)
File.Copy(sourcePath, targetPath, overwrite: true);
File.Delete(sourcePath);
```

---

## 15. Git Commit Standards

Siehe separates Dokument: `GIT-COMMIT-RULES.txt`

Format:
```
[vX.Y.Z] Modul/Lib
Typ: Kurztitel (max 50 Zeichen, Englisch)

2-4 Zeilen Kontext (WARUM, nicht WAS)

Dateien:
- Datei1.cs
- Datei2.cs
```

---

## 16. Performance-Richtlinien

### 16.1 Allgemein

```csharp
// RICHTIG — LINQ ist lesbar, aber bei großen Datenmengen prüfen
var activePlans = plans.Where(p => p.IsActive).ToList();

// RICHTIG — Bei großen Dateien: Stream statt alles in Memory
using var stream = File.OpenRead(largePdfPath);
var hash = await ComputeHashAsync(stream);

// FALSCH — Gesamte große Datei in Memory laden
var allBytes = File.ReadAllBytes(largePdfPath);  // 500MB PDF → OutOfMemory!
```

### 16.2 WPF-spezifisch

```csharp
// RICHTIG — Virtualisierung für große Listen aktivieren
// In XAML:
// <ListBox VirtualizingPanel.IsVirtualizing="True"
//          VirtualizingPanel.VirtualizationMode="Recycling"/>

// RICHTIG — UI nicht blockieren
// Schwere Operationen immer mit await Task.Run()

// FALSCH — UI-Thread blockieren
Thread.Sleep(1000);                          // NIEMALS!
var result = heavyOperation.Result;          // NIEMALS!
```

---

## 17. Datenschutz im Code (PFLICHT)

Verbindliche Regeln aus [DSVGO-Architektur.md](DSVGO-Architektur.md) (ADR-035, ADR-036). Gelten ab sofort für jeden neuen Code.

### 17.1 Logging — Keine Personendaten
```csharp
// RICHTIG — nur IDs loggen
_logger.LogInformation("Client {ClientId} aktualisiert", client.Id);
_logger.LogInformation("Projekt {ProjectId} geladen", project.Id);

// FALSCH — Personendaten loggen
_logger.LogInformation("Client {Name} ({Email}) aktualisiert",
    client.ContactPerson, client.Email);
_logger.LogInformation("Mitarbeiter {Name} hat {Stunden}h gearbeitet",
    employee.Name, hours);
```

**VERBOTEN in Log-Nachrichten:** Personennamen, E-Mail-Adressen, Telefonnummern, Personalnummern, Dokumenteninhalte, API-Keys.

### 17.2 Externe Kommunikation — Kein direkter HttpClient

Alle HTTP-Calls an externe APIs MÜSSEN über `IExternalCommunicationService` laufen (ADR-035). Direkter `HttpClient`-Zugriff für externe Dienste ist VERBOTEN.
```csharp
// RICHTIG — über zentralen Service
public class SteiermarkGisService
{
    private readonly IExternalCommunicationService _comm;

    public async Task<ParcelData> QueryParcelAsync(double east, double north, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, BuildGisUrl(east, north));
        var response = await _comm.SendAsync("gis_stmk", request, DataClassification.ClassA, "Grundstück laden", ct);
        // ...
    }
}

// FALSCH — direkter HttpClient
public class SteiermarkGisService
{
    private readonly HttpClient _http;
    public async Task<ParcelData> QueryParcelAsync(...)
    {
        var response = await _http.GetAsync(url);  // VERBOTEN — kein Audit, kein Kill-Switch
    }
}
```

### 17.3 Datenklassifizierung — Bei jedem externen Call

Jeder externe Call bekommt eine `DataClassification`:
```csharp
// Klasse A — keine Personendaten (Koordinaten, Wetter)
await _comm.SendAsync("wetter", request, DataClassification.ClassA, "Wettervorhersage", ct);

// Klasse B — personenbezogene Daten (Kontakte, Mitarbeiter)
await _comm.SendAsync("task_mgmt", request, DataClassification.ClassB, "Materialbestellung", ct);

// Klasse C — sensible Drittdaten (LVs, Bescheide) → Default: BLOCKIERT
await _comm.SendAsync("ki", request, DataClassification.ClassC, "LV-Analyse", ct);
```

### 17.4 API-Keys — Sichere Speicherung
```csharp
// RICHTIG — DPAPI (Windows Data Protection API)
var key = ProtectedData.Protect(Encoding.UTF8.GetBytes(apiKey),
    null, DataProtectionScope.CurrentUser);

// FALSCH — API-Keys in Klartext
settings.OpenAiApiKey = "sk-abc123...";  // NIEMALS in settings.json
_logger.LogInformation("API Key: {Key}", apiKey);  // NIEMALS loggen
```

**VERBOTEN:** API-Keys in settings.json, registry.json, Git, Logs oder Quellcode.

### 17.5 Privacy Policy — Austauschbar per DI

Die Datenschutz-Logik wird über `IPrivacyPolicy` gesteuert (ADR-036). Der aktive Modus kommt aus der Lizenz, NICHT aus settings.json.
```csharp
// In App.xaml.cs — DI Registrierung
if (license.RequiresStrictCompliance)
    services.AddSingleton<IPrivacyPolicy, StrictPrivacyPolicy>();
else
    services.AddSingleton<IPrivacyPolicy, RelaxedPrivacyPolicy>();

// FALSCH — über Settings steuerbar
if (settings.PrivacyMode == "relaxed")  // VERBOTEN — User kann DSGVO umgehen
```

### 17.6 registry.json — Whitelist

Neue Felder in `registry.json` müssen gegen die Whitelist (DSVGO-Architektur Kap. 9.3) geprüft werden. Klasse-B-Felder nur wenn für VBA-Kompatibilität zwingend nötig.

### 17.7 Datenschutz-Logik — Trennung von Policy und UI

Datenschutz-Entscheidungen werden NIEMALS im ViewModel getroffen. ViewModels dürfen nur Policy-Entscheidungen anfordern, darstellen und User-Bestätigungen zurückmelden.
```csharp
// RICHTIG — ViewModel fragt Policy, zeigt Ergebnis
var decision = _policy.Evaluate(module, classification, purpose);
if (decision.RequiresUserConfirmation)
{
    // Dialog anzeigen, User-Antwort zurückmelden
}

// FALSCH — ViewModel entscheidet selbst
if (classification == DataClassification.ClassC)
{
    ShowWarning("Klasse C!"); // ViewModel trifft Policy-Entscheidung
}
```
## 18. WPF Code-Behind Dialoge (v0.24.2)

### 18.1 Resource-Vererbung (PFLICHT)

Programmatisch erstellte Fenster (z.B. in ShowPartEditDialog, ShowLevelEditDialog) erben NICHT die impliziten Styles aus dem XAML-Dialog. Ohne explizite Vererbung haben ComboBoxen, TextBoxen etc. kein Dark Theme.

```csharp
var w = new Window { ... };
// PFLICHT: Dark Theme Styles vom Owner-Dialog vererben
foreach (var key in Resources.Keys)
    w.Resources[key] = Resources[key];
```

### 18.2 Toggle-Switch statt CheckBox

Für An/Aus-Schalter im Dark Theme einen Custom Toggle verwenden (Border mit CornerRadius + Ellipse), nicht die WPF-CheckBox.

### 18.3 ShowDarkConfirm statt MessageBox

Für Ja/Nein-Fragen in Code-Behind-Dialogen eigenen Dark-Theme-Dialog verwenden. `MessageBox.Show()` ist weiß und passt nicht zum Dark Theme.

### 18.4 VERBOTEN

- `MessageBox.Show()` in Code-behind Dialogen innerhalb von Dark-Theme-Fenstern
- WPF-CheckBox für Toggle-Schalter (visuell inkompatibel)
- Code-behind Fenster ohne Resource-Vererbung


**Regel:** Kein ViewModel, kein Dialog-Code-Behind darf prüfen ob eine Datenklasse erlaubt ist oder ob eine Warnung angezeigt werden soll. Das entscheidet ausschließlich `IPrivacyPolicy`. Das ViewModel visualisiert nur die `PolicyDecision`.

---

## 19. Sync-Vorbereitung & Datenkonventionen (PFLICHT ab v0.24.3)

> **Gültig ab sofort** für alle neuen Tabellen und Services.
> Bestehende Tabellen werden bei nächster Migration nachgerüstet.
> Siehe ADR-050 (SoR je Modus) und ADR-051 (Local-First).

### 19.1 Pflicht-Spalten für neue fachliche Tabellen

Jede neue Tabelle die fachliche Daten speichert bekommt:

```sql
id                  TEXT PRIMARY KEY,  -- ULID, clientseitig erzeugt
created_at          TEXT NOT NULL,     -- UTC
created_by          TEXT,              -- Modus A: settings.json, Modus C: JWT
last_modified_at    TEXT NOT NULL,     -- UTC
last_modified_by    TEXT,
sync_version        INTEGER NOT NULL DEFAULT 0,
is_deleted          INTEGER NOT NULL DEFAULT 0
```

### 19.2 Zeitstempel — nur UTC

```csharp
// RICHTIG
DateTime.UtcNow

// VERBOTEN
DateTime.Now
```

Alle `created_at`, `last_modified_at` und sonstige Zeitstempel
werden als UTC gespeichert. Anzeige in lokaler Zeit erfolgt
ausschließlich in der UI-Schicht.

### 19.3 Soft Delete

Sync-relevante Tabellen verwenden Soft Delete:

```csharp
// RICHTIG
entity.IsDeleted = true;
entity.LastModifiedAt = DateTime.UtcNow;
entity.LastModifiedBy = userId;
entity.SyncVersion++;
await _repository.SaveAsync(entity);

// VERBOTEN für sync-relevante Tabellen
DELETE FROM table WHERE id = @id
```

### 19.4 IDs — ULID, clientseitig erzeugt

IDs werden immer vom Client erzeugt, nie vom Server.
ULID statt auto-increment für alle neuen fachlichen Tabellen.

### 19.5 Writes nur über Application Services

ViewModels schreiben nie direkt in die DB.
Alle Writes laufen über Services die Metadaten setzen:

```csharp
public async Task UpdateAsync(Entity entity, string userId)
{
    entity.LastModifiedBy = userId;
    entity.LastModifiedAt = DateTime.UtcNow;
    entity.SyncVersion++;
    await _repository.SaveAsync(entity);
}
```

### 19.6 Lokaler Benutzerkontext (IUserContext)

Benutzeridentität läuft über `IUserContext` (Domain-Interface):

```csharp
public interface IUserContext
{
    string UserId { get; }
    string DisplayName { get; }
    UserContextSource Source { get; }
}

public enum UserContextSource { Local, Server }
```

**Modus A:** `LocalUserContext` liest aus `settings.json`:

```json
{
  "localUserId": "Surface7\\herbe",
  "localUserName": "Herbert"
}
```

- `localUserId`: automatisch aus `Environment.MachineName\Environment.UserName`, nur intern/Debug
- `localUserName`: lesbarer Anzeigename, vom User in den Einstellungen pflegbar
- `created_by` / `last_modified_by` = immer `IUserContext.DisplayName` (lesbarer Name)

**Modus C:** `JwtUserContext` liest aus JWT-Claims.

> **Wichtig:** `created_by`/`last_modified_by` sind Anzeige-/Auditnamen,
> keine belastbaren Authentitätsnachweise. Echte Authentifizierung
> existiert erst in Modus C (Server).

**NICHT einführen in Modus A:**
- `IsAuthenticated` (semantisch schief ohne echte Auth)
- `localUserId` als Pflicht-Spalte in DB-Tabellen
- E-Mail in settings.json
- Lokale User-Tabelle / Login-Dialog / Passwort

### 19.7 HttpClient

Kein direkter `HttpClient` irgendwo im Code.
Server-Kommunikation nur über `IExternalCommunicationService` (ADR-035).

## Zusammenfassung — Die 10 wichtigsten Regeln

1. **PascalCase** für Klassen/Methoden, **camelCase** für Variablen, **_camelCase** für private Felder
2. **Allman-Style** Klammern (jede Klammer eigene Zeile)
3. **Immer Klammern** auch bei einzelnen Anweisungen
4. **Eine Klasse pro Datei**, maximal 300-400 Zeilen
5. **Kleine Methoden** (max 30-40 Zeilen), eine Aufgabe pro Methode
6. **Return Early** mit Guard Clauses statt tiefer Verschachtelung
7. **Spezifische Exceptions** fangen, nie leere catch-Blöcke
8. **MVVM strikt** einhalten: View hat keinen Code, ViewModel kennt keine View
9. **async/await** für I/O, nie `.Result` oder `.Wait()`
10. **XML-Dokumentation** auf allen public Members

---

*Diese Standards gelten für alle C#/WPF-Dateien im BauProjektManager-Projekt.*  
*Claude verwendet diese Standards automatisch beim Code-Generieren.*  
*Für UI-Design-Regeln (Farben, Spacing, Komponenten-Verhalten) siehe `../Referenz/UI_UX_Guidelines.md`.*