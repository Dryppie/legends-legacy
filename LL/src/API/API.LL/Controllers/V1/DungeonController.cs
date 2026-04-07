using Application.UseCases.Dungeons.Commands.StartDungeonRun;
using Common.Primitives;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Runs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize]
public class DungeonController : BaseController
{
    public record StartDungeonRequest(string DungeonId, DungeonTier DungeonTier);
    [HttpPost("StartDungeon")]
    public async Task<ActionResult<Response<DungeonRun>>> StartDungeon([FromBody] StartDungeonRequest startDungeonRequest) =>
        await Mediator.Send(new StartDungeonRunCommand(CurrentCharacterGuid, startDungeonRequest.DungeonId, startDungeonRequest.DungeonTier));
}
