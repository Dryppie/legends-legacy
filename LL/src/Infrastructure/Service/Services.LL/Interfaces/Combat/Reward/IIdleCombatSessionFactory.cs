using Domain.Models.CharacterActions.Sessions;
using Services.LL.Combat.Layers.Rewards.Models;

namespace Services.LL.Interfaces.Combat.Reward;

public interface IIdleCombatSessionFactory
{
    CombatSession Create(
        CombatOutcomeRequest request,
        IdleCombatRewardFacts facts,
        IdleCombatCalculatedOutcome outcome);
}