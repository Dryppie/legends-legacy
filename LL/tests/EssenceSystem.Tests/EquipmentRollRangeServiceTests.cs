using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class EquipmentRollRangeServiceTests
{
    [Fact]
    public void Resolve_EnclosesTheOriginalCraftingRolls()
    {
        var definitions = CreateProvider();
        var recipe = definitions.GetRecipes().First(candidate => candidate.InitialStatProfile.Count > 0);
        var equipmentBase = definitions.GetEquipmentBases()[recipe.OutputItemId];
        var design = EquipmentCraftingDesignComposer.Compose(recipe, null);
        var balance = Options.Create(new CraftingBalanceOptions());
        var statRolls = new ItemStatRollService(balance);
        var potentialService = new ItemPotentialService(balance);
        var potential = potentialService.CalculateStartingPotential(
            equipmentBase,
            1,
            ItemQuality.Fine,
            masteryLevel: 10,
            craftingLevel: 1);
        var equipment = new EquipmentInstance
        {
            ItemBase = equipmentBase,
            ItemBaseId = equipmentBase.Id,
            BaseRecipeId = recipe.Id,
            Tier = 1,
            Quality = ItemQuality.Fine,
            Potential = potential,
            MaxPotential = potential,
            InstanceModifiers =
            [
                .. statRolls.RollBaseStats(
                    equipmentBase,
                    design,
                    1,
                    ItemQuality.Fine,
                    new FixedRandom(0.5d))
            ]
        };
        var service = new EquipmentRollRangeService(definitions, statRolls, balance);

        var range = service.Resolve(equipment);

        Assert.NotNull(range);
        Assert.True(range.MinimumPotential <= potential);
        Assert.True(range.MaximumPotential >= potential);
        foreach (var modifier in equipment.InstanceModifiers)
        {
            var attributeRange = Assert.Single(
                range.Attributes,
                candidate => candidate.AttributeType == modifier.AttributeType);
            Assert.InRange(
                modifier.Amount,
                attributeRange.MinimumAmount,
                attributeRange.MaximumAmount);
        }
    }

    private static JsonCraftingDefinitionProvider CreateProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "."
            })
            .Build();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return new JsonCraftingDefinitionProvider(configuration, FindDataRoot(), options);
    }

    private static string FindDataRoot([CallerFilePath] string sourceFile = "")
    {
        var sourceCandidate = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(sourceFile)!,
            "..",
            "..",
            "src",
            "API",
            "API.LL",
            "Data"));
        if (Directory.Exists(sourceCandidate)) return sourceCandidate;

        foreach (var candidate in new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "LL", "src", "API", "API.LL", "Data"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "API", "API.LL", "Data")
        })
        {
            if (Directory.Exists(candidate)) return candidate;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(current.FullName, "LL", "src", "API", "API.LL", "Data"),
                Path.Combine(current.FullName, "src", "API", "API.LL", "Data")
            })
            {
                if (Directory.Exists(candidate)) return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Crafting data root not found.");
    }

    private sealed class FixedRandom(double nextDouble) : Random
    {
        public override double NextDouble() => nextDouble;
    }
}
