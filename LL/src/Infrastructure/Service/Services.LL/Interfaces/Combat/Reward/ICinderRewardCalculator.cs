using Domain.Models.Entities.Creatures;

namespace Services.LL.Interfaces.Combat.Reward;

public interface ICinderRewardCalculator
{
    int Calculate(IReadOnlyCollection<Creature> defeatedCreatures);
}
