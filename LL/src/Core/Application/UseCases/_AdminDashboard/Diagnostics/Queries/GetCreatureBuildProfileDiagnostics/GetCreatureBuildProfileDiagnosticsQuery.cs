using Application.Interfaces.Services.LL.Entities;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.GetCreatureBuildProfileDiagnostics;

public record GetCreatureBuildProfileDiagnosticsQuery() : IRequest<CreatureBuildProfileDiagnosticReport>;

public sealed class GetCreatureBuildProfileDiagnosticsQueryHandler
    : IRequestHandler<GetCreatureBuildProfileDiagnosticsQuery, CreatureBuildProfileDiagnosticReport>
{
    private readonly ICreatureBuildProfileDiagnostics _diagnostics;

    public GetCreatureBuildProfileDiagnosticsQueryHandler(ICreatureBuildProfileDiagnostics diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public Task<CreatureBuildProfileDiagnosticReport> Handle(
        GetCreatureBuildProfileDiagnosticsQuery request,
        CancellationToken cancellationToken) =>
        _diagnostics.CreateReportAsync(cancellationToken);
}
