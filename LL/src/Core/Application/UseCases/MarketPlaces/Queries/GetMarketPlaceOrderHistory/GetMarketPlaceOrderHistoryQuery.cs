using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Queries.GetMarketPlaceOrderHistory;

public sealed record GetMarketPlaceOrderHistoryQuery(Guid CharacterId, int Take = 50)
    : IQuery<Response<List<MarketPlaceOrderDto>>>;

public sealed class GetMarketPlaceOrderHistoryQueryHandler(
    IMarketPlaceService marketPlaceService,
    IMapper mapper)
    : IRequestHandler<GetMarketPlaceOrderHistoryQuery, Response<List<MarketPlaceOrderDto>>>
{
    public async Task<Response<List<MarketPlaceOrderDto>>> Handle(
        GetMarketPlaceOrderHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var history = await marketPlaceService.GetOrderHistoryAsync(
            request.CharacterId,
            request.Take,
            cancellationToken);
        return Response<List<MarketPlaceOrderDto>>.Success(
            mapper.Map<List<MarketPlaceOrderDto>>(history));
    }
}
