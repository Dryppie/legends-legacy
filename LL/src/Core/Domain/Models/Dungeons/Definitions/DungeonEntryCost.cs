namespace Domain.Models.Dungeons.Definitions;

public sealed class DungeonEntryCost
{
    public string ItemId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public bool ConsumedOnEntry { get; set; } = true;
}
