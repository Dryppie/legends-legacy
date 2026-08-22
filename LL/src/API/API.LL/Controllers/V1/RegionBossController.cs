using API.LL.Common;
using Application.UseCases.RegionBosses;
using Application.UseCases.RegionBosses.Dtos;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize(Policy = AuthorizationPolicies.MultiplayerAllowed)]
[Route("~/api/v{version:apiVersion}/region-bosses")]
public sealed class RegionBossController : BaseController
{
    public sealed record SpawnDevelopmentRegionBossRequest(int RegionId, int AdditionalSignupCount = 24);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RegionBossStatusDto>>> GetStatus([FromQuery] int? regionId) =>
        Ok(await Mediator.Send(new GetRegionBossStatusQuery(CurrentCharacterGuid, regionId)));

    [HttpGet("events/{eventId:guid}")]
    public async Task<ActionResult<RegionBossStatusDto?>> GetEvent(Guid eventId) =>
        await Mediator.Send(new GetRegionBossEventQuery(CurrentCharacterGuid, eventId));

    [HttpPost("events/{eventId:guid}/signup")]
    public async Task<ActionResult<Response<RegionBossStatusDto>>> Signup(Guid eventId) =>
        await Mediator.Send(new SignupRegionBossCommand(CurrentCharacterGuid, eventId));

    [HttpDelete("events/{eventId:guid}/signup")]
    public async Task<ActionResult<Response<RegionBossStatusDto>>> Withdraw(Guid eventId) =>
        await Mediator.Send(new WithdrawRegionBossCommand(CurrentCharacterGuid, eventId));

    [HttpPost("rewards/{grantId:guid}/claim")]
    public async Task<ActionResult<Response<RegionBossClaimResultDto>>> Claim(Guid grantId) =>
        await Mediator.Send(new ClaimRegionBossRewardCommand(CurrentCharacterGuid, grantId));

    [HttpPost("development/spawn")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<Response<RegionBossStatusDto>>> SpawnDevelopment(
        SpawnDevelopmentRegionBossRequest request,
        [FromServices] IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        return await Mediator.Send(new SpawnDevelopmentRegionBossCommand(
            CurrentCharacterGuid,
            request.RegionId,
            request.AdditionalSignupCount));
    }

    [HttpGet("runs/{runId:guid}/playback")]
    public async Task<ActionResult<RegionBossPlaybackDto?>> GetPlayback(Guid runId) =>
        await Mediator.Send(new GetRegionBossPlaybackQuery(CurrentCharacterGuid, runId));

    [HttpGet("runs/{runId:guid}/playback/bundle")]
    public async Task<IActionResult> GetPlaybackBundle(Guid runId)
    {
        var bundle = await Mediator.Send(new GetRegionBossPlaybackBundleQuery(CurrentCharacterGuid, runId));
        if (bundle is null)
            return NotFound();
        var etag = $"\"{bundle.ETag}\"";
        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "private, max-age=31536000, immutable";
        Response.Headers.Vary = "Authorization, Accept-Encoding";
        if (Request.Headers.IfNoneMatch.Any(value => (value ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(candidate => candidate == "*" || string.Equals(candidate, etag, StringComparison.Ordinal))))
            return StatusCode(StatusCodes.Status304NotModified);
        Response.Headers.ContentEncoding = bundle.ContentEncoding;
        return File(bundle.Bytes, bundle.ContentType);
    }
}
