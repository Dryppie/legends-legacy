using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Regions;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogBehaviorDiagnostics;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogCoverage;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogDiagnostics;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetCreatureBuildProfileDiagnostics;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetRegionOneContentDiagnostics;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.RunAbilityBalanceSimulation;
using Microsoft.AspNetCore.Mvc;

namespace API.AdminDashboard.Controllers.V1;

public class DiagnosticsController : BaseController
{
    private readonly IAbilityBalanceAuditService _balanceAudits;

    public DiagnosticsController(IAbilityBalanceAuditService balanceAudits)
    {
        _balanceAudits = balanceAudits;
    }

    [HttpGet("ability-catalog")]
    public async Task<ActionResult<AbilityCatalogDiagnosticReport>> GetAbilityCatalogDiagnostics() =>
        await Mediator.Send(new GetAbilityCatalogDiagnosticsQuery());

    [HttpGet("ability-catalog-coverage")]
    public async Task<ActionResult<AbilityCatalogCoverageReport>> GetAbilityCatalogCoverage() =>
        await Mediator.Send(new GetAbilityCatalogCoverageQuery());

    [HttpGet("ability-catalog-behaviors")]
    public async Task<ActionResult<AbilityCatalogBehaviorDiagnosticReport>> GetAbilityCatalogBehaviorDiagnostics() =>
        await Mediator.Send(new GetAbilityCatalogBehaviorDiagnosticsQuery());

    [HttpGet("creature-build-profiles")]
    public async Task<ActionResult<CreatureBuildProfileDiagnosticReport>> GetCreatureBuildProfileDiagnostics() =>
        await Mediator.Send(new GetCreatureBuildProfileDiagnosticsQuery());

    [HttpGet("region-one-content")]
    public async Task<ActionResult<RegionOneContentDiagnosticReport>> GetRegionOneContentDiagnostics() =>
        await Mediator.Send(new GetRegionOneContentDiagnosticsQuery());

    [HttpPost("ability-balance-simulation")]
    public async Task<ActionResult<AbilityBalanceSimulationReport>> RunAbilityBalanceSimulation(
        [FromBody] AbilityBalanceSimulationRequest request) =>
        await Mediator.Send(new RunAbilityBalanceSimulationQuery(request));

    [HttpPost("ability-balance-audit")]
    public ActionResult<AbilityBalanceAuditReport> RunAbilityBalanceAudit(
        [FromBody] AbilityBalanceAuditRequest request,
        CancellationToken cancellationToken) =>
        Ok(_balanceAudits.Run(request, cancellationToken));
}
