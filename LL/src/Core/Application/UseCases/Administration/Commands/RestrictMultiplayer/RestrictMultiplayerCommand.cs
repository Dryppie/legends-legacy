using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Commands.RestrictMultiplayer;

public sealed record RestrictMultiplayerCommand(
    Guid OperationId,
    Guid AccountId,
    AdministrationActor Actor,
    string Reason,
    string? InternalNotes,
    DateTimeOffset? ExpiresAt) : ICommand<Response<MultiplayerRestrictionResultDto>>;

public sealed class RestrictMultiplayerCommandHandler(
    ILiveOpsService liveOps,
    IMapper mapper)
    : IRequestHandler<RestrictMultiplayerCommand, Response<MultiplayerRestrictionResultDto>>
{
    public async Task<Response<MultiplayerRestrictionResultDto>> Handle(
        RestrictMultiplayerCommand request,
        CancellationToken cancellationToken)
    {
        var result = await liveOps.RestrictMultiplayerAsync(
            request.OperationId,
            request.AccountId,
            request.Actor,
            request.Reason,
            request.InternalNotes,
            request.ExpiresAt,
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
