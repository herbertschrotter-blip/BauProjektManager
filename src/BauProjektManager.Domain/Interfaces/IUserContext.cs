using BauProjektManager.Domain.Enums;

namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// Benutzerkontext für created_by/last_modified_by in Sync-fähigen Tabellen.
/// Modus A: LocalUserContext liest aus settings.json.
/// Modus C: JwtUserContext liest aus JWT-Claims (Post-V1).
/// Siehe ADR-052.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Technische User-ID. Modus A: "MachineName\UserName". Modus C: Server-User-ID.
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Lesbarer Anzeigename für created_by/last_modified_by.
    /// Modus A: aus settings.json (localUserName). Modus C: aus JWT DisplayName-Claim.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Quelle: Local (settings.json) oder Server (JWT).
    /// </summary>
    UserContextSource Source { get; }
}
