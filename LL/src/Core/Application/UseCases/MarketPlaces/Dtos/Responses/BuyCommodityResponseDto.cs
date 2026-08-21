namespace Application.UseCases.MarketPlaces.Dtos.Responses;

public sealed record BuyCommodityResponseDto(
    int FilledQuantity,
    long TotalPrice,
    long BuyerCinders,
    MarketplaceChangeSetDto Marketplace);
