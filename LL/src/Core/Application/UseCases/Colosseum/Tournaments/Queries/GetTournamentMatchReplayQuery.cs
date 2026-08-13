using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Queries;

public sealed record GetTournamentMatchReplayQuery(Guid CharacterId, Guid TournamentId, Guid MatchId)
    : IQuery<CombatResultDto?>;

public sealed class GetTournamentMatchReplayQueryHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<GetTournamentMatchReplayQuery, CombatResultDto?>
{
    public async Task<CombatResultDto?> Handle(
        GetTournamentMatchReplayQuery request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetMatchReplayAsync(request.CharacterId, request.TournamentId, request.MatchId, cancellationToken);
        return result is null ? null : mapper.Map<CombatResultDto>(result);
    }
}

public sealed record GetTournamentMatchPlaybackQuery(Guid CharacterId, Guid TournamentId, Guid MatchId)
    : IQuery<TournamentPlaybackManifestDto?>;

public sealed class GetTournamentMatchPlaybackQueryHandler(ITournamentGroundsService service)
    : IRequestHandler<GetTournamentMatchPlaybackQuery, TournamentPlaybackManifestDto?>
{
    public Task<TournamentPlaybackManifestDto?> Handle(
        GetTournamentMatchPlaybackQuery request,
        CancellationToken cancellationToken) =>
        service.GetMatchPlaybackAsync(
            request.CharacterId,
            request.TournamentId,
            request.MatchId,
            cancellationToken);
}

public sealed record GetTournamentMatchPlaybackBundleQuery(Guid CharacterId, Guid TournamentId, Guid MatchId)
    : IQuery<TournamentPlaybackBundleContentDto?>;

public sealed class GetTournamentMatchPlaybackBundleQueryHandler(ITournamentGroundsService service)
    : IRequestHandler<GetTournamentMatchPlaybackBundleQuery, TournamentPlaybackBundleContentDto?>
{
    public Task<TournamentPlaybackBundleContentDto?> Handle(
        GetTournamentMatchPlaybackBundleQuery request,
        CancellationToken cancellationToken) =>
        service.GetMatchPlaybackBundleAsync(
            request.CharacterId,
            request.TournamentId,
            request.MatchId,
            cancellationToken);
}
