using Application.Interfaces.Services.LL.CombatProfiles;
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
using Services.AdminDashboard.Combat;
using Services.LL.Combat.Engine;

namespace API.AdminDashboard.Controllers.V1;

public class DiagnosticsController : BaseController
{
    private readonly IAbilityBalanceAuditService _balanceAudits;
    private readonly ICombatCharacterProfileService _characterProfiles;
    private readonly ICombatCharacterProfileCatalogService _characterProfileCatalog;
    private readonly ICombatCharacterProfileBatchService _characterProfileBatches;
    private readonly WorldTowerProductionCalibrationRunner _worldTowerProductionCalibration;
    private readonly WorldTowerProfileShadowCalibrationRunner _worldTowerProfileCalibration;
    private readonly IWorldTowerCalibrationCertificationRunner _worldTowerCertification;
    private readonly IWorldTowerAuditCampaignService _worldTowerAuditCampaigns;

    public DiagnosticsController(
        IAbilityBalanceAuditService balanceAudits,
        ICombatCharacterProfileService characterProfiles,
        ICombatCharacterProfileCatalogService characterProfileCatalog,
        ICombatCharacterProfileBatchService characterProfileBatches,
        WorldTowerProductionCalibrationRunner worldTowerProductionCalibration,
        WorldTowerProfileShadowCalibrationRunner worldTowerProfileCalibration,
        IWorldTowerCalibrationCertificationRunner worldTowerCertification,
        IWorldTowerAuditCampaignService worldTowerAuditCampaigns)
    {
        _balanceAudits = balanceAudits;
        _characterProfiles = characterProfiles;
        _characterProfileCatalog = characterProfileCatalog;
        _characterProfileBatches = characterProfileBatches;
        _worldTowerProductionCalibration = worldTowerProductionCalibration;
        _worldTowerProfileCalibration = worldTowerProfileCalibration;
        _worldTowerCertification = worldTowerCertification;
        _worldTowerAuditCampaigns = worldTowerAuditCampaigns;
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

    [HttpPost("combat-character-profiles")]
    public async Task<ActionResult<CombatCharacterProfileGenerationReport>> GenerateCombatCharacterProfiles(
        [FromBody] CombatCharacterProfileGenerationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _characterProfiles.GenerateAsync(request, cancellationToken));

    [HttpPost("combat-character-profiles/batch")]
    public async Task<ActionResult<CombatCharacterProfileBatchGenerationReport>> GenerateCombatCharacterProfileBatch(
        [FromBody] CombatCharacterProfileBatchGenerationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _characterProfileBatches.GenerateCatalogAsync(request, cancellationToken));

    [HttpGet("combat-character-profile-catalog")]
    public async Task<ActionResult<CombatCharacterProfileCatalogValidationReport>> GetCombatCharacterProfileCatalog(
        CancellationToken cancellationToken) =>
        Ok(await _characterProfileCatalog.GetApprovedAsync(cancellationToken));

    [HttpPost("combat-character-profile-catalog/validate")]
    public async Task<ActionResult<CombatCharacterProfileCatalogValidationReport>> ValidateCombatCharacterProfileCatalog(
        [FromBody] CombatCharacterProfileCatalogDocument catalog,
        CancellationToken cancellationToken) =>
        Ok(await _characterProfileCatalog.ValidateAsync(catalog, cancellationToken));

    [HttpPost("world-tower-profile-shadow-calibration")]
    public async Task<ActionResult<WorldTowerProfileShadowCalibrationReport>> RunWorldTowerProfileShadowCalibration(
        [FromBody] WorldTowerProfileShadowCalibrationOptions options,
        CancellationToken cancellationToken) =>
        Ok(await _worldTowerProfileCalibration.RunAsync(options, cancellationToken));

    [HttpPost("world-tower-calibration-certification")]
    public async Task<ActionResult<WorldTowerCalibrationCertificationReport>> RunWorldTowerCalibrationCertification(
        [FromBody] WorldTowerCalibrationCertificationOptions options,
        CancellationToken cancellationToken) =>
        Ok(await _worldTowerCertification.RunAsync(options, cancellationToken));

    [HttpGet("world-tower-profile-requirements")]
    public ActionResult<IReadOnlyList<WorldTowerProfileScenarioRequirement>> GetWorldTowerProfileRequirements(
        [FromQuery] int minimumFloor = 1,
        [FromQuery] int maximumFloor = 15) =>
        Ok(_worldTowerProductionCalibration.GetProfileScenarioRequirements(minimumFloor, maximumFloor));

    [HttpGet("world-tower-audit-campaigns")]
    public async Task<ActionResult<IReadOnlyList<WorldTowerAuditCampaign>>> GetWorldTowerAuditCampaigns(
        CancellationToken cancellationToken) =>
        Ok(await _worldTowerAuditCampaigns.ListAsync(cancellationToken));

    [HttpGet("world-tower-audit-campaigns/{id:guid}")]
    public async Task<ActionResult<WorldTowerAuditCampaign>> GetWorldTowerAuditCampaign(
        Guid id,
        CancellationToken cancellationToken)
    {
        var campaign = await _worldTowerAuditCampaigns.GetAsync(id, cancellationToken);
        return campaign is null ? NotFound() : Ok(campaign);
    }

    [HttpPost("world-tower-audit-campaigns")]
    public async Task<ActionResult<WorldTowerAuditCampaign>> CreateWorldTowerAuditCampaign(
        [FromBody] WorldTowerAuditCampaignOptions options,
        CancellationToken cancellationToken)
    {
        var campaign = await _worldTowerAuditCampaigns.CreateAsync(options, cancellationToken);
        return AcceptedAtAction(
            nameof(GetWorldTowerAuditCampaign),
            new { id = campaign.Id },
            campaign);
    }

    [HttpPost("world-tower-audit-campaigns/{id:guid}/cancel")]
    public async Task<ActionResult<WorldTowerAuditCampaign>> CancelWorldTowerAuditCampaign(
        Guid id,
        CancellationToken cancellationToken)
    {
        var campaign = await _worldTowerAuditCampaigns.CancelAsync(id, cancellationToken);
        return campaign is null ? NotFound() : Ok(campaign);
    }

    [HttpPost("world-tower-audit-campaigns/{id:guid}/retry")]
    public async Task<ActionResult<WorldTowerAuditCampaign>> RetryWorldTowerAuditCampaign(
        Guid id,
        CancellationToken cancellationToken)
    {
        var campaign = await _worldTowerAuditCampaigns.RetryAsync(id, cancellationToken);
        return campaign is null ? NotFound() : Accepted(campaign);
    }

    [HttpGet("world-tower-audit-campaigns/{id:guid}/catalog")]
    public async Task<ActionResult<CombatCharacterProfileCatalogDocument>> GetWorldTowerAuditCampaignCatalog(
        Guid id,
        CancellationToken cancellationToken)
    {
        var catalog = await _worldTowerAuditCampaigns.GetCatalogAsync(id, cancellationToken);
        return catalog is null ? NotFound() : Ok(catalog);
    }

    [HttpPost("world-tower-audit-campaigns/{id:guid}/candidate-shadow")]
    public async Task<ActionResult<WorldTowerProfileShadowCalibrationReport>> RunWorldTowerCampaignCandidateShadow(
        Guid id,
        [FromBody] WorldTowerProfileShadowCalibrationOptions options,
        CancellationToken cancellationToken)
    {
        var catalog = await _worldTowerAuditCampaigns.GetCatalogAsync(id, cancellationToken);
        return catalog is null
            ? NotFound()
            : Ok(await _worldTowerProfileCalibration.RunCandidateAsync(
                catalog,
                $"campaign:{id:D}",
                options,
                cancellationToken));
    }

    [HttpPost("world-tower-audit-campaigns/{id:guid}/candidate-certification")]
    public async Task<ActionResult<WorldTowerCalibrationCertificationReport>> RunWorldTowerCampaignCandidateCertification(
        Guid id,
        [FromBody] WorldTowerCalibrationCertificationOptions options,
        CancellationToken cancellationToken)
    {
        var catalog = await _worldTowerAuditCampaigns.GetCatalogAsync(id, cancellationToken);
        return catalog is null
            ? NotFound()
            : Ok(await _worldTowerCertification.RunCandidateAsync(
                catalog,
                $"campaign:{id:D}",
                options,
                cancellationToken));
    }

    [HttpGet("world-tower-audit-campaigns/{id:guid}/evidence")]
    public async Task<ActionResult<WorldTowerAuditCampaignEvidence>> GetWorldTowerAuditCampaignEvidence(
        Guid id,
        CancellationToken cancellationToken)
    {
        var evidence = await _worldTowerAuditCampaigns.GetEvidenceAsync(id, cancellationToken);
        return evidence is null ? NotFound() : Ok(evidence);
    }
}
