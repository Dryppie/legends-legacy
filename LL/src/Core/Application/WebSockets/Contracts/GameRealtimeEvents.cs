using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.UseCases.Characters.Dtos;
using Application.UseCases.Inventories.Dtos;

namespace Application.WebSockets.Contracts;

public abstract record GameRealtimeEvent;

public sealed record DungeonRewardsClaimed(
    Guid CharacterId,
    IReadOnlyList<InventoryItemDto> ClaimedLoot) : GameRealtimeEvent;

public sealed record LootReceived(
    Guid CharacterId,
    IReadOnlyList<InventoryItemDto> Items,
    string Source) : GameRealtimeEvent;

public sealed record InventorySnapshot(
    Guid CharacterId,
    IReadOnlyList<InventoryItemDto> Items,
    string Reason) : GameRealtimeEvent;

public sealed record CharacterSnapshot(
    Guid CharacterId,
    CharacterDto Character,
    string Reason) : GameRealtimeEvent;

public sealed record IdleCombatProcessed(
    Guid CharacterId,
    CharacterActionDto Action) : GameRealtimeEvent;

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
