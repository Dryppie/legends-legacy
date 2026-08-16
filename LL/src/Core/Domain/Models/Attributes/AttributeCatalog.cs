namespace Domain.Models.Attributes;

public static class AttributeCatalog
{
    private static readonly IReadOnlyDictionary<AttributeType, AttributeDefinition> Definitions =
        new Dictionary<AttributeType, AttributeDefinition>
        {
            [AttributeType.Power] = Flat(
                AttributeType.Power,
                "Power",
                "Damage, healing, and barriers scale on Power.",
                Scenarios(
                    AttributeBenchmarkScenario.PhysicalOffense,
                    AttributeBenchmarkScenario.MagicalOffense,
                    AttributeBenchmarkScenario.PeriodicOffense,
                    AttributeBenchmarkScenario.HealingSustain,
                    AttributeBenchmarkScenario.SummonOffense),
                equipmentDisplayPrecision: 2),
            [AttributeType.MaxHealth] = Flat(
                AttributeType.MaxHealth,
                "Max Health",
                "Maximum health.",
                Scenarios(
                    AttributeBenchmarkScenario.MixedPressure,
                    AttributeBenchmarkScenario.UnmitigatedPressure,
                    AttributeBenchmarkScenario.BurstPressure,
                    AttributeBenchmarkScenario.LongSustain)),
            [AttributeType.Armor] = Percent(
                AttributeType.Armor,
                "Physical Damage Reduction",
                "Reduces incoming physical damage by this many percentage points.",
                AttributeCombatRules.TypedMitigationCapPercent,
                Scenarios(AttributeBenchmarkScenario.PhysicalPressure, AttributeBenchmarkScenario.MixedPressure),
                "Armor Rating"),
            [AttributeType.Resistance] = Percent(
                AttributeType.Resistance,
                "Magical Damage Reduction",
                "Reduces incoming magical and elemental damage by this many percentage points.",
                AttributeCombatRules.TypedMitigationCapPercent,
                Scenarios(AttributeBenchmarkScenario.MagicalPressure, AttributeBenchmarkScenario.MixedPressure),
                "Resistance Rating"),
            [AttributeType.CritChance] = Percent(
                AttributeType.CritChance,
                "Crit Chance",
                "Chance for direct damage and crit-eligible healing to critically strike.",
                AttributeCombatRules.CritChanceCapPercent,
                Scenarios(
                    AttributeBenchmarkScenario.PhysicalOffense,
                    AttributeBenchmarkScenario.MagicalOffense,
                    AttributeBenchmarkScenario.HealingSustain),
                "Critical Rating"),
            [AttributeType.CritDamage] = Percent(
                AttributeType.CritDamage,
                "Crit Damage",
                "Bonus output added when a critical strike occurs.",
                scenarios: Scenarios(
                    AttributeBenchmarkScenario.PhysicalOffense,
                    AttributeBenchmarkScenario.MagicalOffense,
                    AttributeBenchmarkScenario.HealingSustain),
                equipmentDisplayName: "Critical Damage Rating"),
            [AttributeType.ArmorPenetration] = Percent(
                AttributeType.ArmorPenetration,
                "Armor Penetration",
                "Removes this many percentage points of the target's Armor.",
                AttributeCombatRules.TypedPenetrationCapPercent,
                Scenarios(AttributeBenchmarkScenario.PhysicalOffense),
                "Armor Penetration Rating"),
            [AttributeType.MagicPenetration] = Percent(
                AttributeType.MagicPenetration,
                "Magic Penetration",
                "Removes this many percentage points of the target's Resistance.",
                AttributeCombatRules.TypedPenetrationCapPercent,
                Scenarios(AttributeBenchmarkScenario.MagicalOffense, AttributeBenchmarkScenario.PeriodicOffense),
                "Magic Penetration Rating"),

            [AttributeType.DodgeChance] = Percent(
                AttributeType.DodgeChance,
                "Dodge",
                "Chance to avoid a dodgeable incoming hit.",
                AttributeCombatRules.DodgeChanceCapPercent,
                Scenarios(
                    AttributeBenchmarkScenario.PhysicalPressure,
                    AttributeBenchmarkScenario.MagicalPressure,
                    AttributeBenchmarkScenario.MixedPressure),
                "Dodge Rating"),
            [AttributeType.BlockChance] = Percent(
                AttributeType.BlockChance,
                "Block",
                $"Chance to reduce a blockable hit by {AttributeCombatRules.BlockDamageReductionPercent:0}%.",
                AttributeCombatRules.BlockChanceCapPercent,
                Scenarios(AttributeBenchmarkScenario.PhysicalPressure, AttributeBenchmarkScenario.MixedPressure),
                "Block Rating"),
            [AttributeType.DamageReduction] = Percent(
                AttributeType.DamageReduction,
                "Damage Reduction",
                "General damage reduction applied after defense and block.",
                AttributeCombatRules.DamageReductionCapPercent,
                Scenarios(
                    AttributeBenchmarkScenario.PhysicalPressure,
                    AttributeBenchmarkScenario.MagicalPressure,
                    AttributeBenchmarkScenario.MixedPressure),
                "Damage Reduction Rating"),

            [AttributeType.HealingPowerPercent] = Percent(
                AttributeType.HealingPowerPercent,
                "Healing Power",
                "Increases health restoration, including life steal, but does not increase barriers.",
                scenarios: Scenarios(AttributeBenchmarkScenario.HealingSustain, AttributeBenchmarkScenario.LongSustain),
                equipmentDisplayName: "Healing Rating"),
            [AttributeType.HealthRegeneration] = new(
                AttributeType.HealthRegeneration,
                "Health Regen",
                "Health restored every five seconds.",
                AttributeUnit.HealthPerFiveSeconds,
                AttributeStackingRule.Additive,
                0,
                null,
                AttributeCapKind.None,
                true,
                true,
                2,
                " HP/5s",
                Scenarios(AttributeBenchmarkScenario.HealingSustain, AttributeBenchmarkScenario.LongSustain),
                "Health Regen",
                "Health restored every five seconds.",
                AttributeUnit.HealthPerFiveSeconds,
                2,
                " HP/5s"),
            [AttributeType.LifeSteal] = Percent(
                AttributeType.LifeSteal,
                "Life Steal",
                "Percentage of damage dealt restored as health.",
                scenarios: Scenarios(
                    AttributeBenchmarkScenario.HealingSustain,
                    AttributeBenchmarkScenario.LongSustain),
                equipmentDisplayName: "Life Steal Rating"),

            [AttributeType.Cooldown] = Percent(
                AttributeType.Cooldown,
                "Cooldown Reduction",
                "Reduces active ability cooldowns.",
                AttributeCombatRules.CooldownReductionCapPercent,
                Scenarios(
                    AttributeBenchmarkScenario.PhysicalOffense,
                    AttributeBenchmarkScenario.MagicalOffense,
                    AttributeBenchmarkScenario.HealingSustain,
                    AttributeBenchmarkScenario.SummonOffense),
                "Cooldown Rating"),
            [AttributeType.StatusResistance] = Rating(
                AttributeType.StatusResistance,
                "Status Resistance",
                "Reduces the duration of non-crowd-control status effects.",
                Scenarios(AttributeBenchmarkScenario.StatusResilience)),
            [AttributeType.CrowdControlResistance] = Percent(
                AttributeType.CrowdControlResistance,
                "Crowd Control Resistance",
                "Reduces the duration of crowd-control effects by this many percentage points.",
                AttributeCombatRules.CrowdControlResistanceCapPercent,
                Scenarios(AttributeBenchmarkScenario.CrowdControlResilience),
                "Crowd Control Resistance Rating"),

            [AttributeType.AttackSpeed] = new(
                AttributeType.AttackSpeed,
                "Attack Speed",
                "Increases basic-attack rate up to the context-dependent 4x attack-rate ceiling.",
                AttributeUnit.PercentagePoints,
                AttributeStackingRule.Additive,
                0,
                null,
                AttributeCapKind.ContextDependent,
                true,
                true,
                2,
                "%",
                Scenarios(AttributeBenchmarkScenario.PhysicalOffense),
                "Attack Speed Rating",
                "Higher rating increases basic-attack rate with diminishing returns.",
                AttributeUnit.Rating,
                2,
                string.Empty)
        };

