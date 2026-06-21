using Application.Interfaces.Services.LL.Essences;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogCoverage;

public record GetAbilityCatalogCoverageQuery() : IRequest<AbilityCatalogCoverageReport>;

public sealed class GetAbilityCatalogCoverageQueryHandler
    : IRequestHandler<GetAbilityCatalogCoverageQuery, AbilityCatalogCoverageReport>
{
    private readonly IAbilityCatalogCoverageAnalyzer _analyzer;

    public GetAbilityCatalogCoverageQueryHandler(IAbilityCatalogCoverageAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public Task<AbilityCatalogCoverageReport> Handle(
        GetAbilityCatalogCoverageQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(_analyzer.Analyze());
}
