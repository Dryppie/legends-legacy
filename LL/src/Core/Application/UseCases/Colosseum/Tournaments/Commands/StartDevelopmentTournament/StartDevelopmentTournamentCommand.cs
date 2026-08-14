using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Commands.StartDevelopmentTournament;

[NonTransactional]
public sealed record StartDevelopmentTournamentCommand(Guid CharacterId)
    : ICommand<Response<StartDevelopmentTournamentResponseDto>>;

public sealed class StartDevelopmentTournamentCommandHandler(
    ITournamentGroundsService service,
    IMapper mapper)
    : IRequestHandler<StartDevelopmentTournamentCommand, Response<StartDevelopmentTournamentResponseDto>>
{
    public async Task<Response<StartDevelopmentTournamentResponseDto>> Handle(
        StartDevelopmentTournamentCommand request,
        CancellationToken cancellationToken)
    {
        var result = await service.StartDevelopmentTournamentAsync(
            request.CharacterId,
            cancellationToken);

        return result.Started
            ? Response<StartDevelopmentTournamentResponseDto>.Success(
                mapper.Map<StartDevelopmentTournamentResponseDto>(result))
            : Response<StartDevelopmentTournamentResponseDto>.Fail(
                result.ErrorMessage ?? "Development tournament could not be started.");
    }
}
