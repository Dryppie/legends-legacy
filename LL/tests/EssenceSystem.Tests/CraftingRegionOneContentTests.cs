using System.Text.Json.Nodes;

namespace EssenceSystem.Tests;

public sealed class CraftingRegionOneContentTests
{
    [Fact]
    public void GatheringCatalog_UsesOnlyOreWoodAndHideAsStandardMaterials()
    {
        var materials = ReadArray("crafting/materials.json");
        var standardMaterials = materials
            .Where(material => material?["isStandardTieredMaterial"]?.GetValue<bool>() == true)
            .ToList();

        Assert.Equal(
            ["ore", "rawhide", "wood"],
            standardMaterials
                .Select(material => material?["itemId"]?.GetValue<string>() ?? string.Empty)
                .Order(StringComparer.OrdinalIgnoreCase));
        Assert.Equal(
            ["Hide", "Metal", "Wood"],
            standardMaterials
                .Select(material => material?["family"]?.GetValue<string>() ?? string.Empty)
                .Order(StringComparer.OrdinalIgnoreCase));

        var allowedFamilies = new HashSet<string>(["Metal", "Wood", "Hide"], StringComparer.OrdinalIgnoreCase);
        var invalidRecipeRequirements = ReadArray("crafting/base-recipes.json")
            .SelectMany(recipe => ChildArray(recipe, "materialRequirements"))
            .Select(requirement => requirement?["family"]?.GetValue<string>() ?? string.Empty)
            .Where(family => !allowedFamilies.Contains(family))
            .ToList();

        Assert.Empty(invalidRecipeRequirements);
    }

    [Fact]
    public void EveryCraftingMaterial_DescribesWhereItCanBeObtained()
    {
        Assert.All(ReadArray("crafting/materials.json"), material =>
        {
            var sources = ChildArray(material, "sources")
                .Select(source => source?.GetValue<string>() ?? string.Empty)
                .Where(source => !string.IsNullOrWhiteSpace(source))
                .ToList();

            Assert.NotEmpty(sources);
        });
    }

    [Fact]
    public void DungeonGathering_UsesThreeSkillsAndTheirMatchingBaseMaterials()
    {
        var specialResources = ReadArray("crafting/materials.json")
            .Where(material => material?["isSpecialResource"]?.GetValue<bool>() == true)
            .Select(material => material?["itemId"]?.GetValue<string>() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var expectedMaterialBySkill = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Mining"] = "ore",
            ["Woodcutting"] = "wood",
            ["Skinning"] = "rawhide"
        };

        foreach (var node in ReadDungeonDifficulties()
                     .SelectMany(dungeon => ChildArray(dungeon, "gatheringNodes")))
        {
            var skill = node?["type"]?.GetValue<string>() ?? string.Empty;
            Assert.True(expectedMaterialBySkill.TryGetValue(skill, out var expectedMaterial));

            var lootItemIds = ChildArray(node, "loot")
                .Select(loot => loot?["itemId"]?.GetValue<string>() ?? string.Empty)
                .ToList();

            Assert.Contains(expectedMaterial!, lootItemIds, StringComparer.OrdinalIgnoreCase);
            Assert.All(lootItemIds, itemId =>
                Assert.True(
                    itemId.Equals(expectedMaterial, StringComparison.OrdinalIgnoreCase) ||
                    specialResources.Contains(itemId),
                    $"Gathering node '{node?["id"]}' contains retired material '{itemId}'."));
        }
    }

