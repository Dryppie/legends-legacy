using Domain.Models.Combat;
using Domain.Models.Snapshots;

namespace Domain.Models.Raids;

public enum RaidRunStatus
{
    Mustering = 0,
    Resolving = 1,
    Resolved = 2,
    Settled = 3,
    Cancelled = 4,
    Expired = 5,
    Playback = 6
}

public enum RaidOutcome
{
    Repelled = 0,
    Wounded = 1,
    Broken = 2,
    Slain = 3
}

public enum RaidRewardKind
{
    WeeklyBase = 0,
    WeeklyUpgrade = 1,
    Repeat = 2
}

public enum RaidSignupStatus
{
    // Approved remains zero so signups created before approval was introduced
    // continue to be treated as raid participants.
    Approved = 0,
    Pending = 1
}

public enum RaidLane
{
    // Explicit values preserve assignments created before the party redesign:
    // old Vanguard -> Main Guard, old Flank -> Rearguard, old Ward -> Vanguard.
    MainGuard = 0,
    Rearguard = 1,
    Vanguard = 2,

    // Resolution/playback phase only. Characters can never be assigned here.
    FinalAssault = 3
}

public static class RaidParties
{
    public static readonly IReadOnlyList<RaidLane> All =
        [RaidLane.Rearguard, RaidLane.Vanguard, RaidLane.MainGuard];

    public static bool IsAssignable(RaidLane lane) => All.Contains(lane);

    public static int FormationNumber(RaidLane lane) => lane switch
    {
        RaidLane.Rearguard => 1,
        RaidLane.Vanguard => 2,
        RaidLane.MainGuard => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(lane), lane, "Only raid parties have formations.")
    };

    public static int EncounterOrder(RaidLane lane) => lane switch
    {
        RaidLane.Rearguard => 1,
        RaidLane.Vanguard => 2,
        RaidLane.MainGuard => 3,
        RaidLane.FinalAssault => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(lane), lane, "Unknown raid encounter.")
    };
}

public sealed class RaidRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RaidBossId { get; set; } = string.Empty;
    public int Tier { get; set; }
    public string DefinitionHash { get; set; } = string.Empty;
    public string DefinitionSnapshotJson { get; set; } = string.Empty;
    public Guid LeaderCharacterId { get; set; }
    public RaidRunStatus Status { get; set; } = RaidRunStatus.Mustering;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset SignupClosesAt { get; set; }
    public DateTimeOffset? CommencedAt { get; set; }
    public DateTimeOffset? PlaybackStartedAt { get; set; }
    public DateTimeOffset? PlaybackEndsAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? SettledAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public int WeekKey { get; set; }
    public decimal? ReinforcementPenalty { get; set; }
    public decimal? GuardianBreak { get; set; }
    public decimal? SignatureDisruption { get; set; }
    public decimal? BossHealthRemainingPercent { get; set; }
    public RaidOutcome? Outcome { get; set; }
    public string? SimulationLeaseOwner { get; set; }
    public DateTimeOffset? SimulationLeaseUntil { get; set; }
    public int SimulationAttempts { get; set; }
    public long RowVersion { get; set; }
    public ICollection<RaidSignup> Signups { get; set; } = [];
    public ICollection<RaidLaneResult> LaneResults { get; set; } = [];
    public ICollection<RaidPlayback> Playbacks { get; set; } = [];
    public ICollection<RaidParticipantResult> ParticipantResults { get; set; } = [];
    public ICollection<RaidRewardClaim> RewardClaims { get; set; } = [];
}

public sealed class RaidSignup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RaidRunId { get; set; }
    public RaidRun RaidRun { get; set; } = null!;
    public Guid CharacterId { get; set; }
    public Guid AccountId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public Guid CharacterSnapshotId { get; set; }
    public CharacterSnapshot CharacterSnapshot { get; set; } = null!;
    public string LoadoutHash { get; set; } = string.Empty;
    public int PowerRating { get; set; }
    public RaidSignupStatus Status { get; set; } = RaidSignupStatus.Approved;
    public RaidLane? Lane { get; set; }
    public int? WingSlotIndex { get; set; }
    public DateTimeOffset SignedUpAt { get; set; }
    public DateTimeOffset? SnapshotRefreshedAt { get; set; }
}

