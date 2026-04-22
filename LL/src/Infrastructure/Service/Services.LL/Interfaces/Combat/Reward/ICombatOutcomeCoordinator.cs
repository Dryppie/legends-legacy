using Domain.Models.CharacterActions.Sessions;
using Services.LL.Combat.Layers.Rewards.Models;

namespace Services.LL.Interfaces.Combat.Reward;

public interface ICombatOutcomeCoordinator
{
    Task<CombatSession> ApplyAsync(
        CombatOutcomeRequest request,
        CancellationToken cancellationToken);
}
