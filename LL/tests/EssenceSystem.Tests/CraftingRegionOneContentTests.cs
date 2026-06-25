using System.Text.Json.Nodes;

namespace EssenceSystem.Tests;

public sealed class CraftingRegionOneContentTests
{
    [Fact]
    public void RegionOneDungeons_SourceEveryStandardCraftingMaterial()
    {
        var materials = ReadArray("crafting/materials.json");
        var dungeons = ReadArray("dungeons.json");

        var gatheredItemIds = dungeons
            .SelectMany(dungeon => ChildArray(dungeon, "gatheringNodes"))
            .SelectMany(node => ChildArray(node, "loot"))
            .Select(loot => loot?["itemId"]?.GetValue<string>())
            .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = materials
            .Where(material => material?["isStandardTieredMaterial"]?.GetValue<bool>() == true)
            .Select(material => material?["itemId"]?.GetValue<string>() ?? string.Empty)
            .Where(itemId => !gatheredItemIds.Contains(itemId))
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void RegionOneDungeons_SourceEveryBlueprintAndSpecialResource()
    {
        var materials = ReadArray("crafting/materials.json");
        var blueprints = ReadArray("crafting/blueprints.json");
        var dungeons = ReadArray("dungeons.json");

        var firstClearItemIds = dungeons
            .SelectMany(dungeon => ChildArray(dungeon?["rewardTable"], "firstClearRewards"))
            .Select(reward => reward?["itemId"]?.GetValue<string>())
            .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sourcedItemIds = dungeons
            .SelectMany(dungeon => ChildArray(dungeon, "gatheringNodes"))
            .SelectMany(node => ChildArray(node, "loot"))
            .Select(loot => loot?["itemId"]?.GetValue<string>())
            .Concat(dungeons.SelectMany(dungeon =>
                new[] { "firstClearRewards", "completionRewards", "bonusRewards" }
                    .SelectMany(list => ChildArray(dungeon?["rewardTable"], list)))
                    .Select(reward => reward?["itemId"]?.GetValue<string>()))
            .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingBlueprints = blueprints
            .Select(blueprint => blueprint?["itemId"]?.GetValue<string>() ?? string.Empty)
            .Where(itemId => !firstClearItemIds.Contains(itemId))
            .ToList();

        var missingSpecialResources = materials
            .Where(material => material?["isSpecialResource"]?.GetValue<bool>() == true)
            .Select(material => material?["itemId"]?.GetValue<string>() ?? string.Empty)
            .Where(itemId => !sourcedItemIds.Contains(itemId))
            .ToList();

        Assert.Empty(missingBlueprints);
        Assert.Empty(missingSpecialResources);
    }

    [Fact]
    public void CraftingV2_HasExpandedTemperingPlate()
    {
        var temperingRecipes = ReadArray("crafting/tempering-recipes.json");

        Assert.True(temperingRecipes.Count >= 15);
        Assert.Contains(temperingRecipes, recipe => recipe?["id"]?.GetValue<string>() == "shield_reinforcement");
        Assert.Contains(temperingRecipes, recipe => recipe?["id"]?.GetValue<string>() == "caster_focusing");
        Assert.Contains(temperingRecipes, recipe => recipe?["id"]?.GetValue<string>() == "hive_chitin_lacquering");
    }

    [Fact]
    public void CraftingV2_TemperingRecipes_UseExternalModifierCatalogs()
    {
        var temperingRecipes = ReadArray("crafting/tempering-recipes.json");
        var affixes = ReadArray("crafting/affixes.json");
        var specialModifiers = ReadArray("crafting/special-modifiers.json");
        var tierBudgets = ReadArray("crafting/tier-budgets.json");

        Assert.True(affixes.Count >= 30);
        Assert.True(specialModifiers.Count >= 9);

        var affixIds = affixes
            .Select(affix => affix?["id"]?.GetValue<string>() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var specialModifierIds = specialModifiers
            .Select(modifier => modifier?["id"]?.GetValue<string>() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var recipe in temperingRecipes)
        {
            foreach (var affixRef in ChildArray(recipe, "affixPool"))
            {
                Assert.Contains(affixRef?["id"]?.GetValue<string>() ?? string.Empty, affixIds);
                Assert.False(affixRef?.AsObject().ContainsKey("name") == true);
                Assert.False(affixRef?.AsObject().ContainsKey("statModifier") == true);
            }

            foreach (var specialModifierRef in ChildArray(recipe, "specialModifierPool"))
            {
                Assert.Contains(specialModifierRef?["id"]?.GetValue<string>() ?? string.Empty, specialModifierIds);
                Assert.False(specialModifierRef?.AsObject().ContainsKey("name") == true);
                Assert.False(specialModifierRef?.AsObject().ContainsKey("statModifier") == true);
            }
        }

        var budgetRarities = tierBudgets
            .Select(budget => budget?["rarity"]?.GetValue<string>() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("Uncommon", budgetRarities);
        Assert.Contains("Rare", budgetRarities);
        Assert.Contains("Epic", budgetRarities);
        Assert.Contains("Legendary", budgetRarities);
    }

    private static JsonArray ReadArray(string relativePath)
    {
        var dataRoot = FindDataRoot();
        var path = Path.Combine(dataRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var json = File.ReadAllText(path);
        return JsonNode.Parse(json)?.AsArray()
            ?? throw new InvalidOperationException($"Unable to parse JSON array '{path}'.");
    }

    private static IEnumerable<JsonNode?> ChildArray(JsonNode? node, string propertyName) =>
        node?[propertyName]?.AsArray() ?? Enumerable.Empty<JsonNode?>();

    private static string FindDataRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "src", "API", "API.LL", "Data");
            if (Directory.Exists(candidate)) return candidate;
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find API.LL Data directory.");
    }
}
