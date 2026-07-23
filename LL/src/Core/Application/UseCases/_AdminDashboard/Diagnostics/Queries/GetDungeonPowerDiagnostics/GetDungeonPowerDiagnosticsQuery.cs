using Application.Interfaces.Services.LL.PowerRatings;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.GetDungeonPowerDiagnostics;

public sealed record GetDungeonPowerDiagnosticsQuery : IRequest<IReadOnlyList<DungeonPowerDiagnostic>>;

public sealed class GetDungeonPowerDiagnosticsQueryHandler
    : IRequestHandler<GetDungeonPowerDiagnosticsQuery, IReadOnlyList<DungeonPowerDiagnostic>>
{
    private readonly IPowerAnalysisDiagnostics _diagnostics;

    public GetDungeonPowerDiagnosticsQueryHandler(IPowerAnalysisDiagnostics diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public Task<IReadOnlyList<DungeonPowerDiagnostic>> Handle(
        GetDungeonPowerDiagnosticsQuery request,
        CancellationToken cancellationToken) =>
        _diagnostics.AnalyzeAllDungeonsAsync(cancellationToken);
}
