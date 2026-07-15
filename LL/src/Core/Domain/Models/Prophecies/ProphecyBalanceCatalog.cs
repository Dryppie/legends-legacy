using Domain.Models.Items;

namespace Domain.Models.Prophecies;

public sealed class ProphecyBalanceCatalog
{
    public IReadOnlyList<ProphecyTargetProfile> Targets { get; init; } = [];
    public ProphecyRewardScalingSettings RewardScaling { get; init; } = new();
    public IReadOnlyList<ProphecyRewardProfile> RewardProfiles { get; init; } = [];
    public IReadOnlyList<ProphecyCategoryRewardPackage> CategoryRewardPackages { get; init; } = [];
    public IReadOnlyList<ProphecyFavorReward> FavorRewards { get; init; } = [];
    public IReadOnlyList<ProphecyWeeklyMilestoneDefinition> WeeklyMilestones { get; init; } = [];
    public IReadOnlyList<ProphecyCacheDefinition> Caches { get; init; } = [];
    public ProphecyEconomySettings Economy { get; init; } = new();
}

public sealed class ProphecyRewardScalingSettings
{
    public int CinderGrowthBasisPointsPerCharacterLevel { get; set; } = 100;
    public int CinderGrowthCapBasisPoints { get; set; } = 20000;
    public int CinderRoundingIncrement { get; set; } = 5;
}

public sealed class ProphecyEconomySettings
{
    public bool PaidRerollsEnabled { get; set; } = true;
    public int DailyRerollLimit { get; set; } = 3;
    public List<int> PaidRerollCosts { get; set; } = [40, 80];
}

public sealed class ProphecyTargetProfile
{
    public ProphecyScope Scope { get; set; }
    public string ObjectiveType { get; set; } = string.Empty;
    public ProphecyDifficultyTargets Values { get; set; } = new();

    public int GetValue(ProphecyDifficulty difficulty) => difficulty switch
    {
        ProphecyDifficulty.Common => Values.Common,
        ProphecyDifficulty.Uncommon => Values.Uncommon,
        ProphecyDifficulty.Rare => Values.Rare,
        ProphecyDifficulty.Epic => Values.Epic,
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unsupported prophecy difficulty.")
    };
}

public sealed class ProphecyDifficultyTargets
{
    public int Common { get; set; }
    public int Uncommon { get; set; }
    public int Rare { get; set; }
    public int Epic { get; set; }
}

public sealed class ProphecyRewardProfile
{
    public string Id { get; set; } = string.Empty;
    public ProphecyScope Scope { get; set; }
    public ProphecyDifficulty Difficulty { get; set; }
    public ProphecyScaledAmount CharacterExperience { get; set; } = new();
    public long MinimumCinders { get; set; }
    public ProphecyRewardSnapshot FlatReward { get; set; } = new();
}

public sealed class ProphecyScaledAmount
{
    public int NextLevelBasisPoints { get; set; }
}

public sealed class ProphecyCategoryRewardPackage
{
    public ProphecyScope Scope { get; set; }
    public ProphecyCategory Category { get; set; }
    public ProphecyDifficulty? Difficulty { get; set; }
    public ProphecyRewardSnapshot Reward { get; set; } = new();
    public List<ProphecyLevelScaledItemReward> LevelScaledItems { get; set; } = [];
}

public sealed class ProphecyLevelScaledItemReward
{
    public int MinLevel { get; set; } = 1;
    public int? MaxLevel { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public sealed class ProphecyFavorReward
{
    public ProphecyScope Scope { get; set; }
    public int Amount { get; set; }
}

public sealed class ProphecyWeeklyMilestoneDefinition
{
    public int FavorRequired { get; set; }
    public string Title { get; set; } = string.Empty;
    public ProphecyRewardSnapshot Reward { get; set; } = new();
}

public sealed class ProphecyCacheDefinition
{
    public string ItemId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Rarity Rarity { get; set; }
    public int Rolls { get; set; }
    public List<string> PreviewRewards { get; set; } = [];
    public List<ProphecyCacheRewardEntry> Rewards { get; set; } = [];
}

public sealed class ProphecyCacheRewardEntry
{
    public int Weight { get; set; }
    public ProphecyRewardSnapshot Reward { get; set; } = new();
}
