using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Queries;

public sealed record GetTournamentHallOfFameQuery() : IQuery<IReadOnlyList<TournamentHallOfFameEntryDto>>;

public sealed class GetTournamentHallOfFameQueryHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<GetTournamentHallOfFameQuery, IReadOnlyList<TournamentHallOfFameEntryDto>>
{
    public async Task<IReadOnlyList<TournamentHallOfFameEntryDto>> Handle(
        GetTournamentHallOfFameQuery request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetHallOfFameAsync(cancellationToken);
        return mapper.Map<IReadOnlyList<TournamentHallOfFameEntryDto>>(result);
    }
}
