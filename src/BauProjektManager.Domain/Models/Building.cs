namespace BauProjektManager.Domain.Models;

/// <summary>
/// Ein Gebäude innerhalb eines Bauprojekts.
/// </summary>
public class Building
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string> Levels { get; set; } = [];
}
