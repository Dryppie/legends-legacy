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

namespace Application.UseCases.MarketPlaces.Commands.SellCommodity;

public sealed record SellCommodityCommand(Guid CharacterId, SellCommodityRequest Sell)
    : ICommand<Response<SellCommodityResponseDto>>;

public sealed class SellCommodityCommandHandler(
    IMarketPlaceService marketPlaceService,
    IGameEventPublisher eventPublisher,
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

        foreach (var fill in result.Fills)
        {
            await eventPublisher.PublishAsync(
                new Audience.World(),
                new MarketBuyOrderFulfilledMsg(
                    fill.BuyOrderId,
                    fill.BuyerId,
                    fill.SellerId,
                    fill.Quantity,
                    fill.TotalPrice,
                    fill.SellerCinders,
                    mapper.Map<InventoryItemDto>(fill.PurchasedItem),
                    mapper.Map<MarketPlaceBuyOrderDto?>(fill.RemainingBuyOrder)));
        }

        var remaining = result.Fills.LastOrDefault()?.RemainingSellerInventoryItem;
        return Response<SellCommodityResponseDto>.Success(new(
            result.FilledQuantity,
            result.TotalPrice,
            result.SellerFees,
            result.SellerCinders,
            mapper.Map<InventoryItemDto?>(remaining)));
    }
}
