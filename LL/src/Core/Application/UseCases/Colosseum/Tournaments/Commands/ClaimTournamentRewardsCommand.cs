using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Commands;

public sealed record ClaimTournamentRewardsCommand(Guid CharacterId, Guid? TournamentId)
    : ICommand<Response<ClaimTournamentRewardsResponseDto>>;

public sealed class ClaimTournamentRewardsCommandHandler(
    ITournamentGroundsService service,
    IMapper mapper)
    : IRequestHandler<ClaimTournamentRewardsCommand, Response<ClaimTournamentRewardsResponseDto>>
{
    public async Task<Response<ClaimTournamentRewardsResponseDto>> Handle(
        ClaimTournamentRewardsCommand request,
        CancellationToken cancellationToken)
    {
        var result = await service.ClaimRewardsAsync(request.CharacterId, request.TournamentId, cancellationToken);
        return Response<ClaimTournamentRewardsResponseDto>.Success(mapper.Map<ClaimTournamentRewardsResponseDto>(result));
    }
}
