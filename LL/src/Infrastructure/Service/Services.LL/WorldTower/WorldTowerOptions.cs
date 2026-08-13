namespace Services.LL.WorldTower;

public sealed class WorldTowerOptions
{
    public const string SectionName = "WorldTower";

    public string ServerId { get; set; } = "default";
    public int FailedAttemptScoutingGain { get; set; } = 10;
    public int FailedAttemptScoutingWeeklyCap { get; set; } = 3;
    public int ManualScoutingWeeklyCapPerCharacter { get; set; } = 3;
    public int PreparationWeeklyCapPerCharacter { get; set; } = 3;
    public decimal PreparationPercentPerPoint { get; set; } = 0.25m;
    public decimal PreparationMaxEffectPercent { get; set; } = 10m;
    public int CombatTicksPerFrame { get; set; } = 10;
    public bool CompactPlaybackEnabled { get; set; } = true;
    public int PlaybackPollMilliseconds { get; set; } = 250;
    public int FinalizationPollMilliseconds { get; set; } = 1000;
    public int SimulationPollMilliseconds { get; set; } = 250;
    public int WorkerLeaseSeconds { get; set; } = 30;
    public int SimulationClaimBatchSize { get; set; } = 2;
    public int SimulationMaxConcurrency { get; set; } = 1;
    public int PlaybackClaimBatchSize { get; set; } = 50;
    public int RecoveryFrameLimit { get; set; } = 60;
    public int MaximumBundleUncompressedBytes { get; set; } = 16 * 1024 * 1024;
    public int MaximumBundleCompressedBytes { get; set; } = 4 * 1024 * 1024;
    public bool DevelopmentToolsEnabled { get; set; }
}
