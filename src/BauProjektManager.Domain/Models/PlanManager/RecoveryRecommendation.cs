using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Empfehlung des <see cref="Services.RecoveryDecisionService"/> für einen pending Import.
/// Enthält die empfohlene Aktion, eine User-lesbare Begründung und die Information ob
/// die Aktion ohne User-Bestätigung automatisch ausgeführt werden darf.
/// Siehe BPM-016 / 016.02.
/// </summary>
/// <param name="Action">Empfohlene Recovery-Aktion.</param>
/// <param name="Reason">Begründung als kurzer User-lesbarer Satz für den Recovery-Dialog.</param>
/// <param name="IsAutomaticAllowed">
/// True wenn die Aktion ohne User-Bestätigung sicher ausgeführt werden kann
/// (z.B. Forward bei IsRollbackTrivial — es ist faktisch nichts passiert).
/// False bedeutet: User-Bestätigung im Recovery-Dialog (016.04) erforderlich.
/// </param>
public sealed record RecoveryRecommendation(
    RecoveryAction Action,
    string Reason,
    bool IsAutomaticAllowed);
