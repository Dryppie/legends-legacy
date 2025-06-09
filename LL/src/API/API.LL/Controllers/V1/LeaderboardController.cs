using Application.UseCases.Leaderboards.Dtos;
using Application.UseCases.Leaderboards.Queries.GetLeaderboard;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public class LeaderboardController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<Response<LeaderboardDto>>> Get() =>
        await Mediator.Send(new GetLeaderboardQuery(CurrentCharacterGuid));
}
