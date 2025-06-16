using Application.UseCases.Players.Queries.GetOnlinePlayerCount;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

public class PlayerController : BaseController
{
    [HttpGet("OnlineCount")]
    public async Task<ActionResult<int>> OnlineCount() =>
        await Mediator.Send(new GetOnlinePlayerCountQuery());
}
