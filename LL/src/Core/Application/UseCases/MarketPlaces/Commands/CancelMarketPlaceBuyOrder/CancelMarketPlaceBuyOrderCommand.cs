using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.MarketPlaces;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.CancelMarketPlaceBuyOrder;

public record CancelMarketPlaceBuyOrderCommand(Guid CharacterId, string BuyOrderId) : ICommand<Response<CancelMarketPlaceBuyOrderResponseDto>>;

public class CancelMarketPlaceBuyOrderCommandHandler : IRequestHandler<CancelMarketPlaceBuyOrderCommand, Response<CancelMarketPlaceBuyOrderResponseDto>>
{
    private readonly IMarketPlaceService _marketPlaceService;
    private readonly MarketplaceChangePublisher _changePublisher;
    private readonly IMapper _mapper;

    public CancelMarketPlaceBuyOrderCommandHandler(
        IMarketPlaceService marketPlaceService,
        MarketplaceChangePublisher changePublisher,
        IMapper mapper)
    {
        _marketPlaceService = marketPlaceService;
        _changePublisher = changePublisher;
        _mapper = mapper;
    }

    public async Task<Response<CancelMarketPlaceBuyOrderResponseDto>> Handle(CancelMarketPlaceBuyOrderCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.BuyOrderId, out var buyOrderId))
            return Response<CancelMarketPlaceBuyOrderResponseDto>.Fail("Invalid buy order");

        var result = await _marketPlaceService.CancelMarketPlaceBuyOrderAsync(request.CharacterId, buyOrderId, cancellationToken);
        if (result == null)
            return Response<CancelMarketPlaceBuyOrderResponseDto>.Fail("Failed to cancel buy order");

        var marketplace = await _changePublisher.PublishAsync(
            [],
            [new MarketplaceBuyOrderChangeDto(buyOrderId, null)],
            [],
            [request.CharacterId],
            nameof(CancelMarketPlaceBuyOrderCommand),
            cancellationToken);

        var response = _mapper.Map<CancelMarketPlaceBuyOrderResponseDto>(result);
        response.Marketplace = marketplace;
        return Response<CancelMarketPlaceBuyOrderResponseDto>.Success(response);
    }
}
