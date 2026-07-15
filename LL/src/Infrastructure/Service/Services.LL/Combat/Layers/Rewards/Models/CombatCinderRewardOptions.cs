namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed class CombatCinderRewardOptions
{
    public int RewardBasisPointsOfCreatureExperience { get; set; } = 2000;
    public int MinimumCindersPerVictory { get; set; } = 1;
}
