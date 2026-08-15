using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Queries;

public sealed record GetTournamentRewardTiersQuery : IQuery<IReadOnlyList<TournamentRewardTierDto>>;

public sealed class GetTournamentRewardTiersQueryHandler(
    ITournamentGroundsService service,
    IMapper mapper)
    : IRequestHandler<GetTournamentRewardTiersQuery, IReadOnlyList<TournamentRewardTierDto>>
{
    public Task<IReadOnlyList<TournamentRewardTierDto>> Handle(
        GetTournamentRewardTiersQuery request,
        CancellationToken cancellationToken)
    {
        var result = mapper.Map<IReadOnlyList<TournamentRewardTierDto>>(
            service.GetRewardTiers());
        return Task.FromResult(result);
    }
}
