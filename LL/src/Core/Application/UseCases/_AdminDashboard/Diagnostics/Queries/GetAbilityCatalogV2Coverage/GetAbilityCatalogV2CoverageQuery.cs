using Application.Interfaces.Services.LL.Essences;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogV2Coverage;

public record GetAbilityCatalogV2CoverageQuery() : IRequest<AbilityCatalogV2CoverageReport>;

public sealed class GetAbilityCatalogV2CoverageQueryHandler
    : IRequestHandler<GetAbilityCatalogV2CoverageQuery, AbilityCatalogV2CoverageReport>
{
    private readonly IAbilityCatalogV2CoverageAnalyzer _analyzer;

    public GetAbilityCatalogV2CoverageQueryHandler(IAbilityCatalogV2CoverageAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public Task<AbilityCatalogV2CoverageReport> Handle(
        GetAbilityCatalogV2CoverageQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(_analyzer.Analyze());
}
