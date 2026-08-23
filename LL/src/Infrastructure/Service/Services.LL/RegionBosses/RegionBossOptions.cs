namespace Services.LL.RegionBosses;

public sealed class RegionBossOptions
{
    public const string SectionName = "RegionBosses";

    public bool DevelopmentToolsEnabled { get; set; }
    public int DevelopmentProgressionIntervalSeconds { get; set; } = 2;
    public int MaximumEventsPerProgression { get; set; } = 10;
    public int MaximumRunResolutionsPerEvent { get; set; } = 25;
}
