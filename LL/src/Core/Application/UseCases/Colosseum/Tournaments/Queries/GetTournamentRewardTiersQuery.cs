using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Queries;

public sealed record GetTournamentRewardTiersQuery(Guid CharacterId) : IQuery<IReadOnlyList<TournamentRewardTierDto>>;

public sealed class GetTournamentRewardTiersQueryHandler(
    ITournamentGroundsService service,
    IMapper mapper)
    : IRequestHandler<GetTournamentRewardTiersQuery, IReadOnlyList<TournamentRewardTierDto>>
{
    public async Task<IReadOnlyList<TournamentRewardTierDto>> Handle(
        GetTournamentRewardTiersQuery request,
        CancellationToken cancellationToken)
    {
        var result = mapper.Map<IReadOnlyList<TournamentRewardTierDto>>(
            await service.GetRewardTiersAsync(request.CharacterId, cancellationToken));
        return result;
    }
}
