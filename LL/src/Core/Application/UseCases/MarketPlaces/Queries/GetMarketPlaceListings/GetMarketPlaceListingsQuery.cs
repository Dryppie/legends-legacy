using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Queries.GetMarketPlaceListings;
public record GetMarketPlaceListingsQuery() : IQuery<Response<List<MarketPlaceListingDto>>>;
public class GetMarketPlaceListingsQueryHandler : IRequestHandler<GetMarketPlaceListingsQuery, Response<List<MarketPlaceListingDto>>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly IMapper _mapper;

    public GetMarketPlaceListingsQueryHandler(IMarketPlaceService marketPlaceService, IMapper mapper)
    {
        _marketPlaceService = marketPlaceService;
        _mapper = mapper;
    }

    public async Task<Response<List<MarketPlaceListingDto>>> Handle(GetMarketPlaceListingsQuery request, CancellationToken cancellationToken)
    {
        var marketPlaceListings = await _marketPlaceService.GetMarketPlaceListingsAsync(cancellationToken);

        return Response<List<MarketPlaceListingDto>>.Success(_mapper.Map<List<MarketPlaceListingDto>>(marketPlaceListings));
    }
}
