using Application.UseCases.Administration;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Queries.SearchPlayers;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/players")]
public sealed class PlayersController : LiveOpsControllerBase
{
    [HttpGet]
    [Authorize(Policy = AdministrationPermissions.Read)]
    public async Task<ActionResult<Response<IReadOnlyList<PlayerAdministrationDto>>>> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 20)
    {
        var result = await Mediator.Send(new SearchPlayersQuery(query, limit));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
