using Application.Interfaces.Services.LL.WorldTower;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.RunWorldTowerBalance;

public sealed record RunWorldTowerBalanceQuery(WorldTowerBalanceRequest Request)
    : IRequest<WorldTowerBalanceReport>;

public sealed class RunWorldTowerBalanceQueryHandler
    : IRequestHandler<RunWorldTowerBalanceQuery, WorldTowerBalanceReport>
{
    private readonly IWorldTowerBalanceAnalyzer _analyzer;

    public RunWorldTowerBalanceQueryHandler(IWorldTowerBalanceAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public Task<WorldTowerBalanceReport> Handle(
        RunWorldTowerBalanceQuery request,
        CancellationToken cancellationToken) =>
        _analyzer.AnalyzeAsync(request.Request, cancellationToken);
}
