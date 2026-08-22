namespace Domain.Models.RegionBosses;

public enum RegionBossEventStatus
{
    Scheduled = 0,
    SignupOpen = 1,
    Matching = 2,
    Resolving = 3,
    Playback = 4,
    Settled = 5,
    Cancelled = 6
}

public enum RegionBossRunStatus
{
    Queued = 0,
    Resolving = 1,
    Ready = 2,
    Settled = 3,
    Errored = 4
}

public enum RegionBossTerminationReason
{
    PartyDefeated = 0,
    TimeExpired = 1,
    SimulationError = 2,
    Cancelled = 3
}

public enum RegionBossRewardStatus
{
    Unclaimed = 0,
    Claimed = 1
}

public sealed class RegionBossEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RegionBossDefinitionId { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public RegionBossEventStatus Status { get; set; }
    public DateTimeOffset SignupStartsAtUtc { get; set; }
    public DateTimeOffset SignupClosesAtUtc { get; set; }
    public DateTimeOffset EncounterStartsAtUtc { get; set; }
    public DateTimeOffset? PlaybackStartsAtUtc { get; set; }
    public DateTimeOffset? PlaybackEndsAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public string DefinitionHash { get; set; } = string.Empty;
    public string DefinitionSnapshotJson { get; set; } = string.Empty;
    public int MatchmakingAlgorithmVersion { get; set; }
    public int CombatRulesVersion { get; set; }
    public long RowVersion { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<RegionBossSignup> Signups { get; set; } = [];
    public ICollection<RegionBossRun> Runs { get; set; } = [];
    public ICollection<RegionBossRewardGrant> RewardGrants { get; set; } = [];
}

public sealed class RegionBossSignup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RegionBossEventId { get; set; }
    public RegionBossEvent Event { get; set; } = null!;
    public Guid CharacterId { get; set; }
    public Guid AccountId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public int PowerRating { get; set; }
    public int PowerRatingAlgorithmVersion { get; set; }
    public Guid? RegionBossRunId { get; set; }
    public RegionBossRun? Run { get; set; }
    public int? PartySlot { get; set; }
    public DateTimeOffset SignedUpAtUtc { get; set; }
}

public sealed class RegionBossRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RegionBossEventId { get; set; }
    public RegionBossEvent Event { get; set; } = null!;
    public int PartyNumber { get; set; }
    public int PartySize { get; set; }
    public int MatchmakingBand { get; set; }
    public int PartySizeScalingVersion { get; set; }
    public int RandomSeed { get; set; }
    public RegionBossRunStatus Status { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public DateTimeOffset? PlaybackStartsAtUtc { get; set; }
    public DateTimeOffset? PlaybackEndsAtUtc { get; set; }
    public int HighestLevelDefeated { get; set; }
    public int CurrentBossLevel { get; set; } = 1;
    public int CurrentBossMaxHealth { get; set; }
    public int CurrentBossHealthRemaining { get; set; }
    public int CurrentBossProgressBasisPoints { get; set; }
    public int DurationTicks { get; set; }
    public int FuryStacksAtEnd { get; set; }
    public RegionBossTerminationReason? TerminationReason { get; set; }
    public string? SimulationLeaseOwner { get; set; }
    public DateTimeOffset? SimulationLeaseUntil { get; set; }
    public int SimulationAttempts { get; set; }
    public string? LastError { get; set; }
    public long RowVersion { get; set; }
    public ICollection<RegionBossSignup> Members { get; set; } = [];
    public ICollection<RegionBossParticipantResult> ParticipantResults { get; set; } = [];
    public RegionBossPlayback? Playback { get; set; }
}

public sealed class RegionBossParticipantResult
{
    public Guid RegionBossRunId { get; set; }
    public RegionBossRun Run { get; set; } = null!;
    public Guid CharacterId { get; set; }
    public int DamageDone { get; set; }
    public int DamageTaken { get; set; }
    public int HealingDone { get; set; }
    public int HealingReceived { get; set; }
    public int BarrierGenerated { get; set; }
    public int DamagePrevented { get; set; }
    public int ThreatGenerated { get; set; }
    public int Deaths { get; set; }
    public int Revivals { get; set; }
    public int DownedTicks { get; set; }
}

public sealed class RegionBossPlayback
{
    public const int CompactBundleSchemaVersion = 1;

    public Guid RegionBossRunId { get; set; }
    public RegionBossRun Run { get; set; } = null!;
    public int SchemaVersion { get; set; } = CompactBundleSchemaVersion;
    public int TicksPerSecond { get; set; } = 10;
    public int TicksPerFrame { get; set; } = 10;
    public int TotalTicks { get; set; }
    public int FrameCount { get; set; }
    public string BundleHash { get; set; } = string.Empty;
    public int BundleLength { get; set; }
    public string BundleContentType { get; set; } = "application/json";
    public string BundleContentEncoding { get; set; } = "br";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public RegionBossPlaybackArtifact Artifact { get; set; } = null!;
}

public sealed class RegionBossPlaybackArtifact
{
    public Guid RegionBossRunId { get; set; }
    public RegionBossPlayback Playback { get; set; } = null!;
    public byte[] BundleBytes { get; set; } = [];
}

public sealed class RegionBossRewardGrant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RegionBossEventId { get; set; }
    public RegionBossEvent Event { get; set; } = null!;
    public Guid RegionBossRunId { get; set; }
    public string RegionBossDefinitionId { get; set; } = string.Empty;
    public Guid CharacterId { get; set; }
    public string RewardKey { get; set; } = string.Empty;
    public int MilestoneLevel { get; set; }
    public string RewardSnapshotJson { get; set; } = string.Empty;
    public RegionBossRewardStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ClaimedAtUtc { get; set; }
}
