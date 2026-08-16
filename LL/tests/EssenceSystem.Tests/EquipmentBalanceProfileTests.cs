using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Professions.Crafting.V2;
using Domain.Models.Snapshots;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Engine;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class EquipmentBalanceProfileTests
{
    [Fact]
    public void V17_scaling_kinds_match_the_canonical_unit_contract()
    {
        Assert.Equal(17, EquipmentStatBudgetCatalog.BalanceVersion);
        Assert.Equal(16, EquipmentStatBudgetCatalog.PreviousBalanceVersion);

        var ratings = EquipmentStatBudgetCatalog.Attributes
            .Where(EquipmentStatBudgetCatalog.IsRating)
            .Order()
            .ToArray();
        Assert.Equal(new[] { AttributeType.Armor, AttributeType.Resistance }, ratings);

        var anchors = EquipmentStatBudgetCatalog.Attributes
            .Where(EquipmentStatBudgetCatalog.IsTierAnchor)
            .Order()
            .ToArray();
        Assert.Equal(
            new[]
            {
                AttributeType.Power,
                AttributeType.MaxHealth,
                AttributeType.Armor,
                AttributeType.Resistance,
                AttributeType.HealthRegeneration
            }.Order(),
            anchors);

        Assert.All(
            EquipmentStatBudgetCatalog.Attributes.Except(anchors),
            attribute => Assert.True(EquipmentStatBudgetCatalog.IsDirectPercentage(attribute)));
        Assert.Throws<InvalidOperationException>(() =>
            EquipmentStatBudgetCatalog.ConvertRatingToEffectiveValue(
                AttributeType.CritChance,
                10,
                1));
    }

    [Fact]
    public void Combat_pacing_contract_matches_the_approved_equal_tier_targets()
    {
        Assert.Equal(900, EquipmentCombatPacingTargets.OffensiveBenchmarkTicks);
        Assert.Equal(1_200, EquipmentCombatPacingTargets.SustainBenchmarkTicks);
        Assert.Equal(new CombatDurationBand(110, 90, 140),
            EquipmentCombatPacingTargets.GetStandardEnemyTtk(EquipmentCombatRole.Offense));
        Assert.Equal(new CombatDurationBand(140, 120, 160),
            EquipmentCombatPacingTargets.GetStandardEnemyTtk(EquipmentCombatRole.Balanced));
        Assert.Equal(new CombatDurationBand(390, 340, 450),
            EquipmentCombatPacingTargets.GetRawTtd(EquipmentCombatRole.Sustain));
        Assert.Equal(new CombatDurationBand(1_200, 900, 1_350),
            EquipmentCombatPacingTargets.GetEffectiveTtd(EquipmentCombatRole.Defensive));
        Assert.Equal(new CombatDurationBand(1_800, 1_500, 2_100),
            EquipmentCombatPacingTargets.SoloBossTtk);
        Assert.Equal(new CombatDurationBand(2_100, 1_800, 2_400),
            EquipmentCombatPacingTargets.PartyBossTtk);
        Assert.Equal(250, EquipmentCombatPacingTargets.DevelopmentSeedCount);
        Assert.Equal(1_000, EquipmentCombatPacingTargets.ActivationSeedCount);
    }

    [Fact]
    public void Profile_covers_every_attribute_with_one_positive_constant_price()
    {
        Assert.Equal(
            AttributeCatalog.All
                .Where(x => x.IsEquipmentEligible)
                .Select(x => x.AttributeType)
                .Order(),
            EquipmentStatBudgetCatalog.Attributes.Order());

        foreach (var attribute in EquipmentStatBudgetCatalog.Attributes)
        {
            var anchors = EquipmentStatBudgetCatalog.GetCostAnchors(attribute);
            var anchor = Assert.Single(anchors);
            Assert.Equal(EquipmentStatBudgetCatalog.MinimumTier, anchors[0].Tier);
            Assert.True(anchor.CostPerPoint > 0);
            Assert.All(
                new[] { 1, 5, 10, 20, 50, 100 },
                tier => Assert.Equal(
                    anchor.CostPerPoint,
                    EquipmentStatBudgetCatalog.Get(attribute, tier).CostPerPoint,
                    precision: 8));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(100)]
    public void Typed_defense_uses_one_price_at_every_tier(int tier)
    {
        Assert.Equal(
            0.9d,
            EquipmentStatBudgetCatalog.Get(AttributeType.Armor, tier).CostPerPoint,
            precision: 4);
        Assert.Equal(
            0.75d,
            EquipmentStatBudgetCatalog.Get(AttributeType.Resistance, tier).CostPerPoint,
            precision: 4);
    }

    [Theory]
    [InlineData(AttributeType.Power, 22.5d)]
    [InlineData(AttributeType.CritDamage, 2.2d)]
    [InlineData(AttributeType.StatusResistance, 0.82d)]
    [InlineData(AttributeType.HealthRegeneration, 3d)]
    public void Representative_attribute_prices_are_tier_independent(
        AttributeType attribute,
        double expectedCost)
    {
        Assert.All(
            new[] { 1, 5, 10, 20, 50, 100 },
            tier => Assert.Equal(
                expectedCost,
                EquipmentStatBudgetCatalog.Get(attribute, tier).CostPerPoint,
                precision: 4));
    }

    [Fact]
    public void Every_equipment_item_uses_unit_budget_except_two_handed_items()
    {
        var options = new CraftingBalanceOptions();

        Assert.All(
            Enum.GetValues<EquipmentType>().Where(type => type != EquipmentType.TwoHanded),
            equipmentType => Assert.Equal(
                1d,
                options.GetSlotBudgetWeight(equipmentType),
                precision: 4));
        Assert.Equal(2d, options.GetSlotBudgetWeight(EquipmentType.TwoHanded), precision: 4);
        Assert.Equal(
            options.GetSlotBudgetWeight(EquipmentType.OneHanded) * 2d,
            options.GetSlotBudgetWeight(EquipmentType.TwoHanded),
            precision: 4);
    }

    [Fact]
    public void Tier_is_open_ended_and_non_positive_values_are_rejected()
    {
        Assert.Equal(
            EquipmentStatBudgetCatalog.Get(AttributeType.Armor),
            EquipmentStatBudgetCatalog.Get(AttributeType.Armor, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EquipmentStatBudgetCatalog.Get(AttributeType.Armor, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EquipmentTierBudgetCurve.GetBudget(0));
    }

    [Fact]
    public void Equipment_budget_evaluation_is_independent_of_item_tier()
    {
        AttributeModifierBase[] modifiers =
        [
            new InstanceAttributeModifier(AttributeType.Armor, 100)
        ];

        Assert.Equal(90d, EquipmentBudgetEvaluator.Evaluate(modifiers, tier: 1));
        Assert.Equal(90d, EquipmentBudgetEvaluator.Evaluate(modifiers, tier: 5));
        Assert.Equal(90d, EquipmentBudgetEvaluator.Evaluate(modifiers, tier: 100));
        Assert.Equal(17, EquipmentBudgetEvaluator.BalanceVersion);
    }

    [Fact]
    public void Equipment_balance_exposes_only_the_active_profile_and_canonical_item_surface()
    {
        Assert.DoesNotContain(
            typeof(EquipmentStatBudgetCatalog).GetMethods(),
            method => method.Name == nameof(EquipmentStatBudgetCatalog.Get)
                      && method.GetParameters().Length == 3);
        Assert.Null(typeof(EquipmentInstance).GetProperty("BalanceVersion"));
        Assert.Null(typeof(FastCombatEngineOptions).GetProperty("SummonsEnterReady"));

        foreach (var removedProperty in new[]
                 {
                     "AttackSpeed",
                     "Magnitude",
                     "MagnitudeRange",
                     "ScalingAttribute",
                     "ScalingAmount"
                 })
        {
            Assert.Null(typeof(EquipmentBase).GetProperty(removedProperty));
        }
    }

    [Fact]
    public void LegacyBasePercentageModifiersRemainReadableAndCannotExceedCharacterCaps()
    {
        var equipment = new EquipmentInstance
        {
            ItemBaseId = "test-shield",
            ItemBase = new EquipmentBase
            {
                Id = "test-shield",
                Name = "Test Shield",
                EquipmentType = EquipmentType.OffHand,
                AttributeModifiers =
                [
                    new ItemAttributeModifier(
                        AttributeType.BlockChance,
                        50,
                        ModifierType.Flat),
                    new ItemAttributeModifier(
                        AttributeType.Armor,
                        10,
                        ModifierType.Flat)
                ]
            },
            Rarity = Rarity.Legacy
        };

        Assert.Equal(
            50,
            equipment.BaseModifiers.Single(x =>
                x.AttributeType == AttributeType.BlockChance).Amount);
        Assert.Equal(
            10,
            equipment.BaseModifiers.Single(x =>
                x.AttributeType == AttributeType.Armor).Amount);
    }

    [Fact]
    public void Crafted_equipment_uses_recipe_modifiers_without_authored_base_modifiers()
    {
        var equipmentId = Guid.NewGuid();
        var equipment = new EquipmentInstance
        {
            Id = equipmentId,
            ItemBaseId = "test-crafted-armor",
            BaseRecipeId = "recipe.test-crafted-armor",
            ItemBase = new EquipmentBase
            {
                Id = "test-crafted-armor",
                Name = "Test Crafted Armor",
                EquipmentType = EquipmentType.Chest,
                AttributeModifiers =
                [
                    new ItemAttributeModifier(
                        AttributeType.Power,
                        100,
                        ModifierType.Flat)
                ]
            },
            Rarity = Rarity.Legendary,
            InstanceModifiers =
            [
                new InstanceAttributeModifier(AttributeType.Armor, 12)
                {
                    ItemInstanceId = equipmentId
                }
            ]
        };

        Assert.True(equipment.UsesRecipeStatBudget);
        Assert.Empty(equipment.BaseModifiers);
        var modifier = Assert.Single(equipment.AttributeModifiers);
        Assert.Equal(AttributeType.Armor, modifier.AttributeType);
        Assert.Equal(12, modifier.Amount);
    }

    [Fact]
    public void Allocator_preserves_recipe_shares_without_raw_point_caps()
    {
        var allocation = EquipmentBudgetAllocator.Allocate(
            tier: 1,
            budget: 47_000d,
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.95d,
                [AttributeType.MaxHealth] = 0.05d
            },
            roundToWholePoints: false);

        Assert.Equal(44_650d / 22.5d, allocation.AddedPoints[AttributeType.Power], precision: 4);
        Assert.Equal(2_350d / 0.185d, allocation.AddedPoints[AttributeType.MaxHealth], precision: 4);
        Assert.Equal(47_000d, allocation.SpentBudget, precision: 4);
        Assert.Equal(0d, allocation.UnspentBudget, precision: 4);
        Assert.Empty(allocation.CappedAttributes);
    }

    [Fact]
    public void Allocator_is_deterministic_regardless_of_profile_insertion_order()
    {
        var first = EquipmentBudgetAllocator.Allocate(
            10,
            900d,
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.7d,
                [AttributeType.CritChance] = 0.2d,
                [AttributeType.MaxHealth] = 0.1d
            });
        var second = EquipmentBudgetAllocator.Allocate(
            10,
            900d,
            new Dictionary<AttributeType, double>
            {
                [AttributeType.MaxHealth] = 0.1d,
                [AttributeType.CritChance] = 0.2d,
                [AttributeType.Power] = 0.7d
            });

        Assert.Equal(first.AddedPoints, second.AddedPoints);
        Assert.Equal(first.SpentBudget, second.SpentBudget);
        Assert.Equal(first.CappedAttributes, second.CappedAttributes);
    }

    [Fact]
    public void Direct_percentage_allocator_respects_the_intrinsic_per_item_cap()
    {
        var allocation = EquipmentBudgetAllocator.Allocate(
            tier: 1,
            budget: 1_000d,
            new Dictionary<AttributeType, double>
            {
                [AttributeType.DamageReduction] = 1d
            },
            roundToWholePoints: false);

        Assert.Equal(AttributeCombatRules.DamageReductionCapPercent,
            allocation.AddedPoints[AttributeType.DamageReduction], precision: 4);
        Assert.Equal(240d, allocation.SpentBudget, precision: 4);
        Assert.Equal(760d, allocation.UnspentBudget, precision: 4);
        Assert.Contains(AttributeType.DamageReduction, allocation.CappedAttributes);
    }

    [Fact]
    public void Crafted_item_preserves_authored_budget_shares_for_direct_percentages()
    {
        var options = new CraftingBalanceOptions();
        var equipment = new EquipmentBase
        {
            Id = "overflow-test-head",
            Name = "Overflow Test Head",
            EquipmentType = EquipmentType.Head
        };
        var recipe = new CraftingRecipeDefinition
        {
            Id = "recipe.overflow-test-head",
            Name = "Overflow Test Head",
            OutputItemId = equipment.Id,
            OutputItemType = equipment.EquipmentType,
            InitialStatProfile = new Dictionary<AttributeType, double>
            {
                [AttributeType.CritDamage] = 0.90d,
                [AttributeType.MaxHealth] = 0.05d,
                [AttributeType.Armor] = 0.05d
            }
        };

        var modifiers = new ItemStatRollService(Options.Create(options)).RollBaseStats(
            equipment,
            EquipmentCraftingDesignComposer.Compose(recipe, null),
            targetTier: 10,
            ItemQuality.Standard,
            new FixedRandom(0.5d));
        var expectedBudget =
            options.GetTierPowerBudget(10)
            * options.GetSlotBudgetWeight(equipment.EquipmentType);
        var evaluatedBudget = EquipmentBudgetEvaluator.Evaluate(modifiers, tier: 10);
        var maximumRoundingError = modifiers.Sum(modifier =>
            EquipmentStatBudgetCatalog.GetMaterializedCostPerPoint(modifier.AttributeType, 10) / 2d);

        var budgetByAttribute = EquipmentBudgetEvaluator.EvaluateByAttribute(modifiers, 10);
        Assert.InRange(
            budgetByAttribute[AttributeType.CritDamage] / evaluatedBudget,
            0.895d,
            0.905d);
        Assert.InRange(
            Math.Abs(evaluatedBudget - expectedBudget),
            0,
            maximumRoundingError + 0.01d);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Crafted_typed_defense_spends_the_requested_tier_budget_within_rounding(int tier)
    {
        var options = new CraftingBalanceOptions();
        var service = new ItemStatRollService(Options.Create(options));
        var equipment = new EquipmentBase
        {
            Id = "balance-test-chest",
            Name = "Balance Test Chest",
            EquipmentType = EquipmentType.Chest
        };
        var recipe = new CraftingRecipeDefinition
        {
            Id = "recipe.balance-test-chest",
            Name = "Balance Test Chest",
            OutputItemId = equipment.Id,
            OutputItemType = equipment.EquipmentType,
            InitialStatProfile = new Dictionary<AttributeType, double>
            {
                [AttributeType.Armor] = 0.25d,
                [AttributeType.Resistance] = 0.25d,
                [AttributeType.MaxHealth] = 0.25d,
                [AttributeType.Power] = 0.25d
            }
        };
        var design = EquipmentCraftingDesignComposer.Compose(recipe, null);
        var modifiers = service.RollBaseStats(
            equipment,
            design,
            tier,
            ItemQuality.Standard,
            new FixedRandom(0.5d));
        var expectedBudget =
            options.GetTierPowerBudget(tier)
            * options.GetSlotBudgetWeight(equipment.EquipmentType);
        var evaluatedBudget = EquipmentBudgetEvaluator.Evaluate(modifiers, tier);
        var maximumRoundingError = modifiers.Sum(modifier =>
            EquipmentStatBudgetCatalog.Get(modifier.AttributeType, tier).CostPerPoint / 2d);

        Assert.InRange(
            Math.Abs(evaluatedBudget - expectedBudget),
            0,
            maximumRoundingError + 0.01d);
    }

    [Fact]
    public void Constrained_allocator_respects_a_direct_combat_cap()
    {
        var weights = new Dictionary<AttributeType, double>
        {
            [AttributeType.CritChance] = 0.75d,
            [AttributeType.Power] = 0.25d
        };
        EquipmentLinearBudgetConstraint[] constraints =
        [
            new(AttributeType.CritChance, MaximumAddedValue: 10d)
        ];

        var allocation = EquipmentBudgetAllocator.AllocateConstrained(
            tier: 10,
            budget: 2_000d,
            weights,
            constraints);
        var critContribution = allocation.AddedPoints.GetValueOrDefault(AttributeType.CritChance);

        Assert.InRange(critContribution, 9.999d, 10.001d);
        Assert.InRange(allocation.UnspentBudget, 0, 0.001d);
        Assert.Contains(AttributeType.CritChance, allocation.BindingCombatCaps);
        Assert.True(
            allocation.AddedPoints[AttributeType.Power]
            > 2_000d * weights[AttributeType.Power]
            / EquipmentStatBudgetCatalog.Get(AttributeType.Power, 10).CostPerPoint);
        Assert.Equal(17, EquipmentConstraintProfile.BalanceVersion);
        Assert.True(EquipmentConstraintProfile.ProductionActive);
    }

    [Fact]
    public void Overflow_keeps_fury_and_execution_in_distinct_offensive_roles()
    {
        var recipe = new CraftingRecipeDefinition
        {
            Id = "recipe.test.ring",
            Name = "Test Ring",
            OutputItemId = "test-ring",
            OutputItemType = EquipmentType.Ring,
            Tags = ["Accessory"],
            InitialStatProfile = new Dictionary<AttributeType, double>
            {
                [AttributeType.CritChance] = 1d
            }
        };
        var blueprint = new BlueprintDefinition
        {
            Id = "blueprint.test.fury",
            Name = "Blueprint: Test Fury",
            AnyRecipeTags = ["Accessory"],
            BonusStatProfile = new Dictionary<AttributeType, double>
            {
                [AttributeType.CritChance] = 1d
            },
            Tags = ["Fury"]
        };
        var design = EquipmentCraftingDesignComposer.Compose(recipe, blueprint);
        var executionBlueprint = new BlueprintDefinition
        {
            Id = "blueprint.test.execution",
            Name = "Blueprint: Test Execution",
            AnyRecipeTags = ["Accessory"],
            BonusStatProfile = new Dictionary<AttributeType, double>
            {
                [AttributeType.ArmorPenetration] = 1d
            },
            Tags = ["Execution"]
        };
        var executionDesign = EquipmentCraftingDesignComposer.Compose(recipe, executionBlueprint);

        Assert.Equal(
            [
                AttributeType.Power,
                AttributeType.CritChance,
                AttributeType.CritDamage,
                AttributeType.AttackSpeed
            ],
            EquipmentConstraintProfile.GetOverflowWeights(design).Keys);
        Assert.Equal(
            [
                AttributeType.Power,
                AttributeType.ArmorPenetration,
                AttributeType.CritDamage,
                AttributeType.Cooldown
            ],
            EquipmentConstraintProfile.GetOverflowWeights(executionDesign).Keys);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(100)]
    public void Production_profile_uses_constant_health_regeneration_cost(int tier)
    {
        Assert.Equal(
            3d,
            EquipmentConstraintProfile.GetCostPerPoint(
                AttributeType.HealthRegeneration,
                tier),
            precision: 4);
    }

    [Fact]
    public void Open_ended_budget_curve_is_monotonic_and_preserves_reference_endpoints()
    {
        Assert.Equal(100d, EquipmentTierBudgetCurve.GetBudget(1), precision: 8);
        Assert.Equal(1_520d, EquipmentTierBudgetCurve.GetBudget(10), precision: 6);

        var checkpoints = new[] { 1, 5, 10, 20, 50, 100 }
            .Select(EquipmentTierBudgetCurve.GetBudget)
            .ToArray();
        Assert.All(checkpoints, value => Assert.True(double.IsFinite(value) && value > 0));
        Assert.True(checkpoints.Zip(checkpoints.Skip(1)).All(pair => pair.First < pair.Second));
    }

    [Theory]
    [InlineData(AttributeType.Armor)]
    [InlineData(AttributeType.Resistance)]
    public void Progression_normalized_ratings_have_stable_equal_tier_effects(AttributeType attribute)
    {
        var tierOne = EquipmentStatBudgetCatalog.ConvertRatingToEffectiveValue(attribute, 40d, 1);

        foreach (var tier in new[] { 5, 10, 20, 50, 100 })
        {
            var scaledRawRating = 40d * EquipmentTierBudgetCurve.GetScale(tier);
            Assert.Equal(
                tierOne,
                EquipmentStatBudgetCatalog.ConvertRatingToEffectiveValue(
                    attribute,
                    scaledRawRating,
                    tier),
                precision: 4);
        }
    }

    [Theory]
    [InlineData(AttributeType.CritChance)]
    [InlineData(AttributeType.Cooldown)]
    [InlineData(AttributeType.AttackSpeed)]
    public void Direct_percentage_allocations_are_stable_across_tiers(AttributeType attribute)
    {
        var tierOne = EquipmentBudgetAllocator.Allocate(
            1,
            EquipmentTierBudgetCurve.GetBudget(1),
            new Dictionary<AttributeType, double> { [attribute] = 1d },
            roundToWholePoints: false).AddedPoints[attribute];

        foreach (var tier in new[] { 5, 10, 20, 50, 100 })
        {
            var points = EquipmentBudgetAllocator.Allocate(
                tier,
                EquipmentTierBudgetCurve.GetBudget(tier),
                new Dictionary<AttributeType, double> { [attribute] = 1d },
                roundToWholePoints: false).AddedPoints[attribute];
            Assert.Equal(tierOne, points, precision: 4);
        }
    }

    [Fact]
    public void Higher_tier_rating_gear_overperforms_in_lower_progression_context()
    {
        var tierTenRawRating = 40d * EquipmentTierBudgetCurve.GetScale(10);
        var equalTier = EquipmentStatBudgetCatalog.ConvertRatingToEffectiveValue(
            AttributeType.Armor,
            tierTenRawRating,
            progressionTier: 10);
        var overgeared = EquipmentStatBudgetCatalog.ConvertRatingToEffectiveValue(
            AttributeType.Armor,
            tierTenRawRating,
            progressionTier: 5);

        Assert.True(overgeared > equalTier);
        Assert.True(overgeared < AttributeCombatRules.TypedMitigationCapPercent);
    }

    [Fact]
    public void Legacy_item_conversion_preserves_reconstructed_budget_and_is_idempotent()
    {
        var equipment = new EquipmentInstance
        {
            BaseRecipeId = "recipe.legacy",
            Tier = 5,
            StatModelVersion = EquipmentStatBudgetCatalog.LegacyBalanceVersion,
            InstanceModifiers =
            [
                new InstanceAttributeModifier(AttributeType.Power, 10),
                new InstanceAttributeModifier(AttributeType.Armor, 10)
            ]
        };

        Assert.True(EquipmentStatModelMigrator.MigrateToCurrent(equipment));
        Assert.Equal(EquipmentStatBudgetCatalog.BalanceVersion, equipment.StatModelVersion);
        Assert.Equal(5f, equipment.InstanceModifiers.Single(x => x.AttributeType == AttributeType.Power).Amount);
        Assert.Equal(27.5f, equipment.InstanceModifiers.Single(x => x.AttributeType == AttributeType.Armor).Amount);
        Assert.Equal(137.25d, EquipmentBudgetEvaluator.Evaluate(equipment.InstanceModifiers), precision: 2);

        var converted = equipment.InstanceModifiers.Select(x => x.Amount).ToArray();
        Assert.False(EquipmentStatModelMigrator.MigrateToCurrent(equipment));
        Assert.Equal(converted, equipment.InstanceModifiers.Select(x => x.Amount));
    }

    [Fact]
    public void V16_to_v17_conversion_preserves_effective_value_and_rating_points()
    {
        const int tier = 5;
        var rawCriticalRating = 40d * EquipmentTierBudgetCurve.GetScale(tier);
        var equipment = new EquipmentInstance
        {
            BaseRecipeId = "recipe.medium",
            CraftedName = "Broken Medium Helm",
            Tier = tier,
            StatModelVersion = EquipmentStatBudgetCatalog.PreviousBalanceVersion,
            InstanceModifiers =
            [
                new InstanceAttributeModifier(AttributeType.CritChance, (float)(rawCriticalRating / 2d)),
                new InstanceAttributeModifier(AttributeType.CritChance, (float)(rawCriticalRating / 2d)),
                new InstanceAttributeModifier(AttributeType.Armor, 123.45f),
                new InstanceAttributeModifier(AttributeType.MaxHealth, 500f)
            ]
        };
        var expectedCriticalChance = AttributeValueQuantizer.Quantize(
            AttributeType.CritChance,
            EquipmentStatModelMigrator.ConvertV16RatingToDirectPercentage(
                AttributeType.CritChance,
                rawCriticalRating,
                tier));

        Assert.True(EquipmentStatModelMigrator.MigrateToCurrent(equipment));
        Assert.Equal(EquipmentStatBudgetCatalog.BalanceVersion, equipment.StatModelVersion);
        Assert.Equal("Broken Medium Helm", equipment.CraftedName);
        Assert.Equal(
            expectedCriticalChance,
            equipment.InstanceModifiers.Single(x => x.AttributeType == AttributeType.CritChance).Amount,
            precision: 2);
        Assert.Equal(123.45f,
            equipment.InstanceModifiers.Single(x => x.AttributeType == AttributeType.Armor).Amount);
        Assert.Equal(500f,
            equipment.InstanceModifiers.Single(x => x.AttributeType == AttributeType.MaxHealth).Amount);

        var values = equipment.InstanceModifiers
            .Select(modifier => (modifier.AttributeType, modifier.Amount))
            .ToArray();
        Assert.False(EquipmentStatModelMigrator.MigrateToCurrent(equipment));
        Assert.Equal(values, equipment.InstanceModifiers.Select(modifier =>
            (modifier.AttributeType, modifier.Amount)));
    }

    [Fact]
    public void V16_snapshot_conversion_uses_the_same_idempotent_unit_migration()
    {
        const int tier = 5;
        var rawRating = 40d * EquipmentTierBudgetCurve.GetScale(tier);
        var source = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = "medium_helm",
            BaseRecipeId = "recipe.medium",
            Tier = tier,
            StatModelVersion = EquipmentStatBudgetCatalog.BalanceVersion
        };
        var snapshot = EquipmentSnapshot.From(EquipmentSlotType.Head, source);
        snapshot.StatModelVersion = EquipmentStatBudgetCatalog.PreviousBalanceVersion;
        snapshot.InstanceModifiers =
        [
            EquipmentAttributeModifierSnapshot.From(
                new InstanceAttributeModifier(AttributeType.CritChance, (float)rawRating)),
            EquipmentAttributeModifierSnapshot.From(
                new InstanceAttributeModifier(AttributeType.Armor, 123.45f))
        ];

        Assert.True(EquipmentStatModelMigrator.MigrateToCurrent(snapshot));
        Assert.Equal(EquipmentStatBudgetCatalog.BalanceVersion, snapshot.StatModelVersion);
        Assert.Equal(
            EquipmentStatModelMigrator.ConvertV16RatingToDirectPercentage(
                AttributeType.CritChance,
                rawRating,
                tier),
            snapshot.InstanceModifiers.Single(x => x.AttributeType == AttributeType.CritChance).Amount,
            precision: 2);
        Assert.Equal(
            123.45f,
            snapshot.InstanceModifiers.Single(x => x.AttributeType == AttributeType.Armor).Amount);
        Assert.False(EquipmentStatModelMigrator.MigrateToCurrent(snapshot));
    }

    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }
}
