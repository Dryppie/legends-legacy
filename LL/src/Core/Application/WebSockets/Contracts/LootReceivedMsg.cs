using Application.UseCases.Inventories.Dtos;

namespace Application.WebSockets.Contracts;
public record LootReceivedMsg(
    Guid CharacterId,
    IReadOnlyList<InventoryItemDto> Payload,
    string Source = "combat-reward",
    string? Location = null,
    Guid? GrantId = null) : GameEventMsg;
