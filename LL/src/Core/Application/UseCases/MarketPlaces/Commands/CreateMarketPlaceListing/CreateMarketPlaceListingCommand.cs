using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.MarketPlaces;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using Domain.Models.MarketPlaces;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.CreateMarketPlaceListing;
public record CreateMarketPlaceListingCommand(Guid CharacterId, CreateMarketPlaceListingRequest Listing) : ICommand<Response<CreateMarketPlaceListingResponseDto>>;
public class CreateMarketPlaceListingCommandHandler : IRequestHandler<CreateMarketPlaceListingCommand, Response<CreateMarketPlaceListingResponseDto>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly MarketplaceChangePublisher _changePublisher;
    private readonly IMapper _mapper;

    public CreateMarketPlaceListingCommandHandler(
        IMarketPlaceService marketPlaceService,
        MarketplaceChangePublisher changePublisher,
        IMapper mapper)
    {
        _marketPlaceService = marketPlaceService;
        _changePublisher = changePublisher;
        _mapper = mapper;
    }

    public async Task<Response<CreateMarketPlaceListingResponseDto>> Handle(CreateMarketPlaceListingCommand request, CancellationToken cancellationToken)
    {
        if (request.Listing.Quantity <= 0 || request.Listing.UnitPrice <= 0) return Response<CreateMarketPlaceListingResponseDto>.Fail("Failed to create marketplace listing.");
        var marketPlaceListing = new MarketPlaceListing()
        {
            SellerId = request.CharacterId,
            ItemInstanceId = request.Listing.ItemInstanceId,
            Quantity = request.Listing.Quantity,
            UnitPrice = request.Listing.UnitPrice,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await _marketPlaceService.CreateMarketPlaceListingAsync(request.CharacterId, marketPlaceListing, cancellationToken);
        if (result == null) return Response<CreateMarketPlaceListingResponseDto>.Fail("Failed to create marketplace listing.");

        var listing = _mapper.Map<MarketPlaceListingDto?>(result.Listing);
        var marketplace = await _changePublisher.PublishAsync(
            listing is null
                ? []
                : [new MarketplaceListingChangeDto(listing.Id, listing)],
            result.Fills.Select(fill => new MarketplaceBuyOrderChangeDto(
                fill.BuyOrderId,
                _mapper.Map<MarketPlaceBuyOrderDto?>(fill.RemainingBuyOrder))).ToArray(),
            result.Fills.Select(fill => _mapper.Map<MarketPlaceOrderDto>(fill.Order)).ToArray(),
            result.Fills.Select(fill => fill.BuyerId).Append(request.CharacterId),
            nameof(CreateMarketPlaceListingCommand),
            cancellationToken);

        return Response<CreateMarketPlaceListingResponseDto>.Success(new CreateMarketPlaceListingResponseDto
        {
            Listing = listing,
            ListedItemInstanceId = request.Listing.ItemInstanceId,
            ListedQuantity = result.Listing?.Quantity ?? 0,
            FilledQuantity = result.FilledQuantity,
            FilledTotalPrice = result.FilledTotalPrice,
            SellerFees = result.SellerFees,
            SellerCinders = result.SellerCinders,
            RemainingInventoryItem = _mapper.Map<InventoryItemDto?>(result.RemainingSellerInventoryItem),
            Marketplace = marketplace
        });
    }
}
