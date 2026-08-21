using Domain.Models.Combat;

namespace Domain.Models.Raids;

public static class RaidRules
{
    public const int Version = 7;
}

public static class RaidPlusDifficulty
{
    public const double RecommendedPowerGrowth = 1.085d;
    private const double HealthGrowth = 1.10d;
    private const double OffenseGrowth = 1.075d;
    private const double DefenseGrowth = 0.045d;
    private const double PenetrationGrowth = 0.03d;
    private const double RegenerationGrowth = 0.05d;
    private const double TrophyGrowth = 0.08d;
    private const double MaterialGrowth = 0.05d;

    public static RaidBossTierDefinition Create(RaidBossDefinition boss, int plusLevel)
    {
        if (plusLevel < 0)
            throw new ArgumentOutOfRangeException(nameof(plusLevel));
        var regular = boss.Tiers.OrderBy(x => x.Tier).FirstOrDefault()
            ?? throw new InvalidOperationException($"Raid boss '{boss.Id}' has no Regular definition.");
        var milestoneRank = Math.Min(plusLevel / 3, 3);

        return new RaidBossTierDefinition
        {
            Tier = plusLevel,
            LaneSlots = regular.LaneSlots,
            MinimumRoster = regular.MinimumRoster,
            SignupWindowHours = regular.SignupWindowHours,
            RecommendedWingPower = new RaidRecommendedWingPowerDefinition
            {
                Rearguard = ScaleInt(regular.RecommendedWingPower.Rearguard, RecommendedPowerGrowth, plusLevel),
                Vanguard = ScaleInt(regular.RecommendedWingPower.Vanguard, RecommendedPowerGrowth, plusLevel),
                MainGuard = ScaleInt(regular.RecommendedWingPower.MainGuard, RecommendedPowerGrowth, plusLevel)
            },
            TickBudget = new RaidTickBudgetDefinition
            {
                Rearguard = regular.TickBudget.Rearguard,
                Vanguard = regular.TickBudget.Vanguard,
                MainGuard = regular.TickBudget.MainGuard,
                FinalAssault = regular.TickBudget.FinalAssault
            },
            Boss = new RaidBossCombatDefinition
            {
                CreatureId = regular.Boss.CreatureId,
                Scaling = Scale(regular.Boss.Scaling, plusLevel),
                Variants = regular.Boss.Variants.Select(variant => new RaidBossVariantDefinition
                {
                    CreatureId = variant.CreatureId,
                    SpawnChancePercent = variant.SpawnChancePercent,
                    Scaling = variant.Scaling is null ? null : Scale(variant.Scaling, plusLevel)
                }).ToArray(),
                MaxGuardianBreakPercent = regular.Boss.MaxGuardianBreakPercent,
                MaxSignaturePowerReductionPercent = regular.Boss.MaxSignaturePowerReductionPercent,
                MaxSignatureCooldownDelayPercent = regular.Boss.MaxSignatureCooldownDelayPercent,
                Stagger = ScaleStagger(regular.Boss.Stagger, plusLevel),
                OvertimeStartsAtTick = Math.Max(1, regular.Boss.OvertimeStartsAtTick - milestoneRank * 150),
                OvertimePowerIncreasePercent = regular.Boss.OvertimePowerIncreasePercent + milestoneRank
            },
            Rearguard = new RaidRearguardDefinition
            {
                WaveCount = regular.Rearguard.WaveCount,
                Adds = ScaleGroups(regular.Rearguard.Adds, plusLevel, milestoneRank)
            },
            Vanguard = new RaidVanguardDefinition
            {
                GuardianCreatureId = regular.Vanguard.GuardianCreatureId,
                GuardianScaling = Scale(regular.Vanguard.GuardianScaling, plusLevel),
                Escorts = ScaleGroups(regular.Vanguard.Escorts, plusLevel, milestoneRank)
            },
            MainGuard = new RaidMainGuardDefinition
            {
                ProjectionCreatureId = regular.MainGuard.ProjectionCreatureId,
                ProjectionScaling = Scale(regular.MainGuard.ProjectionScaling, plusLevel),
                SurvivalThresholdsPercent = regular.MainGuard.SurvivalThresholdsPercent
                    .Select((threshold, index) => index < regular.MainGuard.SurvivalThresholdsPercent.Count - 1
                        ? Math.Min(99m, threshold + milestoneRank * 2m)
                        : threshold)
                    .ToArray()
            },
            Rewards = new RaidRewardDefinition
            {
                SlainTrophies = ScaleLinear(regular.Rewards.SlainTrophies, TrophyGrowth, plusLevel),
                BrokenTrophies = ScaleLinear(regular.Rewards.BrokenTrophies, TrophyGrowth, plusLevel),
                WoundedTrophies = ScaleLinear(regular.Rewards.WoundedTrophies, TrophyGrowth, plusLevel),
                RepelledTrophies = ScaleLinear(regular.Rewards.RepelledTrophies, TrophyGrowth, plusLevel),
                GuaranteedItems = regular.Rewards.GuaranteedItems.Select(item => new RaidPendingItem(
                    item.ItemId,
                    ScaleLinear(item.Quantity, MaterialGrowth, plusLevel))).ToArray()
            }
        };
    }

    public static string Label(int plusLevel) => plusLevel == 0 ? "Regular" : $"+{plusLevel}";

