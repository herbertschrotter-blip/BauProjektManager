using CommunityToolkit.Mvvm.ComponentModel;
using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// Eine Zeile des Archiv-Sub-Tabs (BPM-111.07 Slice D): Dokument mit
/// current-Revision und Primärdatei, read-only aus der DB. Zeilen des
/// letzten Imports sind grün gekennzeichnet (Undo-Invariante: nur der
/// letzte Import ist rückgängig machbar — als Ganzes).
/// </summary>
public partial class ArchiveRowViewModel : ObservableObject
{
    public ArchiveRowViewModel(PlanArchiveEntry entry, bool isLastImport)
    {
        Entry = entry;
        IsLastImport = isLastImport;
    }

    public PlanArchiveEntry Entry { get; }

    /// <summary>Gehört zur jüngsten abgeschlossenen Import-Journal-Id (grüne Kennzeichnung).</summary>
    public bool IsLastImport { get; }

    public string FileName => Entry.FileName ?? Entry.PlanNumber;
    public string? RelativePath => Entry.RelativePath;
    public string IndexText => Entry.PlanIndex ?? "—";
    public string TypeText => Entry.DocumentType;

    /// <summary>Ablageordner (Primärdatei-Pfad ohne Dateiname) — ohne System.IO (ADR-060).</summary>
    public string DirectoryText
    {
        get
        {
            var path = Entry.RelativePath;
            var file = Entry.FileName;
            if (path is null || file is null || path.Length <= file.Length)
                return path ?? "—";
            return path.EndsWith(file, StringComparison.OrdinalIgnoreCase)
                ? path[..^(file.Length + 1)]
                : path;
        }
    }

    /// <summary>Hinzugefügt-Datum (received_at) als dd.MM.yyyy.</summary>
    public string DateText
        => DateTime.TryParse(Entry.ReceivedAt, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt.ToLocalTime().ToString("dd.MM.yyyy")
            : string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
