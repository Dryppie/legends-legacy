namespace Services.LL.WorldTower;

public sealed class WorldTowerOptions
{
    public const string SectionName = "WorldTower";

    public string ServerId { get; set; } = "default";
    public int EchoModeUnlockFloor { get; set; } = 5;
    public int FailedAttemptScoutingGain { get; set; } = 10;
    public int FailedAttemptScoutingWeeklyCap { get; set; } = 3;
    public int ManualScoutingWeeklyCapPerCharacter { get; set; } = 10;
    public int PreparationWeeklyCapPerCharacter { get; set; } = 10;
    public decimal PreparationPercentPerPoint { get; set; } = 0.25m;
    public decimal PreparationMaxEffectPercent { get; set; } = 5m;
    public int CombatTicksPerFrame { get; set; } = 10;
    public int PlaybackPollMilliseconds { get; set; } = 250;
    public bool DevelopmentToolsEnabled { get; set; }
}
