using Application.Interfaces.Services.LL.Essences;
using MediatR;

namespace Application.UseCases._AdminDashboard.Essences.Queries.GetEssenceCatalog;

public record GetEssenceCatalogQuery() : IRequest<EssenceCatalogReport>;

public sealed class GetEssenceCatalogQueryHandler
    : IRequestHandler<GetEssenceCatalogQuery, EssenceCatalogReport>
{
    private readonly IEssenceCatalogService _catalogService;

    public GetEssenceCatalogQueryHandler(IEssenceCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public Task<EssenceCatalogReport> Handle(
        GetEssenceCatalogQuery request,
        CancellationToken cancellationToken) =>
        _catalogService.GetCatalogAsync(cancellationToken);
}
