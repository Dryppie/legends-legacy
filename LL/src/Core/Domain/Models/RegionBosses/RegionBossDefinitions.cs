using Domain.Models.Combat;

namespace Domain.Models.RegionBosses;

public static class RegionBossRules
{
    public const int Version = 1;
    public const int MatchmakingAlgorithmVersion = 1;
    public const int PartySizeScalingVersion = 1;
    public const int MaximumPartySize = 5;
    public const int RecommendedMinimumPartySize = 3;
    public const int TicksPerSecond = 10;
    public const int EncounterTicks = 6000;
}

public sealed class RegionBossCatalogDocument
{
    public IReadOnlyList<RegionBossDefinition> RegionBosses { get; init; } = [];
}

public sealed class RegionBossDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int RegionId { get; init; }
    public Guid CreatureId { get; init; }
    public string ImagePath { get; init; } = string.Empty;
    public int LevelRequirement { get; init; }
    public string? RequiredCompletedQuestId { get; init; }
    public int? RequiredTowerFloor { get; init; }
    public RegionBossBaseScalingDefinition BaseScaling { get; init; } = new();
    public RegionBossLevelScalingDefinition LevelScaling { get; init; } = new();
    public RegionBossFuryDefinition Fury { get; init; } = new();
    public RegionBossRevivalDefinition Revival { get; init; } = new();
    public RegionBossRecoveryDefinition Recovery { get; init; } = new();
    public BossStaggerDefinition? Stagger { get; init; }
    public RegionBossScheduleDefinition Schedule { get; init; } = new();
    public IReadOnlyList<RegionBossRewardBracketDefinition> RewardBrackets { get; init; } = [];
}

public sealed class RegionBossBaseScalingDefinition
{
    public double Health { get; init; } = 1;
    public double Power { get; init; } = 1;
    public double Armor { get; init; } = 1;
    public double Resistance { get; init; } = 1;
    public double Penetration { get; init; } = 1;
    public double Regeneration { get; init; } = 1;
}

public sealed class RegionBossLevelScalingDefinition
{
    public double HealthGrowth { get; init; } = 1.12;
    public double PowerGrowth { get; init; } = 1.055;
    public double ArmorGrowthPerLevel { get; init; } = 0.03;
    public double ResistanceGrowthPerLevel { get; init; } = 0.03;
    public double PenetrationGrowthPerLevel { get; init; }
}

public sealed class RegionBossFuryDefinition
{
    public int IntervalSeconds { get; init; } = 60;
    public float PowerPercentPerStack { get; init; } = 6;
    public float AttackSpeedPercentPerStack { get; init; } = 4;
}

public sealed class RegionBossRevivalDefinition
{
    public int BaseDelaySeconds { get; init; } = 15;
    public int AdditionalDelaySecondsPerDeath { get; init; } = 10;
    public int MaximumDelaySeconds { get; init; } = 60;
    public float ReviveHealthPercent { get; init; } = 50;
}

public sealed class RegionBossRecoveryDefinition
{
    public float LivingHealPercent { get; init; } = 20;
    public float DownedReviveHealthPercent { get; init; } = 50;
}

public sealed class RegionBossScheduleDefinition
{
    public int MinimumIntervalHours { get; init; } = 4;
    public int MaximumIntervalHours { get; init; } = 8;
    public int SignupDurationMinutes { get; init; } = 10;
}

public sealed class RegionBossRewardBracketDefinition
{
    public string Key { get; init; } = string.Empty;
    public int MinimumLevelDefeated { get; init; }
    public string? RewardTableId { get; init; }
    public int Cinders { get; init; }
    public int Soulstones { get; init; }
}
