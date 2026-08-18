using Application.UseCases.Administration;
using Application.UseCases.Administration.Commands.GrantCompensationItems;
using Application.UseCases.Administration.Dtos;
using API.LiveOps.Previews;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/characters")]
public sealed class CompensationController(
    LiveOpsActionPreviewService previews) : LiveOpsControllerBase
{
    public sealed record GrantItemsPreviewRequest(
        Guid OperationId,
        string ItemBaseId,
        int Quantity,
        string Reason,
        string? InternalNotes);

    public sealed record GrantItemsRequest(
        Guid PreviewToken,
        Guid OperationId,
        string ItemBaseId,
        int Quantity,
        string Reason,
        string? InternalNotes);

    [HttpPost("{characterId:guid}/item-grants/preview")]
    [Authorize(Policy = AdministrationPermissions.EconomyCompensation)]
    public async Task<ActionResult<Response<ActionPreviewDto>>> PreviewGrantItems(
        Guid characterId,
        [FromBody] GrantItemsPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await previews.CreateCompensationGrantAsync(
            request.OperationId,
            characterId,
            CurrentActor,
            request.ItemBaseId,
            request.Quantity,
            request.Reason,
            request.InternalNotes,
            cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{characterId:guid}/item-grants")]
    [Authorize(Policy = AdministrationPermissions.EconomyCompensation)]
    public async Task<ActionResult<Response<CompensationItemGrantResultDto>>> GrantItems(
        Guid characterId,
        [FromBody] GrantItemsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await previews.BeginCompensationGrantAsync(
            request.PreviewToken,
            request.OperationId,
            characterId,
            CurrentActor,
            request.ItemBaseId,
            request.Quantity,
            request.Reason,
            request.InternalNotes,
            cancellationToken);
        if (!validation.IsSuccess)
        {
            var failure = Response<CompensationItemGrantResultDto>.Fail(validation.ErrorMessage);
            return validation.IsConflict ? Conflict(failure) : BadRequest(failure);
        }
        var result = await Mediator.Send(new GrantCompensationItemsCommand(
            request.OperationId,
            characterId,
            CurrentActor,
            request.ItemBaseId,
            request.Quantity,
            request.Reason,
            request.InternalNotes), cancellationToken);
        await previews.CompleteAsync(request.PreviewToken, result.IsSuccess, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
