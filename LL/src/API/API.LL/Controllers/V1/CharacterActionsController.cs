using Application.UseCases.CharacterActions.Commands.DeleteCharacterAction;
using Application.UseCases.CharacterActions.Commands.StartCombatAction;
using Application.UseCases.CharacterActions.Commands.ResolveCharacterAction;
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

    [HttpGet]
    public async Task<ActionResult<Response<CharacterActionDto?>>> Get() =>
        await Mediator.Send(new GetCharacterActionQuery(CurrentCharacterGuid));

    [HttpPost("Resolve")]
    public async Task<ActionResult<Response<CharacterActionDto?>>> Resolve() =>
        await Mediator.Send(new ResolveCharacterActionCommand(CurrentCharacterGuid));

    [HttpPost("StartCombat")]
    public async Task<ActionResult<Response<CharacterActionDto>>> StartCombat([FromBody] StartCombatActionRequest request) =>
        await Mediator.Send(new StartCombatActionCommand(CurrentCharacterGuid, request.AreaId));


    [HttpDelete]
    public async Task<ActionResult<Response<bool>>> Delete() =>
        await Mediator.Send(new DeleteCharacterActionCommand(CurrentCharacterGuid));
}
