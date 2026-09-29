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
    [HttpGet("details")]
    [Authorize(Policy = AdministrationPermissions.Read)]
    public async Task<IActionResult> Details([FromQuery] string view, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] string? status = null)
    {
        if (view is not ("deliveries" or "restrictions" or "jobs"))
            return BadRequest(Response<OperationalDetailPage>.Fail("Choose deliveries, restrictions or jobs."));
        try { return Ok(Response<OperationalDetailPage>.Success((await statusService.GetDetailsAsync(view, ct, page, status))!)); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { return StatusCode(503, Response<OperationalDetailPage>.Fail("Operational details are unavailable. Retry when the Game database is reachable.")); }
    }

    [HttpGet]
    [Authorize(Policy = AdministrationPermissions.Read)]
    public async Task<ActionResult<Response<OperationalStatusDto>>> Get(
        CancellationToken cancellationToken) =>
        Ok(Response<OperationalStatusDto>.Success(
            await statusService.GetAsync(cancellationToken)));
}
