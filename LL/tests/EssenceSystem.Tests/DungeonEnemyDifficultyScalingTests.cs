using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Services.LL.Combat.Layers.Resolution.Dungeon;

namespace EssenceSystem.Tests;

public sealed class DungeonEnemyDifficultyScalingTests
{
    [Theory]
    [InlineData(1, 1.6)]
    [InlineData(2, 2.0)]
    [InlineData(3, 2.0)]
    public void GetStrengthMultiplier_uses_content_pressure_after_the_shared_baseline(
        int dungeonTier,
        float expectedMultiplier)
    {
        Assert.Equal(
            expectedMultiplier,
            DungeonEnemyDifficultyScaling.GetStrengthMultiplier(dungeonTier));
    }

    [Fact]
    public void GetStrengthMultiplier_uses_a_valid_authored_override()
    {
        Assert.Equal(1.68f, DungeonEnemyDifficultyScaling.GetStrengthMultiplier(1, 1.05f), 3);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(3, 30)]
    public void GetProgressionPosition_anchors_each_tier_to_a_region_end(
        int dungeonTier,
        int expectedPosition)
    {
        Assert.Equal(
            expectedPosition,
            DungeonEnemyDifficultyScaling.GetProgressionPosition(dungeonTier));
    }

    [Theory]
    [InlineData(1, 160, 16)]
    [InlineData(2, 200, 20)]
    [InlineData(3, 200, 20)]
    public void Apply_scales_core_stats_without_scaling_capped_rates(
        int dungeonTier,
        int expectedMaxHealth,
        int expectedPower)
    {
        var entity = new Character
        {
            BaseAttributes =
            [
                new EntityAttribute { AttributeType = AttributeType.MaxHealth, Value = 100 },
                new EntityAttribute { AttributeType = AttributeType.Power, Value = 10 },
                new EntityAttribute { AttributeType = AttributeType.CritChance, Value = 5 }
            ]
        };
        var enemy = new CombatEntity(entity);

        DungeonEnemyDifficultyScaling.Apply(enemy, dungeonTier);
        AttributeCalculator.CalculateBaseCombatAttributes(enemy);

        Assert.Equal(expectedMaxHealth, enemy.GetAttributeValue(AttributeType.MaxHealth));
        Assert.Equal(expectedPower, enemy.GetAttributeValue(AttributeType.Power));
        Assert.Equal(5, enemy.GetAttributeValue(AttributeType.CritChance));
    }
}
