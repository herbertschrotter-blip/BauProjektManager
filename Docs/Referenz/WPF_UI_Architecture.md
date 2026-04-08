# BauProjektManager — WPF UI Architecture

**Version:** 2.0  
**Datum:** 04.04.2026  
**Bezug:** UI_UX_Guidelines.md v2.1 (Design-Regeln), CODING_STANDARDS.md Kap. 10+17 (Code-Regeln), ADR-028 (Theme-System)

**Verwandte Dokumente:**
- [UI_UX_Guidelines.md](UI_UX_Guidelines.md) — Design-Token, Screen States, Feedback-Matrix, Komponenten
- [CODING_STANDARDS.md](../Kern/CODING_STANDARDS.md) — Kap. 10 XAML-Konventionen, Kap. 17.7 Datenschutz nie im ViewModel
- [ADR-028](ADR.md) — Theme-System mit 7 Resource Dictionaries
- [DSVGO-Architektur.md](../Kern/DSVGO-Architektur.md) — Kap. 13 Datenschutz-GUI

---

## 1. Ziel

Dieses Dokument beschreibt den **technischen Aufbau** der UI-Schicht: Wie Views, Styles, Themes und Basis-Komponenten organisiert sind. Es ist die Brücke zwischen den Design-Regeln (UI_UX_Guidelines.md) und dem tatsächlichen Code.

