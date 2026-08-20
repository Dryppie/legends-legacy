using API.LL.Common;
using Application.UseCases.Raids;
using Application.UseCases.Raids.Commands.UpdateRaidParties;
using Application.UseCases.Raids.Dtos;
using Common.Primitives;
using Domain.Models.Raids;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize(Policy = AuthorizationPolicies.MultiplayerAllowed)]
[Route("~/api/v{version:apiVersion}/raids")]
public sealed class RaidController : BaseController
{
    public sealed record CreateRaidRequest(string RaidBossId, int PlusLevel);
    public sealed record CreateDevelopmentRaidRequest(int PlusLevel);
    public sealed record AssignRaidWingRequest(Guid CharacterId, RaidLane Lane, int SlotIndex);
    public sealed record RaidPartyAssignmentRequest(Guid CharacterId, RaidLane? Lane, int? WingSlotIndex);
    public sealed record UpdateRaidPartiesRequest(IReadOnlyList<RaidPartyAssignmentRequest> Assignments);
    public sealed record TransferRaidLeadershipRequest(Guid CharacterId);
    public sealed record RaidSignupDecisionRequest(Guid CharacterId);
    public sealed record FillDevelopmentRosterRequest(double PowerMultiplier = 1d);
    public sealed record PurchaseTrophyVendorItemRequest(string ItemId, int Quantity = 1);

    [HttpGet("bosses")]
    public async Task<ActionResult<IReadOnlyList<RaidBossSummaryDto>>> GetRaidBosses([FromQuery] int? region) =>
        Ok(await Mediator.Send(new GetRaidBossesQuery(CurrentCharacterGuid, region)));

    [HttpGet("bosses/{raidBossId}/open")]
    public async Task<ActionResult<IReadOnlyList<RaidRunSummaryDto>>> GetOpenRaids(string raidBossId) =>
        Ok(await Mediator.Send(new GetOpenRaidsQuery(CurrentCharacterGuid, raidBossId)));

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<RaidHistoryEntryDto>>> GetHistory(
        [FromQuery] string? raidBossId,
        [FromQuery] int take = 20) =>
        Ok(await Mediator.Send(new GetRaidHistoryQuery(CurrentCharacterGuid, raidBossId, take)));

    [HttpGet("active")]
    public async Task<ActionResult<RaidRunDto?>> GetActiveRaid() =>
        await Mediator.Send(new GetActiveRaidQuery(CurrentCharacterGuid));

    [HttpGet("{raidRunId:guid}")]
    public async Task<ActionResult<RaidRunDto?>> GetRaid(Guid raidRunId) =>
        await Mediator.Send(new GetRaidQuery(CurrentCharacterGuid, raidRunId));

    [HttpPost("create")]
    public async Task<ActionResult<Response<RaidRunDto>>> Create(CreateRaidRequest request) =>
        await Mediator.Send(new CreateRaidCommand(CurrentCharacterGuid, request.RaidBossId, request.PlusLevel));

