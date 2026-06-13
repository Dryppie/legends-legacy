using Domain.Models.Attributes;

namespace Domain.Components.Attributes;

public static class CombatRatingCalculator
{
    public static int Calculate(IReadOnlyDictionary<AttributeType, float> attributes, int characterLevel)
    {
        var primary =
            Get(attributes, AttributeType.Power) * 8 +
            Get(attributes, AttributeType.Fortitude) * 8 +
            Get(attributes, AttributeType.Precision) * 8 +
            Get(attributes, AttributeType.Spirit) * 5;

        var offense =
            Get(attributes, AttributeType.WeaponDamage) * 18 +
            Get(attributes, AttributeType.CritChance) * 4 +
            Get(attributes, AttributeType.CritDamage) * 1.5f +
            Get(attributes, AttributeType.ArmorPenetration) * 2 +
            Get(attributes, AttributeType.MagicPenetration) * 2;

        var defense =
            Get(attributes, AttributeType.MaxHealth) * 0.18f +
            Get(attributes, AttributeType.Armor) * 4 +
            Get(attributes, AttributeType.Resistance) * 4 +
            Get(attributes, AttributeType.DodgeChance) * 5 +
            Get(attributes, AttributeType.BlockChance) * 3 +
            Get(attributes, AttributeType.DamageReduction) * 7;

        var recovery =
            Get(attributes, AttributeType.HealingPowerPercent) * 2 +
            Get(attributes, AttributeType.HealthRegeneration) * 8 +
            Get(attributes, AttributeType.LifeSteal) * 4;

        var utility =
            Get(attributes, AttributeType.Cooldown) * 3 +
            Get(attributes, AttributeType.StatusResistance) * 2 +
            Get(attributes, AttributeType.CrowdControlResistance) * 2 +
            Get(attributes, AttributeType.SummonPower) * 4 +
            Get(attributes, AttributeType.SummonHealth) * 0.15f;

        var level = Math.Max(1, characterLevel) * 10;

        return Math.Max(0, (int)MathF.Round(primary + offense + defense + recovery + utility + level));
    }

    private static float Get(IReadOnlyDictionary<AttributeType, float> attributes, AttributeType type) =>
        attributes.GetValueOrDefault(type);
}
