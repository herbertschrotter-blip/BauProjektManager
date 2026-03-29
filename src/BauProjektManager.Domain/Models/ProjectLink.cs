namespace BauProjektManager.Domain.Models;

/// <summary>
/// Ein externer Link oder ein Bauherren-Portal für ein Bauprojekt.
/// Wird im Dashboard als klickbarer Button angezeigt.
/// </summary>
public class ProjectLink
{
    /// <summary>
    /// Eindeutige ID (plink_001, plink_002...).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Bezeichnung des Links (z.B. "InfoRaum", "PlanRadar", "SharePoint", "LV Online").
    /// Bei Portalen: aus der Portal-Typen-Liste (settings.json).
    /// Bei eigenen Links: frei wählbar.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL des Links (z.B. "https://inforaum.oewg.at/proj/dobl-ba02").
    /// Leer wenn Portal noch nicht konfiguriert.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Typ des Links: "Portal" (Bauherren-Portal) oder "Custom" (eigener Link).
    /// </summary>
    public string LinkType { get; set; } = "Custom";

    /// <summary>
    /// Sortierreihenfolge im UI.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Ob der Link konfiguriert ist (URL vorhanden).
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Url);
}
