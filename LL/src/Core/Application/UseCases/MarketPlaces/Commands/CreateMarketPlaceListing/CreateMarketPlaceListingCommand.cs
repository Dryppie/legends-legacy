using Application.Interfaces.Services.LL;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using Domain.Models.MarketPlaces;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.CreateMarketPlaceListing;
public record CreateMarketPlaceListingCommand(Guid CharacterId, CreateMarketPlaceListingRequest Listing) : IRequest<Response<MarketPlaceListingDto>>;
public class CreateMarketPlaceListingCommandHandler : IRequestHandler<CreateMarketPlaceListingCommand, Response<MarketPlaceListingDto>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly IMapper _mapper;

    public CreateMarketPlaceListingCommandHandler(IMarketPlaceService marketPlaceService, IMapper mapper)
    {
        _marketPlaceService = marketPlaceService;
        _mapper = mapper;
    }

    public async Task<Response<MarketPlaceListingDto>> Handle(CreateMarketPlaceListingCommand request, CancellationToken cancellationToken)
    {
        if (request.Listing.Quantity <= 0 || request.Listing.UnitPrice <= 0) return Response<MarketPlaceListingDto>.Fail("Failed to create marketplace listing.");
        var marketPlaceListing = new MarketPlaceListing()
        {
            SellerId = request.CharacterId,
            ItemInstanceId = request.Listing.ItemInstanceId,
            Quantity = request.Listing.Quantity,
            UnitPrice = request.Listing.UnitPrice,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var listing = await _marketPlaceService.CreateMarketPlaceListingAsync(request.CharacterId, marketPlaceListing, cancellationToken);
        if (listing == null) return Response<MarketPlaceListingDto>.Fail("Failed to create marketplace listing.");

        return Response<MarketPlaceListingDto>.Success(_mapper.Map<MarketPlaceListingDto>(listing));
    }
}
