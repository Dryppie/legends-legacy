using Application.UseCases.Colosseum.Commands.StartArenaBattle;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Application.UseCases.Colosseum.Commands.PurchaseChampionMarketItem;
using Application.UseCases.Colosseum.Commands.UpdateArenaDefenseSnapshot;
using Application.UseCases.Colosseum.Dtos;
using Application.UseCases.Colosseum.Tournaments;
using Application.UseCases.Colosseum.Tournaments.Commands;
using Application.UseCases.Colosseum.Tournaments.Queries;
using Application.UseCases.Colosseum.Queries.GetArenaOpponents;
using Application.UseCases.Colosseum.Queries.GetArenaTickets;
using Application.UseCases.Colosseum.Queries.GetChampionMarket;
using Application.UseCases.Colosseum.Queries.GetColosseumStatus;
using Application.UseCases.Colosseum.Queries.GetColosseumMatchResults;
using Application.UseCases.Colosseum.Queries.GetRankings;
using Application.UseCases.Leaderboards.Dtos;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

public class ColosseumController : BaseController
{
    public sealed record CreateTournamentTeamRequest(string Name);
    public sealed record InviteTournamentTeamMemberRequest(Guid InvitedParticipantId);

    [HttpGet("status")]
    [HttpGet("GetStatus")]
    public async Task<ActionResult<ColosseumStatusDto>> GetStatus() =>
        await Mediator.Send(new GetColosseumStatusQuery(CurrentCharacterGuid));

    [HttpGet("opponents")]
    [HttpGet("GetArenaOpponents")]
    public async Task<ActionResult<List<ArenaOpponentPreviewDto>>> GetArenaOpponents() =>
        await Mediator.Send(new GetArenaOpponentsQuery(CurrentCharacterGuid));

    [HttpGet("GetArenaTicketStatus")]
    public async Task<ActionResult<ArenaTicketStatusDto>> GetArenaTicketStatus() =>
        await Mediator.Send(new GetArenaTicketsQuery(CurrentCharacterGuid));

    [HttpGet("GetRankings")]
    public async Task<ActionResult<List<LeaderboardEntryDto>>> GetRankings() =>
        await Mediator.Send(new GetRankingsQuery(CurrentCharacterGuid));

    [HttpGet("history")]
    [HttpGet("GetColosseumMatchResults")]
    public async Task<ActionResult<List<ColosseumMatchResultDto>>> GetColosseumMatchResults() =>
        await Mediator.Send(new GetColosseumMatchResultsQuery(CurrentCharacterGuid));

    [HttpPost("battle")]
    [HttpPost("StartArenaBattle")]
    public async Task<ActionResult<Response<StartArenaBattleResponseDto>>> StartArenaBattle([FromBody] StartArenaBattleRequestDto request) =>
        await Mediator.Send(new StartArenaBattleCommand(CurrentCharacterGuid, request.OpponentId));

    [HttpPost("defense-snapshot")]
    [HttpPost("UpdateDefenseSnapshot")]
    public async Task<ActionResult<Response<ArenaDefenseStatusDto>>> UpdateDefenseSnapshot() =>
        await Mediator.Send(new UpdateArenaDefenseSnapshotCommand(CurrentCharacterGuid));

    [HttpGet("market")]
    [HttpGet("GetChampionMarket")]
    public async Task<ActionResult<ChampionMarketDto>> GetChampionMarket() =>
        await Mediator.Send(new GetChampionMarketQuery(CurrentCharacterGuid));

    [HttpPost("market/purchase")]
    [HttpPost("PurchaseChampionMarketItem")]
    public async Task<ActionResult<Response<PurchaseChampionMarketItemResponseDto>>> PurchaseChampionMarketItem([FromBody] PurchaseChampionMarketItemRequestDto request) =>
        await Mediator.Send(new PurchaseChampionMarketItemCommand(CurrentCharacterGuid, request.ItemId, request.Quantity));

    [HttpGet("tournaments/status")]
    public async Task<ActionResult<TournamentGroundsStatusDto>> GetTournamentGroundsStatus() =>
        await Mediator.Send(new GetTournamentGroundsStatusQuery(CurrentCharacterGuid));

    [HttpGet("tournaments/history")]
    public async Task<ActionResult<IReadOnlyList<TournamentHistoryEntryDto>>> GetTournamentHistory() =>
        Ok(await Mediator.Send(new GetTournamentHistoryQuery(CurrentCharacterGuid)));

    [HttpGet("tournaments/hall-of-fame")]
    public async Task<ActionResult<IReadOnlyList<TournamentHallOfFameEntryDto>>> GetTournamentHallOfFame() =>
        Ok(await Mediator.Send(new GetTournamentHallOfFameQuery()));

    [HttpGet("tournaments/season-leaderboard")]
    public async Task<ActionResult<IReadOnlyList<TournamentSeasonLeaderboardEntryDto>>> GetTournamentSeasonLeaderboard() =>
        Ok(await Mediator.Send(new GetTournamentSeasonLeaderboardQuery()));

    [HttpGet("tournaments/{tournamentId:guid}")]
    public async Task<ActionResult<TournamentDetailsDto?>> GetTournament(Guid tournamentId) =>
        await Mediator.Send(new GetTournamentDetailsQuery(CurrentCharacterGuid, tournamentId));

    [HttpGet("tournaments/{tournamentId:guid}/bracket")]
    public async Task<ActionResult<TournamentBracketDto?>> GetTournamentBracket(Guid tournamentId) =>
        await Mediator.Send(new GetTournamentBracketQuery(CurrentCharacterGuid, tournamentId));

