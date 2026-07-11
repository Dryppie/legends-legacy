namespace Application.WebSockets.Contracts;

public record MarketBuyOrderCanceledMsg(Guid BuyOrderId, Guid BuyerId) : GameEventMsg;
