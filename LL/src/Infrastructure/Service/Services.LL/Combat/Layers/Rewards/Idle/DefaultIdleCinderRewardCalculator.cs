using Domain.Models.Entities.Creatures;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class DefaultIdleCinderRewardCalculator : ICinderRewardCalculator
{
    private readonly CombatCinderRewardOptions _options;

    public DefaultIdleCinderRewardCalculator(IOptions<CombatCinderRewardOptions> options)
    {
        _options = options.Value;
    }

    public int Calculate(IReadOnlyCollection<Creature> defeatedCreatures)
    {
        var totalCreatureExperience = defeatedCreatures.Sum(x => Math.Max(0, x.ExperienceReward));
        if (totalCreatureExperience == 0)
        {
            return 0;
        }

        if (_options.RewardBasisPointsOfCreatureExperience <= 0 ||
            _options.MinimumCindersPerVictory < 0)
        {
            throw new InvalidOperationException("Combat Cinder reward settings are invalid.");
        }

        var scaledReward = (int)Math.Ceiling(
            (double)totalCreatureExperience * _options.RewardBasisPointsOfCreatureExperience / 10_000d);
        return Math.Max(_options.MinimumCindersPerVictory, scaledReward);
    }
}
