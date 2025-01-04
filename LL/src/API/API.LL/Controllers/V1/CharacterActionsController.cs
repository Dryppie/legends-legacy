using Application.UseCases.CharacterActions.Commands.DeleteCharacterAction;
using Application.UseCases.CharacterActions.Commands.StartCharacterAction;
using Application.UseCases.CharacterActions.Dtos;
using Application.UseCases.CharacterActions.Queries.GetCharacterAction;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
[Authorize]
public class CharacterActionsController : BaseController
{
    public record StartCombatActionRequest(CombatActionDetails CombatActionDetails);
    public record StartGatheringActionRequest(GatheringActionDetails GatheringActionDetails);
    [HttpGet]
    public async Task<ActionResult<CharacterActionDto?>> Get()
    {
        return await Mediator.Send(new GetCharacterActionQuery(CurrentCharacterGuid));
    }
    // POST api/<CharacterActionsController>
    [HttpPost("StartCombat")]
    public async Task<ActionResult<bool>> StartCombat([FromBody] StartCombatActionRequest request)
    {

        return await Mediator.Send(new StartCharacterActionCommand(CurrentCharacterGuid, CharacterActionType.Combat, request.CombatActionDetails));
    }
    // POST api/<CharacterActionsController>
    [HttpPost("StartGathering")]
    public async Task<ActionResult<bool>> StartGathering([FromBody] StartGatheringActionRequest request)
    {

        return await Mediator.Send(new StartCharacterActionCommand(CurrentCharacterGuid, CharacterActionType.Gathering, request.GatheringActionDetails));
    }

    [HttpDelete]
    public async Task<ActionResult> Delete()
    {
        await Mediator.Send(new DeleteCharacterActionCommand(CurrentCharacterGuid));

        return Ok();
    }
}
