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
    [InlineData(1, 3.6)]
    [InlineData(2, 6.25)]
    [InlineData(3, 8.2)]
    public void GetStrengthMultiplier_uses_the_calibrated_tier_curve(
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
        Assert.Equal(3.7f, DungeonEnemyDifficultyScaling.GetStrengthMultiplier(1, 3.7f));
    }

    [Theory]
    [InlineData(1, 360, 36)]
    [InlineData(2, 625, 62)]
    [InlineData(3, 820, 82)]
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
