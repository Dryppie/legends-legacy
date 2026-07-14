using Application.UseCases.Prophecies.Commands.AcceptProphecy;
using Application.UseCases.Prophecies.Commands.AssembleProphecySigil;
using Application.UseCases.Prophecies.Commands.ClaimProphecy;
using Application.UseCases.Prophecies.Commands.ClaimWeeklyRevelationMilestone;
using Application.UseCases.Prophecies.Commands.GetPropheciesOverview;
using Application.UseCases.Prophecies.Commands.OpenProphecyCache;
using Application.UseCases.Prophecies.Commands.RerollProphecy;
using Application.UseCases.Prophecies.Dtos;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize]
public sealed class PropheciesController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<Response<PropheciesOverviewDto>>> GetOverview() =>
        await Mediator.Send(new GetPropheciesOverviewCommand(CurrentUserId, CurrentCharacterGuid));

    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<Response<PropheciesOverviewDto>>> Accept(Guid id) =>
        await Mediator.Send(new AcceptProphecyCommand(CurrentUserId, CurrentCharacterGuid, id));

    [HttpPost("reroll")]
    public async Task<ActionResult<Response<PropheciesOverviewDto>>> Reroll() =>
        await Mediator.Send(new RerollProphecyCommand(CurrentUserId, CurrentCharacterGuid));

    public sealed record AssembleSigilRequest(string SigilItemId);

    [HttpPost("sigil-forge/assemble")]
    public async Task<ActionResult<Response<ProphecySigilForgeResponseDto>>> AssembleSigil(AssembleSigilRequest request) =>
        await Mediator.Send(new AssembleProphecySigilCommand(CurrentUserId, CurrentCharacterGuid, request.SigilItemId));

    [HttpPost("{id:guid}/claim")]
    public async Task<ActionResult<Response<ProphecyClaimResponseDto>>> Claim(Guid id) =>
        await Mediator.Send(new ClaimProphecyCommand(CurrentUserId, CurrentCharacterGuid, id));

    public sealed record ClaimWeeklyMilestoneRequest(int FavorRequired);

    [HttpPost("weekly-revelation/claim")]
    public async Task<ActionResult<Response<ClaimWeeklyRevelationMilestoneResponseDto>>> ClaimWeeklyMilestone(
        ClaimWeeklyMilestoneRequest request) =>
        await Mediator.Send(new ClaimWeeklyRevelationMilestoneCommand(
            CurrentUserId,
            CurrentCharacterGuid,
            request.FavorRequired));

    public sealed record OpenCacheRequest(string CacheItemId);

    [HttpPost("caches/open")]
    public async Task<ActionResult<Response<OpenProphecyCacheResponseDto>>> OpenCache(OpenCacheRequest request) =>
        await Mediator.Send(new OpenProphecyCacheCommand(CurrentCharacterGuid, request.CacheItemId));
}
