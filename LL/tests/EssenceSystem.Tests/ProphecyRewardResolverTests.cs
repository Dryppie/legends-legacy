using Application.Interfaces.Services.LL.Prophecies;
using Domain.Models.Prophecies;
using Services.LL.Prophecies;

namespace EssenceSystem.Tests;

public sealed class ProphecyRewardResolverTests
{
    [Fact]
    public void Resolve_uses_percentage_scaling_for_early_characters()
    {
        var resolver = new ProphecyRewardResolver(new BalanceProvider(CreateCatalog()));

        var reward = resolver.Resolve(DailyRare(ProphecyCategory.Combat), new ProphecyRewardContext(1, 125));

        Assert.Equal(8, reward.CharacterExperience);
        Assert.Equal(195, reward.Cinders);
        Assert.Equal(1, reward.Soulstones);
        Assert.Equal(2, reward.SigilFragments);
        Assert.Equal(13, reward.FateEcho);
        Assert.Equal(1, reward.PropheticFavor);
    }

    [Fact]
    public void Resolve_scales_experience_and_cinders_from_next_level_requirement()
    {
        var resolver = new ProphecyRewardResolver(new BalanceProvider(CreateCatalog()));

        var reward = resolver.Resolve(DailyRare(ProphecyCategory.Combat), new ProphecyRewardContext(50, 10_725));

        Assert.Equal(644, reward.CharacterExperience);
        Assert.Equal(290, reward.Cinders);
        Assert.Equal(1, reward.Soulstones);
        Assert.Equal(13, reward.FateEcho);
    }

    [Fact]
    public void Resolve_caps_cinder_growth_independently_from_experience_growth()
    {
        var resolver = new ProphecyRewardResolver(new BalanceProvider(CreateCatalog()));

        var reward = resolver.Resolve(DailyRare(ProphecyCategory.Combat), new ProphecyRewardContext(500, 1_000_000));

        Assert.Equal(60_000, reward.CharacterExperience);
        Assert.Equal(585, reward.Cinders);
    }

    [Fact]
    public void Resolve_continues_granting_scaled_experience_above_level_100()
    {
        var resolver = new ProphecyRewardResolver(new BalanceProvider(CreateCatalog()));

        var reward = resolver.Resolve(DailyRare(ProphecyCategory.Combat), new ProphecyRewardContext(101, 754_975));

        Assert.Equal(45_299, reward.CharacterExperience);
    }

    [Fact]
    public void Resolve_adds_matching_category_package_and_level_band()
    {
        var resolver = new ProphecyRewardResolver(new BalanceProvider(CreateCatalog()));

        var reward = resolver.Resolve(DailyRare(ProphecyCategory.Dungeon), new ProphecyRewardContext(45, 10_725));

        Assert.Equal(5, reward.SigilFragments);
        var dust = Assert.Single(reward.Items);
        Assert.Equal("soul_dust", dust.ItemId);
        Assert.Equal(20, dust.Quantity);
    }

    private static ProphecyDefinition DailyRare(ProphecyCategory category) => new()
    {
        Id = $"daily.{category}.rare",
        Scope = ProphecyScope.Daily,
        Category = category,
        Difficulty = ProphecyDifficulty.Rare,
        RewardProfileId = "Daily.Rare"
    };

    private static ProphecyBalanceCatalog CreateCatalog() => new()
    {
        RewardScaling = new ProphecyRewardScalingSettings
        {
            CinderGrowthBasisPointsPerCharacterLevel = 100,
            CinderGrowthCapBasisPoints = 20000,
            CinderRoundingIncrement = 5
        },
        RewardProfiles =
        [
            new ProphecyRewardProfile
            {
                Id = "Daily.Rare",
                Scope = ProphecyScope.Daily,
                Difficulty = ProphecyDifficulty.Rare,
                CharacterExperience = new ProphecyScaledAmount
                {
                    NextLevelBasisPoints = 600
                },
                MinimumCinders = 195,
                FlatReward = new ProphecyRewardSnapshot
                {
                    Soulstones = 1,
                    SigilFragments = 2,
                    FateEcho = 13
                }
            }
        ],
        CategoryRewardPackages =
        [
            new ProphecyCategoryRewardPackage
            {
                Scope = ProphecyScope.Daily,
                Category = ProphecyCategory.Dungeon,
                Reward = new ProphecyRewardSnapshot { SigilFragments = 3 },
                LevelScaledItems =
                [
                    new ProphecyLevelScaledItemReward
                    {
                        MinLevel = 1,
                        MaxLevel = 29,
                        ItemId = "soul_dust",
                        Quantity = 10
                    },
                    new ProphecyLevelScaledItemReward
                    {
                        MinLevel = 30,
                        MaxLevel = 59,
                        ItemId = "soul_dust",
                        Quantity = 20
                    },
                    new ProphecyLevelScaledItemReward
                    {
                        MinLevel = 60,
                        ItemId = "soul_dust",
                        Quantity = 40
                    }
                ]
            }
        ],
        FavorRewards =
        [
            new ProphecyFavorReward { Scope = ProphecyScope.Daily, Amount = 1 },
            new ProphecyFavorReward { Scope = ProphecyScope.Weekly, Amount = 2 }
        ]
    };

    private sealed class BalanceProvider(ProphecyBalanceCatalog catalog) : IProphecyBalanceProvider
    {
        public ProphecyBalanceCatalog GetCatalog() => catalog;
    }
}
