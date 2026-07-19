namespace Application.UseCases.MarketPlaces.Dtos.Responses;

public sealed record MarketPlaceItemSummaryDto(
    string ItemBaseId,
    long? LowestSellUnitPrice,
    long TotalSellQuantity,
    long? HighestBuyUnitPrice,
    long TotalBuyQuantity,
    long? LastTradeUnitPrice,
    decimal? MedianUnitPrice7Days,
    long TradeVolume24Hours);
