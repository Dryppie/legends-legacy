using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.FulfillMarketPlaceBuyOrder;

public record FulfillMarketPlaceBuyOrderCommand(Guid CharacterId, FulfillMarketPlaceBuyOrderRequest Fulfillment) : ICommand<Response<FulfillMarketPlaceBuyOrderResponseDto>>;

public class FulfillMarketPlaceBuyOrderCommandHandler : IRequestHandler<FulfillMarketPlaceBuyOrderCommand, Response<FulfillMarketPlaceBuyOrderResponseDto>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly IGameEventPublisher _eventPublisher;
    private readonly IMapper _mapper;

    public FulfillMarketPlaceBuyOrderCommandHandler(
        IMarketPlaceService marketPlaceService,
        IGameEventPublisher eventPublisher,
        IMapper mapper)
    {
        _marketPlaceService = marketPlaceService;
        _eventPublisher = eventPublisher;
        _mapper = mapper;
    }

    public async Task<Response<FulfillMarketPlaceBuyOrderResponseDto>> Handle(FulfillMarketPlaceBuyOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Fulfillment.Quantity <= 0)
            return Response<FulfillMarketPlaceBuyOrderResponseDto>.Fail("Failed to fulfill buy order.");

        var result = await _marketPlaceService.FulfillMarketPlaceBuyOrderAsync(
            request.CharacterId,
            request.Fulfillment.MarketPlaceBuyOrderId,
            request.Fulfillment.ItemInstanceId,
            request.Fulfillment.Quantity,
            cancellationToken);

        if (result == null)
            return Response<FulfillMarketPlaceBuyOrderResponseDto>.Fail("Failed to fulfill buy order.");

        var response = _mapper.Map<FulfillMarketPlaceBuyOrderResponseDto>(result);

        await _eventPublisher.PublishAsync(
            new Audience.World(),
            new MarketBuyOrderFulfilledMsg(
                result.BuyOrderId,
                result.BuyerId,
                result.SellerId,
                result.Quantity,
                result.TotalPrice,
                result.SellerCinders,
                response.PurchasedItem,
                response.RemainingBuyOrder));

        return Response<FulfillMarketPlaceBuyOrderResponseDto>.Success(response);
    }
}
