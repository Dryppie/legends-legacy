using Application.Interfaces.Services.LL.Regions;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.GetRegionOneContentDiagnostics;

public record GetRegionOneContentDiagnosticsQuery() : IRequest<RegionOneContentDiagnosticReport>;

public sealed class GetRegionOneContentDiagnosticsQueryHandler
    : IRequestHandler<GetRegionOneContentDiagnosticsQuery, RegionOneContentDiagnosticReport>
{
    private readonly IRegionOneContentDiagnostics _diagnostics;

    public GetRegionOneContentDiagnosticsQueryHandler(IRegionOneContentDiagnostics diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public Task<RegionOneContentDiagnosticReport> Handle(
        GetRegionOneContentDiagnosticsQuery request,
        CancellationToken cancellationToken) =>
        _diagnostics.AnalyzeAsync(cancellationToken);
}
