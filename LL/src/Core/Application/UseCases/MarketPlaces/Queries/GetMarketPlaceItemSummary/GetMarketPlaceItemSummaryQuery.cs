using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Queries.GetMarketPlaceItemSummary;

public sealed record GetMarketPlaceItemSummaryQuery(string ItemBaseId)
    : IQuery<Response<MarketPlaceItemSummaryDto>>;

public sealed class GetMarketPlaceItemSummaryQueryHandler(IMarketPlaceService marketplace)
    : IRequestHandler<GetMarketPlaceItemSummaryQuery, Response<MarketPlaceItemSummaryDto>>
{
    public async Task<Response<MarketPlaceItemSummaryDto>> Handle(
        GetMarketPlaceItemSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var summary = await marketplace.GetItemSummaryAsync(request.ItemBaseId, cancellationToken);
        return Response<MarketPlaceItemSummaryDto>.Success(new MarketPlaceItemSummaryDto(
            summary.ItemBaseId,
            summary.LowestSellUnitPrice,
            summary.TotalSellQuantity,
            summary.HighestBuyUnitPrice,
            summary.TotalBuyQuantity,
            summary.LastTradeUnitPrice,
            summary.MedianUnitPrice7Days,
            summary.TradeVolume24Hours));
    }
}
