namespace Application.UseCases.MarketPlaces.Dtos.Requests;

public record FulfillMarketPlaceBuyOrderRequest(Guid MarketPlaceBuyOrderId, Guid ItemInstanceId, int Quantity);
