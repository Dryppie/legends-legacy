using Application.Common.Responses;
using Application.UseCases.CharacterActions.Dtos;
using Application.UseCases.CharacterActions.Queries.GetCharacterAction;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Inventories.Queries.GetInventoryById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
[Authorize]

public class InventoryController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<Response<InventoryDto?>>> Get()
    {
        return Ok(await Mediator.Send(new GetInventoryByIdQuery(CurrentCharacterGuid)));
    }
}
