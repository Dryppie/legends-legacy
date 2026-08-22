using Domain.Models.RegionBosses;

namespace Application.UseCases.RegionBosses.Dtos;

public sealed record RegionBossStatusDto(
    Guid EventId,
    string DefinitionId,
    string Name,
    string ImagePath,
    int RegionId,
    RegionBossEventStatus Status,
    DateTimeOffset SignupStartsAtUtc,
    DateTimeOffset SignupClosesAtUtc,
    DateTimeOffset EncounterStartsAtUtc,
    DateTimeOffset? PlaybackStartsAtUtc,
    DateTimeOffset? PlaybackEndsAtUtc,
    DateTimeOffset ServerNowUtc,
    bool IsUnlocked,
    string? LockReason,
    bool IsSignedUp,
    int SignupCount,
    RegionBossRunSummaryDto? Run,
    IReadOnlyList<RegionBossRewardDto> Rewards);

public sealed record RegionBossRunSummaryDto(
    Guid RunId,
    int PartyNumber,
    RegionBossRunStatus Status,
    int HighestLevelDefeated,
    int CurrentBossLevel,
    int CurrentBossHealthRemaining,
    int CurrentBossMaxHealth,
    int CurrentBossProgressBasisPoints,
    int DurationTicks,
    int FuryStacks,
    RegionBossTerminationReason? TerminationReason,
    IReadOnlyList<RegionBossPartyMemberDto> Members,
    bool HasPlayback);

public sealed record RegionBossPartyMemberDto(
    Guid CharacterId,
    string CharacterName,
    int PartySlot,
    int PowerRating,
    RegionBossParticipantResultDto? Result);

public sealed record RegionBossParticipantResultDto(
    int DamageDone,
    int DamageTaken,
    int HealingDone,
    int HealingReceived,
    int BarrierGenerated,
    int DamagePrevented,
    int ThreatGenerated,
    int Deaths,
    int Revivals,
    int DownedTicks);

public sealed record RegionBossRewardDto(
    Guid GrantId,
    string RewardKey,
    int MilestoneLevel,
    RegionBossRewardStatus Status,
    int Cinders,
    int Soulstones,
    DateTimeOffset? ClaimedAtUtc);

public sealed record RegionBossClaimResultDto(
    Guid GrantId,
    int Cinders,
    int Soulstones,
    long CindersBalance,
    long SoulstonesBalance);

public sealed record RegionBossPlaybackDto(
    Guid RunId,
    int SchemaVersion,
    int TicksPerSecond,
    int TicksPerFrame,
    int TotalTicks,
    int FrameCount,
    string BundleETag);

public sealed record RegionBossPlaybackBundleContentDto(
    byte[] Bytes,
    string ContentType,
    string ContentEncoding,
    string ETag);

public sealed record RegionBossOperationResult<T>(T? Value, string? Error)
{
    public bool Succeeded => Error is null;
    public static RegionBossOperationResult<T> Success(T value) => new(value, null);
    public static RegionBossOperationResult<T> Fail(string error) => new(default, error);
}
