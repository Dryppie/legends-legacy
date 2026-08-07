using Domain.Models.Snapshots;
using Services.LL.Combat.Layers.Orchestration.Dungeon;

namespace EssenceSystem.Tests;

public sealed class DungeonCombatPlannerTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    public void Dungeon_plan_carries_the_selected_difficulty_tier(
        int requestedTier,
        int expectedTier)
    {
        var characterId = Guid.NewGuid();
        var plan = new DungeonCombatPlanner().CreatePlan(
            Guid.NewGuid(),
            characterId,
            new CharacterSnapshot { CharacterId = characterId },
            requestedTier,
            [characterId],
            [Guid.NewGuid()]);

        Assert.Equal(expectedTier, plan.DungeonTier);
    }

    [Fact]
    public void Dungeon_plan_carries_the_authored_enemy_strength_multiplier()
    {
        var characterId = Guid.NewGuid();
        var plan = new DungeonCombatPlanner().CreatePlan(
            Guid.NewGuid(),
            characterId,
            new CharacterSnapshot { CharacterId = characterId },
            1,
            [characterId],
            [Guid.NewGuid()],
            enemyStrengthMultiplier: 3.7f);

        Assert.Equal(3.7f, plan.EnemyStrengthMultiplier);
    }
}
