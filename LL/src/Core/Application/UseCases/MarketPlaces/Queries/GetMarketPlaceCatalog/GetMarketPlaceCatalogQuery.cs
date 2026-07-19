using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Queries.GetMarketPlaceCatalog;

public sealed record GetMarketPlaceCatalogQuery : IQuery<Response<List<ItemBaseDto>>>;

public sealed class GetMarketPlaceCatalogQueryHandler(
    IMarketPlaceService marketPlaceService,
    IMapper mapper)
    : IRequestHandler<GetMarketPlaceCatalogQuery, Response<List<ItemBaseDto>>>
{
    public async Task<Response<List<ItemBaseDto>>> Handle(
        GetMarketPlaceCatalogQuery request,
        CancellationToken cancellationToken)
    {
        var items = await marketPlaceService.GetTradableItemBasesAsync(cancellationToken);
        return Response<List<ItemBaseDto>>.Success(mapper.Map<List<ItemBaseDto>>(items));
    }
}
