using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class IdleCombatSessionFactory : IIdleCombatSessionFactory
{
    public CombatSession Create(IdleCombatRewardFacts facts, IdleCombatCalculatedOutcome outcome)
    {
        var lastCombatResult = facts.LastEncounter?.CombatResult ?? new CombatResult();
        var lastEncounterOutcome = outcome.LastEncounterOutcome;

        // Backward-compatibility bridge:
        // your API already exposes reward fields on CombatResult.
        // Keep it for now, but this is presentation glue, not core resolution data.
        if (lastEncounterOutcome is not null)
        {
            lastCombatResult.Loot = [.. lastEncounterOutcome.Loot];
            lastCombatResult.ExperienceGained = lastEncounterOutcome.ExperienceGained;
        }

        var summary = new CombatSummary
        {
            TotalExperience = outcome.TotalExperience,
            TotalCinders = outcome.TotalCinders,
            TotalSoulstones = outcome.TotalSoulstones
        };

        // If your existing CombatSummary has more counters,
        // populate them here from facts.Encounters instead of inside the reward calculator.

        return new CombatSession
        {
            From = facts.From,
            To = facts.ProcessedUntil,
            CombatResult = lastCombatResult,
            CombatSummary = summary
        };
    }
}
