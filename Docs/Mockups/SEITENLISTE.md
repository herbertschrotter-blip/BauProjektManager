# BPM Seitenliste

Erzeugt aus `Docs/Mockups/seiten.json` (`python3 tools/seitenliste.py`) – dort ändern, nicht hier. Die **Nr.** ist der
feste Ort einer Seite. Je Seite zwei Versionen: läuft **Mockup** vor **Code**, ist es abgenommen, aber noch nicht
gebaut. **1.0** = überarbeitet und abgenommen, **0.1** = noch nicht durchgegangen.

## 1 Shell

| Nr. | Seite | Art | Version Mockup | Version Code | Mockup | Code |
|---|---|---|---|---|---|---|
| 1.1 | Hauptfenster | Seite | – | 0.2 | `App/01_Shell/01_Sidebar.html (alt)` | `BauProjektManager.App/MainWindow.xaml, BauProjektManager.App/MainWindow.xaml.cs` |
| 1.2 | Ersteinrichtung | Formular | – | 0.1 | `–` | `BauProjektManager.App/SetupDialog.xaml, BauProjektManager.App/SetupDialog.xaml.cs` |
| 1.3 | Bestätigung | Formular | – | 0.1 | `–` | `BauProjektManager.App/BpmConfirmDialog.xaml, BauProjektManager.App/BpmConfirmDialog.xaml.cs, BauProjektManager.App/BpmDialogService.cs` |
| 1.4 | Hinweis | Formular | – | 0.1 | `–` | `BauProjektManager.App/BpmInfoDialog.xaml, BauProjektManager.App/BpmInfoDialog.xaml.cs, BauProjektManager.App/BpmDialogService.cs` |
| 1.5 | Entwicklerwerkzeuge – Log | Formular | – | 0.1 | `Settings/02_DevTools/01_Log.html (alt)` | `BauProjektManager.App/DevToolsDialog.xaml, BauProjektManager.App/DevToolsDialog.xaml.cs` |
| 1.6 | Entwicklerwerkzeuge – Zurücksetzen | Formular | – | 0.1 | `Settings/02_DevTools/02_Reset.html (alt)` | `BauProjektManager.App/DevToolsDialog.xaml, BauProjektManager.App/DevToolsDialog.xaml.cs` |

## 2 Einstellungen

