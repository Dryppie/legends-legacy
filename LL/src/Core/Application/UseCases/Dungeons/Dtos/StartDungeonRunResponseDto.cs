using Application.UseCases.Inventories.Dtos;

namespace Application.UseCases.Dungeons.Dtos;

public sealed class StartDungeonRunResponseDto
{
    public required DungeonRunDto Run { get; init; }
    public List<InventoryItemDto>? InventoryItems { get; init; }
}
