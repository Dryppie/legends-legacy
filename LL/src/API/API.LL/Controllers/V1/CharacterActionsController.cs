using Application.Common.Responses;
using Application.UseCases.CharacterActions.Commands.DeleteCharacterAction;
using Application.UseCases.CharacterActions.Commands.StartCombatAction;
using Application.UseCases.CharacterActions.Commands.StartGatheringAction;
using Application.UseCases.CharacterActions.Dtos;
using Application.UseCases.CharacterActions.Queries.GetCharacterAction;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.GatheringNodes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
[Authorize]
public class CharacterActionsController : BaseController
{
    public record StartGatheringActionRequest(string GatheringNodeId, GatheringType GatheringType);
    public record StartCombatActionRequest(string AreaId);
    [HttpGet]
    public async Task<ActionResult<Response<CharacterActionDto?>>> Get()
    {
        var characterAction = await Mediator.Send(new GetCharacterActionQuery(CurrentCharacterGuid));
        return Ok(characterAction);
    }

    // POST api/<CharacterActionsController>
    [HttpPost("StartCombat")]
    public async Task<ActionResult<Response<bool>>> StartCombat([FromBody] StartCombatActionRequest request)
    {
        var startCombat = await Mediator.Send(new StartCombatActionCommand(CurrentCharacterGuid, request.AreaId));
        return Ok(startCombat);
    }

    // POST api/<CharacterActionsController>
    [HttpPost("StartGathering")]
    public async Task<ActionResult<Response<bool>>> StartGathering([FromBody] StartGatheringActionRequest request)
    {
        var startGathering = await Mediator.Send(new StartGatheringActionCommand(CurrentCharacterGuid, request.GatheringNodeId, request.GatheringType));
        return Ok(startGathering);
    }

    [HttpDelete]
    public async Task<ActionResult<Response<Unit>>> Delete()
    {
        await Mediator.Send(new DeleteCharacterActionCommand(CurrentCharacterGuid));

        return Ok();
    }
}
