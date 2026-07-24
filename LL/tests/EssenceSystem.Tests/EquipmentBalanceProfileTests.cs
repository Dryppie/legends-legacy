using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Options;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class EquipmentBalanceProfileTests
{
    [Fact]
    public void Profile_covers_every_attribute_with_positive_ordered_anchors()
    {
        Assert.Equal(
            Enum.GetValues<AttributeType>().Order(),
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
    [InlineData(1, 0.54d)]
    [InlineData(3, 0.78d)]
    [InlineData(5, 1.02d)]
    [InlineData(8, 1.23d)]
    [InlineData(10, 1.37d)]
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
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Fortitude_cost_equals_its_direct_derived_stat_basket(int tier)
    {
        var fortitudeCost = EquipmentStatBudgetCatalog.Get(AttributeType.Fortitude, tier).CostPerPoint;
        var maxHealthCost = EquipmentStatBudgetCatalog.Get(AttributeType.MaxHealth, tier).CostPerPoint;
        var armorCost = EquipmentStatBudgetCatalog.Get(AttributeType.Armor, tier).CostPerPoint;
        var resistanceCost = EquipmentStatBudgetCatalog.Get(AttributeType.Resistance, tier).CostPerPoint;
        var basketCost = 4 * maxHealthCost + 0.5d * armorCost + 0.5d * resistanceCost;

        Assert.Equal(basketCost, fortitudeCost, precision: 4);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Precision_and_spirit_costs_equal_their_direct_derived_stat_baskets(int tier)
    {
        var precisionBasket =
            0.1d * EquipmentStatBudgetCatalog.Get(AttributeType.CritChance, tier).CostPerPoint
            + 0.1d * EquipmentStatBudgetCatalog.Get(AttributeType.ArmorPenetration, tier).CostPerPoint
            + 0.1d * EquipmentStatBudgetCatalog.Get(AttributeType.MagicPenetration, tier).CostPerPoint
            + 0.05d * EquipmentStatBudgetCatalog.Get(AttributeType.AttackSpeed, tier).CostPerPoint;
        var spiritBasket =
            0.15d * EquipmentStatBudgetCatalog.Get(AttributeType.HealingPowerPercent, tier).CostPerPoint
            + 0.05d * EquipmentStatBudgetCatalog.Get(AttributeType.HealthRegeneration, tier).CostPerPoint
            + 0.1d * EquipmentStatBudgetCatalog.Get(AttributeType.StatusResistance, tier).CostPerPoint
            + 0.1d * EquipmentStatBudgetCatalog.Get(AttributeType.CrowdControlResistance, tier).CostPerPoint
            + 0.05d * EquipmentStatBudgetCatalog.Get(AttributeType.SummonPower, tier).CostPerPoint
            + 0.1d * EquipmentStatBudgetCatalog.Get(AttributeType.SummonHealth, tier).CostPerPoint;

        Assert.Equal(
            precisionBasket,
            EquipmentStatBudgetCatalog.Get(AttributeType.Precision, tier).CostPerPoint,
            precision: 4);
        Assert.Equal(
            spiritBasket,
            EquipmentStatBudgetCatalog.Get(AttributeType.Spirit, tier).CostPerPoint,
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

        Assert.Equal(54d, EquipmentBudgetEvaluator.Evaluate(modifiers, tier: 1));
        Assert.Equal(102d, EquipmentBudgetEvaluator.Evaluate(modifiers, tier: 5));
        Assert.Equal(137d, EquipmentBudgetEvaluator.Evaluate(modifiers, tier: 10));
        Assert.Equal(2, EquipmentBudgetEvaluator.BalanceVersion);
    }

    [Fact]
    public void Allocator_redistributes_capped_budget_to_other_eligible_stats()
    {
        var allocation = EquipmentBudgetAllocator.Allocate(
            tier: 1,
            budget: 700d,
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.9d,
                [AttributeType.MaxHealth] = 0.1d
            },
            roundToWholePoints: false);

        Assert.Equal(500d, allocation.AddedPoints[AttributeType.Power], precision: 4);
        Assert.Equal(1_000d, allocation.AddedPoints[AttributeType.MaxHealth], precision: 4);
        Assert.Equal(700d, allocation.SpentBudget, precision: 4);
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
                [AttributeType.Power] = 0.90d,
                [AttributeType.MaxHealth] = 0.05d,
                [AttributeType.WeaponDamage] = 0.05d
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

        Assert.Equal(
            EquipmentStatBudgetCatalog.Get(AttributeType.Power, 10).HardCap,
            modifiers.Single(x => x.AttributeType == AttributeType.Power).Amount);
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

    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }
}
