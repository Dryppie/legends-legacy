using Application.UseCases.Leaderboards.Dtos;
using Application.UseCases.Leaderboards.Queries.GetLeaderboard;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public class LeaderboardController : BaseController
{
    [HttpGet("{boardKey}")]
    public async Task<ActionResult<Response<LeaderboardBoardDto>>> Get(
        string boardKey,
        [FromQuery] int limit = 50,
        [FromQuery] string? cursor = null,
        [FromQuery] string? search = null) =>
        await Mediator.Send(new GetLeaderboardQuery(
            CurrentCharacterGuid,
            boardKey,
            limit,
            cursor,
            search));
}
