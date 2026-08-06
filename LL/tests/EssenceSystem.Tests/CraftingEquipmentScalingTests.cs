using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Options;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class CraftingEquipmentScalingTests
{
    [Fact]
    public void RollBaseStats_HigherTierBudgetCreatesStrongerEquipmentBase()
    {
        var service = new ItemStatRollService(Options.Create(new CraftingBalanceOptions()));
        var equipment = new EquipmentBase { Id = "iron_helm", Name = "Iron Helm", EquipmentType = EquipmentType.Head };
        var recipe = CreateSingleStatDesign();

        var tierOne = service.RollBaseStats(equipment, recipe, 1, ItemQuality.Standard, new FixedRandom(0.5d)).Single();
        var tierTwo = service.RollBaseStats(equipment, recipe, 2, ItemQuality.Standard, new FixedRandom(0.5d)).Single();

        Assert.True(tierTwo.Amount > tierOne.Amount);
    }

    [Fact]
    public void RollBaseStats_QualityMultiplierIncreasesCraftedStatBudget()
    {
        var service = new ItemStatRollService(Options.Create(new CraftingBalanceOptions()));
        var equipment = new EquipmentBase { Id = "iron_helm", Name = "Iron Helm", EquipmentType = EquipmentType.Head };
        var recipe = CreateSingleStatDesign();

        var standard = service.RollBaseStats(equipment, recipe, 1, ItemQuality.Standard, new FixedRandom(0.5d)).Single();
        var fine = service.RollBaseStats(equipment, recipe, 1, ItemQuality.Fine, new FixedRandom(0.5d)).Single();

        Assert.True(fine.Amount > standard.Amount);
    }

    [Fact]
    public void GetBaseStatRanges_EnclosesEveryPossibleCurrentMasteryRoll()
    {
        var service = new ItemStatRollService(Options.Create(new CraftingBalanceOptions()));
        var equipment = new EquipmentBase { Id = "iron_helm", Name = "Iron Helm", EquipmentType = EquipmentType.Head };
        var recipe = CreateSingleStatDesign();
        var possibleQualities = new[]
        {
            ItemQuality.Crude,
            ItemQuality.Standard,
            ItemQuality.Fine,
            ItemQuality.Exceptional
        };

        var preview = service.GetBaseStatRanges(equipment, recipe, 1, possibleQualities).Single();
        var minimumRoll = service.RollBaseStats(
            equipment,
            recipe,
            1,
            ItemQuality.Crude,
            new FixedRandom(0d)).Single();
        var maximumRoll = service.RollBaseStats(
            equipment,
            recipe,
            1,
            ItemQuality.Exceptional,
            new FixedRandom(1d)).Single();

        Assert.Equal(minimumRoll.Amount, preview.MinimumAmount);
        Assert.Equal(maximumRoll.Amount, preview.MaximumAmount);
    }

    [Fact]
    public void Blueprint_roll_preserves_base_recipe_stats_and_adds_a_separate_bonus()
    {
        var service = new ItemStatRollService(Options.Create(new CraftingBalanceOptions()));
        var equipment = new EquipmentBase
        {
            Id = "cloth_cowl",
            Name = "Cloth Cowl",
            EquipmentType = EquipmentType.Head
        };
        var recipe = new CraftingRecipeDefinition
        {
            Id = "recipe.armor.head.cloth",
            Name = "Cloth Cowl",
            OutputItemId = equipment.Id,
            OutputItemType = equipment.EquipmentType,
            InitialStatProfile = new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.3d,
                [AttributeType.HealingPowerPercent] = 0.3d,
                [AttributeType.MaxHealth] = 0.25d,
                [AttributeType.Resistance] = 0.15d
            }
        };
        var blueprint = new BlueprintDefinition
        {
            Id = "blueprint_aegis",
            Name = "Blueprint: Aegis",
            BonusStatBudgetMultiplier = 0.2d,
            BonusStatProfile = new Dictionary<AttributeType, double>
            {
                [AttributeType.Armor] = 0.6d,
                [AttributeType.DamageReduction] = 0.4d
            }
        };
        var baseDesign = EquipmentCraftingDesignComposer.Compose(recipe, null);
        var blueprintDesign = EquipmentCraftingDesignComposer.Compose(recipe, blueprint);

        var baseStats = service.RollBaseStats(
                equipment,
                baseDesign,
                1,
                ItemQuality.Standard,
                new FixedRandom(0.5d))
            .ToDictionary(modifier => modifier.AttributeType, modifier => modifier.Amount);
        var blueprintStats = service.RollBaseStats(
                equipment,
                blueprintDesign,
                1,
                ItemQuality.Standard,
                new FixedRandom(0.5d))
            .ToDictionary(modifier => modifier.AttributeType, modifier => modifier.Amount);

        Assert.All(baseStats, stat => Assert.Equal(stat.Value, blueprintStats[stat.Key]));
        Assert.True(blueprintStats[AttributeType.Armor] > 0);
        Assert.True(blueprintStats[AttributeType.DamageReduction] > 0);
    }

    [Fact]
    public void GetQualityChances_ReportsTheActualMasteryZeroDistribution()
    {
        var service = new ItemQualityRollService();

        var chances = service.GetQualityChances(0);

        Assert.Equal(100d, chances.Values.Sum());
        Assert.Equal(25d, chances[ItemQuality.Crude]);
        Assert.Equal(60d, chances[ItemQuality.Standard]);
        Assert.Equal(14d, chances[ItemQuality.Fine]);
        Assert.Equal(1d, chances[ItemQuality.Exceptional]);
        Assert.Equal(0d, chances[ItemQuality.Masterwork]);
    }

    [Fact]
    public void CalculateStartingPotential_DefaultBalanceDefinesExpectedTemperingActionBudget()
    {
        var potentialService = new ItemPotentialService(Options.Create(new CraftingBalanceOptions()));
        var equipment = new EquipmentBase { Id = "iron_helm", Name = "Iron Helm", EquipmentType = EquipmentType.Head };
        var instance = new EquipmentInstance
        {
            ItemBaseId = equipment.Id,
            ItemBase = equipment,
            Tier = 1,
            Quality = ItemQuality.Standard,
            Potential = potentialService.CalculateStartingPotential(
                equipment,
                targetTier: 1,
                quality: ItemQuality.Standard,
                masteryLevel: 0,
                craftingLevel: 0)
        };
        Assert.Equal(200, instance.Potential);
    }

    [Fact]
    public void CalculateStartingPotential_UsesConfiguredPotentialMultipliers()
    {
        var service = new ItemPotentialService(Options.Create(new CraftingBalanceOptions
        {
            PotentialSlotWeights = new Dictionary<EquipmentType, double>
            {
                [EquipmentType.Head] = 1d
            },
            PotentialQualityMultipliers = new Dictionary<ItemQuality, double>
            {
                [ItemQuality.Standard] = 2d
            }
        }));
        var equipment = new EquipmentBase { Id = "iron_helm", Name = "Iron Helm", EquipmentType = EquipmentType.Head };

        var potential = service.CalculateStartingPotential(
            equipment,
            targetTier: 1,
            quality: ItemQuality.Standard,
            masteryLevel: 0,
            craftingLevel: 0);

        Assert.Equal(400, potential);
    }

    private static EquipmentCraftingDesign CreateSingleStatDesign()
    {
        var recipe = new CraftingRecipeDefinition
        {
            Id = "recipe.armor.head.test",
            Name = "Head Armor",
            OutputItemId = "iron_helm",
            OutputItemType = EquipmentType.Head,
            InitialStatProfile = new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 1d
            }
        };
        return EquipmentCraftingDesignComposer.Compose(recipe, null);
    }

    private sealed class FixedRandom(double nextDouble) : Random
    {
        public override double NextDouble() => nextDouble;
    }
}
