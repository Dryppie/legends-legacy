using Domain.Models.CharacterActions.Sessions;
using Services.LL.Combat.Layers.Rewards.Models;

namespace Services.LL.Interfaces.Combat.Reward.Dungeon;

public interface IDungeonCombatSessionFactory
{
    CombatSession Create(DungeonCombatRewardFacts facts, DungeonCombatCalculatedOutcome outcome);
}
