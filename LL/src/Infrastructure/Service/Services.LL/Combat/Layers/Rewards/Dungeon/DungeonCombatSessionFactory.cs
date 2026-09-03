using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

internal class DungeonCombatSessionFactory : IDungeonCombatSessionFactory
{
    public CombatSession Create(DungeonCombatRewardFacts facts, DungeonCombatCalculatedOutcome outcome)
    {
        var lastCombatResult = facts.LastEncounter?.CombatResult ?? new CombatResult();
        var lastEncounterOutcome = outcome.LastEncounterOutcome;

        // Backward-compatibility bridge:
        // your API already exposes reward fields on CombatResult.
        // Keep it for now, but this is presentation glue, not core resolution data.
        if (lastEncounterOutcome is not null)
        {
            lastCombatResult.Loot = [.. outcome.TotalLoot];
            lastCombatResult.ExperienceGained = outcome.TotalExperience;
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
            From = DateTimeOffset.UtcNow,
            To = DateTimeOffset.UtcNow,
            CombatResult = lastCombatResult,
            CombatSummary = summary
        };
    }
}
