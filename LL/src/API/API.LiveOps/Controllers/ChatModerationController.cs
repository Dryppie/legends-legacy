using Application.UseCases.Administration;
using Application.UseCases.Administration.Commands.MuteChat;
using Application.UseCases.Administration.Commands.UnmuteChat;
using Application.UseCases.Administration.Dtos;
using API.LiveOps.Previews;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/chat")]
public sealed class ChatModerationController(
    LiveOpsActionPreviewService previews) : LiveOpsControllerBase
{
    public sealed record MutePreviewRequest(
        Guid OperationId,
        string Reason,
        DateTimeOffset? ExpiresAt);

    public sealed record MuteRequest(
        Guid PreviewToken,
        Guid OperationId,
        string Reason,
        DateTimeOffset? ExpiresAt);

    public sealed record UnmutePreviewRequest(
        Guid OperationId,
        Guid CharacterId,
        string Reason);

    public sealed record UnmuteRequest(
        Guid PreviewToken,
        Guid OperationId,
        Guid CharacterId,
        string Reason);

    [HttpPost("characters/{characterId:guid}/mutes/preview")]
    [Authorize(Policy = AdministrationPermissions.ChatModeration)]
    public async Task<ActionResult<Response<ActionPreviewDto>>> PreviewMute(
        Guid characterId,
        [FromBody] MutePreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await previews.CreateChatMuteAsync(
            request.OperationId,
            characterId,
            CurrentActor,
            request.Reason,
            request.ExpiresAt,
            cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("characters/{characterId:guid}/mutes")]
    [Authorize(Policy = AdministrationPermissions.ChatModeration)]
    public async Task<ActionResult<Response<ChatModerationResultDto>>> Mute(
        Guid characterId,
        [FromBody] MuteRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await previews.BeginChatMuteAsync(
            request.PreviewToken,
            request.OperationId,
            characterId,
            CurrentActor,
            request.Reason,
            request.ExpiresAt,
            cancellationToken);
        if (!validation.IsSuccess)
        {
            var failure = Response<ChatModerationResultDto>.Fail(validation.ErrorMessage);
            return validation.IsConflict ? Conflict(failure) : BadRequest(failure);
        }
        var result = await Mediator.Send(new MuteChatCommand(
            request.OperationId,
            characterId,
            CurrentActor,
            request.Reason,
            request.ExpiresAt), cancellationToken);
        await previews.CompleteAsync(request.PreviewToken, result.IsSuccess, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("mutes/{restrictionId:guid}/revoke/preview")]
    [Authorize(Policy = AdministrationPermissions.ChatModeration)]
    public async Task<ActionResult<Response<ActionPreviewDto>>> PreviewUnmute(
        Guid restrictionId,
        [FromBody] UnmutePreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await previews.CreateChatUnmuteAsync(
            request.OperationId,
            restrictionId,
            request.CharacterId,
            CurrentActor,
            request.Reason,
            cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("mutes/{restrictionId:guid}/revoke")]
    [Authorize(Policy = AdministrationPermissions.ChatModeration)]
    public async Task<ActionResult<Response<ChatModerationResultDto>>> Unmute(
        Guid restrictionId,
        [FromBody] UnmuteRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await previews.BeginChatUnmuteAsync(
            request.PreviewToken,
            request.OperationId,
            restrictionId,
            request.CharacterId,
            CurrentActor,
            request.Reason,
            cancellationToken);
        if (!validation.IsSuccess)
        {
            var failure = Response<ChatModerationResultDto>.Fail(validation.ErrorMessage);
            return validation.IsConflict ? Conflict(failure) : BadRequest(failure);
        }
        var result = await Mediator.Send(new UnmuteChatCommand(
            request.OperationId,
            restrictionId,
            CurrentActor,
            request.Reason), cancellationToken);
        await previews.CompleteAsync(request.PreviewToken, result.IsSuccess, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
