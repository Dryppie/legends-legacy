namespace Application.UseCases.Inventories.Dtos;

public sealed class MarkInventoryItemSeenResponseDto
{
    public Guid ItemInstanceId { get; set; }
    public required IReadOnlyList<InventoryItemDto> InventoryItems { get; init; }
}
