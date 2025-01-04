using Application.UseCases.Essences.Commands.DeleteEquippedEssence;
using Application.UseCases.Essences.Commands.EquipEssence;
using Application.UseCases.Essences.Dtos;
using Application.UseCases.Essences.Queries.GetEquippedEssencesAndInventoryEssences;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

public class EssenceController : BaseController
{

    [HttpGet("GetEquippedEssencesAndInventoryEssences")]
    public async Task<ActionResult<EquippedEssencesAndInventoryEssencesDto>> GetEquippedEssencesAndInventoryEssences()
    {
        var essences = await Mediator.Send(new GetEquippedEssencesAndInventoryEssencesQuery(CurrentCharacterGuid));

        return essences;
    }

    [HttpPost("EquipEssence")]
    public async Task<ActionResult> EquipEssence([FromBody] string essenceItemId)
    {
        await Mediator.Send(new EquipEssenceCommand(CurrentCharacterGuid, essenceItemId));

        return Ok();
    }

    [HttpPost("DeleteEquippedEssence")]
    public async Task<ActionResult<bool>> DeleteEquippedEssence([FromBody] string essenceId)
    {
        return await Mediator.Send(new DeleteEquippedEssenceCommand(CurrentCharacterGuid, essenceId));
    }
}
