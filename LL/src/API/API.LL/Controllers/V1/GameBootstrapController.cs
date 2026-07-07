using Application.UseCases.GameBootstrap.Dtos;
using Application.UseCases.GameBootstrap.Queries.GetGameBootstrap;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize]
public sealed class GameBootstrapController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<Response<GameBootstrapDto>>> Get() =>
        await Mediator.Send(new GetGameBootstrapQuery(CurrentUserId, CurrentCharacterGuid));
}
