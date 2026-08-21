using Domain.Models.Combat;
using Domain.Models.Raids;

namespace Application.UseCases.Raids.Dtos;

public sealed record RaidBossSummaryDto(
    string Id,
    string Name,
    int Region,
    IReadOnlyList<int> Regions,
    int LevelRequirement,
    string ImagePath,
    bool IsUnlocked,
    string? LockReason,
    int OpenRaidCount,
    bool HasWeeklyRewardThisWeek,
    Guid? ActiveRaidId,
    IReadOnlyList<RaidBossTierSummaryDto> Tiers,
    bool DevelopmentToolsEnabled);

public sealed record RaidBossTierSummaryDto(
    int Tier,
    int LaneSlots,
    int MinimumRoster,
    int SignupWindowHours,
    RaidRecommendedWingPowerDto RecommendedWingPower);

public sealed record RaidRecommendedWingPowerDto(
    int Rearguard,
    int Vanguard,
    int MainGuard);

public sealed record RaidRunSummaryDto(
    Guid Id,
    string RaidBossId,
    string RaidBossName,
    int Tier,
    Guid LeaderCharacterId,
    string LeaderCharacterName,
    RaidRunStatus Status,
    DateTimeOffset SignupClosesAt,
    int SignupCount,
    int MaximumRoster,
    int RearguardCount,
    int VanguardCount,
    int MainGuardCount,
    bool CanJoin);

public sealed record RaidHistoryEntryDto(
    Guid RaidRunId,
    string RaidBossId,
    string RaidBossName,
    int Tier,
    RaidOutcome Outcome,
    DateTimeOffset ResolvedAt,
    int Trophies,
    RaidRewardKind RewardKind,
    DateTimeOffset? ClaimedAt,
    bool CanClaim);

public sealed record RaidRunDto(
    Guid Id,
    string RaidBossId,
    string RaidBossName,
    string ImagePath,
    int Region,
    int Tier,
    RaidRunStatus Status,
    Guid LeaderCharacterId,
    DateTimeOffset CreatedAt,
    DateTimeOffset SignupClosesAt,
    DateTimeOffset? CommencedAt,
    DateTimeOffset? PlaybackStartedAt,
    DateTimeOffset? PlaybackEndsAt,
    DateTimeOffset ServerNow,
    DateTimeOffset? ResolvedAt,
    int LaneSlots,
    int MinimumRoster,
    IReadOnlyList<RaidSignupDto> Signups,
    IReadOnlyList<RaidJoinRequestDto> JoinRequests,
    IReadOnlyList<RaidLaneResultDto> LaneResults,
    IReadOnlyList<RaidParticipantResultDto> ParticipantResults,
    RaidOutcome? Outcome,
    decimal? ReinforcementPenalty,
    decimal? GuardianBreak,
    decimal? SignatureDisruption,
    decimal? BossHealthRemainingPercent,
    bool CanJoin,
    bool CanLeave,
    bool CanAssign,
    bool CanCommence,
    bool CanRefreshSnapshot,
    bool CanClaim,
    RaidRewardKind? RewardKind,
    bool CanPreviewBattlePlan,
    bool CanCancel,
    bool CanTransferLeadership,
    bool DevelopmentToolsEnabled);

public sealed record RaidSignupDto(
    Guid CharacterId,
    string CharacterName,
    int PowerRating,
    RaidLane? Lane,
    int? WingSlotIndex,
    DateTimeOffset SignedUpAt,
    DateTimeOffset? SnapshotRefreshedAt,
    bool IsLeader,
    bool IsCurrentCharacter);

public sealed record RaidJoinRequestDto(
    Guid CharacterId,
    string CharacterName,
    int PowerRating,
    DateTimeOffset RequestedAt,
    DateTimeOffset? SnapshotRefreshedAt,
    bool IsCurrentCharacter);

public sealed record RaidLaneResultDto(
    RaidLane Lane,
    int DurationTicks,
    BattleOutcome BattleOutcome,
    long TotalFriendlyDamage,
    decimal SurvivingHostileHealthFraction,
    decimal DerivedModifier,
    bool HasPlayback);

