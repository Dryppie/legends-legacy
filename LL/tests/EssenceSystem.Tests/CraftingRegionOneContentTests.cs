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
    public void CraftingV2_HasTemperingProfilesOnBlueprintsAndBaseRecipes()
    {
        var baseRecipes = ReadArray("crafting/base-recipes.json");
        var blueprints = ReadArray("crafting/blueprints.json");

        Assert.All(baseRecipes, recipe => Assert.NotNull(recipe?["temperingProfile"]));
        Assert.All(blueprints, blueprint => Assert.NotNull(blueprint?["temperingProfile"]));
    }

    [Fact]
    public void CraftingV2_TemperingProfiles_UseExternalModifierCatalogs()
    {
        var baseRecipes = ReadArray("crafting/base-recipes.json");
        var blueprints = ReadArray("crafting/blueprints.json");
        var affixes = ReadArray("crafting/affixes.json");
        var specialModifiers = ReadArray("crafting/special-modifiers.json");

        Assert.True(affixes.Count >= 30);
        Assert.True(specialModifiers.Count >= 9);

        var affixIds = affixes
            .Select(affix => affix?["id"]?.GetValue<string>() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var specialModifierIds = specialModifiers
            .Select(modifier => modifier?["id"]?.GetValue<string>() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in baseRecipes.Select(x => x?["temperingProfile"]).Concat(blueprints.Select(x => x?["temperingProfile"])))
        {
            Assert.NotNull(profile);

            foreach (var affixRef in ChildArray(profile, "affixPool"))
            {
                Assert.Contains(affixRef?["id"]?.GetValue<string>() ?? string.Empty, affixIds);
                Assert.False(affixRef?.AsObject().ContainsKey("name") == true);
                Assert.False(affixRef?.AsObject().ContainsKey("statModifier") == true);
            }

            foreach (var specialModifierRef in ChildArray(profile, "specialModifierPool"))
            {
                Assert.Contains(specialModifierRef?["id"]?.GetValue<string>() ?? string.Empty, specialModifierIds);
                Assert.False(specialModifierRef?.AsObject().ContainsKey("name") == true);
                Assert.False(specialModifierRef?.AsObject().ContainsKey("statModifier") == true);
            }
        }
    }

    [Fact]
    public void Dungeons_DoNotRewardCompletedEquipmentItems()
    {
        var items = ReadArray("items.json")
            .ToDictionary(
                item => item?["id"]?.GetValue<string>() ?? string.Empty,
                item => item?["itemType"]?.GetValue<string>() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);
        var dungeons = ReadArray("dungeons.json");

        var completedEquipmentRewards = dungeons
            .SelectMany(GetDungeonRewardItemIds)
            .Where(itemId => items.TryGetValue(itemId, out var itemType) &&
                             itemType.Equals("Equipment", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(completedEquipmentRewards);
    }

    [Fact]
    public void Dungeons_DoNotRewardCatalogedCraftingMaterialsAboveTheirOwnTier()
    {
        var materialTierByItemId = ReadArray("crafting/materials.json")
            .Select(material => new
            {
                ItemId = material?["itemId"]?.GetValue<string>() ?? string.Empty,
                Tier = material?["tier"]?.GetValue<int?>()
            })
            .Where(material => !string.IsNullOrWhiteSpace(material.ItemId) && material.Tier.HasValue)
            .ToDictionary(material => material.ItemId, material => material.Tier!.Value, StringComparer.OrdinalIgnoreCase);
        var dungeons = ReadArray("dungeons.json");

        var invalidRewards = dungeons
            .SelectMany(dungeon =>
            {
                var dungeonTier = dungeon?["tier"]?.GetValue<int>() ?? 1;
                return GetDungeonRewardItemIds(dungeon)
                    .Where(itemId => materialTierByItemId.TryGetValue(itemId, out var materialTier) &&
                                     materialTier > dungeonTier)
                    .Select(itemId => $"{dungeon?["id"]?.GetValue<string>()}:{itemId}");
            })
            .ToList();

        Assert.Empty(invalidRewards);
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

    private static IEnumerable<string> GetDungeonRewardItemIds(JsonNode? dungeon)
    {
        var directRewards = new[] { "firstClearRewards", "completionRewards", "bonusRewards" }
            .SelectMany(list => ChildArray(dungeon?["rewardTable"], list));
        var gatheringRewards = ChildArray(dungeon, "gatheringNodes")
            .SelectMany(node => ChildArray(node, "loot"));

        return directRewards
            .Concat(gatheringRewards)
            .Select(reward => reward?["itemId"]?.GetValue<string>())
            .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
            .Select(itemId => itemId!);
    }

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
