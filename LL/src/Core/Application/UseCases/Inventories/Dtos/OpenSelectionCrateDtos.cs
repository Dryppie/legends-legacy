namespace Application.UseCases.Inventories.Dtos;

public sealed class OpenSelectionCrateRequestDto
{
    public string OptionId { get; set; } = string.Empty;
}

public sealed class OpenSelectionCrateResultDto
{
    public Guid ConsumedItemInstanceId { get; set; }
    public Guid GrantId { get; set; }
    public List<InventoryItemDto> Rewards { get; set; } = [];
    public required IReadOnlyList<InventoryItemDto> InventoryItems { get; init; }
}
