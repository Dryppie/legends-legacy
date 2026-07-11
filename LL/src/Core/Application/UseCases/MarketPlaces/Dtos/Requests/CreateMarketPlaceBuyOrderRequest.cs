namespace Application.UseCases.MarketPlaces.Dtos.Requests;

public record CreateMarketPlaceBuyOrderRequest(string ItemBaseId, int Quantity, long UnitPrice);
