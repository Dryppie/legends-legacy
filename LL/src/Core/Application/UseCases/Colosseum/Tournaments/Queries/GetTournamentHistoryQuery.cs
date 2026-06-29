using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Queries;

public sealed record GetTournamentHistoryQuery(Guid CharacterId) : IQuery<IReadOnlyList<TournamentHistoryEntryDto>>;

public sealed class GetTournamentHistoryQueryHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<GetTournamentHistoryQuery, IReadOnlyList<TournamentHistoryEntryDto>>
{
    public async Task<IReadOnlyList<TournamentHistoryEntryDto>> Handle(
        GetTournamentHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetHistoryAsync(request.CharacterId, cancellationToken);
        return mapper.Map<IReadOnlyList<TournamentHistoryEntryDto>>(result);
    }
}
