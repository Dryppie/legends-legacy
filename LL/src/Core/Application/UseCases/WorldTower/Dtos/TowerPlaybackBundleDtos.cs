using Domain.Models.Combat;

namespace Application.UseCases.WorldTower.Dtos;

public sealed record TowerPlaybackBundleDto(
    int SchemaVersion,
    int TicksPerSecond,
    int TicksPerFrame,
    int TotalTicks,
    IReadOnlyList<TowerPlaybackEntityDto> Entities,
    IReadOnlyList<TowerPlaybackAbilityDto> Abilities,
    IReadOnlyList<TowerPlaybackBundleFrameDto> Frames);

public sealed record TowerPlaybackEntityDto(
    int Index,
    string Id,
    string Name,
    string ImagePath,
    bool IsFriendly,
    int MaxHealth,
    int Level,
    int? PartyNumber = null);

public sealed record TowerPlaybackAbilityDto(
    int Index,
    int EntityIndex,
    string Name);

public sealed record TowerPlaybackBundleFrameDto(
    int Sequence,
    int Tick,
    IReadOnlyList<TowerPlaybackEntityStateDto> EntityStates,
    IReadOnlyList<TowerPlaybackEntityTotalsDto> EntityTotals,
    IReadOnlyList<TowerPlaybackAbilityTotalsDto> AbilityTotals,
    bool IsFinal,
    BattleOutcome? Outcome);

public sealed record TowerPlaybackEntityStateDto(
    int EntityIndex,
    int Health,
    int Barrier,
    int CurrentStagger = 0,
    int MaxStagger = 0,
    bool IsStaggered = false,
    bool IsStaggerRecovering = false);

public sealed record TowerPlaybackEntityTotalsDto(
    int EntityIndex,
    int DamageDone,
    int DamageTaken,
    int HealingDone,
    int HealingReceived,
    int HealthRegenerated,
    int BarrierGenerated,
    int DamageBlocked,
    int ThreatGenerated = 0,
    int TargetedAttacks = 0,
    double AttentionSharePercent = 0,
    int StaggerContributed = 0,
    int StaggerBreaks = 0);

public sealed record TowerPlaybackAbilityTotalsDto(
    int AbilityIndex,
    int Uses,
    int TotalDamage,
    int TotalHealing,
    int TotalBarrier,
    IReadOnlyList<AbilityDamageTypeStats>? DamageByType = null,
    int TotalThreat = 0,
    int TotalStagger = 0,
    int StaggerBreaks = 0);

public sealed record TowerPlaybackBundleContentDto(
    byte[] Bytes,
    string ContentType,
    string ContentEncoding,
    string ETag);