public sealed record RaidBattlePlanPreviewDto(
    Guid RaidRunId,
    DateTimeOffset GeneratedAt,
    int SampleCount,
    string Readiness,
    RaidOutcome PredictedOutcome,
    decimal SlainProbability,
    decimal SlainProbabilityLower,
    decimal SlainProbabilityUpper,
    IReadOnlyList<RaidBattlePlanLaneDto> Lanes,
    IReadOnlyDictionary<RaidOutcome, int> OutcomeCounts);

public sealed record RaidBattlePlanLaneDto(
    RaidLane Lane,
    string Readiness,
    decimal SuccessProbability,
    decimal SuccessProbabilityLower,
    decimal SuccessProbabilityUpper,
    int AverageDurationTicks,
    decimal ExpectedDerivedModifier,
    decimal DerivedModifierLower,
    decimal DerivedModifierUpper);

public sealed record RaidPlaybackDto(
    Guid RaidRunId,
    RaidLane Lane,
    int SchemaVersion,
    int TicksPerSecond,
    int TicksPerFrame,
    int TotalTicks,
    int FrameCount,
    string BundleETag);

public sealed record RaidPlaybackBundleContentDto(
    byte[] Bytes,
    string ContentType,
    string ContentEncoding,
    string ETag);

public sealed record RaidPlaybackBundleDto(
    int SchemaVersion,
    int TicksPerSecond,
    int TicksPerFrame,
    int TotalTicks,
    IReadOnlyList<RaidPlaybackEntityDto> Entities,
    IReadOnlyList<RaidPlaybackAbilityDto> Abilities,
    IReadOnlyList<RaidPlaybackFrameDto> Frames);

public sealed record RaidPlaybackEntityDto(
    int Index,
    string Id,
    string Name,
    string ImagePath,
    bool IsFriendly,
    int MaxHealth,
    int Level,
    int? PartyNumber = null);

public sealed record RaidPlaybackAbilityDto(int Index, int EntityIndex, string Name);

public sealed record RaidPlaybackFrameDto(
    int Sequence,
    int Tick,
    IReadOnlyList<RaidPlaybackEntityStateDto> EntityStates,
    IReadOnlyList<RaidPlaybackEntityTotalsDto> EntityTotals,
    IReadOnlyList<RaidPlaybackAbilityTotalsDto> AbilityTotals,
    bool IsFinal,
    BattleOutcome? Outcome);

public sealed record RaidPlaybackEntityStateDto(int EntityIndex, int Health, int Barrier);

public sealed record RaidPlaybackEntityTotalsDto(
    int EntityIndex,
    int DamageDone,
    int DamageTaken,
    int HealingDone,
    int HealingReceived,
    int HealthRegenerated,
    int BarrierGenerated,
    int DamageBlocked,
    int ThreatGenerated = 0);

public sealed record RaidPlaybackAbilityTotalsDto(
    int AbilityIndex,
    int Uses,
    int TotalDamage,
    int TotalHealing,
    int TotalBarrier,
    IReadOnlyList<AbilityDamageTypeStats>? DamageByType = null,
    int TotalThreat = 0);

public sealed record RaidParticipantResultDto(
    Guid CharacterId,
    RaidLane Lane,
    long DamageDone,
    decimal ContributionScore,
    int ContributionRank);

public sealed record RaidRewardDto(
    Guid RaidRunId,
    int Trophies,
    long TrophyBalance,
    IReadOnlyList<RaidRewardItemDto> Items,
    RaidRewardKind RewardKind,
    DateTimeOffset ClaimedAt);

public sealed record RaidRewardItemDto(string ItemId, string ItemName, int Quantity);

public sealed record RaidTrophyVendorDto(
    string RaidBossId,
    long TrophyBalance,
    IReadOnlyList<RaidTrophyVendorItemDto> Items);

public sealed record RaidTrophyVendorItemDto(
    string Id,
    string Name,
    string Description,
    string Category,
    int TrophyCost,
    string RewardItemId,
    int RewardQuantity,
    int? WeeklyPurchaseLimit,
    int WeeklyPurchased,
    int? LifetimePurchaseLimit,
    int LifetimePurchased,
    int RequiredTier,
    bool IsUnlocked,
    bool CanPurchase);

public sealed record RaidTrophyPurchaseDto(
    string RaidBossId,
    string VendorItemId,
    string RewardItemId,
    int RewardQuantity,
    int TrophiesSpent,
    long TrophyBalance,
    DateTimeOffset PurchasedAt);
