using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Commands.BanAccount;

public sealed record BanAccountCommand(
    Guid OperationId,
    Guid AccountId,
    AdministrationActor Actor,
    string Reason,
    string? InternalNotes,
    DateTimeOffset? ExpiresAt) : ICommand<Response<AccountBanResultDto>>;

public sealed class BanAccountCommandHandler(
    ILiveOpsService liveOps,
    IMapper mapper)
    : IRequestHandler<BanAccountCommand, Response<AccountBanResultDto>>
{
    public async Task<Response<AccountBanResultDto>> Handle(
        BanAccountCommand request,
        CancellationToken cancellationToken)
    {
        var result = await liveOps.BanAccountAsync(
            request.OperationId,
            request.AccountId,
            request.Actor,
            request.Reason,
            request.InternalNotes,
            request.ExpiresAt,
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
