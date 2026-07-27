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
    [InlineData(1, 3)]
    [InlineData(2, 15)]
    [InlineData(3, 75)]
    public void GetStrengthMultiplier_uses_the_calibrated_tier_curve(
        int dungeonTier,
        float expectedMultiplier)
    {
        Assert.Equal(
            expectedMultiplier,
            DungeonEnemyDifficultyScaling.GetStrengthMultiplier(dungeonTier));
    }

    [Theory]
    [InlineData(1, 300, 30)]
    [InlineData(2, 1500, 150)]
    [InlineData(3, 7500, 750)]
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
        enemy.BaseAttributes.Add(new EntityAttribute
        {
            AttributeType = AttributeType.Spirit,
            Value = 0
        });
        enemy.TemporaryModifiers.Add(new DungeonAttributeModifier(
            AttributeType.Spirit,
            10,
            ModifierType.Flat));
        AttributeCalculator.CalculateBaseCombatAttributes(enemy);

        Assert.Equal(expectedMaxHealth, enemy.GetAttributeValue(AttributeType.MaxHealth));
        Assert.Equal(expectedPower, enemy.GetAttributeValue(AttributeType.Power));
        Assert.Equal(
            dungeonTier switch
            {
                1 => 30,
                2 => 150,
                _ => 750
            },
            enemy.GetAttributeValue(AttributeType.Spirit));
        Assert.Equal(5, enemy.GetAttributeValue(AttributeType.CritChance));
    }
}
