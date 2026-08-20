namespace Domain.Models.Raids;

public static class RaidRules
{
    public const int Version = 1;
}

public sealed class RaidBossCatalogDocument
{
    public IReadOnlyList<RaidBossDefinition> RaidBosses { get; init; } = [];
}

public sealed class RaidBossDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Region { get; init; }
    public IReadOnlyList<int> Regions { get; init; } = [];
    public int LevelRequirement { get; init; }
    public string? RequiredCompletedQuestId { get; init; }
    public int? RequiredTowerFloor { get; init; }
    public string ImagePath { get; init; } = string.Empty;
    public IReadOnlyList<RaidBossTierDefinition> Tiers { get; init; } = [];
}

public sealed class RaidBossTierDefinition
{
    public int Tier { get; init; }
    public int LaneSlots { get; init; }
    public int MinimumRoster { get; init; }
    public int SignupWindowHours { get; init; } = 24;
    public string RaidSealItemId { get; init; } = string.Empty;
    public string RaidSealFragmentItemId { get; init; } = string.Empty;
    public int RaidSealFragmentCost { get; init; } = 20;
    public RaidRecommendedWingPowerDefinition RecommendedWingPower { get; init; } = new();
    public RaidTickBudgetDefinition TickBudget { get; init; } = new();
    public RaidBossCombatDefinition Boss { get; init; } = new();
    public RaidFlankDefinition Flank { get; init; } = new();
    public RaidWardDefinition Ward { get; init; } = new();
    public RaidRewardDefinition Rewards { get; init; } = new();
}

public sealed class RaidRecommendedWingPowerDefinition
{
    public int Vanguard { get; init; }
    public int Flank { get; init; }
    public int Ward { get; init; }
}

public sealed class RaidTickBudgetDefinition
{
    public int Vanguard { get; init; } = 6000;
    public int Flank { get; init; } = 3000;
    public int Ward { get; init; } = 4000;
}

public sealed class RaidBossCombatDefinition
{
    public Guid CreatureId { get; init; }
    public RaidAttributeScalingDefinition Scaling { get; init; } = new();
    public IReadOnlyList<RaidBossVariantDefinition> Variants { get; init; } = [];
    public decimal MaxReinforceOffensePercent { get; init; } = 40;
    public decimal MaxWardBreakPercent { get; init; } = 50;
    public int OvertimeStartsAtTick { get; init; } = 4500;
    public float OvertimePowerIncreasePercent { get; init; } = 6;
}

public sealed class RaidBossVariantDefinition
{
    public Guid CreatureId { get; init; }
    public decimal SpawnChancePercent { get; init; }
    public RaidAttributeScalingDefinition? Scaling { get; init; }
}

public sealed class RaidFlankDefinition
{
    public IReadOnlyList<RaidCreatureGroupEntry> Adds { get; init; } = [];
}

public sealed class RaidWardDefinition
{
    public Guid ObjectiveCreatureId { get; init; }
    public RaidAttributeScalingDefinition ObjectiveScaling { get; init; } = new();
    public IReadOnlyList<RaidCreatureGroupEntry> Guards { get; init; } = [];
}

public sealed class RaidCreatureGroupEntry
{
    public Guid CreatureId { get; init; }
    public int Count { get; init; } = 1;
    public decimal SpawnChancePercent { get; init; } = 100;
    public RaidAttributeScalingDefinition Scaling { get; init; } = new();
}

public sealed class RaidAttributeScalingDefinition
{
    public float Health { get; init; } = 1;
    public float Offense { get; init; } = 1;
    public float Defense { get; init; } = 1;
    public float Resistance { get; init; } = 1;
    public float Penetration { get; init; } = 1;
    public float Regeneration { get; init; } = 1;
}

public sealed class RaidRewardDefinition
{
    public int SlainTrophies { get; init; } = 100;
    public int BrokenTrophies { get; init; } = 65;
    public int WoundedTrophies { get; init; } = 40;
    public int RepelledTrophies { get; init; } = 20;
    public IReadOnlyList<RaidPendingItem> GuaranteedItems { get; init; } = [];
}

public sealed class RaidTrophyVendorCatalogDocument
{
    public IReadOnlyList<RaidTrophyVendorItemDefinition> Items { get; init; } = [];
}

public sealed class RaidTrophyVendorItemDefinition
{
    public string Id { get; init; } = string.Empty;
    public string RaidBossId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public int TrophyCost { get; init; }
    public string RewardItemId { get; init; } = string.Empty;
    public int RewardQuantity { get; init; } = 1;
    public int? WeeklyPurchaseLimit { get; init; }
    public int? LifetimePurchaseLimit { get; init; }
    public int RequiredTier { get; init; } = 1;
    public bool IsEnabled { get; init; } = true;
    public int SortOrder { get; init; }
}
