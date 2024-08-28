using Application.UseCases.CharacterActions.Commands.DeleteCharacterAction;
using Application.UseCases.CharacterActions.Commands.StartCharacterAction;
using Application.UseCases.CharacterActions.Dtos;
using Application.UseCases.CharacterActions.Queries.GetCharacterAction;
using Domain.Models.CharacterActions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
[Authorize]
public class CharacterActionsController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<CharacterActionDto?>> Get()
    {
        return await Mediator.Send(new GetCharacterActionQuery(CharacterGuid));
    }

    // POST api/<CharacterActionsController>
    [HttpPost("start")]
    public async Task<ActionResult<bool>> Start([FromBody] CharacterActionType CharacterActionType, Guid LootTableId)
    {
        LootTableId = Guid.Parse("6b5a2d58-6695-4690-84c4-dc0c6559702a");
        return await Mediator.Send(new StartCharacterActionCommand(CharacterGuid, CharacterActionType, LootTableId));
    }

    [HttpPost("delete")]
    public async Task<ActionResult> Delete()
    {
        await Mediator.Send(new DeleteCharacterActionCommand(CharacterGuid));

        return Ok();
    }
}
