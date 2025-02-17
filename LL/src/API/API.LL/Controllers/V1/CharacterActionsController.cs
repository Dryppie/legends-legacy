using Application.UseCases.CharacterActions.Commands.DeleteCharacterAction;
using Application.UseCases.CharacterActions.Commands.StartCombatAction;
using Application.UseCases.CharacterActions.Commands.StartGatheringAction;
using Application.UseCases.CharacterActions.Dtos;
using Application.UseCases.CharacterActions.Queries.GetCharacterAction;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.GatheringNodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
[Authorize]
public class CharacterActionsController : BaseController
{
    public record StartGatheringActionRequest(string GatheringNodeId, GatheringType GatheringType);
    public record StartCombatActionRequest(string AreaId);
    [HttpGet]
    public async Task<ActionResult<CharacterActionDto?>> Get()
    {
        return await Mediator.Send(new GetCharacterActionQuery(CurrentCharacterGuid));
    }
    // POST api/<CharacterActionsController>
    [HttpPost("StartCombat")]
    public async Task<ActionResult<bool>> StartCombat([FromBody] StartCombatActionRequest request)
    {

        return await Mediator.Send(new StartCombatActionCommand(CurrentCharacterGuid, request.AreaId));
    }
    // POST api/<CharacterActionsController>
    [HttpPost("StartGathering")]
    public async Task<ActionResult<bool>> StartGathering([FromBody] StartGatheringActionRequest request)
    {

        return await Mediator.Send(new StartGatheringActionCommand(CurrentCharacterGuid, request.GatheringNodeId, request.GatheringType));
    }

    [HttpDelete]
    public async Task<ActionResult> Delete()
    {
        await Mediator.Send(new DeleteCharacterActionCommand(CurrentCharacterGuid));

        return Ok();
    }
}
