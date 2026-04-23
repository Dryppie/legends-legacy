using Domain.Models.CharacterActions.Sessions;
using Services.LL.Combat.Layers.Rewards.Models;

namespace Services.LL.Interfaces.Combat.Reward.Idle;

public interface IIdleCombatSessionFactory
{
    CombatSession Create(IdleCombatRewardFacts facts, IdleCombatCalculatedOutcome outcome);
}