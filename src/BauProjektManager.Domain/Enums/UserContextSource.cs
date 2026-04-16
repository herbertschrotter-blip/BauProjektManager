namespace BauProjektManager.Domain.Enums;

/// <summary>
/// Quelle des Benutzerkontexts.
/// Modus A: Local (aus settings.json). Modus C: Server (aus JWT-Claims).
/// </summary>
public enum UserContextSource
{
    Local,
    Server
}
