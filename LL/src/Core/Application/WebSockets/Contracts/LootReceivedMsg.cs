using Application.UseCases.Inventories.Dtos;

namespace Application.WebSockets.Contracts;
public record LootReceivedMsg(Guid CharacterId, IReadOnlyList<InventoryItemDto> Payload) : GameEventMsg;
