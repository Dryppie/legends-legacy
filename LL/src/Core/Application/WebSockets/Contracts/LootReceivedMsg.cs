using Application.Common.Mappings;
using Application.UseCases.Inventories.Dtos;

namespace Application.WebSockets.Contracts;
public record LootReceivedMsg(
    IReadOnlyList<InventoryItemDto> Payload
) : IMessage
{
    public string Type => "loot";
}
