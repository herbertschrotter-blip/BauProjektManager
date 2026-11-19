using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// Profil-Wizard (Seitenliste 3.8, Mockup 0.2, BPM-130). Schritt 2 bindet den Segment-Editor (BPM-126c) ein; sein
/// Stand geht nach jeder Aenderung an das ViewModel. Logik im <see cref="ProfileWizardViewModel"/>.
/// </summary>
public partial class ProfileWizardDialog : Window
{
    /// <summary>Text → sichtbar, wenn nicht leer.</summary>
    public static readonly IValueConverter TextToVis = new TextToVisibilityConverter();

    private readonly ProfileWizardViewModel _vm;
    private readonly ISegmentTypeCatalog? _segmentTypeCatalog;
    private readonly ISegmentTypeRepository? _segmentTypeRepository;
    private readonly IIdGenerator? _idGenerator;
    private string? _editorFile;
    private bool _pushPending;

    public ProfileWizardDialog(
        Project? project = null,
        IProfileManager? profileManager = null,
        PatternTemplateService? templateService = null,
        string? appDataPath = null,
        ISegmentTypeCatalog? segmentTypeCatalog = null,
        ISegmentTypeRepository? segmentTypeRepository = null,
        IIdGenerator? idGenerator = null,
        ProjectDatabase? bpmDb = null)
    {
        InitializeComponent();
        _segmentTypeCatalog = segmentTypeCatalog;
        _segmentTypeRepository = segmentTypeRepository;
        _idGenerator = idGenerator;
        _vm = new ProfileWizardViewModel(project, profileManager, templateService, appDataPath, segmentTypeCatalog, bpmDb);
        DataContext = _vm;

        // Jede Aenderung im Editor (Zuweisung, Verbinden/Trennen) baut die Kacheln neu → Stand ans ViewModel
        SegmentEditor.ViewModel.Elements.CollectionChanged += (_, _) => SchiebeEditorStand();
        SegmentEditor.ManageTypesRequested += OnManageTypesRequested;
        Loaded += (_, _) => UpdateStepVisibility();
    }

    /// <summary>Mehrere Aenderungen eines Rebuilds zu einer Uebergabe zusammenfassen.</summary>
    private void SchiebeEditorStand()
    {
        if (_pushPending)
            return;
        _pushPending = true;
        Dispatcher.BeginInvoke(() =>
        {
            _pushPending = false;
            var editor = SegmentEditor.ViewModel;
            _vm.ApplyEditorState(
                [.. editor.Tokens().Select(t => (t.Text, t.AssignedType?.Id))],
                editor.ActiveDelimiters(), editor.HasMixedSeparators());
            UpdateStep2Note();
        });
    }

    private void UpdateStep2Note()
        => Step2Note.BorderBrush = (Brush)FindResource(_vm.Step2Ok ? "BpmSuccess" : "BpmWarning");

    /// <summary>Editor nur neu laden, wenn sich die Beispieldatei geaendert hat (Zurueck/Weiter behaelt die Zuweisungen).</summary>
    private void LadeEditorBeiBedarf()
    {
        if (_editorFile != _vm.EditorFileName)
        {
            _editorFile = _vm.EditorFileName;
            SegmentEditor.Load(_vm.EditorFileName, _segmentTypeCatalog, null);
        }
        // Immer neu uebergeben: ein Dateiwechsel in Schritt 1 hat die Segmente im ViewModel geleert
        SchiebeEditorStand();
    }

    private void OnIgnoreOpenClick(object sender, MouseButtonEventArgs e)
    {
        if (_vm.IgnoreType is { } ignore)
            SegmentEditor.ViewModel.AssignAllOpen(ignore);
    }

    /// <summary>Segmenttyp-Manager; danach Editor mit den bisherigen Zuweisungen neu laden (neue Typen in der Palette).</summary>
    private void OnManageTypesRequested(object? sender, EventArgs e)
    {
        if (_segmentTypeRepository is null || _segmentTypeCatalog is null || _idGenerator is null)
            return;
        var bisher = SegmentEditor.ViewModel.CurrentAssignments()
            .Select(a => new PlanDocumentSegment("", "", a.Type.Id, a.Type.TokenKey ?? "", a.RawValue, a.RawValue))
            .ToList();
        new SegmentTypeManagerDialog(_segmentTypeRepository, _segmentTypeCatalog, _idGenerator) { Owner = this }
            .ShowDialog();
        SegmentEditor.Load(_vm.EditorFileName, _segmentTypeCatalog, bisher);
        SchiebeEditorStand();
    }

    private void OnPickFileClick(object sender, MouseButtonEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Beispieldatei wählen", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true)
            _vm.SampleFileName = System.IO.Path.GetFileName(dialog.FileName);
    }

    private void OnIndexSourceChecked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { DataContext: IndexSourceOption option })
            _vm.SelectedIndexSource = option.Value;
    }

    private void OnIndexSourceLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { DataContext: IndexSourceOption option } radio)
            radio.IsChecked = option.Value == _vm.SelectedIndexSource;
    }

    private void OnRecognitionSegmentClick(object sender, RoutedEventArgs e) => _vm.OnRecognitionSegmentToggled();

    private void OnNext(object sender, RoutedEventArgs e)
    {
        if (_vm.CurrentStep >= _vm.TotalSteps)
        {
            _vm.SaveProfileCommand.Execute(null);
            if (_vm.ProfileSaved)
            {
                DialogResult = true;
                Close();
            }
            return;
        }
        if (_vm.CurrentStep == 1)
            LadeEditorBeiBedarf();
        _vm.GoNextCommand.Execute(null);
        UpdateStepVisibility();
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        _vm.GoBackCommand.Execute(null);
        UpdateStepVisibility();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void UpdateStepVisibility()
    {
        var step = _vm.CurrentStep;
        UIElement[] panels = [Step1Panel, Step2Panel, Step3Panel, Step4Panel];
        System.Windows.Shapes.Ellipse[] dots = [Dot1, Dot2, Dot3, Dot4];
        for (var i = 0; i < panels.Length; i++)
        {
            panels[i].Visibility = i + 1 == step ? Visibility.Visible : Visibility.Collapsed;
            dots[i].Fill = (Brush)FindResource(i + 1 < step ? "BpmBgActive" : i + 1 == step ? "BpmAccentPrimary" : "BpmBorderDefault");
        }
        StepCounter.Text = $"Schritt {step} von {_vm.TotalSteps}";
        NextButton.Content = step >= _vm.TotalSteps ? "Speichern" : "Weiter";
        if (step == 2)
            UpdateStep2Note();
    }

    private sealed class TextToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