    public static IReadOnlyCollection<AttributeDefinition> All => [.. Definitions.Values];

    public static AttributeDefinition Get(AttributeType attributeType) => Definitions[attributeType];

    public static bool IsKnown(AttributeType attributeType) => Definitions.ContainsKey(attributeType);

    public static bool IsContentFacing(AttributeType attributeType) =>
        Definitions.TryGetValue(attributeType, out var definition) && definition.IsContentFacing;

    public static bool IsEquipmentEligible(AttributeType attributeType) =>
        Definitions.TryGetValue(attributeType, out var definition) && definition.IsEquipmentEligible;

    public static float GetFixedCap(AttributeType attributeType)
    {
        var definition = Get(attributeType);
        if (definition.CapKind != AttributeCapKind.Fixed || definition.MaximumValue is not { } maximum)
            throw new InvalidOperationException($"Attribute '{attributeType}' does not have a fixed cap.");

        return maximum;
    }

    public static bool TryGetEffectiveCharacterCap(
        AttributeType attributeType,
        double basicAttackIntervalMultiplier,
        out float cap)
    {
        var definition = Get(attributeType);
        if (definition.CapKind == AttributeCapKind.Fixed && definition.MaximumValue is { } maximum)
        {
            cap = maximum;
            return true;
        }

        if (definition.CapKind == AttributeCapKind.ContextDependent
            && attributeType == AttributeType.AttackSpeed)
        {
            cap = AttributeCombatRules.CalculateUsefulAttackSpeedCapPercent(
                basicAttackIntervalMultiplier);
            return true;
        }

        cap = 0;
        return false;
    }

