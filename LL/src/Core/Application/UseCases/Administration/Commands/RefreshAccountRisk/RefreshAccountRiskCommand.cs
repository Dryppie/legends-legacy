using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Administration.Commands.RefreshAccountRisk;

public sealed record RefreshAccountRiskCommand : ICommand<Response<int>>;

public sealed class RefreshAccountRiskCommandHandler(ILiveOpsAccountRiskService service)
    : IRequestHandler<RefreshAccountRiskCommand, Response<int>>
{
    public async Task<Response<int>> Handle(RefreshAccountRiskCommand request, CancellationToken cancellationToken) =>
        Response<int>.Success(await service.RefreshAsync(cancellationToken));
}
