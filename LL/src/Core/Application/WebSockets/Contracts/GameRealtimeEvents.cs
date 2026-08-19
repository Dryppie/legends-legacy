using Application.UseCases.Characters.Dtos;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.WorldTower.Dtos;

namespace Application.WebSockets.Contracts;

public abstract record GameRealtimeEvent;

public sealed record DungeonRewardsClaimed(
    Guid CharacterId,
    IReadOnlyList<InventoryItemDto> ClaimedLoot,
    string? Location) : GameRealtimeEvent;

public sealed record LootReceived(
    Guid CharacterId,
    IReadOnlyList<InventoryItemDto> Items,
    string Source,
    string? Location,
    Guid? GrantId = null) : GameRealtimeEvent;

public sealed record InventorySnapshot(
    Guid CharacterId,
    IReadOnlyList<InventoryItemDto> Items,
    string Reason) : GameRealtimeEvent;

public sealed record CharacterSnapshot(
    Guid CharacterId,
    CharacterDto Character,
    string Reason) : GameRealtimeEvent;

public sealed record StateInvalidated(
    Guid? CharacterId,
    string Scope,
    long Revision,
    string Reason) : GameRealtimeEvent;

public sealed record AccountAccessChanged(
    Guid AccountId,
    string Reason,
    DateTimeOffset OccurredAtUtc) : GameRealtimeEvent;

public sealed record StateSyncCheckpoint(
    Guid CharacterId,
    IReadOnlyDictionary<string, long> Revisions,
    DateTimeOffset ServerTimeUtc);

public sealed record TournamentGroundsUpdated(
    Guid TournamentId,
    int TournamentNumber,
    string TournamentName,
    string Event,
    string Status,
    int RegisteredParticipantCount,
    int MinParticipants,
    int MaxParticipants,
    bool HasBracket,
    int? CurrentRoundNumber,
    DateTimeOffset? NextActionAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset OccurredAtUtc) : GameRealtimeEvent;

public sealed record WorldTowerRallyUpdated(
    Guid RallyId,
    int FloorNumber,
    string Event,
    string Status,
    int ParticipantCount,
    int RequiredSlots,
    int PendingApplicationCount,
    DateTimeOffset OccurredAtUtc) : GameRealtimeEvent;

public sealed record WorldTowerCombatFrameUpdated(
    Guid AttemptId,
    Guid RallyId,
    DateTimeOffset PlaybackStartedAt,
    int TicksPerSecond,
    int TicksPerFrame,
    TowerCombatFrameDto Frame) : GameRealtimeEvent;
