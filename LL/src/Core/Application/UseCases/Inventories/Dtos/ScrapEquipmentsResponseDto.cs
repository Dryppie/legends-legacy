namespace Application.UseCases.Inventories.Dtos;

public sealed class ScrapEquipmentsResponseDto
{
    public required InventoryItemDto GainedItem { get; init; }
    public required IReadOnlyList<InventoryItemDto> InventoryItems { get; init; }
}
