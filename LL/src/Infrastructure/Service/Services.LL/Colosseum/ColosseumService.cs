using Application.Interfaces.Services.LL;
using Domain.Models.Colosseum;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Services.LL.Combat;
using Services.LL.Interfaces;

namespace Services.LL.Colosseum;
public class ColosseumService : IColosseumService
{
    private readonly IEntityService _entityService;
    private readonly ICombatSetupService _combatSetupService;
    private readonly IColosseumRepository _colosseumRepository;

    public ColosseumService(IEntityService entityService, ICombatSetupService combatSetupService, IColosseumRepository colosseumRepository)
    {
        _entityService = entityService;
        _combatSetupService = combatSetupService;
        _colosseumRepository = colosseumRepository;
    }

    public async Task<CombatResult> StartArenaBattle(Guid characterId, Guid enemyId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var matchResult = new ColosseumMatchResult();
        var playerTeam = await _entityService.GetEntitiesByIdsForCombatAsync([characterId], cancellationToken);
        var enemyTeam = await _entityService.GetEntitiesByIdsForCombatAsync([enemyId], cancellationToken);

        var combatPlayerEntities = _combatSetupService.CreateCombatEntities(playerTeam);
        var combatEnemyEntities = _combatSetupService.CreateCombatEntities(enemyTeam);
        await _combatSetupService.PrepareEntitiesForCombat([.. combatPlayerEntities, .. combatEnemyEntities]);

        var combatSimulation = new CombatSimulation(combatPlayerEntities, combatEnemyEntities);
        var combatResult = combatSimulation.RunSimulation();
        combatResult.StartedAt = now;

        combatResult.PlayerTeam = _combatSetupService.CreateSimpleCombatEntities(combatPlayerEntities);
        combatResult.EnemyTeam = _combatSetupService.CreateSimpleCombatEntities(combatEnemyEntities);

        return combatResult;
    }



    public async Task<List<Character>> GetArenaOpponents(Guid characterId, CancellationToken cancellationToken)
    {
        return await _colosseumRepository.GetArenaOpponents(characterId, cancellationToken);
    }
}