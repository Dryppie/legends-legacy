namespace Domain.Models.Attributes;

public static class AttributeCatalog
{
    private static readonly IReadOnlyDictionary<AttributeType, AttributeDefinition> Definitions =
        new Dictionary<AttributeType, AttributeDefinition>
        {
            [AttributeType.Power] = Flat(
                AttributeType.Power,
                "Power",
                "Scales all numeric ability output and basic-attack damage.",
                Scenarios(
                    AttributeBenchmarkScenario.PhysicalOffense,
                    AttributeBenchmarkScenario.MagicalOffense,
                    AttributeBenchmarkScenario.PeriodicOffense,
                    AttributeBenchmarkScenario.HealingSustain,
                    AttributeBenchmarkScenario.SummonOffense)),
            [AttributeType.Fortitude] = Flat(
                AttributeType.Fortitude,
                "Fortitude",
                "Increases maximum health, armor, and resistance.",
                Scenarios(
                    AttributeBenchmarkScenario.PhysicalPressure,
                    AttributeBenchmarkScenario.MagicalPressure,
                    AttributeBenchmarkScenario.MixedPressure,
                    AttributeBenchmarkScenario.BurstPressure,
                    AttributeBenchmarkScenario.LongSustain)),
            [AttributeType.Precision] = Flat(
                AttributeType.Precision,
                "Precision",
                "Increases critical chance, physical and magical penetration, and attack speed.",
                Scenarios(
                    AttributeBenchmarkScenario.PhysicalOffense,
                    AttributeBenchmarkScenario.MagicalOffense,
                    AttributeBenchmarkScenario.PeriodicOffense)),
            [AttributeType.Spirit] = Flat(
                AttributeType.Spirit,
                "Spirit",
                "Increases healing, regeneration, status resilience, and summon attributes.",
                Scenarios(
                    AttributeBenchmarkScenario.HealingSustain,
                    AttributeBenchmarkScenario.StatusResilience,
                    AttributeBenchmarkScenario.CrowdControlResilience,
                    AttributeBenchmarkScenario.SummonOffense,
                    AttributeBenchmarkScenario.LongSustain)),

            [AttributeType.MaxHealth] = Flat(
                AttributeType.MaxHealth,
                "Max Health",
                "Maximum health.",
                Scenarios(
                    AttributeBenchmarkScenario.MixedPressure,
                    AttributeBenchmarkScenario.UnmitigatedPressure,
                    AttributeBenchmarkScenario.BurstPressure,
                    AttributeBenchmarkScenario.LongSustain),
                AttributeType.Fortitude),
            [AttributeType.WeaponDamage] = Flat(
                AttributeType.WeaponDamage,
                "Weapon Damage",
                "Flat damage added to basic attacks.",
                Scenarios(AttributeBenchmarkScenario.PhysicalOffense)),
            [AttributeType.Armor] = Rating(
                AttributeType.Armor,
                "Armor",
                "Physical mitigation rating with diminishing returns.",
                Scenarios(AttributeBenchmarkScenario.PhysicalPressure, AttributeBenchmarkScenario.MixedPressure),
                AttributeType.Fortitude),
            [AttributeType.Resistance] = Rating(
                AttributeType.Resistance,
                "Resistance",
                "Magical and elemental mitigation rating with diminishing returns.",
                Scenarios(AttributeBenchmarkScenario.MagicalPressure, AttributeBenchmarkScenario.MixedPressure),
                AttributeType.Fortitude),
            [AttributeType.CritChance] = Percent(
                AttributeType.CritChance,
                "Crit Chance",
                "Chance for direct damage and crit-eligible healing to critically strike.",
                AttributeCombatRules.CritChanceCapPercent,
                Scenarios(
                    AttributeBenchmarkScenario.PhysicalOffense,
                    AttributeBenchmarkScenario.MagicalOffense,
                    AttributeBenchmarkScenario.HealingSustain),
                AttributeType.Precision),
            [AttributeType.CritDamage] = Percent(
                AttributeType.CritDamage,
                "Crit Damage",
                "Bonus output added when a critical strike occurs.",
                scenarios: Scenarios(
                    AttributeBenchmarkScenario.PhysicalOffense,
                    AttributeBenchmarkScenario.MagicalOffense,
                    AttributeBenchmarkScenario.HealingSustain)),
            [AttributeType.ArmorPenetration] = Rating(
                AttributeType.ArmorPenetration,
                "Armor Penetration",
                "Physical defense rating ignored by attacks.",
                Scenarios(AttributeBenchmarkScenario.PhysicalOffense),
                AttributeType.Precision),
            [AttributeType.MagicPenetration] = Rating(
                AttributeType.MagicPenetration,
                "Magic Penetration",
                "Magical defense rating ignored by attacks.",
                Scenarios(AttributeBenchmarkScenario.MagicalOffense, AttributeBenchmarkScenario.PeriodicOffense),
                AttributeType.Precision),

            [AttributeType.DodgeChance] = Percent(
                AttributeType.DodgeChance,
                "Dodge",
                "Chance to avoid a dodgeable incoming hit.",
                AttributeCombatRules.DodgeChanceCapPercent,
                Scenarios(
                    AttributeBenchmarkScenario.PhysicalPressure,
                    AttributeBenchmarkScenario.MagicalPressure,
                    AttributeBenchmarkScenario.MixedPressure)),
            [AttributeType.BlockChance] = Percent(
                AttributeType.BlockChance,
                "Block",
                $"Chance to reduce a blockable hit by {AttributeCombatRules.BlockDamageReductionPercent:0}%.",
                AttributeCombatRules.BlockChanceCapPercent,
                Scenarios(AttributeBenchmarkScenario.PhysicalPressure, AttributeBenchmarkScenario.MixedPressure)),
            [AttributeType.DamageReduction] = Percent(
                AttributeType.DamageReduction,
                "Damage Reduction",
                "General damage reduction applied after typed mitigation and block.",
                AttributeCombatRules.DamageReductionCapPercent,
                Scenarios(
                    AttributeBenchmarkScenario.PhysicalPressure,
                    AttributeBenchmarkScenario.MagicalPressure,
                    AttributeBenchmarkScenario.MixedPressure)),

            [AttributeType.HealingPowerPercent] = Percent(
                AttributeType.HealingPowerPercent,
                "Healing Power",
                "Increases health restoration, including life steal, but does not increase barriers.",
                scenarios: Scenarios(AttributeBenchmarkScenario.HealingSustain, AttributeBenchmarkScenario.LongSustain),
                primarySource: AttributeType.Spirit),
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
                0,
                " HP/5s",
                AttributeType.Spirit,
                Scenarios(AttributeBenchmarkScenario.HealingSustain, AttributeBenchmarkScenario.LongSustain)),
            [AttributeType.LifeSteal] = Percent(
                AttributeType.LifeSteal,
                "Life Steal",
                "Percentage of damage dealt restored as health.",
                AttributeCombatRules.LifeStealCapPercent,
                Scenarios(AttributeBenchmarkScenario.HealingSustain, AttributeBenchmarkScenario.LongSustain)),

