namespace Application.UseCases.Inventories.Dtos;

public sealed class TransferInventoryItemRequestDto
{
    public string RecipientName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

public sealed class TransferInventoryItemResponseDto
{
    public Guid ItemInstanceId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
