using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Engine;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class EquipmentBalanceProfileTests
{
    [Fact]
    public void Profile_covers_every_attribute_with_positive_ordered_anchors()
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
            Assert.NotEmpty(anchors);
            Assert.Equal(EquipmentStatBudgetCatalog.MinimumTier, anchors[0].Tier);
            Assert.Equal(EquipmentStatBudgetCatalog.MaximumTier, anchors[^1].Tier);
            Assert.All(anchors, anchor => Assert.True(anchor.CostPerPoint > 0));
            Assert.Equal(anchors.Select(x => x.Tier).Distinct().Order(), anchors.Select(x => x.Tier));
        }
    }

    [Theory]
    [InlineData(1, 0.68d)]
    [InlineData(3, 1.275d)]
    [InlineData(5, 1.87d)]
    [InlineData(8, 3.22d)]
    [InlineData(10, 4.12d)]
    public void Typed_defense_cost_interpolates_between_reviewed_tier_anchors(
        int tier,
        double expectedCost)
    {
        Assert.Equal(
            expectedCost,
            EquipmentStatBudgetCatalog.Get(AttributeType.Armor, tier).CostPerPoint,
            precision: 4);
        Assert.Equal(
            expectedCost,
            EquipmentStatBudgetCatalog.Get(AttributeType.Resistance, tier).CostPerPoint,
            precision: 4);
    }

    [Theory]
    [InlineData(1, 2d)]
    [InlineData(5, 2.25d)]
    [InlineData(10, 2.5d)]
    public void CritDamageCostRisesWithEndgameCritSynergy(
        int tier,
        double expectedCost)
    {
        Assert.Equal(
            expectedCost,
            EquipmentStatBudgetCatalog
                .Get(AttributeType.CritDamage, tier)
                .CostPerPoint,
            precision: 4);
    }

    [Theory]
    [InlineData(1, 24d)]
    [InlineData(3, 15.35d)]
    [InlineData(5, 6.7d)]
    [InlineData(8, 4.78d)]
    [InlineData(10, 3.5d)]
    public void Power_cost_tracks_its_tier_relative_throughput_value(
        int tier,
        double expectedCost)
    {
        Assert.Equal(
            expectedCost,
            EquipmentStatBudgetCatalog.Get(AttributeType.Power, tier).CostPerPoint,
            precision: 4);
    }

    [Fact]
    public void TwoHandedAndDualWieldReceiveEqualHandSlotFunding()
    {
        var options = new CraftingBalanceOptions();

        Assert.Equal(
            options.GetSlotBudgetWeight(EquipmentType.OneHanded) * 2d,
            options.GetSlotBudgetWeight(EquipmentType.TwoHanded),
            precision: 4);
    }

    [Fact]
    public void Tier_is_clamped_to_the_supported_profile_range()
    {
        Assert.Equal(
            EquipmentStatBudgetCatalog.Get(AttributeType.Armor, 1),
            EquipmentStatBudgetCatalog.Get(AttributeType.Armor, -100));
        Assert.Equal(
            EquipmentStatBudgetCatalog.Get(AttributeType.Armor, 10),
            EquipmentStatBudgetCatalog.Get(AttributeType.Armor, 100));
    }

    [Fact]
    public void Equipment_budget_evaluation_uses_the_items_tier()
    {
        AttributeModifierBase[] modifiers =
        [
            new InstanceAttributeModifier(AttributeType.Armor, 100)
        ];

        Assert.Equal(68d, EquipmentBudgetEvaluator.Evaluate(modifiers, tier: 1));
        Assert.Equal(187d, EquipmentBudgetEvaluator.Evaluate(modifiers, tier: 5));
        Assert.Equal(412d, EquipmentBudgetEvaluator.Evaluate(modifiers, tier: 10));
        Assert.Equal(10, EquipmentBudgetEvaluator.BalanceVersion);
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
    public void Allocator_redistributes_capped_budget_to_other_eligible_stats()
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

        Assert.Equal(1_800d, allocation.AddedPoints[AttributeType.Power], precision: 4);
        Assert.Equal(19_000d, allocation.AddedPoints[AttributeType.MaxHealth], precision: 4);
        Assert.Equal(47_000d, allocation.SpentBudget, precision: 4);
        Assert.Equal(0d, allocation.UnspentBudget, precision: 4);
        Assert.Contains(AttributeType.Power, allocation.CappedAttributes);
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
    public void Allocator_reports_unspent_budget_only_when_every_eligible_stat_is_capped()
    {
        var allocation = EquipmentBudgetAllocator.Allocate(
            tier: 1,
            budget: 1_000d,
            new Dictionary<AttributeType, double>
            {
                [AttributeType.DamageReduction] = 1d
            },
            roundToWholePoints: false);

        Assert.Equal(
            AttributeCombatRules.DamageReductionCapPercent,
            allocation.AddedPoints[AttributeType.DamageReduction]);
        Assert.Equal(240d, allocation.SpentBudget, precision: 4);
        Assert.Equal(760d, allocation.UnspentBudget, precision: 4);
    }

    [Fact]
    public void Crafted_item_redistributes_a_capped_roll_instead_of_discarding_its_budget()
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
                [AttributeType.DamageReduction] = 0.90d,
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
            EquipmentStatBudgetCatalog.Get(modifier.AttributeType, 10).CostPerPoint / 2d);

        Assert.True(
            modifiers.Single(x => x.AttributeType == AttributeType.DamageReduction).Amount
            < expectedBudget * recipe.InitialStatProfile[AttributeType.DamageReduction]
            / EquipmentStatBudgetCatalog.Get(AttributeType.DamageReduction, 10).CostPerPoint);
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
            budget: 100d,
            weights,
            constraints);
        var critContribution = allocation.AddedPoints.GetValueOrDefault(AttributeType.CritChance);

        Assert.InRange(critContribution, 9.999d, 10.001d);
        Assert.InRange(allocation.UnspentBudget, 0, 0.001d);
        Assert.Contains(AttributeType.CritChance, allocation.BindingCombatCaps);
        Assert.True(
            allocation.AddedPoints[AttributeType.Power]
            > 100d * weights[AttributeType.Power]
            / EquipmentStatBudgetCatalog.Get(AttributeType.Power, 10).CostPerPoint);
        Assert.Equal(10, EquipmentConstraintProfile.BalanceVersion);
        Assert.True(EquipmentConstraintProfile.ProductionActive);
    }

    [Fact]
    public void Overflow_preserves_blueprint_identity_across_compatible_slots()
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

        Assert.Equal(
            [
                AttributeType.Power,
                AttributeType.CritChance,
                AttributeType.CritDamage,
                AttributeType.ArmorPenetration,
                AttributeType.AttackSpeed
            ],
            EquipmentConstraintProfile.GetOverflowWeights(design).Keys);
    }

    [Theory]
    [InlineData(1, 3.00d)]
    [InlineData(3, 2.125d)]
    [InlineData(5, 1.25d)]
    [InlineData(10, 1.00d)]
    public void Production_profile_uses_tiered_summon_power_costs(
        int tier,
        double expectedCost)
    {
        Assert.Equal(
            expectedCost,
            EquipmentConstraintProfile.GetCostPerPoint(
                AttributeType.SummonPower,
                tier),
            precision: 4);
        Assert.Equal(
            EquipmentStatBudgetCatalog.Get(AttributeType.Power, tier).CostPerPoint,
            EquipmentConstraintProfile.GetCostPerPoint(
                AttributeType.Power,
                tier),
            precision: 4);
    }

    [Theory]
    [InlineData(1, 1.90d)]
    [InlineData(3, 1.325d)]
    [InlineData(5, 0.75d)]
    [InlineData(8, 0.60d)]
    [InlineData(10, 0.50d)]
    public void Production_profile_uses_tiered_summon_health_costs(
        int tier,
        double expectedCost)
    {
        Assert.Equal(
            expectedCost,
            EquipmentConstraintProfile.GetCostPerPoint(
                AttributeType.SummonHealth,
                tier),
            precision: 4);
    }

    [Theory]
    [InlineData(1, 1.50d)]
    [InlineData(5, 1.50d)]
    [InlineData(8, 1.86d)]
    [InlineData(10, 2.10d)]
    public void Production_profile_uses_tiered_health_regeneration_costs(
        int tier,
        double expectedCost)
    {
        Assert.Equal(
            expectedCost,
            EquipmentConstraintProfile.GetCostPerPoint(
                AttributeType.HealthRegeneration,
                tier),
            precision: 4);
    }

    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }
}
