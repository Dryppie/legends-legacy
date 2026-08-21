using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.MarketPlaces;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.BuyoutMarketPlaceListing;

public record BuyoutMarketPlaceListingCommand(Guid CharacterId, BuyoutMarketPlaceListingRequest Buyout) : ICommand<Response<BuyoutMarketPlaceListingResponseDto>>;

public class BuyoutMarketPlaceListingCommandHandler : IRequestHandler<BuyoutMarketPlaceListingCommand, Response<BuyoutMarketPlaceListingResponseDto>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly MarketplaceChangePublisher _changePublisher;
    private readonly IMapper _mapper;

    public BuyoutMarketPlaceListingCommandHandler(
        IMarketPlaceService marketPlaceService,
        MarketplaceChangePublisher changePublisher,
        IMapper mapper)
    {
        _marketPlaceService = marketPlaceService;
        _changePublisher = changePublisher;
        _mapper = mapper;
    }

    public async Task<Response<BuyoutMarketPlaceListingResponseDto>> Handle(BuyoutMarketPlaceListingCommand request, CancellationToken cancellationToken)
    {
        if (request.Buyout.Quantity <= 0)
            return Response<BuyoutMarketPlaceListingResponseDto>.Fail("Failed to buy order.");

        var result = await _marketPlaceService.BuyoutMarketPlaceListingAsync(
            request.CharacterId,
            request.Buyout.MarketPlaceListingId,
            request.Buyout.Quantity,
            cancellationToken);

        if (result == null)
            return Response<BuyoutMarketPlaceListingResponseDto>.Fail("Failed to buy order.");

        var remainingListing = _mapper.Map<MarketPlaceListingDto?>(result.RemainingListing);

        var marketplace = await _changePublisher.PublishAsync(
            [new MarketplaceListingChangeDto(result.ListingId, remainingListing)],
            [],
            [_mapper.Map<MarketPlaceOrderDto>(result.Order)],
            [request.CharacterId, result.SellerId],
            nameof(BuyoutMarketPlaceListingCommand),
            cancellationToken);

        return Response<BuyoutMarketPlaceListingResponseDto>.Success(new BuyoutMarketPlaceListingResponseDto
        {
            ListingId = result.ListingId,
            RemainingListing = remainingListing,
            PurchasedItem = _mapper.Map<InventoryItemDto>(result.PurchasedItem),
            PurchasedQuantity = result.Quantity,
            TotalPrice = result.TotalPrice,
            BuyerCinders = result.BuyerCinders,
            Marketplace = marketplace
        });
    }
}