    [HttpGet("tournaments/{tournamentId:guid}/matches/{matchId:guid}/replay")]
    public async Task<ActionResult<CombatResultDto?>> GetTournamentMatchReplay(Guid tournamentId, Guid matchId) =>
        await Mediator.Send(new GetTournamentMatchReplayQuery(CurrentCharacterGuid, tournamentId, matchId));

    [HttpGet("tournaments/{tournamentId:guid}/matches/{matchId:guid}/playback")]
    public async Task<ActionResult<TournamentPlaybackManifestDto?>> GetTournamentMatchPlayback(
        Guid tournamentId,
        Guid matchId) =>
        await Mediator.Send(new GetTournamentMatchPlaybackQuery(
            CurrentCharacterGuid,
            tournamentId,
            matchId));

    [HttpGet("tournaments/{tournamentId:guid}/matches/{matchId:guid}/playback/bundle")]
    public async Task<IActionResult> GetTournamentMatchPlaybackBundle(Guid tournamentId, Guid matchId)
    {
        var bundle = await Mediator.Send(new GetTournamentMatchPlaybackBundleQuery(
            CurrentCharacterGuid,
            tournamentId,
            matchId));
        if (bundle is null) return NotFound();

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

    [HttpGet("tournaments/rewards")]
    public async Task<ActionResult<IReadOnlyList<TournamentRewardGrantDto>>> GetTournamentRewards() =>
        Ok(await Mediator.Send(new GetTournamentRewardsQuery(CurrentCharacterGuid, null)));

    [HttpGet("tournaments/{tournamentId:guid}/rewards")]
    public async Task<ActionResult<IReadOnlyList<TournamentRewardGrantDto>>> GetTournamentRewards(Guid tournamentId) =>
        Ok(await Mediator.Send(new GetTournamentRewardsQuery(CurrentCharacterGuid, tournamentId)));

    [HttpPost("tournaments/{tournamentId:guid}/register")]
    public async Task<ActionResult<Response<RegisterTournamentResponseDto>>> RegisterTournament(Guid tournamentId) =>
        await Mediator.Send(new RegisterTournamentCommand(CurrentCharacterGuid, tournamentId));

    [HttpPost("tournaments/{tournamentId:guid}/withdraw")]
    public async Task<ActionResult<Response<WithdrawTournamentResponseDto>>> WithdrawTournament(Guid tournamentId) =>
        await Mediator.Send(new WithdrawTournamentRegistrationCommand(CurrentCharacterGuid, tournamentId));

    [HttpPost("tournaments/{tournamentId:guid}/teams")]
    public async Task<ActionResult<Response<CreateTournamentTeamResponseDto>>> CreateTournamentTeam(
        Guid tournamentId,
        [FromBody] CreateTournamentTeamRequest request) =>
        await Mediator.Send(new CreateTournamentTeamCommand(CurrentCharacterGuid, tournamentId, request.Name));

    [HttpPost("tournaments/{tournamentId:guid}/teams/{teamId:guid}/invite")]
    public async Task<ActionResult<Response<TournamentTeamActionResponseDto>>> InviteTournamentTeamMember(
        Guid tournamentId,
        Guid teamId,
        [FromBody] InviteTournamentTeamMemberRequest request) =>
        await Mediator.Send(new InviteTournamentTeamMemberCommand(
            CurrentCharacterGuid,
            tournamentId,
            teamId,
            request.InvitedParticipantId));

    [HttpPost("tournaments/team-invites/{inviteId:guid}/accept")]
    public async Task<ActionResult<Response<TournamentTeamActionResponseDto>>> AcceptTournamentTeamInvite(Guid inviteId) =>
        await Mediator.Send(new AcceptTournamentTeamInviteCommand(CurrentCharacterGuid, inviteId));

    [HttpPost("tournaments/{tournamentId:guid}/teams/{teamId:guid}/apply")]
    public async Task<ActionResult<Response<TournamentTeamActionResponseDto>>> ApplyToTournamentTeam(Guid tournamentId, Guid teamId) =>
        await Mediator.Send(new ApplyToTournamentTeamCommand(CurrentCharacterGuid, tournamentId, teamId));

    [HttpPost("tournaments/team-applications/{applicationId:guid}/accept")]
    public async Task<ActionResult<Response<TournamentTeamActionResponseDto>>> AcceptTournamentTeamApplication(Guid applicationId) =>
        await Mediator.Send(new AcceptTournamentTeamApplicationCommand(CurrentCharacterGuid, applicationId));

    [HttpPost("tournaments/{tournamentId:guid}/teams/{teamId:guid}/members/{participantId:guid}/kick")]
    public async Task<ActionResult<Response<TournamentTeamActionResponseDto>>> KickTournamentTeamMember(
        Guid tournamentId,
        Guid teamId,
        Guid participantId) =>
        await Mediator.Send(new KickTournamentTeamMemberCommand(CurrentCharacterGuid, tournamentId, teamId, participantId));

    [HttpPost("tournaments/rewards/claim")]
    public async Task<ActionResult<Response<ClaimTournamentRewardsResponseDto>>> ClaimTournamentRewards() =>
        await Mediator.Send(new ClaimTournamentRewardsCommand(CurrentCharacterGuid, null));

    [HttpPost("tournaments/{tournamentId:guid}/rewards/claim")]
    public async Task<ActionResult<Response<ClaimTournamentRewardsResponseDto>>> ClaimTournamentRewards(Guid tournamentId) =>
        await Mediator.Send(new ClaimTournamentRewardsCommand(CurrentCharacterGuid, tournamentId));
}
