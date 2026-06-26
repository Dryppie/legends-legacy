using Application.UseCases.Colosseum.Commands.StartArenaBattle;
using Application.UseCases.Colosseum.Commands.PurchaseChampionMarketItem;
using Application.UseCases.Colosseum.Commands.UpdateArenaDefenseSnapshot;
using Application.UseCases.Colosseum.Dtos;
using Application.UseCases.Colosseum.Queries.GetArenaOpponents;
using Application.UseCases.Colosseum.Queries.GetArenaTickets;
using Application.UseCases.Colosseum.Queries.GetChampionMarket;
using Application.UseCases.Colosseum.Queries.GetColosseumStatus;
using Application.UseCases.Colosseum.Queries.GetColosseumMatchResults;
using Application.UseCases.Colosseum.Queries.GetRankings;
using Application.UseCases.Leaderboards.Dtos;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public class ColosseumController : BaseController
{
    [HttpGet("status")]
    [HttpGet("GetStatus")]
    public async Task<ActionResult<ColosseumStatusDto>> GetStatus() =>
        await Mediator.Send(new GetColosseumStatusQuery(CurrentCharacterGuid));

    [HttpGet("opponents")]
    [HttpGet("GetArenaOpponents")]
    public async Task<ActionResult<List<ArenaOpponentPreviewDto>>> GetArenaOpponents() =>
        await Mediator.Send(new GetArenaOpponentsQuery(CurrentCharacterGuid));

    [HttpGet("GetArenaTicketStatus")]
    public async Task<ActionResult<ArenaTicketStatusDto>> GetArenaTicketStatus() =>
        await Mediator.Send(new GetArenaTicketsQuery(CurrentCharacterGuid));

    [HttpGet("GetRankings")]
    public async Task<ActionResult<List<LeaderboardEntryDto>>> GetRankings() =>
        await Mediator.Send(new GetRankingsQuery(CurrentCharacterGuid));

    [HttpGet("history")]
    [HttpGet("GetColosseumMatchResults")]
    public async Task<ActionResult<List<ColosseumMatchResultDto>>> GetColosseumMatchResults() =>
        await Mediator.Send(new GetColosseumMatchResultsQuery(CurrentCharacterGuid));

    [HttpPost("battle")]
    [HttpPost("StartArenaBattle")]
    public async Task<ActionResult<Response<StartArenaBattleResponseDto>>> StartArenaBattle([FromBody] StartArenaBattleRequestDto request) =>
        await Mediator.Send(new StartArenaBattleCommand(CurrentCharacterGuid, request.OpponentId));

    [HttpPost("defense-snapshot")]
    [HttpPost("UpdateDefenseSnapshot")]
    public async Task<ActionResult<Response<ArenaDefenseStatusDto>>> UpdateDefenseSnapshot() =>
        await Mediator.Send(new UpdateArenaDefenseSnapshotCommand(CurrentCharacterGuid));

    [HttpGet("market")]
    [HttpGet("GetChampionMarket")]
    public async Task<ActionResult<ChampionMarketDto>> GetChampionMarket() =>
        await Mediator.Send(new GetChampionMarketQuery(CurrentCharacterGuid));

    [HttpPost("market/purchase")]
    [HttpPost("PurchaseChampionMarketItem")]
    public async Task<ActionResult<Response<PurchaseChampionMarketItemResponseDto>>> PurchaseChampionMarketItem([FromBody] PurchaseChampionMarketItemRequestDto request) =>
        await Mediator.Send(new PurchaseChampionMarketItemCommand(CurrentCharacterGuid, request.ItemId, request.Quantity));
}