            [AttributeType.Cooldown] = Percent(
                AttributeType.Cooldown,
                "Cooldown Reduction",
                "Reduces active ability cooldowns.",
                AttributeCombatRules.CooldownReductionCapPercent,
                Scenarios(
                    AttributeBenchmarkScenario.PhysicalOffense,
                    AttributeBenchmarkScenario.MagicalOffense,
                    AttributeBenchmarkScenario.HealingSustain,
                    AttributeBenchmarkScenario.SummonOffense)),
            [AttributeType.StatusResistance] = Rating(
                AttributeType.StatusResistance,
                "Status Resistance",
                "Reduces the duration of non-crowd-control status effects.",
                Scenarios(AttributeBenchmarkScenario.StatusResilience),
                AttributeType.Spirit),
            [AttributeType.CrowdControlResistance] = Rating(
                AttributeType.CrowdControlResistance,
                "Crowd Control Resistance",
                "Reduces the duration of crowd-control effects.",
                Scenarios(AttributeBenchmarkScenario.CrowdControlResilience),
                AttributeType.Spirit),

            [AttributeType.SummonPower] = Percent(
                AttributeType.SummonPower,
                "Summon Power",
                "Increases the Power inherited by summoned units.",
                scenarios: Scenarios(AttributeBenchmarkScenario.SummonOffense),
                primarySource: AttributeType.Spirit),
            [AttributeType.SummonHealth] = Percent(
                AttributeType.SummonHealth,
                "Summon Health",
                "Increases the maximum health inherited by summoned units.",
                scenarios: Scenarios(AttributeBenchmarkScenario.SummonOffense),
                primarySource: AttributeType.Spirit),

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
                AttributeType.Precision,
                Scenarios(AttributeBenchmarkScenario.PhysicalOffense))
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
        AttributeType? primarySource = null) =>
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
            primarySource,
            scenarios);

    private static AttributeDefinition Rating(
        AttributeType attributeType,
        string displayName,
        string description,
        IReadOnlyList<AttributeBenchmarkScenario> scenarios,
        AttributeType? primarySource = null) =>
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
            primarySource,
            scenarios);

    private static AttributeDefinition Percent(
        AttributeType attributeType,
        string displayName,
        string description,
        float? maximum = null,
        IReadOnlyList<AttributeBenchmarkScenario>? scenarios = null,
        AttributeType? primarySource = null) =>
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
            primarySource,
            scenarios ?? []);

    private static IReadOnlyList<AttributeBenchmarkScenario> Scenarios(
        params AttributeBenchmarkScenario[] scenarios) =>
        scenarios;
}
