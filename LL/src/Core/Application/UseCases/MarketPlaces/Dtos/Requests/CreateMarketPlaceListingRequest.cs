namespace Application.UseCases.MarketPlaces.Dtos.Requests;
public record CreateMarketPlaceListingRequest(Guid ItemInstanceId, int Quantity, long UnitPrice);
