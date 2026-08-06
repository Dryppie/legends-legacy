using Application.Interfaces.Services.LL.Balance;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Engine;
using Services.LL.Professions.Craftings;

namespace Services.LL.Balance;

public sealed class AttributeMarginalValueAnalyzer : IAttributeMarginalValueAnalyzer
{
    private const double MarginalBudgetFraction = 0.10d;
    private const double InertThresholdPercent = 0.05d;
    private const double CostWarningThreshold = 0.20d;
    private const double StrictPeerTolerancePercentagePoints = 10d;
    private const double GeneralistPeerTolerancePercentagePoints = 21d;
    private const double LoadoutWarningThresholdPercent = 20d;
    private const double SummonCalibrationTolerancePercent = 20d;
    private const double HandCalibrationTolerancePercent = 20d;
    private const double CraftingCombatPeerTolerancePercent = 20d;
    private const double CraftingArmorFamilyTolerancePercent = 30d;
    private const double AggregateCapWasteTolerancePercent = 1d;
    private const int NominalSummonDurationTicks = 100;
    private const int NominalRoleAbilityCooldownTicks = NominalSummonDurationTicks;
    private const int NominalSummonStrikeCooldownTicks = 24;
    private const int NominalSummonBasicAttackIntervalTicks = 20;
    private const int NominalSummonPowerBase = 20;
    private const float NominalSummonPowerCoefficient = 0.30f;
    private const int NominalSummonStrikeBase = 8;
    private const float NominalSummonStrikePowerCoefficient = 0.55f;
    private const string RepresentativeFastWeaponRecipeId =
        "recipe.weapon.one_handed.dagger";
    private const string RepresentativeSlowWeaponRecipeId =
        "recipe.weapon.two_handed.maul";
    private const int MaximumEquipmentTier = 10;
    private const ItemQuality MaximumEquipmentQuality = ItemQuality.Masterwork;
    private const Rarity MaximumEquipmentRarity = Rarity.Legacy;
    private const double MaximumCraftingVarianceMultiplier = 1.05d;

    private static readonly IReadOnlyList<int> ReferenceTiers = Array.AsReadOnly([1, 5, 10]);
    private static readonly IReadOnlyList<int> DeterministicSeeds =
        Array.AsReadOnly([101, 211, 307, 401, 503, 601, 701, 809]);
    private static readonly IReadOnlyList<int> CalibrationDurations =
        Array.AsReadOnly([90, 180, 600]);
    private static readonly IReadOnlyList<double> StandardEquipmentSlotWeights =
        Array.AsReadOnly([0.85d, 1.15d, 0.95d, 0.45d, 0.60d, 0.75d]);
    private static readonly EqualBudgetBenchmarkContext LowCritContext =
        new(
            "low crit investment",
            new Dictionary<AttributeType, double>
            {
                [AttributeType.CritChance] = 10d,
                [AttributeType.CritDamage] = 50d
            },
            OpponentDefenseMultiplier: 1d);
    private static readonly EqualBudgetBenchmarkContext MediumCritContext =
        new(
            "medium crit investment",
            new Dictionary<AttributeType, double>
            {
                [AttributeType.CritChance] = 40d,
                [AttributeType.CritDamage] = 125d
            },
            OpponentDefenseMultiplier: 1d);
    private static readonly EqualBudgetBenchmarkContext NearCapCritContext =
        new(
            "near-cap crit investment",
            new Dictionary<AttributeType, double>
            {
                [AttributeType.CritChance] = 65d,
                [AttributeType.CritDamage] = 200d
            },
            OpponentDefenseMultiplier: 1d);
    private static readonly EqualBudgetBenchmarkContext LowDefenseContext =
        new(
            "15% reference mitigation",
            new Dictionary<AttributeType, double>(),
            OpponentDefenseMultiplier: 0.5d);
    private static readonly EqualBudgetBenchmarkContext HighDefenseContext =
        new(
            "45% reference mitigation",
            new Dictionary<AttributeType, double>(),
            OpponentDefenseMultiplier: 1.5d);
    private static readonly EqualBudgetBenchmarkContext BasicAttackThroughputContext =
        new(
            "basic attack throughput",
            new Dictionary<AttributeType, double>(),
            OpponentDefenseMultiplier: 1d,
            FriendlyAbilityIds: [],
            MaxTicksOverride: 600,
            BasicAttackDamageMultiplier: 10d);
    private static readonly IReadOnlyList<EqualBudgetPeerSpec> EqualBudgetPeerSpecs =
        Array.AsReadOnly<EqualBudgetPeerSpec>(
        [
            ContextPeer(
                "power-attack-speed",
                AttributePeerComparisonGroup.Offense,
                AttributeBalanceScenario.PhysicalOffense,
                AttributeType.Power,
                AttributeType.AttackSpeed,
                BasicAttackThroughputContext,
                AttributePeerComparisonIntent.StrictPeer,
                StrictPeerTolerancePercentagePoints,
                budgetFraction: MarginalBudgetFraction),
            ContextPeer(
                "crit-chance-crit-damage-low",
                AttributePeerComparisonGroup.Crit,
                AttributeBalanceScenario.PhysicalOffense,
                AttributeType.CritChance,
                AttributeType.CritDamage,
                LowCritContext,
                AttributePeerComparisonIntent.StrictPeer,
                StrictPeerTolerancePercentagePoints,
                budgetFraction: 0.01d),
            ContextPeer(
                "crit-chance-crit-damage-medium",
                AttributePeerComparisonGroup.Crit,
                AttributeBalanceScenario.PhysicalOffense,
                AttributeType.CritChance,
                AttributeType.CritDamage,
                MediumCritContext,
                AttributePeerComparisonIntent.StrictPeer,
                StrictPeerTolerancePercentagePoints,
                budgetFraction: 0.01d),
            ContextPeer(
                "crit-chance-crit-damage-near-cap",
                AttributePeerComparisonGroup.Crit,
                AttributeBalanceScenario.PhysicalOffense,
                AttributeType.CritChance,
                AttributeType.CritDamage,
                NearCapCritContext,
                AttributePeerComparisonIntent.StrictPeer,
                StrictPeerTolerancePercentagePoints,
                budgetFraction: 0.01d),
            StrictPeer(
                "max-health-armor",
                AttributePeerComparisonGroup.Defense,
                AttributeBalanceScenario.PhysicalPressure,
                AttributeType.MaxHealth,
                AttributeType.Armor,
                budgetFraction: MarginalBudgetFraction),
            StrictPeer(
                "max-health-resistance",
                AttributePeerComparisonGroup.Defense,
                AttributeBalanceScenario.MagicalPressure,
                AttributeType.MaxHealth,
                AttributeType.Resistance,
                budgetFraction: MarginalBudgetFraction),
            GeneralistPeer(
                "max-health-dodge",
                AttributePeerComparisonGroup.Defense,
                AttributeBalanceScenario.PhysicalPressure,
                AttributeType.MaxHealth,
                AttributeType.DodgeChance),
            GeneralistPeer(
                "max-health-block",
                AttributePeerComparisonGroup.Defense,
                AttributeBalanceScenario.PhysicalPressure,
                AttributeType.MaxHealth,
                AttributeType.BlockChance),
            GeneralistPeer(
                "max-health-damage-reduction",
                AttributePeerComparisonGroup.Defense,
                AttributeBalanceScenario.MixedPressure,
                AttributeType.MaxHealth,
                AttributeType.DamageReduction),
            GeneralistPeer(
                "max-health-armor-burst",
                AttributePeerComparisonGroup.Defense,
                AttributeBalanceScenario.BurstPressure,
                AttributeType.MaxHealth,
                AttributeType.Armor),
            GeneralistPeer(
                "max-health-dodge-burst",
                AttributePeerComparisonGroup.Defense,
                AttributeBalanceScenario.BurstPressure,
                AttributeType.MaxHealth,
                AttributeType.DodgeChance),
            GeneralistPeer(
                "max-health-block-burst",
                AttributePeerComparisonGroup.Defense,
                AttributeBalanceScenario.BurstPressure,
                AttributeType.MaxHealth,
                AttributeType.BlockChance),
            GeneralistPeer(
                "max-health-damage-reduction-burst",
                AttributePeerComparisonGroup.Defense,
                AttributeBalanceScenario.BurstPressure,
                AttributeType.MaxHealth,
                AttributeType.DamageReduction),
            GeneralistPeer(
                "healing-power-health-regeneration",
                AttributePeerComparisonGroup.Sustain,
                AttributeBalanceScenario.HealingSustain,
                AttributeType.HealingPowerPercent,
                AttributeType.HealthRegeneration),
            GeneralistPeer(
                "health-regeneration-life-steal",
                AttributePeerComparisonGroup.Sustain,
                AttributeBalanceScenario.LongSustain,
                AttributeType.HealthRegeneration,
                AttributeType.LifeSteal),
            GeneralistPeer(
                "health-regeneration-max-health-physical",
                AttributePeerComparisonGroup.Sustain,
                AttributeBalanceScenario.PhysicalPressure,
                AttributeType.HealthRegeneration,
                AttributeType.MaxHealth),
            GeneralistPeer(
                "health-regeneration-max-health-long",
                AttributePeerComparisonGroup.Sustain,
                AttributeBalanceScenario.LongSustain,
                AttributeType.HealthRegeneration,
                AttributeType.MaxHealth),
            GeneralistPeer(
                "health-regeneration-max-health-long-low",
                AttributePeerComparisonGroup.Sustain,
                AttributeBalanceScenario.LongSustain,
                AttributeType.HealthRegeneration,
                AttributeType.MaxHealth,
                budgetFraction: 0.01d),
            GeneralistPeer(
                "health-regeneration-max-health-long-observed",
                AttributePeerComparisonGroup.Sustain,
                AttributeBalanceScenario.LongSustain,
                AttributeType.HealthRegeneration,
                AttributeType.MaxHealth,
                budgetFraction: 0.03d),
            GeneralistPeer(
                "health-regeneration-max-health-long-high",
                AttributePeerComparisonGroup.Sustain,
                AttributeBalanceScenario.LongSustain,
                AttributeType.HealthRegeneration,
                AttributeType.MaxHealth,
                budgetFraction: 0.05d),
            GeneralistPeer(
                "health-regeneration-armor",
                AttributePeerComparisonGroup.Sustain,
                AttributeBalanceScenario.PhysicalPressure,
                AttributeType.HealthRegeneration,
                AttributeType.Armor),
            GeneralistPeer(
                "health-regeneration-resistance",
                AttributePeerComparisonGroup.Sustain,
                AttributeBalanceScenario.MagicalPressure,
                AttributeType.HealthRegeneration,
                AttributeType.Resistance),
            GeneralistPeer(
                "health-regeneration-damage-reduction-mixed",
                AttributePeerComparisonGroup.Sustain,
                AttributeBalanceScenario.MixedPressure,
                AttributeType.HealthRegeneration,
                AttributeType.DamageReduction),
            GeneralistPeer(
                "health-regeneration-damage-reduction-long",
                AttributePeerComparisonGroup.Sustain,
                AttributeBalanceScenario.LongSustain,
                AttributeType.HealthRegeneration,
                AttributeType.DamageReduction),
            ContextPeer(
                "armor-penetration-power-low-defense",
                AttributePeerComparisonGroup.Penetration,
                AttributeBalanceScenario.PhysicalOffense,
                AttributeType.ArmorPenetration,
                AttributeType.Power,
                LowDefenseContext,
                AttributePeerComparisonIntent.GeneralistVersusSpecialist,
                GeneralistPeerTolerancePercentagePoints),
            ContextPeer(
                "armor-penetration-power-high-defense",
                AttributePeerComparisonGroup.Penetration,
                AttributeBalanceScenario.PhysicalOffense,
                AttributeType.ArmorPenetration,
                AttributeType.Power,
                HighDefenseContext,
                AttributePeerComparisonIntent.GeneralistVersusSpecialist,
                GeneralistPeerTolerancePercentagePoints,
                isReleaseGate: false),
            ContextPeer(
                "magic-penetration-power-low-defense",
                AttributePeerComparisonGroup.Penetration,
                AttributeBalanceScenario.MagicalOffense,
                AttributeType.MagicPenetration,
                AttributeType.Power,
                LowDefenseContext,
                AttributePeerComparisonIntent.GeneralistVersusSpecialist,
                GeneralistPeerTolerancePercentagePoints),
            ContextPeer(
                "magic-penetration-power-high-defense",
                AttributePeerComparisonGroup.Penetration,
                AttributeBalanceScenario.MagicalOffense,
                AttributeType.MagicPenetration,
                AttributeType.Power,
                HighDefenseContext,
                AttributePeerComparisonIntent.GeneralistVersusSpecialist,
                GeneralistPeerTolerancePercentagePoints,
                isReleaseGate: false)
        ]);
    private static readonly IReadOnlyList<CraftingCombatPeerSpec> CraftingCombatPeerSpecs =
        Array.AsReadOnly<CraftingCombatPeerSpec>(
        [
            CraftingPeer(
                "balanced-dual-greatsword",
                CraftingCombatPeerGroup.HandConfiguration,
                AttributeBalanceScenario.PhysicalOffense,
                "Medium",
                "dual:recipe.weapon.one_handed.shortsword",
                null,
                "Medium",
                "two-handed:recipe.weapon.two_handed.greatsword",
                null),
            CraftingPeer(
                "fast-dual-gauntlets",
                CraftingCombatPeerGroup.HandConfiguration,
                AttributeBalanceScenario.PhysicalOffense,
                "Light",
                "dual:recipe.weapon.one_handed.dagger",
                null,
                "Light",
                "two-handed:recipe.weapon.two_handed.gauntlets",
                null),
            CraftingPeer(
                "penetration-dual-battle-axe",
                CraftingCombatPeerGroup.HandConfiguration,
                AttributeBalanceScenario.PhysicalOffense,
                "Medium",
                "dual:recipe.weapon.one_handed.hand_axe",
                null,
                "Medium",
                "two-handed:recipe.weapon.two_handed.battle_axe",
                null),
            CraftingPeer(
                "tower-shield-aegis-warden",
                CraftingCombatPeerGroup.Shield,
                AttributeBalanceScenario.MixedPressure,
                "Heavy",
                "one-off:recipe.weapon.one_handed.shortsword+recipe.offhand.towershield",
                "blueprint_aegis",
                "Heavy",
                "one-off:recipe.weapon.one_handed.shortsword+recipe.offhand.towershield",
                "blueprint_warden"),
            CraftingPeer(
                "heavy-medium-physical-defense",
                CraftingCombatPeerGroup.ArmorFamily,
                AttributeBalanceScenario.PhysicalPressure,
                "Heavy",
                "one-off:recipe.weapon.one_handed.shortsword+recipe.offhand.towershield",
                null,
                "Medium",
                "one-off:recipe.weapon.one_handed.shortsword+recipe.offhand.towershield",
                null,
                CraftingArmorFamilyTolerancePercent,
                isReleaseGate: false),
            CraftingPeer(
                "medium-light-physical-offense",
                CraftingCombatPeerGroup.ArmorFamily,
                AttributeBalanceScenario.PhysicalOffense,
                "Medium",
                "dual:recipe.weapon.one_handed.shortsword",
                null,
                "Light",
                "dual:recipe.weapon.one_handed.shortsword",
                null,
                CraftingArmorFamilyTolerancePercent,
                isReleaseGate: false),
            CraftingPeer(
                "fury-execution",
                CraftingCombatPeerGroup.Blueprint,
                AttributeBalanceScenario.PhysicalOffense,
                "Medium",
                "dual:recipe.weapon.one_handed.shortsword",
                "blueprint_fury",
                "Medium",
                "dual:recipe.weapon.one_handed.shortsword",
                "blueprint_execution"),
            CraftingPeer(
                "spirit-phoenix",
                CraftingCombatPeerGroup.Blueprint,
                AttributeBalanceScenario.HealingSustain,
                "Cloth",
                "one-off:recipe.weapon.one_handed.wand+recipe.offhand.spiritward",
                "blueprint_spirit",
                "Cloth",
                "one-off:recipe.weapon.one_handed.wand+recipe.offhand.spiritward",
                "blueprint_phoenix",
                isReleaseGate: false),
            CraftingPeer(
                "endurance-phoenix",
                CraftingCombatPeerGroup.Blueprint,
                AttributeBalanceScenario.HealingSustain,
                "Cloth",
                "one-off:recipe.weapon.one_handed.wand+recipe.offhand.spiritward",
                "blueprint_endurance",
                "Cloth",
                "one-off:recipe.weapon.one_handed.wand+recipe.offhand.spiritward",
                "blueprint_phoenix"),
            CraftingPeer(
                "venom-hive",
                CraftingCombatPeerGroup.Blueprint,
                AttributeBalanceScenario.LongSustain,
                "Light",
                "dual:recipe.weapon.one_handed.dagger",
                "blueprint_venom",
                "Light",
                "dual:recipe.weapon.one_handed.dagger",
                "blueprint_hive"),
            CraftingPeer(
                "arcane-primal",
                CraftingCombatPeerGroup.Blueprint,
                AttributeBalanceScenario.SummonOffense,
                "Cloth",
                "two-handed:recipe.weapon.two_handed.staff",
                "blueprint_arcane",
                "Cloth",
                "two-handed:recipe.weapon.two_handed.staff",
                "blueprint_primal",
                isReleaseGate: false)
        ]);

