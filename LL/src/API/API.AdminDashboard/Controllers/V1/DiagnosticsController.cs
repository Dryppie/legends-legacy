using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Regions;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogBehaviorDiagnostics;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogCoverage;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogDiagnostics;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetCreatureBuildProfileDiagnostics;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.GetRegionOneContentDiagnostics;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.RunAbilityBalanceSimulation;
using Application.UseCases._AdminDashboard.Diagnostics.Queries.RunDungeonSimulation;
using Application.Interfaces.Services.LL.Dungeons;
using Microsoft.AspNetCore.Mvc;
using Domain.Components.Attributes;

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

    [HttpGet("dungeon-simulation-options")]
    public async Task<ActionResult<DungeonSimulationOptions>> GetDungeonSimulationOptions()
    {
        return await Mediator.Send(new GetDungeonSimulationOptionsQuery());
    }

    [HttpPost("dungeon-simulation")]
    public async Task<ActionResult<DungeonSimulationReport>> RunDungeonSimulation(
        [FromBody] DungeonSimulationRequest request)
    {
        return await Mediator.Send(new RunDungeonSimulationQuery(request));
    }

    [HttpPost("dungeon-simulation-combat-rating")]
    public async Task<ActionResult<CombatRatingBreakdown>> GetDungeonSimulationCombatRating(
        [FromBody] DungeonSimulationCharacter character)
    {
        return await Mediator.Send(new GetDungeonSimulationCombatRatingQuery(character));
    }

}
