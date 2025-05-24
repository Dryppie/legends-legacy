using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Professions.Commands.CraftItem;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public class CraftingController : BaseController
{
    [HttpPost("CraftItem")]
    public async Task<ActionResult<Response<InventoryItemDto>>> CraftItem([FromBody] string recipeId) =>
        await Mediator.Send(new CraftItemCommand(CurrentCharacterGuid, recipeId));
}
