using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;

namespace BauProjektManager.Infrastructure.Services;

/// <summary>
/// Lokaler Geräte-Kontext für Sync-History und Audit (zusätzlich zu IUserContext).
/// Wrapper über das bestehende <see cref="DeviceSettings"/>-Modell —
/// liest <see cref="DeviceSettings.DeviceId"/> und <see cref="DeviceSettings.MachineName"/>.
/// Persistenz erfolgt durch <see cref="Persistence.AppSettingsService"/>.
/// Siehe ADR-053 Punkt 12.
/// </summary>
public class LocalDeviceContext : IDeviceContext
{
    private readonly DeviceSettings _deviceSettings;

    public LocalDeviceContext(DeviceSettings deviceSettings)
    {
        _deviceSettings = deviceSettings;
    }

    public string DeviceId => _deviceSettings.DeviceId;
    public string DeviceName => _deviceSettings.MachineName;
}
