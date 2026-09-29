using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Commands.RetryStateRefreshDelivery;
public sealed record RetryStateRefreshDeliveryCommand(Guid OperationId, Guid DeliveryId, AdministrationActor Actor, string Reason)
    : ICommand<Response<StateRefreshRecoveryResultDto>>;
public sealed class RetryStateRefreshDeliveryCommandHandler(IStateRefreshRecoveryService service, IMapper mapper)
    : IRequestHandler<RetryStateRefreshDeliveryCommand, Response<StateRefreshRecoveryResultDto>>
{
    public async Task<Response<StateRefreshRecoveryResultDto>> Handle(RetryStateRefreshDeliveryCommand request, CancellationToken ct)
    {
        var result = await service.QueueAsync(request.OperationId, request.DeliveryId, request.Actor, request.Reason, ct);
        return result.IsSuccess ? Response<StateRefreshRecoveryResultDto>.Success(mapper.Map<StateRefreshRecoveryResultDto>(result.Data))
            : result.IsConflict ? Response<StateRefreshRecoveryResultDto>.Conflict(result.ErrorMessage, result.ErrorCode)
            : Response<StateRefreshRecoveryResultDto>.Fail(result.ErrorMessage, result.ErrorCode);
    }
}
