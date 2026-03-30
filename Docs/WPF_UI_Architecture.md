# BauProjektManager — WPF UI Architecture

**Version:** 1.0  
**Datum:** 30.03.2026  
**Bezug:** UI_UX_Guidelines.md (Design-Regeln), CODING_STANDARDS.md (Code-Regeln)

---

## 1. Ziel

Dieses Dokument beschreibt den **technischen Aufbau** der UI-Schicht: Wie Views, Styles, Themes und Basis-Komponenten organisiert sind. Es ist die Brücke zwischen den Design-Regeln (UI_UX_Guidelines.md) und dem tatsächlichen Code.

---

## 2. Projektstruktur (UI-relevant)

```
BauProjektManager.App/
├── App.xaml                          ← ResourceDictionary-Merge
├── App.xaml.cs                       ← DI Setup, Startup
├── MainWindow.xaml                   ← Shell (Sidebar + Content + Statusleiste)
├── MainWindow.xaml.cs
│
├── Themes/                           ← ZENTRAL: Alle Styles und Farben
│   ├── Colors.xaml                   ← Farb-Token (bg-base, accent-primary etc.)
│   ├── Typography.xaml               ← Schriftgrößen, -gewichte
│   ├── Buttons.xaml                  ← Button-Styles (Primary, Secondary, Danger, Ghost)
│   ├── Inputs.xaml                   ← TextBox, ComboBox, DatePicker Styles
│   ├── DataGrid.xaml                 ← Tabellen-Styles (Header, Zeilen, Hover)
│   ├── Dialogs.xaml                  ← Dialog-Basis (Overlay, Header, Footer)
│   └── Tabs.xaml                     ← TabControl + TabItem Styles
│
├── Controls/                         ← Wiederverwendbare Basis-Komponenten
│   ├── BpmToast.xaml                 ← Toast-Benachrichtigungen
│   ├── BpmToast.xaml.cs
│   ├── BpmStatusBar.xaml             ← Statusleiste
│   ├── BpmStatusBar.xaml.cs
│   ├── BpmToolbar.xaml               ← Modul-spezifische Toolbar
│   ├── BpmToolbar.xaml.cs
│   ├── BpmBreadcrumb.xaml            ← Breadcrumb-Navigation
│   └── BpmBreadcrumb.xaml.cs
│
├── Converters/                       ← Globale Value Converters
│   ├── StatusToColorConverter.cs
│   ├── BoolToVisibilityConverter.cs
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

- **Themes/** liegt im `.App`-Projekt → ist für alle Module sichtbar
- **Controls/** liegt im `.App`-Projekt → ist für alle Module sichtbar
- **Views/** liegen in den jeweiligen Modul-Projekten (`.Settings`, `.PlanManager`)
- Modul-spezifische Styles gehören NICHT in Themes/ → sie bleiben lokal im Modul
- Globale Styles (Button, DataGrid, Tabs) gehören in Themes/

---

## 3. Resource Dictionary Architektur

### 3.1 Merge-Reihenfolge in App.xaml

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- 1. Farben zuerst (werden von allen anderen referenziert) -->
            <ResourceDictionary Source="Themes/Colors.xaml"/>
            <!-- 2. Typografie -->
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

### 3.2 Naming-Konvention für Keys

| Typ | Prefix | Beispiel |
|-----|--------|---------|
| Farbe (SolidColorBrush) | `Bpm` | `BpmBgBase`, `BpmAccentPrimary`, `BpmTextSecondary` |
| Farbe (Color) | `BpmColor` | `BpmColorAccentPrimary`, `BpmColorError` |
| Style | `Bpm<Typ>` | `BpmButtonPrimary`, `BpmDataGridDefault` |
| ControlTemplate | `Bpm<Typ>Template` | `BpmButtonTemplate`, `BpmTabItemTemplate` |
| Thickness/Margin | `Bpm<Kontext>` | `BpmDialogPadding`, `BpmCardMargin` |

### 3.3 Referenzierung

```xml
<!-- RICHTIG — Immer StaticResource für Styles und Farben -->
<Button Style="{StaticResource BpmButtonPrimary}"/>
<Border Background="{StaticResource BpmBgSurface}"/>

