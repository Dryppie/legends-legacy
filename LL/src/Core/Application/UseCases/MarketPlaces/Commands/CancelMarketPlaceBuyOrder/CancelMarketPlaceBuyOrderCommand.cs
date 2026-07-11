using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.CancelMarketPlaceBuyOrder;

public record CancelMarketPlaceBuyOrderCommand(Guid CharacterId, string BuyOrderId) : ICommand<Response<CancelMarketPlaceBuyOrderResponseDto>>;

public class CancelMarketPlaceBuyOrderCommandHandler : IRequestHandler<CancelMarketPlaceBuyOrderCommand, Response<CancelMarketPlaceBuyOrderResponseDto>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly IGameEventPublisher _eventPublisher;
    private readonly IMapper _mapper;

    public CancelMarketPlaceBuyOrderCommandHandler(
        IMarketPlaceService marketPlaceService,
        IGameEventPublisher eventPublisher,
        IMapper mapper)
    {
        _marketPlaceService = marketPlaceService;
        _eventPublisher = eventPublisher;
        _mapper = mapper;
    }

    public async Task<Response<CancelMarketPlaceBuyOrderResponseDto>> Handle(CancelMarketPlaceBuyOrderCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.BuyOrderId, out var buyOrderId))
            return Response<CancelMarketPlaceBuyOrderResponseDto>.Fail("Invalid buy order");

        var result = await _marketPlaceService.CancelMarketPlaceBuyOrderAsync(request.CharacterId, buyOrderId, cancellationToken);
        if (result == null)
            return Response<CancelMarketPlaceBuyOrderResponseDto>.Fail("Failed to cancel buy order");

        await _eventPublisher.PublishAsync(
            new Audience.World(),
            new MarketBuyOrderCanceledMsg(buyOrderId, request.CharacterId));

        return Response<CancelMarketPlaceBuyOrderResponseDto>.Success(
            _mapper.Map<CancelMarketPlaceBuyOrderResponseDto>(result));
    }
}
