using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.BuyoutMarketPlaceListing;

public record BuyoutMarketPlaceListingCommand(Guid CharacterId, BuyoutMarketPlaceListingRequest Buyout) : ICommand<Response<BuyoutMarketPlaceListingResponseDto>>;

public class BuyoutMarketPlaceListingCommandHandler : IRequestHandler<BuyoutMarketPlaceListingCommand, Response<BuyoutMarketPlaceListingResponseDto>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly IGameEventPublisher _eventPublisher;
    private readonly IMapper _mapper;

    public BuyoutMarketPlaceListingCommandHandler(
        IMarketPlaceService marketPlaceService,
        IGameEventPublisher eventPublisher,
        IMapper mapper)
    {
        _marketPlaceService = marketPlaceService;
        _eventPublisher = eventPublisher;
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

        await _eventPublisher.PublishAsync(
            new Audience.Character(result.SellerId),
            new MarketListingSoldMsg(
                result.ListingId,
                result.SellerId,
                result.Quantity,
                result.TotalPrice,
                result.SellerCinders,
                remainingListing));

        return Response<BuyoutMarketPlaceListingResponseDto>.Success(new BuyoutMarketPlaceListingResponseDto
        {
            ListingId = result.ListingId,
            RemainingListing = remainingListing,
            PurchasedItem = _mapper.Map<InventoryItemDto>(result.PurchasedItem),
            BuyerCinders = result.BuyerCinders
        });
    }
}