<!-- RICHTIG — DynamicResource nur wenn sich der Wert zur Laufzeit ändern kann -->
<!-- (z.B. Theme-Wechsel Dark→Light) -->
<Border Background="{DynamicResource BpmBgBase}"/>

<!-- FALSCH — Farben direkt in XAML -->
<Border Background="#1E1E1E"/>
```

**Entscheidung:** Für V1 `StaticResource` verwenden (kein Laufzeit-Theme-Wechsel). Wenn Light Theme kommt, auf `DynamicResource` umstellen.

---

## 4. Shell-Architektur (MainWindow)

### 4.1 Aufbau

```
┌──────────────────────────────────────────────────────────────┐
│ BpmBreadcrumb (28px)                                         │
├────────┬─────────────────────────────────────────────────────┤
│        │ BpmToolbar (40px)                                   │
│ Side-  ├─────────────────────────────────────────────────────┤
│ bar    │                                                     │
│ (220px)│ ContentControl (wechselt pro Modul)                 │
│        │                                                     │
│        │                                                     │
├────────┴─────────────────────────────────────────────────────┤
│ BpmStatusBar (24px)                                          │
└──────────────────────────────────────────────────────────────┘
```

### 4.2 Grid-Layout

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="220"/>  <!-- Sidebar -->
        <ColumnDefinition Width="*"/>    <!-- Content -->
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
        <RowDefinition Height="28"/>     <!-- Breadcrumb -->
        <RowDefinition Height="40"/>     <!-- Toolbar -->
        <RowDefinition Height="*"/>      <!-- Content -->
        <RowDefinition Height="24"/>     <!-- Statusleiste -->
    </Grid.RowDefinitions>

    <!-- Sidebar: Zeile 0-3, Spalte 0 -->
    <!-- Breadcrumb: Zeile 0, Spalte 1 -->
    <!-- Toolbar: Zeile 1, Spalte 1 -->
    <!-- Content: Zeile 2, Spalte 1 -->
    <!-- Statusleiste: Zeile 3, Spalte 0-1 -->
</Grid>
```

### 4.3 Navigation

```csharp
// MainWindow.xaml.cs oder MainViewModel.cs
private void NavigateTo(string moduleId)
{
    ContentArea.Content = moduleId switch
    {
        "settings" => new SettingsView(),
        "planManager" => new PlanManagerView(),
        "dashboard" => new DashboardView(),
        _ => throw new ArgumentException($"Unknown module: {moduleId}")
    };

    // Breadcrumb aktualisieren
    Breadcrumb.SetPath(moduleId);

    // Toolbar aktualisieren (modul-spezifisch)
    Toolbar.SetModule(moduleId);
}
```

---

## 5. Theme-System

### 5.1 Aktueller Stand (v0.15.0)

Farben sind hardcoded in XAML. Kein Theme-System.

### 5.2 Ziel-Architektur

```
Colors.xaml (Dark Theme)     Colors.Light.xaml (Light Theme)
─────────────────────────    ─────────────────────────────
BpmBgBase     = #1E1E1E      BpmBgBase     = #FFFFFF
BpmBgSurface  = #252526      BpmBgSurface  = #F3F3F3
BpmTextPrimary = #CCCCCC     BpmTextPrimary = #1E1E1E
...                           ...
```

Alle Komponenten-Styles (Buttons.xaml, DataGrid.xaml etc.) referenzieren nur Token aus Colors.xaml. Beim Theme-Wechsel wird nur Colors.xaml ausgetauscht — alle Styles passen sich automatisch an.

### 5.3 Theme-Wechsel (später)

