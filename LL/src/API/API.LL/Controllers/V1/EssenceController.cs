using Application.Common.Responses;
using Application.UseCases.Essences.Commands.DeleteEquippedEssence;
using Application.UseCases.Essences.Commands.EquipEssence;
using Application.UseCases.Essences.Dtos;
using Application.UseCases.Essences.Queries.GetEquippedEssencesAndInventoryEssences;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

public class EssenceController : BaseController
{

    [HttpGet("GetEquippedEssencesAndInventoryEssences")]
    public async Task<ActionResult<Response<EquippedEssencesAndInventoryEssencesDto>>> GetEquippedEssencesAndInventoryEssences()
    {
        var essences = await Mediator.Send(new GetEquippedEssencesAndInventoryEssencesQuery(CurrentCharacterGuid));

        return Ok(essences);
    }

    [HttpPost("EquipEssence")]
    public async Task<ActionResult<Response<Unit>>> EquipEssence([FromBody] string essenceItemId)
    {
        var equipEssence = await Mediator.Send(new EquipEssenceCommand(CurrentCharacterGuid, essenceItemId));

        return Ok(equipEssence);
    }

    [HttpPost("DeleteEquippedEssence")]
    public async Task<ActionResult<Response<bool>>> DeleteEquippedEssence([FromBody] string essenceId)
    {
        var deleteResponse = await Mediator.Send(new DeleteEquippedEssenceCommand(CurrentCharacterGuid, essenceId));

        return Ok(deleteResponse);
    }
}
