using Application.UseCases.Administration;
using Application.UseCases.Nobility.Commands.GrantAlphaSignets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/characters/{characterId:guid}/signets")]
[Authorize(Policy = AdministrationPermissions.EconomyCompensation)]
public sealed class NobilityController : LiveOpsControllerBase
{
    public sealed record GrantRequest(Guid OperationId, int Quantity, string Reason);

    [HttpPost]
    public async Task<IActionResult> Grant(Guid characterId, [FromBody] GrantRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GrantAlphaSignetsCommand(CurrentActor.Subject, characterId,
            request.OperationId, request.Quantity, request.Reason), ct);
        return result.IsSuccess ? Ok(result) : result.IsConflict ? Conflict(result) : BadRequest(result);
    }
}
