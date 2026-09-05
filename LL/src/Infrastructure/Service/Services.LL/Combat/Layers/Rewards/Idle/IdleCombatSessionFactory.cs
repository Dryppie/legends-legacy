using Domain.Models.Items.Equipments.Progression;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Inventories;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class IdleCombatSessionFactory : IIdleCombatSessionFactory
{
    public CombatSession Create(IdleCombatRewardFacts facts, IdleCombatCalculatedOutcome outcome)
    {
        var lastCombatResult = facts.LastEncounter?.CombatResult ?? new CombatResult();

        // Preserve the existing CombatResult contract, but return the complete
        // offline interval instead of only the final encounter's rewards. The
        // response is compacted by item base so a 24-hour return stays bounded.
        lastCombatResult.Loot = SummarizeItems(outcome.TotalLoot);
        lastCombatResult.ExperienceGained = outcome.TotalExperience;


        var summary = new CombatSummary
        {
            TotalBattles = facts.Encounters.Count,
            Wins = facts.Encounters.Count(x => x.Outcome == BattleOutcome.Victory),
            Losses = facts.Encounters.Count(x => x.Outcome == BattleOutcome.Defeat),
            Draws = facts.Encounters.Count(x => x.Outcome == BattleOutcome.Draw),
            TotalExperience = outcome.TotalExperience,
            TotalCinders = outcome.TotalCinders,
            TotalSoulstones = outcome.TotalSoulstones,
            RewardBreakdown = new CombatRewardBreakdown
            {
                PowerItems = SummarizeItems(outcome.PowerRewards),
                MiscellaneousItems = SummarizeItems(outcome.MiscellaneousRewards),
                EssenceItems = SummarizeItems(outcome.EssenceRewards),
                DungeonAccessItems = SummarizeItems(outcome.DungeonAccessRewards)
            }
        };

        return new CombatSession
        {
            From = facts.From,
            To = facts.ProcessedUntil,
            CombatResult = lastCombatResult,
            CombatSummary = summary
        };
    }

    private static List<InventoryItem> SummarizeItems(
        IReadOnlyList<InventoryItem> items) =>
        items
            .GroupBy(
                item => item.ItemInstance is Domain.Models.Items.Equipments.EquipmentInstance { ProgressionData: not null }
                    ? EquipmentKeys.SourcePrefix + $"{item.ItemInstanceId:N}" : $"base:{item.ItemInstance.ItemBaseId}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                return new InventoryItem
                {
                    InventoryId = first.InventoryId,
                    ItemInstanceId = first.ItemInstanceId,
                    ItemInstance = first.ItemInstance,
                    Quantity = group.Sum(item => item.Quantity)
                };
            })
            .OrderBy(item => item.ItemInstance.ItemBase.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
