using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.UseCases.Characters.Dtos;
using Application.UseCases.Inventories.Dtos;

namespace Application.WebSockets.Contracts.V2;

public abstract record GameRealtimeEventV2;

public sealed record DungeonRewardsClaimedV2(
    Guid CharacterId,
    IReadOnlyList<InventoryItemDto> ClaimedLoot) : GameRealtimeEventV2;

public sealed record LootReceivedV2(
    Guid CharacterId,
    IReadOnlyList<InventoryItemDto> Items,
    string Source) : GameRealtimeEventV2;

public sealed record InventorySnapshotV2(
    Guid CharacterId,
    IReadOnlyList<InventoryItemDto> Items,
    string Reason) : GameRealtimeEventV2;

public sealed record CharacterSnapshotV2(
    Guid CharacterId,
    CharacterDto Character,
    string Reason) : GameRealtimeEventV2;

public sealed record IdleCombatProcessedV2(
    Guid CharacterId,
    CharacterActionDto Action) : GameRealtimeEventV2;
