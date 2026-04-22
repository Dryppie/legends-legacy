namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record RaidCombatOrchestrationRequest(
    Guid RaidRunId,
    Guid RaidPartyId,
    DateTimeOffset Now)
    : CombatOrchestrationRequest(CombatMode.Raid);