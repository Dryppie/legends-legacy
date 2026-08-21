using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Items.Dtos;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Queries.GetMarketPlaceSnapshot;

public sealed record GetMarketPlaceSnapshotQuery(Guid CharacterId, int HistoryTake = 50)
    : IQuery<Response<MarketPlaceSnapshotDto>>;

public sealed class GetMarketPlaceSnapshotQueryHandler(
    IMarketPlaceService marketPlaceService,
    IMapper mapper)
    : IRequestHandler<GetMarketPlaceSnapshotQuery, Response<MarketPlaceSnapshotDto>>
{
    public async Task<Response<MarketPlaceSnapshotDto>> Handle(
        GetMarketPlaceSnapshotQuery request,
        CancellationToken cancellationToken)
    {
        // These reads intentionally remain sequential because the service uses
        // one scoped EF DbContext. The endpoint consolidates transport without
        // introducing concurrent operations on that context.
        var listings = await marketPlaceService.GetMarketPlaceListingsAsync(cancellationToken);
        var catalog = await marketPlaceService.GetTradableItemBasesAsync(cancellationToken);
        var history = await marketPlaceService.GetOrderHistoryAsync(
            request.CharacterId,
            request.HistoryTake,
            cancellationToken);
        var buyOrders = await marketPlaceService.GetMarketPlaceBuyOrdersAsync(cancellationToken);

        return Response<MarketPlaceSnapshotDto>.Success(new MarketPlaceSnapshotDto(
            mapper.Map<List<MarketPlaceListingDto>>(listings),
            mapper.Map<List<ItemBaseDto>>(catalog),
            mapper.Map<List<MarketPlaceOrderDto>>(history),
            mapper.Map<List<MarketPlaceBuyOrderDto>>(buyOrders)));
    }
}
