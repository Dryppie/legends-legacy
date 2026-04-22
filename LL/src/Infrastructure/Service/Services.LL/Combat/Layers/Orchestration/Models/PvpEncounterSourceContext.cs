namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record PvpEncounterSourceContext(
    Guid MatchId,
    Guid AttackerCharacterId,
    Guid DefenderCharacterId)
    : CombatEncounterSourceContext(CombatMode.Pvp);