using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.MarketPlaces;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using Domain.Models.MarketPlaces;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.CreateMarketPlaceBuyOrder;

public record CreateMarketPlaceBuyOrderCommand(Guid CharacterId, CreateMarketPlaceBuyOrderRequest BuyOrder) : ICommand<Response<CreateMarketPlaceBuyOrderResponseDto>>;

public class CreateMarketPlaceBuyOrderCommandHandler : IRequestHandler<CreateMarketPlaceBuyOrderCommand, Response<CreateMarketPlaceBuyOrderResponseDto>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly MarketplaceChangePublisher _changePublisher;
    private readonly IMapper _mapper;

    public CreateMarketPlaceBuyOrderCommandHandler(
        IMarketPlaceService marketPlaceService,
        MarketplaceChangePublisher changePublisher,
        IMapper mapper)
    {
        _marketPlaceService = marketPlaceService;
        _changePublisher = changePublisher;
        _mapper = mapper;
    }

    public async Task<Response<CreateMarketPlaceBuyOrderResponseDto>> Handle(CreateMarketPlaceBuyOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.BuyOrder.Quantity <= 0 || request.BuyOrder.UnitPrice <= 0 || string.IsNullOrWhiteSpace(request.BuyOrder.ItemBaseId))
            return Response<CreateMarketPlaceBuyOrderResponseDto>.Fail("Failed to create buy order.");

        var buyOrder = new MarketPlaceBuyOrder
        {
            BuyerId = request.CharacterId,
            ItemBaseId = request.BuyOrder.ItemBaseId,
            Quantity = request.BuyOrder.Quantity,
            UnitPrice = request.BuyOrder.UnitPrice,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await _marketPlaceService.CreateMarketPlaceBuyOrderAsync(request.CharacterId, buyOrder, cancellationToken);
        if (result == null) return Response<CreateMarketPlaceBuyOrderResponseDto>.Fail("Failed to create buy order.");

        var response = _mapper.Map<CreateMarketPlaceBuyOrderResponseDto>(result);

        response.Marketplace = await _changePublisher.PublishAsync(
            result.Fills.Select(fill => new MarketplaceListingChangeDto(
                fill.ListingId,
                _mapper.Map<MarketPlaceListingDto?>(fill.RemainingListing))).ToArray(),
            response.BuyOrder is null
                ? []
                : [new MarketplaceBuyOrderChangeDto(response.BuyOrder.Id, response.BuyOrder)],
            result.Fills.Select(fill => _mapper.Map<MarketPlaceOrderDto>(fill.Order)).ToArray(),
            result.Fills.Select(fill => fill.SellerId).Append(request.CharacterId),
            nameof(CreateMarketPlaceBuyOrderCommand),
            cancellationToken);

        return Response<CreateMarketPlaceBuyOrderResponseDto>.Success(response);
    }
}
