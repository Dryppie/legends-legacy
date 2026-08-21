using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.MarketPlaces;
using Application.UseCases.MarketPlaces.Dtos.Requests;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.MarketPlaces.Commands.SellCommodity;

public sealed record SellCommodityCommand(Guid CharacterId, SellCommodityRequest Sell)
    : ICommand<Response<SellCommodityResponseDto>>;

public sealed class SellCommodityCommandHandler(
    IMarketPlaceService marketPlaceService,
    MarketplaceChangePublisher changePublisher,
    IMapper mapper)
    : IRequestHandler<SellCommodityCommand, Response<SellCommodityResponseDto>>
{
    public async Task<Response<SellCommodityResponseDto>> Handle(
        SellCommodityCommand request,
        CancellationToken cancellationToken)
    {
        var result = await marketPlaceService.SellCommodityAsync(
            request.CharacterId,
            request.Sell.ItemInstanceId,
            request.Sell.Quantity,
            request.Sell.MinimumUnitPrice,
            cancellationToken);
        if (result == null)
            return Response<SellCommodityResponseDto>.Fail(
                "There is not enough demand at or above that price.");

        var marketplace = await changePublisher.PublishAsync(
            [],
            result.Fills.Select(fill => new MarketplaceBuyOrderChangeDto(
                fill.BuyOrderId,
                mapper.Map<MarketPlaceBuyOrderDto?>(fill.RemainingBuyOrder))).ToArray(),
            result.Fills.Select(fill => mapper.Map<MarketPlaceOrderDto>(fill.Order)).ToArray(),
            result.Fills.Select(fill => fill.BuyerId).Append(request.CharacterId),
            nameof(SellCommodityCommand),
            cancellationToken);

        var remaining = result.Fills.LastOrDefault()?.RemainingSellerInventoryItem;
        return Response<SellCommodityResponseDto>.Success(new(
            result.FilledQuantity,
            result.TotalPrice,
            result.SellerFees,
            result.SellerCinders,
            mapper.Map<InventoryItemDto?>(remaining),
            marketplace));
    }
}
