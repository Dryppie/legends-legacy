namespace Application.UseCases.MarketPlaces.Dtos.Requests;

public sealed record SellCommodityRequest(
    Guid ItemInstanceId,
    int Quantity,
    long MinimumUnitPrice);
