namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// Geräte-Kontext für Sync-History und Audit (zusätzlich zu IUserContext).
/// Stellt eine stabile Geräte-ID bereit, persistent in
/// %LocalAppData%\BauProjektManager\device-settings.json.
/// Siehe ADR-053 Punkt 12.
/// </summary>
public interface IDeviceContext
{
    /// <summary>
    /// Stabile Geräte-ID (ULID). Persistent in device-settings.json.
    /// Wird beim ersten Start einmalig erzeugt.
    /// </summary>
    string DeviceId { get; }

    /// <summary>
    /// Lesbarer Geräte-Name für Audit/Logs. Default: Environment.MachineName.
    /// Kann via device-settings.json überschrieben werden.
    /// </summary>
    string DeviceName { get; }
}