    private static readonly IReadOnlyDictionary<AttributeType, AttributeBalanceScenario[]> RelevantScenarios =
        new Dictionary<AttributeType, AttributeBalanceScenario[]>
        {
            [AttributeType.Power] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.PeriodicOffense,
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.SummonOffense
            ],
            [AttributeType.MaxHealth] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.MixedPressure,
                AttributeBalanceScenario.UnmitigatedPressure,
                AttributeBalanceScenario.BurstPressure,
                AttributeBalanceScenario.LongSustain
            ],
            [AttributeType.Armor] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MixedPressure,
                AttributeBalanceScenario.BurstPressure
            ],
            [AttributeType.Resistance] =
            [
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.MixedPressure
            ],
            [AttributeType.CritChance] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.HealingSustain
            ],
            [AttributeType.CritDamage] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.HealingSustain
            ],
            [AttributeType.ArmorPenetration] = [AttributeBalanceScenario.PhysicalOffense],
            [AttributeType.MagicPenetration] =
            [
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.PeriodicOffense
            ],
            [AttributeType.DodgeChance] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.MixedPressure
            ],
            [AttributeType.BlockChance] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.MixedPressure
            ],
            [AttributeType.DamageReduction] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.MixedPressure,
                AttributeBalanceScenario.UnmitigatedPressure,
                AttributeBalanceScenario.BurstPressure,
                AttributeBalanceScenario.LongSustain
            ],
            [AttributeType.HealingPowerPercent] =
            [
                AttributeBalanceScenario.HealingSustain
            ],
            [AttributeType.HealthRegeneration] =
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.LongSustain
            ],
            [AttributeType.LifeSteal] =
            [
                AttributeBalanceScenario.LongSustain
            ],
            [AttributeType.Cooldown] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.PeriodicOffense,
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.SummonOffense,
                AttributeBalanceScenario.LongSustain
            ],
            [AttributeType.StatusResistance] = [AttributeBalanceScenario.StatusResilience],
            [AttributeType.CrowdControlResistance] = [AttributeBalanceScenario.CrowdControlResilience],
            [AttributeType.SummonPower] = [AttributeBalanceScenario.SummonOffense],
            [AttributeType.SummonHealth] = [AttributeBalanceScenario.SummonOffense],
            [AttributeType.AttackSpeed] =
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.CrowdControlResilience
            ]
        };

    private static readonly IReadOnlyList<EquipmentLoadoutProfile> LoadoutProfiles =
    [
        new(
            "heavy-shield",
            "Heavy Shield",
            [.. StandardEquipmentSlotWeights, 0.85d, 0.65d],
            new Dictionary<AttributeType, double>
            {
                [AttributeType.DamageReduction] = 0.25d,
                [AttributeType.MaxHealth] = 0.20d,
                [AttributeType.Armor] = 0.25d,
                [AttributeType.Resistance] = 0.10d,
                [AttributeType.BlockChance] = 0.10d,
                [AttributeType.Power] = 0.10d
            },
            ["balance.physical-strike", "balance.self-barrier"],
            [
                AttributeBalanceScenario.PhysicalPressure,
                AttributeBalanceScenario.MagicalPressure,
                AttributeBalanceScenario.MixedPressure,
                AttributeBalanceScenario.BurstPressure
            ],
            1.15d,
            0.85d,
            AttackType.Melee,
            DamageType.Physical),
        new(
            "medium-dual-wield",
            "Medium Dual Wield",
            [.. StandardEquipmentSlotWeights, 0.85d, 0.85d],
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.35d,
                [AttributeType.CritChance] = 0.15d,
                [AttributeType.CritDamage] = 0.20d,
                [AttributeType.ArmorPenetration] = 0.15d,
                [AttributeType.AttackSpeed] = 0.15d
            },
            ["balance.physical-strike"],
            [
                AttributeBalanceScenario.PhysicalOffense,
                AttributeBalanceScenario.CrowdControlResilience
            ],
            0.75d,
            0.78d,
            AttackType.Melee,
            DamageType.Physical),
        new(
            "cloth-support",
            "Cloth Support",
            [.. StandardEquipmentSlotWeights, 0.85d, 0.65d],
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.25d,
                [AttributeType.MaxHealth] = 0.10d,
                [AttributeType.HealingPowerPercent] = 0.30d,
                [AttributeType.Cooldown] = 0.10d,
                [AttributeType.Resistance] = 0.10d,
                [AttributeType.HealthRegeneration] = 0.15d
            },
            ["balance.magical-strike", "balance.self-heal", "balance.self-barrier"],
            [
                AttributeBalanceScenario.MagicalOffense,
                AttributeBalanceScenario.HealingSustain,
                AttributeBalanceScenario.StatusResilience,
                AttributeBalanceScenario.LongSustain
            ],
            1d,
            0.8d,
            AttackType.None,
            DamageType.Magical),
        new(
            "two-handed-damage",
            "Two-Handed Damage",
            [.. StandardEquipmentSlotWeights, 1.40d],
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.45d,
                [AttributeType.CritChance] = 0.15d,
                [AttributeType.CritDamage] = 0.20d,
                [AttributeType.ArmorPenetration] = 0.15d,
                [AttributeType.AttackSpeed] = 0.05d
            },
            ["balance.physical-strike"],
            [AttributeBalanceScenario.PhysicalOffense],
            1.25d,
            1.22d,
            AttackType.Melee,
            DamageType.Physical),
        new(
            "summoner",
            "Summoner",
            [.. StandardEquipmentSlotWeights, 1.40d],
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.30d,
                [AttributeType.SummonPower] = 0.35d,
                [AttributeType.SummonHealth] = 0.15d,
                [AttributeType.Cooldown] = 0.15d,
                [AttributeType.MaxHealth] = 0.05d
            },
            ["balance.magical-strike", "balance.summon"],
            [
                AttributeBalanceScenario.SummonOffense,
                AttributeBalanceScenario.MagicalOffense
            ],
            1d,
            0.7d,
            AttackType.None,
            DamageType.Magical)
    ];

    private static readonly IReadOnlyDictionary<AttributeType, double> MatchedHandBudgetShares =
        new Dictionary<AttributeType, double>
        {
            [AttributeType.Power] = 0.40d,
            [AttributeType.CritChance] = 0.15d,
            [AttributeType.CritDamage] = 0.15d,
            [AttributeType.ArmorPenetration] = 0.15d,
            [AttributeType.AttackSpeed] = 0.15d
        };

    private static readonly IReadOnlyDictionary<string, CompiledStatus> Statuses =
        AbilityCompiler.CompileStatuses(CreateStatusSpecs());
    private static readonly IReadOnlyDictionary<string, CompiledSummon> Summons =
        AbilityCompiler.CompileSummons(CreateSummonSpecs());
    private static readonly IReadOnlyDictionary<string, CompiledAbility> AllAbilities =
        AbilityCompiler.CompileAbilities(CreateAbilitySpecs());

    private readonly CraftingBalanceOptions _craftingBalance;
    private readonly ICraftingDefinitionProvider _craftingDefinitions;

    public AttributeMarginalValueAnalyzer(
        IOptions<CraftingBalanceOptions> craftingBalance,
        ICraftingDefinitionProvider craftingDefinitions)
    {
        _craftingBalance = craftingBalance.Value;
        _craftingDefinitions = craftingDefinitions;
    }

    public AttributeBalanceAnalysisReport Analyze(CancellationToken cancellationToken)
    {
        var measurements = new List<AttributeMarginalValueMeasurement>();
        var baselineCache = new Dictionary<(int Tier, AttributeBalanceScenario Scenario), ScenarioSample>();

        foreach (var tier in ReferenceTiers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var marginalBudget = _craftingBalance.GetTierPowerBudget(tier) * MarginalBudgetFraction;

            foreach (var attribute in EquipmentStatBudgetCatalog.Attributes.Order())
            {
                var rule = EquipmentStatBudgetCatalog.Get(attribute, tier);
                var baselineAttributes = CreateReferenceAttributes(tier);
                var benchmarkContext = attribute is AttributeType.CritChance or AttributeType.CritDamage
                    ? MediumCritContext
                    : null;
                var baselineValue = baselineAttributes.GetValueOrDefault(attribute);
                var desiredPointDelta = marginalBudget / rule.CostPerPoint;
                var pointDelta = Math.Max(0d, Math.Min(desiredPointDelta, rule.PerItemHardCap - baselineValue));
                var capLimited = pointDelta + 0.0001d < desiredPointDelta;
                var budgetSpent = pointDelta * rule.CostPerPoint;
                var scenarios = new List<AttributeScenarioMeasurement>();

                foreach (var scenario in RelevantScenarios[attribute])
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ScenarioSample baselineSample;
                    if (benchmarkContext is not null)
                    {
                        baselineSample = MeasureScenario(
                            tier,
                            scenario,
                            new Dictionary<AttributeType, double>(),
                            cancellationToken,
                            benchmarkContext);
                    }
                    else if (!baselineCache.TryGetValue((tier, scenario), out baselineSample!))
                    {
                        baselineSample = MeasureScenario(tier, scenario, null, 0, cancellationToken);
                        baselineCache.Add((tier, scenario), baselineSample);
                    }

                    var modifiedSample = pointDelta <= 0
                        ? baselineSample
                        : MeasureScenario(
                            tier,
                            scenario,
                            new Dictionary<AttributeType, double> { [attribute] = pointDelta },
                            cancellationToken,
                            benchmarkContext);
                    var relativeGains = baselineSample.Scores
                        .Zip(
                            modifiedSample.Scores,
                            (baseline, modified) => CalculateRelativeGain(baseline, modified))
                        .ToArray();
                    var relativeGain = relativeGains.Average();
                    var (confidenceLow, confidenceHigh) = CalculateConfidenceInterval(relativeGains);
                    scenarios.Add(new AttributeScenarioMeasurement(
                        scenario,
                        Round(baselineSample.Mean),
                        Round(modifiedSample.Mean),
                        Round(relativeGain),
                        Round(confidenceLow),
                        Round(confidenceHigh)));
                }

                var medianGain = Median(scenarios.Select(x => x.RelativeGainPercent));
                measurements.Add(new AttributeMarginalValueMeasurement(
                    tier,
                    attribute,
                    Round(baselineValue),
                    Round(pointDelta),
                    Round(budgetSpent),
                    rule.CostPerPoint,
                    null,
                    Round(medianGain),
                    budgetSpent <= 0 ? 0 : Round(medianGain / budgetSpent),
                    capLimited,
                    scenarios));
            }
        }

        measurements = AddSuggestedCosts(measurements);
        var equalBudgetComparisons = CreateEqualBudgetComparisons(baselineCache, cancellationToken);
        var loadouts = AnalyzeLoadouts(cancellationToken);
        var loadoutComparisons = CreateLoadoutComparisons(loadouts);
        var summonCalibrations = AnalyzeSummonCalibration(cancellationToken);
        var handCalibrations = AnalyzeHandCalibration(cancellationToken);
        var craftingCombatPeers = AnalyzeCraftingCombatPeers(cancellationToken);
        var craftingCatalogConstraints = AnalyzeCraftingCatalogConstraints(cancellationToken);
        var maximumEquipmentProgression =
            AnalyzeMaximumEquipmentProgression(cancellationToken);
        var calibrationGate = CreateCalibrationGate(
            equalBudgetComparisons,
            loadouts,
            summonCalibrations,
            handCalibrations,
            craftingCombatPeers,
            maximumEquipmentProgression);
        var findings = CreateFindings(
            measurements,
            equalBudgetComparisons,
            loadouts,
            loadoutComparisons,
            summonCalibrations,
            handCalibrations,
            craftingCombatPeers,
            maximumEquipmentProgression,
            calibrationGate);

        return new AttributeBalanceAnalysisReport(
            EquipmentBudgetEvaluator.BalanceVersion,
            PowerRatingAlgorithm.CombatRulesVersion,
            AttributeCatalog.All,
            ReferenceTiers,
            DeterministicSeeds,
            MarginalBudgetFraction,
            measurements,
            equalBudgetComparisons,
            loadouts,
            loadoutComparisons,
            summonCalibrations,
            handCalibrations,
            craftingCombatPeers,
            craftingCatalogConstraints,
            maximumEquipmentProgression,
            calibrationGate,
            findings);
    }

    private ScenarioSample MeasureScenario(
        int tier,
        AttributeBalanceScenario scenario,
        AttributeType? modifiedAttribute,
        double pointDelta,
        CancellationToken cancellationToken) =>
        MeasureScenario(
            tier,
            scenario,
            modifiedAttribute is { } attribute && pointDelta > 0
                ? new Dictionary<AttributeType, double> { [attribute] = pointDelta }
                : new Dictionary<AttributeType, double>(),
            cancellationToken,
            benchmarkContext: null);

    private ScenarioSample MeasureScenario(
        int tier,
        AttributeBalanceScenario scenario,
        IReadOnlyDictionary<AttributeType, double> pointDeltas,
        CancellationToken cancellationToken,
        EqualBudgetBenchmarkContext? benchmarkContext = null)
    {
        var outcomes = new List<ScenarioOutcome>(DeterministicSeeds.Count);
        foreach (var seed in DeterministicSeeds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            outcomes.Add(RunScenario(
                tier,
                scenario,
                pointDeltas,
                seed,
                cancellationToken,
                benchmarkContext));
        }

        var scores = outcomes.Select(x => x.Utility.Total).ToList();
        return new ScenarioSample(scores.Average(), scores, AverageOutput(outcomes));
    }

    private static ScenarioOutcome RunScenario(
        int tier,
        AttributeBalanceScenario scenario,
        IReadOnlyDictionary<AttributeType, double> pointDeltas,
        int seed,
        CancellationToken cancellationToken,
        EqualBudgetBenchmarkContext? benchmarkContext)
    {
        var friendlyAttributes = CreateReferenceAttributes(tier);
        if (benchmarkContext is not null)
        {
            foreach (var (attribute, value) in benchmarkContext.ReferenceAttributeOverrides)
                friendlyAttributes[attribute] = (float)value;
        }

        foreach (var (attribute, pointDelta) in pointDeltas.Where(x => x.Value > 0))
            ApplyAttributeDelta(friendlyAttributes, attribute, (float)pointDelta);

        var friendlyAbilities = benchmarkContext?.FriendlyAbilityIds is { } abilityIds
            ? abilityIds.Select(id => AllAbilities[id]).ToArray()
            : SelectFriendlyAbilities(scenario);

        return ExecuteScenario(
            tier,
            scenario,
            friendlyAttributes,
            friendlyAbilities,
            basicAttackIntervalMultiplier:
                benchmarkContext?.BasicAttackIntervalMultiplier ?? 1d,
            basicAttackDamageMultiplier:
                benchmarkContext?.BasicAttackDamageMultiplier ?? 1d,
            basicAttackType: AttackType.Melee,
            basicAttackDamageType: scenario == AttributeBalanceScenario.MagicalOffense
                ? DamageType.Magical
                : DamageType.Physical,
            seed,
            cancellationToken,
            maxTicksOverride: benchmarkContext?.MaxTicksOverride,
            opponentDefenseMultiplier:
                benchmarkContext?.OpponentDefenseMultiplier ?? 1d);
    }

    private static ScenarioOutcome ExecuteScenario(
        int tier,
        AttributeBalanceScenario scenario,
        Dictionary<AttributeType, float> friendlyAttributes,
        IReadOnlyList<CompiledAbility> friendlyAbilities,
        double basicAttackIntervalMultiplier,
        double basicAttackDamageMultiplier,
        AttackType basicAttackType,
        DamageType basicAttackDamageType,
        int seed,
        CancellationToken cancellationToken,
        int? maxTicksOverride = null,
        double opponentDefenseMultiplier = 1d)
    {
        var friendly = new RuntimeCombatant(
            "balance-friendly",
            "Reference Character",
            CombatTeam.Friendly,
            friendlyAttributes,
            friendlyAbilities,
            ["Role.Balance"],
            basicAttackIntervalMultiplier: basicAttackIntervalMultiplier,
            basicAttackDamageMultiplier: basicAttackDamageMultiplier,
            basicAttackType: basicAttackType,
            basicAttackDamageType: basicAttackDamageType);
        var hostile = new RuntimeCombatant(
            "balance-hostile",
            "Reference Opponent",
            CombatTeam.Hostile,
            CreateOpponentAttributes(tier, scenario, opponentDefenseMultiplier),
            SelectHostileAbilities(scenario),
            ["Role.Balance.Target"],
            basicAttackDamageType: scenario == AttributeBalanceScenario.MagicalPressure
                ? DamageType.Magical
                : DamageType.Physical,
            basicAttackDamageMultiplier: CreateOpponentBasicAttackDamageMultiplier(tier, scenario));
        var maxTicks = maxTicksOverride ?? GetMaxTicks(scenario);
        var engine = new FastCombatEngine(
            Statuses,
            Summons,
            AllAbilities,
            new FastCombatEngineOptions(
                MaxTicks: maxTicks,
                BasicAttackIntervalTicks: GetBasicAttackInterval(scenario),
                RandomSeed: seed,
                StartActiveAbilitiesOnCooldown: true));
        var result = engine.Run([friendly], [hostile], cancellationToken);
        var friendlyStats = result.EntityStats
            .Where(x => x.Team.Equals(nameof(CombatTeam.Friendly), StringComparison.OrdinalIgnoreCase))
            .ToList();
        var friendlyIds = friendlyStats.Select(x => x.EntityId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        friendlyIds.Add(friendly.Id);
        var directStats = friendlyStats
            .Where(x => x.EntityId.Equals(friendly.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var summonStats = friendlyStats
            .Where(x => !x.EntityId.Equals(friendly.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var summonActivity = CalculateSummonActivity(result, friendly.Id);
        var output = new EquipmentLoadoutOutput(
            DirectDamage: directStats.Sum(x => x.DamageDone),
            SummonDamage: summonStats.Sum(x => x.DamageDone),
            Healing: friendlyStats.Sum(x => x.HealingDone),
            HealthRegeneration: friendlyStats.Sum(x => x.HealthRegenerated),
            BarrierGenerated: friendlyStats.Sum(x => x.BarrierGenerated),
            BarrierAbsorbed: friendlyStats.Sum(x => x.DamageBlocked),
            IncomingRawDamage: friendlyStats.Sum(x => x.IncomingRawDamage),
            AvoidedDamage: friendlyStats.Sum(x => x.AvoidedDamage),
            TypedMitigationPrevented: friendlyStats.Sum(x => x.TypedMitigationPrevented),
            PhysicalMitigationPrevented: friendlyStats.Sum(x => x.PhysicalMitigationPrevented),
            MagicalMitigationPrevented: friendlyStats.Sum(x => x.MagicalMitigationPrevented),
            BlockPrevented: friendlyStats.Sum(x => x.BlockPrevented),
            DamageReductionPrevented: friendlyStats.Sum(x => x.DamageReductionPrevented),
            DamageAmplified: friendlyStats.Sum(x => x.DamageAmplified),
            FinalHealthDamage: friendlyStats.Sum(x => x.FinalHealthDamage),
            DamageTaken: friendlyStats.Sum(x => x.DamageTaken),
            RemainingHealth: friendly.Health,
            DurationTicks: result.Duration,
            AvoidedAttacks: friendlyStats.Sum(x => x.AvoidedAttacks),
            SummonsCreated: summonActivity.SummonsCreated,
            AverageActiveSummons: summonActivity.AverageActiveSummons,
            SummonUptimePercent: summonActivity.UptimePercent);
        var utility = CreateUtilityBreakdown(scenario, output);
        return new ScenarioOutcome(output, utility);
    }

    private static SummonActivity CalculateSummonActivity(CombatResult result, string ownerId)
    {
        if (result.Duration <= 0)
            return new SummonActivity(0, 0, 0);

        var intervals = result.EventLog
            .Where(x =>
                x.EventType == EventType.Summon
                && x.ActorId.Equals(ownerId, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(x.TargetId))
            .Select(spawn =>
            {
                var end = result.EventLog
                    .Where(x =>
                        x.Timestamp >= spawn.Timestamp
                        && !string.IsNullOrWhiteSpace(x.TargetId)
                        && x.TargetId.Equals(spawn.TargetId, StringComparison.OrdinalIgnoreCase)
                        && x.EventType is EventType.SummonExpired or EventType.Death)
                    .Select(x => x.Timestamp)
                    .DefaultIfEmpty(result.Duration)
                    .Min();
                return (Start: spawn.Timestamp, End: Math.Clamp(end, spawn.Timestamp, result.Duration));
            })
            .ToList();
        if (intervals.Count == 0)
            return new SummonActivity(0, 0, 0);

        var activeTicks = intervals.Sum(x => Math.Max(0, x.End - x.Start));
        var occupiedTicks = new bool[result.Duration];
        foreach (var (start, end) in intervals)
        {
            for (var tick = Math.Clamp(start, 0, result.Duration);
                 tick < Math.Clamp(end, 0, result.Duration);
                 tick++)
            {
                occupiedTicks[tick] = true;
            }
        }

        return new SummonActivity(
            intervals.Count,
            activeTicks / (double)result.Duration,
            occupiedTicks.Count(x => x) / (double)result.Duration * 100d);
    }

    private static EquipmentLoadoutUtilityBreakdown CreateUtilityBreakdown(
        AttributeBalanceScenario scenario,
        EquipmentLoadoutOutput output)
    {
        var damage = output.DirectDamage + output.SummonDamage;
        var sustain = output.Healing + output.HealthRegeneration;
        var prevention = output.BarrierAbsorbed;
        var (damageContribution, sustainContribution, preventionContribution, survivalContribution) =
            scenario switch
            {
                AttributeBalanceScenario.PhysicalOffense or
                AttributeBalanceScenario.MagicalOffense or
                AttributeBalanceScenario.PeriodicOffense =>
                    (damage, sustain * 0.35d, prevention * 0.35d, output.RemainingHealth * 0.05d),
                AttributeBalanceScenario.PhysicalPressure or
                AttributeBalanceScenario.MagicalPressure or
                AttributeBalanceScenario.MixedPressure or
                AttributeBalanceScenario.UnmitigatedPressure or
                AttributeBalanceScenario.BurstPressure =>
                    (0d, sustain, prevention, output.DurationTicks * 10d + output.RemainingHealth),
                AttributeBalanceScenario.HealingSustain or
                AttributeBalanceScenario.LongSustain =>
                    (0d, sustain, prevention, output.DurationTicks * 5d + output.RemainingHealth),
                AttributeBalanceScenario.StatusResilience =>
                    (damage * 0.25d, 0d, prevention, output.DurationTicks * 5d + output.RemainingHealth),
                AttributeBalanceScenario.CrowdControlResilience =>
                    (damage, 0d, prevention, output.DurationTicks + output.RemainingHealth * 0.05d),
                AttributeBalanceScenario.SummonOffense =>
                    (damage, 0d, prevention, output.RemainingHealth * 0.05d),
                _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
            };

        return new EquipmentLoadoutUtilityBreakdown(
            Round(damageContribution),
            Round(sustainContribution),
            Round(preventionContribution),
            Round(survivalContribution),
            Round(damageContribution + sustainContribution + preventionContribution + survivalContribution));
    }

    private static IReadOnlyList<CompiledAbility> SelectFriendlyAbilities(AttributeBalanceScenario scenario) =>
        scenario switch
        {
            AttributeBalanceScenario.PhysicalOffense => [AllAbilities["balance.physical-strike"]],
            AttributeBalanceScenario.MagicalOffense => [AllAbilities["balance.magical-strike"]],
            AttributeBalanceScenario.PeriodicOffense => [AllAbilities["balance.periodic-strike"]],
            AttributeBalanceScenario.HealingSustain =>
            [
                AllAbilities["balance.physical-strike"],
                AllAbilities["balance.self-heal"]
            ],
            AttributeBalanceScenario.CrowdControlResilience => [AllAbilities["balance.physical-strike"]],
            AttributeBalanceScenario.SummonOffense =>
            [
                AllAbilities["balance.physical-strike"],
                AllAbilities["balance.summon"]
            ],
            AttributeBalanceScenario.LongSustain => [AllAbilities["balance.physical-strike"]],
            _ => []
        };

    private static IReadOnlyList<CompiledAbility> SelectHostileAbilities(AttributeBalanceScenario scenario) =>
        scenario switch
        {
            AttributeBalanceScenario.StatusResilience => [AllAbilities["balance.apply-weaken"]],
            AttributeBalanceScenario.CrowdControlResilience => [AllAbilities["balance.apply-stun"]],
            AttributeBalanceScenario.SummonOffense => [AllAbilities["balance.area-pressure"]],
            AttributeBalanceScenario.MixedPressure =>
            [
                AllAbilities["balance.mixed-physical-pressure"],
                AllAbilities["balance.mixed-magical-pressure"]
            ],
            AttributeBalanceScenario.UnmitigatedPressure => [AllAbilities["balance.unmitigated-pressure"]],
            AttributeBalanceScenario.BurstPressure => [AllAbilities["balance.burst-pressure"]],
            _ => []
        };

    private static Dictionary<AttributeType, float> CreateReferenceAttributes(int tier)
    {
        var attributes = new Dictionary<AttributeType, float>
        {
            [AttributeType.Power] = 8f * tier,
            [AttributeType.MaxHealth] = 180 + tier * 112,
            [AttributeType.Armor] = 0,
            [AttributeType.Resistance] = 0,
            [AttributeType.CritChance] = 5,
            [AttributeType.CritDamage] = 50,
            [AttributeType.ArmorPenetration] = 0,
            [AttributeType.MagicPenetration] = 0,
            [AttributeType.DodgeChance] = 0,
            [AttributeType.BlockChance] = 0,
            [AttributeType.DamageReduction] = 0,
            [AttributeType.HealingPowerPercent] = 0,
            [AttributeType.HealthRegeneration] = 0,
            [AttributeType.LifeSteal] = 0,
            [AttributeType.Cooldown] = 0,
            [AttributeType.StatusResistance] = 0,
            [AttributeType.CrowdControlResistance] = 0,
            [AttributeType.SummonPower] = 0,
            [AttributeType.SummonHealth] = 0,
            [AttributeType.AttackSpeed] = 0
        };
        return attributes;
    }

    private static Dictionary<AttributeType, float> CreateOpponentAttributes(
        int tier,
        AttributeBalanceScenario scenario,
        double defenseMultiplier = 1d)
    {
        var pressureScenario = scenario is
            AttributeBalanceScenario.PhysicalPressure or
            AttributeBalanceScenario.MagicalPressure or
            AttributeBalanceScenario.HealingSustain or
            AttributeBalanceScenario.StatusResilience or
            AttributeBalanceScenario.CrowdControlResilience or
            AttributeBalanceScenario.MixedPressure or
            AttributeBalanceScenario.UnmitigatedPressure or
            AttributeBalanceScenario.BurstPressure or
            AttributeBalanceScenario.LongSustain;
        return new Dictionary<AttributeType, float>
        {
            [AttributeType.MaxHealth] = pressureScenario ? 1_000_000 : 2_000_000,
            [AttributeType.Power] = 8 + tier * 6,
            [AttributeType.Armor] = (float)(30d * Math.Max(0d, defenseMultiplier)),
            [AttributeType.Resistance] = (float)(30d * Math.Max(0d, defenseMultiplier)),
            [AttributeType.CritChance] = 0,
            [AttributeType.CritDamage] = 50,
            [AttributeType.AttackSpeed] = 0
        };
    }

    private static double CreateOpponentBasicAttackDamageMultiplier(
        int tier,
        AttributeBalanceScenario scenario)
    {
        var pressureScenario = scenario is
            AttributeBalanceScenario.PhysicalPressure or
            AttributeBalanceScenario.MagicalPressure or
            AttributeBalanceScenario.HealingSustain or
            AttributeBalanceScenario.StatusResilience or
            AttributeBalanceScenario.CrowdControlResilience or
            AttributeBalanceScenario.MixedPressure or
            AttributeBalanceScenario.UnmitigatedPressure or
            AttributeBalanceScenario.BurstPressure or
            AttributeBalanceScenario.LongSustain;
        var previousBasicAttackDamage = scenario switch
        {
            AttributeBalanceScenario.StatusResilience or
            AttributeBalanceScenario.CrowdControlResilience => 4 + tier * 3,
            AttributeBalanceScenario.MixedPressure => 4 + tier * 4,
            AttributeBalanceScenario.UnmitigatedPressure or
            AttributeBalanceScenario.BurstPressure => 3 + tier * 3,
            AttributeBalanceScenario.LongSustain => 4 + tier * 4,
            _ when pressureScenario => 10 + tier * 10,
            _ => 4 + tier * 2
        };
        var power = 8 + tier * 6;
        var previousRawDamage =
            Math.Max(1, previousBasicAttackDamage)
            + power * 0.1d;
        var powerOnlyRawDamage =
            1 + power * AttributeCombatRules.BasicAttackPowerCoefficient;
        return powerOnlyRawDamage <= 0 ? 1d : previousRawDamage / powerOnlyRawDamage;
    }

    private static void ApplyAttributeDelta(
        IDictionary<AttributeType, float> attributes,
        AttributeType attribute,
        float amount)
    {
        attributes[attribute] = (attributes.TryGetValue(attribute, out var current) ? current : 0) + amount;
    }

    private static int GetMaxTicks(AttributeBalanceScenario scenario) =>
        scenario switch
        {
            AttributeBalanceScenario.PhysicalOffense or
            AttributeBalanceScenario.MagicalOffense or
            AttributeBalanceScenario.PeriodicOffense or
            AttributeBalanceScenario.SummonOffense => 180,
            AttributeBalanceScenario.LongSustain => 600,
            _ => 240
        };

    private static int GetBasicAttackInterval(AttributeBalanceScenario scenario) =>
        scenario switch
        {
            AttributeBalanceScenario.PhysicalPressure or
            AttributeBalanceScenario.MagicalPressure or
            AttributeBalanceScenario.HealingSustain or
            AttributeBalanceScenario.StatusResilience or
            AttributeBalanceScenario.CrowdControlResilience or
            AttributeBalanceScenario.MixedPressure or
            AttributeBalanceScenario.UnmitigatedPressure or
            AttributeBalanceScenario.BurstPressure or
            AttributeBalanceScenario.LongSustain => 10,
            AttributeBalanceScenario.SummonOffense => NominalSummonBasicAttackIntervalTicks,
            _ => 20
        };

    private IReadOnlyList<EquipmentLoadoutMeasurement> AnalyzeLoadouts(
        CancellationToken cancellationToken)
    {
        var work = new List<LoadoutAnalysisWork>();
        var scenarios = Enum.GetValues<AttributeBalanceScenario>();

        foreach (var tier in ReferenceTiers)
        {
            foreach (var profile in LoadoutProfiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var allocation = CreateLoadoutAllocation(tier, profile);
                var samples = new Dictionary<AttributeBalanceScenario, LoadoutScenarioSample>();

                foreach (var scenario in scenarios)
                {
                    var outcomes = new List<ScenarioOutcome>(DeterministicSeeds.Count);
                    foreach (var seed in DeterministicSeeds)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        outcomes.Add(ExecuteScenario(
                            tier,
                            scenario,
                            new Dictionary<AttributeType, float>(allocation.Attributes),
                            profile.AbilityIds.Select(id => AllAbilities[id]).ToList(),
                            profile.BasicAttackIntervalMultiplier,
                            profile.BasicAttackDamageMultiplier,
                            profile.BasicAttackType,
                            profile.BasicAttackDamageType,
                            seed,
                            cancellationToken));
                    }

                    samples.Add(scenario, new LoadoutScenarioSample(outcomes));
                }

                work.Add(new LoadoutAnalysisWork(tier, profile, allocation, samples));
            }
        }

        var measurements = new List<EquipmentLoadoutMeasurement>(work.Count);
        foreach (var item in work)
        {
            var scenarioMeasurements = new List<EquipmentLoadoutScenarioMeasurement>();
            foreach (var (scenario, sample) in item.Samples)
            {
                var scenarioMedian = Median(work
                    .Where(x => x.Tier == item.Tier)
                    .Select(x => x.Samples[scenario].MeanUtility));
                var scores = sample.Outcomes.Select(x => x.Utility.Total).ToList();
                var (confidenceLow, confidenceHigh) = CalculateConfidenceInterval(scores);
                scenarioMeasurements.Add(new EquipmentLoadoutScenarioMeasurement(
                    scenario,
                    item.Profile.RelevantScenarios.Contains(scenario),
                    Round(sample.MeanUtility),
                    Round(CalculateRelativeGain(scenarioMedian, sample.MeanUtility)),
                    Round(confidenceLow),
                    Round(confidenceHigh),
                    AverageOutput(sample.Outcomes),
                    AverageUtility(sample.Outcomes)));
            }

            var relevantScenarioUtilityIndex = scenarioMeasurements
                .Where(x => x.IsRoleRelevant)
                .Select(x => 100d + x.RelativeToScenarioMedianPercent)
                .DefaultIfEmpty(0d)
                .Average();
            measurements.Add(new EquipmentLoadoutMeasurement(
                item.Profile.Id,
                item.Profile.Name,
                item.Tier,
                Round(item.Allocation.TargetBudget),
                Round(item.Allocation.SpentBudget),
                Round(item.Allocation.TargetBudget - item.Allocation.SpentBudget),
                Round(item.Allocation.AggregateRedistributedBudget),
                Round(relevantScenarioUtilityIndex),
                item.Allocation.Points,
                item.Allocation.PreRedistributionPoints,
                item.Allocation.AttributesOverSingleStatCap,
                CreateAggregateCapMeasurements(item.Allocation, item.Profile, beforeRedistribution: true),
                CreateAggregateCapMeasurements(item.Allocation, item.Profile, beforeRedistribution: false),
                CreateAllocationRecommendations(item.Allocation),
                scenarioMeasurements.OrderBy(x => x.Scenario).ToList()));
        }

        return measurements;
    }

    private static EquipmentLoadoutOutput AverageOutput(IReadOnlyList<ScenarioOutcome> outcomes) =>
        new(
            Round(outcomes.Average(x => x.Output.DirectDamage)),
            Round(outcomes.Average(x => x.Output.SummonDamage)),
            Round(outcomes.Average(x => x.Output.Healing)),
            Round(outcomes.Average(x => x.Output.HealthRegeneration)),
            Round(outcomes.Average(x => x.Output.BarrierGenerated)),
            Round(outcomes.Average(x => x.Output.BarrierAbsorbed)),
            Round(outcomes.Average(x => x.Output.IncomingRawDamage)),
            Round(outcomes.Average(x => x.Output.AvoidedDamage)),
            Round(outcomes.Average(x => x.Output.TypedMitigationPrevented)),
            Round(outcomes.Average(x => x.Output.PhysicalMitigationPrevented)),
            Round(outcomes.Average(x => x.Output.MagicalMitigationPrevented)),
            Round(outcomes.Average(x => x.Output.BlockPrevented)),
            Round(outcomes.Average(x => x.Output.DamageReductionPrevented)),
            Round(outcomes.Average(x => x.Output.DamageAmplified)),
            Round(outcomes.Average(x => x.Output.FinalHealthDamage)),
            Round(outcomes.Average(x => x.Output.DamageTaken)),
            Round(outcomes.Average(x => x.Output.RemainingHealth)),
            Round(outcomes.Average(x => x.Output.DurationTicks)),
            Round(outcomes.Average(x => x.Output.AvoidedAttacks)),
            Round(outcomes.Average(x => x.Output.SummonsCreated)),
            Round(outcomes.Average(x => x.Output.AverageActiveSummons)),
            Round(outcomes.Average(x => x.Output.SummonUptimePercent)));

    private static EquipmentLoadoutUtilityBreakdown AverageUtility(IReadOnlyList<ScenarioOutcome> outcomes) =>
        new(
            Round(outcomes.Average(x => x.Utility.Damage)),
            Round(outcomes.Average(x => x.Utility.Sustain)),
            Round(outcomes.Average(x => x.Utility.Prevention)),
            Round(outcomes.Average(x => x.Utility.Survival)),
            Round(outcomes.Average(x => x.Utility.Total)));

    private IReadOnlyList<SummonCalibrationComparison> AnalyzeSummonCalibration(
        CancellationToken cancellationToken)
    {
        var summonerProfile = LoadoutProfiles.Single(x => x.Id == "summoner");
        var directCasterProfile = new EquipmentLoadoutProfile(
            "direct-caster-control",
            "Direct Caster Control",
            summonerProfile.SlotWeights,
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.40d,
                [AttributeType.MaxHealth] = 0.15d,
                [AttributeType.CritChance] = 0.15d,
                [AttributeType.CritDamage] = 0.15d,
                [AttributeType.Cooldown] = 0.15d
            },
            ["balance.magical-strike", "balance.direct-control-burst"],
            [AttributeBalanceScenario.MagicalOffense],
            summonerProfile.BasicAttackIntervalMultiplier,
            summonerProfile.BasicAttackDamageMultiplier,
            summonerProfile.BasicAttackType,
            summonerProfile.BasicAttackDamageType);
        var comparisons = new List<SummonCalibrationComparison>();

        foreach (var tier in ReferenceTiers)
        {
            var summonerAllocation = CreateLoadoutAllocation(tier, summonerProfile);
            var directCasterAllocation = CreateLoadoutAllocation(tier, directCasterProfile);
            var noExplicitSummonAttributes = new Dictionary<AttributeType, float>(summonerAllocation.Attributes);
            noExplicitSummonAttributes[AttributeType.SummonPower] = Math.Max(
                0,
                noExplicitSummonAttributes.GetValueOrDefault(AttributeType.SummonPower)
                - (float)summonerAllocation.Points.GetValueOrDefault(AttributeType.SummonPower));
            noExplicitSummonAttributes[AttributeType.SummonHealth] = Math.Max(
                0,
                noExplicitSummonAttributes.GetValueOrDefault(AttributeType.SummonHealth)
                - (float)summonerAllocation.Points.GetValueOrDefault(AttributeType.SummonHealth));

            foreach (var duration in CalibrationDurations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var summonerOutput = RunCalibrationOutput(
                    tier,
                    duration,
                    summonerAllocation.Attributes,
                    ["balance.magical-strike", "balance.summon"],
                    summonerProfile,
                    cancellationToken);
                var withoutSummonAbilityOutput = RunCalibrationOutput(
                    tier,
                    duration,
                    summonerAllocation.Attributes,
                    ["balance.magical-strike"],
                    summonerProfile,
                    cancellationToken);
                var withoutExplicitSummonStatsOutput = RunCalibrationOutput(
                    tier,
                    duration,
                    noExplicitSummonAttributes,
                    ["balance.magical-strike", "balance.summon"],
                    summonerProfile,
                    cancellationToken);
                var directCasterOutput = RunCalibrationOutput(
                    tier,
                    duration,
                    directCasterAllocation.Attributes,
                    directCasterProfile.AbilityIds,
                    directCasterProfile,
                    cancellationToken);
                var summonerEfficiency = CalculateDamagePerHundredBudget(
                    summonerOutput,
                    summonerAllocation.SpentBudget);
                var directEfficiency = CalculateDamagePerHundredBudget(
                    directCasterOutput,
                    directCasterAllocation.SpentBudget);
                var referencePower = CreateReferenceAttributes(tier)
                    .GetValueOrDefault(AttributeType.Power);
                var summonAbilityReferenceDamage =
                    CalculateNominalSummonLifetimeDamage(referencePower);
                var directAbilityReferenceDamage =
                    CalculateNominalDirectControlDamage(referencePower);

                comparisons.Add(new SummonCalibrationComparison(
                    tier,
                    duration,
                    Round(summonerAllocation.SpentBudget),
                    Round(directCasterAllocation.SpentBudget),
                    Round(summonerEfficiency),
                    Round(directEfficiency),
                    Round(CalculateSymmetricDifference(summonerEfficiency, directEfficiency)),
                    Round(summonAbilityReferenceDamage),
                    Round(directAbilityReferenceDamage),
                    Round(CalculateSymmetricDifference(
                        summonAbilityReferenceDamage,
                        directAbilityReferenceDamage)),
                    Round(CalculateShare(
                        summonerOutput.SummonDamage,
                        summonerOutput.DirectDamage + summonerOutput.SummonDamage)),
                    Round(CalculateMarginalContribution(
                        withoutExplicitSummonStatsOutput.SummonDamage,
                        summonerOutput.SummonDamage)),
                    summonerOutput,
                    withoutSummonAbilityOutput,
                    withoutExplicitSummonStatsOutput,
                    directCasterOutput));
            }
        }

        return comparisons;
    }

    private IReadOnlyList<HandCalibrationComparison> AnalyzeHandCalibration(
        CancellationToken cancellationToken)
    {
        var recipes = _craftingDefinitions.GetRecipes();
        var fastWeaponBehavior = recipes
            .Single(x => x.Id == RepresentativeFastWeaponRecipeId)
            .Behavior;
        var slowWeaponBehavior = recipes
            .Single(x => x.Id == RepresentativeSlowWeaponRecipeId)
            .Behavior;
        var comparisons = new List<HandCalibrationComparison>();
        foreach (var tier in ReferenceTiers)
        {
            foreach (var duration in CalibrationDurations)
            {
                foreach (var mode in Enum.GetValues<HandCalibrationMode>())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var equalBudget = mode is
                        HandCalibrationMode.EqualBudget or
                        HandCalibrationMode.EqualBudgetAndBehavior;
                    var equalBehavior = mode == HandCalibrationMode.EqualBudgetAndBehavior;
                    var dualProfile = CreateMatchedHandProfile(
                        "matched-dual-wield",
                        [.. StandardEquipmentSlotWeights, 0.85d, 0.85d],
                        equalBehavior
                            ? 1d
                            : fastWeaponBehavior.BasicAttackIntervalMultiplier,
                        equalBehavior
                            ? 1d
                            : fastWeaponBehavior.BasicAttackDamageMultiplier);
                    var twoHandedProfile = CreateMatchedHandProfile(
                        "matched-two-handed",
                        [.. StandardEquipmentSlotWeights, equalBudget ? 1.70d : 1.40d],
                        equalBehavior
                            ? 1d
                            : slowWeaponBehavior.BasicAttackIntervalMultiplier,
                        equalBehavior
                            ? 1d
                            : slowWeaponBehavior.BasicAttackDamageMultiplier);
                    var dualAllocation = CreateLoadoutAllocation(tier, dualProfile);
                    var twoHandedAllocation = CreateLoadoutAllocation(tier, twoHandedProfile);
                    var dualOutput = RunCalibrationOutput(
                        tier,
                        duration,
                        dualAllocation.Attributes,
                        dualProfile.AbilityIds,
                        dualProfile,
                        cancellationToken);
                    var twoHandedOutput = RunCalibrationOutput(
                        tier,
                        duration,
                        twoHandedAllocation.Attributes,
                        twoHandedProfile.AbilityIds,
                        twoHandedProfile,
                        cancellationToken);
                    var dualEfficiency = CalculateDamagePerHundredBudget(
                        dualOutput,
                        dualAllocation.SpentBudget);
                    var twoHandedEfficiency = CalculateDamagePerHundredBudget(
                        twoHandedOutput,
                        twoHandedAllocation.SpentBudget);

                    comparisons.Add(new HandCalibrationComparison(
                        tier,
                        duration,
                        mode,
                        Round(dualAllocation.TargetBudget),
                        Round(twoHandedAllocation.TargetBudget),
                        Round(dualAllocation.SpentBudget),
                        Round(twoHandedAllocation.SpentBudget),
                        Round(dualEfficiency),
                        Round(twoHandedEfficiency),
                        Round(CalculateSymmetricDifference(dualEfficiency, twoHandedEfficiency)),
                        dualOutput,
                        twoHandedOutput));
                }
            }
        }

        return comparisons;
    }

    private IReadOnlyList<CraftingCombatPeerComparison> AnalyzeCraftingCombatPeers(
        CancellationToken cancellationToken)
    {
        var recipes = _craftingDefinitions.GetRecipes()
            .Where(x => x.Enabled && x.OutputItemType != EquipmentType.Tool)
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();
        var blueprints = _craftingDefinitions.GetBlueprints()
            .Where(x => x.Enabled)
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();
        var templates = CreateCatalogLoadoutTemplates(recipes, blueprints);
        var comparisons = new List<CraftingCombatPeerComparison>(
            ReferenceTiers.Count * CraftingCombatPeerSpecs.Count);

        foreach (var tier in ReferenceTiers)
        {
            foreach (var spec in CraftingCombatPeerSpecs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var firstTemplate = FindCatalogTemplate(
                    templates,
                    spec.FirstArmorFamily,
                    spec.FirstHandConfiguration,
                    spec.FirstBlueprintId);
                var secondTemplate = FindCatalogTemplate(
                    templates,
                    spec.SecondArmorFamily,
                    spec.SecondHandConfiguration,
                    spec.SecondBlueprintId);
                var first = CreateCatalogCombatAllocation(tier, firstTemplate);
                var second = CreateCatalogCombatAllocation(tier, secondTemplate);
                var firstOutcomes = MeasureCatalogCombatScenario(
                    tier,
                    spec.Scenario,
                    first,
                    cancellationToken);
                var secondOutcomes = MeasureCatalogCombatScenario(
                    tier,
                    spec.Scenario,
                    second,
                    cancellationToken);
                var firstUtility = CalculateUtilityPerHundredBudget(
                    AverageUtility(firstOutcomes).Total,
                    first.SpentBudget);
                var secondUtility = CalculateUtilityPerHundredBudget(
                    AverageUtility(secondOutcomes).Total,
                    second.SpentBudget);
                var difference = CalculateSymmetricDifference(firstUtility, secondUtility);

                comparisons.Add(new CraftingCombatPeerComparison(
                    spec.Id,
                    spec.Group,
                    tier,
                    spec.Scenario,
                    first.Id,
                    second.Id,
                    Round(first.SpentBudget),
                    Round(second.SpentBudget),
                    first.Points.ToDictionary(x => x.Key, x => Round(x.Value)),
                    second.Points.ToDictionary(x => x.Key, x => Round(x.Value)),
                    Round(firstUtility),
                    Round(secondUtility),
                    Round(difference),
                    spec.TolerancePercent,
                    spec.IsReleaseGate,
                    Math.Abs(difference) <= spec.TolerancePercent,
                    AverageOutput(firstOutcomes),
                    AverageOutput(secondOutcomes)));
            }
        }

        return comparisons;
    }

    private MaximumEquipmentProgressionReport AnalyzeMaximumEquipmentProgression(
        CancellationToken cancellationToken)
    {
        var recipes = _craftingDefinitions.GetRecipes()
            .Where(x => x.Enabled && x.OutputItemType != EquipmentType.Tool)
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();
        var blueprints = _craftingDefinitions.GetBlueprints()
            .Where(x => x.Enabled)
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();
        var templates = CreateCatalogLoadoutTemplates(recipes, blueprints);
        var allocations = new Dictionary<string, MaximumCatalogAllocation>(
            StringComparer.Ordinal);

        foreach (var template in templates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var allocation = CreateMaximumCatalogAllocation(template);
            allocations.Add(allocation.Combat.Id, allocation);
        }

        var comparisons = new List<CraftingCombatPeerComparison>(
            CraftingCombatPeerSpecs.Count);
        foreach (var spec in CraftingCombatPeerSpecs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var firstTemplate = FindCatalogTemplate(
                templates,
                spec.FirstArmorFamily,
                spec.FirstHandConfiguration,
                spec.FirstBlueprintId);
            var secondTemplate = FindCatalogTemplate(
                templates,
                spec.SecondArmorFamily,
                spec.SecondHandConfiguration,
                spec.SecondBlueprintId);
            var first = allocations[CreateCatalogLoadoutId(firstTemplate)];
            var second = allocations[CreateCatalogLoadoutId(secondTemplate)];
            var firstOutcomes = MeasureCatalogCombatScenario(
                MaximumEquipmentTier,
                spec.Scenario,
                first.Combat,
                cancellationToken);
            var secondOutcomes = MeasureCatalogCombatScenario(
                MaximumEquipmentTier,
                spec.Scenario,
                second.Combat,
                cancellationToken);
            var firstUtility = CalculateUtilityPerHundredBudget(
                AverageUtility(firstOutcomes).Total,
                first.Combat.SpentBudget);
            var secondUtility = CalculateUtilityPerHundredBudget(
                AverageUtility(secondOutcomes).Total,
                second.Combat.SpentBudget);
            var difference = CalculateSymmetricDifference(firstUtility, secondUtility);

            comparisons.Add(new CraftingCombatPeerComparison(
                spec.Id,
                spec.Group,
                MaximumEquipmentTier,
                spec.Scenario,
                first.Combat.Id,
                second.Combat.Id,
                Round(first.Combat.SpentBudget),
                Round(second.Combat.SpentBudget),
                first.Combat.Points.ToDictionary(x => x.Key, x => Round(x.Value)),
                second.Combat.Points.ToDictionary(x => x.Key, x => Round(x.Value)),
                Round(firstUtility),
                Round(secondUtility),
                Round(difference),
                spec.TolerancePercent,
                spec.IsReleaseGate,
                Math.Abs(difference) <= spec.TolerancePercent,
                AverageOutput(firstOutcomes),
                AverageOutput(secondOutcomes)));
        }

        var measuredAllocations = allocations.Values
            .Select(allocation =>
            {
                var caps = CalculateCatalogCapResults(
                    MaximumEquipmentTier,
                    allocation.TargetBudget,
                    allocation.Combat.Points,
                    allocation.Combat.BasicAttackIntervalMultiplier);
                return (
                    Measurement: new MaximumEquipmentLoadoutMeasurement(
                        allocation.Combat.Id,
                        allocation.ArmorFamily,
                        allocation.HandConfiguration,
                        allocation.BlueprintId,
                        Round(allocation.TargetBudget),
                        Round(allocation.Combat.SpentBudget),
                        Round(allocation.StaticBaseModifierBudget),
                        Round(allocation.GeneratedStatBudget),
                        Round(allocation.RarityImprovementBudget),
                        Round(allocation.UnspentBudget),
                        Round(caps.Select(x => x.WastedBudgetPercent)
                            .DefaultIfEmpty(0d)
                            .Max()),
                        caps
                            .Where(x => x.ExcessPoints > 0.001d)
                            .Select(x => x.Attribute)
                            .Order()
                            .ToList()),
                    Caps: caps);
            })
            .ToList();
        var measurements = measuredAllocations
            .Select(entry => entry.Measurement)
            .ToList();
        var capSaturationByAttribute = measuredAllocations
            .SelectMany(entry => entry.Caps.Where(cap => cap.ExcessPoints > 0.001d))
            .GroupBy(cap => cap.Attribute)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => new MaximumEquipmentCapSaturationGroup(
                group.Key,
                group.Count(),
                Round(group.Average(cap => cap.WastedBudgetPercent)),
                Round(group.Max(cap => cap.WastedBudgetPercent))))
            .ToList();
        var unspentBudgetByRecipe = allocations.Values
            .SelectMany(allocation => allocation.ItemDiagnostics)
            .Where(item => item.GeneratedUnspentBudget > 0.01d
                           || item.RarityUnspentBudget > 0.01d)
            .GroupBy(item => new
            {
                item.RecipeId,
                item.EquipmentType,
                item.BlueprintId
            })
            .Select(group => new MaximumEquipmentUnspentBudgetGroup(
                group.Key.RecipeId,
                group.Key.EquipmentType,
                group.Key.BlueprintId,
                group.Count(),
                Round(group.Sum(item => item.GeneratedUnspentBudget)),
                Round(group.Sum(item => item.RarityUnspentBudget)),
                Round(group.Sum(item =>
                    item.GeneratedUnspentBudget + item.RarityUnspentBudget)),
                group.SelectMany(item => item.CappedAttributes)
                    .Distinct()
                    .Order()
                    .ToList(),
                group.SelectMany(item => item.BindingCombatCaps)
                    .Distinct()
                    .Order()
                    .ToList()))
            .OrderByDescending(group => group.TotalUnspentBudget)
            .ThenBy(group => group.RecipeId, StringComparer.Ordinal)
            .ThenBy(group => group.BlueprintId, StringComparer.Ordinal)
            .ToList();

        return new MaximumEquipmentProgressionReport(
            MaximumEquipmentTier,
            MaximumEquipmentQuality,
            MaximumEquipmentRarity,
            _craftingBalance.GetQualityStatMultiplier(MaximumEquipmentQuality),
            MaximumCraftingVarianceMultiplier,
            TemperingConstants.GetRarityUpgradeCount(MaximumEquipmentRarity),
            measurements.Count,
            measurements.Count(x => x.AttributesOverCap.Count > 0),
            measurements.Count(x => x.UnspentBudget > 0.01d),
            comparisons,
            capSaturationByAttribute,
            unspentBudgetByRecipe,
            measurements
                .Where(x => x.AttributesOverCap.Count > 0 || x.UnspentBudget > 0.01d)
                .OrderByDescending(x => x.MaximumWastedBudgetPercent)
                .ThenByDescending(x => x.UnspentBudget)
                .ThenBy(x => x.Id, StringComparer.Ordinal)
                .Take(20)
                .ToList());
    }

    private MaximumCatalogAllocation CreateMaximumCatalogAllocation(
        CatalogLoadoutTemplate template)
    {
        var designs = template.Recipes
            .Select(recipe => EquipmentCraftingDesignComposer.Compose(
                recipe,
                template.Blueprint is not null
                && EquipmentCraftingDesignComposer.IsCompatible(recipe, template.Blueprint)
                    ? template.Blueprint
                    : null))
            .ToList();
        var mainHandIndex = template.Recipes
            .Select((recipe, index) => (recipe, index))
            .First(x => x.recipe.Id.Equals(
                template.MainHandRecipeId,
                StringComparison.OrdinalIgnoreCase))
            .index;
        var mainHandBehavior = designs[mainHandIndex].Behavior;
        var tierBudget = _craftingBalance.GetTierPowerBudget(MaximumEquipmentTier);
        var maximumLoadoutWeight = _craftingBalance.GetMaximumCombatLoadoutBudgetWeight();
        var qualityMultiplier =
            _craftingBalance.GetQualityStatMultiplier(MaximumEquipmentQuality);
        var rarityUpgradeBudget =
            TemperingConstants.GetRarityUpgradeCount(MaximumEquipmentRarity)
            * TemperingConstants.GetDirectedImprovementBudget(MaximumEquipmentTier);
        var points = new Dictionary<AttributeType, double>();
        var targetBudget = 0d;
        var spentBudget = 0d;
        var staticBaseModifierBudget = 0d;
        var generatedStatBudget = 0d;
        var rarityImprovementBudget = 0d;
        var unspentBudget = 0d;
        var itemDiagnostics = new List<MaximumItemProgressionDiagnostic>(
            designs.Count);

        for (var index = 0; index < designs.Count; index++)
        {
            var design = designs[index];
            var recipe = template.Recipes[index];
            var slotWeight = _craftingBalance.GetSlotBudgetWeight(recipe.OutputItemType);
            var constraints = EquipmentConstraintProfile.CreateItemConstraints(
                EquipmentConstraintProfile.CreateTierBaseline(MaximumEquipmentTier),
                MaximumEquipmentTier,
                slotWeight,
                maximumLoadoutWeight,
                EquipmentConstraintProfile.MinimumSupportedBasicAttackIntervalMultiplier);
            var perItemCapMultiplier =
                EquipmentConstraintProfile.GetPerItemCapMultiplier(slotWeight);
            // Crafted equipment is entirely budgeted by its generated and tempered
            // instance modifiers. Authored item-base modifiers are a legacy/direct-grant
            // path and must not be included in recipe progression audits.
            var itemPoints = new Dictionary<AttributeType, double>();

            var generatedBaseBudget =
                tierBudget
                * slotWeight
                * qualityMultiplier
                * MaximumCraftingVarianceMultiplier;
            var generated = EquipmentBudgetAllocator.AllocateDesignConstrained(
                MaximumEquipmentTier,
                generatedBaseBudget,
                design,
                constraints,
                EquipmentConstraintProfile.GetOverflowWeights(
                    EquipmentCraftingDesignComposer.Compose(recipe, null)),
                itemPoints,
                perItemCapMultiplier);
            AddPoints(itemPoints, generated.AddedPoints);
            generatedStatBudget += generated.SpentBudget;
            targetBudget += generated.TargetBudget;
            spentBudget += generated.SpentBudget;
            unspentBudget += generated.UnspentBudget;

            var temperingWeights = design.TemperingProfile.Stats
                .Where(stat =>
                    stat.MinimumTier is null
                    || stat.MinimumTier <= MaximumEquipmentTier)
                .Where(stat =>
                    itemPoints.ContainsKey(stat.Stat)
                        ? stat.CanIncrease
                        : stat.CanIntroduce)
                .GroupBy(stat => stat.Stat)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(stat => Math.Max(0d, stat.Weight)));
            var rarity = EquipmentBudgetAllocator.AllocateConstrained(
                MaximumEquipmentTier,
                rarityUpgradeBudget,
                temperingWeights,
                constraints,
                EquipmentConstraintProfile.GetRarityOverflowWeights(
                    recipe.OutputItemType,
                    design.TemperingProfile),
                itemPoints,
                perItemCapMultiplier
                * EquipmentConstraintProfile.RarityImprovementCapMultiplier);
            AddPoints(itemPoints, rarity.AddedPoints);
            rarityImprovementBudget += rarity.SpentBudget;
            targetBudget += rarity.TargetBudget;
            spentBudget += rarity.SpentBudget;
            unspentBudget += rarity.UnspentBudget;
            AddPoints(points, itemPoints);
            itemDiagnostics.Add(new MaximumItemProgressionDiagnostic(
                recipe.Id,
                recipe.OutputItemType,
                template.Blueprint is not null
                && EquipmentCraftingDesignComposer.IsCompatible(
                    recipe,
                    template.Blueprint)
                    ? template.Blueprint.Id
                    : null,
                generated.UnspentBudget,
                rarity.UnspentBudget,
                generated.CappedAttributes
                    .Concat(rarity.CappedAttributes)
                    .Distinct()
                    .Order()
                    .ToList(),
                generated.BindingCombatCaps
                    .Concat(rarity.BindingCombatCaps)
                    .Distinct()
                    .Order()
                    .ToList()));
        }

        var attributes = CreateReferenceAttributes(MaximumEquipmentTier);
        foreach (var (attribute, pointDelta) in points)
            ApplyAttributeDelta(attributes, attribute, (float)pointDelta);

        var combat = new CatalogCombatAllocation(
            CreateCatalogLoadoutId(template),
            spentBudget,
            points,
            attributes,
            mainHandBehavior.BasicAttackIntervalMultiplier,
            mainHandBehavior.BasicAttackDamageMultiplier,
            mainHandBehavior.RangeCategory.Equals("Ranged", StringComparison.OrdinalIgnoreCase)
                ? AttackType.Ranged
                : AttackType.Melee,
            mainHandBehavior.AttackCategory.Equals("Magical", StringComparison.OrdinalIgnoreCase)
                ? DamageType.Magical
                : DamageType.Physical);
        return new MaximumCatalogAllocation(
            template.ArmorFamily,
            template.HandConfiguration,
            template.Blueprint?.Id,
            targetBudget,
            staticBaseModifierBudget,
            generatedStatBudget,
            rarityImprovementBudget,
            unspentBudget,
            itemDiagnostics,
            combat);
    }

    private static string CreateCatalogLoadoutId(CatalogLoadoutTemplate template) =>
        $"{template.ArmorFamily}|{template.HandConfiguration}|" +
        $"{template.Blueprint?.Id ?? "base"}";

    private static CatalogLoadoutTemplate FindCatalogTemplate(
        IReadOnlyList<CatalogLoadoutTemplate> templates,
        string armorFamily,
        string handConfiguration,
        string? blueprintId) =>
        templates.Single(x =>
            x.ArmorFamily.Equals(armorFamily, StringComparison.OrdinalIgnoreCase)
            && x.HandConfiguration.Equals(handConfiguration, StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                x.Blueprint?.Id,
                blueprintId,
                StringComparison.OrdinalIgnoreCase));

    private CatalogCombatAllocation CreateCatalogCombatAllocation(
        int tier,
        CatalogLoadoutTemplate template)
    {
        var designs = template.Recipes
            .Select(recipe => EquipmentCraftingDesignComposer.Compose(
                recipe,
                template.Blueprint is not null
                && EquipmentCraftingDesignComposer.IsCompatible(recipe, template.Blueprint)
                    ? template.Blueprint
                    : null))
            .ToList();
        var mainHandIndex = template.Recipes
            .Select((recipe, index) => (recipe, index))
            .First(x => x.recipe.Id.Equals(
                template.MainHandRecipeId,
                StringComparison.OrdinalIgnoreCase))
            .index;
        var mainHandBehavior = designs[mainHandIndex].Behavior;
        var slotWeights = template.Recipes
            .Select(recipe => _craftingBalance.GetSlotBudgetWeight(recipe.OutputItemType))
            .ToList();
        var expectedLoadoutWeight =
            _craftingBalance.GetMaximumCombatLoadoutBudgetWeight();
        var tierBudget = _craftingBalance.GetTierPowerBudget(tier);
        var baselineAttributes = CreateReferenceAttributes(tier);
        var points = new Dictionary<AttributeType, double>();
        var spentBudget = 0d;

        for (var index = 0; index < designs.Count; index++)
        {
            var itemBudget = tierBudget * slotWeights[index];
            var constraints = EquipmentConstraintProfile.CreateItemConstraints(
                baselineAttributes,
                tier,
                slotWeights[index],
                expectedLoadoutWeight,
                EquipmentConstraintProfile.MinimumSupportedBasicAttackIntervalMultiplier);
            var allocation = EquipmentBudgetAllocator.AllocateDesignConstrained(
                tier,
                itemBudget,
                designs[index],
                constraints,
                EquipmentConstraintProfile.GetOverflowWeights(
                    EquipmentCraftingDesignComposer.Compose(designs[index].Recipe, null)),
                perItemCapMultiplier:
                    EquipmentConstraintProfile.GetPerItemCapMultiplier(slotWeights[index]));
            AddPoints(points, allocation.AddedPoints);
            spentBudget += allocation.SpentBudget;
        }

        var attributes = CreateReferenceAttributes(tier);
        foreach (var (attribute, pointDelta) in points)
            ApplyAttributeDelta(attributes, attribute, (float)pointDelta);

        return new CatalogCombatAllocation(
            $"{template.ArmorFamily}|{template.HandConfiguration}|" +
            $"{template.Blueprint?.Id ?? "base"}",
            spentBudget,
            points,
            attributes,
            mainHandBehavior.BasicAttackIntervalMultiplier,
            mainHandBehavior.BasicAttackDamageMultiplier,
            mainHandBehavior.RangeCategory.Equals("Ranged", StringComparison.OrdinalIgnoreCase)
                ? AttackType.Ranged
                : AttackType.Melee,
            mainHandBehavior.AttackCategory.Equals("Magical", StringComparison.OrdinalIgnoreCase)
                ? DamageType.Magical
                : DamageType.Physical);
    }

    private static IReadOnlyList<ScenarioOutcome> MeasureCatalogCombatScenario(
        int tier,
        AttributeBalanceScenario scenario,
        CatalogCombatAllocation allocation,
        CancellationToken cancellationToken)
    {
        var outcomes = new List<ScenarioOutcome>(DeterministicSeeds.Count);
        foreach (var seed in DeterministicSeeds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            outcomes.Add(ExecuteScenario(
                tier,
                scenario,
                new Dictionary<AttributeType, float>(allocation.Attributes),
                SelectFriendlyAbilities(scenario),
                allocation.BasicAttackIntervalMultiplier,
                allocation.BasicAttackDamageMultiplier,
                allocation.BasicAttackType,
                allocation.BasicAttackDamageType,
                seed,
                cancellationToken));
        }

        return outcomes;
    }

    private static double CalculateUtilityPerHundredBudget(
        double utility,
        double spentBudget) =>
        spentBudget <= 0 ? 0 : utility / spentBudget * 100d;

    private CraftingCatalogConstraintReport AnalyzeCraftingCatalogConstraints(
        CancellationToken cancellationToken)
    {
        var recipes = _craftingDefinitions.GetRecipes()
            .Where(x => x.Enabled && x.OutputItemType != EquipmentType.Tool)
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();
        var blueprints = _craftingDefinitions.GetBlueprints()
            .Where(x => x.Enabled)
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();
        var templates = CreateCatalogLoadoutTemplates(recipes, blueprints);
        var measurements = new List<CatalogShadowWork>(templates.Count * ReferenceTiers.Count);

        foreach (var tier in ReferenceTiers)
        {
            foreach (var template in templates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                measurements.Add(MeasureCatalogShadowLoadout(tier, template));
            }
        }

        var statSummaries = EquipmentStatBudgetCatalog.Attributes
            .Where(attribute => AttributeCatalog.TryGetEffectiveCharacterCap(
                attribute,
                basicAttackIntervalMultiplier: 1d,
                out _))
            .Order()
            .Select(attribute => new CraftingCatalogStatConstraintSummary(
                attribute,
                measurements.Count(x => x.CurrentCaps.Any(cap =>
                    cap.Attribute == attribute && cap.ExcessPoints > 0.001d)),
                measurements.Count(x => x.ShadowCaps.Any(cap =>
                    cap.Attribute == attribute && cap.ExcessPoints > 0.001d)),
                Round(measurements
                    .SelectMany(x => x.CurrentCaps)
                    .Where(x => x.Attribute == attribute)
                    .Select(x => x.ExcessPoints)
                    .DefaultIfEmpty(0d)
                    .Max()),
                Round(measurements
                    .SelectMany(x => x.ShadowCaps)
                    .Where(x => x.Attribute == attribute)
                    .Select(x => x.ExcessPoints)
                    .DefaultIfEmpty(0d)
                    .Max())))
            .ToList();
        var worstCurrentLoadouts = measurements
            .Where(x => x.CurrentAttributesOverCap.Count > 0 || x.CurrentUnspentBudget > 0.01d)
            .OrderByDescending(x => x.CurrentMaximumWastedBudgetPercent)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .Take(20)
            .Select(x => new CraftingCatalogLoadoutConstraintMeasurement(
                x.Id,
                x.Tier,
                x.ArmorFamily,
                x.HandConfiguration,
                x.BlueprintId,
                Round(x.TargetBudget),
                Round(x.CurrentSpentBudget),
                Round(x.ShadowSpentBudget),
                Round(x.CurrentMaximumWastedBudgetPercent),
                Round(x.ShadowMaximumWastedBudgetPercent),
                x.CurrentAttributesOverCap,
                x.ShadowAttributesOverCap))
            .ToList();
        var composedDesignCount = recipes.Count
            + recipes.Sum(recipe => blueprints.Count(blueprint =>
                EquipmentCraftingDesignComposer.IsCompatible(recipe, blueprint)));
        return new CraftingCatalogConstraintReport(
            EquipmentConstraintProfile.BalanceVersion,
            EquipmentConstraintProfile.ProductionActive,
            recipes.Count,
            blueprints.Count,
            composedDesignCount,
            measurements.Count,
            measurements.Count(x => x.CurrentAttributesOverCap.Count > 0),
            measurements.Count(x => x.ShadowAttributesOverCap.Count > 0),
            measurements.Count(x => x.CurrentUnspentBudget > 0.01d),
            measurements.Count(x => x.ShadowUnspentBudget > 0.01d),
            statSummaries,
            worstCurrentLoadouts);
    }

    private CatalogShadowWork MeasureCatalogShadowLoadout(
        int tier,
        CatalogLoadoutTemplate template)
    {
        var designs = template.Recipes
            .Select(recipe => EquipmentCraftingDesignComposer.Compose(
                recipe,
                template.Blueprint is not null
                && EquipmentCraftingDesignComposer.IsCompatible(recipe, template.Blueprint)
                    ? template.Blueprint
                    : null))
            .ToList();
        var mainHandIndex = template.Recipes
            .Select((recipe, index) => (recipe, index))
            .First(x => x.recipe.Id.Equals(
                template.MainHandRecipeId,
                StringComparison.OrdinalIgnoreCase))
            .index;
        var basicAttackIntervalMultiplier =
            designs[mainHandIndex].Behavior.BasicAttackIntervalMultiplier;
        var slotWeights = template.Recipes
            .Select(recipe => _craftingBalance.GetSlotBudgetWeight(recipe.OutputItemType))
            .ToList();
        var expectedLoadoutWeight = slotWeights.Sum();
        var tierBudget = _craftingBalance.GetTierPowerBudget(tier);
        var targetBudget = designs
            .Select((design, index) =>
                tierBudget
                * slotWeights[index]
                * (1d + design.BlueprintBonusBudgetMultiplier))
            .Sum();
        var baselineAttributes = CreateReferenceAttributes(tier);
        var currentPoints = new Dictionary<AttributeType, double>();
        var shadowPoints = new Dictionary<AttributeType, double>();
        var currentSpentBudget = 0d;
        var shadowSpentBudget = 0d;

        for (var index = 0; index < designs.Count; index++)
        {
            var itemBudget = tierBudget * slotWeights[index];
            var productionConstraints = EquipmentConstraintProfile.CreateItemConstraints(
                baselineAttributes,
                tier,
                slotWeights[index],
                _craftingBalance.GetMaximumCombatLoadoutBudgetWeight(),
                EquipmentConstraintProfile.MinimumSupportedBasicAttackIntervalMultiplier);
            var baseDesign = EquipmentCraftingDesignComposer.Compose(designs[index].Recipe, null);
            var current = EquipmentBudgetAllocator.AllocateDesignConstrained(
                tier,
                itemBudget,
                designs[index],
                productionConstraints,
                EquipmentConstraintProfile.GetOverflowWeights(baseDesign),
                perItemCapMultiplier:
                    EquipmentConstraintProfile.GetPerItemCapMultiplier(slotWeights[index]));
            var candidateConstraints = EquipmentConstraintProfile.CreateItemConstraints(
                baselineAttributes,
                tier,
                slotWeights[index],
                expectedLoadoutWeight,
                basicAttackIntervalMultiplier);
            var shadow = EquipmentBudgetAllocator.AllocateDesignConstrained(
                tier,
                itemBudget,
                designs[index],
                candidateConstraints,
                EquipmentConstraintProfile.GetOverflowWeights(baseDesign),
                perItemCapMultiplier:
                    EquipmentConstraintProfile.GetPerItemCapMultiplier(slotWeights[index]));

            AddPoints(currentPoints, current.AddedPoints);
            AddPoints(shadowPoints, shadow.AddedPoints);
            currentSpentBudget += current.SpentBudget;
            shadowSpentBudget += shadow.SpentBudget;
        }

        var currentCaps = CalculateCatalogCapResults(
            tier,
            targetBudget,
            currentPoints,
            basicAttackIntervalMultiplier);
        var shadowCaps = CalculateCatalogCapResults(
            tier,
            targetBudget,
            shadowPoints,
            basicAttackIntervalMultiplier);

        return new CatalogShadowWork(
            $"{template.ArmorFamily}|{template.HandConfiguration}|" +
            $"{template.Blueprint?.Id ?? "base"}|t{tier}",
            tier,
            template.ArmorFamily,
            template.HandConfiguration,
            template.Blueprint?.Id,
            targetBudget,
            currentSpentBudget,
            shadowSpentBudget,
            Math.Max(0d, targetBudget - currentSpentBudget),
            Math.Max(0d, targetBudget - shadowSpentBudget),
            currentCaps,
            shadowCaps);
    }

    private static IReadOnlyList<CatalogLoadoutTemplate> CreateCatalogLoadoutTemplates(
        IReadOnlyList<CraftingRecipeDefinition> recipes,
        IReadOnlyList<BlueprintDefinition> blueprints)
    {
        var armorSets = recipes
            .Where(x => x.OutputItemType is
                EquipmentType.Head or EquipmentType.Chest or EquipmentType.Legs)
            .GroupBy(x => x.Behavior.Role, StringComparer.OrdinalIgnoreCase)
            .Where(group =>
                group.Count(x => x.OutputItemType == EquipmentType.Head) == 1
                && group.Count(x => x.OutputItemType == EquipmentType.Chest) == 1
                && group.Count(x => x.OutputItemType == EquipmentType.Legs) == 1)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => (
                Family: group.Key,
                Recipes: group.OrderBy(x => x.OutputItemType).ToList()))
            .ToList();
        var jewelry = recipes
            .Where(x => x.OutputItemType is
                EquipmentType.Ring or EquipmentType.Necklace or EquipmentType.Relic)
            .OrderBy(x => x.OutputItemType)
            .ToList();
        var oneHanded = recipes
            .Where(x => x.OutputItemType == EquipmentType.OneHanded)
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();
        var twoHanded = recipes
            .Where(x => x.OutputItemType == EquipmentType.TwoHanded)
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();
        var offHands = recipes
            .Where(x => x.OutputItemType == EquipmentType.OffHand)
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();
        var handConfigurations = new List<CatalogHandConfiguration>();
        handConfigurations.AddRange(twoHanded.Select(recipe =>
            new CatalogHandConfiguration(
                $"two-handed:{recipe.Id}",
                [recipe],
                recipe.Id)));
        handConfigurations.AddRange(oneHanded.Select(recipe =>
            new CatalogHandConfiguration(
                $"dual:{recipe.Id}",
                [recipe, recipe],
                recipe.Id)));
        handConfigurations.AddRange(
            oneHanded.SelectMany(mainHand => offHands.Select(offHand =>
                new CatalogHandConfiguration(
                    $"one-off:{mainHand.Id}+{offHand.Id}",
                    [mainHand, offHand],
                    mainHand.Id))));

        var templates = new List<CatalogLoadoutTemplate>();
        foreach (var armorSet in armorSets)
        {
            foreach (var handConfiguration in handConfigurations)
            {
                var loadoutRecipes = armorSet.Recipes
                    .Concat(jewelry)
                    .Concat(handConfiguration.Recipes)
                    .ToList();
                templates.Add(new CatalogLoadoutTemplate(
                    armorSet.Family,
                    handConfiguration.Id,
                    loadoutRecipes,
                    handConfiguration.MainHandRecipeId,
                    null));
                foreach (var blueprint in blueprints.Where(blueprint =>
                             loadoutRecipes.Any(recipe =>
                                 EquipmentCraftingDesignComposer.IsCompatible(
                                     recipe,
                                     blueprint))))
                {
                    templates.Add(new CatalogLoadoutTemplate(
                        armorSet.Family,
                        handConfiguration.Id,
                        loadoutRecipes,
                        handConfiguration.MainHandRecipeId,
                        blueprint));
                }
            }
        }

        return templates;
    }

    private static IReadOnlyList<CatalogCapResult> CalculateCatalogCapResults(
        int tier,
        double targetBudget,
        IReadOnlyDictionary<AttributeType, double> points,
        double basicAttackIntervalMultiplier)
    {
        var attributes = CreateReferenceAttributes(tier);
        foreach (var (attribute, pointDelta) in points)
            ApplyAttributeDelta(attributes, attribute, (float)pointDelta);

        return EquipmentStatBudgetCatalog.Attributes
            .Order()
            .Where(attribute => AttributeCatalog.TryGetEffectiveCharacterCap(
                attribute,
                basicAttackIntervalMultiplier,
                out _))
            .Select(attribute =>
            {
                AttributeCatalog.TryGetEffectiveCharacterCap(
                    attribute,
                    basicAttackIntervalMultiplier,
                    out var effectiveCap);
                var excessPoints = Math.Max(
                    0d,
                    attributes.GetValueOrDefault(attribute) - effectiveCap);
                var equivalentWastedBudget =
                    excessPoints * EquipmentStatBudgetCatalog.Get(attribute, tier).CostPerPoint;
                return new CatalogCapResult(
                    attribute,
                    excessPoints,
                    targetBudget <= 0
                        ? 0
                        : equivalentWastedBudget / targetBudget * 100d);
            })
            .ToList();
    }

    private static void AddPoints(
        IDictionary<AttributeType, double> target,
        IReadOnlyDictionary<AttributeType, double> source)
    {
        foreach (var (attribute, points) in source)
            target[attribute] =
                (target.TryGetValue(attribute, out var current) ? current : 0d) + points;
    }

    private static EquipmentLoadoutProfile CreateMatchedHandProfile(
        string id,
        IReadOnlyList<double> slotWeights,
        double basicAttackIntervalMultiplier,
        double basicAttackDamageMultiplier) =>
        new(
            id,
            id,
            slotWeights,
            MatchedHandBudgetShares,
            ["balance.physical-strike"],
            [AttributeBalanceScenario.PhysicalOffense],
            basicAttackIntervalMultiplier,
            basicAttackDamageMultiplier,
            AttackType.Melee,
            DamageType.Physical);

    private static EquipmentLoadoutOutput RunCalibrationOutput(
        int tier,
        int duration,
        IReadOnlyDictionary<AttributeType, float> attributes,
        IReadOnlyList<string> abilityIds,
        EquipmentLoadoutProfile profile,
        CancellationToken cancellationToken)
    {
        var outcomes = new List<ScenarioOutcome>(DeterministicSeeds.Count);
        foreach (var seed in DeterministicSeeds)
        {
            outcomes.Add(ExecuteScenario(
                tier,
                profile.Id.StartsWith("summoner", StringComparison.Ordinal)
                    ? AttributeBalanceScenario.SummonOffense
                    : profile.Id == "direct-caster-control"
                        ? AttributeBalanceScenario.SummonOffense
                        : AttributeBalanceScenario.PhysicalOffense,
                new Dictionary<AttributeType, float>(attributes),
                abilityIds.Select(id => AllAbilities[id]).ToList(),
                profile.BasicAttackIntervalMultiplier,
                profile.BasicAttackDamageMultiplier,
                profile.BasicAttackType,
                profile.BasicAttackDamageType,
                seed,
                cancellationToken,
                duration));
        }

        return AverageOutput(outcomes);
    }

    private static double CalculateDamagePerHundredBudget(
        EquipmentLoadoutOutput output,
        double spentBudget) =>
        spentBudget <= 0
            ? 0
            : (output.DirectDamage + output.SummonDamage) / spentBudget * 100d;

    private static double CalculateSymmetricDifference(double first, double second)
    {
        var midpoint = (first + second) / 2d;
        return midpoint <= 0 ? 0 : (first - second) / midpoint * 100d;
    }

    private static double CalculateShare(double part, double total) =>
        total <= 0 ? 0 : part / total * 100d;

    private static double CalculateMarginalContribution(double baseline, double modified) =>
        modified <= 0 ? 0 : (modified - baseline) / modified * 100d;

    private static EquipmentBalanceCalibrationGate CreateCalibrationGate(
        IReadOnlyList<EqualBudgetAttributeComparison> equalBudgetComparisons,
        IReadOnlyList<EquipmentLoadoutMeasurement> loadouts,
        IReadOnlyList<SummonCalibrationComparison> summonCalibrations,
        IReadOnlyList<HandCalibrationComparison> handCalibrations,
        IReadOnlyList<CraftingCombatPeerComparison> craftingCombatPeers,
        MaximumEquipmentProgressionReport maximumEquipmentProgression)
    {
        var equalBudgetFailures = equalBudgetComparisons
            .Where(x => x.IsReleaseGate && !x.Passed)
            .ToList();
        var aggregateCapFailures = loadouts
            .Where(x => x.AggregateCapsBeforeRedistribution.Any(cap =>
                cap.WastedTargetBudgetPercent > AggregateCapWasteTolerancePercent))
            .ToList();
        var candidateAggregateCapFailures = loadouts
            .Where(x => x.AggregateCaps.Any(cap =>
                cap.WastedTargetBudgetPercent > AggregateCapWasteTolerancePercent))
            .ToList();
        var summonFailures = summonCalibrations
            .Where(x => Math.Abs(x.EqualBudgetDifferencePercent) > SummonCalibrationTolerancePercent)
            .ToList();
        var handFailures = handCalibrations
            .Where(x =>
                x.Mode == HandCalibrationMode.RepresentativeFundingAndBehavior
                && Math.Abs(x.DifferencePercent) > HandCalibrationTolerancePercent)
            .ToList();
        var craftingCombatPeerFailures = craftingCombatPeers
            .Where(x => x.IsReleaseGate && !x.Passed)
            .ToList();
        var blockers = new List<string>();
        if (aggregateCapFailures.Count > 0)
        {
            blockers.Add(
                $"{aggregateCapFailures.Count} loadouts waste more than " +
                $"{AggregateCapWasteTolerancePercent:0.##}% of target budget at aggregate combat caps.");
        }

        if (equalBudgetFailures.Count > 0)
        {
            blockers.Add(
                $"{equalBudgetFailures.Count} equal-budget peer comparisons exceed " +
                "their approved tolerances.");
        }

        if (summonFailures.Count > 0)
        {
            blockers.Add(
                $"{summonFailures.Count} summon comparisons exceed " +
                $"{SummonCalibrationTolerancePercent:0.##}%.");
        }

        if (handFailures.Count > 0)
        {
            blockers.Add(
                $"{handFailures.Count} representative hand comparisons exceed " +
                $"{HandCalibrationTolerancePercent:0.##}%.");
        }

        if (craftingCombatPeerFailures.Count > 0)
        {
            blockers.Add(
                $"{craftingCombatPeerFailures.Count} real crafting combat peer comparisons exceed " +
                "their approved tolerances.");
        }

        var maximumEquipmentProgressionAnalyzed =
            maximumEquipmentProgression.LoadoutsAnalyzed > 0
            && maximumEquipmentProgression.CombatPeers.Count == CraftingCombatPeerSpecs.Count
            && maximumEquipmentProgression.CombatPeers.All(x =>
                x.FirstSpentBudget > 0
                && x.SecondSpentBudget > 0
                && double.IsFinite(x.FirstUtilityPerHundredBudget)
                && double.IsFinite(x.SecondUtilityPerHundredBudget)
                && double.IsFinite(x.DifferencePercent));
        if (!maximumEquipmentProgressionAnalyzed)
        {
            blockers.Add(
                "The tier-10 Masterwork/Legacy maximum-equipment analysis is incomplete or invalid.");
        }
        var maximumEquipmentPeerFailures = maximumEquipmentProgression.CombatPeers
            .Where(x => x.IsReleaseGate && !x.Passed)
            .ToList();
        if (maximumEquipmentProgression.LoadoutsOverCap > 0)
        {
            blockers.Add(
                $"{maximumEquipmentProgression.LoadoutsOverCap} tier-10 Masterwork/Legacy " +
                "loadouts exceed an effective character cap.");
        }

        if (maximumEquipmentProgression.LoadoutsWithUnspentBudget > 0)
        {
            blockers.Add(
                $"{maximumEquipmentProgression.LoadoutsWithUnspentBudget} tier-10 " +
                "Masterwork/Legacy loadouts cannot spend their full progression budget.");
        }

        if (maximumEquipmentPeerFailures.Count > 0)
        {
            blockers.Add(
                $"{maximumEquipmentPeerFailures.Count} tier-10 Masterwork/Legacy " +
                "combat peer comparisons exceed their approved tolerances.");
        }
        var maximumEquipmentProgressionPassed =
            maximumEquipmentProgressionAnalyzed
            && maximumEquipmentProgression.LoadoutsOverCap == 0
            && maximumEquipmentProgression.LoadoutsWithUnspentBudget == 0
            && maximumEquipmentPeerFailures.Count == 0;

        return new EquipmentBalanceCalibrationGate(
            SummonCalibrationTolerancePercent,
            HandCalibrationTolerancePercent,
            AggregateCapWasteTolerancePercent,
            OverflowRedistributionActive: true,
            AggregateCapUtilizationPassed: aggregateCapFailures.Count == 0,
            CandidateAggregateCapUtilizationPassed: candidateAggregateCapFailures.Count == 0,
            EqualBudgetPeerMatrixPassed: equalBudgetFailures.Count == 0,
            SummonCalibrationPassed: summonFailures.Count == 0,
            HandCalibrationPassed: handFailures.Count == 0,
            CraftingCombatPeerMatrixPassed: craftingCombatPeerFailures.Count == 0,
            MaximumEquipmentProgressionAnalyzed: maximumEquipmentProgressionAnalyzed,
            MaximumEquipmentProgressionPassed: maximumEquipmentProgressionPassed,
            ActiveProfilePassed: blockers.Count == 0,
            blockers);
    }

    private LoadoutAllocation CreateLoadoutAllocation(int tier, EquipmentLoadoutProfile profile)
    {
        var tierBudget = _craftingBalance.GetTierPowerBudget(tier);
        var targetBudget = tierBudget * profile.SlotWeights.Sum();
        var preRedistributionAttributes = CreateReferenceAttributes(tier);
        var preRedistributionPoints = new Dictionary<AttributeType, double>();
        var overCap = new HashSet<AttributeType>();

        foreach (var slotWeight in profile.SlotWeights)
        {
            var slotBudget = tierBudget * slotWeight;
            var constraints = EquipmentConstraintProfile.CreateItemConstraints(
                preRedistributionAttributes,
                tier,
                slotWeight,
                _craftingBalance.GetMaximumCombatLoadoutBudgetWeight(),
                EquipmentConstraintProfile.MinimumSupportedBasicAttackIntervalMultiplier);
            var allocation = EquipmentBudgetAllocator.AllocateConstrained(
                tier,
                slotBudget,
                profile.BudgetShares,
                constraints,
                profile.BudgetShares,
                perItemCapMultiplier:
                    EquipmentConstraintProfile.GetPerItemCapMultiplier(slotWeight));
            foreach (var (attribute, pointDelta) in allocation.AddedPoints)
            {
                preRedistributionPoints[attribute] =
                    preRedistributionPoints.GetValueOrDefault(attribute) + pointDelta;
            }

            overCap.UnionWith(allocation.CappedAttributes);
        }

        foreach (var (attribute, pointDelta) in preRedistributionPoints)
            ApplyAttributeDelta(preRedistributionAttributes, attribute, (float)pointDelta);

        var (candidatePoints, spentBudget) = AllocateAggregateCappedPoints(
            tier,
            targetBudget,
            profile);
        var attributes = CreateReferenceAttributes(tier);
        foreach (var (attribute, pointDelta) in candidatePoints)
            ApplyAttributeDelta(attributes, attribute, (float)pointDelta);
        var aggregateRedistributedBudget = preRedistributionPoints
            .Select(entry =>
            {
                var rule = EquipmentStatBudgetCatalog.Get(entry.Key, tier);
                var removedPoints = Math.Max(
                    0d,
                    entry.Value - candidatePoints.GetValueOrDefault(entry.Key));
                return removedPoints * rule.CostPerPoint;
            })
            .Sum();

        return new LoadoutAllocation(
            tier,
            targetBudget,
            spentBudget,
            attributes,
            candidatePoints.ToDictionary(x => x.Key, x => Round(x.Value)),
            preRedistributionAttributes,
            preRedistributionPoints.ToDictionary(x => x.Key, x => Round(x.Value)),
            aggregateRedistributedBudget,
            overCap.Order().ToList());
    }

    private static (Dictionary<AttributeType, double> Points, double SpentBudget)
        AllocateAggregateCappedPoints(
            int tier,
            double targetBudget,
            EquipmentLoadoutProfile profile)
    {
        const double tolerance = 0.000001d;
        var baselineAttributes = CreateReferenceAttributes(tier);
        var points = profile.BudgetShares.Keys.ToDictionary(x => x, _ => 0d);
        var activeWeights = profile.BudgetShares
            .Where(x => x.Value > 0 && EquipmentStatBudgetCatalog.IsKnown(x.Key))
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value);
        var remainingBudget = targetBudget;
        var slotCount = profile.SlotWeights.Count;

        for (var iteration = 0;
             iteration < EquipmentStatBudgetCatalog.Attributes.Count * 2
             && remainingBudget > tolerance
             && activeWeights.Count > 0;
             iteration++)
        {
            var totalWeight = activeWeights.Values.Sum();
            if (totalWeight <= tolerance)
                break;

            var proposedPoints = activeWeights.ToDictionary(
                entry => entry.Key,
                entry =>
                    remainingBudget
                    * entry.Value
                    / totalWeight
                    / EquipmentConstraintProfile.GetCostPerPoint(
                        entry.Key,
                        tier));
            var scale = 1d;

            foreach (var (attribute, proposedPointDelta) in proposedPoints)
            {
                var aggregatePerItemCapacity =
                    EquipmentStatBudgetCatalog.Get(attribute, tier).PerItemHardCap * slotCount;
                if (proposedPointDelta > tolerance)
                {
                    scale = Math.Min(
                        scale,
                        Math.Max(0d, aggregatePerItemCapacity - points[attribute])
                        / proposedPointDelta);
                }
            }

            foreach (var cappedAttribute in EquipmentStatBudgetCatalog.Attributes.Order())
            {
                if (!AttributeCatalog.TryGetEffectiveCharacterCap(
                        cappedAttribute,
                        profile.BasicAttackIntervalMultiplier,
                        out var effectiveCap))
                {
                    continue;
                }

                var currentValue = CalculateAggregateAttributeValue(
                    cappedAttribute,
                    baselineAttributes,
                    points);
                var proposedIncrease = proposedPoints.Sum(entry =>
                    entry.Value
                    * GetDirectContribution(entry.Key, cappedAttribute));
                if (proposedIncrease <= tolerance)
                    continue;

                scale = Math.Min(
                    scale,
                    Math.Max(0d, effectiveCap - currentValue) / proposedIncrease);
            }

            scale = Math.Clamp(scale, 0d, 1d);
            var spentThisIteration = 0d;
            foreach (var (attribute, proposedPointDelta) in proposedPoints)
            {
                var addedPoints = proposedPointDelta * scale;
                points[attribute] += addedPoints;
                spentThisIteration +=
                    addedPoints
                    * EquipmentConstraintProfile.GetCostPerPoint(
                        attribute,
                        tier);
            }

            remainingBudget = Math.Max(0d, remainingBudget - spentThisIteration);
            if (scale >= 1d - tolerance)
                break;

            var blockedAttributes = new HashSet<AttributeType>();
            foreach (var attribute in activeWeights.Keys)
            {
                var aggregatePerItemCapacity =
                    EquipmentStatBudgetCatalog.Get(attribute, tier).PerItemHardCap * slotCount;
                if (points[attribute] >= aggregatePerItemCapacity - tolerance)
                    blockedAttributes.Add(attribute);
            }

            foreach (var cappedAttribute in EquipmentStatBudgetCatalog.Attributes.Order())
            {
                if (!AttributeCatalog.TryGetEffectiveCharacterCap(
                        cappedAttribute,
                        profile.BasicAttackIntervalMultiplier,
                        out var effectiveCap)
                    || CalculateAggregateAttributeValue(
                        cappedAttribute,
                        baselineAttributes,
                        points) < effectiveCap - tolerance)
                {
                    continue;
                }

                foreach (var attribute in activeWeights.Keys.Where(attribute =>
                             GetDirectContribution(attribute, cappedAttribute) > 0))
                {
                    blockedAttributes.Add(attribute);
                }
            }

            if (blockedAttributes.Count == 0)
                break;

            foreach (var attribute in blockedAttributes)
                activeWeights.Remove(attribute);
        }

        return (points, targetBudget - remainingBudget);
    }

    private static double CalculateAggregateAttributeValue(
        AttributeType attribute,
        IReadOnlyDictionary<AttributeType, float> baselineAttributes,
        IReadOnlyDictionary<AttributeType, double> equipmentPoints) =>
        baselineAttributes.GetValueOrDefault(attribute)
        + equipmentPoints.Sum(entry =>
            entry.Value * GetDirectContribution(entry.Key, attribute));

    private static double GetDirectContribution(
        AttributeType source,
        AttributeType target) =>
        source == target ? 1d : 0d;

    private static IReadOnlyList<EquipmentAggregateCapMeasurement> CreateAggregateCapMeasurements(
        LoadoutAllocation allocation,
        EquipmentLoadoutProfile profile,
        bool beforeRedistribution)
    {
        var baselineAttributes = CreateReferenceAttributes(allocation.Tier);
        var attributes = beforeRedistribution
            ? allocation.PreRedistributionAttributes
            : allocation.Attributes;
        var points = beforeRedistribution
            ? allocation.PreRedistributionPoints
            : allocation.Points;
        var measurements = new List<EquipmentAggregateCapMeasurement>();
        foreach (var attribute in EquipmentStatBudgetCatalog.Attributes.Order())
        {
            if (!AttributeCatalog.TryGetEffectiveCharacterCap(
                    attribute,
                    profile.BasicAttackIntervalMultiplier,
                    out var effectiveCap))
            {
                continue;
            }

            var baselineValue = baselineAttributes.GetValueOrDefault(attribute);
            var directEquipmentPoints = points.GetValueOrDefault(attribute);
            var totalValue = attributes.GetValueOrDefault(attribute);
            var excessPoints = Math.Max(0d, totalValue - effectiveCap);
            var directEquipmentExcessPoints = Math.Min(directEquipmentPoints, excessPoints);
            var equivalentWastedBudget =
                directEquipmentExcessPoints
                * EquipmentStatBudgetCatalog.Get(attribute, allocation.Tier).CostPerPoint;
            var wastedTargetBudgetPercent = allocation.TargetBudget <= 0
                ? 0
                : equivalentWastedBudget / allocation.TargetBudget * 100d;
            measurements.Add(new EquipmentAggregateCapMeasurement(
                attribute,
                Round(effectiveCap),
                Round(baselineValue),
                Round(directEquipmentPoints),
                Round(totalValue),
                Round(Math.Min(totalValue, effectiveCap)),
                Round(excessPoints),
                Round(directEquipmentExcessPoints),
                Round(equivalentWastedBudget),
                Round(wastedTargetBudgetPercent)));
        }

        return measurements;
    }

    private static IReadOnlyList<EquipmentLoadoutAllocationRecommendation>
        CreateAllocationRecommendations(LoadoutAllocation allocation)
    {
        var attributes = allocation.PreRedistributionPoints.Keys
            .Union(allocation.Points.Keys)
            .Order()
            .ToList();
        return attributes.Select(attribute =>
        {
            var rule = EquipmentStatBudgetCatalog.Get(attribute, allocation.Tier);
            var currentBudget =
                allocation.PreRedistributionPoints.GetValueOrDefault(attribute)
                * rule.CostPerPoint;
            var candidateBudget =
                allocation.Points.GetValueOrDefault(attribute)
                * EquipmentConstraintProfile.GetCostPerPoint(
                    attribute,
                    allocation.Tier);
            return new EquipmentLoadoutAllocationRecommendation(
                attribute,
                allocation.TargetBudget <= 0
                    ? 0
                    : Round(currentBudget / allocation.TargetBudget * 100d),
                allocation.TargetBudget <= 0
                    ? 0
                    : Round(candidateBudget / allocation.TargetBudget * 100d),
                Round(
                    allocation.Points.GetValueOrDefault(attribute)
                    - allocation.PreRedistributionPoints.GetValueOrDefault(attribute)),
                Round(candidateBudget - currentBudget));
        }).ToList();
    }

    private static double CalculateNominalSummonLifetimeDamage(double ownerPower)
    {
        var summonPower =
            NominalSummonPowerBase + ownerPower * NominalSummonPowerCoefficient;
        var strikeUses = NominalSummonDurationTicks / NominalSummonStrikeCooldownTicks;
        var basicAttackUses =
            NominalSummonDurationTicks / NominalSummonBasicAttackIntervalTicks;
        return strikeUses
               * (NominalSummonStrikeBase
                  + summonPower * NominalSummonStrikePowerCoefficient)
               + basicAttackUses
               * (1 + summonPower * AttributeCombatRules.BasicAttackPowerCoefficient);
    }

    private static double CalculateNominalDirectControlDamage(double ownerPower) =>
        CalculateNominalSummonLifetimeDamage(0)
        + ownerPower
        * (CalculateNominalSummonLifetimeDamage(1)
           - CalculateNominalSummonLifetimeDamage(0));

    private static IReadOnlyList<EquipmentLoadoutComparison> CreateLoadoutComparisons(
        IReadOnlyList<EquipmentLoadoutMeasurement> loadouts)
    {
        var comparisons = new List<EquipmentLoadoutComparison>();
        foreach (var tier in ReferenceTiers)
        {
            comparisons.Add(CompareLoadouts(
                loadouts,
                tier,
                AttributeBalanceScenario.PhysicalOffense,
                EquipmentLoadoutComparisonPurpose.PeerBalance,
                "medium-dual-wield",
                "two-handed-damage"));
            comparisons.Add(CompareLoadouts(
                loadouts,
                tier,
                AttributeBalanceScenario.MagicalOffense,
                EquipmentLoadoutComparisonPurpose.OutputDecomposition,
                "cloth-support",
                "summoner"));
        }

        return comparisons;
    }

    private static EquipmentLoadoutComparison CompareLoadouts(
        IReadOnlyList<EquipmentLoadoutMeasurement> loadouts,
        int tier,
        AttributeBalanceScenario scenario,
        EquipmentLoadoutComparisonPurpose purpose,
        string firstLoadoutId,
        string secondLoadoutId)
    {
        var first = loadouts.Single(x => x.Tier == tier && x.Id == firstLoadoutId);
        var second = loadouts.Single(x => x.Tier == tier && x.Id == secondLoadoutId);
        var firstScore = first.Scenarios.Single(x => x.Scenario == scenario).MeanScore;
        var secondScore = second.Scenarios.Single(x => x.Scenario == scenario).MeanScore;
        var midpoint = (firstScore + secondScore) / 2d;
        var difference = midpoint <= 0 ? 0 : (firstScore - secondScore) / midpoint * 100d;

        return new EquipmentLoadoutComparison(
            tier,
            scenario,
            purpose,
            firstLoadoutId,
            secondLoadoutId,
            firstScore,
            secondScore,
            Round(difference),
            first.Scenarios.Single(x => x.Scenario == scenario).Output,
            second.Scenarios.Single(x => x.Scenario == scenario).Output);
    }

    private IReadOnlyList<EqualBudgetAttributeComparison> CreateEqualBudgetComparisons(
        IDictionary<(int Tier, AttributeBalanceScenario Scenario), ScenarioSample> baselineCache,
        CancellationToken cancellationToken)
    {
        var comparisons = new List<EqualBudgetAttributeComparison>();
        foreach (var tier in ReferenceTiers)
        {
            foreach (var spec in EqualBudgetPeerSpecs)
            {
                comparisons.Add(Compare(
                    tier,
                    spec,
                    _craftingBalance.GetTierPowerBudget(tier) * spec.BudgetFraction,
                    baselineCache,
                    cancellationToken));
            }
        }

        return comparisons;
    }

    private EqualBudgetAttributeComparison Compare(
        int tier,
        EqualBudgetPeerSpec spec,
        double budget,
        IDictionary<(int Tier, AttributeBalanceScenario Scenario), ScenarioSample> baselineCache,
        CancellationToken cancellationToken)
    {
        ScenarioSample baseline;
        if (spec.BenchmarkContext is null)
        {
            if (baselineCache.TryGetValue((tier, spec.Scenario), out var cachedBaseline))
            {
                baseline = cachedBaseline;
            }
            else
            {
                baseline = MeasureScenario(tier, spec.Scenario, null, 0, cancellationToken);
                baselineCache.Add((tier, spec.Scenario), baseline);
            }
        }
        else
        {
            baseline = MeasureScenario(
                tier,
                spec.Scenario,
                new Dictionary<AttributeType, double>(),
                cancellationToken,
                spec.BenchmarkContext);
        }

        var referenceAttributes = CreateReferenceAttributes(tier);
        if (spec.BenchmarkContext is not null)
        {
            foreach (var (attribute, value) in spec.BenchmarkContext.ReferenceAttributeOverrides)
                referenceAttributes[attribute] = (float)value;
        }

        var firstDelta = CalculateAffordablePointDelta(
            tier,
            spec.FirstAttribute,
            budget,
            referenceAttributes.GetValueOrDefault(spec.FirstAttribute));
        IReadOnlyDictionary<AttributeType, double> firstPointDeltas =
            new Dictionary<AttributeType, double>
            {
                [spec.FirstAttribute] = firstDelta
            };
        var secondAttribute = spec.SecondAttribute
            ?? throw new InvalidOperationException(
                $"Peer comparison '{spec.Id}' has no second investment.");
        IReadOnlyDictionary<AttributeType, double> secondPointDeltas =
            new Dictionary<AttributeType, double>
        {
            [secondAttribute] = CalculateAffordablePointDelta(
                tier,
                secondAttribute,
                budget,
                referenceAttributes.GetValueOrDefault(secondAttribute))
        };

        var firstScore = MeasureScenario(
            tier,
            spec.Scenario,
            firstPointDeltas,
            cancellationToken,
            spec.BenchmarkContext);
        var secondScore = MeasureScenario(
            tier,
            spec.Scenario,
            secondPointDeltas,
            cancellationToken,
            spec.BenchmarkContext);
        var firstGain = baseline.Scores
            .Zip(
                firstScore.Scores,
                (baselineScore, modifiedScore) => CalculateRelativeGain(baselineScore, modifiedScore))
            .Average();
        var secondGain = baseline.Scores
            .Zip(
                secondScore.Scores,
                (baselineScore, modifiedScore) => CalculateRelativeGain(baselineScore, modifiedScore))
            .Average();
        var difference = firstGain - secondGain;

        return new EqualBudgetAttributeComparison(
            spec.Id,
            spec.Group,
            spec.Intent,
            tier,
            spec.Scenario,
            spec.BenchmarkContext?.Label ?? "reference baseline",
            spec.IsReleaseGate,
            spec.FirstAttribute.ToString(),
            secondAttribute.ToString(),
            spec.FirstAttribute,
            spec.SecondAttribute,
            Round(budget),
            spec.TolerancePercentagePoints,
            Round(firstGain),
            Round(secondGain),
            Round(difference),
            Math.Abs(difference) <= spec.TolerancePercentagePoints,
            baseline.Output,
            firstScore.Output,
            secondScore.Output);
    }

    private static EqualBudgetPeerSpec StrictPeer(
        string id,
        AttributePeerComparisonGroup group,
        AttributeBalanceScenario scenario,
        AttributeType first,
        AttributeType second,
        double budgetFraction = 0.02d) =>
        new(
            id,
            group,
            AttributePeerComparisonIntent.StrictPeer,
            scenario,
            first,
            second,
            null,
            true,
            budgetFraction,
            StrictPeerTolerancePercentagePoints);

    private static EqualBudgetPeerSpec GeneralistPeer(
        string id,
        AttributePeerComparisonGroup group,
        AttributeBalanceScenario scenario,
        AttributeType first,
        AttributeType second,
        double budgetFraction = 0.02d,
        bool isReleaseGate = true) =>
        new(
            id,
            group,
            AttributePeerComparisonIntent.GeneralistVersusSpecialist,
            scenario,
            first,
            second,
            null,
            isReleaseGate,
            budgetFraction,
            GeneralistPeerTolerancePercentagePoints);

    private static EqualBudgetPeerSpec ContextPeer(
        string id,
        AttributePeerComparisonGroup group,
        AttributeBalanceScenario scenario,
        AttributeType first,
        AttributeType second,
        EqualBudgetBenchmarkContext context,
        AttributePeerComparisonIntent intent,
        double tolerancePercentagePoints,
        bool isReleaseGate = true,
        double budgetFraction = 0.02d) =>
        new(
            id,
            group,
            intent,
            scenario,
            first,
            second,
            context,
            isReleaseGate,
            budgetFraction,
            tolerancePercentagePoints);

    private static CraftingCombatPeerSpec CraftingPeer(
        string id,
        CraftingCombatPeerGroup group,
        AttributeBalanceScenario scenario,
        string firstArmorFamily,
        string firstHandConfiguration,
        string? firstBlueprintId,
        string secondArmorFamily,
        string secondHandConfiguration,
        string? secondBlueprintId,
        double tolerancePercent = CraftingCombatPeerTolerancePercent,
        bool isReleaseGate = true) =>
        new(
            id,
            group,
            scenario,
            firstArmorFamily,
            firstHandConfiguration,
            firstBlueprintId,
            secondArmorFamily,
            secondHandConfiguration,
            secondBlueprintId,
            tolerancePercent,
            isReleaseGate);

    private static double CalculateAffordablePointDelta(
        int tier,
        AttributeType attribute,
        double budget,
        double? baselineOverride = null)
    {
        var rule = EquipmentStatBudgetCatalog.Get(attribute, tier);
        var baseline = baselineOverride
            ?? CreateReferenceAttributes(tier).GetValueOrDefault(attribute);
        return Math.Max(
            0,
            Math.Min(
                budget / EquipmentConstraintProfile.GetCostPerPoint(attribute, tier),
                rule.PerItemHardCap - baseline));
    }

    private static List<AttributeMarginalValueMeasurement> AddSuggestedCosts(
        IReadOnlyList<AttributeMarginalValueMeasurement> measurements)
    {
        var result = new List<AttributeMarginalValueMeasurement>(measurements.Count);
        foreach (var tierGroup in measurements.GroupBy(x => x.Tier))
        {
            var targetEfficiency = Median(tierGroup
                .Where(x => x.PointDelta > 0 && x.MedianRelativeGainPercent > InertThresholdPercent)
                .Select(x => x.MedianRelativeGainPercent / x.PointDelta / x.CurrentCostPerPoint));

            foreach (var measurement in tierGroup)
            {
                double? suggestedCost = null;
                if (targetEfficiency > 0
                    && measurement.PointDelta > 0
                    && measurement.MedianRelativeGainPercent > InertThresholdPercent)
                {
                    var pointUtility = measurement.MedianRelativeGainPercent / measurement.PointDelta;
                    suggestedCost = Round(Math.Clamp(pointUtility / targetEfficiency, 0.01d, 100d));
                }

                result.Add(measurement with { SuggestedCostPerPoint = suggestedCost });
            }
        }

        return result.OrderBy(x => x.Tier).ThenBy(x => x.Attribute).ToList();
    }

    private static IReadOnlyList<AttributeBalanceFinding> CreateFindings(
        IReadOnlyList<AttributeMarginalValueMeasurement> measurements,
        IReadOnlyList<EqualBudgetAttributeComparison> comparisons,
        IReadOnlyList<EquipmentLoadoutMeasurement> loadouts,
        IReadOnlyList<EquipmentLoadoutComparison> loadoutComparisons,
        IReadOnlyList<SummonCalibrationComparison> summonCalibrations,
        IReadOnlyList<HandCalibrationComparison> handCalibrations,
        IReadOnlyList<CraftingCombatPeerComparison> craftingCombatPeers,
        MaximumEquipmentProgressionReport maximumEquipmentProgression,
        EquipmentBalanceCalibrationGate calibrationGate)
    {
        var findings = new List<AttributeBalanceFinding>();
        foreach (var measurement in measurements)
        {
            if (measurement.Scenarios.All(x => Math.Abs(x.RelativeGainPercent) <= InertThresholdPercent))
            {
                findings.Add(new AttributeBalanceFinding(
                    AttributeBalanceFindingKind.Inert,
                    measurement.Tier,
                    measurement.Attribute,
                    $"{measurement.Attribute} produced only {measurement.MedianRelativeGainPercent:0.##}% median gain in its relevant scenarios."));
            }

            if (measurement.CapLimited)
            {
                findings.Add(new AttributeBalanceFinding(
                    AttributeBalanceFindingKind.CapLimited,
                    measurement.Tier,
                    measurement.Attribute,
                    $"{measurement.Attribute} could not spend the full marginal budget before its hard cap."));
            }

            if (measurement.SuggestedCostPerPoint is not { } suggested)
                continue;

            if (suggested < measurement.CurrentCostPerPoint * (1 - CostWarningThreshold))
            {
                findings.Add(new AttributeBalanceFinding(
                    AttributeBalanceFindingKind.Overvalued,
                    measurement.Tier,
                    measurement.Attribute,
                    $"{measurement.Attribute} costs {measurement.CurrentCostPerPoint:0.##} per point; the measured candidate is {suggested:0.##}."));
            }
            else if (suggested > measurement.CurrentCostPerPoint * (1 + CostWarningThreshold))
            {
                findings.Add(new AttributeBalanceFinding(
                    AttributeBalanceFindingKind.Undervalued,
                    measurement.Tier,
                    measurement.Attribute,
                    $"{measurement.Attribute} costs {measurement.CurrentCostPerPoint:0.##} per point; the measured candidate is {suggested:0.##}."));
            }
        }

        foreach (var comparison in comparisons.Where(x => !x.Passed))
        {
            findings.Add(new AttributeBalanceFinding(
                AttributeBalanceFindingKind.EqualBudgetMismatch,
                comparison.Tier,
                null,
                $"{comparison.Id}: {comparison.FirstLabel} and {comparison.SecondLabel} differ by " +
                $"{Math.Abs(comparison.DifferencePercentagePoints):0.##} percentage points in " +
                $"{comparison.Scenario} at equal budget; tolerance is " +
                $"{comparison.TolerancePercentagePoints:0.##}."));
        }

        foreach (var loadout in loadouts.Where(x =>
                     x.AttributesOverSingleStatCap.Count > 0
                     && x.UnspentBudget > 0.01d))
        {
            findings.Add(new AttributeBalanceFinding(
                AttributeBalanceFindingKind.LoadoutCapPressure,
                loadout.Tier,
                null,
                $"{loadout.Name} leaves budget unspent because a per-item hard cap is reached for: " +
                $"{string.Join(", ", loadout.AttributesOverSingleStatCap)}."));
        }

        foreach (var loadout in loadouts)
        {
            foreach (var cap in loadout.AggregateCapsBeforeRedistribution.Where(x =>
                         x.WastedTargetBudgetPercent > AggregateCapWasteTolerancePercent))
            {
                findings.Add(new AttributeBalanceFinding(
                    AttributeBalanceFindingKind.AggregateCapWaste,
                    loadout.Tier,
                    cap.Attribute,
                    $"{loadout.Name} has {cap.ExcessPoints:0.##} {cap.Attribute} points above " +
                    $"its effective character cap, including {cap.DirectEquipmentExcessPoints:0.##} " +
                    $"direct equipment points worth {cap.WastedTargetBudgetPercent:0.##}% of target budget."));
            }
        }

        foreach (var comparison in loadoutComparisons.Where(x =>
                     x.Purpose == EquipmentLoadoutComparisonPurpose.PeerBalance
                     && Math.Abs(x.DifferencePercent) > LoadoutWarningThresholdPercent))
        {
            findings.Add(new AttributeBalanceFinding(
                AttributeBalanceFindingKind.LoadoutMismatch,
                comparison.Tier,
                null,
                $"{comparison.FirstLoadoutId} and {comparison.SecondLoadoutId} differ by " +
                $"{Math.Abs(comparison.DifferencePercent):0.##}% in {comparison.Scenario}."));
        }

        foreach (var comparison in summonCalibrations.Where(x =>
                     Math.Abs(x.EqualBudgetDifferencePercent) > SummonCalibrationTolerancePercent))
        {
            findings.Add(new AttributeBalanceFinding(
                AttributeBalanceFindingKind.SummonCalibrationMismatch,
                comparison.Tier,
                null,
                $"Summoner and direct-caster damage efficiency differ by " +
                $"{Math.Abs(comparison.EqualBudgetDifferencePercent):0.##}% over " +
                $"{comparison.DurationTicks} ticks."));
        }

        foreach (var comparison in handCalibrations.Where(x =>
                     x.Mode == HandCalibrationMode.RepresentativeFundingAndBehavior
                     && Math.Abs(x.DifferencePercent) > HandCalibrationTolerancePercent))
        {
            findings.Add(new AttributeBalanceFinding(
                AttributeBalanceFindingKind.HandCalibrationMismatch,
                comparison.Tier,
                null,
                $"Dual Wield and Two-Handed damage efficiency differ by " +
                $"{Math.Abs(comparison.DifferencePercent):0.##}% over " +
                $"{comparison.DurationTicks} ticks."));
        }

        foreach (var comparison in craftingCombatPeers.Where(x => !x.Passed))
        {
            findings.Add(new AttributeBalanceFinding(
                AttributeBalanceFindingKind.CraftingCombatPeerMismatch,
                comparison.Tier,
                null,
                $"{comparison.Id}: {comparison.FirstDesignId} and " +
                $"{comparison.SecondDesignId} differ by " +
                $"{Math.Abs(comparison.DifferencePercent):0.##}% in " +
                $"{comparison.Scenario}; tolerance is {comparison.TolerancePercent:0.##}%."));
        }

        foreach (var comparison in maximumEquipmentProgression.CombatPeers.Where(x => !x.Passed))
        {
            findings.Add(new AttributeBalanceFinding(
                AttributeBalanceFindingKind.MaximumProgressionMismatch,
                maximumEquipmentProgression.Tier,
                null,
                $"{comparison.Id} at {maximumEquipmentProgression.Quality}/" +
                $"{maximumEquipmentProgression.Rarity}: {comparison.FirstDesignId} and " +
                $"{comparison.SecondDesignId} differ by " +
                $"{Math.Abs(comparison.DifferencePercent):0.##}% in " +
                $"{comparison.Scenario}; tolerance is {comparison.TolerancePercent:0.##}%."));
        }

        if (!calibrationGate.ActiveProfilePassed)
        {
            findings.Add(new AttributeBalanceFinding(
                AttributeBalanceFindingKind.BalanceVersionBlocked,
                0,
                null,
                $"Equipment balance version {EquipmentBudgetEvaluator.BalanceVersion} is blocked: " +
                string.Join(" ", calibrationGate.Blockers)));
        }

        return findings;
    }

    private static double CalculateRelativeGain(double baseline, double modified) =>
        baseline <= 0 ? 0 : (modified - baseline) / baseline * 100d;

    private static (double Low, double High) CalculateConfidenceInterval(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return (0, 0);

        var mean = values.Average();
        if (values.Count == 1)
            return (mean, mean);

        var variance = values.Sum(value => Math.Pow(value - mean, 2)) / (values.Count - 1);
        var margin = 1.96d * Math.Sqrt(variance / values.Count);
        return (mean - margin, mean + margin);
    }

    private static double Median(IEnumerable<double> source)
    {
        var values = source.Order().ToArray();
        if (values.Length == 0)
            return 0;

        var middle = values.Length / 2;
        return values.Length % 2 == 0
            ? (values[middle - 1] + values[middle]) / 2d
            : values[middle];
    }

    private static double Round(double value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private sealed record EqualBudgetPeerSpec(
        string Id,
        AttributePeerComparisonGroup Group,
        AttributePeerComparisonIntent Intent,
        AttributeBalanceScenario Scenario,
        AttributeType FirstAttribute,
        AttributeType? SecondAttribute,
        EqualBudgetBenchmarkContext? BenchmarkContext,
        bool IsReleaseGate,
        double BudgetFraction,
        double TolerancePercentagePoints);

    private sealed record EqualBudgetBenchmarkContext(
        string Label,
        IReadOnlyDictionary<AttributeType, double> ReferenceAttributeOverrides,
        double OpponentDefenseMultiplier,
        IReadOnlyList<string>? FriendlyAbilityIds = null,
        int? MaxTicksOverride = null,
        double BasicAttackIntervalMultiplier = 1d,
        double BasicAttackDamageMultiplier = 1d);

    private sealed record CraftingCombatPeerSpec(
        string Id,
        CraftingCombatPeerGroup Group,
        AttributeBalanceScenario Scenario,
        string FirstArmorFamily,
        string FirstHandConfiguration,
        string? FirstBlueprintId,
        string SecondArmorFamily,
        string SecondHandConfiguration,
        string? SecondBlueprintId,
        double TolerancePercent,
        bool IsReleaseGate);

    private sealed record ScenarioSample(
        double Mean,
        IReadOnlyList<double> Scores,
        EquipmentLoadoutOutput Output);

    private sealed record ScenarioOutcome(
        EquipmentLoadoutOutput Output,
        EquipmentLoadoutUtilityBreakdown Utility);

    private sealed record SummonActivity(
        double SummonsCreated,
        double AverageActiveSummons,
        double UptimePercent);

    private sealed record LoadoutScenarioSample(IReadOnlyList<ScenarioOutcome> Outcomes)
    {
        public double MeanUtility => Outcomes.Average(x => x.Utility.Total);
    }

    private sealed record EquipmentLoadoutProfile(
        string Id,
        string Name,
        IReadOnlyList<double> SlotWeights,
        IReadOnlyDictionary<AttributeType, double> BudgetShares,
        IReadOnlyList<string> AbilityIds,
        IReadOnlyList<AttributeBalanceScenario> RelevantScenarios,
        double BasicAttackIntervalMultiplier,
        double BasicAttackDamageMultiplier,
        AttackType BasicAttackType,
        DamageType BasicAttackDamageType);

    private sealed record LoadoutAllocation(
        int Tier,
        double TargetBudget,
        double SpentBudget,
        Dictionary<AttributeType, float> Attributes,
        IReadOnlyDictionary<AttributeType, double> Points,
        Dictionary<AttributeType, float> PreRedistributionAttributes,
        IReadOnlyDictionary<AttributeType, double> PreRedistributionPoints,
        double AggregateRedistributedBudget,
        IReadOnlyList<AttributeType> AttributesOverSingleStatCap);

    private sealed record LoadoutAnalysisWork(
        int Tier,
        EquipmentLoadoutProfile Profile,
        LoadoutAllocation Allocation,
        IReadOnlyDictionary<AttributeBalanceScenario, LoadoutScenarioSample> Samples);

    private sealed record CatalogHandConfiguration(
        string Id,
        IReadOnlyList<CraftingRecipeDefinition> Recipes,
        string MainHandRecipeId);

    private sealed record CatalogLoadoutTemplate(
        string ArmorFamily,
        string HandConfiguration,
        IReadOnlyList<CraftingRecipeDefinition> Recipes,
        string MainHandRecipeId,
        BlueprintDefinition? Blueprint);

    private sealed record CatalogCombatAllocation(
        string Id,
        double SpentBudget,
        IReadOnlyDictionary<AttributeType, double> Points,
        IReadOnlyDictionary<AttributeType, float> Attributes,
        double BasicAttackIntervalMultiplier,
        double BasicAttackDamageMultiplier,
        AttackType BasicAttackType,
        DamageType BasicAttackDamageType);

    private sealed record MaximumCatalogAllocation(
        string ArmorFamily,
        string HandConfiguration,
        string? BlueprintId,
        double TargetBudget,
        double StaticBaseModifierBudget,
        double GeneratedStatBudget,
        double RarityImprovementBudget,
        double UnspentBudget,
        IReadOnlyList<MaximumItemProgressionDiagnostic> ItemDiagnostics,
        CatalogCombatAllocation Combat);

    private sealed record MaximumItemProgressionDiagnostic(
        string RecipeId,
        EquipmentType EquipmentType,
        string? BlueprintId,
        double GeneratedUnspentBudget,
        double RarityUnspentBudget,
        IReadOnlyList<AttributeType> CappedAttributes,
        IReadOnlyList<AttributeType> BindingCombatCaps);

    private sealed record CatalogCapResult(
        AttributeType Attribute,
        double ExcessPoints,
        double WastedBudgetPercent);

    private sealed record CatalogShadowWork(
        string Id,
        int Tier,
        string ArmorFamily,
        string HandConfiguration,
        string? BlueprintId,
        double TargetBudget,
        double CurrentSpentBudget,
        double ShadowSpentBudget,
        double CurrentUnspentBudget,
        double ShadowUnspentBudget,
        IReadOnlyList<CatalogCapResult> CurrentCaps,
        IReadOnlyList<CatalogCapResult> ShadowCaps)
    {
        public double CurrentMaximumWastedBudgetPercent =>
            CurrentCaps.Select(x => x.WastedBudgetPercent).DefaultIfEmpty(0d).Max();

        public double ShadowMaximumWastedBudgetPercent =>
            ShadowCaps.Select(x => x.WastedBudgetPercent).DefaultIfEmpty(0d).Max();

        public IReadOnlyList<AttributeType> CurrentAttributesOverCap =>
            CurrentCaps.Where(x => x.ExcessPoints > 0.001d).Select(x => x.Attribute).ToList();

        public IReadOnlyList<AttributeType> ShadowAttributesOverCap =>
            ShadowCaps.Where(x => x.ExcessPoints > 0.001d).Select(x => x.Attribute).ToList();
    }

    private static IReadOnlyList<AbilitySpec> CreateAbilitySpecs() =>
    [
        CreateDamageAbility("balance.physical-strike", DamageType.Physical, AttackType.Melee),
        CreateDamageAbility("balance.magical-strike", DamageType.Magical, AttackType.None),
        CreateDamageAbility(
            "balance.summon-strike",
            DamageType.Physical,
            AttackType.Melee,
            NominalSummonStrikeBase,
            NominalSummonStrikePowerCoefficient,
            NominalSummonStrikeCooldownTicks),
        CreateDirectControlAbility(),
        CreatePressureAbility(
            "balance.mixed-physical-pressure",
            DamageType.Physical,
            baseValue: 10,
            powerCoefficient: 0.6f,
            cooldownTicks: 40),
        CreatePressureAbility(
            "balance.mixed-magical-pressure",
            DamageType.Magical,
            baseValue: 10,
            powerCoefficient: 0.6f,
            cooldownTicks: 40),
        CreatePressureAbility(
            "balance.unmitigated-pressure",
            DamageType.None,
            baseValue: 12,
            powerCoefficient: 0.7f,
            cooldownTicks: 20),
        CreatePressureAbility(
            "balance.burst-pressure",
            DamageType.Physical,
            baseValue: 40,
            powerCoefficient: 1.5f,
            cooldownTicks: 70),
        new AbilitySpec
        {
            Id = "balance.area-pressure",
            Name = "Balance Area Pressure",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 20,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "balance.area-pressure.effect",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.SummonedEnemies,
                    BaseValue = 12,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.5f,
                    AttackType = AttackType.None,
                    DamageType = DamageType.Physical,
                    CritEligibility = CritEligibility.Disallowed
                }
            ]
        },
        new AbilitySpec
        {
            Id = "balance.self-barrier",
            Name = "Balance Self Barrier",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 45,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "balance.effect.barrier",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 8,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.4f
                }
            ]
        },
        new AbilitySpec
        {
            Id = "balance.periodic-strike",
            Name = "Balance Periodic Strike",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 36,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "balance.effect.periodic",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 3,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.18f,
                    DurationTicks = 24,
                    IntervalTicks = 6,
                    Uses = 4,
                    AttackType = AttackType.DamageOverTime,
                    DamageType = DamageType.Burn
                }
            ]
        },
        new AbilitySpec
        {
            Id = "balance.self-heal",
            Name = "Balance Self Heal",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 35,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "balance.effect.heal",
                    Operation = AbilityEffectOperation.Heal,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 6,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.45f
                }
            ]
        },
        new AbilitySpec
        {
            Id = "balance.summon",
            Name = "Balance Summon",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = NominalRoleAbilityCooldownTicks,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "balance.effect.summon",
                    Operation = AbilityEffectOperation.Summon,
                    Target = AbilityTargetSelector.Self,
                    SummonId = "balance.summon.unit",
                    DurationTicks = NominalSummonDurationTicks
                }
            ]
        },
        CreateStatusAbility("balance.apply-weaken", "balance.status.weaken", 45),
        CreateStatusAbility("balance.apply-stun", "balance.status.stun", 30)
    ];

    private static AbilitySpec CreateDamageAbility(
        string id,
        DamageType damageType,
        AttackType attackType,
        int baseValue = 8,
        float powerCoefficient = 0.55f,
        int cooldownTicks = 24) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = AbilitySpecKind.Active,
            CooldownTicks = cooldownTicks,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = $"{id}.effect",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = baseValue,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = powerCoefficient,
                    AttackType = attackType,
                    DamageType = damageType
                }
            ]
        };

    private static AbilitySpec CreateDirectControlAbility()
    {
        var baseValue = CalculateNominalDirectControlDamage(0);
        var powerCoefficient =
            CalculateNominalDirectControlDamage(1) - baseValue;
        return new AbilitySpec
        {
            Id = "balance.direct-control-burst",
            Name = "Balance Direct Control Burst",
            Kind = AbilitySpecKind.Active,
            CooldownTicks = NominalRoleAbilityCooldownTicks,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "balance.direct-control-burst.effect",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = (int)Math.Round(baseValue),
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = (float)powerCoefficient,
                    AttackType = AttackType.None,
                    DamageType = DamageType.Magical
                }
            ]
        };
    }

    private static AbilitySpec CreatePressureAbility(
        string id,
        DamageType damageType,
        int baseValue,
        float powerCoefficient,
        int cooldownTicks) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = AbilitySpecKind.Active,
            CooldownTicks = cooldownTicks,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = $"{id}.effect",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = baseValue,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = powerCoefficient,
                    AttackType = AttackType.None,
                    DamageType = damageType,
                    CritEligibility = CritEligibility.Disallowed
                }
            ]
        };

    private static AbilitySpec CreateStatusAbility(string id, string statusId, int cooldownTicks) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = AbilitySpecKind.Active,
            CooldownTicks = cooldownTicks,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = $"{id}.effect",
                    Operation = AbilityEffectOperation.ApplyStatus,
                    Target = AbilityTargetSelector.CurrentTarget,
                    StatusId = statusId,
                    BaseValue = 1
                }
            ]
        };

    private static IReadOnlyList<StatusSpec> CreateStatusSpecs() =>
    [
        new StatusSpec
        {
            Id = "balance.status.weaken",
            Name = "Balance Weaken",
            StackingPolicy = AbilityStatusStackingPolicy.Refresh,
            MaxStacks = 1,
            DurationTicks = 36,
            Tags = ["Status.Debuff"],
            Triggers =
            [
                new AbilityTriggerSpec
                {
                    Event = AbilityTriggerEvent.OnStatusApplied,
                    EffectIds = ["balance.status.weaken.effect"]
                }
            ],
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "balance.status.weaken.effect",
                    Operation = AbilityEffectOperation.ModifyAttribute,
                    Target = AbilityTargetSelector.EventTarget,
                    Attribute = AttributeType.DamageReduction,
                    BaseValue = -20,
                    DurationTicks = 36
                }
            ]
        },
        new StatusSpec
        {
            Id = "balance.status.stun",
            Name = "Balance Stun",
            StackingPolicy = AbilityStatusStackingPolicy.Refresh,
            MaxStacks = 1,
            DurationTicks = 20,
            Tags = ["Control.Stun"]
        }
    ];

    private static IReadOnlyList<SummonSpec> CreateSummonSpecs() =>
    [
        new SummonSpec
        {
            Id = "balance.summon.unit",
            Name = "Balance Summon",
            DurationTicks = NominalSummonDurationTicks,
            MaxActive = 2,
            AbilityIds = ["balance.summon-strike"],
            Attributes =
            [
                new SummonAttributeSpec
                {
                    Attribute = AttributeType.MaxHealth,
                    BaseValue = 50,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.4f,
                    MinimumValue = 1
                },
                new SummonAttributeSpec
                {
                    Attribute = AttributeType.Power,
                    BaseValue = NominalSummonPowerBase,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = NominalSummonPowerCoefficient
                }
            ]
        }
    ];
}
