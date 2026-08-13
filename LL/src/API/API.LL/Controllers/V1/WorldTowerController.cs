using Application.UseCases.WorldTower;
using Application.UseCases.WorldTower.Dtos;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Common.Primitives;
using Domain.Models.WorldTower;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize]
[Route("~/api/v{version:apiVersion}/world-tower")]
public sealed class WorldTowerController : BaseController
{
    public sealed record CreateRallyRequest(int FloorNumber, TowerRallyMode Mode);
    public sealed record ContributionRequest(TowerContributionKind Kind, int Amount);

    [HttpGet]
    public async Task<ActionResult<TowerOverviewDto>> GetOverview() =>
        await Mediator.Send(new GetWorldTowerOverviewQuery(CurrentCharacterGuid));

    [HttpGet("floors/{floorNumber:int}")]
    public async Task<ActionResult<TowerFloorDetailDto?>> GetFloor(int floorNumber) =>
        await Mediator.Send(new GetTowerFloorQuery(CurrentCharacterGuid, floorNumber));

    [HttpGet("rallies/{rallyId:guid}")]
    public async Task<ActionResult<TowerRallyDto?>> GetRally(Guid rallyId) =>
        await Mediator.Send(new GetTowerRallyQuery(CurrentCharacterGuid, rallyId));

    [HttpGet("attempts/{attemptId:guid}/report")]
    public async Task<ActionResult<TowerBattleReportDto?>> GetAttemptReport(Guid attemptId) =>
        await Mediator.Send(new GetTowerAttemptReportQuery(CurrentCharacterGuid, attemptId));

    [HttpGet("attempts/{attemptId:guid}/combat-result")]
    public async Task<ActionResult<CombatResultDto?>> GetAttemptCombatResult(Guid attemptId) =>
        await Mediator.Send(new GetTowerAttemptCombatResultQuery(CurrentCharacterGuid, attemptId));

    [HttpGet("attempts/{attemptId:guid}/playback")]
    public async Task<ActionResult<TowerCombatPlaybackDto?>> GetAttemptPlayback(Guid attemptId) =>
        await Mediator.Send(new GetTowerAttemptPlaybackQuery(CurrentCharacterGuid, attemptId));

    [HttpGet("attempts/{attemptId:guid}/playback/frames")]
    public async Task<ActionResult<TowerCombatFrameBatchDto?>> GetAttemptPlaybackFrames(
        Guid attemptId,
        [FromQuery] int after = -1) =>
        await Mediator.Send(new GetTowerAttemptPlaybackFramesQuery(
            CurrentCharacterGuid,
            attemptId,
            after));

    [HttpGet("hall-of-fame")]
    public async Task<ActionResult<IReadOnlyList<TowerHallOfFameEntryDto>>> GetHallOfFame() =>
        Ok(await Mediator.Send(new GetTowerHallOfFameQuery()));

    [HttpGet("personal-expeditions")]
    public async Task<ActionResult<IReadOnlyList<TowerPersonalExpeditionDto>>> GetPersonalExpeditions() =>
        Ok(await Mediator.Send(new GetPersonalTowerExpeditionsQuery(CurrentCharacterGuid)));

    [HttpPost("rallies")]
    public async Task<ActionResult<Response<TowerRallyDto>>> CreateRally(CreateRallyRequest request) =>
        await Mediator.Send(new CreateTowerRallyCommand(CurrentCharacterGuid, request.FloorNumber, request.Mode));

    [HttpPost("rallies/{rallyId:guid}/applications")]
    public async Task<ActionResult<Response<TowerRallyDto>>> ApplyToRally(Guid rallyId) =>
        await Mediator.Send(new ApplyToTowerRallyCommand(CurrentCharacterGuid, rallyId));

    [HttpPost("rallies/{rallyId:guid}/applications/{applicationId:guid}/accept")]
    public async Task<ActionResult<Response<TowerRallyDto>>> AcceptApplication(Guid rallyId, Guid applicationId) =>
        await Mediator.Send(new AcceptTowerRallyApplicationCommand(CurrentCharacterGuid, rallyId, applicationId));

    [HttpPost("rallies/{rallyId:guid}/applications/{applicationId:guid}/decline")]
    public async Task<ActionResult<Response<TowerRallyDto>>> DeclineApplication(Guid rallyId, Guid applicationId) =>
        await Mediator.Send(new DeclineTowerRallyApplicationCommand(CurrentCharacterGuid, rallyId, applicationId));

    [HttpPost("rallies/{rallyId:guid}/leave")]
    public async Task<ActionResult<Response<TowerRallyDto>>> LeaveRally(Guid rallyId) =>
        await Mediator.Send(new LeaveTowerRallyCommand(CurrentCharacterGuid, rallyId));

    [HttpPost("rallies/{rallyId:guid}/development/fill-roster")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<Response<TowerRallyDto>>> FillDevelopmentRoster(
        Guid rallyId,
        [FromServices] IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        return await Mediator.Send(
            new FillTowerRallyWithDevelopmentCharactersCommand(
                CurrentCharacterGuid,
                rallyId));
    }

    [HttpPost("rallies/{rallyId:guid}/start")]
    public async Task<ActionResult<Response<TowerAttemptResultDto>>> StartRally(Guid rallyId)
    {
        var response = await Mediator.Send(
            new StartTowerRallyCommand(CurrentCharacterGuid, rallyId));
        return response?.IsSuccess == true
            ? Accepted(response)
            : BadRequest(response);
    }

    [HttpPost("floors/{floorNumber:int}/contributions")]
    public async Task<ActionResult<Response<TowerFloorDetailDto>>> Contribute(
        int floorNumber,
        ContributionRequest request) =>
        await Mediator.Send(new ContributeToTowerCommand(
            CurrentCharacterGuid,
            floorNumber,
            request.Kind,
            request.Amount));
}
