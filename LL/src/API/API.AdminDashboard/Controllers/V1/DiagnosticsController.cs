using Application.Interfaces.Services.LL.CombatStyles;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Regions;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogBehaviorDiagnostics;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogCoverage;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogDiagnostics;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetCreatureBuildProfileDiagnostics;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetRegionOneContentDiagnostics;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.RunAbilityBalanceSimulation;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.RunCombatStyleBalanceSimulation;
using Microsoft.AspNetCore.Mvc;

namespace API.AdminDashboard.Controllers.V1;

public class DiagnosticsController : BaseController
{
    [HttpGet("ability-catalog")]
    public async Task<ActionResult<AbilityCatalogDiagnosticReport>> GetAbilityCatalogDiagnostics()
    {
        return await Mediator.Send(new GetAbilityCatalogDiagnosticsQuery());
    }

    [HttpGet("ability-catalog-coverage")]
    public async Task<ActionResult<AbilityCatalogCoverageReport>> GetAbilityCatalogCoverage()
    {
        return await Mediator.Send(new GetAbilityCatalogCoverageQuery());
    }

    [HttpGet("ability-catalog-behaviors")]
    public async Task<ActionResult<AbilityCatalogBehaviorDiagnosticReport>> GetAbilityCatalogBehaviorDiagnostics()
    {
        return await Mediator.Send(new GetAbilityCatalogBehaviorDiagnosticsQuery());
    }

    [HttpGet("creature-build-profiles")]
    public async Task<ActionResult<CreatureBuildProfileDiagnosticReport>> GetCreatureBuildProfileDiagnostics()
    {
        return await Mediator.Send(new GetCreatureBuildProfileDiagnosticsQuery());
    }

    [HttpGet("region-one-content")]
    public async Task<ActionResult<RegionOneContentDiagnosticReport>> GetRegionOneContentDiagnostics()
    {
        return await Mediator.Send(new GetRegionOneContentDiagnosticsQuery());
    }

    [HttpPost("ability-balance-simulation")]
    public async Task<ActionResult<AbilityBalanceSimulationReport>> RunAbilityBalanceSimulation(
        [FromBody] AbilityBalanceSimulationRequest request)
    {
        return await Mediator.Send(new RunAbilityBalanceSimulationQuery(request));
    }

    [HttpPost("combat-style-balance-simulation")]
    public async Task<ActionResult<CombatStyleBalanceSimulationReport>> RunCombatStyleBalanceSimulation(
        [FromBody] CombatStyleBalanceSimulationRequest request)
    {
        return await Mediator.Send(new RunCombatStyleBalanceSimulationQuery(request));
    }
}
