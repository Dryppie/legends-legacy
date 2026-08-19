using Application.UseCases.Administration;
using Application.UseCases.Administration.Commands.BanAccount;
using Application.UseCases.Administration.Commands.RevokeAccountBan;
using Application.UseCases.Administration.Commands.RestrictMultiplayer;
using Application.UseCases.Administration.Commands.RevokeMultiplayerRestriction;
using Application.UseCases.Administration.Dtos;
using API.LiveOps.Previews;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/accounts")]
public sealed class AccountModerationController(
    LiveOpsActionPreviewService previews) : LiveOpsControllerBase
{
    public sealed record BanAccountPreviewRequest(
        Guid OperationId,
        string Reason,
        string? InternalNotes,
        DateTimeOffset? ExpiresAt);

    public sealed record BanAccountRequest(
        Guid PreviewToken,
        Guid OperationId,
        string Reason,
        string? InternalNotes,
        DateTimeOffset? ExpiresAt);

    public sealed record RevokeBanPreviewRequest(
        Guid OperationId,
        string Reason);

    public sealed record RevokeBanRequest(
        Guid PreviewToken,
        Guid OperationId,
        string Reason);

    public sealed record MultiplayerRestrictionPreviewRequest(
        Guid OperationId,
        string Reason,
        string? InternalNotes,
        DateTimeOffset? ExpiresAt);

    public sealed record MultiplayerRestrictionRequest(
        Guid PreviewToken,
        Guid OperationId,
        string Reason,
        string? InternalNotes,
        DateTimeOffset? ExpiresAt);

    [HttpPost("{accountId:guid}/bans/preview")]
    [Authorize(Policy = AdministrationPermissions.AccountModeration)]
    public async Task<ActionResult<Response<ActionPreviewDto>>> PreviewBan(
        Guid accountId,
        [FromBody] BanAccountPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await previews.CreateAccountBanAsync(
            request.OperationId,
            accountId,
            CurrentActor,
            request.Reason,
            request.InternalNotes,
            request.ExpiresAt,
            cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{accountId:guid}/bans")]
    [Authorize(Policy = AdministrationPermissions.AccountModeration)]
    public async Task<ActionResult<Response<AccountBanResultDto>>> Ban(
        Guid accountId,
        [FromBody] BanAccountRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await previews.BeginAccountBanAsync(
            request.PreviewToken,
            request.OperationId,
            accountId,
            CurrentActor,
            request.Reason,
            request.InternalNotes,
            request.ExpiresAt,
            cancellationToken);
        if (!validation.IsSuccess)
        {
            var failure = Response<AccountBanResultDto>.Fail(validation.ErrorMessage);
            return validation.IsConflict ? Conflict(failure) : BadRequest(failure);
        }
        var result = await Mediator.Send(new BanAccountCommand(
            request.OperationId,
            accountId,
            CurrentActor,
            request.Reason,
            request.InternalNotes,
            request.ExpiresAt), cancellationToken);
        await previews.CompleteAsync(request.PreviewToken, result.IsSuccess, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("bans/{restrictionId:guid}/revoke/preview")]
    [Authorize(Policy = AdministrationPermissions.AccountModeration)]
    public async Task<ActionResult<Response<ActionPreviewDto>>> PreviewRevokeBan(
        Guid restrictionId,
        [FromBody] RevokeBanPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await previews.CreateAccountBanRevokeAsync(
            request.OperationId,
            restrictionId,
            CurrentActor,
            request.Reason,
            cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("bans/{restrictionId:guid}/revoke")]
    [Authorize(Policy = AdministrationPermissions.AccountModeration)]
    public async Task<ActionResult<Response<AccountBanResultDto>>> RevokeBan(
        Guid restrictionId,
        [FromBody] RevokeBanRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await previews.BeginAccountBanRevokeAsync(
            request.PreviewToken,
            request.OperationId,
            restrictionId,
            CurrentActor,
            request.Reason,
            cancellationToken);
        if (!validation.IsSuccess)
        {
            var failure = Response<AccountBanResultDto>.Fail(validation.ErrorMessage);
            return validation.IsConflict ? Conflict(failure) : BadRequest(failure);
        }
        var result = await Mediator.Send(new RevokeAccountBanCommand(
            request.OperationId,
            restrictionId,
            CurrentActor,
            request.Reason), cancellationToken);
        await previews.CompleteAsync(request.PreviewToken, result.IsSuccess, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{accountId:guid}/multiplayer-restrictions/preview")]
    [Authorize(Policy = AdministrationPermissions.AccountModeration)]
    public async Task<ActionResult<Response<ActionPreviewDto>>> PreviewMultiplayerRestriction(
        Guid accountId,
        [FromBody] MultiplayerRestrictionPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await previews.CreateMultiplayerRestrictionAsync(
            request.OperationId,
            accountId,
            CurrentActor,
            request.Reason,
            request.InternalNotes,
            request.ExpiresAt,
            cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{accountId:guid}/multiplayer-restrictions")]
    [Authorize(Policy = AdministrationPermissions.AccountModeration)]
    public async Task<ActionResult<Response<MultiplayerRestrictionResultDto>>> RestrictMultiplayer(
        Guid accountId,
        [FromBody] MultiplayerRestrictionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await previews.BeginMultiplayerRestrictionAsync(
            request.PreviewToken,
            request.OperationId,
            accountId,
            CurrentActor,
            request.Reason,
            request.InternalNotes,
            request.ExpiresAt,
            cancellationToken);
        if (!validation.IsSuccess)
        {
            var failure = Response<MultiplayerRestrictionResultDto>.Fail(validation.ErrorMessage);
            return validation.IsConflict ? Conflict(failure) : BadRequest(failure);
        }
        var result = await Mediator.Send(new RestrictMultiplayerCommand(
            request.OperationId,
            accountId,
            CurrentActor,
            request.Reason,
            request.InternalNotes,
            request.ExpiresAt), cancellationToken);
        await previews.CompleteAsync(request.PreviewToken, result.IsSuccess, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("multiplayer-restrictions/{restrictionId:guid}/revoke/preview")]
    [Authorize(Policy = AdministrationPermissions.AccountModeration)]
    public async Task<ActionResult<Response<ActionPreviewDto>>> PreviewRevokeMultiplayerRestriction(
        Guid restrictionId,
        [FromBody] RevokeBanPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await previews.CreateMultiplayerRestrictionRevokeAsync(
            request.OperationId,
            restrictionId,
            CurrentActor,
            request.Reason,
            cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("multiplayer-restrictions/{restrictionId:guid}/revoke")]
    [Authorize(Policy = AdministrationPermissions.AccountModeration)]
    public async Task<ActionResult<Response<MultiplayerRestrictionResultDto>>> RevokeMultiplayerRestriction(
        Guid restrictionId,
        [FromBody] RevokeBanRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await previews.BeginMultiplayerRestrictionRevokeAsync(
            request.PreviewToken,
            request.OperationId,
            restrictionId,
            CurrentActor,
            request.Reason,
            cancellationToken);
        if (!validation.IsSuccess)
        {
            var failure = Response<MultiplayerRestrictionResultDto>.Fail(validation.ErrorMessage);
            return validation.IsConflict ? Conflict(failure) : BadRequest(failure);
        }
        var result = await Mediator.Send(new RevokeMultiplayerRestrictionCommand(
            request.OperationId,
            restrictionId,
            CurrentActor,
            request.Reason), cancellationToken);
        await previews.CompleteAsync(request.PreviewToken, result.IsSuccess, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
