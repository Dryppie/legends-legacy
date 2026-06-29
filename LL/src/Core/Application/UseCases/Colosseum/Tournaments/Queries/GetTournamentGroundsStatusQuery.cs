using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Queries;

public sealed record GetTournamentGroundsStatusQuery(Guid CharacterId) : IQuery<TournamentGroundsStatusDto>;

public sealed class GetTournamentGroundsStatusQueryHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<GetTournamentGroundsStatusQuery, TournamentGroundsStatusDto>
{
    public async Task<TournamentGroundsStatusDto> Handle(
        GetTournamentGroundsStatusQuery request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetStatusAsync(request.CharacterId, cancellationToken);
        return mapper.Map<TournamentGroundsStatusDto>(result);
    }
}
