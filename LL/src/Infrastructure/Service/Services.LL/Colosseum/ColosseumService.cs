using Application.Interfaces.Services.LL.Colosseum;
using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Colosseum;
using Domain.Models.Combat;
using Domain.Models.Leaderboards;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Colosseum;
public class ColosseumService : IColosseumService
{
    private readonly IEntityService _entityService;
    private readonly ICharacterService _characterService;
    private readonly ICombatSetupService _combatSetupService;
    private readonly IColosseumRepository _colosseumRepository;
    private readonly ICombatEngineExecutor _combatEngineExecutor;
    private readonly ICombatEncounterResultFactory _combatEncounterResultFactory;
    private readonly IRatingService _ratingService;

    public ColosseumService(
        IEntityService es,
        ICharacterService cs,
        ICombatSetupService css,
        IColosseumRepository cr,
        ICombatEngineExecutor combatEngineExecutor,
        ICombatEncounterResultFactory combatEncounterResultFactory,
        IRatingService rs)
    {
        _entityService = es;
        _characterService = cs;
        _combatSetupService = css;
        _colosseumRepository = cr;
        _combatEngineExecutor = combatEngineExecutor;
        _combatEncounterResultFactory = combatEncounterResultFactory;
        _ratingService = rs;
    }

    public async Task<StartArenaBattleResult?> StartArenaBattle(Guid characterId, Guid enemyId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var arenaTicketStatus = await GetArenaTicketStatusAsync(characterId, cancellationToken);

        if (arenaTicketStatus.CurrentTickets < 1) return null;
        arenaTicketStatus.CurrentTickets--;
        _colosseumRepository.UpdateArenaTicketStatus(arenaTicketStatus);

        var playerTeam = await _entityService.GetEntitiesByIdsForCombatAsync([characterId], cancellationToken);
        if (playerTeam.Count == 0) return null;
        var enemyTeam = await _entityService.GetEntitiesByIdsForCombatAsync([enemyId], cancellationToken);
        if (enemyTeam.Count == 0) return null;

        var combatPlayerEntities = _combatSetupService.CreatePlayerCombatEntities(playerTeam);
        var combatEnemyEntities = _combatSetupService.CreatePlayerCombatEntities(enemyTeam);
        await _combatSetupService.PrepareEntitiesForCombat([.. combatPlayerEntities, .. combatEnemyEntities]);

        var encounterPlan = CreateArenaEncounterPlan(characterId, enemyId, now);
        var runtime = new CombatEncounterRuntime(
            encounterPlan,
            [
                new CombatRuntimeParticipant(
                    encounterPlan.FriendlyParticipants.Single(),
                    playerTeam.Single(),
                    combatPlayerEntities.Single())
            ],
            [
                new CombatRuntimeParticipant(
                    encounterPlan.HostileParticipants.Single(),
                    enemyTeam.Single(),
                    combatEnemyEntities.Single())
            ]);

        var combatResult = await _combatEngineExecutor.ExecuteAsync(runtime, cancellationToken);
        combatResult = _combatEncounterResultFactory.Create(runtime, combatResult).CombatResult;

        return new StartArenaBattleResult(combatResult, arenaTicketStatus);
    }

    private static CombatEncounterPlan CreateArenaEncounterPlan(
        Guid characterId,
        Guid enemyId,
        DateTimeOffset startsAt)
    {
        var matchId = Guid.NewGuid();
        return new CombatEncounterPlan(
            EncounterId: matchId,
            Mode: CombatMode.Pvp,
            Sequence: 1,
            StartsAt: startsAt,
            Participants:
            [
                new CombatParticipantSlot(characterId.ToString(), characterId, CombatSide.Friendly),
                new CombatParticipantSlot(enemyId.ToString(), enemyId, CombatSide.Hostile)
            ],
            SourceContext: new PvpEncounterSourceContext(matchId, characterId, enemyId));
    }

    /// <summary>
    /// Get the opponents you are eligible to fight, including rating gains / losses
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<ArenaOpponentPreview>> GetArenaOpponents(Guid characterId, CancellationToken cancellationToken)
    {
        var (opponents, myRating) = await _colosseumRepository.GetArenaOpponentsWithRating(characterId, cancellationToken);
        return opponents
            .Select(opp => new ArenaOpponentPreview
            {
                Opponent = opp,
                RatingDelta = _ratingService.Preview(myRating, opp.ArenaRating)
            })
            .ToList();
    }

    public async Task SaveArenaMatchResult(Guid characterId, Guid enemyId, BattleOutcome outcome, ColosseumRatingResult ratingResult, CancellationToken cancellationToken)
    {
        var characterA = await _characterService.GetBaseCharacterByIdAsync(characterId, cancellationToken);
        if (characterA == null) return;
        var characterB = await _characterService.GetBaseCharacterByIdAsync(enemyId, cancellationToken);
        if (characterB == null) return;

        var arenaMatchResult = new ColosseumMatchResult
        {
            CharacterAId = characterId,
            CharacterAName = characterA.Name,
            CharacterARatingBefore = ratingResult.CharacterARatingBefore,
            CharacterARatingAfter = ratingResult.CharacterARatingAfter,

            CharacterBId = enemyId,
            CharacterBName = characterB.Name,
            CharacterBRatingBefore = ratingResult.CharacterBRatingBefore,
            CharacterBRatingAfter = ratingResult.CharacterBRatingAfter,

            WinnerId = outcome == BattleOutcome.Victory ? characterId : outcome == BattleOutcome.Defeat ? enemyId : null,
            WinnerName = outcome == BattleOutcome.Victory ? characterA.Name : outcome == BattleOutcome.Defeat ? characterB.Name : "",
            PlayedAt = DateTimeOffset.UtcNow
        };

        await _colosseumRepository.SaveArenaMatchResult(arenaMatchResult, cancellationToken);
    }

    public async Task<List<ColosseumMatchResult>> GetColosseumMatchResults(Guid characterId, CancellationToken cancellationToken)
    {
        return await _colosseumRepository.GetColosseumMatchResults(characterId, cancellationToken);
    }

    public async Task<List<LeaderboardEntry>> GetRankings(Guid characterId, CancellationToken cancellationToken)
    {
        var characters = await _colosseumRepository.GetRankings(characterId, cancellationToken);

        // Assign the real rank to each character
        var rankedCharacters = characters
            .Select((character, index) => new { Character = character, Rank = index + 1 })
            .ToList();

        // Take the top 50
        var top50 = rankedCharacters.Take(50).ToList();

        // Check if requester is in the top 50
        var inTop50 = top50.Any(r => r.Character.Id == characterId);

        if (!inTop50)
        {
            // Find the requester's actual ranking
            var requesterRank = rankedCharacters.FirstOrDefault(r => r.Character.Id == characterId);
            if (requesterRank != null)
            {
                top50.Add(requesterRank); // Add requester to the bottom of the list, with their true rank
            }
        }

        // Create the result list
        var rankings = top50
            .Select(ranking => new LeaderboardEntry()
            {
                CharacterId = ranking.Character.Id,
                CharacterName = ranking.Character.Name,
                Level = ranking.Character.ArenaRating,
                Rank = ranking.Rank,
            })
            .ToList();

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

            _colosseumRepository.UpdateArenaTicketStatus(arenaTicketStatus);
        }

        return arenaTicketStatus;
    }
}