    private static IReadOnlyList<RaidCreatureGroupEntry> ScaleGroups(
        IReadOnlyList<RaidCreatureGroupEntry> groups,
        int plusLevel,
        int milestoneRank) =>
        groups.Select((group, index) => new RaidCreatureGroupEntry
        {
            CreatureId = group.CreatureId,
            Count = checked(group.Count + (index == 0 ? milestoneRank : 0)),
            SpawnChancePercent = group.SpawnChancePercent,
            Scaling = Scale(group.Scaling, plusLevel)
        }).ToArray();

    private static RaidAttributeScalingDefinition Scale(
        RaidAttributeScalingDefinition value,
        int plusLevel) => new()
    {
        Health = ScaleFloat(value.Health, Math.Pow(HealthGrowth, plusLevel)),
        Offense = ScaleFloat(value.Offense, Math.Pow(OffenseGrowth, plusLevel)),
        Defense = ScaleFloat(value.Defense, 1d + DefenseGrowth * plusLevel),
        Resistance = ScaleFloat(value.Resistance, 1d + DefenseGrowth * plusLevel),
        Penetration = ScaleFloat(value.Penetration, 1d + PenetrationGrowth * plusLevel),
        Regeneration = ScaleFloat(value.Regeneration, 1d + RegenerationGrowth * plusLevel)
    };

    private static float ScaleFloat(float value, double multiplier)
    {
        var scaled = value * multiplier;
        if (!double.IsFinite(scaled) || scaled > float.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(multiplier), "Raid +level exceeds numeric limits.");
        return (float)scaled;
    }

    private static int ScaleInt(int value, double growth, int plusLevel) =>
        ScaleToInt(value * Math.Pow(growth, plusLevel));

    private static int ScaleLinear(int value, double growth, int plusLevel) =>
        ScaleToInt(value * (1d + growth * plusLevel));

    private static BossStaggerDefinition? ScaleStagger(BossStaggerDefinition? value, int plusLevel)
    {
        if (value is null)
            return null;

        return new BossStaggerDefinition
        {
            Enabled = value.Enabled,
            BaseThreshold = ScaleToInt(value.BaseThreshold * (1d + 0.05d * plusLevel)),
            ReferenceParticipantCount = value.ReferenceParticipantCount,
            ParticipantExponent = value.ParticipantExponent,
            BreakDurationTicks = value.BreakDurationTicks,
            RecoveryDurationTicks = value.RecoveryDurationTicks,
            DamageTakenBonusPercent = value.DamageTakenBonusPercent,
            ThresholdGrowthPercentPerBreak = value.ThresholdGrowthPercentPerBreak,
            MaximumBreaks = value.MaximumBreaks
        };
    }

    private static int ScaleToInt(double value)
    {
        if (!double.IsFinite(value) || value > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), "Raid +level exceeds numeric limits.");
        return Math.Max(1, (int)Math.Round(value, MidpointRounding.AwayFromZero));
    }
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
    public RaidRecommendedWingPowerDefinition RecommendedWingPower { get; init; } = new();
    public RaidTickBudgetDefinition TickBudget { get; init; } = new();
    public RaidBossCombatDefinition Boss { get; init; } = new();
    public RaidRearguardDefinition Rearguard { get; init; } = new();
    public RaidVanguardDefinition Vanguard { get; init; } = new();
    public RaidMainGuardDefinition MainGuard { get; init; } = new();
    public RaidRewardDefinition Rewards { get; init; } = new();
}

public sealed class RaidRecommendedWingPowerDefinition
{
    public int Rearguard { get; init; }
    public int Vanguard { get; init; }
    public int MainGuard { get; init; }
}

public sealed class RaidTickBudgetDefinition
{
    public int Rearguard { get; init; } = 3000;
    public int Vanguard { get; init; } = 4000;
    public int MainGuard { get; init; } = 600;
    public int FinalAssault { get; init; } = 6000;
}

public sealed class RaidBossCombatDefinition
{
    public Guid CreatureId { get; init; }
    public RaidAttributeScalingDefinition Scaling { get; init; } = new();
    public IReadOnlyList<RaidBossVariantDefinition> Variants { get; init; } = [];
    public decimal MaxGuardianBreakPercent { get; init; } = 50;
    public decimal MaxSignaturePowerReductionPercent { get; init; } = 30;
    public decimal MaxSignatureCooldownDelayPercent { get; init; } = 25;
    public BossStaggerDefinition? Stagger { get; init; }
    public int OvertimeStartsAtTick { get; init; } = 4500;
    public float OvertimePowerIncreasePercent { get; init; } = 6;
}

public sealed class RaidBossVariantDefinition
{
    public Guid CreatureId { get; init; }
    public decimal SpawnChancePercent { get; init; }
    public RaidAttributeScalingDefinition? Scaling { get; init; }
}

public sealed class RaidRearguardDefinition
{
    public const int MaximumWaveCount = 10;

    public int WaveCount { get; init; } = MaximumWaveCount;
    public IReadOnlyList<RaidCreatureGroupEntry> Adds { get; init; } = [];
}

public sealed class RaidVanguardDefinition
{
    public Guid GuardianCreatureId { get; init; }
    public RaidAttributeScalingDefinition GuardianScaling { get; init; } = new();
    public IReadOnlyList<RaidCreatureGroupEntry> Escorts { get; init; } = [];
}

public sealed class RaidMainGuardDefinition
{
    public Guid ProjectionCreatureId { get; init; }
    public RaidAttributeScalingDefinition ProjectionScaling { get; init; } = new();
    public IReadOnlyList<decimal> SurvivalThresholdsPercent { get; init; } = [33, 67, 100];
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
    public int RequiredTier { get; init; }
    public bool IsEnabled { get; init; } = true;
    public int SortOrder { get; init; }
}
