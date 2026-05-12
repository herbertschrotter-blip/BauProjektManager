using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Ein einzelnes Segment eines geparsten Dateinamens.
/// Position 0 = erstes Segment nach dem Split.
/// FieldType ist null solange der User noch nichts zugewiesen hat.
/// INPC fuer FieldType + CustomFieldName, damit das UI (Token-Farbe + Label)
/// nach Drag&Drop-Zuweisung sofort aktualisiert.
/// </summary>
public class FileNameSegment : INotifyPropertyChanged
{
    private FieldType? _fieldType;
    private string? _customFieldName;

    public int Position { get; set; }
    public string RawValue { get; set; } = string.Empty;

    public FieldType? FieldType
    {
        get => _fieldType;
        set
        {
            if (!Nullable.Equals(_fieldType, value))
            {
                _fieldType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string? CustomFieldName
    {
        get => _customFieldName;
        set
        {
            if (_customFieldName != value)
            {
                _customFieldName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    /// <summary>
    /// Anzeigename für die UI: Enum-Name, Custom-Name, oder "—".
    /// </summary>
    public string DisplayName =>
        FieldType == PlanManager.FieldType.Custom
            ? CustomFieldName ?? "—"
            : FieldType?.ToString() ?? "—";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
