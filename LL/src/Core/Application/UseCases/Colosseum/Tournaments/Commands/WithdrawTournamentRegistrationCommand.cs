using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Commands;

[NonTransactional]
public sealed record WithdrawTournamentRegistrationCommand(Guid CharacterId, Guid TournamentId)
    : ICommand<Response<WithdrawTournamentResponseDto>>;

public sealed class WithdrawTournamentRegistrationCommandHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<WithdrawTournamentRegistrationCommand, Response<WithdrawTournamentResponseDto>>
{
    public async Task<Response<WithdrawTournamentResponseDto>> Handle(
        WithdrawTournamentRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        var result = await service.WithdrawAsync(request.CharacterId, request.TournamentId, cancellationToken);
        return result is null
            ? Response<WithdrawTournamentResponseDto>.Fail("Tournament withdrawal failed.")
            : Response<WithdrawTournamentResponseDto>.Success(mapper.Map<WithdrawTournamentResponseDto>(result));
    }
}
