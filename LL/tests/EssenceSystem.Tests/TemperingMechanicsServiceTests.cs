using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Options;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class TemperingMechanicsServiceTests
{
    [Fact]
    public void ApplyTemperingAttempt_WhenRarityDoesNotIncrease_DoesNotAddInstanceModifier()
    {
        var service = new TemperingMechanicsService();
        var equipment = CreateEquipment();
        var profile = CreateProfile();

        var result = service.ApplyTemperingAttempt(equipment, profile, new FixedRandom(0.50d));

        Assert.False(result.RarityUpgraded);
        Assert.Equal(Rarity.Common, equipment.Rarity);
        Assert.Equal(0, equipment.ItemXp);
        Assert.Equal(9, equipment.Potential);
        Assert.Empty(equipment.InstanceModifiers);
    }

    [Fact]
    public void ApplyTemperingAttempt_WhenRarityIncreases_AddsRarityUpgradeReward()
    {
        var service = new TemperingMechanicsService();
        var equipment = CreateEquipment();
        equipment.ItemXp = 9;
        var profile = CreateProfile();

        var result = service.ApplyTemperingAttempt(equipment, profile, new FixedRandom(0.01d));

        Assert.True(result.RarityUpgraded);
        Assert.Equal(Rarity.Uncommon, equipment.Rarity);
        Assert.Equal(0, equipment.ItemXp);
        var modifier = Assert.Single(equipment.InstanceModifiers);
        Assert.Equal(AttributeType.Armor, modifier.AttributeType);
        Assert.Equal(4, modifier.Amount);
    }

    [Fact]
    public void ApplyTemperingAttempt_WhenQualityIncreaseRollSucceeds_IncreasesQualityWithoutRestoringPotential()
    {
        var service = new TemperingMechanicsService(Options.Create(new CraftingBalanceOptions
        {
            QualityIncreaseChanceOnTemper = 1d
        }));
        var equipment = CreateEquipment();
        equipment.Quality = ItemQuality.Fine;
        equipment.MaxPotential = 10;
        equipment.InstanceModifiers.Add(new InstanceAttributeModifier(AttributeType.Armor, 100));
        var profile = CreateProfile();

        var result = service.ApplyTemperingAttempt(equipment, profile, new FixedRandom(0.01d));

        Assert.True(result.QualityIncreased);
        Assert.Equal(ItemQuality.Fine, result.PreviousQuality);
        Assert.Equal(ItemQuality.Exceptional, result.NewQuality);
        Assert.Equal(ItemQuality.Exceptional, equipment.Quality);
        Assert.Equal(9, equipment.Potential);
        Assert.Equal(10, equipment.MaxPotential);
        Assert.Equal(104, equipment.InstanceModifiers.Single().Amount);
    }

    [Fact]
    public void ApplyTemperingAttempt_WhenQualityAlreadyMaximum_DoesNotIncreaseQuality()
    {
        var service = new TemperingMechanicsService(Options.Create(new CraftingBalanceOptions
        {
            QualityIncreaseChanceOnTemper = 1d
        }));
        var equipment = CreateEquipment();
        equipment.Quality = ItemQuality.Masterwork;
        var profile = CreateProfile();

        var result = service.ApplyTemperingAttempt(equipment, profile, new FixedRandom(0.01d));

        Assert.False(result.QualityIncreased);
        Assert.Null(result.PreviousQuality);
        Assert.Null(result.NewQuality);
        Assert.Equal(ItemQuality.Masterwork, equipment.Quality);
        Assert.Equal(9, equipment.Potential);
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
        Id = "armor_fortification",
        Name = "Armor Fortification",
        StatImprovementPool =
        [
            new WeightedStatDefinition
            {
                Stat = AttributeType.Fortitude,
                Weight = 100
            }
        ],
        ResolvedAffixPool =
        [
            new WeightedAffixDefinition
            {
                Id = "armor",
                Name = "Armor",
                MinRarity = Rarity.Uncommon,
                Weight = 100,
                StatModifier = new WeightedStatDefinition
                {
                    Stat = AttributeType.Armor,
                    Weight = 2
                }
            }
        ]
    };

    private sealed class FixedRandom(double nextDouble) : Random
    {
        public override double NextDouble() => nextDouble;

        public override int Next(int maxValue) => 0;

        public override int Next(int minValue, int maxValue) => minValue;
    }
}
