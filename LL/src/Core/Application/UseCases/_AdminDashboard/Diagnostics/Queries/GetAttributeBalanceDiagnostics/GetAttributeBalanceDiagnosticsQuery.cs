using Application.Interfaces.Services.LL.Balance;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAttributeBalanceDiagnostics;

public sealed record GetAttributeBalanceDiagnosticsQuery : IRequest<AttributeBalanceAnalysisReport>;

public sealed class GetAttributeBalanceDiagnosticsQueryHandler
    : IRequestHandler<GetAttributeBalanceDiagnosticsQuery, AttributeBalanceAnalysisReport>
{
    private readonly IAttributeMarginalValueAnalyzer _analyzer;

    public GetAttributeBalanceDiagnosticsQueryHandler(IAttributeMarginalValueAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public Task<AttributeBalanceAnalysisReport> Handle(
        GetAttributeBalanceDiagnosticsQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(_analyzer.Analyze(cancellationToken));
}
