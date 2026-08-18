using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Mappings;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Commands.AddAccountRiskNote;

public sealed record AddAccountRiskNoteCommand(
    Guid OperationId,
    Guid AccountId,
    AdministrationActor Actor,
    string Note) : ICommand<Response<AccountRiskOperationDto>>;

public sealed class AddAccountRiskNoteCommandHandler(ILiveOpsAccountRiskService service)
    : IRequestHandler<AddAccountRiskNoteCommand, Response<AccountRiskOperationDto>>
{
    public async Task<Response<AccountRiskOperationDto>> Handle(AddAccountRiskNoteCommand request, CancellationToken cancellationToken)
    {
        var result = await service.AddNoteAsync(request.OperationId, request.AccountId, request.Actor, request.Note, cancellationToken);
        return result.IsSuccess
            ? Response<AccountRiskOperationDto>.Success(new AccountRiskOperationDto(
                result.OperationId,
                result.WasAlreadyProcessed,
                null,
                result.Note is null ? null : AccountRiskDtoMapper.ToDto(result.Note)))
            : Response<AccountRiskOperationDto>.Fail(result.ErrorMessage);
    }
}
