using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.Domain.Models;

/// <summary>
/// Typ-Metadaten eines Vorlagen-Knotens (ADR-061 Punkt 2) als eigener Wert — damit die
/// Ordnervorlagen-UI sie beim Bearbeiten unsichtbar mitfuehrt und beim Speichern
/// unveraendert zurueckschreibt (R01-004.04 / SE-05). Ohne diesen Round-Trip verlor jede
/// Aenderung in den Einstellungen die CreatesDocumentType-Knoten in shared-config.json.
/// </summary>
public sealed record FolderTypeMetadata(
    bool CreatesDocumentType,
    string? DocumentTypeKey,
    string? DocumentTypeDisplayName,
    Ring2Source? Ring2Source,
    IReadOnlyList<FolderTemplateCategory> Categories)
{
    /// <summary>Kein Dokumenttyp (Default fuer neu angelegte Ordner).</summary>
    public static FolderTypeMetadata None { get; } = new(false, null, null, null, []);

    public static FolderTypeMetadata From(FolderTemplateEntry entry) =>
        new(entry.CreatesDocumentType, entry.DocumentTypeKey, entry.DocumentTypeDisplayName,
            entry.Ring2Source, [.. entry.Categories]);

    public static FolderTypeMetadata From(SubFolderEntry entry) =>
        new(entry.CreatesDocumentType, entry.DocumentTypeKey, entry.DocumentTypeDisplayName,
            entry.Ring2Source, [.. entry.Categories]);

    public void ApplyTo(FolderTemplateEntry entry)
    {
        entry.CreatesDocumentType = CreatesDocumentType;
        entry.DocumentTypeKey = DocumentTypeKey;
        entry.DocumentTypeDisplayName = DocumentTypeDisplayName;
        entry.Ring2Source = Ring2Source;
        entry.Categories = [.. Categories];
    }

    public void ApplyTo(SubFolderEntry entry)
    {
        entry.CreatesDocumentType = CreatesDocumentType;
        entry.DocumentTypeKey = DocumentTypeKey;
        entry.DocumentTypeDisplayName = DocumentTypeDisplayName;
        entry.Ring2Source = Ring2Source;
        entry.Categories = [.. Categories];
    }
}
