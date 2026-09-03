using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;

namespace EssenceSystem.Tests;

public sealed class IdleCombatSessionFactoryTests
{
    [Fact]
    public void Create_returns_complete_interval_rewards_grouped_by_purpose()
    {
        var from = DateTimeOffset.Parse("2026-07-24T08:00:00Z");
        var to = from.AddHours(2);
        var power = CreateItem("sword", "Sword");
        var crafting = CreateItem("ore", "Ore", 3);
        var moreCrafting = CreateItem("ore", "Ore", 2);
        var essence = CreateItem("essence.goblin", "Goblin Essence");
        var sigil = CreateItem("sigil_goblin_mines", "Goblin Sigil");
        var lastEncounterResult = new CombatResult
        {
            Loot = [CreateItem("old-last-drop", "Old Last Drop")],
            ExperienceGained = 10
        };
        var facts = new IdleCombatRewardFacts(
            Guid.NewGuid(),
            from,
            to,
            to,
            to - from,
            new Area { Id = "test-area" },
            [],
            [
                new IdleEncounterRewardFacts(
                    Guid.NewGuid(),
                    1,
                    from,
                    BattleOutcome.Victory,
                    [],
                    [],
                    lastEncounterResult)
            ]);
        var outcome = new IdleCombatCalculatedOutcome(
            facts.CharacterId,
            from,
            to,
            250,
            12,
            4,
            [power, crafting, moreCrafting, essence, sigil],
            [power],
            [crafting, moreCrafting],
            [essence],
            [sigil],
            []);

        var session = new IdleCombatSessionFactory().Create(facts, outcome);

        Assert.Equal(4, session.CombatResult.Loot.Count);
        Assert.Equal(250, session.CombatResult.ExperienceGained);
        Assert.Equal(
            "sword",
            Assert.Single(session.CombatSummary.RewardBreakdown.PowerItems)
                .ItemInstance.ItemBaseId);
        var summarizedCrafting = Assert.Single(session.CombatSummary.RewardBreakdown.CraftingItems);
        Assert.Equal("ore", summarizedCrafting.ItemInstance.ItemBaseId);
        Assert.Equal(5, summarizedCrafting.Quantity);
        Assert.Equal(
            "essence.goblin",
            Assert.Single(session.CombatSummary.RewardBreakdown.EssenceItems)
                .ItemInstance.ItemBaseId);
        Assert.Equal(
            "sigil_goblin_mines",
            Assert.Single(session.CombatSummary.RewardBreakdown.DungeonAccessItems)
                .ItemInstance.ItemBaseId);
        Assert.Equal(12, session.CombatSummary.TotalCinders);
        Assert.Equal(4, session.CombatSummary.TotalSoulstones);
    }

    [Fact]
    public void Create_handles_loss_with_no_rewards()
    {
        var now = DateTimeOffset.Parse("2026-07-24T10:00:00Z");
        var facts = new IdleCombatRewardFacts(
            Guid.NewGuid(),
            now,
            now,
            now,
            TimeSpan.Zero,
            new Area { Id = "test-area" },
            [],
            [
                new IdleEncounterRewardFacts(
                    Guid.NewGuid(),
                    1,
                    now,
                    BattleOutcome.Defeat,
                    [],
                    [],
                    new CombatResult { Outcome = BattleOutcome.Defeat })
            ]);
        var outcome = new IdleCombatCalculatedOutcome(
            facts.CharacterId,
            now,
            now,
            0,
            0,
            0,
            [],
            [],
            [],
            [],
            [],
            []);

        var session = new IdleCombatSessionFactory().Create(facts, outcome);

        Assert.Equal(1, session.CombatSummary.TotalBattles);
        Assert.Equal(1, session.CombatSummary.Losses);
        Assert.Empty(session.CombatResult.Loot);
        Assert.Empty(session.CombatSummary.RewardBreakdown.PowerItems);
        Assert.Empty(session.CombatSummary.RewardBreakdown.CraftingItems);
        Assert.Empty(session.CombatSummary.RewardBreakdown.EssenceItems);
        Assert.Empty(session.CombatSummary.RewardBreakdown.DungeonAccessItems);
    }

    private static InventoryItem CreateItem(string id, string name, int quantity = 1) =>
        new()
        {
            Quantity = quantity,
            ItemInstance = new ItemInstance
            {
                Id = Guid.NewGuid(),
                ItemBaseId = id,
                ItemBase = new ItemBase { Id = id, Name = name }
            }
        };
}