```csharp
public void SwitchTheme(string theme)
{
    var dict = Application.Current.Resources.MergedDictionaries;

    // Alte Colors.xaml entfernen
    var oldColors = dict.FirstOrDefault(d =>
        d.Source?.OriginalString.Contains("Colors") == true);
    if (oldColors != null) dict.Remove(oldColors);

    // Neue Colors.xaml laden
    var source = theme == "Light"
        ? "Themes/Colors.Light.xaml"
        : "Themes/Colors.xaml";
    dict.Insert(0, new ResourceDictionary { Source = new Uri(source, UriKind.Relative) });
}
```

---

## 6. Basis-Komponenten

### 6.1 BpmToast

Toast-Benachrichtigungen die oben rechts eingeblendet werden.

```
Verantwortung: Erfolg/Fehler/Warnung/Info Meldungen anzeigen
Position: Oben rechts im MainWindow (Overlay über Content)
API: ToastService.Show("Projekt gespeichert", ToastType.Success)
Verhalten: Slide-In, Auto-Dismiss (3s für Erfolg), Stapelbar (max 3)
```

### 6.2 BpmStatusBar

Statusleiste am unteren Rand.

```
Verantwortung: DB-Pfad, Schema-Version, letzte Aktion anzeigen
Position: Ganz unten, volle Breite
API: StatusBar.SetStatus("Projekt gespeichert ✓")
Verhalten: Statisch, aktualisiert sich bei Aktionen
```

### 6.3 BpmToolbar

Modul-spezifische Aktionsleiste.

```
Verantwortung: Buttons für das aktive Modul anzeigen
Position: Unter Breadcrumb, über Content
API: Toolbar.SetModule("settings") → zeigt Projekt-Buttons
Verhalten: Wechselt bei Modul-Navigation, Buttons sind Commands
```

### 6.4 BpmBreadcrumb

Navigations-Pfad oben.

```
Verantwortung: Aktuellen Pfad anzeigen (Modul > Unterseite > Detail)
Position: Ganz oben über der Toolbar
API: Breadcrumb.SetPath("Einstellungen", "Projekte", "ÖWG Dobl")
Verhalten: Klickbar, navigiert zurück
```

---

## 7. Dialog-Architektur

### 7.1 Dialog-Basis

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
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Content="Abbrechen" Style="{StaticResource BpmButtonSecondary}"
                        Command="{Binding CancelCommand}"/>
                <Button Content="Speichern" Style="{StaticResource BpmButtonPrimary}"
                        Command="{Binding SaveCommand}"/>
            </StackPanel>
        </Border>
    </Grid>
</Window>
```

### 7.2 Dialog öffnen (aus ViewModel)

```csharp
// RICHTIG — Dialog wird aus Code-Behind geöffnet, nicht aus ViewModel
// ViewModel sagt "ich will einen Dialog", View entscheidet wie

// SettingsView.xaml.cs
private void OnAddProject(object sender, RoutedEventArgs e)
{
    var dialog = new ProjectEditDialog();
    dialog.Owner = Window.GetWindow(this);
    if (dialog.ShowDialog() == true)
    {
        _viewModel.ReloadProjects();
    }
}

// ODER über einen DialogService (sauberer, testbar)
public interface IDialogService
{
    bool? ShowDialog<TDialog>(object viewModel) where TDialog : Window, new();
}
```

---

## 8. Screen State Pattern

### 8.1 Implementierung

Jede View die Daten lädt, verwendet ein `ViewState` Enum:

```csharp
public enum ViewState
{
    Loading,
    Data,
    Empty,
    Error,
    Offline
}