    [Fact]
    public void RegionOneDungeons_SourceEveryStandardCraftingMaterial()
    {
        var materials = ReadArray("crafting/materials.json");
        var dungeons = ReadDungeonDifficulties();

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
        var dungeons = ReadDungeonDifficulties();

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
            .Where(blueprint =>
                blueprint?["enabled"]?.GetValue<bool>() == true &&
                blueprint?["sourceType"]?.GetValue<string>() == "Dungeon")
            .Select(blueprint => blueprint?["itemId"]?.GetValue<string>() ?? string.Empty)
            .Where(itemId => !firstClearItemIds.Contains(itemId))
            .ToList();

        var enabledDungeonCatalysts = blueprints
            .Where(blueprint =>
                blueprint?["enabled"]?.GetValue<bool>() == true &&
                blueprint?["sourceType"]?.GetValue<string>() == "Dungeon")
            .SelectMany(blueprint => ChildArray(blueprint, "additionalMaterialRequirements"))
            .Where(requirement => requirement?["type"]?.GetValue<string>() == "SpecialResource")
            .Select(requirement => requirement?["itemId"]?.GetValue<string>() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingSpecialResources = materials
            .Where(material =>
                material?["isSpecialResource"]?.GetValue<bool>() == true &&
                enabledDungeonCatalysts.Contains(material?["itemId"]?.GetValue<string>() ?? string.Empty))
            .Select(material => material?["itemId"]?.GetValue<string>() ?? string.Empty)
            .Where(itemId => !sourcedItemIds.Contains(itemId))
            .ToList();

        Assert.Empty(missingBlueprints);
        Assert.Empty(missingSpecialResources);
    }

    [Fact]
    public void EveryDungeonBlueprintCatalystIsSourcedByItsDungeonFamily()
    {
        var blueprints = ReadArray("crafting/blueprints.json");
        var dungeonDocument = ReadDocument("dungeons/dungeons.json");
        var sourcedByFamily = ChildArray(dungeonDocument, "families")
            .ToDictionary(
                family => family?["id"]?.GetValue<string>() ?? string.Empty,
                family => ChildArray(family, "difficulties")
                    .SelectMany(GetDungeonRewardItemIds)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

        var missing = blueprints
            .Where(blueprint =>
                blueprint?["enabled"]?.GetValue<bool>() == true &&
                blueprint?["sourceType"]?.GetValue<string>() == "Dungeon")
            .Select(blueprint => new
            {
                BlueprintId = blueprint?["id"]?.GetValue<string>() ?? string.Empty,
                FamilyId = blueprint?["sourceId"]?.GetValue<string>() ?? string.Empty,
                CatalystItemId = ChildArray(blueprint, "additionalMaterialRequirements")
                    .Single(requirement => requirement?["type"]?.GetValue<string>() == "SpecialResource")?
                    ["itemId"]?.GetValue<string>() ?? string.Empty
            })
            .Where(source =>
                !sourcedByFamily.TryGetValue(source.FamilyId, out var itemIds) ||
                !itemIds.Contains(source.CatalystItemId))
            .Select(source => $"{source.BlueprintId}:{source.CatalystItemId}@{source.FamilyId}")
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void Shenic_dungeon_sigils_describe_their_region_wide_drop_source()
    {
        var items = ReadArray("items/items.json")
            .ToDictionary(
                item => item?["id"]?.GetValue<string>() ?? string.Empty,
                item => item?["description"]?.GetValue<string>() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

        foreach (var sigilId in new[] { "sigil_goblin_mines", "sigil_forgotten_catacombs" })
        {
            Assert.True(items.TryGetValue(sigilId, out var description));
            Assert.Contains("every area of the Shenic Region", description, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void StandardMaterialSources_ListEveryActualCombatAreaAndExcludeTheBazaar()
    {
        var regionDocument = ReadDocument("world/regions.json");
        var areas = ChildArray(regionDocument, "regions")
            .SelectMany(region => ChildArray(region, "areas"))
            .ToList();
        var materials = ReadArray("crafting/materials.json")
            .Where(material => material?["isStandardTieredMaterial"]?.GetValue<bool>() == true)
            .ToDictionary(
                material => material?["family"]?.GetValue<string>() ?? string.Empty,
                material => string.Join(" ", ChildArray(material, "sources").Select(source => source?.GetValue<string>())),
                StringComparer.OrdinalIgnoreCase);
        var familyByGatheringType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Mining"] = "Metal",
            ["Woodcutting"] = "Wood",
            ["Skinning"] = "Hide"
        };

        Assert.All(materials.Values, source =>
            Assert.DoesNotContain("Cinder Bazaar", source, StringComparison.OrdinalIgnoreCase));

        foreach (var (gatheringType, family) in familyByGatheringType)
        {
            var expectedAreaNames = areas
                .Where(area => ChildArray(area, "gatheringNodes").Any(node =>
                    string.Equals(
                        node?["type"]?.GetValue<string>(),
                        gatheringType,
                        StringComparison.OrdinalIgnoreCase)))
                .Select(area => area?["name"]?.GetValue<string>() ?? string.Empty)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var listedAreaNames = areas
                .Select(area => area?["name"]?.GetValue<string>() ?? string.Empty)
                .Where(areaName => materials[family].Contains(areaName, StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Assert.InRange(expectedAreaNames.Length, 4, 5);
            Assert.Equal(expectedAreaNames, listedAreaNames);
        }
    }

    [Fact]
    public void CombatAreaGathering_YieldsAboutThirtyTwoAverageEquipmentCraftsPerDay()
    {
        const double encountersPerDay = 24 * 60 * 60 / 10d;
        var recipes = ReadArray("crafting/base-recipes.json");
        var averageTierOneRecipeCost = recipes.Average(recipe =>
            ChildArray(recipe, "materialRequirements")
                .Sum(requirement => requirement?["baseAmount"]?.GetValue<int>() ?? 0));
        var rewardTables = ChildArray(ReadDocument("rewards/reward-tables.json"), "rewardTables")
            .ToDictionary(
                table => table?["id"]?.GetValue<string>() ?? string.Empty,
                table => table,
                StringComparer.OrdinalIgnoreCase);
        var nodes = ChildArray(ReadDocument("world/regions.json"), "regions")
            .SelectMany(region => ChildArray(region, "areas"))
            .SelectMany(area => ChildArray(area, "gatheringNodes"))
            .ToList();

        Assert.NotEmpty(nodes);
        Assert.All(nodes, node =>
        {
            var rewardTableId = node?["rewardTableId"]?.GetValue<string>() ?? string.Empty;
            Assert.True(rewardTables.TryGetValue(rewardTableId, out var rewardTable));
            var quantities = ChildArray(rewardTable, "rolls")
                .SelectMany(roll => ChildArray(roll, "entries"))
                .Select(entry => entry?["quantity"])
                .Where(quantity => quantity is not null)
                .Select(quantity =>
                    ((quantity?["min"]?.GetValue<int>() ?? 0) +
                     (quantity?["max"]?.GetValue<int>() ?? 0)) / 2d)
                .ToArray();
            var averageYield = Assert.Single(quantities);
            var procChance = node?["procChance"]?.GetValue<double>() ?? 0;
            var expectedCrafts = encountersPerDay * procChance * averageYield / averageTierOneRecipeCost;

            Assert.InRange(expectedCrafts, 31.2, 32.8);
        });
    }

    [Fact]
    public void DungeonCraftingResourceDropsUseDoubledAmounts()
    {
        var materials = ReadArray("crafting/materials.json");
        var standardMaterialIds = materials
            .Where(material => material?["isStandardTieredMaterial"]?.GetValue<bool>() == true)
            .Select(material => material?["itemId"]?.GetValue<string>() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var specialResourceIds = materials
            .Where(material => material?["isSpecialResource"]?.GetValue<bool>() == true)
            .Select(material => material?["itemId"]?.GetValue<string>() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var dungeons = ReadDungeonDifficulties();

        var gatheringDrops = dungeons
            .SelectMany(dungeon => ChildArray(dungeon, "gatheringNodes"))
            .SelectMany(node => ChildArray(node, "loot"))
            .Where(drop => standardMaterialIds.Contains(
                drop?["itemId"]?.GetValue<string>() ?? string.Empty))
            .ToList();
        Assert.Equal(15, gatheringDrops.Count);
        Assert.All(gatheringDrops, drop =>
        {
            Assert.Equal(8, drop?["minQuantity"]?.GetValue<int>());
            var maxQuantity = drop?["maxQuantity"]?.GetValue<int>() ?? 0;
            Assert.Contains(
                maxQuantity,
                new[] { 16, 24, 32 });
        });

        var catalystDrops = dungeons
            .SelectMany(dungeon => ChildArray(dungeon?["rewardTable"], "completionRewards"))
            .Where(drop => specialResourceIds.Contains(
                drop?["itemId"]?.GetValue<string>() ?? string.Empty))
            .ToList();
        Assert.Equal(11, catalystDrops.Count);
        Assert.All(catalystDrops, drop =>
        {
            Assert.Equal(2, drop?["minAmount"]?.GetValue<int>());
            Assert.Equal(2, drop?["maxAmount"]?.GetValue<int>());
        });
    }

    [Fact]
    public void FormerHivesAbyssBlueprints_AreMigratedToLiveDungeonFamilies()
    {
        var expectedFamilyByBlueprintId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["blueprint_warden"] = "forgotten_catacombs",
            ["blueprint_primal"] = "forgotten_catacombs",
            ["blueprint_venom"] = "goblin_mines",
            ["blueprint_hive"] = "goblin_mines"
        };
        var migratedBlueprints = ReadArray("crafting/blueprints.json")
            .Where(blueprint => expectedFamilyByBlueprintId.ContainsKey(
                blueprint?["id"]?.GetValue<string>() ?? string.Empty))
            .ToList();

        Assert.Equal(expectedFamilyByBlueprintId.Count, migratedBlueprints.Count);
        Assert.All(migratedBlueprints, blueprint =>
        {
            var blueprintId = blueprint?["id"]?.GetValue<string>() ?? string.Empty;
            Assert.True(blueprint?["enabled"]?.GetValue<bool>());
            Assert.Equal("Dungeon", blueprint?["sourceType"]?.GetValue<string>());
            Assert.Equal(expectedFamilyByBlueprintId[blueprintId], blueprint?["sourceId"]?.GetValue<string>());
        });
    }

    [Fact]
    public void EveryBlueprintRequiresOneUniqueRegisteredCatalyst()
    {
        var blueprints = ReadArray("crafting/blueprints.json");
        var materials = ReadArray("crafting/materials.json")
            .ToDictionary(
                material => material?["itemId"]?.GetValue<string>() ?? string.Empty,
                material => material,
                StringComparer.OrdinalIgnoreCase);
        var items = ReadArray("items/items.json")
            .ToDictionary(
                item => item?["id"]?.GetValue<string>() ?? string.Empty,
                item => item,
                StringComparer.OrdinalIgnoreCase);
        var catalystItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.All(blueprints, blueprint =>
        {
            var catalyst = Assert.Single(
                ChildArray(blueprint, "additionalMaterialRequirements"),
                requirement =>
                    requirement?["type"]?.GetValue<string>() == "SpecialResource");
            var catalystItemId = catalyst?["itemId"]?.GetValue<string>() ?? string.Empty;

            Assert.False(string.IsNullOrWhiteSpace(catalystItemId));
            Assert.Equal(1, catalyst?["baseAmount"]?.GetValue<int>());
            Assert.Equal(0, catalyst?["amountPerTier"]?.GetValue<int>());
            Assert.True(
                catalystItemIds.Add(catalystItemId),
                $"Catalyst '{catalystItemId}' is assigned to more than one Blueprint.");
            Assert.True(materials.TryGetValue(catalystItemId, out var material));
            Assert.True(material?["isSpecialResource"]?.GetValue<bool>());
            Assert.True(items.TryGetValue(catalystItemId, out var item));
            Assert.Equal("Resource", item?["itemType"]?.GetValue<string>());
        });
    }

    [Fact]
    public void CraftingV2_RecipesAndBlueprintsDefineComposableTemperingProfiles()
    {
        var recipes = ReadArray("crafting/base-recipes.json");
        var blueprints = ReadArray("crafting/blueprints.json");

        Assert.All(recipes, recipe => Assert.NotEmpty(ChildArray(recipe?["temperingProfile"], "stats")));
        Assert.All(blueprints, blueprint => Assert.NotEmpty(ChildArray(blueprint?["temperingProfile"], "stats")));
    }

    [Fact]
    public void CraftingV2_DoesNotUseLegacyAffixOrSpecialModifierCatalogs()
    {
        var dataRoot = FindDataRoot();
        Assert.False(File.Exists(Path.Combine(dataRoot, "crafting", "affixes.json")));
        Assert.False(File.Exists(Path.Combine(dataRoot, "crafting", "special-modifiers.json")));

        foreach (var definition in ReadArray("crafting/base-recipes.json").Concat(ReadArray("crafting/blueprints.json")))
        {
            var profile = definition?["temperingProfile"];
            Assert.False(profile!.AsObject().ContainsKey("affixPool"));
            Assert.False(profile.AsObject().ContainsKey("specialModifierPool"));
        }
    }

    [Fact]
    public void Dungeons_DoNotRewardCompletedEquipmentItems()
    {
        var items = ReadArray("items/items.json")
            .ToDictionary(
                item => item?["id"]?.GetValue<string>() ?? string.Empty,
                item => item?["itemType"]?.GetValue<string>() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);
        var dungeons = ReadDungeonDifficulties();

        var completedEquipmentRewards = dungeons
            .SelectMany(GetDungeonRewardItemIds)
            .Where(itemId => items.TryGetValue(itemId, out var itemType) &&
                             itemType.Equals("Equipment", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(completedEquipmentRewards);
    }

    [Fact]
    public void RewardTablesDoNotDropFinishedNonToolEquipment()
    {
        var items = ReadArray("items/items.json")
            .ToDictionary(
                item => item!["id"]!.GetValue<string>(),
                item => new
                {
                    ItemType = item!["itemType"]!.GetValue<string>(),
                    EquipmentType = item["equipmentType"]?.GetValue<string>()
                },
                StringComparer.OrdinalIgnoreCase);
        var rewardDocument = ReadDocument("rewards/reward-tables.json");
        var rewardItemIds = ChildArray(rewardDocument, "rewardTables")
            .SelectMany(table => ChildArray(table, "rolls"))
            .SelectMany(roll => ChildArray(roll, "entries"))
            .Select(entry => entry?["itemId"]?.GetValue<string>())
            .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
            .Select(itemId => itemId!);

        var invalid = rewardItemIds
            .Where(itemId =>
                items.TryGetValue(itemId, out var item) &&
                item.ItemType.Equals("Equipment", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.EquipmentType, "Tool", StringComparison.OrdinalIgnoreCase) &&
                !itemId.StartsWith("tutorial_", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(invalid);
    }

    [Fact]
    public void DungeonRewardTablesDoNotDropAdvancementStones()
    {
        var dungeonTables = ChildArray(ReadDocument("rewards/reward-tables.json"), "rewardTables")
            .Where(table =>
                (table?["id"]?.GetValue<string>() ?? string.Empty)
                .StartsWith("reward.dungeon.", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(dungeonTables);
        Assert.DoesNotContain(
            dungeonTables
                .SelectMany(table => ChildArray(table, "rolls"))
                .SelectMany(roll => ChildArray(roll, "entries")),
            entry => string.Equals(
                entry?["itemId"]?.GetValue<string>(),
                "advancement_stone",
                StringComparison.OrdinalIgnoreCase));

        var expectedTierNoDropWeights = new Dictionary<string, double>
        {
            ["reward.dungeon.tier.1"] = 80,
            ["reward.dungeon.tier.2"] = 78,
            ["reward.dungeon.tier.3"] = 72.2
        };
        foreach (var (tableId, expectedNoDropWeight) in expectedTierNoDropWeights)
        {
            var table = Assert.Single(dungeonTables, candidate =>
                candidate?["id"]?.GetValue<string>() == tableId);
            var roll = Assert.Single(ChildArray(table, "rolls"));
            Assert.Equal(expectedNoDropWeight, roll?["noDropWeight"]?.GetValue<double>());
        }
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
        var dungeons = ReadDungeonDifficulties();

        var invalidRewards = dungeons
            .SelectMany(dungeon =>
            {
                var dungeonTier = dungeon?["difficulty"]?.GetValue<int>() ?? 1;
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

    private static JsonNode ReadDocument(string relativePath)
    {
        var dataRoot = FindDataRoot();
        var path = Path.Combine(dataRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return JsonNode.Parse(File.ReadAllText(path))
            ?? throw new InvalidOperationException($"Unable to parse JSON document '{path}'.");
    }

    private static IReadOnlyList<JsonNode?> ReadDungeonDifficulties()
    {
        var dataRoot = FindDataRoot();
        var path = Path.Combine(dataRoot, "dungeons", "dungeons.json");
        var document = JsonNode.Parse(File.ReadAllText(path))
            ?? throw new InvalidOperationException($"Unable to parse dungeon catalog '{path}'.");

        return ChildArray(document, "families")
            .SelectMany(family => ChildArray(family, "difficulties"))
            .ToList();
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
            foreach (var candidate in new[]
            {
                Path.Combine(current.FullName, "src", "API", "API.LL", "Data"),
                Path.Combine(current.FullName, "LL", "src", "API", "API.LL", "Data")
            })
            {
                if (Directory.Exists(candidate)) return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find API.LL Data directory.");
    }
}
