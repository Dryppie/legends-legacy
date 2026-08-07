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
        var apiRoot = TestContentPaths.FindApiRoot();
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
        Assert.Equal(24, catalog.Targets.Count);
        Assert.Equal(7, catalog.RewardProfiles.Count);
        Assert.Equal(5, catalog.CategoryRewardPackages.Count);
        Assert.Equal(100, catalog.RewardScaling.CinderGrowthBasisPointsPerCharacterLevel);
        Assert.Equal(20000, catalog.RewardScaling.CinderGrowthCapBasisPoints);
        Assert.Equal(5, catalog.RewardScaling.CinderRoundingIncrement);
        Assert.Equal(3, catalog.WeeklyMilestones.Count);
        Assert.Equal(4, catalog.Caches.Count);
        Assert.Equal(1, catalog.FavorRewards.Single(x => x.Scope == ProphecyScope.Daily).Amount);
        Assert.Equal(2, catalog.FavorRewards.Single(x => x.Scope == ProphecyScope.Weekly).Amount);
        Assert.Equal([40, 80], catalog.Economy.PaidRerollCosts);
        Assert.Equal(3, catalog.Economy.DailyRerollLimit);

        var targets = catalog.Targets.ToDictionary(
            x => (x.Scope, x.ObjectiveType),
            x => x.Values);
        Assert.Equal(300, targets[(ProphecyScope.Daily, ProphecyObjectiveType.WinEncounters)].Common);
        Assert.Equal(900, targets[(ProphecyScope.Daily, ProphecyObjectiveType.WinEncounters)].Rare);
        Assert.Equal(300, targets[(ProphecyScope.Daily, ProphecyObjectiveType.KillCreatures)].Common);
        Assert.Equal(900, targets[(ProphecyScope.Daily, ProphecyObjectiveType.KillCreatures)].Rare);
        Assert.Equal(360, targets[(ProphecyScope.Daily, ProphecyObjectiveType.TemperItems)].Common);
        Assert.Equal(1, targets[(ProphecyScope.Daily, ProphecyObjectiveType.AbsorbEssence)].Epic);
        Assert.Equal(21_600, targets[(ProphecyScope.Weekly, ProphecyObjectiveType.TemperItems)].Uncommon);
        Assert.Equal(35_000, targets[(ProphecyScope.Weekly, ProphecyObjectiveType.KillCreatures)].Uncommon);
        Assert.Equal(14, targets[(ProphecyScope.Weekly, ProphecyObjectiveType.CompleteDungeons)].Rare);
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
        Assert.Equal(700, shares["Daily.Epic"]);
        Assert.Equal(2500, shares["Weekly.Uncommon"]);
        Assert.Equal(3000, shares["Weekly.Rare"]);
        Assert.Equal(3500, shares["Weekly.Epic"]);
        Assert.Equal(4500, 5 * shares["Daily.Common"] + shares["Weekly.Uncommon"]);
        Assert.Equal(6500, 5 * shares["Daily.Rare"] + shares["Weekly.Epic"]);

        Assert.All(catalog.RewardProfiles, profile => Assert.Equal(0, profile.MinimumCinders));
        Assert.All(catalog.WeeklyMilestones, milestone => Assert.Equal(0, milestone.Reward.Cinders));
        Assert.All(catalog.Caches, cache =>
            Assert.All(cache.Rewards, entry => Assert.Equal(0, entry.Reward.Cinders)));

        var soulstonesByProfile = catalog.RewardProfiles.ToDictionary(
            x => x.Id,
            x => x.FlatReward.Soulstones);
        Assert.Equal(2, soulstonesByProfile["Daily.Common"]);
        Assert.Equal(3, soulstonesByProfile["Daily.Uncommon"]);
        Assert.Equal(4, soulstonesByProfile["Daily.Rare"]);
        Assert.Equal(6, soulstonesByProfile["Daily.Epic"]);
        Assert.Equal(8, soulstonesByProfile["Weekly.Uncommon"]);
        Assert.Equal(12, soulstonesByProfile["Weekly.Rare"]);
        Assert.Equal(16, soulstonesByProfile["Weekly.Epic"]);

        var fragmentsByProfile = catalog.RewardProfiles.ToDictionary(
            x => x.Id,
            x => x.FlatReward.SigilFragments);
        Assert.Equal(2, fragmentsByProfile["Daily.Common"]);
        Assert.Equal(3, fragmentsByProfile["Daily.Uncommon"]);
        Assert.Equal(4, fragmentsByProfile["Daily.Rare"]);
        Assert.Equal(5, fragmentsByProfile["Daily.Epic"]);
        Assert.Equal(8, fragmentsByProfile["Weekly.Uncommon"]);
        Assert.Equal(10, fragmentsByProfile["Weekly.Rare"]);
        Assert.Equal(12, fragmentsByProfile["Weekly.Epic"]);

        var expectedWeeklySoulstones = 5 * soulstonesByProfile["Daily.Common"]
            + soulstonesByProfile["Weekly.Uncommon"]
            + catalog.WeeklyMilestones.Sum(x => x.Reward.Soulstones)
            + new[]
            {
                "greater_prophecy_cache",
                "revelation_cache_small",
                "revelation_cache_greater",
                "revelation_cache_perfect_week"
            }.Sum(cacheId => ExpectedReward(catalog.Caches.Single(x => x.ItemId == cacheId), x => x.Soulstones));
        Assert.Equal(50.35, expectedWeeklySoulstones, precision: 2);

        var expectedWeeklyFragments = 5 * fragmentsByProfile["Daily.Common"]
            + fragmentsByProfile["Weekly.Uncommon"]
            + catalog.WeeklyMilestones.Sum(x => x.Reward.SigilFragments)
            + new[]
        {
            "greater_prophecy_cache",
            "revelation_cache_small",
            "revelation_cache_greater",
            "revelation_cache_perfect_week"
        }.Sum(cacheId => ExpectedReward(catalog.Caches.Single(x => x.ItemId == cacheId), x => x.SigilFragments));
        Assert.Equal(47.75, expectedWeeklyFragments, precision: 2);

        var weeklyDungeon = catalog.CategoryRewardPackages.Single(x =>
            x.Scope == ProphecyScope.Weekly &&
            x.Category == ProphecyCategory.Dungeon &&
            x.Difficulty == ProphecyDifficulty.Rare);
        Assert.Contains(weeklyDungeon.LevelScaledItems,
            x => x.MinLevel == 60 && x.ItemId == "item.monster_core.primal" && x.Quantity == 1);

        var weeklyEssence = catalog.CategoryRewardPackages.Single(x =>
            x.Scope == ProphecyScope.Weekly &&
            x.Category == ProphecyCategory.Essence &&
            x.Difficulty == ProphecyDifficulty.Uncommon);
        Assert.Contains(weeklyEssence.LevelScaledItems,
            x => x.MinLevel == 30 && x.MaxLevel == 59 && x.ItemId == "item.monster_core.greater" && x.Quantity == 1);

        var weeklyCrafting = catalog.CategoryRewardPackages.Single(x =>
            x.Scope == ProphecyScope.Weekly &&
            x.Category == ProphecyCategory.Crafting &&
            x.Difficulty == ProphecyDifficulty.Uncommon);
        var catalystCrate = Assert.Single(weeklyCrafting.Reward.Items);
        Assert.Equal("item.catalyst_selection_crate", catalystCrate.ItemId);
        Assert.Equal(1, catalystCrate.Quantity);

        var perfectWeek = catalog.Caches.Single(x => x.ItemId == "revelation_cache_perfect_week");
        Assert.Equal(0.2, ExpectedItemQuantity(perfectWeek, "item.catalyst_selection_crate"), precision: 2);
    }

    private static double ExpectedReward(
        ProphecyCacheDefinition cache,
        Func<ProphecyRewardSnapshot, long> selector)
    {
        var totalWeight = cache.Rewards.Sum(x => x.Weight);
        var expectedPerRoll = cache.Rewards.Sum(x =>
            (double)x.Weight / totalWeight * selector(x.Reward));
        return cache.Rolls * expectedPerRoll;
    }

    private static double ExpectedItemQuantity(ProphecyCacheDefinition cache, string itemId)
    {
        var totalWeight = cache.Rewards.Sum(x => x.Weight);
        var expectedPerRoll = cache.Rewards.Sum(x =>
            (double)x.Weight / totalWeight * x.Reward.Items
                .Where(item => item.ItemId == itemId)
                .Sum(item => item.Quantity));
        return cache.Rolls * expectedPerRoll;
    }
}
