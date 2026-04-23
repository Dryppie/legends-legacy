namespace Services.LL.Combat.Layers.Orchestration.Models;

public static class CombatOrchestrationResults
{
    public static CombatOrchestrationResult None(
        CombatMode mode, ICombatOrchestrationDetails details)
    {
        return new CombatOrchestrationResult(
            SessionId: Guid.NewGuid(),
            Mode: mode,
            Encounters: [],
            Details: details);
    }
}