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
        var equipment = CreateEquipment();
        equipment.Tier = 10;
        equipment.Quality = ItemQuality.Fine;
        equipment.InstanceModifiers.Add(new InstanceAttributeModifier(AttributeType.Power, 499));
        equipment.InstanceModifiers.Add(new InstanceAttributeModifier(AttributeType.MaxHealth, 100));
        var service = new TemperingMechanicsService(Options.Create(new CraftingBalanceOptions
        {
            CriticalChanceBase = 1d,
            CriticalChancePerRarityStep = 0d,
            CriticalLevelingItemChance = 0d
        }));

        var result = service.ApplyTemperingAttempt(equipment, CreateProfile(), new FixedRandom(0.5d));

        Assert.True(result.QualityIncreased);
        Assert.Equal(
            EquipmentStatBudgetCatalog.Get(AttributeType.Power, equipment.Tier).HardCap,
            equipment.InstanceModifiers.Single(x => x.AttributeType == AttributeType.Power).Amount);
        Assert.True(
            equipment.InstanceModifiers.Single(x => x.AttributeType == AttributeType.MaxHealth).Amount > 100);
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

    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }
}
