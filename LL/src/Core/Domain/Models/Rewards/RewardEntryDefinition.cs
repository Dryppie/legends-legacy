namespace Domain.Models.Rewards;

public sealed class RewardEntryDefinition
{
    public string Id { get; set; } = string.Empty;
    public RewardEntryType Type { get; set; } = RewardEntryType.Item;
    public string? ItemId { get; set; }
    public string? RewardTableId { get; set; }
    public double Weight { get; set; }
    public double Chance { get; set; } = 1;
    public RewardQuantityRange Quantity { get; set; } = new();
    public List<string> Tags { get; set; } = [];
}
