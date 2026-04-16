using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;

namespace BauProjektManager.Infrastructure.Services;

/// <summary>
/// Lokaler Benutzerkontext für Modus A (Offline/Solo).
/// Liest UserId und DisplayName aus AppSettings (settings.json).
/// Wird in Modus C durch JwtUserContext ersetzt.
/// Siehe ADR-052.
/// </summary>
public class LocalUserContext : IUserContext
{
    private readonly AppSettings _settings;

    public LocalUserContext(AppSettings settings)
    {
        _settings = settings;
    }

    public string UserId => _settings.LocalUserId;
    public string DisplayName => _settings.LocalUserName;
    public UserContextSource Source => UserContextSource.Local;
}
