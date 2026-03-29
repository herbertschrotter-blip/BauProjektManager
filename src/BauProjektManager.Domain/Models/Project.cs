using BauProjektManager.Domain.Enums;

namespace BauProjektManager.Domain.Models;

/// <summary>
/// Ein Bauprojekt — zentrale Entität im BauProjektManager.
/// </summary>
public class Project
{
    public string Id { get; set; } = string.Empty;
    public string ProjectNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    /// <summary>
    /// Projektart (z.B. "Neubau", "Sanierung", "Umbau").
    /// Kein Enum — die Liste ist vom User editierbar (settings.json).
    /// </summary>
    public string ProjectType { get; set; } = string.Empty;

    public ProjectLocation Location { get; set; } = new();
    public ProjectTimeline Timeline { get; set; } = new();
    public Client Client { get; set; } = new();

    /// <summary>
    /// Bauteile/Bauabschnitte mit eigenen Geschossen und Höhen.
    /// Ersetzt die alte Buildings-Liste.
    /// </summary>
    public List<BuildingPart> BuildingParts { get; set; } = [];

    /// <summary>
    /// Beteiligte Firmen/Personen am Projekt (Architekt, Statiker, ÖBA etc.).
    /// Projektbezogen gespeichert, später mit zentralem Adressbuch verknüpft.
    /// </summary>
    public List<ProjectParticipant> Participants { get; set; } = [];

    public ProjectPaths Paths { get; set; } = new();
    public string Tags { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Ordnername im Format YYYYMM_Kurzname (z.B. 202512_ÖWG-Dobl-Zwaring).
    /// </summary>
    public string FolderName => $"{ProjectNumber}_{Name}";

    /// <summary>
    /// Generiert die Projektnummer aus dem Projektstart-Datum (YYYYMM).
    /// </summary>
    public void UpdateProjectNumberFromStart()
    {
        if (Timeline.ProjectStart.HasValue)
        {
            ProjectNumber = Timeline.ProjectStart.Value.ToString("yyyyMM");
        }
    }
}
