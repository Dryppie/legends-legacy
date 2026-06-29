using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Queries;

public sealed record GetTournamentSeasonLeaderboardQuery() : IQuery<IReadOnlyList<TournamentSeasonLeaderboardEntryDto>>;

public sealed class GetTournamentSeasonLeaderboardQueryHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<GetTournamentSeasonLeaderboardQuery, IReadOnlyList<TournamentSeasonLeaderboardEntryDto>>
{
    public async Task<IReadOnlyList<TournamentSeasonLeaderboardEntryDto>> Handle(
        GetTournamentSeasonLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetSeasonLeaderboardAsync(cancellationToken);
        return mapper.Map<IReadOnlyList<TournamentSeasonLeaderboardEntryDto>>(result);
    }
}
