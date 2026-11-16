using BauProjektManager.Domain.Models;

namespace BauProjektManager.Tests;

/// <summary>
/// R01-004.04 (Audit Review 01, SE-05): Typ-Metadaten der Ordnervorlage (ADR-061 Punkt 2)
/// ueberleben den Weg durch die Vorlagen-UI — Lesen in den Baum, Schreiben zurueck.
/// </summary>
public class FolderTypeMetadataTests
{
    private static IEnumerable<(string Name, FolderTypeMetadata Meta)> Flatten(List<FolderTemplateEntry> template)
    {
        foreach (var main in template)
        {
            yield return (main.Name, FolderTypeMetadata.From(main));
            foreach (var sub in FlattenSubs(main.SubFolders))
                yield return sub;
        }
    }

    private static IEnumerable<(string Name, FolderTypeMetadata Meta)> FlattenSubs(List<SubFolderEntry> subs)
    {
        foreach (var sub in subs)
        {
            yield return (sub.Name, FolderTypeMetadata.From(sub));
            foreach (var deeper in FlattenSubs(sub.SubFolders))
                yield return deeper;
        }
    }

    [Fact]
    public void DefaultTemplate_RoundTrip_KeepsAllDocumentTypeNodes()
    {
        var original = SharedConfigDefaults.GetDefaultFolderTemplate();
        Assert.Contains(Flatten(original), n => n.Meta.CreatesDocumentType);

        // Simuliert LoadFromTemplate -> ToTemplate der Vorlagen-UI: nur Name/Struktur +
        // mitgefuehrte Metadaten werden in neue Eintraege geschrieben.
        var copy = original.Select(main =>
        {
            var entry = new FolderTemplateEntry { Name = main.Name, HasInbox = main.HasInbox,
                SubFolders = CopySubs(main.SubFolders) };
            FolderTypeMetadata.From(main).ApplyTo(entry);
            return entry;
        }).ToList();

        var before = Flatten(original).ToList();
        var after = Flatten(copy).ToList();
        Assert.Equal(before.Count, after.Count);
        for (var i = 0; i < before.Count; i++)
        {
            Assert.Equal(before[i].Name, after[i].Name);
            Assert.Equal(before[i].Meta.CreatesDocumentType, after[i].Meta.CreatesDocumentType);
            Assert.Equal(before[i].Meta.DocumentTypeKey, after[i].Meta.DocumentTypeKey);
            Assert.Equal(before[i].Meta.DocumentTypeDisplayName, after[i].Meta.DocumentTypeDisplayName);
            Assert.Equal(before[i].Meta.Ring2Source, after[i].Meta.Ring2Source);
            Assert.Equal(before[i].Meta.Categories.Select(c => c.Name), after[i].Meta.Categories.Select(c => c.Name));
        }
    }

    private static List<SubFolderEntry> CopySubs(List<SubFolderEntry> subs) =>
        [.. subs.Select(s =>
        {
            var entry = new SubFolderEntry { Name = s.Name, HasPrefix = s.HasPrefix, SubFolders = CopySubs(s.SubFolders) };
            FolderTypeMetadata.From(s).ApplyTo(entry);
            return entry;
        })];

    [Fact]
    public void NewFolder_HasNoDocumentType()
    {
        var entry = new SubFolderEntry { Name = "Neu" };
        FolderTypeMetadata.None.ApplyTo(entry);
        Assert.False(entry.CreatesDocumentType);
        Assert.Null(entry.DocumentTypeKey);
        Assert.Empty(entry.Categories);
    }
}
