using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using Domain.Models.MarketPlaces;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.CreateMarketPlaceListing;
public record CreateMarketPlaceListingCommand(Guid CharacterId, CreateMarketPlaceListingRequest Listing) : ICommand<Response<CreateMarketPlaceListingResponseDto>>;
public class CreateMarketPlaceListingCommandHandler : IRequestHandler<CreateMarketPlaceListingCommand, Response<CreateMarketPlaceListingResponseDto>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly IInventoryService _inventoryService;
    private readonly IGameEventPublisher _eventPublisher;
    private readonly IMapper _mapper;

    public CreateMarketPlaceListingCommandHandler(
        IMarketPlaceService marketPlaceService,
        IInventoryService inventoryService,
        IGameEventPublisher eventPublisher,
        IMapper mapper)
    {
        _marketPlaceService = marketPlaceService;
        _inventoryService = inventoryService;
        _eventPublisher = eventPublisher;
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

        var listing = await _marketPlaceService.CreateMarketPlaceListingAsync(request.CharacterId, marketPlaceListing, cancellationToken);
        if (listing == null) return Response<CreateMarketPlaceListingResponseDto>.Fail("Failed to create marketplace listing.");

        var dto = _mapper.Map<MarketPlaceListingDto>(listing);
        var inventory = await _inventoryService.GetInventoryByIdAsync(request.CharacterId, cancellationToken);
        var remainingInventoryItem = inventory?.InventoryItems
            .FirstOrDefault(item => item.ItemInstanceId == request.Listing.ItemInstanceId);

        await _eventPublisher.PublishAsync(
            new Audience.World(),
            new MarketListingCreatedMsg(dto));

        return Response<CreateMarketPlaceListingResponseDto>.Success(new CreateMarketPlaceListingResponseDto
        {
            Listing = dto,
            ListedItemInstanceId = request.Listing.ItemInstanceId,
            ListedQuantity = request.Listing.Quantity,
            RemainingInventoryItem = _mapper.Map<InventoryItemDto?>(remainingInventoryItem)
        });
    }
}
