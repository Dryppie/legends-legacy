namespace Domain.Models.Dungeons.PowerRatings;

public sealed class DungeonPowerRecommendationCacheEntry
{
    public string DungeonId { get; set; } = null!;
    public int DungeonTier { get; set; }
    public string DungeonContentHash { get; set; } = null!;
    public int AlgorithmVersion { get; set; }
    public int CombatRulesVersion { get; set; }
    public int BenchmarkDefinitionVersion { get; set; }
    public int RecommendationSeedSetVersion { get; set; }
    public string RecommendationJson { get; set; } = null!;
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
