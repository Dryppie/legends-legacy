namespace Domain.Models.Dungeons.Runs;

public sealed class RunReward
{
    public string ItemId { get; set; } = default!;
    public int Quantity { get; set; }
    public string Source { get; set; } = default!; // e.g. "floor:3", "boss", "treasure"
}
