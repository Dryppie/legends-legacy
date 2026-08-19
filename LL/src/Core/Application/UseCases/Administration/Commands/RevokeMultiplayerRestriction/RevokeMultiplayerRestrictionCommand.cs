using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Commands.RevokeMultiplayerRestriction;

public sealed record RevokeMultiplayerRestrictionCommand(
    Guid OperationId,
    Guid RestrictionId,
    AdministrationActor Actor,
    string Reason) : ICommand<Response<MultiplayerRestrictionResultDto>>;

public sealed class RevokeMultiplayerRestrictionCommandHandler(
    ILiveOpsService liveOps,
    IMapper mapper)
    : IRequestHandler<RevokeMultiplayerRestrictionCommand, Response<MultiplayerRestrictionResultDto>>
{
    public async Task<Response<MultiplayerRestrictionResultDto>> Handle(
        RevokeMultiplayerRestrictionCommand request,
        CancellationToken cancellationToken)
    {
        var result = await liveOps.RevokeMultiplayerRestrictionAsync(
            request.OperationId,
            request.RestrictionId,
            request.Actor,
            request.Reason,
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return Response<MultiplayerRestrictionResultDto>.Fail(result.ErrorMessage);
        }

        return Response<MultiplayerRestrictionResultDto>.Success(
            new MultiplayerRestrictionResultDto(
                result.Value.Action.Id,
                mapper.Map<AccountRestrictionDto>(result.Value.Restriction),
                result.Value.WasAlreadyProcessed));
    }
}
