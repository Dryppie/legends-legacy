using Domain.Models.Items;

namespace Domain.Models.Dungeons.Runs;

public sealed class RunReward
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ItemId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public int Quantity { get; set; }
    public string Source { get; set; } = string.Empty; // e.g. "room:3", "boss", "treasure"
}
