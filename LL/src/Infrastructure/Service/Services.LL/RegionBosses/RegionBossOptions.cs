namespace Services.LL.RegionBosses;

public sealed class RegionBossOptions
{
    public const string SectionName = "RegionBosses";

    public bool DevelopmentToolsEnabled { get; set; }
    public int DevelopmentProgressionIntervalSeconds { get; set; } = 2;
}
