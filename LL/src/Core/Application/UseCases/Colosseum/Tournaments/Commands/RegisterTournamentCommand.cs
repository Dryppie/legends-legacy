using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Commands;

[NonTransactional]
public sealed record RegisterTournamentCommand(Guid CharacterId, Guid TournamentId)
    : ICommand<Response<RegisterTournamentResponseDto>>;

public sealed class RegisterTournamentCommandHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<RegisterTournamentCommand, Response<RegisterTournamentResponseDto>>
{
    public async Task<Response<RegisterTournamentResponseDto>> Handle(
        RegisterTournamentCommand request,
        CancellationToken cancellationToken)
    {
        var result = await service.RegisterAsync(request.CharacterId, request.TournamentId, cancellationToken);
        return result is null
            ? Response<RegisterTournamentResponseDto>.Fail("Tournament registration failed.")
            : Response<RegisterTournamentResponseDto>.Success(mapper.Map<RegisterTournamentResponseDto>(result));
    }
}
