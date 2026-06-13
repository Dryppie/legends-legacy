using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogDiagnostics;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetCreatureBuildProfileDiagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.AdminDashboard.Controllers.V1;

public class DiagnosticsController : BaseController
{
    [HttpGet("ability-catalog")]
    public async Task<ActionResult<AbilityCatalogSmokeTestReport>> GetAbilityCatalogDiagnostics()
    {
        return await Mediator.Send(new GetAbilityCatalogDiagnosticsQuery());
    }

    [HttpGet("creature-build-profiles")]
    public async Task<ActionResult<CreatureBuildProfileDiagnosticReport>> GetCreatureBuildProfileDiagnostics()
    {
        return await Mediator.Send(new GetCreatureBuildProfileDiagnosticsQuery());
    }
}
