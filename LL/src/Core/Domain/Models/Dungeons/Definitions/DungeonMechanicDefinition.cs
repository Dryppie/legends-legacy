namespace Domain.Models.Dungeons.Definitions;

public sealed class DungeonMechanicDefinition
{
    public string Id { get; set; } = "pressure";
    public string DisplayName { get; set; } = "Pressure";
    public int InitialValue { get; set; } = 0;
    public int MaxValue { get; set; } = 100;
    public List<DungeonMechanicThreshold> Thresholds { get; set; } = [];
}

public sealed class DungeonMechanicThreshold
{
    public string Id { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> EnemyModifierIds { get; set; } = [];
    public List<string> BossModifierIds { get; set; } = [];
    public int RewardMultiplierBonusPercent { get; set; }
}