public sealed record RaidPartyAssignment(
    Guid CharacterId,
    RaidLane? Lane,
    int? WingSlotIndex);

public sealed class RaidLaneResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RaidRunId { get; set; }
    public RaidRun RaidRun { get; set; } = null!;
    public RaidLane Lane { get; set; }
    public int Seed { get; set; }
    public int DurationTicks { get; set; }
    public BattleOutcome BattleOutcome { get; set; }
    public long TotalFriendlyDamage { get; set; }
    public long ObjectiveDamage { get; set; }
    public long ObjectiveBarrierAbsorbed { get; set; }
    public decimal SurvivingHostileHealthFraction { get; set; }
    public decimal DerivedModifier { get; set; }
    public Guid? PlaybackId { get; set; }
    public RaidPlayback? Playback { get; set; }
}

public sealed class RaidPlayback
{
    public const int CompactBundleSchemaVersion = 3;

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RaidRunId { get; set; }
    public RaidRun RaidRun { get; set; } = null!;
    public RaidLane Lane { get; set; }
    public int SchemaVersion { get; set; } = CompactBundleSchemaVersion;
    public int TicksPerSecond { get; set; } = 10;
    public int TicksPerFrame { get; set; } = 10;
    public int TotalTicks { get; set; }
    public int FrameCount { get; set; }
    public string BundleHash { get; set; } = string.Empty;
    public int BundleLength { get; set; }
    public string BundleContentType { get; set; } = "application/json";
    public string BundleContentEncoding { get; set; } = "br";
    public DateTimeOffset CreatedAt { get; set; }
    public RaidPlaybackArtifact Artifact { get; set; } = null!;
}

public sealed class RaidPlaybackArtifact
{
    public Guid RaidPlaybackId { get; set; }
    public RaidPlayback Playback { get; set; } = null!;
    public byte[] BundleBytes { get; set; } = [];
}

public sealed class RaidParticipantResult
{
    public Guid RaidRunId { get; set; }
    public RaidRun RaidRun { get; set; } = null!;
    public Guid CharacterId { get; set; }
    public RaidLane Lane { get; set; }
    public long DamageDone { get; set; }
    public int? DeathTick { get; set; }
    public decimal ContributionScore { get; set; }
    public int ContributionRank { get; set; }
}

public sealed class RaidRewardClaim
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RaidRunId { get; set; }
    public RaidRun RaidRun { get; set; } = null!;
    public string RaidBossId { get; set; } = string.Empty;
    public Guid CharacterId { get; set; }
    public int WeekKey { get; set; }
    public int Trophies { get; set; }
    public string PendingItemsJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public RaidRewardKind Kind { get; set; }
}

public sealed record RaidPendingItem(string ItemId, int Quantity);

public sealed class RaidTrophyPurchase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CharacterId { get; set; }
    public string RaidBossId { get; set; } = string.Empty;
    public string VendorItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int TrophiesSpent { get; set; }
    public int WeekKey { get; set; }
    public DateTimeOffset PurchasedAt { get; set; }
}

public sealed class RaidPowerRecommendationCacheEntry
{
    public string RaidBossId { get; set; } = string.Empty;
    public int Tier { get; set; }
    public string DefinitionHash { get; set; } = string.Empty;
    public int RaidRulesVersion { get; set; }
    public int PowerRatingAlgorithmVersion { get; set; }
    public int CombatRulesVersion { get; set; }
    public int EquipmentBalanceVersion { get; set; }
    public int SeedSetVersion { get; set; }
    public string RecommendationJson { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
