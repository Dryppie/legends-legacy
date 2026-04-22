using Domain.Models.Entities.Creatures;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class DefaultIdleCinderRewardCalculator : ICinderRewardCalculator
{
    public int Calculate(IReadOnlyCollection<Creature> defeatedCreatures)
    {
        return defeatedCreatures.Sum(x => x.ExperienceReward * 10);
    }
}