    [HttpPost("bosses/{raidBossId}/development/create")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<Response<RaidRunDto>>> CreateDevelopment(
        string raidBossId,
        CreateDevelopmentRaidRequest request,
        [FromServices] IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        return await Mediator.Send(new CreateDevelopmentRaidCommand(
            CurrentCharacterGuid,
            raidBossId,
            request.PlusLevel));
    }

    [HttpPost("{raidRunId:guid}/join")]
    public async Task<ActionResult<Response<RaidRunDto>>> Join(Guid raidRunId) =>
        await Mediator.Send(new JoinRaidCommand(CurrentCharacterGuid, raidRunId));

    [HttpPost("{raidRunId:guid}/signups/approve")]
    public async Task<ActionResult<Response<RaidRunDto>>> ApproveSignup(
        Guid raidRunId,
        RaidSignupDecisionRequest request) =>
        await Mediator.Send(new ApproveRaidSignupCommand(
            CurrentCharacterGuid,
            raidRunId,
            request.CharacterId));

    [HttpPost("{raidRunId:guid}/signups/remove")]
    public async Task<ActionResult<Response<RaidRunDto>>> RemoveSignup(
        Guid raidRunId,
        RaidSignupDecisionRequest request) =>
        await Mediator.Send(new RemoveRaidSignupCommand(
            CurrentCharacterGuid,
            raidRunId,
            request.CharacterId));

    [HttpPost("{raidRunId:guid}/leave")]
    public async Task<ActionResult<Response<RaidRunDto>>> Leave(Guid raidRunId) =>
        await Mediator.Send(new LeaveRaidCommand(CurrentCharacterGuid, raidRunId));

    [HttpPost("{raidRunId:guid}/cancel")]
    public async Task<ActionResult<Response<RaidRunDto>>> Cancel(Guid raidRunId) =>
        await Mediator.Send(new CancelRaidCommand(CurrentCharacterGuid, raidRunId));

    [HttpPost("{raidRunId:guid}/transfer-leadership")]
    public async Task<ActionResult<Response<RaidRunDto>>> TransferLeadership(
        Guid raidRunId,
        TransferRaidLeadershipRequest request) =>
        await Mediator.Send(new TransferRaidLeadershipCommand(
            CurrentCharacterGuid,
            raidRunId,
            request.CharacterId));

    [HttpPost("{raidRunId:guid}/loadout")]
    public async Task<ActionResult<Response<RaidRunDto>>> RefreshLoadout(Guid raidRunId) =>
        await Mediator.Send(new RefreshRaidSnapshotCommand(CurrentCharacterGuid, raidRunId));

    [HttpPost("{raidRunId:guid}/assign")]
    public async Task<ActionResult<Response<RaidRunDto>>> Assign(Guid raidRunId, AssignRaidWingRequest request) =>
        await Mediator.Send(new AssignRaidWingCommand(
            CurrentCharacterGuid,
            raidRunId,
            request.CharacterId,
            request.Lane,
            request.SlotIndex));

    [HttpPut("{raidRunId:guid}/parties")]
    public async Task<ActionResult<Response<RaidRunDto>>> UpdateParties(
        Guid raidRunId,
        UpdateRaidPartiesRequest request) =>
        await Mediator.Send(new UpdateRaidPartiesCommand(
            CurrentCharacterGuid,
            raidRunId,
            (request.Assignments ?? [])
                .Select(assignment => new RaidPartyAssignment(
                    assignment.CharacterId,
                    assignment.Lane,
                    assignment.WingSlotIndex))
                .ToArray()));

    [HttpPost("{raidRunId:guid}/development/fill-roster")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<Response<RaidRunDto>>> FillDevelopmentRoster(
        Guid raidRunId,
        FillDevelopmentRosterRequest request,
        [FromServices] IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        return await Mediator.Send(
            new FillRaidWithDevelopmentCharactersCommand(
                CurrentCharacterGuid,
                raidRunId,
                request.PowerMultiplier));
    }

    [HttpPost("{raidRunId:guid}/battle-plan")]
    public async Task<ActionResult<Response<RaidBattlePlanPreviewDto>>> PreviewBattlePlan(Guid raidRunId) =>
        await Mediator.Send(new PreviewRaidBattlePlanQuery(CurrentCharacterGuid, raidRunId));

    [HttpPost("{raidRunId:guid}/commence")]
    public async Task<ActionResult<Response<RaidRunDto>>> Commence(Guid raidRunId)
    {
        var response = await Mediator.Send(new CommenceRaidCommand(CurrentCharacterGuid, raidRunId));
        return response?.IsSuccess == true ? Accepted(response) : BadRequest(response);
    }

    [HttpPost("{raidRunId:guid}/claim")]
    public async Task<ActionResult<Response<RaidRewardDto>>> Claim(Guid raidRunId) =>
        await Mediator.Send(new ClaimRaidRewardsCommand(CurrentCharacterGuid, raidRunId));

    [HttpGet("{raidRunId:guid}/lanes/{lane}/playback")]
    public async Task<ActionResult<RaidPlaybackDto?>> GetPlayback(Guid raidRunId, RaidLane lane) =>
        await Mediator.Send(new GetRaidPlaybackQuery(CurrentCharacterGuid, raidRunId, lane));

    [HttpGet("{raidRunId:guid}/lanes/{lane}/playback/bundle")]
    public async Task<IActionResult> GetPlaybackBundle(Guid raidRunId, RaidLane lane)
    {
        var bundle = await Mediator.Send(
            new GetRaidPlaybackBundleQuery(CurrentCharacterGuid, raidRunId, lane));
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

    [HttpGet("bosses/{raidBossId}/vendor")]
    public async Task<ActionResult<RaidTrophyVendorDto?>> GetTrophyVendor(string raidBossId) =>
        await Mediator.Send(new GetRaidTrophyVendorQuery(CurrentCharacterGuid, raidBossId));

    [HttpPost("bosses/{raidBossId}/vendor/purchase")]
    public async Task<ActionResult<Response<RaidTrophyPurchaseDto>>> PurchaseTrophyVendorItem(
        string raidBossId,
        PurchaseTrophyVendorItemRequest request) =>
        await Mediator.Send(new PurchaseRaidTrophyVendorItemCommand(
            CurrentCharacterGuid,
            raidBossId,
            request.ItemId,
            request.Quantity));
}