| Nr. | Seite | Art | Version Mockup | Version Code | Mockup | Code |
|---|---|---|---|---|---|---|
| 2.1 | Allgemein | Blatt | – | 0.2 | `Settings/01_Einstellungen/01_Allgemein.html (alt)` | `BauProjektManager.Settings/Views/SettingsView.xaml, BauProjektManager.Settings/Views/SettingsView.xaml.cs, BauProjektManager.Settings/ViewModels/SettingsViewModel.cs` |
| 2.2 | Projekte | Blatt | – | 0.1 | `–` | `BauProjektManager.Settings/Views/SettingsView.xaml, BauProjektManager.Settings/Views/SettingsView.xaml.cs, BauProjektManager.Settings/ViewModels/SettingsViewModel.cs` |
| 2.3 | Standard-Ordnerstruktur | Blatt | – | 0.1 | `–` | `BauProjektManager.Settings/Views/SettingsView.xaml, BauProjektManager.Settings/Views/SettingsView.xaml.cs, BauProjektManager.Settings/ViewModels/SettingsViewModel.cs, BauProjektManager.Settings/Views/FolderTemplateControl.xaml, BauProjektManager.Settings/Views/FolderTemplateControl.xaml.cs` |
| 2.4 | Sync-Defaults | Blatt | – | 0.1 | `–` | `BauProjektManager.Settings/Views/SettingsView.xaml, BauProjektManager.Settings/Views/SettingsView.xaml.cs, BauProjektManager.Settings/ViewModels/SettingsViewModel.cs` |
| 2.5 | Über | Blatt | – | 0.1 | `–` | `BauProjektManager.Settings/Views/SettingsView.xaml, BauProjektManager.Settings/Views/SettingsView.xaml.cs, BauProjektManager.Settings/ViewModels/SettingsViewModel.cs` |
| 2.6 | Projekt bearbeiten – Stammdaten | Formular | – | 0.1 | `–` | `BauProjektManager.Settings/Views/ProjectEditDialog.xaml, BauProjektManager.Settings/Views/ProjectEditDialog.xaml.cs, BauProjektManager.Settings/Views/ProjectEditDialog.Helpers.cs, BauProjektManager.Settings/Views/ProjectEditDialog.Stammdaten.cs` |
| 2.7 | Projekt bearbeiten – Bauwerk | Formular | – | 0.1 | `–` | `BauProjektManager.Settings/Views/ProjectEditDialog.xaml, BauProjektManager.Settings/Views/ProjectEditDialog.xaml.cs, BauProjektManager.Settings/Views/ProjectEditDialog.Helpers.cs, BauProjektManager.Settings/Views/ProjectEditDialog.Bauwerk.cs, BauProjektManager.Settings/Views/ProjectEditDialog.BauwerkDialoge.cs` |
| 2.8 | Projekt bearbeiten – Beteiligte | Formular | – | 0.1 | `–` | `BauProjektManager.Settings/Views/ProjectEditDialog.xaml, BauProjektManager.Settings/Views/ProjectEditDialog.xaml.cs, BauProjektManager.Settings/Views/ProjectEditDialog.Helpers.cs, BauProjektManager.Settings/Views/ProjectEditDialog.Beteiligte.cs` |
| 2.9 | Projekt bearbeiten – Portale + Links | Formular | – | 0.1 | `–` | `BauProjektManager.Settings/Views/ProjectEditDialog.xaml, BauProjektManager.Settings/Views/ProjectEditDialog.xaml.cs, BauProjektManager.Settings/Views/ProjectEditDialog.Helpers.cs, BauProjektManager.Settings/Views/ProjectEditDialog.PortaleLinks.cs` |
| 2.10 | Projekt bearbeiten – Ordnerstruktur | Formular | – | 0.1 | `–` | `BauProjektManager.Settings/Views/ProjectEditDialog.xaml, BauProjektManager.Settings/Views/ProjectEditDialog.xaml.cs, BauProjektManager.Settings/Views/ProjectEditDialog.Helpers.cs, BauProjektManager.Settings/Views/ProjectEditDialog.Ordnerstruktur.cs, BauProjektManager.Settings/Views/FolderTemplateControl.xaml, BauProjektManager.Settings/Views/FolderTemplateControl.xaml.cs` |

## 3 PlanManager

