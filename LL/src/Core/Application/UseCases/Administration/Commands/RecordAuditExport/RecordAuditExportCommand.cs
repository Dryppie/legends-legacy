using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Commands.RecordAuditExport;

public sealed record RecordAuditExportCommand(
    Guid OperationId,
    AdministrationActor Actor,
    int RowCount,
    string DetailsJson) : ICommand<Response<Guid>>;

public sealed class RecordAuditExportCommandHandler(ILiveOpsService liveOps)
    : IRequestHandler<RecordAuditExportCommand, Response<Guid>>
{
    public async Task<Response<Guid>> Handle(
        RecordAuditExportCommand request,
        CancellationToken cancellationToken)
    {
        var result = await liveOps.RecordAuditExportAsync(
            request.OperationId,
            request.Actor,
            request.RowCount,
            request.DetailsJson,
            cancellationToken);
        return result.IsSuccess
            ? Response<Guid>.Success(request.OperationId)
            : Response<Guid>.Fail(result.ErrorMessage);
    }
}