// Im ViewModel
public class ProjectListViewModel : ObservableObject
{
    [ObservableProperty]
    private ViewState _viewState = ViewState.Loading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public async Task LoadAsync()
    {
        ViewState = ViewState.Loading;
        try
        {
            var projects = await _db.LoadAllProjects();
            ViewState = projects.Count > 0 ? ViewState.Data : ViewState.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ViewState = ViewState.Error;
        }
    }
}
```

### 8.2 In XAML

```xml
<!-- ContentPresenter wechselt je nach ViewState -->
<Grid>
    <!-- Loading -->
    <StackPanel Visibility="{Binding ViewState, Converter={StaticResource StateToVisibility},
                             ConverterParameter=Loading}">
        <TextBlock Text="⟳ Lade Projekte..." Style="{StaticResource BpmBodyText}"
                   HorizontalAlignment="Center" VerticalAlignment="Center"/>
    </StackPanel>

    <!-- Data (normale Ansicht) -->
    <DataGrid Visibility="{Binding ViewState, Converter={StaticResource StateToVisibility},
                           ConverterParameter=Data}"
              ItemsSource="{Binding Projects}" .../>

    <!-- Empty -->
    <StackPanel Visibility="{Binding ViewState, Converter={StaticResource StateToVisibility},
                             ConverterParameter=Empty}"
                HorizontalAlignment="Center" VerticalAlignment="Center">
        <TextBlock Text="📁 Noch keine Projekte angelegt"/>
        <Button Content="+ Neues Projekt anlegen"
                Style="{StaticResource BpmButtonPrimary}"
                Command="{Binding AddProjectCommand}"/>
    </StackPanel>

    <!-- Error -->
    <StackPanel Visibility="{Binding ViewState, Converter={StaticResource StateToVisibility},
                             ConverterParameter=Error}"
                HorizontalAlignment="Center" VerticalAlignment="Center">
        <TextBlock Text="❌ Fehler beim Laden"/>
        <TextBlock Text="{Binding ErrorMessage}" Style="{StaticResource BpmSmallText}"/>
        <Button Content="Erneut versuchen"
                Style="{StaticResource BpmButtonSecondary}"
                Command="{Binding RetryCommand}"/>
    </StackPanel>
</Grid>
```

---

## 9. Datenformatierung

### 9.1 CultureInfo Setup

```csharp
// App.xaml.cs — beim Start setzen
protected override void OnStartup(StartupEventArgs e)
{
    // Österreichische Formate für die gesamte App
    var culture = CultureInfo.GetCultureInfo("de-AT");
    Thread.CurrentThread.CurrentCulture = culture;
    Thread.CurrentThread.CurrentUICulture = culture;
    FrameworkElement.LanguageProperty.OverrideMetadata(
        typeof(FrameworkElement),
        new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.Name)));
}
```

### 9.2 Converter für Einheiten

```csharp
public class UnitFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d && parameter is string unit)
        {
            return $"{d.ToString("N2", CultureInfo.GetCultureInfo("de-AT"))} {unit}";
        }
        return value;
    }
}

// Verwendung in XAML:
// <TextBlock Text="{Binding PlannedQuantity, Converter={StaticResource UnitFormat},
//                   ConverterParameter=m²}"/>
// Ergebnis: "198,00 m²"
```

---

## 10. Implementierungsreihenfolge

| Phase | Was | Wann |
|-------|-----|------|
| 1 | Colors.xaml + Typography.xaml | Jetzt (Grundlage für alles) |
| 2 | Buttons.xaml + Inputs.xaml | Jetzt (Basis-Styles) |
| 3 | DataGrid.xaml + Tabs.xaml | Jetzt (Tabellen sind Kern) |
| 4 | Dialogs.xaml | Jetzt (Dialog-Basis) |
| 5 | App.xaml Merge | Jetzt (alles zusammenführen) |
| 6 | BpmStatusBar, BpmBreadcrumb | Mit PlanManager |
| 7 | BpmToolbar | Mit PlanManager |
| 8 | BpmToast | Nach V1 |
| 9 | Screen State Pattern | Schrittweise pro View |
| 10 | Theme-Wechsel (Light) | Nach V1 |

---

*Dieses Dokument beschreibt WIE die UI technisch gebaut wird. WAS sie zeigen soll steht in UI_UX_Guidelines.md. WIE der Code strukturiert ist steht in CODING_STANDARDS.md.*