using Domain.Models.Combat;

namespace Application.UseCases.Colosseum.Tournaments;

public sealed record TournamentPlaybackManifestDto(
    Guid TournamentId,
    Guid MatchId,
    int SchemaVersion,
    int TicksPerSecond,
    int TicksPerFrame,
    int TotalTicks,
    int OvertimeStartsAtTick,
    int OvertimeDurationTicks,
    int OvertimePowerIncreaseIntervalTicks,
    float OvertimePowerIncreasePercent,
    int FrameCount,
    DateTimeOffset PlaybackStartedAtUtc,
    DateTimeOffset PlaybackEndsAtUtc,
    DateTimeOffset ServerNowUtc,
    int CurrentSequence,
    bool IsCompleted,
    string BundleETag);

public sealed record TournamentPlaybackBundleContentDto(
    byte[] Bytes,
    string ContentType,
    string ContentEncoding,
    string ETag);

public sealed record TournamentPlaybackBundleDto(
    int SchemaVersion,
    int TicksPerSecond,
    int TicksPerFrame,
    int TotalTicks,
    IReadOnlyList<TournamentPlaybackEntityDto> Entities,
    IReadOnlyList<TournamentPlaybackAbilityDto> Abilities,
    IReadOnlyList<TournamentPlaybackFrameDto> Frames);

public sealed record TournamentPlaybackEntityDto(
    int Index,
    string Id,
    string Name,
    string ImagePath,
    bool IsFriendly,
    int MaxHealth,
    int Level);

public sealed record TournamentPlaybackAbilityDto(
    int Index,
    int EntityIndex,
    string Name);

public sealed record TournamentPlaybackFrameDto(
    int Sequence,
    int Tick,
    IReadOnlyList<TournamentPlaybackEntityStateDto> EntityStates,
    IReadOnlyList<TournamentPlaybackEntityTotalsDto> EntityTotals,
    IReadOnlyList<TournamentPlaybackAbilityTotalsDto> AbilityTotals,
    bool IsFinal,
    BattleOutcome? Outcome);

public sealed record TournamentPlaybackEntityStateDto(
    int EntityIndex,
    int Health,
    int Barrier);

public sealed record TournamentPlaybackEntityTotalsDto(
    int EntityIndex,
    int DamageDone,
    int DamageTaken,
    int HealingDone,
    int HealingReceived,
    int HealthRegenerated,
    int BarrierGenerated,
    int DamageBlocked);

public sealed record TournamentPlaybackAbilityTotalsDto(
    int AbilityIndex,
    int Uses,
    int TotalDamage,
    int TotalHealing,
    int TotalBarrier);
