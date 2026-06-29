using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Queries;

public sealed record GetTournamentBracketQuery(Guid CharacterId, Guid TournamentId) : IQuery<TournamentBracketDto?>;

public sealed class GetTournamentBracketQueryHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<GetTournamentBracketQuery, TournamentBracketDto?>
{
    public async Task<TournamentBracketDto?> Handle(
        GetTournamentBracketQuery request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetBracketAsync(request.CharacterId, request.TournamentId, cancellationToken);
        return result is null ? null : mapper.Map<TournamentBracketDto>(result);
    }
}
