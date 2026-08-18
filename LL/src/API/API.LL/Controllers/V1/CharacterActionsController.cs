using Application.UseCases.CharacterActions.Commands.DeleteCharacterAction;
using Application.UseCases.CharacterActions.Commands.StartCombatAction;
using Application.UseCases.CharacterActions.Commands.StartCraftingAction;
using Application.UseCases.CharacterActions.Commands.ResolveCharacterAction;
using Application.UseCases.CharacterActions.Commands.ResumeTempering;
using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.UseCases.CharacterActions.Queries.GetCharacterAction;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
[Authorize]
public class CharacterActionsController : BaseController
{
    public record StartCombatActionRequest(string AreaId);
    public record StartCraftingActionRequest(string QueueId, string ItemInstanceId);

    [HttpGet]
    public async Task<ActionResult<Response<CharacterActionDto?>>> Get() =>
        await Mediator.Send(new GetCharacterActionQuery(CurrentCharacterGuid));

    [HttpPost("Resolve")]
    public async Task<ActionResult<Response<CharacterActionDto?>>> Resolve() =>
        await Mediator.Send(new ResolveCharacterActionCommand(CurrentCharacterGuid));

    [HttpPost("StartCombat")]
    public async Task<ActionResult<Response<CharacterActionDto>>> StartCombat([FromBody] StartCombatActionRequest request) =>
        await Mediator.Send(new StartCombatActionCommand(CurrentCharacterGuid, request.AreaId));

    [HttpPost("StartCrafting")]
    public async Task<ActionResult<Response<bool>>> StartCrafting([FromBody] StartCraftingActionRequest request) =>
        await Mediator.Send(new StartCraftingActionCommand(CurrentCharacterGuid, request.QueueId, request.ItemInstanceId));

    [HttpPost("ResumeTempering")]
    public async Task<ActionResult<Response<CharacterActionDto>>> ResumeTempering() =>
        await Mediator.Send(new ResumeTemperingCommand(CurrentCharacterGuid));

    [HttpDelete]
    public async Task<ActionResult<Response<bool>>> Delete() =>
        await Mediator.Send(new DeleteCharacterActionCommand(CurrentCharacterGuid));
}
