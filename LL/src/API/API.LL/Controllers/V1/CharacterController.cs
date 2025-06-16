using Application.UseCases.CharacterActions.Commands.StartGatheringAction;
using Application.UseCases.Characters.Commands.RenameCharacter;
using Application.UseCases.Characters.Dtos;
using Application.UseCases.Characters.Queries.GetCharacter;
using Application.UseCases.Characters.Queries.GetCharacterOverview;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize]
public class CharacterController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<Response<CharacterDto>>> Get() =>
        await Mediator.Send(new GetCharacterQuery(CurrentUserId));

    [HttpGet("Overview")]
    public async Task<ActionResult<Response<CharacterOverviewDto>>> Overview() =>
        await Mediator.Send(new GetCharacterOverviewQuery(CurrentCharacterGuid));

    [HttpPost("Rename")]
    public async Task<ActionResult<Response<bool>>> Rename([FromBody] string newName) =>
        await Mediator.Send(new RenameCharacterCommand(CurrentUserId, newName));
}
