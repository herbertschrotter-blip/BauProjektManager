using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;

namespace BauProjektManager.Infrastructure.Services;

/// <summary>
/// Lokaler Benutzerkontext für Modus A (Offline/Solo).
/// Liest UserId und DisplayName aus DeviceSettings (device-settings.json, BPM-069).
/// Wird in Modus C durch JwtUserContext ersetzt.
/// Siehe ADR-052.
/// </summary>
public class LocalUserContext : IUserContext
{
    private readonly DeviceSettings _settings;

    public LocalUserContext(DeviceSettings settings)
    {
        _settings = settings;
    }

    public string UserId => _settings.LocalUserId;
    public string DisplayName => _settings.LocalUserName;
    public UserContextSource Source => UserContextSource.Local;
}
