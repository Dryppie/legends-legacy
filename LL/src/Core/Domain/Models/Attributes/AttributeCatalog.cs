namespace Domain.Models.Attributes;

public static class AttributeCatalog
{
    private static readonly IReadOnlyDictionary<AttributeType, AttributeDefinition> Definitions =
        new Dictionary<AttributeType, AttributeDefinition>
        {
            [AttributeType.Power] = new(AttributeType.Power, "Primary offensive force."),
            [AttributeType.Fortitude] = new(AttributeType.Fortitude, "Primary toughness."),
            [AttributeType.Precision] = new(AttributeType.Precision, "Primary accuracy and critical reliability."),
            [AttributeType.Spirit] = new(AttributeType.Spirit, "Primary resource and magical affinity."),

            [AttributeType.MaxHealth] = new(AttributeType.MaxHealth, "Maximum health."),
            [AttributeType.WeaponDamage] = new(AttributeType.WeaponDamage, "Weapon damage input."),
            [AttributeType.Armor] = new(AttributeType.Armor, "Physical mitigation input."),
            [AttributeType.Resistance] = new(AttributeType.Resistance, "Magical and elemental mitigation input."),
            [AttributeType.CritChance] = new(AttributeType.CritChance, "Critical strike chance."),
            [AttributeType.CritDamage] = new(AttributeType.CritDamage, "Critical strike damage."),
            [AttributeType.ArmorPenetration] = new(AttributeType.ArmorPenetration, "Physical defense bypass."),
            [AttributeType.MagicPenetration] = new(AttributeType.MagicPenetration, "Magical defense bypass."),

            [AttributeType.DodgeChance] = new(AttributeType.DodgeChance, "Dodge chance."),
            [AttributeType.BlockChance] = new(AttributeType.BlockChance, "Block chance."),
            [AttributeType.DamageReduction] = new(AttributeType.DamageReduction, "General damage reduction."),

            [AttributeType.HealingPowerPercent] = new(AttributeType.HealingPowerPercent, "Healing output percentage."),
            [AttributeType.HealthRegeneration] = new(AttributeType.HealthRegeneration, "Health regeneration input."),
            [AttributeType.LifeSteal] = new(AttributeType.LifeSteal, "Life restored from damage dealt."),

            [AttributeType.Cooldown] = new(AttributeType.Cooldown, "Cooldown improvement."),
            [AttributeType.StatusResistance] = new(AttributeType.StatusResistance, "Status effect resistance."),
            [AttributeType.CrowdControlResistance] = new(AttributeType.CrowdControlResistance, "Crowd-control resistance."),

            [AttributeType.SummonPower] = new(AttributeType.SummonPower, "Summoned unit damage scaling."),
            [AttributeType.SummonHealth] = new(AttributeType.SummonHealth, "Summoned unit health scaling."),

            [AttributeType.AttackSpeed] = new(AttributeType.AttackSpeed, "Basic attack speed percentage.")
        };

    public static IReadOnlyCollection<AttributeDefinition> All => [.. Definitions.Values];

    public static AttributeDefinition Get(AttributeType attributeType) => Definitions[attributeType];

    public static bool IsKnown(AttributeType attributeType) => Definitions.ContainsKey(attributeType);

    public static bool IsContentFacing(AttributeType attributeType) =>
        Definitions.TryGetValue(attributeType, out var definition) && definition.IsContentFacing;
}
