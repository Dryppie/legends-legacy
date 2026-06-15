namespace Application.WebSockets.Contracts;

public record MarketListingCanceledMsg(Guid ListingId, Guid SellerId) : GameEventMsg;
