using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Services.LL.Colosseum;

namespace EssenceSystem.Tests;

public sealed class ChampionMarketCatalogTests
{
    [Fact]
    public void Catalog_contains_titles_and_five_rewarding_weekly_caches()
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
        Assert.Equal(5, weeklyCaches.Count);
        Assert.Equal(activeItems.Select(x => x.Id), activeItemsLaterThatWeek.Select(x => x.Id));
        Assert.DoesNotContain(items, x => x.Category == "Cosmetic");
        Assert.DoesNotContain(items, x => x.Category == "Blueprint");
        Assert.DoesNotContain(weeklyCaches, x => x.CindersGranted > 0);
        Assert.All(weeklyCaches, cache => Assert.True(
            cache.CindersGranted > 0 ||
            cache.SoulstonesGranted > 0 ||
            cache.SigilFragmentsGranted > 0 ||
            cache.RewardItemQuantity > 0));
        Assert.All(
            items.Where(x => x.RewardItemQuantity > 0),
            item => Assert.Contains(item.RewardItemId, itemBaseIds));
        var titleKeys = Directory
            .EnumerateFiles(Path.Combine(apiRoot, "Data", "titles"), "*.json")
            .SelectMany(ReadTitleKeys)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.All(
            items.Where(item => item.Category == "Title"),
            item => Assert.Contains(item.RewardTitleKey!, titleKeys));
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

}
