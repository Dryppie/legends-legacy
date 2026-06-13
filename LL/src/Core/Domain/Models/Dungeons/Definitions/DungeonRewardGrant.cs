namespace Domain.Models.Dungeons.Definitions;

public sealed class DungeonRewardGrant
{
    public string ItemId { get; set; } = string.Empty;
    public int MinAmount { get; set; }
    public int MaxAmount { get; set; }
    public double Chance { get; set; } = 1;
}
