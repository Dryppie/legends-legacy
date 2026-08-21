using Domain.Models.WorldTower;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;

namespace Application.UseCases.WorldTower.Dtos;

public sealed record TowerOverviewDto(
    string ServerId,
    int HighestUnlockedFloor,
    int HighestClearedFloor,
    bool EchoModeUnlocked,
    long TowerTokens,
    TowerFloorSummaryDto? CurrentFloor,
    IReadOnlyList<TowerFloorSummaryDto> Floors,
    IReadOnlyList<TowerRallySummaryDto> ActiveRallies,
    IReadOnlyList<TowerHallOfFameEntryDto> RecentClears);

public sealed record TowerFloorSummaryDto(
    int FloorNumber,
    string Name,
    TowerFloorType Type,
    TowerFloorStateType State,
    int RequiredSlots,
    int RecommendedPowerRating,
    int ScoutingProgress,
    string GuardianName);

public sealed record TowerFloorDetailDto(
    int FloorNumber,
    string Name,
    TowerFloorType Type,
    TowerFloorStateType State,
    int RequiredSlots,
    int RecommendedPowerRating,
    bool CanCreateRally,
    Guid? CurrentCharacterRallyId,
    bool CanCreateFirstClearRally,
    bool EchoAvailable,
    int ScoutingProgress,
    int WeeklyResearchContribution,
    int WeeklyResearchCap,
    TowerGuardianInfoDto Guardian,
    TowerPreparationSummaryDto Preparation,
    IReadOnlyList<TowerRallySummaryDto> ActiveRallies,
    IReadOnlyList<TowerUnlockDto> Unlocks,
    int FirstClearTowerTokens,
    int EchoTowerTokens,
    bool EchoRewardClaimedThisWeek);

public sealed record TowerGuardianInfoDto(
    string Name,
    IReadOnlyList<string> Tags,
    IReadOnlyList<TowerScoutingRevealDto> KnownReveals);

public sealed record TowerUnlockDto(string Key, string Description);

public sealed record TowerScoutingRevealDto(
    int Threshold,
    string Title,
    string Description,
    AbilitySpecKind Kind,
    int? CooldownSeconds,
    IReadOnlyList<string> Tags);

public sealed record TowerPreparationSummaryDto(
    decimal SupplyWeaponsPercent,
    decimal InscribeWardsPercent,
    decimal ScoutWeakPointsPercent,
    int WeeklyCharacterContribution,
    int WeeklyCharacterCap,
    decimal MaximumEffectPercent);

public sealed record TowerRallySummaryDto(
    Guid Id,
    int FloorNumber,
    TowerRallyMode Mode,
    string LeaderCharacterName,
    TowerRallyStatus Status,
    int ParticipantCount,
    int RequiredSlots,
    int PendingApplicationCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt);

public sealed record TowerRallyDto(
    Guid Id,
    int FloorNumber,
    string GuardianName,
    TowerRallyMode Mode,
    TowerRallyStatus Status,
    Guid CreatedByCharacterId,
    int RequiredSlots,
    int PartyCount,
    int MaximumPartySize,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TowerRallyParticipantDto> Participants,
    IReadOnlyList<TowerRallyApplicationDto> Applications,
    TowerRosterReadinessDto Readiness,
    bool CanApply,
    bool CanManageApplications,
    bool CanManageParties,
    bool CanLeave,
    bool CanStart,
    bool CanUpdateLoadout,
    bool CanTransferLeadership,
    TowerAttemptSummaryDto? Attempt);

public sealed record TowerRallyParticipantDto(
    Guid CharacterId,
    string CharacterName,
    string? GuildName,
    int PowerRating,
    DateTimeOffset JoinedAt,
    bool IsLeader,
    bool IsCurrentCharacter,
    int? PartySlot,
    int? PartyNumber);

public sealed record TowerRallyApplicationDto(
    Guid Id,
    Guid CharacterId,
    string CharacterName,
    string? GuildName,
    int PowerRating,
    TowerRallyApplicationStatus Status,
    DateTimeOffset AppliedAt,
    bool IsCurrentCharacter);

public sealed record TowerRosterReadinessDto(
    string Rating,
    int AveragePowerRating,
    int RecommendedPowerRating,
    IReadOnlyList<string> Warnings);

public sealed record TowerAttemptSummaryDto(
    Guid Id,
    TowerAttemptStatus Status,
    bool Succeeded,
    int? FightDurationSeconds,
    string? FailureReason,
    bool CanViewCombatResult,
    TowerCombatPlaybackDto? Playback,
    TowerBattleReportDto? BattleReport);

public sealed record TowerAttemptResultDto(
    Guid AttemptId,
    int FloorNumber,
    string GuardianName,
    TowerAttemptStatus Status,
    TowerCombatPlaybackDto? Playback);

public sealed record TowerCombatPlaybackDto(
    Guid AttemptId,
    Guid RallyId,
    DateTimeOffset PlaybackStartedAt,
    DateTimeOffset PlaybackEndsAt,
    int TicksPerSecond,
    int TicksPerFrame,
    int TotalTicks,
    int FrameCount,
    int CurrentSequence,
    TowerCombatFrameDto? CurrentFrame,
    bool IsCompleted,
    int SchemaVersion = 1,
    DateTimeOffset? ServerNow = null,
    string? BundleETag = null);

public sealed record TowerCombatFrameDto(
    int Sequence,
    int Tick,
    IReadOnlyList<SimpleCombatEntityDto> Friendly,
    IReadOnlyList<SimpleCombatEntityDto> Hostile,
    IReadOnlyList<EntityStats> EntityStats,
    IReadOnlyList<CombatEventDto> Events,
    bool IsFinal,
    BattleOutcome? Outcome);

public sealed record TowerCombatFrameBatchDto(
    Guid AttemptId,
    int AfterSequence,
    int CurrentSequence,
    bool HasMore,
    IReadOnlyList<TowerCombatFrameDto> Frames);

public sealed record CombatEventDto(
    string Source,
    string StatsSource,
    bool CountsAsActivation,
    int Timestamp,
    string ActorId,
    string TargetId,
    EventType EventType,
    int Magnitude,
    string Details);

public sealed record TowerBattleReportDto(
    int FloorNumber,
    string GuardianName,
    bool Succeeded,
    string? MainFailureReason,
    int FightDurationSeconds,
    decimal GuardianHealthRemainingPercent,
    IReadOnlyList<TowerParticipantCombatSummaryDto> Participants,
    TowerRosterReadinessDto RosterSummary);

public sealed record TowerParticipantCombatSummaryDto(
    Guid CharacterId,
    string CharacterName,
    decimal DamageDone,
    decimal DamageTaken,
    decimal HealingDone,
    bool Survived,
    int? PartyNumber = null);

public sealed record TowerHallOfFameEntryDto(
    int FloorNumber,
    string FloorName,
    string GuardianName,
    Guid AttemptId,
    DateTimeOffset ClearedAt,
    int AttemptNumber,
    int FightDurationSeconds,
    IReadOnlyList<TowerHallOfFameParticipantDto> Participants);

public sealed record TowerHallOfFameParticipantDto(
    Guid CharacterId,
    string CharacterName,
    string? GuildName,
    int PowerRating);

public sealed record TowerPersonalExpeditionDto(
    Guid RallyId,
    Guid AttemptId,
    int FloorNumber,
    string FloorName,
    string GuardianName,
    TowerRallyMode Mode,
    TowerAttemptStatus Status,
    int AttemptNumber,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int? FightDurationSeconds,
    IReadOnlyList<TowerHallOfFameParticipantDto> Participants);
