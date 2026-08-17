using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Commands.RevokeAccountBan;

public sealed record RevokeAccountBanCommand(
    Guid OperationId,
    Guid RestrictionId,
    AdministrationActor Actor,
    string Reason) : ICommand<Response<AccountBanResultDto>>;

public sealed class RevokeAccountBanCommandHandler(
    ILiveOpsService liveOps,
    IMapper mapper)
    : IRequestHandler<RevokeAccountBanCommand, Response<AccountBanResultDto>>
{
    public async Task<Response<AccountBanResultDto>> Handle(
        RevokeAccountBanCommand request,
        CancellationToken cancellationToken)
    {
        var result = await liveOps.RevokeAccountBanAsync(
            request.OperationId,
            request.RestrictionId,
            request.Actor,
            request.Reason,
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return Response<AccountBanResultDto>.Fail(result.ErrorMessage);
        }

        return Response<AccountBanResultDto>.Success(new AccountBanResultDto(
            result.Value.Action.Id,
            mapper.Map<AccountRestrictionDto>(result.Value.Restriction),
            result.Value.WasAlreadyProcessed));
    }
}
