using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed record CombatOutcomeRequest(
    CombatOrchestrationRequest OrchestrationRequest,
    CombatOrchestrationResult OrchestrationResult);
