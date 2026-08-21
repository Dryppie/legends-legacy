using Application.UseCases.Inventories.Dtos;

namespace Application.UseCases.Dungeons.Dtos;

public sealed class StartDungeonRunResponseDto
{
    public required DungeonRunDto Run { get; init; }
    public required List<InventoryItemDto> InventoryItems { get; init; }
    public required DungeonHubDto Hub { get; init; }
}
