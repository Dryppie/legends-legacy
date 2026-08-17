using Application.UseCases.Administration;
using Application.UseCases.Administration.Commands.GrantCompensationItems;
using Application.UseCases.Administration.Dtos;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/characters")]
public sealed class CompensationController : LiveOpsControllerBase
{
    public sealed record GrantItemsRequest(
        Guid OperationId,
        string ItemBaseId,
        int Quantity,
        string Reason,
        string? InternalNotes);

    [HttpPost("{characterId:guid}/item-grants")]
    [Authorize(Policy = AdministrationPermissions.EconomyCompensation)]
    public async Task<ActionResult<Response<CompensationItemGrantResultDto>>> GrantItems(
        Guid characterId,
        [FromBody] GrantItemsRequest request)
    {
        var result = await Mediator.Send(new GrantCompensationItemsCommand(
            request.OperationId,
            characterId,
            CurrentActor,
            request.ItemBaseId,
            request.Quantity,
            request.Reason,
            request.InternalNotes));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
