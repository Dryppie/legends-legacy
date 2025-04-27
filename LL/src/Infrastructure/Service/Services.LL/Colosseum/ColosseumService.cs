using System.Threading;
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
    private readonly ICharacterService _characterService;
    private readonly ICombatSetupService _combatSetupService;
    private readonly IColosseumRepository _colosseumRepository;

    public ColosseumService(IEntityService entityService, ICharacterService characterService, ICombatSetupService combatSetupService, IColosseumRepository colosseumRepository)
    {
        _entityService = entityService;
        _characterService = characterService;
        _combatSetupService = combatSetupService;
        _colosseumRepository = colosseumRepository;
    }

    public async Task<CombatResult> StartArenaBattle(Guid characterId, Guid enemyId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var arenaTicketStatus = await _colosseumRepository.GetArenaTicketStatusAsync(characterId, cancellationToken);

        if (arenaTicketStatus.CurrentTickets < 1) throw new Exception();
        arenaTicketStatus.CurrentTickets--;
        await _colosseumRepository.UpdateArenaTicketStatusAsync(arenaTicketStatus, cancellationToken);

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

    /// <summary>
    /// Get the opponents you are eligible to fight
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<Character>> GetArenaOpponents(Guid characterId, CancellationToken cancellationToken)
    {
        return await _colosseumRepository.GetArenaOpponents(characterId, cancellationToken);
    }

    public async Task SaveArenaMatchResult(Guid characterId, Guid enemyId, BattleOutcome outcome, CancellationToken cancellationToken)
    {
        var characterA = await _characterService.GetBaseCharacterByIdAsync(characterId, cancellationToken);
        var characterB = await _characterService.GetBaseCharacterByIdAsync(enemyId, cancellationToken);

        var arenaMatchResult = new ColosseumMatchResult()
        {
            CharacterAId = characterId,
            CharacterAName = characterA.Name,
            CharacterBId = enemyId,
            CharacterBName = characterB.Name,
            WinnerId = outcome == BattleOutcome.Victory ? characterId : outcome == BattleOutcome.Defeat ? enemyId : null,
            WinnerName = outcome == BattleOutcome.Victory ? characterA.Name : outcome == BattleOutcome.Defeat ? characterB.Name : "",
            PlayedAt = DateTimeOffset.UtcNow,
        };

        await _colosseumRepository.SaveArenaMatchResult(arenaMatchResult, cancellationToken);
    }

    public async Task<List<ColosseumMatchResult>> GetColosseumMatchResults(Guid characterId, CancellationToken cancellationToken)
    {
        return await _colosseumRepository.GetColosseumMatchResults(characterId, cancellationToken);
    }

    public async Task<List<ColosseumArenaRank>> GetRankings(Guid characterId, CancellationToken cancellationToken)
    {
        var characters = await _colosseumRepository.GetRankings(characterId, cancellationToken);

        var top50 = characters.Take(50).ToList();

        // Check if requester is in the top 50
        var inTop50 = top50.Any(r => r.Id == characterId);

        if (!inTop50)
        {
            // Find the requester's ranking (anywhere in the list)
            var requesterRank = characters.FirstOrDefault(r => r.Id == characterId);
            if (requesterRank != null)
            {
                top50.Add(requesterRank);
            }
        }

        var rankings = new List<ColosseumArenaRank>();
        var count = 1;
        foreach (var ranking in top50)
        {
            rankings.Add(new ColosseumArenaRank()
            {
                CharacterId = characterId,
                Character = ranking,
                Rating = ranking.ArenaRating,
                Rank = count++,
            });
        }
        // Get the top 50


        return rankings;
    }

    public async Task<ArenaTicketStatus> GetArenaTicketStatusAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var arenaTicketStatus = await _colosseumRepository.GetArenaTicketStatusAsync(characterId, cancellationToken);
        
        var restoreInterval = TimeSpan.FromHours(3);
        var timePassed = now - arenaTicketStatus.LastTicketUpdate;
        var ticketsToRestore = (int)(timePassed.TotalHours / restoreInterval.TotalHours);

        if (ticketsToRestore > 0)
        {
            arenaTicketStatus.CurrentTickets = Math.Min(arenaTicketStatus.CurrentTickets + ticketsToRestore, arenaTicketStatus.MaxTickets);
            // Update LastTicketUpdate based on restored tickets. Even if capped, a new ticket might still restore in..  17 minutes
            arenaTicketStatus.LastTicketUpdate = arenaTicketStatus.LastTicketUpdate.AddHours(ticketsToRestore * restoreInterval.TotalHours);

            await _colosseumRepository.UpdateArenaTicketStatusAsync(arenaTicketStatus, cancellationToken);
        }

        return arenaTicketStatus;
    }
}