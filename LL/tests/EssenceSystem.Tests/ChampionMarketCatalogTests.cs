using System.Text.Json;
using Application.UseCases.Inventories.SelectionCrates;
using Microsoft.Extensions.Configuration;
using Services.LL.Colosseum;

namespace EssenceSystem.Tests;

public sealed class ChampionMarketCatalogTests
{
    [Fact]
    public void CatalogContainsTitlesAndSixRewardingWeeklyCaches()
    {
        var apiRoot = TestContentPaths.FindApiRoot();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data"
            })
            .Build();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var catalog = new JsonChampionMarketCatalog(config, apiRoot, options);
        var items = catalog.GetAll();
        var weeklyCaches = items.Where(x => x.Category == "Weekly Cache").ToList();
        using var itemDocument = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(apiRoot, "Data", "items", "items.json")));
        var itemBaseIds = itemDocument.RootElement
            .EnumerateArray()
            .Select(x => x.GetProperty("id").GetString())
            .Where(x => x is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var currentWeek = new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);
        var activeItems = catalog.GetActive(currentWeek);
        var activeItemsLaterThatWeek = catalog.GetActive(currentWeek.AddDays(4));

        Assert.Equal(2, items.Count(x => x.Category == "Title"));
        Assert.Equal(6, weeklyCaches.Count);
        Assert.Equal(11, items.Count(x => x.Category == "Blueprint"));
        Assert.All(
            items.Where(x => x.Category == "Blueprint"),
            blueprint =>
            {
                Assert.Equal(1, blueprint.WeeklyPurchaseLimit);
                Assert.Null(blueprint.LifetimePurchaseLimit);
                Assert.True(blueprint.RotatesWeekly);
            });
        var activeBlueprint = Assert.Single(activeItems, x => x.Category == "Blueprint");
        var activeBlueprintLaterThatWeek = Assert.Single(
            activeItemsLaterThatWeek,
            x => x.Category == "Blueprint");
        Assert.Equal(
            activeBlueprint.Id,
            activeBlueprintLaterThatWeek.Id);
        Assert.DoesNotContain(items, x => x.Category == "Cosmetic");
        Assert.DoesNotContain(weeklyCaches, x => x.CindersGranted > 0);
        Assert.All(weeklyCaches, cache => Assert.True(
            cache.CindersGranted > 0 ||
            cache.SoulstonesGranted > 0 ||
            cache.SigilFragmentsGranted > 0 ||
            cache.RewardItemQuantity > 0));
        var catalystCrate = Assert.Single(weeklyCaches, x => x.Id == "cache.catalyst_selection");
        Assert.Equal(CatalystSelectionCrateCatalog.ItemBaseId, catalystCrate.RewardItemId);
        Assert.Equal(1, catalystCrate.RewardItemQuantity);
        Assert.All(
            items.Where(x => x.RewardItemQuantity > 0),
            item => Assert.Contains(item.RewardItemId, itemBaseIds));
        Assert.All(
            CatalystSelectionCrateCatalog.Options,
            option => Assert.Contains(option.ItemId, itemBaseIds));
        var titleKeys = Directory
            .EnumerateFiles(Path.Combine(apiRoot, "Data", "titles"), "*.json")
            .SelectMany(ReadTitleKeys)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.All(
            items.Where(item => item.Category == "Title"),
            item => Assert.Contains(item.RewardTitleKey!, titleKeys));
        Assert.Equal(11, CatalystSelectionCrateCatalog.Options.Count);
        Assert.All(CatalystSelectionCrateCatalog.Options, option => Assert.Equal(6, option.Quantity));
        var crateItem = itemDocument.RootElement
            .EnumerateArray()
            .Single(item => item.GetProperty("id").GetString() == CatalystSelectionCrateCatalog.ItemBaseId);
        Assert.Equal("Resource", crateItem.GetProperty("itemType").GetString());
        Assert.True(crateItem.GetProperty("isBound").GetBoolean());
        Assert.Equal(
            [
                "item.monster_core.lesser",
                "item.monster_core.greater",
                "item.monster_core.primal"
            ],
            weeklyCaches
                .Where(x => x.RewardItemId?.StartsWith("item.monster_core.") == true)
                .Select(x => x.RewardItemId));

        static IReadOnlyList<string> ReadTitleKeys(string path)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement
                .EnumerateArray()
                .Select(title => title.GetProperty("key").GetString()!)
                .ToList();
        }
    }

    [Fact]
    public void CatalystSelectionCacheMatchesAllRegionOneDungeonBlueprintRequirements()
    {
        var apiRoot = TestContentPaths.FindApiRoot();
        using var dungeonDocument = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(apiRoot, "Data", "dungeons", "dungeons.json")));
        using var blueprintDocument = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(apiRoot, "Data", "crafting", "blueprints.json")));

        var regionOneDungeonIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var regionOneBlueprintItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var family in dungeonDocument.RootElement.GetProperty("families").EnumerateArray())
        {
            if (family.GetProperty("region").GetInt32() != 1) continue;

            regionOneDungeonIds.Add(family.GetProperty("id").GetString()!);
            foreach (var difficulty in family.GetProperty("difficulties").EnumerateArray())
            {
                var rewards = difficulty.GetProperty("rewardTable");
                AddBlueprintRewards(rewards.GetProperty("firstClearRewards"));
                AddBlueprintRewards(rewards.GetProperty("completionRewards"));
            }
        }

        var requiredCatalystItemIds = blueprintDocument.RootElement
            .EnumerateArray()
            .Where(blueprint =>
                blueprint.GetProperty("sourceType").GetString() == "Dungeon" &&
                regionOneDungeonIds.Contains(blueprint.GetProperty("sourceId").GetString()!) &&
                regionOneBlueprintItemIds.Contains(blueprint.GetProperty("itemId").GetString()!))
            .SelectMany(blueprint => blueprint
                .GetProperty("additionalMaterialRequirements")
                .EnumerateArray()
                .Where(requirement => requirement.GetProperty("type").GetString() == "SpecialResource")
                .Select(requirement => requirement.GetProperty("itemId").GetString()!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(
            requiredCatalystItemIds.OrderBy(itemId => itemId, StringComparer.OrdinalIgnoreCase),
            CatalystSelectionCrateCatalog.Options
                .Select(option => option.ItemId)
                .OrderBy(itemId => itemId, StringComparer.OrdinalIgnoreCase));

        void AddBlueprintRewards(JsonElement rewards)
        {
            foreach (var reward in rewards.EnumerateArray())
            {
                var itemId = reward.GetProperty("itemId").GetString();
                if (itemId?.StartsWith("blueprint_", StringComparison.OrdinalIgnoreCase) == true)
                {
                    regionOneBlueprintItemIds.Add(itemId);
                }
            }
        }
    }
}
