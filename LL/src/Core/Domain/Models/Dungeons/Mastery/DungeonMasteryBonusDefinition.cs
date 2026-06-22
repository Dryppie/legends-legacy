namespace Domain.Models.Dungeons.Mastery;

public sealed class DungeonMasteryBonusDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int RequiredLevel { get; set; }
    public int RewardMultiplierBonusPercent { get; set; }
    public List<string> AddFlags { get; set; } = [];
}

public sealed class DungeonMasteryAwardReason
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long Experience { get; set; }
}
