using Application.UseCases.CharacterActions.Commands.DeleteCharacterAction;
using Application.UseCases.CharacterActions.Commands.StartCombatAction;
using Application.UseCases.CharacterActions.Commands.StartGatheringAction;
using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.UseCases.CharacterActions.Queries.GetCharacterAction;
using Common.Primitives;
using Domain.Models.Professions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
[Authorize]
public class CharacterActionsController : BaseController
{
    public record StartGatheringActionRequest(string GatheringNodeId, ProfessionType ProfessionType);
    public record StartCombatActionRequest(string AreaId);

    [HttpGet]
    public async Task<ActionResult<Response<CharacterActionDto?>>> Get() =>
        await Mediator.Send(new GetCharacterActionQuery(CurrentCharacterGuid));

    // POST api/<CharacterActionsController>
    [HttpPost("StartCombat")]
    public async Task<ActionResult<Response<bool>>> StartCombat([FromBody] StartCombatActionRequest request) =>
        await Mediator.Send(new StartCombatActionCommand(CurrentCharacterGuid, request.AreaId));

    // POST api/<CharacterActionsController>
    [HttpPost("StartGathering")]
    public async Task<ActionResult<Response<bool>>> StartGathering([FromBody] StartGatheringActionRequest request) =>
        await Mediator.Send(new StartGatheringActionCommand(CurrentCharacterGuid, request.GatheringNodeId, request.ProfessionType));

    [HttpDelete]
    public async Task<ActionResult<Response<bool>>> Delete() => 
        await Mediator.Send(new DeleteCharacterActionCommand(CurrentCharacterGuid));
}
