using Application.UseCases.MarketPlaces.Dtos.Responses;

namespace Application.WebSockets.Contracts;

public record MarketBuyOrderCreatedMsg(MarketPlaceBuyOrderDto BuyOrder) : GameEventMsg;
