using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Services.LL.Dungeons;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class DungeonSimulationEquipmentFactoryTests
{
    private static readonly (string SlotId, EquipmentType EquipmentType)[] Slots =
    [
        ("Head", EquipmentType.Head),
        ("Chest", EquipmentType.Chest),
        ("Legs", EquipmentType.Legs),
        ("Ring", EquipmentType.Ring),
        ("Necklace", EquipmentType.Necklace),
        ("Relic", EquipmentType.Relic),
        ("MainHand", EquipmentType.OneHanded),
        ("OffHand", EquipmentType.OffHand)
    ];

    private readonly DungeonSimulationEquipmentFactory _factory = CreateFactory();

    [Fact]
    public void Simulation_equipment_uses_real_recipe_rolls_and_tempering()
    {
        foreach (var (slotId, equipmentType) in Slots)
        {
            var common = _factory.Create(slotId, equipmentType, Rarity.Common);
            var epic = _factory.Create(slotId, equipmentType, Rarity.Epic);

            Assert.Equal(1, common.Tier);
            Assert.Equal(ItemQuality.Standard, common.Quality);
            Assert.Equal(Rarity.Common, common.Rarity);
            Assert.Equal(Rarity.Epic, epic.Rarity);
            Assert.False(string.IsNullOrWhiteSpace(common.BaseRecipeId));
            Assert.Empty(common.BaseModifiers);
            Assert.NotEmpty(common.InstanceModifiers);
            Assert.True(
                EquipmentBudgetEvaluator.Evaluate(epic.AttributeModifiers, epic.Tier) >
                EquipmentBudgetEvaluator.Evaluate(common.AttributeModifiers, common.Tier));
        }
    }

    [Fact]
    public void Simulation_equipment_and_preview_are_deterministic()
    {
        var first = _factory.Create("Chest", EquipmentType.Chest, Rarity.Epic);
        var second = _factory.Create("Chest", EquipmentType.Chest, Rarity.Epic);
        var preview = _factory.GetAttributeBonuses(
            "Chest",
            EquipmentType.Chest,
            Rarity.Epic);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.ItemBaseId, second.ItemBaseId);
        Assert.Equal(first.BaseRecipeId, second.BaseRecipeId);
        Assert.Equal(
            first.InstanceModifiers.Select(modifier =>
                (modifier.AttributeType, modifier.Amount, modifier.ModifierType)),
            second.InstanceModifiers.Select(modifier =>
                (modifier.AttributeType, modifier.Amount, modifier.ModifierType)));
        Assert.Equal(
            first.InstanceModifiers
                .GroupBy(modifier => modifier.AttributeType)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(modifier => modifier.Amount)),
            preview);
    }

    private static DungeonSimulationEquipmentFactory CreateFactory()
    {
        var apiRoot = FindApiRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data"
            })
            .Build();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        var balance = Options.Create(new CraftingBalanceOptions());

        return new DungeonSimulationEquipmentFactory(
            new JsonCraftingDefinitionProvider(configuration, apiRoot, jsonOptions),
            new ItemStatRollService(balance),
            new TemperingMechanicsService(balance));
    }

    private static string FindApiRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(current.FullName, "src", "API", "API.LL"),
                Path.Combine(current.FullName, "LL", "src", "API", "API.LL")
            })
            {
                if (Directory.Exists(Path.Combine(candidate, "Data")))
                    return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the API.LL content root.");
    }
}
