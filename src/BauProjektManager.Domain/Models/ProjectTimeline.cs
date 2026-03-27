namespace BauProjektManager.Domain.Models;

/// <summary>
/// Zeitplan eines Bauprojekts.
/// </summary>
public class ProjectTimeline
{
    public DateTime? ProjectStart { get; set; }
    public DateTime? ConstructionStart { get; set; }
    public DateTime? PlannedEnd { get; set; }
    public DateTime? ActualEnd { get; set; }
}
