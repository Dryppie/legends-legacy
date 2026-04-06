using Application.UseCases.Dungeons.Commands.StartDungeonRun;
using Common.Primitives;
using Domain.Models.Dungeons.Runs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize]
public class DungeonController : BaseController
{
    [HttpPost("StartDungeonRun")]
    public async Task<ActionResult<Response<DungeonRun>>> StartDungeonRun([FromBody] string dungeonId) =>
        await Mediator.Send(new StartDungeonRunCommand(CurrentCharacterGuid, dungeonId));
}
