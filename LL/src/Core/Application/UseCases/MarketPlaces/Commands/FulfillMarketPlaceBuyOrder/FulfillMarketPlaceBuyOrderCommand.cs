using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.MarketPlaces;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.FulfillMarketPlaceBuyOrder;

public record FulfillMarketPlaceBuyOrderCommand(Guid CharacterId, FulfillMarketPlaceBuyOrderRequest Fulfillment) : ICommand<Response<FulfillMarketPlaceBuyOrderResponseDto>>;

public class FulfillMarketPlaceBuyOrderCommandHandler : IRequestHandler<FulfillMarketPlaceBuyOrderCommand, Response<FulfillMarketPlaceBuyOrderResponseDto>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly MarketplaceChangePublisher _changePublisher;
    private readonly IMapper _mapper;

    public FulfillMarketPlaceBuyOrderCommandHandler(
        IMarketPlaceService marketPlaceService,
        MarketplaceChangePublisher changePublisher,
        IMapper mapper)
    {
        _marketPlaceService = marketPlaceService;
        _changePublisher = changePublisher;
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

        response.Marketplace = await _changePublisher.PublishAsync(
            [],
            [new MarketplaceBuyOrderChangeDto(result.BuyOrderId, response.RemainingBuyOrder)],
            [_mapper.Map<MarketPlaceOrderDto>(result.Order)],
            [result.BuyerId, result.SellerId],
            nameof(FulfillMarketPlaceBuyOrderCommand),
            cancellationToken);

        return Response<FulfillMarketPlaceBuyOrderResponseDto>.Success(response);
    }
}
