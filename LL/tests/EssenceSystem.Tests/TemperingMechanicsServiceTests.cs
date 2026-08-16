using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Options;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class TemperingMechanicsServiceTests
{
    [Fact]
    public void PositiveAttemptOnlyBuildsRarityProgressAndConsumesOnePotential()
    {
        var equipment = CreateEquipment();
        var result = new TemperingMechanicsService()
            .ApplyTemperingAttempt(equipment, CreateProfile(), new FixedRandom(0.01d));

        Assert.Equal(TemperingOutcome.Positive, result.Outcome);
        Assert.Null(result.ImprovedStat);
        Assert.Empty(equipment.InstanceModifiers);
        Assert.Equal(9, equipment.Potential);
        Assert.Equal(1, equipment.ItemXp);
        Assert.Equal(10, result.PreviousPotential);
        Assert.Equal(9, result.NewPotential);
        Assert.Equal(0, result.PreviousItemXp);
        Assert.Equal(1, result.NewItemXp);
    }

    [Fact]
    public void NeutralAttemptIsNonDestructiveAndConsumesOnlyOnePotential()
    {
        var equipment = CreateEquipment();
        var result = new TemperingMechanicsService(Options.Create(new CraftingBalanceOptions
        {
            CriticalChanceBase = 0d,
            CriticalChancePerRarityStep = 0d
        })).ApplyTemperingAttempt(equipment, CreateProfile(), new FixedRandom(0.5d));

        Assert.Equal(TemperingOutcome.Neutral, result.Outcome);
        Assert.Null(result.ImprovedStat);
        Assert.Empty(equipment.InstanceModifiers);
        Assert.Equal(9, equipment.Potential);
        Assert.Equal(0, equipment.ItemXp);
    }

    [Fact]
    public void TenthImprovementUpgradesRarity()
    {
        var equipment = CreateEquipment();
        equipment.ItemXp = 9;

        var result = new TemperingMechanicsService()
            .ApplyTemperingAttempt(equipment, CreateProfile(), new FixedRandom(0.01d));

        Assert.True(result.RarityUpgraded);
        Assert.Equal(Rarity.Uncommon, equipment.Rarity);
        Assert.Equal(0, equipment.ItemXp);
        Assert.Equal(AttributeType.Armor, result.ImprovedStat);
        Assert.Single(equipment.InstanceModifiers);
    }

    [Fact]
    public void RarityUpgradeUsesThematicOverflowWhenAuthoredStatsAreCapped()
    {
        var equipment = CreateEquipment();
        equipment.Tier = 10;
        equipment.ItemXp = 9;
        equipment.InstanceModifiers.Add(new InstanceAttributeModifier(
            AttributeType.Armor,
            719));

        var result = new TemperingMechanicsService()
            .ApplyTemperingAttempt(equipment, CreateProfile(), new FixedRandom(0.01d));

        Assert.True(result.RarityUpgraded);
        Assert.Equal(AttributeType.Power, result.ImprovedStat);
        var powerImprovement = equipment.InstanceModifiers.Single(x =>
            x.AttributeType == AttributeType.Power).Amount;
        Assert.Equal(
            (float)Math.Max(
                1d,
                Math.Round(
                    TemperingConstants.GetDirectedImprovementBudget(equipment.Tier)
                    / EquipmentStatBudgetCatalog
                        .Get(AttributeType.Power, equipment.Tier)
                        .CostPerPoint)),
            powerImprovement);
    }

    [Fact]
    public void CriticalQualityIncreaseDoesNotRestorePotential()
    {
        var equipment = CreateEquipment();
        equipment.Quality = ItemQuality.Fine;
        equipment.MaxPotential = 10;
        var service = new TemperingMechanicsService(Options.Create(new CraftingBalanceOptions
        {
            CriticalChanceBase = 1d,
            CriticalChancePerRarityStep = 0d,
            CriticalLevelingItemChance = 0d
        }));

        var result = service.ApplyTemperingAttempt(equipment, CreateProfile(), new FixedRandom(0.5d));

        Assert.Equal(TemperingOutcome.Critical, result.Outcome);
        Assert.True(result.QualityIncreased);
        Assert.Equal(ItemQuality.Exceptional, equipment.Quality);
        Assert.Equal(9, equipment.Potential);
        Assert.Equal(10, equipment.MaxPotential);
    }

    [Fact]
    public void CriticalQualityIncreaseRedistributesBudgetFromACappedStat()
    {
        var options = new CraftingBalanceOptions
        {
            CriticalChanceBase = 1d,
            CriticalChancePerRarityStep = 0d,
            CriticalLevelingItemChance = 0d
        };
        var equipment = CreateEquipment();
        equipment.Tier = 10;
        equipment.Quality = ItemQuality.Fine;
        var expectedPowerCap = EquipmentStatBudgetCatalog
            .Get(AttributeType.Power, equipment.Tier)
            .HardCap
            * (float)EquipmentConstraintProfile.GetPerItemCapMultiplier(
                options.GetSlotBudgetWeight(equipment.EquipmentBase.EquipmentType));
        equipment.InstanceModifiers.Add(new InstanceAttributeModifier(
            AttributeType.Power,
            expectedPowerCap - 1));
        equipment.InstanceModifiers.Add(new InstanceAttributeModifier(AttributeType.MaxHealth, 100));
        var service = new TemperingMechanicsService(Options.Create(options));

        var result = service.ApplyTemperingAttempt(equipment, CreateProfile(), new FixedRandom(0.5d));

        Assert.True(result.QualityIncreased);
        Assert.Equal(
            expectedPowerCap,
            equipment.InstanceModifiers.Single(x => x.AttributeType == AttributeType.Power).Amount);
        Assert.True(
            equipment.InstanceModifiers.Single(x => x.AttributeType == AttributeType.MaxHealth).Amount > 100);
    }

    [Fact]
    public void NegativeAttemptReportsItsAdditionalPotentialPenalty()
    {
        var equipment = CreateEquipment();
        var result = new TemperingMechanicsService(Options.Create(new CraftingBalanceOptions
        {
            CriticalChanceBase = 0d,
            CriticalChancePerRarityStep = 0d
        })).ApplyTemperingAttempt(equipment, CreateProfile(), new FixedRandom(0.08d));

        Assert.Equal(TemperingOutcome.Negative, result.Outcome);
        Assert.Equal(10, result.PreviousPotential);
        Assert.Equal(8, result.NewPotential);
        Assert.Equal(1, result.PotentialSpent);
    }

    [Fact]
    public void Attempt_normalizes_existing_equipment_values_to_canonical_precision()
    {
        var equipment = CreateEquipment();
        equipment.InstanceModifiers.Add(new InstanceAttributeModifier(
            AttributeType.MaxHealth,
            199.49f));
        equipment.InstanceModifiers.Add(new InstanceAttributeModifier(
            AttributeType.CrowdControlResistance,
            9.933024f));
        var service = new TemperingMechanicsService(Options.Create(new CraftingBalanceOptions
        {
            CriticalChanceBase = 0d,
            CriticalChancePerRarityStep = 0d
        }));

        service.ApplyTemperingAttempt(equipment, CreateProfile(), new FixedRandom(0.5d));

        Assert.Equal(
            199,
            equipment.InstanceModifiers.Single(x =>
                x.AttributeType == AttributeType.MaxHealth).Amount);
        Assert.Equal(
            9.93f,
            equipment.InstanceModifiers.Single(x =>
                x.AttributeType == AttributeType.CrowdControlResistance).Amount);
    }

    [Fact]
    public void Rarity_upgrade_improves_a_realistic_capped_cloth_item()
    {
        var equipment = new EquipmentInstance
        {
            ItemBaseId = "cloth_cowl",
            ItemBase = new EquipmentBase
            {
                Id = "cloth_cowl",
                Name = "Cloth Cowl",
                EquipmentType = EquipmentType.Chest
            },
            BaseRecipeId = "recipe.armor.head.cloth_cowl",
            Tier = 1,
            StatModelVersion = EquipmentStatBudgetCatalog.BalanceVersion,
            Potential = 10,
            ItemXp = 9,
            InstanceModifiers =
            [
                new InstanceAttributeModifier(AttributeType.MaxHealth, 143),
                new InstanceAttributeModifier(AttributeType.Resistance, 6.24f),
                new InstanceAttributeModifier(AttributeType.HealingPowerPercent, 16.65f),
                new InstanceAttributeModifier(AttributeType.Cooldown, 4.76f)
            ]
        };
        var profile = new TemperingProfileDefinition
        {
            Id = "recipe.armor.head.cloth_cowl.tempering",
            Name = "Cloth Cowl Tempering",
            Stats =
            [
                TemperingStat(AttributeType.HealingPowerPercent, 35, 0.4d),
                TemperingStat(AttributeType.Resistance, 25, 0.35d),
                TemperingStat(AttributeType.Cooldown, 20, 0.3d),
                TemperingStat(AttributeType.MaxHealth, 20, 0.3d)
            ]
        };
        var before = EquipmentBudgetEvaluator.Evaluate(
            equipment.AttributeModifiers,
            equipment.Tier);

        var result = new TemperingMechanicsService().ApplyTemperingAttempt(
            equipment,
            profile,
            new FixedRandom(0.0005d));

        Assert.True(result.RarityUpgraded);
        Assert.NotNull(result.ImprovedStat);
        Assert.True(
            EquipmentBudgetEvaluator.Evaluate(equipment.AttributeModifiers, equipment.Tier) > before,
            $"{result.ImprovedStat}: {result.PreviousStatValue} -> {result.NewStatValue}; " +
            string.Join(", ", equipment.InstanceModifiers.Select(x => $"{x.AttributeType}={x.Amount}")));
    }

    private static EquipmentInstance CreateEquipment() => new()
    {
        ItemBaseId = "heavy_breastplate",
        ItemBase = new EquipmentBase
        {
            Id = "heavy_breastplate",
            Name = "Heavy Breastplate",
            EquipmentType = EquipmentType.Chest
        },
        Rarity = Rarity.Common,
        Tier = 2,
        Potential = 10
    };

    private static TemperingProfileDefinition CreateProfile() => new()
    {
        Id = "profile.test",
        Name = "Test",
        Stats =
        [
            new TemperingStatWeightDefinition
            {
                Stat = AttributeType.Armor,
                Weight = 100,
                Category = TemperingStatCategory.Primary,
                CanIntroduce = true,
                CanIncrease = true,
                MaxBudgetShare = 1d
            }
        ]
    };

    private static TemperingStatWeightDefinition TemperingStat(
        AttributeType stat,
        double weight,
        double maximumBudgetShare) => new()
    {
        Stat = stat,
        Weight = weight,
        Category = TemperingStatCategory.Secondary,
        CanIntroduce = true,
        CanIncrease = true,
        MaxBudgetShare = maximumBudgetShare,
        MinimumTier = 1
    };

    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }
}
