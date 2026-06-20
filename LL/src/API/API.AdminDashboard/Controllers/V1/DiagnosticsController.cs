using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogV2BehaviorDiagnostics;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogV2Coverage;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogV2Diagnostics;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetCreatureBuildProfileDiagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.AdminDashboard.Controllers.V1;

public class DiagnosticsController : BaseController
{
    [HttpGet("ability-catalog-v2")]
    public async Task<ActionResult<AbilityCatalogV2DiagnosticReport>> GetAbilityCatalogV2Diagnostics()
    {
        return await Mediator.Send(new GetAbilityCatalogV2DiagnosticsQuery());
    }

    [HttpGet("ability-catalog-v2-coverage")]
    public async Task<ActionResult<AbilityCatalogV2CoverageReport>> GetAbilityCatalogV2Coverage()
    {
        return await Mediator.Send(new GetAbilityCatalogV2CoverageQuery());
    }

    [HttpGet("ability-catalog-v2-behaviors")]
    public async Task<ActionResult<AbilityCatalogV2BehaviorDiagnosticReport>> GetAbilityCatalogV2BehaviorDiagnostics()
    {
        return await Mediator.Send(new GetAbilityCatalogV2BehaviorDiagnosticsQuery());
    }

    [HttpGet("creature-build-profiles")]
    public async Task<ActionResult<CreatureBuildProfileDiagnosticReport>> GetCreatureBuildProfileDiagnostics()
    {
        return await Mediator.Send(new GetCreatureBuildProfileDiagnosticsQuery());
    }
}
