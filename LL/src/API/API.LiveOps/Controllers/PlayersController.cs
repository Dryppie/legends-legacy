using Application.UseCases.Administration;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Queries.SearchPlayers;
using Application.UseCases.Administration.Queries.GetPlayerAdministrationDetails;
using Common.Primitives;
using API.LiveOps.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Route("api/liveops/players")]
public sealed class PlayersController(
    LiveOpsPlayerSupportSnapshotService supportSnapshot) : LiveOpsControllerBase
{
    [HttpGet]
    [Authorize(Policy = AdministrationPermissions.Read)]
    public async Task<ActionResult<Response<IReadOnlyList<PlayerAdministrationDto>>>> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 20)
    {
        var result = await Mediator.Send(new SearchPlayersQuery(query, limit));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{characterId:guid}")]
    [Authorize(Policy = AdministrationPermissions.Read)]
    public async Task<ActionResult<Response<PlayerAdministrationDetailsDto>>> GetDetails(
        Guid characterId,
        [FromQuery] int historyLimit = 50)
    {
        var result = await Mediator.Send(
            new GetPlayerAdministrationDetailsQuery(characterId, historyLimit));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpGet("{characterId:guid}/support-snapshot")]
    [Authorize(Policy = AdministrationPermissions.Read)]
    public async Task<ActionResult<Response<PlayerSupportSnapshotDto>>> GetSupportSnapshot(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var snapshot = await supportSnapshot.GetAsync(characterId, cancellationToken);
        if (snapshot is null)
        {
            return NotFound(Response<PlayerSupportSnapshotDto>.Fail(
                "The target player was not found."));
        }
        return Ok(Response<PlayerSupportSnapshotDto>.Success(snapshot));
    }

    [HttpGet("{characterId:guid}/transfers")]
    [Authorize(Policy = AdministrationPermissions.Read)]
    public async Task<ActionResult<Response<PlayerSupportSection<TransferHistorySupportSnapshotDto>>>> GetTransfers(
        Guid characterId,
        [FromQuery] string? cursor,
        [FromQuery] int take = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await supportSnapshot.GetTransferHistoryAsync(
            characterId,
            cursor,
            take,
            cancellationToken);
        if (!result.CursorValid)
        {
            return BadRequest(Response<PlayerSupportSection<TransferHistorySupportSnapshotDto>>.Fail(
                "The transfer-history cursor is invalid or expired."));
        }
        if (!result.PlayerFound || result.Section is null)
        {
            return NotFound(Response<PlayerSupportSection<TransferHistorySupportSnapshotDto>>.Fail(
                "The target player was not found."));
        }
        return Ok(Response<PlayerSupportSection<TransferHistorySupportSnapshotDto>>.Success(
            result.Section));
    }
}
