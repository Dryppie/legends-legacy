using API.LiveOps.Health;
using Application.UseCases.Administration;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/status")]
public sealed class OperationalStatusController(
    LiveOpsOperationalStatusService statusService) : LiveOpsControllerBase
{
    [HttpGet]
    [Authorize(Policy = AdministrationPermissions.Read)]
    public async Task<ActionResult<Response<OperationalStatusDto>>> Get(
        CancellationToken cancellationToken) =>
        Ok(Response<OperationalStatusDto>.Success(
            await statusService.GetAsync(cancellationToken)));
}
