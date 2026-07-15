using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Prophecies;
using Microsoft.Extensions.Configuration;
using Services.LL.Prophecies;

namespace EssenceSystem.Tests;

public sealed class ProphecyBalanceProviderTests
{
    [Fact]
    public void Committed_prophecy_catalog_is_valid_and_complete()
    {
        var apiRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "API", "API.LL"));
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        var configuration = new ConfigurationBuilder().Build();
        var definitions = new JsonProphecyDefinitionProvider(configuration, apiRoot, options);
        var provider = new JsonProphecyBalanceProvider(configuration, apiRoot, options, definitions);

        var catalog = provider.GetCatalog();
        Assert.Equal(26, catalog.Targets.Count);
        Assert.Equal(6, catalog.RewardProfiles.Count);
        Assert.Equal(3, catalog.CategoryRewardPackages.Count);
        Assert.Equal(100, catalog.RewardScaling.CinderGrowthBasisPointsPerCharacterLevel);
        Assert.Equal(20000, catalog.RewardScaling.CinderGrowthCapBasisPoints);
        Assert.Equal(5, catalog.RewardScaling.CinderRoundingIncrement);
        Assert.Equal(3, catalog.WeeklyMilestones.Count);
        Assert.Equal(4, catalog.Caches.Count);
        Assert.Equal(1, catalog.FavorRewards.Single(x => x.Scope == ProphecyScope.Daily).Amount);
        Assert.Equal(2, catalog.FavorRewards.Single(x => x.Scope == ProphecyScope.Weekly).Amount);
        Assert.Equal([40, 80], catalog.Economy.PaidRerollCosts);
        Assert.Equal(3, catalog.Economy.DailyRerollLimit);
        Assert.All(catalog.Caches, cache =>
        {
            Assert.True(cache.Rolls > 0);
            Assert.NotEmpty(cache.Rewards);
            Assert.All(cache.Rewards, reward => Assert.True(reward.Weight > 0));
            Assert.True(cache.Rewards.Sum(x => x.Weight) > 0);
        });

        var smallCache = catalog.Caches.Single(x => x.ItemId == "revelation_cache_small");
        Assert.Equal(2, smallCache.Rolls);
        Assert.Contains("Soulstones", smallCache.PreviewRewards);

        var weeklyRare = catalog.RewardProfiles.Single(x => x.Id == "Weekly.Rare");
        Assert.Equal(3000, weeklyRare.CharacterExperience.NextLevelBasisPoints);
        Assert.Equal("greater_prophecy_cache", weeklyRare.FlatReward.CacheItemId);

        var shares = catalog.RewardProfiles.ToDictionary(
            x => x.Id,
            x => x.CharacterExperience.NextLevelBasisPoints);
        Assert.Equal(400, shares["Daily.Common"]);
        Assert.Equal(500, shares["Daily.Uncommon"]);
        Assert.Equal(600, shares["Daily.Rare"]);
        Assert.Equal(2500, shares["Weekly.Uncommon"]);
        Assert.Equal(3000, shares["Weekly.Rare"]);
        Assert.Equal(3500, shares["Weekly.Epic"]);
        Assert.Equal(4500, 5 * shares["Daily.Common"] + shares["Weekly.Uncommon"]);
        Assert.Equal(6500, 5 * shares["Daily.Rare"] + shares["Weekly.Epic"]);

        Assert.All(catalog.RewardProfiles.Where(x => x.Scope == ProphecyScope.Daily),
            profile => Assert.Equal(2, profile.FlatReward.SigilFragments));
        Assert.All(catalog.RewardProfiles.Where(x => x.Scope == ProphecyScope.Weekly),
            profile => Assert.Equal(5, profile.FlatReward.SigilFragments));

        var expectedWeeklyFragments = 5 * 2 + 5 + new[]
        {
            "greater_prophecy_cache",
            "revelation_cache_small",
            "revelation_cache_greater",
            "revelation_cache_perfect_week"
        }.Sum(cacheId => ExpectedSigilFragments(catalog.Caches.Single(x => x.ItemId == cacheId)));
        Assert.InRange(expectedWeeklyFragments, 23, 25);
        Assert.Equal(24.05, expectedWeeklyFragments, precision: 2);

        var weeklyDungeon = catalog.CategoryRewardPackages.Single(x =>
            x.Scope == ProphecyScope.Weekly &&
            x.Category == ProphecyCategory.Dungeon &&
            x.Difficulty == ProphecyDifficulty.Rare);
        Assert.Contains(weeklyDungeon.LevelScaledItems,
            x => x.MinLevel == 60 && x.ItemId == "item.monster_core.primal" && x.Quantity == 1);
    }

    private static double ExpectedSigilFragments(ProphecyCacheDefinition cache)
    {
        var totalWeight = cache.Rewards.Sum(x => x.Weight);
        var expectedPerRoll = cache.Rewards.Sum(x =>
            (double)x.Weight / totalWeight * x.Reward.SigilFragments);
        return cache.Rolls * expectedPerRoll;
    }
}
