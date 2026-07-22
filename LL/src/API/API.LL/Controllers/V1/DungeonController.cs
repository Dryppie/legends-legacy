using Application.UseCases.Dungeons.Commands.AssembleDungeonSigil;
using Application.UseCases.Dungeons.Commands.ClaimDungeonRewards;
using Application.UseCases.Dungeons.Commands.DismissFailedDungeonRun;
using Application.UseCases.Dungeons.Commands.ExecuteDungeonAction;
using Application.UseCases.Dungeons.Commands.StartDungeonRun;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Dungeons.Queries.GetAvailableDungeons;
using Application.UseCases.Dungeons.Queries.GetDungeonRecords;
using Application.UseCases.Dungeons.Queries.GetDungeonRun;
using Application.UseCases.Dungeons.Queries.GetDungeonReadiness;
using Application.Interfaces.Services.LL.PowerRatings;
using Common.Primitives;
using Domain.Models.Dungeons.Definitions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace API.LL.Controllers.V1;

[Authorize]
public class DungeonController : BaseController
{
    public sealed record DungeonPowerRecommendationsResponse(
        bool CalibrationComplete,
        IReadOnlyDictionary<string, DungeonPowerRecommendationSummary> Recommendations);

    public sealed record DungeonPowerRecommendationSummary(
        int RecommendedPartyPower,
        bool LowConfidence);

    [HttpGet("GetActiveDungeon")]
    public async Task<ActionResult<DungeonRunDto?>> GetActiveDungeon() =>
        await Mediator.Send(new GetDungeonRunQuery(CurrentCharacterGuid));

    [HttpGet("GetAvailableDungeons")]
    public async Task<ActionResult<DungeonHubDto>> GetAvailableDungeons() =>
        await Mediator.Send(new GetAvailableDungeonsQuery(CurrentCharacterGuid));

    [HttpGet("PowerRecommendations")]
    public ActionResult<DungeonPowerRecommendationsResponse> GetPowerRecommendations(
        [FromServices] IDungeonPowerRecommendationStore recommendationStore) =>
        Ok(new DungeonPowerRecommendationsResponse(
            recommendationStore.IsCalibrationComplete,
            recommendationStore.GetAll().ToDictionary(
                recommendation => recommendation.Key,
                recommendation => new DungeonPowerRecommendationSummary(
                    recommendation.Value.RecommendedPartyPower,
                    recommendation.Value.Confidence == PowerRatingConfidence.Low ||
                    recommendation.Value.State == PowerAnalysisState.LowConfidence),
                StringComparer.OrdinalIgnoreCase)));

    [HttpPost("{dungeonId}/assemble-sigil")]
    public async Task<ActionResult<Response<DungeonSigilAssemblyResponseDto>>> AssembleSigil(string dungeonId) =>
        await Mediator.Send(new AssembleDungeonSigilCommand(CurrentCharacterGuid, dungeonId));

    [HttpGet("GetDungeonRecords/{familyId}")]
    public async Task<ActionResult<DungeonRecordsDto>> GetDungeonRecords(string familyId) =>
        await Mediator.Send(new GetDungeonRecordsQuery(familyId));

    public record StartDungeonRequest(string DungeonId, DungeonTier DungeonTier);

    public sealed record DungeonReadinessRequest(
        string DungeonId,
        DungeonTier DungeonTier,
        IReadOnlyList<Guid>? CompanionIds);

    [HttpPost("Readiness")]
    public async Task<ActionResult<DungeonReadinessResult>> GetReadiness(
        [FromBody] DungeonReadinessRequest request) =>
        await Mediator.Send(new GetDungeonReadinessQuery(
            CurrentCharacterGuid,
            request.DungeonId,
            request.DungeonTier,
            request.CompanionIds ?? []));

    [HttpPost("StartDungeon")]
    public async Task<ActionResult<Response<StartDungeonRunResponseDto>>> StartDungeon([FromBody] StartDungeonRequest startDungeonRequest) =>
        await Mediator.Send(new StartDungeonRunCommand(CurrentCharacterGuid, startDungeonRequest.DungeonId, startDungeonRequest.DungeonTier));

    public class ExecuteDungeonActionRequest
    {
        public string ActionId { get; set; } = string.Empty;
        public JsonElement? Payload { get; set; }
    }

    [HttpPost("ExecuteAction/{runId}")]
    public async Task<ActionResult<Response<ExecuteDungeonActionResponseDto>>> ExecuteAction(Guid runId, ExecuteDungeonActionRequest request) =>
        await Mediator.Send(new ExecuteDungeonActionCommand(CurrentCharacterGuid, runId, request.ActionId, request.Payload));

    [HttpPost("ClaimDungeonRewards")]
    public async Task<ActionResult<Response<ClaimDungeonRewardsResponseDto>>> ClaimDungeonRewards() =>
        await Mediator.Send(new ClaimDungeonRewardsCommand(CurrentCharacterGuid));

    [HttpPost("DismissFailedDungeonRun")]
    public async Task<ActionResult<Response<DismissFailedDungeonRunResponseDto>>> DismissFailedDungeonRun() =>
        await Mediator.Send(new DismissFailedDungeonRunCommand(CurrentCharacterGuid));
}
