using Domain.Models.Combat;

namespace Application.Interfaces.Services.LL.Tutorials;

public interface ITutorialBattleService
{
    Task<CombatResult?> StartTrainingBattleAsync(Guid characterId, CancellationToken cancellationToken);
}
