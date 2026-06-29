using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Queries;

public sealed record GetTournamentRewardsQuery(Guid CharacterId, Guid? TournamentId) : IQuery<IReadOnlyList<TournamentRewardGrantDto>>;

public sealed class GetTournamentRewardsQueryHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<GetTournamentRewardsQuery, IReadOnlyList<TournamentRewardGrantDto>>
{
    public async Task<IReadOnlyList<TournamentRewardGrantDto>> Handle(
        GetTournamentRewardsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetRewardsAsync(request.CharacterId, request.TournamentId, cancellationToken);
        return mapper.Map<IReadOnlyList<TournamentRewardGrantDto>>(result);
    }
}