**Abgrenzung:**
- **UI_UX_Guidelines.md** = WAS die UI zeigt und wie sie sich verhält (Design-Sprache)
- **Dieses Dokument** = WIE die UI technisch gebaut wird (WPF-Umsetzung)
- **CODING_STANDARDS.md** = WIE der Code strukturiert ist (C#/XAML-Regeln)

---

## 2. Projektstruktur (UI-relevant)

```
BauProjektManager.App/
├── App.xaml                          ← ResourceDictionary-Merge
├── App.xaml.cs                       ← DI Setup, Startup
├── MainWindow.xaml                   ← Shell (Sidebar + Content + Statusleiste)
├── MainWindow.xaml.cs
│
├── Themes/                           ← ZENTRAL: Alle globalen Styles und Token
│   ├── Colors.xaml                   ← Farb-Token (bg-base, accent-primary etc.)
│   ├── Typography.xaml               ← Schriftgrößen, -gewichte
│   ├── Buttons.xaml                  ← Button-Styles (Primary, Secondary, Danger, Ghost)
│   ├── Inputs.xaml                   ← TextBox, ComboBox, DatePicker, CheckBox Styles
│   ├── DataGrid.xaml                 ← Tabellen-Styles (Header, Zeilen, Hover)
│   ├── Tabs.xaml                     ← TabControl + TabItem Styles
│   ├── Dialogs.xaml                  ← Dialog-Basis, ContextMenu, Cards, Tooltips
│   └── Icons.xaml                    ← Zentrale Icon-Registry (Emoji → Fluent Icons)
│
├── Controls/                         ← Shell-only Basis-Komponenten
│   ├── BpmToast.xaml                 ← Toast-Benachrichtigungen (🎯)
│   ├── BpmToast.xaml.cs
│   ├── BpmBanner.xaml                ← Modus-/Hinweisbanner (🎯)
│   ├── BpmBanner.xaml.cs
│   ├── BpmStatusBar.xaml             ← Statusleiste
│   ├── BpmStatusBar.xaml.cs
│   ├── BpmToolbar.xaml               ← Modul-spezifische Toolbar (🎯)
│   ├── BpmToolbar.xaml.cs
│   ├── BpmBreadcrumb.xaml            ← Breadcrumb-Navigation (🎯)
│   └── BpmBreadcrumb.xaml.cs
│
├── Converters/                       ← Globale Value Converters
│   ├── StatusToColorConverter.cs
│   ├── BoolToVisibilityConverter.cs
│   ├── ViewStateToVisibilityConverter.cs
│   └── DateFormatConverter.cs
│
└── Assets/                           ← Icons, Bilder
    └── Icons/

BauProjektManager.Settings/
├── Views/
│   ├── SettingsView.xaml             ← Einstellungen-Modul
│   ├── SettingsView.xaml.cs
│   ├── ProjectEditDialog.xaml        ← Projekt bearbeiten (5 Tabs)
│   └── ProjectEditDialog.xaml.cs
└── ViewModels/
    ├── SettingsViewModel.cs
    └── ProjectEditViewModel.cs

BauProjektManager.PlanManager/
├── Views/
│   ├── PlanManagerView.xaml
│   ├── ImportPreviewDialog.xaml
│   └── ...
└── ViewModels/
    ├── PlanManagerViewModel.cs
    └── ...
```

### 2.1 Regeln

- **Themes/** liegt im `.App`-Projekt → globale Styles, für alle Module sichtbar über App.xaml Merge
- **Controls/** liegt im `.App`-Projekt → **Shell-only** (BpmToast, BpmBanner, BpmBreadcrumb, BpmToolbar, BpmStatusBar). Diese Controls sind **nicht für Module zugänglich** — Module referenzieren App nicht (ADR-006). Module lösen Feedback über Events/Messaging aus, die Shell stellt es dar.
- **Views/** liegen in den jeweiligen Modul-Projekten (`.Settings`, `.PlanManager`)
- Modul-spezifische Styles gehören NICHT in Themes/ → sie bleiben lokal im Modul
- Globale Styles (Button, DataGrid, Tabs, Inputs, Dialogs) gehören in Themes/
- Falls Module gemeinsame UI-Controls brauchen → eigenes Shared-UI-Projekt erstellen (erst bei realem Bedarf, aktuell nicht nötig)

---

## 3. Resource Dictionary Architektur

### 3.1 Übersicht: 8 ResourceDictionaries (ADR-028, ADR-044)

| Dictionary | Verantwortung | Abhängigkeiten |
|---|---|---|
| **Colors.xaml** | Farb-Token als SolidColorBrush + Color | Keine |
| **Typography.xaml** | Schriftgrößen, -gewichte als Doubles + Styles | Colors.xaml |
| **Buttons.xaml** | Button-Varianten (Primary, Secondary, Danger, Ghost, Nav) | Colors.xaml, Typography.xaml |
| **Inputs.xaml** | TextBox, ComboBox, DatePicker, CheckBox | Colors.xaml, Typography.xaml |
| **DataGrid.xaml** | Header, Row, Cell, Zebra-Variante | Colors.xaml, Typography.xaml |
| **Tabs.xaml** | TabControl, TabItem mit Unterstrich-Style | Colors.xaml, Typography.xaml |
| **Dialogs.xaml** | Dialog-Basis (Header/Footer/Overlay), Cards, Tooltips, Separatoren, ContextMenu/MenuItem Styles | Colors.xaml, Typography.xaml |
| **Icons.xaml** | Zentrale Icon-Registry — alle UI-Icons als `sys:String` Resources. Provisorisch Emoji, später Segoe Fluent Icons. Nutzung: `Content="{StaticResource IconFolder}"` oder `<Run Text="{StaticResource IconFolderOpen}"/>` | Keine |

**Historie:** Ursprünglich 5 Dictionaries (ADR-028 v1). Erweitert um Inputs.xaml und Tabs.xaml (v0.16.0), dann Icons.xaml (v0.23.0) für zentrale Icon-Verwaltung. ADR-028 auf 8 nachgezogen.

### 3.2 Merge-Reihenfolge in App.xaml

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- 1. Farben zuerst (werden von allen anderen referenziert) -->
            <ResourceDictionary Source="Themes/Colors.xaml"/>
            <!-- 2. Icons -->
            <ResourceDictionary Source="Themes/Icons.xaml"/>
            <!-- 3. Typografie -->
            <ResourceDictionary Source="Themes/Typography.xaml"/>
            <!-- 3. Komponenten-Styles (referenzieren Farben + Typo) -->
            <ResourceDictionary Source="Themes/Buttons.xaml"/>
            <ResourceDictionary Source="Themes/Inputs.xaml"/>
            <ResourceDictionary Source="Themes/DataGrid.xaml"/>
            <ResourceDictionary Source="Themes/Tabs.xaml"/>
            <ResourceDictionary Source="Themes/Dialogs.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### 3.3 Naming-Konvention für Keys

| Typ | Prefix | Beispiel |
|-----|--------|---------|
| Farbe (SolidColorBrush) | `Bpm` | `BpmBgBase`, `BpmAccentPrimary`, `BpmTextSecondary` |
| Farbe (Color) | `BpmColor` | `BpmColorAccentPrimary`, `BpmColorError` |
| Style | `Bpm<Typ>` | `BpmButtonPrimary`, `BpmDataGridDefault` |
| Icon (String) | `Icon<Kategorie><Objekt>` | `IconFolder`, `IconActionDelete`, `IconNavPlans` |
| ControlTemplate | `Bpm<Typ>Template` | `BpmButtonTemplate`, `BpmTabItemTemplate` |
| Thickness/Margin | `Bpm<Kontext>` | `BpmDialogPadding`, `BpmCardMargin` |
| Double (Schriftgröße) | `BpmFontSize<Stufe>` | `BpmFontSizeH1`, `BpmFontSizeBody` |
| Double (Spacing) | `BpmSpace<Stufe>` | `BpmSpaceMd`, `BpmSpaceXl` |

**Spacing-Regel:** `BpmSpaceXs` bis `BpmSpaceXxl` sind die Basis-Doubles (4, 8, 16, 24, 32, 48). Semantische Thickness-Ressourcen (z.B. `BpmDialogPadding`, `BpmCardMargin`) werden nur dort erstellt wo sie häufig wiederverwendet werden — nicht für jeden Einzelfall.

### 3.4 Referenzierung

```xml
<!-- RICHTIG — StaticResource für Styles und Farben (V1 Standard) -->
<Button Style="{StaticResource BpmButtonPrimary}"/>
<Border Background="{StaticResource BpmBgSurface}"/>

<!-- RICHTIG — DynamicResource nur wenn sich Wert zur Laufzeit ändern kann -->
<!-- (z.B. Theme-Wechsel Dark→Light, erst nach V1) -->
<Border Background="{DynamicResource BpmBgBase}"/>

<!-- FALSCH — Farben direkt in XAML -->
<Border Background="#1E1E1E"/>
```

**Entscheidung:** Für V1 `StaticResource` verwenden (kein Laufzeit-Theme-Wechsel). Wenn Light Theme kommt → auf `DynamicResource` umstellen.

---

## 4. Token → WPF-Key Mapping

Verbindliche Brücke zwischen Design-Token (UI_UX_Guidelines.md) und WPF Resource Keys.

### 4.1 Farb-Token

| Design-Token | WPF Resource Key (Brush) | WPF Color Key | Wert (Dark) |
|---|---|---|---|
| `bg-base` | `BpmBgBase` | `BpmColorBgBase` | `#1E1E1E` |
| `bg-surface` | `BpmBgSurface` | `BpmColorBgSurface` | `#252526` |
| `bg-elevated` | `BpmBgElevated` | `BpmColorBgElevated` | `#2D2D30` |
| `bg-hover` | `BpmBgHover` | `BpmColorBgHover` | `#37373D` |
| `bg-active` | `BpmBgActive` | `BpmColorBgActive` | `#04395E` |
| `bg-input` | `BpmBgInput` | `BpmColorBgInput` | `#3C3C3C` |
| `text-primary` | `BpmTextPrimary` | `BpmColorTextPrimary` | `#CCCCCC` |
| `text-secondary` | `BpmTextSecondary` | `BpmColorTextSecondary` | `#858585` |
| `text-bright` | `BpmTextBright` | `BpmColorTextBright` | `#FFFFFF` |
| `text-on-accent` | `BpmTextOnAccent` | — | `#FFFFFF` |
| `accent-primary` | `BpmAccentPrimary` | `BpmColorAccentPrimary` | `#0078D4` |
| `accent-hover` | `BpmAccentHover` | `BpmColorAccentHover` | `#1A8AD4` |
| `accent-pressed` | `BpmAccentPressed` | `BpmColorAccentPressed` | `#005A9E` |
| `success` | `BpmSuccess` | `BpmColorSuccess` | `#4EC94E` |
| `warning` | `BpmWarning` | `BpmColorWarning` | `#F0AD4E` |
| `error` | `BpmError` | `BpmColorError` | `#F44747` |
| `info` | `BpmInfo` | `BpmColorInfo` | `#3794FF` |
| `border-default` | `BpmBorderDefault` | — | `#3C3C3C` |
| `border-focus` | `BpmBorderFocus` | — | `#0078D4` |
| `border-subtle` | `BpmBorderSubtle` | — | `#2D2D30` |

**Brush vs. Color:** Brush-Keys (`BpmBgBase`) für direkte Verwendung in XAML. Color-Keys (`BpmColorBgBase`) nur wo WPF eine `Color` erwartet (z.B. Animationen, GradientStops). Nicht für jeden Brush einen separaten Color-Key anlegen — nur bei Bedarf.

### 4.2 Typografie-Token

| Design-Token | WPF Resource Key | Typ | Wert |
|---|---|---|---|
| `heading-1` | `BpmFontSizeH1` | Double | `24` |
| `heading-2` | `BpmFontSizeH2` | Double | `18` |
| `heading-3` | `BpmFontSizeH3` | Double | `15` |
| `body` | `BpmFontSizeBody` | Double | `13` |
| `body-bold` | `BpmFontSizeBody` + FontWeight SemiBold | Double + Style | `13` |
| `label` | `BpmFontSizeLabel` | Double | `12` |
| `small` | `BpmFontSizeSmall` | Double | `11` |
| `caption` | `BpmFontSizeCaption` | Double | `10` |

### 4.3 Spacing-Token

| Design-Token | WPF Resource Key | Typ | Wert |
|---|---|---|---|
| `space-xs` | `BpmSpaceXs` | Double | `4` |
| `space-sm` | `BpmSpaceSm` | Double | `8` |
| `space-md` | `BpmSpaceMd` | Double | `16` |
| `space-lg` | `BpmSpaceLg` | Double | `24` |
| `space-xl` | `BpmSpaceXl` | Double | `32` |
| `space-xxl` | `BpmSpaceXxl` | Double | `48` |

---

## 5. Shell-Architektur (MainWindow)

### 5.1 Aufbau

```
┌──────────────────────────────────────────────────────────────┐
│ BpmBreadcrumb (28px) 🎯                                      │
├────────┬─────────────────────────────────────────────────────┤
│        │ BpmToolbar (40px) 🎯                                │
│ Side-  ├─────────────────────────────────────────────────────┤
│ bar    │                                                     │
│ (220px)│ ContentControl (wechselt pro Modul)                 │
│        │                                                     │
│        │                                                     │
├────────┴─────────────────────────────────────────────────────┤
│ BpmStatusBar (24px)                                          │
└──────────────────────────────────────────────────────────────┘

🎯 = Zielstandard, kommt mit UI-Refresh
```

### 5.2 Grid-Layout

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="220"/>  <!-- Sidebar -->
        <ColumnDefinition Width="*"/>    <!-- Content -->
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
        <RowDefinition Height="28"/>     <!-- Breadcrumb (🎯) -->
        <RowDefinition Height="40"/>     <!-- Toolbar (🎯) -->
        <RowDefinition Height="*"/>      <!-- Content -->
        <RowDefinition Height="24"/>     <!-- Statusleiste -->
    </Grid.RowDefinitions>

    <!-- Sidebar: Zeile 0-3, Spalte 0 -->
    <!-- Breadcrumb: Zeile 0, Spalte 1 (🎯) -->
    <!-- Toolbar: Zeile 1, Spalte 1 (🎯) -->
    <!-- Content: Zeile 2, Spalte 1 -->
    <!-- Statusleiste: Zeile 3, Spalte 0-1 -->
</Grid>
```

### 5.3 Navigation (V1-Übergang)

**Aktueller Stand:** Direkte View-Instanziierung im Code-Behind. Für 2 Module pragmatisch ausreichend.

```csharp
// MainWindow.xaml.cs
private void NavigateTo(string moduleId)
{
    ContentArea.Content = moduleId switch
    {
        "settings" => new SettingsView(),
        "planManager" => new PlanManagerView(),
        _ => throw new ArgumentException($"Unknown module: {moduleId}")
    };
}
```

**V1-Übergang:** Bei >5 Modulen auf DI-basierte View-Auflösung umstellen (INavigationService oder ViewFactory mit DI-Container). Aktuell nicht nötig.

---

## 6. MVVM-Regeln

### 6.1 Stack

BPM verwendet **CommunityToolkit.Mvvm** (ADR-015). Keine eigene MVVM-Infrastruktur.

```csharp
// ViewModel-Basis: ObservableObject aus CommunityToolkit.Mvvm
public partial class ProjectListViewModel : ObservableObject
{
    [ObservableProperty]
    private ViewState _viewState = ViewState.Loading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [RelayCommand]
    private async Task LoadProjectsAsync()
    {
        // ...
    }
}
```

### 6.2 Code-Behind-Regel

**Kein fachlicher Code im Code-Behind.** Erlaubt sind ausschließlich:
- `InitializeComponent()`
- DataContext-Zuweisung
- Dialog-Öffnung mit Owner-Zuweisung
- Fokus-Steuerung (z.B. erstes Fehlerfeld fokussieren)
- Window-Events für Shell-Orchestrierung (Loaded, Closing)

```csharp
// ERLAUBT — Dialog-Öffnung im Code-Behind
private void OnEditProject(object sender, RoutedEventArgs e)
{
    var dialog = new ProjectEditDialog();
    dialog.Owner = Window.GetWindow(this);
    dialog.DataContext = new ProjectEditViewModel(_selectedProject);
    if (dialog.ShowDialog() == true)
    {
        _viewModel.ReloadProjects();
    }
}

// VERBOTEN — Fachliche Logik im Code-Behind
private void OnSave(object sender, RoutedEventArgs e)
{
    _database.SaveProject(project); // ← gehört ins ViewModel
}
```

### 6.3 Datenschutz-Regel (CODING_STANDARDS 17.7)

Datenschutz-Entscheidungen (Klasse B/C blockieren, Consent anfordern, Audit loggen) gehören **nie** ins ViewModel. Die `IPrivacyPolicy` entscheidet, das ViewModel visualisiert nur das Ergebnis. Siehe [DSVGO-Architektur.md Kap. 13](../Kern/DSVGO-Architektur.md).

---

## 7. Screen State Pattern

### 7.1 Zwei Zustandsebenen

| Ebene | Enum/Flags | Werte | Verantwortung |
|---|---|---|---|
| **ViewState** (Seite) | Enum | Loading, Data, Empty, Error, Offline | Welche Hauptansicht wird gezeigt? |
| **Operation Flags** | Einzelne Properties | IsDirty, IsReadOnly, HasPartialResult | Ergänzende Zustände über der Hauptansicht |

**Warum getrennt?** ViewState bestimmt welches Panel sichtbar ist (nur eines gleichzeitig). Operation Flags sind orthogonal — eine Seite kann `Data` sein UND `IsDirty = true` haben.

### 7.2 ViewState Enum

```csharp
public enum ViewState
{
    Loading,    // Daten werden geladen
    Data,       // Daten vorhanden, normale Ansicht
    Empty,      // Keine Daten, Handlungsaufforderung
    Error,      // Fehler beim Laden/Speichern
    Offline     // Internet nötig aber nicht vorhanden
}
```

### 7.3 Operation Flags

```csharp
public partial class ProjectEditViewModel : ObservableObject
{
    [ObservableProperty]
    private ViewState _viewState = ViewState.Loading;

    [ObservableProperty]
    private bool _isDirty;        // Ungespeicherte Änderungen

    [ObservableProperty]
    private bool _isReadOnly;     // Bearbeitung nicht möglich (Lock, Rolle, Modul deaktiviert)

    [ObservableProperty]
    private bool _hasPartialResult;  // Teilweise erfolgreich (Import: 7/10)

    [ObservableProperty]
    private string _userFriendlyError = string.Empty;
}
```

### 7.4 Fehlerbehandlung (kein ex.Message zum User)

```csharp
// RICHTIG — Benutzerfreundliche Fehlermeldung + technisches Logging
catch (Exception ex)
{
    _logger.LogError(ex, "Projekt {ProjectId} konnte nicht geladen werden", projectId);
    UserFriendlyError = "Projekt konnte nicht geladen werden. Bitte erneut versuchen.";
    ViewState = ViewState.Error;
}

// FALSCH — Technische Exception direkt anzeigen
catch (Exception ex)
{
    ErrorMessage = ex.Message;  // ← VERBOTEN: zeigt Stack Traces, SQL-Fehler etc.
}
```

### 7.5 ViewState in XAML

```xml
<Grid>
    <!-- Loading -->
    <StackPanel Visibility="{Binding ViewState, Converter={StaticResource ViewStateToVisibility},
                             ConverterParameter=Loading}">
        <TextBlock Text="⟳ Lade Projekte..." Style="{StaticResource BpmBodyText}"
                   HorizontalAlignment="Center" VerticalAlignment="Center"/>
    </StackPanel>

    <!-- Data (normale Ansicht) -->
    <DataGrid Visibility="{Binding ViewState, Converter={StaticResource ViewStateToVisibility},
                           ConverterParameter=Data}"
              ItemsSource="{Binding Projects}" .../>

    <!-- Empty -->
    <StackPanel Visibility="{Binding ViewState, Converter={StaticResource ViewStateToVisibility},
                             ConverterParameter=Empty}"
                HorizontalAlignment="Center" VerticalAlignment="Center">
        <TextBlock Text="📁 Noch keine Projekte angelegt"/>
        <Button Content="+ Neues Projekt anlegen"
                Style="{StaticResource BpmButtonPrimary}"
                Command="{Binding AddProjectCommand}"/>
    </StackPanel>

    <!-- Error -->
    <StackPanel Visibility="{Binding ViewState, Converter={StaticResource ViewStateToVisibility},
                             ConverterParameter=Error}"
                HorizontalAlignment="Center" VerticalAlignment="Center">
        <TextBlock Text="❌ Fehler beim Laden"/>
        <TextBlock Text="{Binding UserFriendlyError}" Style="{StaticResource BpmSmallText}"/>
        <Button Content="Erneut versuchen"
                Style="{StaticResource BpmButtonSecondary}"
                Command="{Binding RetryCommand}"/>
    </StackPanel>
</Grid>

<!-- Dirty-Indicator (orthogonal zum ViewState) -->
<TextBlock Text="*" Visibility="{Binding IsDirty, Converter={StaticResource BoolToVisibility}}"
           Foreground="{StaticResource BpmWarning}"/>

<!-- Read-only Banner (orthogonal) -->
<Border Visibility="{Binding IsReadOnly, Converter={StaticResource BoolToVisibility}}"
        Background="{StaticResource BpmWarning}" Padding="8,4">
    <TextBlock Text="🔒 Nur-Lesen-Modus — anderer Benutzer bearbeitet"
               Foreground="{StaticResource BpmTextBright}"/>
</Border>
```

---

## 8. Feedback-Infrastruktur

Technische Einordnung der Feedback-Matrix aus UI_UX_Guidelines.md Kap. 18.

| Feedback-Typ | WPF-Komponente | Wo | Status |
|---|---|---|---|
| **Inline-Validierung** | Border + TextBlock unter Feld | In Inputs.xaml (Style) | ✅ Machbar |
| **Toast** | BpmToast (UserControl in App/Controls/) | Shell-only, Overlay über Content | 🎯 Zielstandard |
| **Modal-Dialog** | Window mit BpmDialogBase Style | Dialogs.xaml | ✅ Vorhanden |
| **Error-Dialog** | Window mit Details-Expander | Dialogs.xaml (neuer Style BpmErrorDialog) | 🎯 |
| **Statusleiste** | BpmStatusBar (TextBlock in MainWindow) | Shell | ✅ Vorhanden |
| **Banner** | BpmBanner (UserControl in App/Controls/) | Shell-only, oben im Content | 🎯 Zielstandard |

**Shell-only Controls:** Toast und Banner leben im App-Projekt. Module können sie nicht direkt ansprechen (ADR-006). Stattdessen: Module lösen Events aus (z.B. `Messenger.Send(new ToastMessage(...))`), die Shell reagiert darauf.

### 8.1 Bis UI-Refresh (V1-Übergangslösung)

| Feedback-Typ | V1-Lösung |
|---|---|
| Erfolgsmeldung | Statusleiste-Text: "Projekt gespeichert ✓ 14:32" |
| Destruktive Bestätigung | Custom Dialog (Window), kein MessageBox.Show() |
| Kritischer Fehler | Custom Error-Dialog mit Details-Expander |
| Modus-Hinweis | TextBlock-Banner oben in der View (lokal pro Modul) |

---

## 9. Dialog-Architektur

### 9.1 Dialog-Basis

Alle Dialoge folgen demselben Muster:

```xml
<Window Style="{StaticResource BpmDialogBase}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- Header -->
            <RowDefinition Height="*"/>     <!-- Content -->
            <RowDefinition Height="Auto"/>  <!-- Footer (Buttons) -->
        </Grid.RowDefinitions>

        <!-- Header: Titel + Schließen -->
        <Border Grid.Row="0" Background="{StaticResource BpmBgElevated}">
            <Grid>
                <TextBlock Text="Dialog-Titel" Style="{StaticResource BpmHeading3}"/>
                <Button Content="✕" HorizontalAlignment="Right"
                        Style="{StaticResource BpmButtonGhost}"
                        Command="{Binding CloseCommand}"/>
            </Grid>
        </Border>

        <!-- Content: Modul-spezifisch -->
        <ScrollViewer Grid.Row="1">
            <!-- Formular, Tabellen, Tabs etc. -->
        </ScrollViewer>

        <!-- Footer: Buttons -->
        <Border Grid.Row="2" Background="{StaticResource BpmBgSurface}">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right"
                        Margin="{StaticResource BpmDialogPadding}">
                <Button Content="Abbrechen" Style="{StaticResource BpmButtonSecondary}"
                        Command="{Binding CancelCommand}" Margin="0,0,8,0"/>
                <Button Content="Speichern" Style="{StaticResource BpmButtonPrimary}"
                        Command="{Binding SaveCommand}"/>
            </StackPanel>
        </Border>
    </Grid>
</Window>
```

### 9.2 Schließ-Regeln (differenziert)

| Dialog-Typ | ✕ Button | Escape | Overlay-Klick |
|---|---|---|---|
| **Info-/Bestätigungsdialog** | ✅ Schließen | ✅ Schließen | ✅ Erlaubt |
| **Bearbeitungsdialog (Formular)** | ✅ mit Dirty-Check | ✅ mit Dirty-Check | ❌ VERBOTEN |

**Dirty-Check bei Escape/✕:** Wenn ungespeicherte Änderungen vorhanden → Bestätigungsdialog: "Änderungen verwerfen?"

### 9.3 Mehrtab-Validierung

Für Dialoge mit mehreren Tabs (z.B. ProjectEditDialog mit 5 Tabs):

1. **Bei Speichern-Versuch:** Alle Tabs validieren, nicht nur der aktive
2. **Tab-Fehlermarker:** Tabs mit Fehlern bekommen ⚠️ im Header
3. **Fehlerzusammenfassung:** Oben im Dialog: "2 Fehler: Tab Stammdaten — Projektname fehlt"
4. **Fokusnavigation:** Klick auf Fehlermeldung → wechselt zum Tab und fokussiert Feld
5. **Speichern blockiert:** Solange Pflichtfehler vorhanden, SaveCommand.CanExecute = false

```csharp
// ViewModel-Beispiel für Tab-Fehlermarker
[ObservableProperty]
private bool _hasStammdatenErrors;

[ObservableProperty]
private bool _hasBauwerkErrors;

[ObservableProperty]
private string _validationSummary = string.Empty;

private void ValidateAllTabs()
{
    var errors = new List<string>();
    HasStammdatenErrors = !ValidateStammdaten(errors);
    HasBauwerkErrors = !ValidateBauwerk(errors);
    ValidationSummary = errors.Count > 0
        ? $"{errors.Count} Fehler: {string.Join(", ", errors)}"
        : string.Empty;
}
```

---

## 10. Datenformatierung

### 10.1 CultureInfo Setup

```csharp
// App.xaml.cs — beim Start setzen
protected override void OnStartup(StartupEventArgs e)
{
    var culture = CultureInfo.GetCultureInfo("de-AT");
    Thread.CurrentThread.CurrentCulture = culture;
    Thread.CurrentThread.CurrentUICulture = culture;
    FrameworkElement.LanguageProperty.OverrideMetadata(
        typeof(FrameworkElement),
        new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.Name)));
}
```

### 10.2 Converter für Einheiten

```csharp
public class UnitFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d && parameter is string unit)
            return $"{d.ToString("N2", CultureInfo.GetCultureInfo("de-AT"))} {unit}";
        return value;
    }
    // ...
}
// Verwendung: Text="{Binding PlannedQuantity, Converter={StaticResource UnitFormat}, ConverterParameter=m²}"
// Ergebnis: "198,00 m²"
```

---

## 11. Responsive-Regeln

Technische Umsetzung der Auflösungsregeln aus UI_UX_Guidelines.md Kap. 17.

### 11.1 Grundregeln

| Auflösung | Verhalten |
|---|---|
| **≥1920×1080** | Volle Breite, 2-Spalten-Layouts in Dialogen |
| **1600–1919** | Volle Funktionalität, kompaktere Margins |
| **1366–1599** | ScrollViewer um Content, 1-Spalten-Fallback in engen Dialogen |
| **<1366** | Nicht offiziell unterstützt |

### 11.2 Technische Leitplanken

- **Sidebar:** Feste 220px. Bei <1600px perspektivisch Icon-only Modus (🎯 Zielstandard)
- **Dialoge:** `MinWidth`/`MinHeight` setzen, `MaxWidth` an Screen binden. Für enge Bildschirme: ScrollViewer um den Content
- **DataGrids:** Horizontaler Scroll erlaubt in Tabellen, NICHT auf Seitenebene
- **Fixe Breiten:** Nur für Sidebar, Toolbar, Statusleiste. Content-Bereich immer `Width="*"`
- **DPI-Skalierung:** 100%–200% unterstützt. Keine pixelgenauen Layouts — immer relative Einheiten wo möglich

---

## 12. Theme-System

### 12.1 Aktueller Stand (v0.16.1)

Theme-System mit 7 ResourceDictionaries implementiert (**Partial** — bestehende Views SettingsView, ProjectEditDialog noch teilweise hardcoded).

### 12.2 Migration: hardcoded → tokenisiert

Bestehende Views haben stellenweise noch direkte Hex-Werte statt Token. Migration schrittweise:

1. **Farben ersetzen:** `Background="#1E1E1E"` → `Background="{StaticResource BpmBgBase}"`
2. **Schriftgrößen ersetzen:** `FontSize="14"` → `FontSize="{StaticResource BpmFontSizeBody}"`
3. **Lokale Styles auflösen:** Wenn ein lokaler Style dasselbe tut wie ein globaler → durch globalen ersetzen
4. **Testen:** Nach jeder View-Migration prüfen ob Farben/Schriftgrößen korrekt

**Reihenfolge:** PlanManagerView (neu, direkt mit Token) → SettingsView → ProjectEditDialog → SetupDialog

### 12.3 Light Theme (nach V1)

```
Colors.xaml (Dark Theme)     Colors.Light.xaml (Light Theme)
─────────────────────────    ─────────────────────────────
BpmBgBase     = #1E1E1E      BpmBgBase     = #FFFFFF
BpmBgSurface  = #252526      BpmBgSurface  = #F3F3F3
BpmTextPrimary = #CCCCCC     BpmTextPrimary = #1E1E1E
...                           ...
```

Alle Komponenten-Styles referenzieren nur Token aus Colors.xaml. Beim Theme-Wechsel wird nur Colors.xaml ausgetauscht.

---

## 13. Basis-Komponenten (Shell-only)

Alle Controls in `App/Controls/` sind **Shell-only** — nicht für Module zugänglich.

### 13.1 BpmStatusBar ✅

```
Verantwortung: DB-Pfad, Schema-Version, letzte Aktion, Zeitstempel
Position: Ganz unten, volle Breite
API: StatusBar.SetStatus("Projekt gespeichert ✓")
Status: Implementiert
```

### 13.2 BpmToast 🎯

```
Verantwortung: Erfolg/Warnung/Info Meldungen
Position: Oben rechts im MainWindow (Overlay)
API: Module senden ToastMessage über CommunityToolkit.Mvvm Messenger
Verhalten: Slide-In, Auto-Dismiss (3s Erfolg), Stapelbar (max 3)
Status: Zielstandard, kommt mit UI-Refresh
```

### 13.3 BpmBanner 🎯

```
Verantwortung: Persistente Modus-/Systemhinweise
Position: Oben im Content-Bereich (unter Toolbar)
Beispiele: "Nur-Lesen-Modus", "Offline", "3 Pläne im Eingang"
Status: Zielstandard
```

### 13.4 BpmToolbar 🎯

```
Verantwortung: Modul-spezifische Aktionsbuttons
Position: Unter Breadcrumb, über Content
Status: Zielstandard
```

### 13.5 BpmBreadcrumb 🎯

```
Verantwortung: Navigationspfad (Modul > Unterseite > Detail)
Position: Ganz oben über Toolbar
Status: Zielstandard
```

---

## 14. Sicherheit und Datenschutz in der UI

### 14.1 Secrets (ADR-042)

API-Keys und Credentials werden über den `SecretStore` Service (Infrastructure) gespeichert. Technisch DPAPI-basiert, aber die UI spricht nur die Abstraktion an:

```csharp
// RICHTIG — SecretStore verwenden
var apiKey = await _secretStore.RetrieveAsync("openai_api_key");

// FALSCH — DPAPI direkt in der UI aufrufen
var data = ProtectedData.Protect(...);
```

Einstellungs-UI zeigt API-Keys als `••••••••` mit "Ändern"-Button, nie im Klartext.

### 14.2 Privacy Policy (ADR-036)

```csharp
// DI-Registrierung über Compliance-Modus der Lizenz
if (license.RequiresStrictCompliance)
    services.AddSingleton<IPrivacyPolicy, StrictPrivacyPolicy>();
else
    services.AddSingleton<IPrivacyPolicy, RelaxedPrivacyPolicy>();
```

Die Datenschutz-Entscheidung trifft die Policy, nicht das ViewModel. Das ViewModel zeigt nur das Ergebnis an (erlaubt/blockiert/Bestätigung nötig).

---

## 15. Implementierungsreihenfolge

| Phase | Was | Status |
|-------|-----|--------|
| 1 | Colors.xaml + Typography.xaml | ✅ Implementiert |
| 2 | Buttons.xaml + Inputs.xaml | ✅ Implementiert |
| 3 | DataGrid.xaml + Tabs.xaml | ✅ Implementiert |
| 4 | Dialogs.xaml | ✅ Implementiert |
| 5 | App.xaml Merge (7 Dictionaries) | ✅ Implementiert |
| 6 | MainWindow Migration → Token | ✅ Implementiert |
| 7 | PlanManagerView (neu, direkt mit Token) | ⬜ Mit PlanManager |
| 8 | SettingsView + ProjectEditDialog Migration | ⬜ Nach PlanManager |
| 9 | BpmStatusBar | ✅ Vorhanden (einfach) |
| 10 | BpmBreadcrumb, BpmToolbar | 🎯 Mit UI-Refresh |
| 11 | BpmToast, BpmBanner | 🎯 Nach V1 |
| 12 | Screen State Pattern (ViewState + Flags) | ⬜ Schrittweise pro View |
| 13 | Theme-Wechsel (Light) | 🎯 Nach V1 |

---

*Änderungen v1.0 → v2.0 (04.04.2026):*
- *Controls/ als Shell-only markiert (nicht für Module zugänglich) — ADR-006*
- *5 → 7 ResourceDictionaries offiziell (+Inputs.xaml, +Tabs.xaml) — ADR-028 nachgezogen*
- *Token → WPF-Key Mapping-Tabelle eingefügt (Kap. 4)*
- *Screen States: ViewState (Seite) + Operation Flags (Dirty, ReadOnly, PartialResult) getrennt (Kap. 7)*
- *Fehlerbehandlung: kein ex.Message zum User, benutzerfreundliche Meldung + Logging (Kap. 7.4)*
- *MVVM: CommunityToolkit.Mvvm statt eigener BaseViewModel/RelayCommand (Kap. 6)*
- *Code-Behind-Regel präzisiert: UI-Orchestrierung erlaubt, fachlicher Code verboten (Kap. 6.2)*
- *Navigation als V1-Übergang markiert (Kap. 5.3)*
- *Feedback-Infrastruktur technisch eingeordnet (Kap. 8)*
- *Dialog-Schließregeln differenziert: Overlay-Klick nur bei Info-Dialogen (Kap. 9.2)*
- *Mehrtab-Validierung technisch beschrieben (Kap. 9.3)*
- *Responsive-Regeln technisch verankert (Kap. 11)*
- *Migration hardcoded → tokenisiert beschrieben (Kap. 12.2)*
- *Datenschutz: license.IsCommercial → RequiresStrictCompliance, DPAPI → SecretStore (Kap. 14)*
- *Verwandte Dokumente + Querverweise ergänzt*
