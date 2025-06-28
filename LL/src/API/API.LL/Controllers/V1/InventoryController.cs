using Application.UseCases.Inventories.Commands.ScrapEquipments;
using Application.UseCases.Inventories.Commands.ShatterEssence;
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

    public record ShatterEssence(string EssenceId, int Amount);
    [HttpPost("Shatter")]
    public async Task<ActionResult<Response<InventoryItemDto>>> Shatter([FromBody] ShatterEssence shatterEssence) =>
        await Mediator.Send(new ShatterEssenceCommand(CurrentCharacterGuid, shatterEssence.EssenceId, shatterEssence.Amount));

    [HttpPost("Scrap")]
    public async Task<ActionResult<Response<InventoryItemDto>>> Scrap([FromBody] List<string> itemIds) =>
        await Mediator.Send(new ScrapEquipmentsCommand(CurrentCharacterGuid, itemIds));
}
