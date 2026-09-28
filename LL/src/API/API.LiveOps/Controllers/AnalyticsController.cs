using Common.Primitives;
using Domain.Models.Analytics;
using Application.UseCases.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/analytics")]
public sealed class AnalyticsController(ITelemetryRepository telemetry) : LiveOpsControllerBase
{
    [HttpGet("itemization")]
    [Authorize(Policy = AdministrationPermissions.Read)]
    public async Task<IActionResult> GetItemization([FromQuery] int days = 30, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new Application.UseCases.Analytics.Queries.GetItemizationTelemetry.GetItemizationTelemetryQuery(days), ct));

    [HttpGet("overview")]
    [Authorize(Policy = AdministrationPermissions.Read)]
    public async Task<ActionResult<Response<IReadOnlyList<TelemetrySnapshot>>>> GetOverview(
        [FromQuery] int days = 30, CancellationToken ct = default) =>
        Ok(Response<IReadOnlyList<TelemetrySnapshot>>.Success(
            await telemetry.GetReportsAsync(Math.Clamp(days, 1, 90), ct)));
}
