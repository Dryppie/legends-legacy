using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Commands;

[NonTransactional]
public sealed record UpdateTournamentLoadoutCommand(Guid CharacterId, Guid TournamentId)
    : ICommand<Response<TournamentTeamActionResponseDto>>;

public sealed class UpdateTournamentLoadoutCommandHandler(
    ITournamentGroundsService service,
    IMapper mapper)
    : IRequestHandler<UpdateTournamentLoadoutCommand, Response<TournamentTeamActionResponseDto>>
{
    public async Task<Response<TournamentTeamActionResponseDto>> Handle(
        UpdateTournamentLoadoutCommand request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateLoadoutAsync(
            request.CharacterId,
            request.TournamentId,
            cancellationToken);
        if (result is null)
        {
            return Response<TournamentTeamActionResponseDto>.Fail(
                "Tournament loadout update failed.");
        }

        return result.Succeeded
            ? Response<TournamentTeamActionResponseDto>.Success(
                mapper.Map<TournamentTeamActionResponseDto>(result))
            : Response<TournamentTeamActionResponseDto>.Fail(
                result.ErrorMessage ?? "Tournament loadout update failed.");
    }
}
