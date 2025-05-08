using Application.UseCases.Essences.Commands.DeleteEquippedEssence;
using Application.UseCases.Essences.Commands.EquipEssence;
using Application.UseCases.Essences.Dtos;
using Application.UseCases.Essences.Queries.GetEquippedEssencesAndInventoryEssences;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public class EssenceController : BaseController
{

    [HttpGet("GetEquippedEssencesAndInventoryEssences")]
    public async Task<ActionResult<EquippedEssencesAndInventoryEssencesDto>> GetEquippedEssencesAndInventoryEssences() => 
        await Mediator.Send(new GetEquippedEssencesAndInventoryEssencesQuery(CurrentCharacterGuid));

    [HttpPost("EquipEssence")]
    public async Task<ActionResult<Response<bool>>> EquipEssence([FromBody] string essenceItemId) => 
        await Mediator.Send(new EquipEssenceCommand(CurrentCharacterGuid, essenceItemId));

    [HttpPost("DeleteEquippedEssence")]
    public async Task<ActionResult<Response<bool>>> DeleteEquippedEssence([FromBody] string essenceId) =>
        await Mediator.Send(new DeleteEquippedEssenceCommand(CurrentCharacterGuid, essenceId));
}
