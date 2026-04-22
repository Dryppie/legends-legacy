namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record PvpCombatOrchestrationRequest(
    Guid MatchId,
    Guid AttackerCharacterId,
    Guid DefenderCharacterId,
    DateTimeOffset Now)
    : CombatOrchestrationRequest(CombatMode.Pvp);