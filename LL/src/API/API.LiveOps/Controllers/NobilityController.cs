using Application.UseCases.Administration;
using Application.UseCases.Nobility.Commands.GrantAlphaSignets;
using API.LiveOps.Previews;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/characters/{characterId:guid}/signets")]
[Authorize(Policy = AdministrationPermissions.EconomyCompensation)]
public sealed class NobilityController(LiveOpsActionPreviewService previews) : LiveOpsControllerBase
{
    public sealed record GrantRequest(Guid OperationId, int Quantity, string Reason, Guid PreviewToken = default);

    [HttpPost("preview")]
    public async Task<IActionResult> Preview(Guid characterId, [FromBody] GrantRequest request, CancellationToken ct)
    {
        var result = await previews.CreateAlphaSignetGrantAsync(request.OperationId, characterId, CurrentActor,
            request.Quantity, request.Reason, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost]
    public async Task<IActionResult> Grant(Guid characterId, [FromBody] GrantRequest request, CancellationToken ct)
    {
        var validation = await previews.BeginAlphaSignetGrantAsync(request.PreviewToken, request.OperationId,
            characterId, CurrentActor, request.Quantity, request.Reason, ct);
        if (!validation.IsSuccess)
            return validation.IsConflict ? Conflict(Response<bool>.Fail(validation.ErrorMessage))
                : BadRequest(Response<bool>.Fail(validation.ErrorMessage));
        var result = await Mediator.Send(new GrantAlphaSignetsCommand(CurrentActor.Subject, characterId,
            request.OperationId, request.Quantity, request.Reason), ct);
        await previews.CompleteAsync(request.PreviewToken, result.IsSuccess, ct);
        return result.IsSuccess ? Ok(result) : result.IsConflict ? Conflict(result) : BadRequest(result);
    }
}
