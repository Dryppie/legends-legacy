using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Domain.Models.Attributes;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Configuration;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class CraftingCompositionContentTests
{
    [Fact]
    public void ProviderLoadsAndValidatesConcreteRecipesAndReusableBlueprints()
    {
        var provider = CreateProvider();

        Assert.Equal(31, provider.GetRecipes().Count);
        Assert.Equal(11, provider.GetBlueprints().Count);
        Assert.Equal(31, provider.GetRecipes().Select(recipe => recipe.OutputItemId).Distinct().Count());
    }

    [Fact]
    public void CraftingFamiliesContainOnlyTheRequestedRecipes()
    {
        var recipes = CreateProvider().GetRecipes();
        var expectedByFamily = new Dictionary<CraftType, string[]>
        {
            [CraftType.JewelryCrafting] = ["Amulet", "Relic", "Ring"],
            [CraftType.ArmorForging] =
            [
                "Cloth Cowl",
                "Cloth Pants",
                "Cloth Robe",
                "Heavy Breastplate",
                "Heavy Helm",
                "Heavy Legplates",
                "Light Hood",
                "Light Legwraps",
                "Light Vest",
                "Medium Greaves",
                "Medium Helm",
                "Medium Mail"
            ],
            [CraftType.WeaponSmithing] =
            [
                "Battle Axe",
                "Crossbow",
                "Dagger",
                "Gauntlets",
                "Greatsword",
                "Grimoire",
                "Hand Axe",
                "Longbow",
                "Mace",
                "Maul",
                "Shortsword",
                "Spear",
                "Spiritward",
                "Staff",
                "Towershield",
                "Wand"
            ]
        };

        Assert.Equal(expectedByFamily.Keys.Order(), recipes.Select(recipe => recipe.Category).Distinct().Order());
        foreach (var (family, expectedNames) in expectedByFamily)
        {
            Assert.Equal(
                expectedNames.Order(StringComparer.Ordinal),
                recipes
                    .Where(recipe => recipe.Category == family)
                    .Select(recipe => recipe.Name)
                    .Order(StringComparer.Ordinal));
        }
    }

    [Fact]
    public void EveryRecipeDefinesItsOutputStatsBehaviorAndTempering()
    {
        Assert.All(CreateProvider().GetRecipes(), recipe =>
        {
            Assert.StartsWith("recipe.", recipe.Id);
            Assert.NotEmpty(recipe.OutputItemId);
            Assert.NotEmpty(recipe.InitialStatProfile);
            Assert.NotEmpty(recipe.TemperingProfile.Stats);
            if (recipe.Tags.Contains("Weapon"))
            {
                Assert.NotEmpty(recipe.Behavior.Handedness);
                Assert.NotEmpty(recipe.Behavior.AttackCategory);
            }
        });
    }

    [Fact]
    public void RepresentativeHandPeersRetainCalibratedCombatBehavior()
    {
        var recipes = CreateProvider().GetRecipes();
        var dagger = recipes.Single(x => x.Id == "recipe.weapon.one_handed.dagger");
        var greatsword = recipes.Single(x => x.Id == "recipe.weapon.two_handed.greatsword");
        var gauntlets = recipes.Single(x => x.Id == "recipe.weapon.two_handed.gauntlets");
        var maul = recipes.Single(x => x.Id == "recipe.weapon.two_handed.maul");

        Assert.Equal(0.75d, dagger.Behavior.BasicAttackIntervalMultiplier);
        Assert.Equal(0.78d, dagger.Behavior.BasicAttackDamageMultiplier);
        Assert.Equal(1d, greatsword.Behavior.BasicAttackIntervalMultiplier);
        Assert.Equal(1.02d, greatsword.Behavior.BasicAttackDamageMultiplier);
        Assert.Equal(0.75d, gauntlets.Behavior.BasicAttackIntervalMultiplier);
        Assert.Equal(0.865d, gauntlets.Behavior.BasicAttackDamageMultiplier);
        Assert.Equal(1.25d, maul.Behavior.BasicAttackIntervalMultiplier);
        Assert.Equal(1.18d, maul.Behavior.BasicAttackDamageMultiplier);
    }

    [Fact]
    public void CalibratedBlueprintProfilesRetainTheirReviewedInfluenceAndWeights()
    {
        var provider = CreateProvider();
        var execution = provider.GetBlueprint("blueprint_execution")!;
        var aegis = provider.GetBlueprint("blueprint_aegis")!;

        Assert.Equal(0.2d, execution.StatProfileInfluence);
        Assert.Equal(
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.4d,
                [AttributeType.CritDamage] = 0.1d,
                [AttributeType.CritChance] = 0.2d,
                [AttributeType.WeaponDamage] = 0.3d
            },
            execution.StatProfile);
        Assert.Equal(
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Armor] = 0.25d,
                [AttributeType.Resistance] = 0.25d,
                [AttributeType.MaxHealth] = 0.35d,
                [AttributeType.DamageReduction] = 0.15d
            },
            aegis.StatProfile);
    }

    [Fact]
    public void BlueprintsComposeAcrossCompatibleRecipesWithoutAuthoredCombinations()
    {
        var provider = CreateProvider();
        var venom = provider.GetBlueprint("blueprint_venom")!;
        var weapons = provider.GetRecipes().Where(recipe => recipe.Tags.Contains("Weapon")).ToList();

        Assert.NotEmpty(weapons);
        Assert.All(weapons, recipe =>
        {
            Assert.True(EquipmentCraftingDesignComposer.IsCompatible(recipe, venom));
            var design = EquipmentCraftingDesignComposer.Compose(recipe, venom);
            Assert.Contains("Venom-Touched", design.Name);
            Assert.NotEmpty(design.InitialStatProfile);
            Assert.NotEmpty(design.TemperingProfile.Stats);
        });
    }

    [Fact]
    public void ExactBlueprintCanRemainNarrow()
    {
        var provider = CreateProvider();
        var hivefang = provider.GetBlueprint("blueprint_hive")!;
        var compatible = provider.GetRecipes()
            .Where(recipe => EquipmentCraftingDesignComposer.IsCompatible(recipe, hivefang))
            .ToList();

        var dagger = Assert.Single(compatible);
        Assert.Equal("recipe.weapon.one_handed.dagger", dagger.Id);
    }

    [Fact]
    public void EveryRecipeHasSeveralBlueprintChoicesWithoutAuthoredCombinationContent()
    {
        var provider = CreateProvider();

        Assert.All(provider.GetRecipes(), recipe =>
            Assert.True(
                provider.GetBlueprints().Count(blueprint =>
                    EquipmentCraftingDesignComposer.IsCompatible(recipe, blueprint)) >= 5,
                $"{recipe.Name} should have at least five reusable Blueprint choices."));
    }

    [Fact]
    public void LegacyRecipeVariantCatalogIsNoLongerRuntimeContent()
    {
        Assert.Throws<FileNotFoundException>(() => ReadArray("crafting/recipe-variants.json"));
        Assert.Equal(31, ReadArray("crafting/base-recipes.json").Count);
    }

    internal static JsonArray ReadArray(string relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            foreach (var root in new[]
            {
                Path.Combine(current.FullName, "LL", "src", "API", "API.LL", "Data"),
                Path.Combine(current.FullName, "src", "API", "API.LL", "Data")
            })
            {
                var candidate = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate))
                    return JsonNode.Parse(File.ReadAllText(candidate))!.AsArray();
            }

            current = current.Parent;
        }

        throw new FileNotFoundException(relativePath);
    }

    private static JsonCraftingDefinitionProvider CreateProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "." })
            .Build();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return new JsonCraftingDefinitionProvider(configuration, FindDataRoot(), options);
    }

    private static string FindDataRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(current.FullName, "LL", "src", "API", "API.LL", "Data"),
                Path.Combine(current.FullName, "src", "API", "API.LL", "Data")
            })
            {
                if (Directory.Exists(candidate))
                    return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Crafting data root not found.");
    }
}
