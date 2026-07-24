using Domain.Models.Attributes;

namespace Domain.Models.Professions.Crafting.V2;

public static class EquipmentStatBudgetCatalog
{
    private static readonly IReadOnlyDictionary<AttributeType, EquipmentStatBudgetRule> Rules =
        new Dictionary<AttributeType, EquipmentStatBudgetRule>
        {
            [AttributeType.Power] = new(1d, 500),
            [AttributeType.Fortitude] = new(1d, 500),
            [AttributeType.Precision] = new(1d, 500),
            [AttributeType.Spirit] = new(1d, 500),
            [AttributeType.MaxHealth] = new(0.2d, 2_500),
            [AttributeType.WeaponDamage] = new(1.5d, 400),
            [AttributeType.Armor] = new(0.18d, 500),
            [AttributeType.Resistance] = new(0.18d, 500),
            [AttributeType.CritChance] = new(4d, 75),
            [AttributeType.CritDamage] = new(2d, 250),
            [AttributeType.ArmorPenetration] = new(3d, 100),
            [AttributeType.MagicPenetration] = new(3d, 100),
            [AttributeType.DodgeChance] = new(5d, 50),
            [AttributeType.BlockChance] = new(5d, 50),
            [AttributeType.DamageReduction] = new(6d, AttributeCombatRules.DamageReductionCapPercent),
            [AttributeType.HealingPowerPercent] = new(3d, 100),
            [AttributeType.HealthRegeneration] = new(1.5d, 300),
            [AttributeType.LifeSteal] = new(6d, 50),
            [AttributeType.Cooldown] = new(6d, AttributeCombatRules.CooldownReductionCapPercent),
            [AttributeType.StatusResistance] = new(2d, 150),
            [AttributeType.CrowdControlResistance] = new(2d, 150),
            [AttributeType.SummonPower] = new(1.5d, 300),
            [AttributeType.SummonHealth] = new(0.5d, 1_000),
            [AttributeType.AttackSpeed] = new(3d, 200)
        };

    public static EquipmentStatBudgetRule Get(AttributeType stat) =>
        Rules.TryGetValue(stat, out var rule)
            ? rule
            : throw new InvalidOperationException($"No equipment budget rule exists for '{stat}'.");

    public static bool IsKnown(AttributeType stat) => Rules.ContainsKey(stat);

    public static IReadOnlyDictionary<AttributeType, EquipmentStatBudgetRule> All => Rules;
}

public sealed record EquipmentStatBudgetRule(double CostPerPoint, float HardCap);
