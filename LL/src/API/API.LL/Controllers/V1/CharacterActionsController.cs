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
    public class StartCharacterActionRequest
    {
        public CharacterActionType CharacterActionType { get; set; }
        public Guid LootTableId { get; set; }
    }
    // POST api/<CharacterActionsController>
    [HttpPost]
    public async Task<ActionResult<bool>> Start([FromBody] StartCharacterActionRequest request)
    {
        request.LootTableId = Guid.Parse("18735aef-f6a0-4953-9e50-7a4c2e8f9043");
        return await Mediator.Send(new StartCharacterActionCommand(CharacterGuid, request.CharacterActionType, request.LootTableId));
    }

    [HttpDelete]
    public async Task<ActionResult> Delete()
    {
        await Mediator.Send(new DeleteCharacterActionCommand(CharacterGuid));

        return Ok();
    }
}
