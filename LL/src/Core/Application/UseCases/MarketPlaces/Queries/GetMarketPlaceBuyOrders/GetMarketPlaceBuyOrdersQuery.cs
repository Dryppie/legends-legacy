using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Queries.GetMarketPlaceBuyOrders;

public record GetMarketPlaceBuyOrdersQuery() : IQuery<Response<List<MarketPlaceBuyOrderDto>>>;

public class GetMarketPlaceBuyOrdersQueryHandler : IRequestHandler<GetMarketPlaceBuyOrdersQuery, Response<List<MarketPlaceBuyOrderDto>>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly IMapper _mapper;

    public GetMarketPlaceBuyOrdersQueryHandler(IMarketPlaceService marketPlaceService, IMapper mapper)
    {
        _marketPlaceService = marketPlaceService;
        _mapper = mapper;
    }

    public async Task<Response<List<MarketPlaceBuyOrderDto>>> Handle(GetMarketPlaceBuyOrdersQuery request, CancellationToken cancellationToken)
    {
        var buyOrders = await _marketPlaceService.GetMarketPlaceBuyOrdersAsync(cancellationToken);

        return Response<List<MarketPlaceBuyOrderDto>>.Success(_mapper.Map<List<MarketPlaceBuyOrderDto>>(buyOrders));
    }
}
