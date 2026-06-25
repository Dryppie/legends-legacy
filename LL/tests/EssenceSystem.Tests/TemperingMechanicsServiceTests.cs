using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class TemperingMechanicsServiceTests
{
    [Fact]
    public void ApplyTemperingAttempt_WhenRarityDoesNotIncrease_DoesNotAddInstanceModifier()
    {
        var service = new TemperingMechanicsService(new TestCraftingDefinitionProvider(
            new Dictionary<Rarity, int> { [Rarity.Uncommon] = 100 }));
        var equipment = CreateEquipment();
        var profile = CreateProfile();

        var result = service.ApplyTemperingAttempt(equipment, profile, new FixedRandom(0.50d));

        Assert.False(result.RarityUpgraded);
        Assert.Equal(Rarity.Common, equipment.Rarity);
        Assert.Equal(10, equipment.TemperingProgress);
        Assert.Equal(9, equipment.Potential);
        Assert.Empty(equipment.InstanceModifiers);
    }

    [Fact]
    public void ApplyTemperingAttempt_WhenRarityIncreases_AddsRarityUpgradeReward()
    {
        var service = new TemperingMechanicsService(new TestCraftingDefinitionProvider(
            new Dictionary<Rarity, int> { [Rarity.Uncommon] = 10 }));
        var equipment = CreateEquipment();
        var profile = CreateProfile();

        var result = service.ApplyTemperingAttempt(equipment, profile, new FixedRandom(0.50d));

        Assert.True(result.RarityUpgraded);
        Assert.Equal(Rarity.Uncommon, equipment.Rarity);
        var modifier = Assert.Single(equipment.InstanceModifiers);
        Assert.Equal(AttributeType.Armor, modifier.AttributeType);
        Assert.Equal(4, modifier.Amount);
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
        ProgressOnOutcome = new Dictionary<TemperingOutcomeType, int>
        {
            [TemperingOutcomeType.CriticalFail] = 0,
            [TemperingOutcomeType.Fail] = 0,
            [TemperingOutcomeType.Success] = 10,
            [TemperingOutcomeType.GreatSuccess] = 20
        },
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

    private sealed class TestCraftingDefinitionProvider(
        IReadOnlyDictionary<Rarity, int> temperingProgressThresholds) : ICraftingDefinitionProvider
    {
        public IReadOnlyList<MaterialDefinition> GetMaterials() => [];

        public IReadOnlyList<CraftingRecipeDefinition> GetRecipes() => [];

        public IReadOnlyList<BlueprintDefinition> GetBlueprints() => [];

        public IReadOnlyDictionary<Rarity, int> GetTemperingProgressThresholds() => temperingProgressThresholds;

        public MaterialDefinition? GetStandardMaterial(MaterialFamily family, int tier) => null;

        public MaterialDefinition? GetMaterialByItemId(string itemId) => null;

        public CraftingRecipeDefinition? GetRecipe(string recipeId) => null;

        public BlueprintDefinition? GetBlueprint(string blueprintId) => null;

        public BlueprintDefinition? GetBlueprintByItemId(string itemId) => null;

    }
}
