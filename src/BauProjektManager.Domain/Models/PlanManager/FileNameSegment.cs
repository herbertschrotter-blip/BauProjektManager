using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Ein einzelnes Segment eines geparsten Dateinamens.
/// Position 0 = erstes Segment nach dem Split.
/// </summary>
/// <remarks>
/// V4 (BPM-108 Phase C, ADR-056): <see cref="FieldTypeId"/> ist die stabile
/// <c>segment_types.id</c>-Referenz (snake_case fuer Built-ins, ULID fuer Custom).
/// Die fruehere <c>FieldType</c>-Enum wurde durch den Katalog-Lookup ersetzt.
/// <see cref="DisplayName"/> liefert den raw token_key — fuer den UI-konformen Namen
/// (z. B. „Plannummer") muss der Wizard bzw. der XAML-Converter ueber den
/// <see cref="Domain.Interfaces.ISegmentTypeCatalog"/> aufloesen.
/// </remarks>
public class FileNameSegment : INotifyPropertyChanged
{
    private string? _fieldTypeId;

    public int Position { get; set; }
    public string RawValue { get; set; } = string.Empty;

    /// <summary>
    /// Stabile <c>segment_types.id</c>-Referenz oder null wenn unzugewiesen.
    /// </summary>
    public string? FieldTypeId
    {
        get => _fieldTypeId;
        set
        {
            if (_fieldTypeId != value)
            {
                _fieldTypeId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(IsAssigned));
            }
        }
    }

    /// <summary>
    /// True wenn ein <see cref="FieldTypeId"/> gesetzt ist.
    /// </summary>
    public bool IsAssigned => !string.IsNullOrEmpty(_fieldTypeId);

    /// <summary>
    /// Roh-Bezeichner fuer das Segment. Das UI sollte den UI-Namen ueber den
    /// <see cref="Domain.Interfaces.ISegmentTypeCatalog"/> aufloesen.
    /// </summary>
    public string DisplayName => _fieldTypeId ?? "—";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
