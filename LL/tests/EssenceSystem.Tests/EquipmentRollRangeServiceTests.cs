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

    [Fact]
    public void Resolve_EnclosesRollsAfterRarityUpgrades()
    {
        var definitions = CreateProvider();
        var balance = Options.Create(new CraftingBalanceOptions
        {
            CriticalChanceBase = 0d,
            CriticalChancePerRarityStep = 0d
        });
        var statRolls = new ItemStatRollService(balance);
        var tempering = new TemperingMechanicsService(balance);
        var service = new EquipmentRollRangeService(definitions, statRolls, balance);
        var recipes = definitions
            .GetRecipes()
            .Where(candidate => candidate.InitialStatProfile.Count > 0)
            .ToList();
        Assert.NotEmpty(recipes);
        var observedGrowthBeyondTheCraftedRange = false;

        foreach (var recipe in recipes)
        {
            var equipmentBase = definitions.GetEquipmentBases()[recipe.OutputItemId];
            var design = EquipmentCraftingDesignComposer.Compose(recipe, null);
            var equipment = CreateCraftedInstance(recipe, equipmentBase, design, statRolls);
            var profile = design.TemperingProfile;
            var craftedRange = service.Resolve(equipment);
            Assert.NotNull(craftedRange);

            while (equipment.Rarity < Rarity.Legendary)
            {
                // Nine XP means the next positive attempt tips the item over the rarity
                // threshold, which is what writes a directed improvement into the instance.
                equipment.ItemXp = 9;
                var previousRarity = equipment.Rarity;
                tempering.ApplyTemperingAttempt(equipment, profile, new FixedRandom(0.0005d));
                Assert.True(
                    equipment.Rarity > previousRarity,
                    $"{recipe.Id}: tempering did not advance rarity beyond {previousRarity}.");

                var range = service.Resolve(equipment);
                Assert.NotNull(range);
                foreach (var modifier in equipment.InstanceModifiers)
                {
                    var attributeRange = range.Attributes.SingleOrDefault(candidate =>
                        candidate.AttributeType == modifier.AttributeType);

                    // Rarity overflow can introduce a stat the recipe never designs for.
                    // Those have no advertised range, so the client renders none either.
                    if (attributeRange is null) continue;

                    Assert.True(
                        modifier.Amount >= attributeRange.MinimumAmount
                        && modifier.Amount <= attributeRange.MaximumAmount,
                        $"{recipe.Id} at {equipment.Rarity}: {modifier.AttributeType} "
                        + $"{modifier.Amount} is outside "
                        + $"{attributeRange.MinimumAmount}-{attributeRange.MaximumAmount}.");

                    var craftedMaximum = craftedRange.Attributes
                        .Single(candidate => candidate.AttributeType == modifier.AttributeType)
                        .MaximumAmount;
                    observedGrowthBeyondTheCraftedRange |= modifier.Amount > craftedMaximum;
                }
            }
        }

        // Guards the regression itself: rarity upgrades really do push at least one
        // attribute past the range the item was crafted against, so a range that ignores
        // rarity would have failed the assertions above.
        Assert.True(
            observedGrowthBeyondTheCraftedRange,
            "No rarity upgrade exceeded its crafted maximum; the test no longer covers the bug.");
    }

    private static EquipmentInstance CreateCraftedInstance(
        CraftingRecipeDefinition recipe,
        EquipmentBase equipmentBase,
        EquipmentCraftingDesign design,
        ItemStatRollService statRolls) => new()
    {
        ItemBase = equipmentBase,
        ItemBaseId = equipmentBase.Id,
        BaseRecipeId = recipe.Id,
        Tier = 1,
        Quality = ItemQuality.Standard,
        Rarity = Rarity.Common,
        Potential = 100,
        MaxPotential = 100,
        StatModelVersion = EquipmentStatBudgetCatalog.BalanceVersion,
        InstanceModifiers =
        [
            .. statRolls.RollBaseStats(
                equipmentBase,
                design,
                1,
                ItemQuality.Standard,
                new FixedRandom(0.5d))
        ]
    };

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
