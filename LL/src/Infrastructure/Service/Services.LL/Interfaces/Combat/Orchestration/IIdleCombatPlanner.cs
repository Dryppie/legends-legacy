using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Interfaces.Combat.Orchestration;

public interface IIdleCombatPlanner
{
    IdleCombatPlan CreatePlan(IdleCombatOrchestrationRequest request);

    CombatEncounterPlan CreateEncounterPlan(
        IdleCombatPlan plan,
        int sequence,
        DateTimeOffset startsAt);
}
