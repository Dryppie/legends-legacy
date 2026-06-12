using Domain.Models.Attributes;

namespace Domain.Models.Entities.Creatures;

public static class MonsterBaseStats
{
    // Tier-1-ish baseline. Adjust until combat "feels" right.
    public const float BaseMaxHealth = 100f;
    public const float BaseHealthRegeneration = 0f;

    public const float BasePower = 10f;
    public const float BaseFortitude = 10f;
    public const float BasePrecision = 10f;
    public const float BaseSpirit = 10f;
    public const float BaseWeaponDamage = 0f;
    public const float BaseArmor = 10f;
    public const float BaseResistance = 10f;
    public const float BaseCritChance = 0.05f;
    public const float BaseCritDamage = 1.5f;
    public const float BaseArmorPenetration = 0.0f;
    public const float BaseMagicPenetration = 0.0f;
    public const float BaseDamageReduction = 0.0f;
    public const float BaseDodgeChance = 0.0f;
    public const float BaseBlockChance = 0.0f;
    public const float BaseHealingPowerPercent = 0.0f;
    public const float BaseLifeSteal = 0.0f;
    public const float BaseCooldown = 0.0f;
    public const float BaseStatusResistance = 0.0f;
    public const float BaseCrowdControlResistance = 0.0f;
    public const float BaseSummonPower = 0.0f;
    public const float BaseSummonHealth = 0.0f;

    public static readonly IReadOnlyDictionary<AttributeType, float> Baseline =
        new Dictionary<AttributeType, float>
        {
                { AttributeType.Power, BasePower },
                { AttributeType.Fortitude, BaseFortitude },
                { AttributeType.Precision, BasePrecision },
                { AttributeType.Spirit, BaseSpirit },
                { AttributeType.MaxHealth, BaseMaxHealth },
                { AttributeType.WeaponDamage, BaseWeaponDamage },
                { AttributeType.Armor, BaseArmor },
                { AttributeType.Resistance, BaseResistance },
                { AttributeType.CritChance, BaseCritChance },
                { AttributeType.CritDamage, BaseCritDamage },
                { AttributeType.ArmorPenetration, BaseArmorPenetration },
                { AttributeType.MagicPenetration, BaseMagicPenetration },
                { AttributeType.DodgeChance, BaseDodgeChance },
                { AttributeType.BlockChance, BaseBlockChance },
                { AttributeType.DamageReduction, BaseDamageReduction },
                { AttributeType.HealingPowerPercent, BaseHealingPowerPercent },
                { AttributeType.HealthRegeneration, BaseHealthRegeneration },
                { AttributeType.LifeSteal, BaseLifeSteal },
                { AttributeType.Cooldown, BaseCooldown },
                { AttributeType.StatusResistance, BaseStatusResistance },
                { AttributeType.CrowdControlResistance, BaseCrowdControlResistance },
                { AttributeType.SummonPower, BaseSummonPower },
                { AttributeType.SummonHealth, BaseSummonHealth },
        };
}
