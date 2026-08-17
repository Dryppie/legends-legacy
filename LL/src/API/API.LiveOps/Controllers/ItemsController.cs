using Application.UseCases.Administration;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Queries.SearchAdministrationItems;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/items")]
public sealed class ItemsController : LiveOpsControllerBase
{
    [HttpGet]
    [Authorize(Policy = AdministrationPermissions.EconomyCompensation)]
    public async Task<ActionResult<Response<IReadOnlyList<AdministrationItemDto>>>> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 20)
    {
        var result = await Mediator.Send(
            new SearchAdministrationItemsQuery(query, limit));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
