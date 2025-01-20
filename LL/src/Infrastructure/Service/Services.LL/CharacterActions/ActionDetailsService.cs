using Application.Interfaces.Services.LL;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Services.LL.Interfaces;

namespace Services.LL.CharacterActions;
public class ActionDetailsService : IActionDetailsService
{
    private readonly IEntityService _entityService;
    private readonly ICreatureService _creatureService;
    public ActionDetailsService(IEntityService entityService, ICreatureService creatureService)
    {
        _entityService = entityService;
        _creatureService = creatureService;
    }
    public async Task<CombatActionDetails> CreateCombatActionDetailsFromAreaNameAsync(string areaName, Guid characterId, CancellationToken cancellationToken)
    {
        var combatDetails = new CombatActionDetails
        {
            CharacterTeam = [characterId], /*_entityService.FindCharacterTeamById();*/
            EnemyTeam = await _creatureService.GetCreatureIdsByArea(areaName, cancellationToken)
        };

        return combatDetails;
    }
}