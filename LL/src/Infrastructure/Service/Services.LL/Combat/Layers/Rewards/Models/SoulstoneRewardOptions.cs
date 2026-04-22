namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed class SoulstoneRewardOptions
{
    // 1 expected drop per 3600 seconds before modifiers.
    public double BaseDropRatePerSecond { get; set; } = 1d / 3600d;
}