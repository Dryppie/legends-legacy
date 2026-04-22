using Domain.Models.CharacterActions.Sessions;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Models;

namespace Services.LL.Interfaces.Combat.Reward;

public interface ICombatOutcomeProcessor
{
    CombatMode Mode { get; }

    Task<CombatSession> ApplyAsync(
        CombatOutcomeRequest request,
        CancellationToken cancellationToken);
}