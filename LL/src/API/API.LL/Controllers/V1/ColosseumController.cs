using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Application.UseCases.Colosseum.Commands.StartArenaBattle;
using Application.UseCases.Colosseum.Dtos;
using Application.UseCases.Colosseum.Queries.GetArenaOpponents;
using Application.UseCases.Colosseum.Queries.GetArenaTickets;
using Application.UseCases.Colosseum.Queries.GetColosseumMatchResults;
using Application.UseCases.Colosseum.Queries.GetRankings;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public class ColosseumController : BaseController
{
    [HttpGet("GetArenaOpponents")]
    public async Task<ActionResult<List<ArenaOpponentPreviewDto>>> GetArenaOpponents() =>
        await Mediator.Send(new GetArenaOpponentsQuery(CurrentCharacterGuid));

    [HttpGet("GetArenaTicketStatus")]
    public async Task<ActionResult<ArenaTicketStatusDto>> GetArenaTicketStatus() =>
        await Mediator.Send(new GetArenaTicketsQuery(CurrentCharacterGuid));

    [HttpGet("GetRankings")]
    public async Task<ActionResult<List<ColosseumArenaRankDto>>> GetRankings() =>
        await Mediator.Send(new GetRankingsQuery(CurrentCharacterGuid));

    [HttpGet("GetColosseumMatchResults")]
    public async Task<ActionResult<List<ColosseumMatchResultDto>>> GetColosseumMatchResults() =>
        await Mediator.Send(new GetColosseumMatchResultsQuery(CurrentCharacterGuid));

    [HttpPost("StartArenaBattle")]
    public async Task<ActionResult<Response<CombatResultDto>>> StartArenaBattle([FromBody] string enemyId) =>
        await Mediator.Send(new StartArenaBattleCommand(CurrentCharacterGuid, enemyId));
}
