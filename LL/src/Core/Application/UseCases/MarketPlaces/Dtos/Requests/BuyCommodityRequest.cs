namespace Application.UseCases.MarketPlaces.Dtos.Requests;

public sealed record BuyCommodityRequest(
    string ItemBaseId,
    int Quantity,
    long MaximumUnitPrice);
