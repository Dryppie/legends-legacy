using Application.UseCases.Administration;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Persistence.LL.BackgroundJobs;

namespace API.LiveOps.Controllers;
[Route("api/liveops/status/job-schedules")]
[Authorize(Policy = AdministrationPermissions.Read)]
public sealed class JobSchedulesController(BackgroundJobScheduleReader reader) : LiveOpsControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        try { return Ok(Response<IReadOnlyList<JobScheduleHealth>>.Success(await reader.ReadAsync(ct))); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { return StatusCode(503, Response<bool>.Fail("Scheduler evidence is unavailable. The worker's Quartz tables must be reachable; no job health has been inferred.")); }
    }
}