    private static AttributeDefinition Flat(
        AttributeType attributeType,
        string displayName,
        string description,
        IReadOnlyList<AttributeBenchmarkScenario> scenarios,
        int equipmentDisplayPrecision = 0) =>
        new(
            attributeType,
            displayName,
            description,
            AttributeUnit.FlatPoints,
            AttributeStackingRule.Additive,
            0,
            null,
            AttributeCapKind.None,
            true,
            true,
            0,
            string.Empty,
            scenarios,
            displayName,
            description,
            AttributeUnit.FlatPoints,
            equipmentDisplayPrecision,
            string.Empty);

    private static AttributeDefinition Rating(
        AttributeType attributeType,
        string displayName,
        string description,
        IReadOnlyList<AttributeBenchmarkScenario> scenarios) =>
        new(
            attributeType,
            displayName,
            description,
            AttributeUnit.Rating,
            AttributeStackingRule.Additive,
            0,
            null,
            AttributeCapKind.None,
            true,
            true,
            0,
            string.Empty,
            scenarios,
            $"{displayName} Rating",
            "Higher rating improves this effect with diminishing returns.",
            AttributeUnit.Rating,
            2,
            string.Empty);

    private static AttributeDefinition Percent(
        AttributeType attributeType,
        string displayName,
        string description,
        float? maximum = null,
        IReadOnlyList<AttributeBenchmarkScenario>? scenarios = null,
        string? equipmentDisplayName = null) =>
        new(
            attributeType,
            displayName,
            description,
            AttributeUnit.PercentagePoints,
            AttributeStackingRule.Additive,
            0,
            maximum,
            maximum.HasValue ? AttributeCapKind.Fixed : AttributeCapKind.None,
            true,
            true,
            2,
            "%",
            scenarios ?? [],
            equipmentDisplayName ?? $"{displayName} Rating",
            "Higher rating improves this effect with diminishing returns.",
            AttributeUnit.Rating,
            2,
            string.Empty);

    private static IReadOnlyList<AttributeBenchmarkScenario> Scenarios(
        params AttributeBenchmarkScenario[] scenarios) =>
        scenarios;
}
