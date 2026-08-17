using Application.UseCases.Administration;
using Application.UseCases.Administration.Commands.MuteChat;
using Application.UseCases.Administration.Commands.UnmuteChat;
using Application.UseCases.Administration.Dtos;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/chat")]
public sealed class ChatModerationController : LiveOpsControllerBase
{
    public sealed record MuteRequest(
        Guid OperationId,
        string Reason,
        DateTimeOffset? ExpiresAt);

    public sealed record UnmuteRequest(
        Guid OperationId,
        string Reason);

    [HttpPost("characters/{characterId:guid}/mutes")]
    [Authorize(Policy = AdministrationPermissions.ChatModeration)]
    public async Task<ActionResult<Response<ChatModerationResultDto>>> Mute(
        Guid characterId,
        [FromBody] MuteRequest request)
    {
        var result = await Mediator.Send(new MuteChatCommand(
            request.OperationId,
            characterId,
            CurrentActor,
            request.Reason,
            request.ExpiresAt));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("mutes/{restrictionId:guid}/revoke")]
    [Authorize(Policy = AdministrationPermissions.ChatModeration)]
    public async Task<ActionResult<Response<ChatModerationResultDto>>> Unmute(
        Guid restrictionId,
        [FromBody] UnmuteRequest request)
    {
        var result = await Mediator.Send(new UnmuteChatCommand(
            request.OperationId,
            restrictionId,
            CurrentActor,
            request.Reason));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
