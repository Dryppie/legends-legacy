using Application.UseCases.Dungeons.Commands.ExecuteDungeonAction;
using Application.UseCases.Dungeons.Commands.ClaimDungeonRewards;
using Application.UseCases.Dungeons.Commands.StartDungeonRun;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Dungeons.Queries.GetDungeonRun;
using Common.Primitives;
using Domain.Models.Dungeons.Definitions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace API.LL.Controllers.V1;

[Authorize]
public class DungeonController : BaseController
{
    [HttpGet("GetActiveDungeon")]
    public async Task<ActionResult<DungeonRunDto?>> GetActiveDungeon() =>
        await Mediator.Send(new GetDungeonRunQuery(CurrentCharacterGuid));

    public record StartDungeonRequest(string DungeonId, DungeonTier DungeonTier);
    [HttpPost("StartDungeon")]
    public async Task<ActionResult<Response<DungeonRunDto>>> StartDungeon([FromBody] StartDungeonRequest startDungeonRequest) =>
        await Mediator.Send(new StartDungeonRunCommand(CurrentCharacterGuid, startDungeonRequest.DungeonId, startDungeonRequest.DungeonTier));

    public class ExecuteDungeonActionRequest
    {
        public string ActionId { get; set; } = string.Empty;
        public JsonElement? Payload { get; set; }
    }

    [HttpPost("ExecuteAction/{runId}")]
    public async Task<ActionResult<Response<ExecuteDungeonActionResponseDto>>> ExecuteAction(Guid runId, ExecuteDungeonActionRequest request) =>
        await Mediator.Send(new ExecuteDungeonActionCommand(runId, request.ActionId, request.Payload));

    [HttpPost("ClaimDungeonRewards")]
    public async Task<ActionResult<Response<bool>>> ClaimDungeonRewards() =>
        await Mediator.Send(new ClaimDungeonRewardsCommand(CurrentCharacterGuid));
}
