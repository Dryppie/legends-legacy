using Application.UseCases.Administration;
using Application.UseCases.Administration.Commands.BanAccount;
using Application.UseCases.Administration.Commands.RevokeAccountBan;
using Application.UseCases.Administration.Dtos;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/accounts")]
public sealed class AccountModerationController : LiveOpsControllerBase
{
    public sealed record BanAccountRequest(
        Guid OperationId,
        string Reason,
        string? InternalNotes,
        DateTimeOffset? ExpiresAt);

    public sealed record RevokeBanRequest(
        Guid OperationId,
        string Reason);

    [HttpPost("{accountId:guid}/bans")]
    [Authorize(Policy = AdministrationPermissions.AccountModeration)]
    public async Task<ActionResult<Response<AccountBanResultDto>>> Ban(
        Guid accountId,
        [FromBody] BanAccountRequest request)
    {
        var result = await Mediator.Send(new BanAccountCommand(
            request.OperationId,
            accountId,
            CurrentActor,
            request.Reason,
            request.InternalNotes,
            request.ExpiresAt));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("bans/{restrictionId:guid}/revoke")]
    [Authorize(Policy = AdministrationPermissions.AccountModeration)]
    public async Task<ActionResult<Response<AccountBanResultDto>>> RevokeBan(
        Guid restrictionId,
        [FromBody] RevokeBanRequest request)
    {
        var result = await Mediator.Send(new RevokeAccountBanCommand(
            request.OperationId,
            restrictionId,
            CurrentActor,
            request.Reason));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
