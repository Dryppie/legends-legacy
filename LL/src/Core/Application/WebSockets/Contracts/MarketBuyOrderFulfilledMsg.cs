using Application.UseCases.Inventories.Dtos;
using Application.UseCases.MarketPlaces.Dtos.Responses;

namespace Application.WebSockets.Contracts;

public record MarketBuyOrderFulfilledMsg(
    Guid BuyOrderId,
    Guid BuyerId,
    Guid SellerId,
    int Quantity,
    long TotalPrice,
    long SellerCinders,
    InventoryItemDto PurchasedItem,
    MarketPlaceBuyOrderDto? RemainingBuyOrder) : GameEventMsg;
