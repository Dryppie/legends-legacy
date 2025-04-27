using Application.UseCases.CharacterActions.Dtos.CombatDtos;
using Application.UseCases.Characters.Dtos;
using Application.UseCases.Colosseum.Commands.StartArenaBattle;
using Application.UseCases.Colosseum.Dtos;
using Application.UseCases.Colosseum.Queries.GetArenaOpponents;
using Application.UseCases.Colosseum.Queries.GetArenaTickets;
using Application.UseCases.Colosseum.Queries.GetColosseumMatchResults;
using Application.UseCases.Colosseum.Queries.GetRankings;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public class ColosseumController : BaseController
{
    [HttpGet("GetArenaOpponents")]
    public async Task<List<CharacterDto>> GetArenaOpponents()
    {
        return await Mediator.Send(new GetArenaOpponentsQuery(CurrentCharacterGuid));
    }

    [HttpGet("GetArenaTicketStatus")]
    public async Task<ArenaTicketStatusDto> GetArenaTicketStatus()
    {
        return await Mediator.Send(new GetArenaTicketsQuery(CurrentCharacterGuid));
    }

    [HttpGet("GetRankings")]
    public async Task<List<ColosseumArenaRankDto>> GetRankings()
    {
        return await Mediator.Send(new GetRankingsQuery(CurrentCharacterGuid));
    }

    [HttpGet("GetColosseumMatchResults")]
    public async Task<List<ColosseumMatchResultDto>> GetColosseumMatchResults()
    {
        return await Mediator.Send(new GetColosseumMatchResultsQuery(CurrentCharacterGuid));
    }

    [HttpPost("StartArenaBattle")]
    public async Task<CombatResultDto> StartArenaBattle([FromBody] string enemyId)
    {
        return await Mediator.Send(new StartArenaBattleCommand(CurrentCharacterGuid, Guid.Parse(enemyId)));
    }
}
