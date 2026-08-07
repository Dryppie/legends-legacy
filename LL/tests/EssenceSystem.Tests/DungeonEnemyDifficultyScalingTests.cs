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
    [InlineData(1, 2.75)]
    [InlineData(2, 13.75)]
    [InlineData(3, 68.75)]
    public void GetStrengthMultiplier_uses_the_calibrated_tier_curve(
        int dungeonTier,
        float expectedMultiplier)
    {
        Assert.Equal(
            expectedMultiplier,
            DungeonEnemyDifficultyScaling.GetStrengthMultiplier(dungeonTier));
    }

    [Theory]
    [InlineData(1, 275, 27)]
    [InlineData(2, 1375, 137)]
    [InlineData(3, 16843, 687)]
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
