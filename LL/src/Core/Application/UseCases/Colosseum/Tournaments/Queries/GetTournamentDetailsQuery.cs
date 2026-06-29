using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Queries;

public sealed record GetTournamentDetailsQuery(Guid CharacterId, Guid TournamentId) : IQuery<TournamentDetailsDto?>;

public sealed class GetTournamentDetailsQueryHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<GetTournamentDetailsQuery, TournamentDetailsDto?>
{
    public async Task<TournamentDetailsDto?> Handle(
        GetTournamentDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetDetailsAsync(request.CharacterId, request.TournamentId, cancellationToken);
        return result is null ? null : mapper.Map<TournamentDetailsDto>(result);
    }
}
