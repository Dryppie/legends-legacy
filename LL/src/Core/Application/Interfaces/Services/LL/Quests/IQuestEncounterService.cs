using Domain.Models.Combat;

namespace Application.Interfaces.Services.LL.Quests;

public interface IQuestEncounterService
{
    Task<CombatResult?> StartAsync(
        Guid characterId,
        string questId,
        string encounterKey,
        CancellationToken cancellationToken);
}