| Nr. | Seite | Art | Version Mockup | Version Code | Mockup | Code |
|---|---|---|---|---|---|---|
| 3.1 | Projektübersicht | Seite | – | 0.2 | `PlanManager/01_Projektuebersicht/01_Projektuebersicht.html (alt)` | `BauProjektManager.PlanManager/Views/PlanManagerView.xaml, BauProjektManager.PlanManager/Views/PlanManagerView.xaml.cs, BauProjektManager.PlanManager/ViewModels/PlanManagerViewModel.cs` |
| 3.2 | Explorer | Blatt | – | 0.1 | `PlanManager/02_Projektdetail/04_Explorer.html (alt)` | `BauProjektManager.PlanManager/Views/ProjectDetailView.xaml, BauProjektManager.PlanManager/Views/ProjectDetailView.xaml.cs, BauProjektManager.PlanManager/ViewModels/ProjectDetailViewModel.cs, BauProjektManager.PlanManager/Views/ExplorerView.xaml, BauProjektManager.PlanManager/Views/ExplorerView.xaml.cs, BauProjektManager.PlanManager/ViewModels/ExplorerViewModel.cs, BauProjektManager.PlanManager/ViewModels/ArchiveRowViewModel.cs` |
| 3.3 | Manuell sortieren | Blatt | – | 0.3 | `PlanManager/02_Projektdetail/02_ManuellSortieren.html (alt)` | `BauProjektManager.PlanManager/Views/ProjectDetailView.xaml, BauProjektManager.PlanManager/Views/ProjectDetailView.xaml.cs, BauProjektManager.PlanManager/ViewModels/ProjectDetailViewModel.cs, BauProjektManager.PlanManager/Views/ManualCaptureView.xaml, BauProjektManager.PlanManager/Views/ManualCaptureView.xaml.cs, BauProjektManager.PlanManager/ViewModels/ManualCaptureViewModel.cs, BauProjektManager.PlanManager/ViewModels/CaptureDetailViewModel.cs, BauProjektManager.PlanManager/Controls/RadialCaptureControl.xaml, BauProjektManager.PlanManager/Controls/RadialCaptureControl.xaml.cs, BauProjektManager.PlanManager/Views/PlanPreviewPanel.xaml, BauProjektManager.PlanManager/Views/PlanPreviewPanel.xaml.cs` |
| 3.4 | Mit Claude sortieren | Blatt | 1.0 | 1.0 | `PlanManager/02_Projektdetail/02_ManuellSortieren_Claude.html (alt)` | `BauProjektManager.PlanManager/Views/ClaudeChatPanel.xaml, BauProjektManager.PlanManager/Views/ClaudeChatPanel.xaml.cs, BauProjektManager.PlanManager/Views/ManualCaptureView.Claude.cs, BauProjektManager.PlanManager/ViewModels/ClaudeSortViewModel.cs, BauProjektManager.PlanManager/ViewModels/ClaudeSortViewModel.Events.cs, BauProjektManager.PlanManager/ViewModels/ClaudeChatModels.cs, BauProjektManager.PlanManager/ViewModels/ManualCaptureViewModel.Claude.cs` |
| 3.5 | Plandaten | Blatt | – | 0.1 | `PlanManager/02_Projektdetail/05_Plandaten.html (alt)` | `BauProjektManager.PlanManager/Views/ProjectDetailView.xaml, BauProjektManager.PlanManager/Views/ProjectDetailView.xaml.cs, BauProjektManager.PlanManager/ViewModels/ProjectDetailViewModel.cs, BauProjektManager.PlanManager/Views/PlanDataView.xaml, BauProjektManager.PlanManager/Views/PlanDataView.xaml.cs, BauProjektManager.PlanManager/ViewModels/PlanDataViewModel.cs, BauProjektManager.PlanManager/Views/SegmentEditorControl.xaml, BauProjektManager.PlanManager/Views/SegmentEditorControl.xaml.cs, BauProjektManager.PlanManager/ViewModels/SegmentEditorViewModel.cs` |
| 3.6 | Profile | Blatt | – | 0.1 | `PlanManager/02_Projektdetail/01_Profile.html (alt)` | `BauProjektManager.PlanManager/Views/ProjectDetailView.xaml, BauProjektManager.PlanManager/Views/ProjectDetailView.xaml.cs, BauProjektManager.PlanManager/ViewModels/ProjectDetailViewModel.cs, BauProjektManager.PlanManager/ViewModels/DocumentTypeOverviewViewModel.cs` |
| 3.7 | Sync | Blatt | – | 0.1 | `–` | `BauProjektManager.PlanManager/Views/ProjectDetailView.xaml, BauProjektManager.PlanManager/Views/ProjectDetailView.xaml.cs, BauProjektManager.PlanManager/ViewModels/ProjectDetailViewModel.cs` |
| 3.8 | Profil-Wizard | Formular | – | 0.1 | `PlanManager/03_ProfilWizard/01_Datei.html (alt)` | `BauProjektManager.PlanManager/Views/ProfileWizardDialog.xaml, BauProjektManager.PlanManager/Views/ProfileWizardDialog.xaml.cs, BauProjektManager.PlanManager/ViewModels/ProfileWizardViewModel.cs` |
| 3.9 | Segmenttypen | Formular | – | 0.1 | `PlanManager/04_FeldtypManager/01_Uebersicht.html (alt)` | `BauProjektManager.PlanManager/Views/SegmentTypeManagerDialog.xaml, BauProjektManager.PlanManager/Views/SegmentTypeManagerDialog.xaml.cs, BauProjektManager.PlanManager/ViewModels/SegmentTypeManagerViewModel.cs` |
| 3.10 | Wiederherstellung | Formular | – | 0.1 | `–` | `BauProjektManager.PlanManager/Views/RecoveryDialog.xaml, BauProjektManager.PlanManager/Views/RecoveryDialog.xaml.cs` |
| 3.11 | Zielordner wählen | Formular | – | 0.1 | `–` | `BauProjektManager.PlanManager/Views/FolderPickerDialog.xaml, BauProjektManager.PlanManager/Views/FolderPickerDialog.xaml.cs` |
