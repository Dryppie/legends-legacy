using Application.UseCases.Inventories.Commands.ScrapEquipments;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Inventories.Queries.GetInventoryById;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
[Authorize]

public class InventoryController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<Response<InventoryDto>>> Get() =>
        await Mediator.Send(new GetInventoryByIdQuery(CurrentCharacterGuid));

    [HttpPost("Scrap")]
    public async Task<ActionResult<Response<ScrapEquipmentsResponseDto>>> Scrap([FromBody] List<string> itemIds) =>
        await Mediator.Send(new ScrapEquipmentsCommand(CurrentCharacterGuid, itemIds));
}
