using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Commands.UpdateAccountRiskStatus;

public sealed record UpdateAccountRiskStatusCommand(
    Guid OperationId,
    Guid AccountId,
    AccountInvestigationStatus Status,
    AdministrationActor Actor,
    string Reason) : ICommand<Response<AccountRiskOperationDto>>;

public sealed class UpdateAccountRiskStatusCommandHandler(ILiveOpsAccountRiskService service)
    : IRequestHandler<UpdateAccountRiskStatusCommand, Response<AccountRiskOperationDto>>
{
    public async Task<Response<AccountRiskOperationDto>> Handle(UpdateAccountRiskStatusCommand request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateStatusAsync(request.OperationId, request.AccountId, request.Status, request.Actor, request.Reason, cancellationToken);
        return result.IsSuccess
            ? Response<AccountRiskOperationDto>.Success(new AccountRiskOperationDto(result.OperationId, result.WasAlreadyProcessed, result.Status, null))
            : Response<AccountRiskOperationDto>.Fail(result.ErrorMessage);
    }
}
